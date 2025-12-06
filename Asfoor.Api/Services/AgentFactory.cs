using System.Diagnostics.CodeAnalysis;
using Asfoo.Models;
using Asfoor.Api.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using OpenAI;
using OpenAI.Chat;

namespace Asfoor.Api.Services;

public interface IAgentFactory
{
    Task<AIAgent> CreateChatAgentAsync(ChatRequest request);
}

public class AgentFactory : IAgentFactory
{
    private readonly OpenAIClient _openAiClient;
    private readonly IConfiguration _configuration;
    private readonly Tools.Tools _tools;
    private readonly ILogger<AgentFactory> _logger;
    private readonly string _chatModel;
    private readonly string _imageModel;

    public AgentFactory(
        OpenAIClient openAiClient,
        IConfiguration configuration,
        Tools.Tools tools,
        ILogger<AgentFactory> logger)
    {
        _openAiClient = openAiClient ?? throw new ArgumentNullException(nameof(openAiClient));
        _configuration = configuration ?? throw new ArgumentNullException(nameof(configuration));
        _tools = tools ?? throw new ArgumentNullException(nameof(tools));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));

        _chatModel = _configuration[ChatServiceConstants.ChatModelConfigKey]
                     ?? throw new InvalidOperationException(
                         $"Configuration key '{ChatServiceConstants.ChatModelConfigKey}' is missing.");
        _imageModel = _configuration[ChatServiceConstants.ImageModelConfigKey]
                      ?? throw new InvalidOperationException(
                          $"Configuration key '{ChatServiceConstants.ImageModelConfigKey}' is missing.");
    }

    /// <summary>
    /// Creates the appropriate agent based on the request type (with or without attachments).
    /// </summary>
    [Experimental("OPENAI001")]
    public async Task<AIAgent> CreateChatAgentAsync(ChatRequest request)
    {
        var reasoningLevel = request.ReasoningEffortLevel ?? ChatServiceConstants.DefaultReasoningEffortLevel;
        return await CreateChatAgentWithToolsAsync(reasoningLevel);
    }

    /// <summary>
    /// Creates a chat agent with search tools and memory capabilities.
    /// </summary>
    /// <param name="reasoningEffortLevel">The reasoning effort level to use for the agent.</param>
    [Experimental("OPENAI001")]
    private async Task<AIAgent> CreateChatAgentWithToolsAsync(string? reasoningEffortLevel = null)
    {
        _logger.LogDebug("Creating chat agent with tools with reasoning level: {ReasoningLevel}", reasoningEffortLevel);

        var searchFunc = AIFunctionFactory.Create(_tools.SearchAsync,"search-tool");
        var parsedReasoningLevel = ParseReasoningEffortLevel(reasoningEffortLevel);
        var memoryAgent = CreateMemoryAgent(parsedReasoningLevel);
        var imageAgent = _openAiClient.GetChatClient(_imageModel)
            .AsIChatClient()
            .CreateAIAgent();

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
                        Tools = [searchFunc, imageAgent.AsAIFunction()],
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
                Middlewares.FunctionCallMiddleware(agent, context, next, ct,
                    _logger)) // Note: Logger type mismatch potentially, Middlewares expecting ILogger?
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
}