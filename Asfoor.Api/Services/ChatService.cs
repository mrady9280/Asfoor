using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using Asfoo.Models;
using Asfoor.Api.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Asfoor.Api.Services;

/// <summary>
/// Service for processing chat requests with AI agents, supporting text and image inputs,
/// semantic search, and conversation memory.
/// </summary>
public class ChatService : IChatService
{
    #region Fields

    private readonly IAgentFactory _agentFactory;
    private readonly ILogger<ChatService> _logger;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class.
    /// </summary>
    /// <param name="agentFactory">The factory for creating AI agents.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public ChatService(
        IAgentFactory agentFactory,
        ILogger<ChatService> logger)
    {
        _agentFactory = agentFactory ?? throw new ArgumentNullException(nameof(agentFactory));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    #endregion

    #region Public Methods

    /// <summary>
    /// Processes a chat request and returns a response from the AI agent.
    /// </summary>
    /// <param name="request">The chat request containing the message, attachments, and thread information.</param>
    /// <returns>A chat response with the AI's reply, updated thread state, and usage information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when request is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown when agent execution fails.</exception>
    [Experimental("OPENAI001")]
    public async Task<CustomChatResponse> ProcessChatRequestAsync(ChatRequest request)
    {
        if (request == null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        try
        {
            _logger.LogInformation("Processing chat request with {AttachmentCount} attachments",
                request.Attachments.Count);

            // Create appropriate user message and agent based on request type
            var userMessage = CreateUserMessage(request);
            
            var agent = await _agentFactory.CreateChatAgentWithToolsAsync(request);

            // Get or deserialize thread
            var thread = GetOrCreateThread(agent, request.ThreadString);

            // Execute agent and get response
            var response = await ExecuteAgentAsync(agent, userMessage, thread);

            // Build and return response
            return BuildResponse(response, thread);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing chat request");
            throw new InvalidOperationException("Failed to process chat request. See inner exception for details.", ex);
        }
    }

    #endregion

    #region Private Methods - Message Creation

    /// <summary>
    /// Creates a user message from the chat request.
    /// </summary>
    /// <summary>
    /// Creates a user message from the chat request.
    /// </summary>
    private ChatMessage CreateUserMessage(ChatRequest request)
    {
        if (!request.Attachments.Any())
        {
            return new ChatMessage(ChatRole.User, request.Message);
        }

        // For requests with attachments, we don't send the data directly to the main reasoning agent.
        // Instead, we inform it about the attachments so it can decide to use the AnalyzeImages tool.
        var messageWithNotification = request.Message + 
                                      $"\n\n[System Notification: The user has attached {request.Attachments.Count} image(s). Use the 'AnalyzeImages' tool to inspect them if necessary.]";
        
        return new ChatMessage(ChatRole.User, messageWithNotification);
    }

    #endregion

    #region Private Methods - Thread Management

    /// <summary>
    /// Gets an existing thread or creates a new one.
    /// </summary>
    private AgentThread GetOrCreateThread(AIAgent agent, string threadString)
    {
        if (string.IsNullOrEmpty(threadString))
        {
            _logger.LogDebug("Creating new thread");
            return agent.GetNewThread();
        }

        _logger.LogDebug("Deserializing existing thread");
        var threadJson = JsonSerializer.Deserialize<JsonElement>(threadString, JsonSerializerOptions.Web);
        return agent.DeserializeThread(threadJson);
    }

    #endregion

    #region Private Methods - Agent Execution

    /// <summary>
    /// Executes the agent with the given message and thread.
    /// </summary>
    private async Task<AgentRunResponse> ExecuteAgentAsync(
        AIAgent agent,
        ChatMessage userMessage,
        AgentThread thread)
    {
        _logger.LogDebug("Executing agent");

        var response = await agent.RunAsync(userMessage, thread);

        LogUsageInformation(response);

        return response;
    }

    #endregion

    #region Private Methods - Response Building

    /// <summary>
    /// Builds the chat response from the agent's output.
    /// </summary>
    private CustomChatResponse BuildResponse(AgentRunResponse response, AgentThread thread)
    {
        var usageLog = FormatUsageLog(response);

        return new CustomChatResponse
        {
            Response = response.Text,
            ThreadString = thread.Serialize(JsonSerializerOptions.Web).GetRawText(),
            Usage = usageLog
        };
    }

    #endregion

    #region Private Methods - Logging

    /// <summary>
    /// Logs usage information from the agent response.
    /// </summary>
    private void LogUsageInformation(AgentRunResponse response)
    {
        var usageLog = FormatUsageLog(response);
        _logger.LogInformation(usageLog);
    }

    /// <summary>
    /// Formats usage information as a log string.
    /// </summary>
    private static string FormatUsageLog(AgentRunResponse response)
    {
        return $"Input Tokens: {response.Usage?.InputTokenCount}, " +
               $"Output Tokens: {response.Usage?.OutputTokenCount}, " +
               $"Total Tokens: {response.Usage?.TotalTokenCount}";
    }

    #endregion
}