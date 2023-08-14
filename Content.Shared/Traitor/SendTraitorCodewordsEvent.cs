using Robust.Shared.Serialization;

namespace Content.Shared.Traitor;

[Serializable, NetSerializable]
public sealed class SendTraitorCodewordsEvent : EntityEventArgs
{
    public readonly string[] Words;

    public SendTraitorCodewordsEvent(string[] words)
    {
        Words = words;
    }
}
