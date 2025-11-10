public class Task
{
    public int Id;

    public string Title;

    public Task? Parent;

    public List<Task> SubTasks = new();

    public Task(int id, string title, Task? parent = null)
    {
        Id = id;
        Title = title;
        Parent = parent;
    }
}
