using System.Net.Http.Json;
using Asfoo.Models;
using Microsoft.Extensions.AI;

namespace Asfoor.Shared.Services;

public class AiApiService(HttpClient httpClient) : IAiService
{
    public async Task<CustomChatResponse?> ChatAsync(string message, string conversationId, List<ChatFileAttachment>? attachments = null,
        List<ChatMessage>? chatMessages = null, string threadString = "", string? reasoningEffortLevel = "Medium")
    {
        var request = new ChatRequest
        {
            Message = message,
            ConversationId = conversationId,
            Attachments = attachments ?? new List<ChatFileAttachment>(),
            ThreadString = threadString,
            ReasoningEffortLevel = reasoningEffortLevel,
            History = new ChatHistory()
            {
                ConversationId = conversationId,
                // Messages = chatMessages ?? new List<ChatMessage>()
            }
        };

        var response = await httpClient.PostAsJsonAsync("/smart-chat", request);
        response.EnsureSuccessStatusCode();

        var result = await response.Content.ReadFromJsonAsync<CustomChatResponse>();
        return result;
    }

    public async Task IngestDocumentsAsync()
    {
        var response = await httpClient.PostAsync("ingest", null);
        response.EnsureSuccessStatusCode();
    }

    public async Task SaveChatHistoryAsync(string conversationId, List<ChatMessage> messages)
    {
        var request = new
        {
            Messages = messages,
            ConversationId = conversationId
        };
        var response = await httpClient.PostAsJsonAsync("chat/history", request);
        response.EnsureSuccessStatusCode();
    }
    
}