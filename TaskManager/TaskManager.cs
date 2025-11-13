public class TaskManager
{
    private List<Task> _rootTasks = new();
    private int _nextId = 1; // auto increment

    public List<Task> RootTasks { get { return _rootTasks; } }

    public void AddTask(string title, string description, Task? parent, DateTime? dueDate, int? interval)
    {
        Task task;
        RecurringTask recurringTask;
        if (interval.HasValue)
        {
            recurringTask = new RecurringTask(_nextId++, title, description, interval.Value, parent, dueDate);
            AddTask(recurringTask, parent); // polymorphism
        }
        else
        {
            task = new Task(_nextId++, title, description, parent, dueDate);
            AddTask(task, parent);
        }
    }

    private void AddTask(Task task, Task? parent)
    {
        if (parent == null)
        {
            _rootTasks.Add(task);
        }
        else
        {
            parent.SubTasks.Add(task);
        }
    }

    public void DeleteTask(Task task)
    {
        if (task.Parent == null)
        {
            _rootTasks.Remove(task);
        }
        else
        {
            task.Parent.SubTasks.Remove(task); 
        }
    }
}