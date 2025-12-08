using Asfoo.Models;

namespace Asfoor.Api.Services;

/// <summary>
/// Service interface for processing chat requests with AI agents.
/// </summary>
public interface IChatService
{
    /// <summary>
    /// Processes a chat request and returns a response from the AI agent.
    /// </summary>
    /// <param name="request">The chat request containing the message, attachments, and thread information.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the chat response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the chat processing fails.</exception>
    Task<CustomChatResponse> ProcessChatRequestAsync(ChatRequest request);
    /// <summary>
    /// Processes a chat request and returns a response from the AI agent.
    /// </summary>
    /// <param name="request">The chat request containing the message, attachments, and thread information.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains the chat response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="request"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when the chat processing fails.</exception>
    Task<CustomChatResponse> ProcessChatRequestWithIntentAsync(ChatRequest request);
}
