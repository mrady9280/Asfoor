using Asfoo.Models;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Reflection;

namespace Asfoor.Api.Services.Executors;

public class IntentExecutor(IAgentFactory factory, ILogger<IntentExecutor> logger)
    : ReflectingExecutor<IntentExecutor>("IntentExecutor"), IMessageHandler<ChatRequest, IntentExecutor.AgentType>
{
    public async ValueTask<AgentType> HandleAsync(ChatRequest request, IWorkflowContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        try
        {
            logger.LogInformation($"Intent : Handling {nameof(ChatRequest)}: {request.Message}");
            var agent = await factory.CreateIntentAgentAsync();
            var response = await agent.RunAsync<AgentType>(request.Message, cancellationToken: cancellationToken);
            return response.Result;
        }
        catch (Exception e)
        {
            logger.LogError(e, $"Intent : Handling {nameof(ChatRequest)}: {request.Message}");
            throw;
        }
    }
    public enum AgentType
    {
        IntentAgent,
        ImageAgent,
        FileAgent,
        AudioAgent
    }
}