using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.VectorData;
using OpenAI;
using OpenAI.Chat;
using ChatMessage = Microsoft.Extensions.AI.ChatMessage;

namespace Asfoor.Api.Services.Ingestion;

public class DocumentIngestor(
    VectorStoreCollection<Guid, IngestedChunk> vectorCollection,
    IEmbeddingGenerator<string, Embedding<float>> embeddingGenerator,
    OpenAIClient openAiClient,
    IConfiguration configuration,
    ILogger<DocumentIngestor> logger)
{
    public async Task IngestDocumentsAsync()
    {
        try
        {
            // Ensure the Qdrant collection exists before attempting any upserts.
            await vectorCollection.EnsureCollectionExistsAsync();

            var agent = CreateAgent();

            var files = Directory.GetFiles(configuration["docPath"] ?? throw new InvalidOperationException(), "*.md",
                SearchOption.AllDirectories);
            foreach (var file in files)
            {
                var content = await File.ReadAllTextAsync(file);
                var response = await agent.RunAsync(new ChatMessage(ChatRole.User, content));
                var embedding = await embeddingGenerator.GenerateAsync([response.Text]);

                // Generate a deterministic GUID from the file path
                var fileId = GenerateGuidFromString(file);

                var chunk = new IngestedChunk
                {
                    Id = fileId,
                    Key = file,
                    DocumentId = file,
                    Text = response.Text,
                    Vector = embedding[0].Vector,
                    Context = content
                };
                await vectorCollection.UpsertAsync(chunk);
                logger.LogInformation($"Ingesting document {chunk.Key}");
            }
        }
        catch (Exception e)
        {
            logger.LogError(e.ToString());
            throw;
        }
    }


    private AIAgent CreateAgent()
    {
        var agent = openAiClient.GetChatClient(configuration["chatModel"]).AsIChatClient().CreateAIAgent(
            options: new ChatClientAgentOptions()
            {
                Instructions =
                    "As your personal assistant, you are responsible for rephrasing and formatting this content into a concise context that is ready for embedding. " +
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
        return agent;
    }

    private static Guid GenerateGuidFromString(string input)
    {
        using var md5 = System.Security.Cryptography.MD5.Create();
        var hash = md5.ComputeHash(System.Text.Encoding.UTF8.GetBytes(input));
        return new Guid(hash);
    }
}