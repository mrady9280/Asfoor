using Asfoo.Models;
using Microsoft.Extensions.AI;

namespace Asfoor.Shared.Services;

public interface IAiService
{
    Task<CustomChatResponse?> ChatAsync(string message, string conversationId, List<ChatFileAttachment>? attachments = null,
        List<ChatMessage>? chatMessages = null, string threadString = "", string? reasoningEffortLevel = "Medium");

    Task IngestDocumentsAsync();
    Task SaveChatHistoryAsync(string conversationId, List<ChatMessage> messages);
}