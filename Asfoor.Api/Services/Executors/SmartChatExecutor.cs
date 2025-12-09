using Asfoo.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;

namespace Asfoor.Api.Services.Executors;

public class SmartChatExecutor(IAgentFactory factory, ILogger<SmartChatExecutor> logger, string message = "")
    : ReflectingExecutor<ImageExecutor>("SmartChatExecutor"), IMessageHandler<string, string>
{
    public async ValueTask<string> HandleAsync(string request, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            logger.LogInformation($"Image Agent : Handling {nameof(ChatRequest)}: {request}");
            var agent = await factory.CreateSmartChatAgentAsync();
            var input = message.Length > 0 ? message : request;
            var imageMessage = new ChatMessage(ChatRole.User, new List<AIContent>()
            {
                new TextContent(input),
            });
            var response = await agent.RunAsync<AgentChatResponse>(imageMessage, cancellationToken: cancellationToken);
            logger.LogInformation($"Image Agent : Success {nameof(ChatRequest)}: {response.Result.Message}");
            return response.Result.Message;
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Image Agent : Failure {nameof(ChatRequest)}: {request}");
            throw;
        }
    }
}