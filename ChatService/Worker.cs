using ChatService.Plugins;
using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;

namespace ChatService;

public class Worker(Kernel kernel) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        ChatCompletionAgent agent = new() // 👈🏼 Definition of the agent
        {
            Instructions = "You are a assistant that helps manage Todo list using only todo_list_plugin",
            Name = "todo_list_assistant",
            Kernel = kernel,
            Arguments = new KernelArguments(new PromptExecutionSettings
            {
                FunctionChoiceBehavior = FunctionChoiceBehavior.Auto()
            })
        };

        agent.Kernel.ImportPluginFromType<WebApiPlugin>("todo_list_plugin");

        AgentThread thread = new ChatHistoryAgentThread();

        while (stoppingToken.IsCancellationRequested is false)
        {
            Console.Write("User: ");
            var message = Console.ReadLine();
            if (string.IsNullOrEmpty(message)) break; // Exit the loop if the message is empty

            Console.Write("\nAssistant: ");
            await foreach (var response in agent.InvokeStreamingAsync(message, thread, cancellationToken: stoppingToken))
            {
                Console.Write(response.Message.Content);
            }
            Console.WriteLine("\n");
        }
    }
}