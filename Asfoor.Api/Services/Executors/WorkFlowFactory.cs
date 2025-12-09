using Microsoft.Agents.AI.Workflows;

namespace Asfoor.Api.Services.Executors;

public class WorkFlowFactory(
    IAgentFactory factory,
    ILogger<WorkFlowFactory> logger,
    IntentExecutor intentExecutor,
    ImageExecutor imageExecutor,
    SmartChatExecutor smartChatExecutor)
{
    public Workflow BuildWorkflowExecutors()
    {
        WorkflowBuilder wfBuilder = new WorkflowBuilder(intentExecutor);
        wfBuilder.AddEdge(source: intentExecutor, target: imageExecutor)
            .AddEdge(source: intentExecutor, target: smartChatExecutor)
            .AddEdge(source: imageExecutor, target: smartChatExecutor);
        wfBuilder.AddSwitch(source: intentExecutor, switchBuilder =>
        {
            switchBuilder.AddCase<IntentExecutor.AgentType>(x => x == IntentExecutor.AgentType.ImageAgent,
                imageExecutor);
            switchBuilder.AddCase<IntentExecutor.AgentType>(x => x == IntentExecutor.AgentType.IntentAgent,
                smartChatExecutor);
        });
        wfBuilder.AddSwitch(source: imageExecutor,
            switchBuilder =>
            {
                switchBuilder.AddCase<AgentChatResponse>(e => e.Message.Length > 0, smartChatExecutor);
            });
        var wf = wfBuilder.Build();
        return wf;
    }
}