using Content.Shared.Chat;

namespace Content.Client.Chat;

[ByRefEvent]
public record struct PrepareChatMessageEvent(ChatMessage Msg);
