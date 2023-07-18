using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Shared.Interaction.Components
{
    /// <summary>
    /// A simple clumsy tag-component.
    /// </summary>
    [RegisterComponent]
    public sealed class ClumsyComponent : Component
    {
        [DataField("clumsyDamage", required: true)]
        [ViewVariables(VVAccess.ReadWrite)]
        public DamageSpecifier ClumsyDamage = default!;

        /// <summary>
        ///     Sound to play when clumsy interactions fail
        /// </summary>
        [DataField("clumsySound")]
        public SoundSpecifier ClumsySound = new SoundPathSpecifier("/Audio/Items/bikehorn.ogg");

        /// <summary>
        ///     Funny sound to play when sleeping
        /// </summary>
        [DataField("sleepingSound")]
        public SoundSpecifier? SleepingSound;
    }
}
