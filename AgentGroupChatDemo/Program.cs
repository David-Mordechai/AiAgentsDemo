/*
 * source:
 * https://github.dev/microsoft/semantic-kernel/blob/main/dotnet/samples/Concepts/Plugins/DescribeAllPluginsAndFunctions.cs
 */

using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.Agents.Chat;
using Microsoft.SemanticKernel.ChatCompletion;

const string reviewerName = "ArtDirector";
const string reviewerInstructions =
    """
        You are an art director who has opinions about copyrighting born of a love for David Ogilvy.
        The goal is to determine if the given copy is acceptable to print.
        If so, state that it is approved.
        If not, provide insight on how to refine suggested copy without examples.
        """;

const string copyWriterName = "CopyWriter";
const string copyWriterInstructions =
    """
        You are a copywriter with ten years of experience and are known for brevity and a dry humor.
        The goal is to refine and decide on the single best copy as an expert in the field.
        Only provide a single proposal per response.
        Never delimit the response with quotation marks.
        You're laser focused on the goal at hand.
        Don't waste time with chit chat.
        Consider suggestions when refining an idea.
        """;

// Define the agents
ChatCompletionAgent agentReviewer =
    new()
    {
        Instructions = reviewerInstructions,
        Name = reviewerName,
        Kernel = CreateKernelWithChatCompletion(),
    };

ChatCompletionAgent agentWriter =
    new()
    {
        Instructions = copyWriterInstructions,
        Name = copyWriterName,
        Kernel = CreateKernelWithChatCompletion(),
    };

#pragma warning disable SKEXP0110 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
var terminationFunction =
    AgentGroupChat.CreatePromptFunctionForStrategy(
        """
                Determine if the copy has been approved.  If so, respond with a single word: yes

                History:
                {{$history}}
                """,
        safeParameterNames: "history");

var selectionFunction =
    AgentGroupChat.CreatePromptFunctionForStrategy(
        $$$"""
                Determine which participant takes the next turn in a conversation based on the the most recent participant.
                State only the name of the participant to take the next turn.
                No participant should take more than one turn in a row.

                Choose only from these participants:
                - {{{reviewerName}}}
                - {{{copyWriterName}}}

                Always follow these rules when selecting the next participant:
                - After {{{copyWriterName}}}, it is {{{reviewerName}}}'s turn.
                - After {{{reviewerName}}}, it is {{{copyWriterName}}}'s turn.

                History:
                {{$history}}
                """,
        safeParameterNames: "history");

// Limit history used for selection and termination to the most recent message.
ChatHistoryTruncationReducer strategyReducer = new(1);

// Create a chat for agent interaction.
AgentGroupChat chat =
    new(agentWriter, agentReviewer)
    {
        ExecutionSettings =
            new AgentGroupChatSettings
            {
                // Here KernelFunctionTerminationStrategy will terminate
                // when the art-director has given their approval.
                TerminationStrategy =
                    new KernelFunctionTerminationStrategy(terminationFunction, CreateKernelWithChatCompletion())
                    {
                        // Only the art-director may approve.
                        Agents = [agentReviewer],
                        // Customer result parser to determine if the response is "yes"
                        ResultParser = (result) => result.GetValue<string>()?.Contains("yes", StringComparison.OrdinalIgnoreCase) ?? false,
                        // The prompt variable name for the history argument.
                        HistoryVariableName = "history",
                        // Limit total number of turns
                        MaximumIterations = 10,
                        // Save tokens by not including the entire history in the prompt
                        HistoryReducer = strategyReducer,
                    },
                // Here a KernelFunctionSelectionStrategy selects agents based on a prompt function.
                SelectionStrategy =
                    new KernelFunctionSelectionStrategy(selectionFunction, CreateKernelWithChatCompletion())
                    {
                        // Always start with the writer agent.
                        InitialAgent = agentWriter,
                        // Returns the entire result value as a string.
                        ResultParser = (result) => result.GetValue<string>() ?? copyWriterName,
                        // The prompt variable name for the history argument.
                        HistoryVariableName = "history",
                        // Save tokens by not including the entire history in the prompt
                        HistoryReducer = strategyReducer,
                        // Only include the agent names and not the message content
                        EvaluateNameOnly = true,
                    },
            }
    };

// Invoke chat and display messages.
ChatMessageContent message = new(AuthorRole.User, "concept: maps made out of egg cartons.");
chat.AddChatMessage(message);
WriteAgentChatMessage(message);

await foreach (var response in chat.InvokeAsync())
{
    WriteAgentChatMessage(response);
}

Console.WriteLine($"\n[IS COMPLETED: {chat.IsComplete}]");
return;

Kernel CreateKernelWithChatCompletion()
{
    var kernelBuilder = Kernel.CreateBuilder();

    #pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    kernelBuilder.AddOllamaChatCompletion("llama3.2", new Uri("http://localhost:11434"));

    return kernelBuilder.Build();
}

void WriteAgentChatMessage(ChatMessageContent chatMessageContent)
{
    // Include ChatMessageContent.AuthorName in output, if present.
    #pragma warning disable SKEXP0001 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
    var authorExpression = chatMessageContent.Role == AuthorRole.User ? string.Empty : $" - {chatMessageContent.AuthorName ?? "*"}";
    // Include TextContent (via ChatMessageContent.Content), if present.
    var contentExpression = string.IsNullOrWhiteSpace(chatMessageContent.Content) ? string.Empty : chatMessageContent.Content;
    Console.WriteLine($"\n# {chatMessageContent.Role}{authorExpression}: {contentExpression}");
    // Provide visibility for inner content (that isn't TextContent).
    foreach (var item in chatMessageContent.Items)
    {
        switch (item)
        {
            case AnnotationContent annotation:
                Console.WriteLine($"  [{item.GetType().Name}] {annotation.Quote}: File #{annotation.FileId}");
                break;
            case FileReferenceContent fileReference:
                Console.WriteLine($"  [{item.GetType().Name}] File #{fileReference.FileId}");
                break;
            case ImageContent image:
                Console.WriteLine($"  [{item.GetType().Name}] {image.Uri?.ToString() ?? image.DataUri ?? $"{image.Data?.Length} bytes"}");
                break;
            case FunctionCallContent functionCall:
                Console.WriteLine($"  [{item.GetType().Name}] {functionCall.Id}");
                break;
            case FunctionResultContent functionResult:
                Console.WriteLine($"  [{item.GetType().Name}] {functionResult.CallId} - {functionResult.Result?.ToString() ?? "*"}");
                break;
        }
    }

}