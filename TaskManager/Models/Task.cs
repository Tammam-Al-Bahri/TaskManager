public class Task
{
    public int Id { get; }

    public string Title;
    public string Description;

    public DateTime CreatedAt { get; }
    public DateTime? DueDate;

    protected bool _isCompleted; // protected for RecurringTask inherited class

    public Task? Parent { get; }

    public List<Task> SubTasks = new();
    public bool IsCompleted
    {
        get
        {
            return _isCompleted;
        }
        set
        {
            _isCompleted = value;
            if (Parent != null)
            {
                if (this.IsCompleted) // mark parent as completed if all sub tasks are marked as completed
                {
                    if (Parent.SubTasks.All(t => t.IsCompleted == true))
                    {
                        Parent.IsCompleted = true;
                    }
                } // constructor handles setting task to incomplete when a new sub task is created
                else
                {
                    Parent.IsCompleted = false;
                }
            }
        } // now if all sub tasks of all sub tasks etc are marked as completed, the parent will automatically be marked as completed
        // but you still have the option to mark a task as completed even if it has incompleted sub tasks
        // and you can also mark a task as incompleted if all of its sub tasks are completed
        // a task will automatically be marked as completed if all of its sub tasks are marked as completed
        // and automatically marked as incomplete if not all of its sub tasks are marked as incompleted
    }

    public Task(int id, string title, string description, Task? parent = null, DateTime? dueDate = null)
    {
        Id = id;

        Title = title;
        Description = description;

        CreatedAt = DateTime.Now;
        DueDate = dueDate;

        _isCompleted = false;

        Parent = parent;
        if(Parent != null) Parent.IsCompleted = false; // mark parent as incompleted since not all of sub tasks are complete
    }
}
