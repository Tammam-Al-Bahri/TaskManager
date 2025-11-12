public class Task
{
    public int Id { get; }

    public string Title;
    public string Description;

    public DateTime? DueDate;
    public DateTime CreatedAt { get; }

    public bool IsCompleted;

    public Task? Parent;

    public List<Task> SubTasks = new();

    public Task(int id, string title, string description, Task? parent = null, DateTime? dueDate = null, DateTime? createdAt = null)
    {
        Id = id;

        Title = title;
        Description = description;

        DueDate = dueDate;
        CreatedAt = createdAt.HasValue ? createdAt.Value : DateTime.Now;

        IsCompleted = false;

        Parent = parent;
    }
}
