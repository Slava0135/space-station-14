using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Server.Fluids.Components;
using Content.Shared;
using Content.Shared.Directions;
using Content.Shared.Fluids;
using Content.Shared.Maps;
using Content.Shared.Physics;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;

namespace Content.Server.Fluids.EntitySystems;

/// <summary>
/// Component that governs overflowing puddles. Controls how Puddles spread and updat
/// </summary>
[UsedImplicitly]
public sealed class FluidSpreaderSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _gameTiming = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly PuddleSystem _puddleSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;

    /// <summary>
    /// Adds an overflow component to the map data component tracking overflowing puddles
    /// </summary>
    /// <param name="puddleUid">EntityUid of overflowing puddle</param>
    /// <param name="puddle">Optional PuddleComponent</param>
    /// <param name="xform">Optional TransformComponent</param>
    public void AddOverflowingPuddle(EntityUid puddleUid, PuddleComponent? puddle = null,
        TransformComponent? xform = null)
    {
        if (!Resolve(puddleUid, ref puddle, ref xform, false) || xform.MapUid == null)
            return;

        var mapId = xform.MapUid.Value;

        EntityManager.EnsureComponent<FluidMapDataComponent>(mapId, out var component);
        component.Puddles.Add(puddleUid);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);
        Span<Direction> exploreDirections = stackalloc Direction[]
        {
            Direction.North,
            Direction.East,
            Direction.South,
            Direction.West,
        };
        var puddles = new List<PuddleComponent>(4);
        var puddleQuery = GetEntityQuery<PuddleComponent>();
        var xFormQuery = GetEntityQuery<TransformComponent>();

        foreach (var fluidMapData in EntityQuery<FluidMapDataComponent>())
        {
            if (fluidMapData.Puddles.Count == 0 || _gameTiming.CurTime <= fluidMapData.GoalTime)
                continue;

            var newIteration = new HashSet<EntityUid>();
            foreach (var puddleUid in fluidMapData.Puddles)
            {
                if (!puddleQuery.TryGetComponent(puddleUid, out var puddle)
                    || !xFormQuery.TryGetComponent(puddleUid, out var transform)
                    || !_mapManager.TryGetGrid(transform.GridUid, out var mapGrid))
                    continue;

                puddles.Clear();
                var pos = transform.Coordinates;

                var totalVolume = _puddleSystem.CurrentVolume(puddleUid, puddle);
                exploreDirections.Shuffle();
                foreach (var direction in exploreDirections)
                {
                    var newPos = pos.Offset(direction);
                    if (CheckTile(puddleUid, puddle, newPos, mapGrid, out var uid, out var component))
                    {
                        if (_puddleSystem.CurrentVolume(uid.Value, component) <= _puddleSystem.CurrentVolume(puddleUid, puddle))
                        {
                            puddles.Add(component);
                            totalVolume += _puddleSystem.CurrentVolume(uid.Value, component);
                            puddle.Flow |= direction.AsFlag();
                            component.Flow |= direction.GetOpposite().AsFlag();
                        } 
                        else 
                        {
                            puddle.Flow &= ~direction.AsFlag();
                            component.Flow &= ~direction.GetOpposite().AsFlag();
                        }
                    } 
                    else
                    {
                        puddle.Flow &= ~direction.AsFlag();
                    }
                }
                _puddleSystem.EqualizePuddles(puddleUid, puddles, totalVolume, newIteration, puddle);
            }

            fluidMapData.Puddles.Clear();
            fluidMapData.Puddles.UnionWith(newIteration);
            fluidMapData.UpdateGoal(_gameTiming.CurTime);
        }

        foreach(var (appearance, puddle) in EntityQuery<AppearanceComponent, PuddleComponent>())
        {
            var uid = appearance.Owner;
            _appearance.SetData(uid, PuddleVisuals.South, (puddle.Flow & DirectionFlag.South) != 0, appearance);
            _appearance.SetData(uid, PuddleVisuals.East, (puddle.Flow & DirectionFlag.East) != 0, appearance);
            _appearance.SetData(uid, PuddleVisuals.North, (puddle.Flow & DirectionFlag.North) != 0, appearance);
            _appearance.SetData(uid, PuddleVisuals.West, (puddle.Flow & DirectionFlag.West) != 0, appearance);
        }
    }


    /// <summary>
    /// Check a tile is valid for solution allocation.
    /// </summary>
    /// <param name="srcUid">Entity Uid of original puddle</param>
    /// <param name="srcPuddle">PuddleComponent attached to srcUid</param>
    /// <param name="dstPos">at which to check tile</param>
    /// <param name="mapGrid">helper param needed to extract entities</param>
    /// <param name="newPuddleUid">either found or newly created PuddleComponent.</param>
    /// <returns>true if tile is empty or occupied by a non-overflowing puddle (or a puddle close to being overflowing)</returns>
    private bool CheckTile(EntityUid srcUid, PuddleComponent srcPuddle, EntityCoordinates dstPos, MapGridComponent mapGrid,
        [NotNullWhen(true)] out EntityUid? newPuddleUid, [NotNullWhen(true)] out PuddleComponent? newPuddleComp)
    {
        if (!mapGrid.TryGetTileRef(dstPos, out var tileRef)
            || tileRef.Tile.IsEmpty)
        {
            newPuddleUid = null;
            newPuddleComp = null;
            return false;
        }

        // check if puddle can spread there at all
        var dst = dstPos.ToMapPos(EntityManager, _transform);
        var src = Transform(srcUid).Coordinates.ToMapPos(EntityManager, _transform);
        var dir = src - dst;
        var ray = new CollisionRay(dst, dir.Normalized, (int) CollisionGroup.MobMask);
        var mapId = dstPos.ToMap(EntityManager, _transform).MapId;
        var results = _physics.IntersectRay(mapId, ray, dir.Length, returnOnFirstHit: true);
        if (results.Count() > 0)
        {
            newPuddleUid = null;
            newPuddleComp = null;
            return false;
        }

        foreach (var entity in dstPos.GetEntitiesInTile())
        {
            if (TryComp<PuddleComponent>(entity, out var existingPuddle))
            {
                newPuddleUid = entity;
                newPuddleComp = existingPuddle;
                return true;
            }
        }

        _puddleSystem.SpawnPuddle(srcUid, dstPos, srcPuddle, out var uid, out var comp);
        newPuddleUid = uid;
        newPuddleComp = comp;
        return true;
    }
}
