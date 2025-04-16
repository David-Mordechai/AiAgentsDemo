using Microsoft.SemanticKernel;
using Microsoft.SemanticKernel.Agents;
using Microsoft.SemanticKernel.ChatCompletion;

var builder = Kernel.CreateBuilder();

#pragma warning disable SKEXP0070 // Type is for evaluation purposes only and is subject to change or removal in future updates. Suppress this diagnostic to proceed.
builder.AddOllamaChatCompletion("llama3.2", new Uri("http://localhost:11434"));

var kernel = builder.Build();

ChatCompletionAgent agent = new() // 👈🏼 Definition of the agent
{
    Instructions = "Answer questions about C# and .NET",
    Name = "C# Agent",
    Kernel = kernel
};

var chatHistory = new ChatHistory("You are a friendly assistant.");

// Define a thread variable to maintain the conversation context.
// Since we are creating the thread, we can pass some initial messages to it.
AgentThread? thread = new ChatHistoryAgentThread(chatHistory);

do
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.WriteLine("Enter your message (or press Enter to exit): ");
    Console.ResetColor();
    
    var message = Console.ReadLine();
    if (string.IsNullOrEmpty(message)) break; // Exit the loop if the message is empty
    
    Console.ForegroundColor = ConsoleColor.Cyan;
    await foreach (var response in agent.InvokeStreamingAsync(message, thread))
    {
        Console.Write(response.Message.Content);
    }
    Console.WriteLine(""); // Print a new line for better readability
}
while (true);
