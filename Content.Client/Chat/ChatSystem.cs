using Content.Shared.Chat;

namespace Content.Client.Chat;

public sealed class ChatSystem : SharedChatSystem
{
    public void PrepareMessage(ChatMessage msg)
    {
        var ev = new PrepareChatMessageEvent(msg);
        RaiseLocalEvent(ref ev);
    }
}
