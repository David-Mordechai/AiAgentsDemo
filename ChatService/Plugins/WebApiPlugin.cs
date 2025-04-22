using Microsoft.SemanticKernel;
using System.ComponentModel;
using System.Net.Http.Json;
using System.Text.Json;

namespace ChatService.Plugins;

internal class WebApiPlugin(IHttpClientFactory httpClientFactory)
{
    private async Task<int?> GetTodoIdByTitleAsync(string name)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient("WebApi");
            var items = await httpClient.GetFromJsonAsync<List<TodoDto>>("todoItems");

            var item = items?.FirstOrDefault(i => i.Title.Equals(name, StringComparison.OrdinalIgnoreCase));
            return item?.Id;
        }
        catch
        {
            return null;
        }
    }

    [KernelFunction("get_all_todos")]
    [Description("Gets the full list of todo items.")]
    [return: Description("Returns all todo items as a JSON array.")]
    public async Task<string> GetAllTodosAsync()
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient("WebApi");
            var todos = await httpClient.GetFromJsonAsync<List<TodoDto>>("todoItems");

            return JsonSerializer.Serialize(todos);
        }
        catch
        {
            return "[]";
        }
    }

    [KernelFunction("create_todo")]
    [Description("Creates a new todo item with the given title and description.")]
    [return: Description("Returns true if the todo was created successfully.")]
    public async Task<bool> CreateTodoAsync(
    [Description("The title of the todo item")] string title,
    [Description("A description for the todo item")] string description)
    {
        try
        {
            using var httpClient = httpClientFactory.CreateClient("WebApi");
            var newTodo = new TodoDto { Title = title, Description = description };
            var response = await httpClient.PostAsJsonAsync("TodoItems/", newTodo);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    [KernelFunction("get_todo_details")]
    [Description("Gets details about a todo item using its title.")]
    [return: Description("Returns the todo item as a JSON string. each todo item should containe title, description and status")]
    public async Task<string> GetTodoDetailsAsync(
        [Description("The title of the todo item")] string title)
    {
        var id = await GetTodoIdByTitleAsync(title);
        if (id == null)
        {
            return $"{{\"error\": \"Todo item '{title}' not found\"}}";
        }

        using var httpClient = httpClientFactory.CreateClient("WebApi");
        var todo = await httpClient.GetFromJsonAsync<TodoDto>($"TodoItems/{id}");
        return todo?.Title ?? string.Empty;
    }

    [KernelFunction("update_todo_description")]
    [Description("Updates the description of an existing todo item by title. Only if asked implicetly to do so.")]
    [return: Description("Returns true if the update was successful.")]
    public async Task<bool> UpdateTodoDescriptionAsync(
        [Description("The title of the todo item")] string title,
        [Description("The new description for the todo item")] string newDescription)
    {
        var id = await GetTodoIdByTitleAsync(title);
        if (id == null) return false;

        using var httpClient = httpClientFactory.CreateClient("WebApi");
        var updated = new { Description = newDescription };
        var response = await httpClient.PutAsJsonAsync($"TodoItems/{id}", updated);
        return response.IsSuccessStatusCode;
    }

    [KernelFunction("update_todo_status")]
    [Description("Updates the status of an existing todo item by title. Only if asked implicetly to do so.")]
    [return: Description("Returns true if the update was successful.")]
    public async Task<bool> UpdateTodoStatusAsync(
        [Description("The title of the todo item")] string title,
        [Description("The new status for the todo item")] string newStatus)
    {
        var id = await GetTodoIdByTitleAsync(title);
        if (id == null) return false;

        using var httpClient = httpClientFactory.CreateClient("WebApi");
        var updated = new { status = newStatus };
        var response = await httpClient.PutAsJsonAsync($"TodoItems/{id}", updated);
        return response.IsSuccessStatusCode;
    }

    [KernelFunction("delete_todo")]
    [Description("Deletes a todo item by title.")]
    [return: Description("Returns true if the todo was deleted successfully.")]
    public async Task<bool> DeleteTodoAsync(
        [Description("The title of the todo item to delete")] string title)
    {
        var id = await GetTodoIdByTitleAsync(title);
        if (id == null) return false;

        using var httpClient = httpClientFactory.CreateClient("WebApi");
        var response = await httpClient.DeleteAsync($"TodoItems/{id}");
        return response.IsSuccessStatusCode;
    }

    // Internal DTO for deserialization
    private class TodoDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = "";
        public string Description { get; set; } = "";
        public string Status { get; set; } = "";
    }
}