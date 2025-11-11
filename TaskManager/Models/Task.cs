public class Task
{
    public int Id { get; }

    public string Title;

    public DateTime? DueDate;
    public DateTime CreatedAt { get; }

    public bool IsCompleted;

    public Task? Parent;

    public List<Task> SubTasks = new();

    public Task(int id, string title, Task? parent = null, DateTime? dueDate = null)
    {
        Id = id;

        Title = title;

        DueDate = dueDate;
        CreatedAt = DateTime.Now;

        IsCompleted = false;

        Parent = parent;
    }
}
