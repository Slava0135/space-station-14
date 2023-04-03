using System.Linq;
using Content.Server.Chemistry.Components;
using Content.Server.Chemistry.ReactionEffects;
using Content.Shared.Chemistry.Reaction;
using JetBrains.Annotations;

namespace Content.Server.Chemistry.EntitySystems;

[UsedImplicitly]
public sealed class SolutionAreaEffectSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<SolutionAreaEffectComponent, ReactionAttemptEvent>(OnReactionAttempt);
    }

    public override void Update(float frameTime)
    {
        foreach (var inception in EntityManager.EntityQuery<SolutionAreaEffectInceptionComponent>().ToArray())
        {
            inception.InceptionUpdate(frameTime);
        }
    }

    /// <summary>
    /// Adds an <see cref="SolutionAreaEffectInceptionComponent"/> to entity so the effect starts spreading and reacting.
    /// </summary>
    /// <param name="amount">The range of the effect</param>
    public void Start(EntityUid uid, SolutionAreaEffectComponent component, int amount, float duration, float spreadDelay, float removeDelay)
    {
        if (component.Inception != null || HasComp<SolutionAreaEffectInceptionComponent>(uid))
            return;

        component.Amount = amount;
        var inception = AddComp<SolutionAreaEffectInceptionComponent>(uid);

        inception.Add(component);
        inception.Setup(amount, duration, spreadDelay, removeDelay);
    }

    private void OnReactionAttempt(EntityUid uid, SolutionAreaEffectComponent component, ref ReactionAttemptEvent args)
    {
        if (args.Solution.Name != SolutionAreaEffectComponent.SolutionName)
            return;

        // Prevent smoke/foam fork bombs (smoke creating more smoke).
        foreach (var effect in args.Reaction.Effects)
        {
            if (effect is AreaReactionEffect)
            {
                args.Cancelled = true;
                return;
            }
        }
    }
}
