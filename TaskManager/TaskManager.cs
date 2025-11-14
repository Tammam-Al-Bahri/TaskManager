public class TaskManager
{
    private readonly List<Task> _rootTasks = new();
    private readonly Dictionary<int, Task> _allTasks = new();
    private int _nextId = 1; // for auto increment

    public List<Task> RootTasks => _rootTasks;

    public void AddTask(string title, string description, Task? parent, DateTime? dueDate, int? interval)
    {
        Task task;

        if (interval.HasValue) // polymorphism
        {
            task = new RecurringTask(_nextId++, title, description, interval.Value, false, parent?.Id, dueDate);
        }
        else // isCompleted false as task has just been created
        {
            task = new Task(_nextId++, title, description, false, parent?.Id, dueDate);
        }

        _allTasks[task.Id] = task;

        if (parent == null)
        {
            _rootTasks.Add(task);
        }
        else
        {
            parent.SubTaskIds.Add(task.Id);
        }
    }

    public Task? GetTaskFromId(int id)
    {
        return _allTasks.TryGetValue(id, out var t) ? t : null;
    }

    public List<Task> GetSubTasks(Task task)
    {
        return task.SubTaskIds.Select(id => GetTaskFromId(id)).Where(t => t != null).ToList()!;
    }

    public void DeleteTask(Task task)
    {
        // remove from parent
        if (task.ParentId == null)
        {
            _rootTasks.Remove(task);
        }
        else
        {
            var parent = GetTaskFromId(task.ParentId.Value);
            parent?.SubTaskIds.Remove(task.Id);
        }

        // removes all subtasks recursively
        DeleteSubTasks(task);

        // remove this task
        _allTasks.Remove(task.Id);
    }

    private void DeleteSubTasks(Task task)
    {
        foreach (int childId in task.SubTaskIds.ToList())
        {
            var child = GetTaskFromId(childId);
            if (child != null)
            {
                DeleteSubTasks(child);
                _allTasks.Remove(child.Id);
            }
        }

        task.SubTaskIds.Clear();
    }

    public void Save(string fileName, string? filePath = null)
    {
        if (Path.GetExtension(fileName).ToLower() != ".dat")
            fileName += ".dat";

        string fullPath = filePath == null ? fileName : Path.Combine(filePath, fileName);

        using FileStream fs = File.Open(fullPath, FileMode.Create);
        using BinaryWriter bw = new BinaryWriter(fs);

        // write nextId
        bw.Write(_nextId);

        // write number of tasks
        bw.Write(_allTasks.Count);

        foreach (var kvp in _allTasks) // key value pair from dictionary
        {
            Task t = kvp.Value;

            // for the inherited class
            bw.Write(t is RecurringTask);

            // id
            bw.Write(t.Id);

            // title and description
            bw.Write(t.Title);
            bw.Write(t.Description);

            // completed bool
            bw.Write(t.IsCompleted);

            // parent
            bw.Write(t.ParentId.HasValue);
            if (t.ParentId.HasValue)
                bw.Write(t.ParentId.Value);

            // due date
            bw.Write(t.DueDate.HasValue);
            if (t.DueDate.HasValue)
                bw.Write(t.DueDate.Value.Ticks); // integer 

            // subtasks
            bw.Write(t.SubTaskIds.Count);
            foreach (int subId in t.SubTaskIds)
                bw.Write(subId);

            // recurring task extra field
            if (t is RecurringTask r)
            {
                bw.Write(r.IntervalDays);
            }
        }
    }

    public static TaskManager? LoadBinary(string fileName, string? filePath = null)
    {

        if (Path.GetExtension(fileName).ToLower() != ".dat")
            fileName += ".dat";

        string fullPath = filePath == null ? fileName : Path.Combine(filePath, fileName);

        if (!File.Exists(fullPath)) return null;

        TaskManager manager = new();

        manager._rootTasks.Clear();
        manager._allTasks.Clear();

        using FileStream fs = File.Open(fullPath, FileMode.Open);
        using BinaryReader br = new BinaryReader(fs);

        // nextId
        manager._nextId = br.ReadInt32();

        // how many tasks
        int count = br.ReadInt32();

        // temporary list for parent tasks
        List<Task> loaded = new();

        for (int i = 0; i < count; i++)
        {
            bool isRecurring = br.ReadBoolean();

            int id = br.ReadInt32();
            string title = br.ReadString();
            string description = br.ReadString();

            bool isCompleted = br.ReadBoolean();

            int? parentId = null;
            if (br.ReadBoolean())
                parentId = br.ReadInt32();

            DateTime? due = null;
            if (br.ReadBoolean())
                due = new DateTime(br.ReadInt64());

            int subCount = br.ReadInt32();
            List<int> subIds = new(subCount);
            for (int j = 0; j < subCount; j++)
                subIds.Add(br.ReadInt32());

            Task t;

            if (isRecurring)
            {
                int interval = br.ReadInt32();
                t = new RecurringTask(id, title, description, interval, isCompleted, parentId, due);
            }
            else
            {
                t = new Task(id, title, description, isCompleted, parentId, due);
            }

            t.SubTaskIds = subIds;
            loaded.Add(t);
        }

        // rebuild dictionary and root tasks
        foreach (Task t in loaded)
        {
            manager._allTasks[t.Id] = t;

            if (t.ParentId == null)
                manager._rootTasks.Add(t);
        }

        return manager;
    }
}
