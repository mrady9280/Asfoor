using System.Text;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace Asfoor.Api.Tools;

public class Middlewares
{
    public static async ValueTask<object?> FunctionCallMiddleware(
        AIAgent callingAgent, 
        FunctionInvocationContext context,
        Func<FunctionInvocationContext, CancellationToken, ValueTask<object?>> next,
        CancellationToken cancellationToken,
        ILogger logger)
    {
        StringBuilder functionCallDetails = new();
        functionCallDetails.Append($"- Tool Call: '{context.Function.Name}'");
        if (context.Arguments.Count > 0)
        {
            functionCallDetails.Append(
                $" (Args: {string.Join(",", context.Arguments.Select(x => $"[{x.Key} = {x.Value}]"))})");
        }

        logger.LogInformation(functionCallDetails.ToString());
        return await next(context, cancellationToken);
    }
}