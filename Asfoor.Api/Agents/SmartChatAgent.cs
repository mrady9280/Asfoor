using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Asfoor.Api.Agents;

public class SmartChatAgent<T>(ChatClientAgent imageAgent, ChatClientAgent chatAgent) : AIAgent
{
    public override AgentThread GetNewThread()
    {
        return chatAgent.GetNewThread();
    }

    public override AgentThread DeserializeThread(JsonElement serializedThread,
        JsonSerializerOptions? jsonSerializerOptions = null)
    {
        return chatAgent.DeserializeThread(serializedThread, jsonSerializerOptions);
    }

    public override async Task<AgentRunResponse> RunAsync(IEnumerable<ChatMessage> messages, AgentThread? thread = null,
        AgentRunOptions? options = null,
        CancellationToken cancellationToken = new CancellationToken())
    {
        throw new NotImplementedException();
    }

    public override async IAsyncEnumerable<AgentRunResponseUpdate> RunStreamingAsync(IEnumerable<ChatMessage> messages,
        AgentThread? thread = null, AgentRunOptions? options = null,
        [EnumeratorCancellation] CancellationToken cancellationToken = new CancellationToken())
    {
        // Track function calls that should trigger state events
        Dictionary<string, FunctionCallContent> trackedFunctionCalls = [];
        var attachmentsExists = messages.Any(e => e.Contents.Any(e => e is DataContent));
        if (attachmentsExists)
        {
            await foreach (var update in imageAgent.RunStreamingAsync(messages, thread, options, cancellationToken)
                               .ConfigureAwait(false))
            {
                yield return update;
            }
        }
        else
        {
            await foreach (var update in chatAgent.RunStreamingAsync(messages, thread, options, cancellationToken)
                               .ConfigureAwait(false))
            {
                yield return update;
            }
        }
    }
}