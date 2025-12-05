using JetBrains.Annotations;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Asfoor.Api.Tools;

public class CustomContextProvider : AIContextProvider
{
    private readonly ChatClientAgent _memoryExtractorAgent;
    private readonly List<string> _userFacts = [];
    private readonly string _userMemoryFilePath;

    public CustomContextProvider(ChatClientAgent memoryExtractorAgent, IConfiguration configuration, string userId)
    {
        _memoryExtractorAgent = memoryExtractorAgent;
        _userMemoryFilePath = Path.Combine(configuration["docPath"], $"{userId}.txt");
        if (File.Exists(_userMemoryFilePath))
        {
            _userFacts.AddRange(File.ReadAllLines(_userMemoryFilePath));
        }
    }

    public override ValueTask<AIContext> InvokingAsync(InvokingContext context,
        CancellationToken cancellationToken = default)
    {
        return new ValueTask<AIContext>(new AIContext
        {
            Instructions = string.Join(" | ", _userFacts)
        });
    }

    public override async ValueTask InvokedAsync(InvokedContext context,
        CancellationToken cancellationToken = new CancellationToken())
    {
        ChatMessage lastMessageFromUser = context.RequestMessages.Last();
        List<ChatMessage> inputToMemoryExtractor =
        [
            new(ChatRole.Assistant,
                $"We know the following about the user already and should not extract that again: {string.Join(" | ", _userFacts)}"),
            lastMessageFromUser
        ];

        ChatClientAgentRunResponse<MemoryUpdate> response =
            await _memoryExtractorAgent.RunAsync<MemoryUpdate>(inputToMemoryExtractor,
                cancellationToken: cancellationToken);
        foreach (string memoryToRemove in response.Result.MemoryToRemove)
        {
            _userFacts.Remove(memoryToRemove);
        }

        _userFacts.AddRange(response.Result.MemoryToAdd);
        await File.WriteAllLinesAsync(_userMemoryFilePath, _userFacts.Distinct(), cancellationToken);
    }

    [UsedImplicitly]
    private record MemoryUpdate(List<string> MemoryToAdd, List<string> MemoryToRemove);
}