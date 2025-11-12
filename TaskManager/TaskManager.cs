public class TaskManager
{
    private List<Task> _rootTasks = new();
    private Dictionary<int, Task> _taskLookup = new();
    private int _nextId = 1; // auto increment

    public List<Task> RootTasks { get { return _rootTasks; } }

    public Task AddTask(string title, Task? parent = null, DateTime? dueDate = null, DateTime? createdAt = null)
    {
        Task task = new Task(_nextId++, title, parent, dueDate, createdAt); // auto increment
        if (parent == null)
        {
            _rootTasks.Add(task);
        }
        else
        {
            parent.SubTasks.Add(task);
        }

        _taskLookup[task.Id] = task;
        return task;
    }

    public Task? GetTaskById(int id)
    {
        _taskLookup.TryGetValue(id, out Task? task);
        return task;
    }

    public void DeleteTask(Task task)
    {
        RemoveFromLookup(task);

        if (task.Parent == null)
            _rootTasks.Remove(task);
        else
            task.Parent.SubTasks.Remove(task); 
    }

    private void RemoveFromLookup(Task task)
    {
        _taskLookup.Remove(task.Id);
        foreach (Task sub in task.SubTasks)
            RemoveFromLookup(sub);
    }
}