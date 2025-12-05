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

    private readonly OpenAIClient _openAiClient;
    private readonly IConfiguration _configuration;
    private readonly Tools.Tools _tools;
    private readonly ILogger<ChatService> _logger;
    private readonly string _chatModel;
    private readonly string _imageModel;

    #endregion

    #region Constructor

    /// <summary>
    /// Initializes a new instance of the <see cref="ChatService"/> class.
    /// </summary>
    /// <param name="openAiClient">The OpenAI client for AI operations.</param>
    /// <param name="configuration">Application configuration.</param>
    /// <param name="tools">Tools available to the AI agent.</param>
    /// <param name="logger">Logger for diagnostic information.</param>
    public ChatService(
        OpenAIClient openAiClient,
        IConfiguration configuration,
        Tools.Tools tools,
        ILogger<ChatService> logger)
    {
        _openAiClient = openAiClient ?? throw new ArgumentNullException(nameof(openAiClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _tools = tools ?? throw new ArgumentNullException(nameof(tools));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        // Cache frequently accessed configuration values
        _chatModel = _configuration[ChatServiceConstants.ChatModelConfigKey]
                     ?? throw new InvalidOperationException(
                         $"Configuration key '{ChatServiceConstants.ChatModelConfigKey}' is missing.");
        _imageModel = _configuration[ChatServiceConstants.ImageModelConfigKey]
                      ?? throw new InvalidOperationException(
                          $"Configuration key '{ChatServiceConstants.ImageModelConfigKey}' is missing.");
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
            var agent = await CreateAgentForRequestAsync(request);

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
    private ChatMessage CreateUserMessage(ChatRequest request)
    {
        if (!request.Attachments.Any())
        {
            return new ChatMessage(ChatRole.User, request.Message);
        }

        var contents = new List<AIContent> { new TextContent(request.Message) };

        foreach (var attachment in request.Attachments)
        {
            contents.Add(new DataContent(attachment.Data, attachment.ContentType));
        }

        return new ChatMessage(ChatRole.User, contents);
    }

    #endregion

    #region Private Methods - Agent Creation

    /// <summary>
    /// Creates the appropriate agent based on the request type (with or without attachments).
    /// </summary>
    [Experimental("OPENAI001")]
    private async Task<AIAgent> CreateAgentForRequestAsync(ChatRequest request)
    {
        var reasoningLevel = request.ReasoningEffortLevel ?? ChatServiceConstants.DefaultReasoningEffortLevel;
        return request.Attachments.Any()
            ? CreateImageAgent()
            : await CreateChatAgentWithToolsAsync(reasoningLevel);
    }

    /// <summary>
    /// Creates an agent for processing requests with image attachments.
    /// </summary>
    private AIAgent CreateImageAgent()
    {
        _logger.LogDebug("Creating image processing agent");

        return _openAiClient
            .GetChatClient(_imageModel)
            .AsIChatClient()
            .CreateAIAgent();
    }

    /// <summary>
    /// Creates a chat agent with search tools and memory capabilities.
    /// </summary>
    /// <param name="reasoningEffortLevel">The reasoning effort level to use for the agent.</param>
    [Experimental("OPENAI001")]
    private async Task<AIAgent> CreateChatAgentWithToolsAsync(string? reasoningEffortLevel = null)
    {
        _logger.LogDebug("Creating chat agent with tools with reasoning level: {ReasoningLevel}", reasoningEffortLevel);

        var searchFunc = AIFunctionFactory.Create(_tools.SearchAsync);
        var parsedReasoningLevel = ParseReasoningEffortLevel(reasoningEffortLevel);
        var memoryAgent = CreateMemoryAgent(parsedReasoningLevel);

        var agent = _openAiClient
            .GetChatClient(_chatModel)
            .AsIChatClient()
            .CreateAIAgent(
                options: new ChatClientAgentOptions
                {
                    Instructions = ChatServiceConstants.ChatAgentInstructions,
                    Name = ChatServiceConstants.ChatAgentName,
                    AIContextProviderFactory = _ => new CustomContextProvider(
                        memoryAgent,
                        _configuration,
                        ChatServiceConstants.DefaultUserId),
                    ChatOptions = new ChatOptions
                    {
                        Tools = [searchFunc],
                        RawRepresentationFactory = _ => new ChatCompletionOptions
                        {
#pragma warning disable OPENAI001
                            ReasoningEffortLevel = parsedReasoningLevel
#pragma warning restore OPENAI001
                        },
                    }
                })
            .AsBuilder()
            .UseOpenTelemetry()
            .Use((agent, context, next, ct) =>
                Middlewares.FunctionCallMiddleware(agent, context, next, ct, _logger))
            .Build();

        return await Task.FromResult(agent);
    }

    /// <summary>
    /// Creates an agent for extracting and managing user memory.
    /// </summary>
    [Experimental("OPENAI001")]
    private ChatClientAgent CreateMemoryAgent(ChatReasoningEffortLevel reasoningEffortLevel)
    {
        _logger.LogDebug("Creating memory extraction agent");

        return _openAiClient
            .GetChatClient(_chatModel)
            .AsIChatClient()
            .CreateAIAgent(
                options: new ChatClientAgentOptions
                {
                    Instructions = ChatServiceConstants.MemoryAgentInstructions,
                    ChatOptions = new ChatOptions
                    {
                        RawRepresentationFactory = _ => new ChatCompletionOptions
                        {
#pragma warning disable OPENAI001
                            ReasoningEffortLevel = reasoningEffortLevel
#pragma warning restore OPENAI001
                        },
                    }
                });
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

    #region Private Methods - Helpers

    /// <summary>
    /// Parses the reasoning effort level string to the corresponding enum value.
    /// </summary>
    /// <param name="level">The reasoning effort level as a string.</param>
    /// <returns>The corresponding ChatReasoningEffortLevel enum value.</returns>
#pragma warning disable OPENAI001
    private static ChatReasoningEffortLevel ParseReasoningEffortLevel(string? level)
    {
        return level?.ToLowerInvariant() switch
        {
            "low" => ChatReasoningEffortLevel.Low,
            "medium" => ChatReasoningEffortLevel.Medium,
            "high" => ChatReasoningEffortLevel.High,
            "minimal" => ChatReasoningEffortLevel.Minimal,
            _ => ChatReasoningEffortLevel.Medium // Default to Medium
        };
    }
#pragma warning restore OPENAI001

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