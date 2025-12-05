namespace Asfoo.Models;

public class ChatRequest
{

    public string Message { get; set; } = "";
    public string ConversationId { get; set; } = "";
    public string ThreadString { get; set; } = "";
    public ChatHistory History { get; set; } = new();
    public List<ChatFileAttachment> Attachments { get; set; } = new();
    public string? ReasoningEffortLevel { get; set; }
}