public class Task
{
    public int Id { get; }
    public string Title;
    public string Description;

    public DateTime CreatedAt { get; }
    public DateTime? DueDate;

    public bool IsCompleted;

    public int? ParentId;
    public List<int> SubTaskIds = new();

    public Task(int id, string title, string description, bool isCompleted, int? parentId = null, DateTime? dueDate = null)
    {
        Id = id;
        Title = title;
        Description = description;

        CreatedAt = DateTime.Now;
        DueDate = dueDate;

        IsCompleted = isCompleted;
        ParentId = parentId;
    }
}
