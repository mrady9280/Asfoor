using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Asfoor.Api.Services.Ingestion;

public class ChatHistoryIngestor(
    VectorStoreCollection<Guid, IngestedChunk> vectorCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    OpenAIClient openAiClient,
    IConfiguration configuration)
{
    public async Task IngestThreadAsync(string conversationId, IEnumerable<ChatMessage> messages)
    {
        try
        {
            var agent = openAiClient.GetChatClient(configuration["chatModel"]).AsIChatClient().CreateAIAgent(
                options: new ChatClientAgentOptions()
                {
                    Instructions =
                        "As your personal assistant, You are responsible for rephrasing and formatting the chat history extract all personal information from it" +
                        " into a concise context that is ready for embedding. " +
                        "This ensures that it is easily retrievable later in the search.",
                    Name = "Asfor",
                    ChatOptions = new ChatOptions()
                    {
                        RawRepresentationFactory = _ => new ChatCompletionOptions()
                        {
#pragma warning disable OPENAI001
                            ReasoningEffortLevel = ChatReasoningEffortLevel.High
#pragma warning restore OPENAI001
                        }
                    }
                }).AsBuilder().UseOpenTelemetry().Build();

            var chatMessages = messages.ToList();
            var text = string.Join("\n", chatMessages.Select(m => $"{m.Role}: {m.Text}"));
            var response = await agent.RunAsync(new ChatMessage(ChatRole.User, text));
            var embedding = await embeddingGenerator.GenerateAsync([response.Text]);

            // Generate a deterministic GUID from the conversationId
            var id = GenerateGuidFromString(conversationId);

            var chunk = new IngestedChunk
            {
                Id = id,
                Key = conversationId,
                DocumentId = "PersonalInformation",
                Text = response.Text,
                Vector = embedding[0].Vector,
                Context = text
            };

            await vectorCollection.UpsertAsync(chunk);
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static Guid GenerateGuidFromString(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}