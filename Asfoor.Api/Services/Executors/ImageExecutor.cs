using Asfoo.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;
using Microsoft.Extensions.AI;

namespace Asfoor.Api.Services.Executors;

public class ImageExecutor(IAgentFactory factory, ILogger<ImageExecutor> logger)
    : ReflectingExecutor<ImageExecutor>("ImageExecutor"), IMessageHandler<ChatRequest, AgentChatResponse>
{
    public async ValueTask<AgentChatResponse> HandleAsync(ChatRequest request, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            logger.LogInformation($"Image Agent : Handling {nameof(ChatRequest)}: {request.Message}");
            var agent = await factory.CreateImageAgentAsync();
            var imageMessage = new ChatMessage(ChatRole.User, new List<AIContent>()
            {
                new TextContent(request.Message),
                new DataContent(request.Attachments.First().Data, request.Attachments.First().ContentType)
            });
            var response = await agent.RunAsync<AgentChatResponse>(imageMessage, cancellationToken: cancellationToken);
            logger.LogInformation($"Image Agent : Success {nameof(ChatRequest)}: {response.Result.Message}");
            return response.Result;
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Image Agent : Failure {nameof(ChatRequest)}: {request.Message}");
            throw;
        }
    }
}

public record AgentChatResponse(string Thinking, string Message);