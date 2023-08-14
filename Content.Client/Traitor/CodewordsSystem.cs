using Content.Client.Chat;
using Content.Shared.Traitor;

namespace Content.Client.Traitor;

public sealed class CodewordsSystem : EntitySystem
{
    private const string _prefix = "[bold][color=red]";
    private const string _suffix = "[/color][/bold]";

    private string[]? _words;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<SendTraitorCodewordsEvent>(OnSendTraitorCodewordsEvent);

        SubscribeLocalEvent<PrepareChatMessageEvent>(OnChatMessage);
    }

    private void OnSendTraitorCodewordsEvent(SendTraitorCodewordsEvent ev)
    {
        _words = ev.Words;
    }

    private void OnChatMessage(ref PrepareChatMessageEvent ev)
    {
        if (_words == null)
        {
            return;
        }
        var msg = ev.Msg.WrappedMessage;
        foreach (var word in _words)
        {
            var sub = _prefix + word + _suffix;
            msg = msg.Replace(word, sub);
        }
        ev.Msg.WrappedMessage = msg;
    }
}
