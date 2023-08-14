using System.Text.RegularExpressions;
using Content.Client.Chat;
using Content.Shared.Traitor;

namespace Content.Client.Traitor;

public sealed class CodewordsSystem : EntitySystem
{
    private const string Prefix = "[bold][color=red]";
    private const string Suffix = "[/color][/bold]";

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
        var msg = ev.Msg.Message;
        foreach (var word in _words)
        {
            var sub = Prefix + "$0" + Suffix;
            msg = Regex.Replace(msg, word, sub, RegexOptions.IgnoreCase);
        }
        ev.Msg.WrappedMessage = ev.Msg.WrappedMessage.Replace(ev.Msg.Message, msg);
    }
}
