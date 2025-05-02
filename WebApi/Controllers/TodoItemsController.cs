using Microsoft.AspNetCore.Mvc;

namespace WebApi.Controllers;

[ApiController]
[Route("[controller]")]
public class TodoItemsController(ILogger<TodoItemsController> logger) : ControllerBase
{
    private static readonly List<TodoDto> Todos = [];
    private static int _nextId = 1;

    [HttpGet]
    public ActionResult<IEnumerable<TodoDto>> GetAll()
    {
        logger.LogInformation("GetAll todo list was called");
        return Ok(Todos);
    }

    [HttpGet("{id:int}")]
    public ActionResult<TodoDto> GetById(int id)
    {
        logger.LogInformation("GetById {Id} was called", id);
        var todo = Todos.FirstOrDefault(t => t.Id == id);
        return todo is not null ? Ok(todo) : NotFound();
    }

    [HttpPost]
    public ActionResult Create([FromBody] TodoDto newTodo)
    {
        logger.LogInformation("Create todo {newTodo} was called", newTodo.Title);
        if (string.IsNullOrWhiteSpace(newTodo.Title))
            return BadRequest("Title is required.");

        newTodo.Id = _nextId++;
        newTodo.Status = "Open";
        Todos.Add(newTodo);
        return CreatedAtAction(nameof(GetById), new { id = newTodo.Id }, newTodo);
    }

    [HttpPut("{id:int}")]
    public ActionResult Update(int id, [FromBody] TodoDto updated)
    {
        logger.LogInformation("Update todo {NewDescription} was called", updated.Description);
        var existing = Todos.FirstOrDefault(t => t.Id == id);
        if (existing is null) return NotFound();

        if (!string.IsNullOrWhiteSpace(updated.Title))
            existing.Title = updated.Title;

        if (!string.IsNullOrWhiteSpace(updated.Description))
            existing.Description = updated.Description;

        if (!string.IsNullOrWhiteSpace(updated.Status))
            existing.Description = updated.Status;

        return NoContent();
    }

    [HttpDelete]
    public ActionResult Delete()
    {
        logger.LogInformation("Delete all todo items api was called");
        Todos.Clear();
        return NoContent();
    }

    [HttpDelete("{id:int}")]
    public ActionResult Delete(int id)
    {
        logger.LogInformation("Delete todo {Id} was called", id);
        var todo = Todos.FirstOrDefault(t => t.Id == id);
        if (todo is null) return NotFound();

        Todos.Remove(todo);
        return NoContent();
    }
}

public class TodoDto
{
    public int Id { get; set; }
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string Status { get; set; } = "";
}
