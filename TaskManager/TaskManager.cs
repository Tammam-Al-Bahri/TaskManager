public class TaskManager
{
    private readonly List<Task> _rootTasks = new();
    private readonly Dictionary<int, Task> _allTasks = new(); // cache all tasks
    private int _nextId = 1; // for auto increment

    // for parallel thread safety
    private readonly object _dictLock = new();
    private readonly object _listLock = new();


    public List<Task> RootTasks => _rootTasks;

    public Task AddTask(string title, string description, Task? parent, DateTime? dueDate, int? interval)
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
        return task;
    }

    public Task? GetTaskFromId(int id)
    {
        return _allTasks.TryGetValue(id, out var t) ? t : null;
    }

    public List<Task> GetSubTasks(Task task)
    {
        List<Task> result = new();

        Parallel.ForEach(task.SubTaskIds, id =>
        {
            Task? t;
            lock (_dictLock)
                t = _allTasks[id];

            if (t != null)
            {
                lock (_listLock)
                    result.Add(t);
            }
        });

        return result;
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
            Task? parent = GetTaskFromId(task.ParentId.Value);
            parent?.SubTaskIds.Remove(task.Id);
        }

        DeleteSubTasks(task);

        // remove this task
        _allTasks.Remove(task.Id);
    }

    // removes all subtasks recursively
    private void DeleteSubTasks(Task task)
    {
        Parallel.ForEach(task.SubTaskIds.ToList(), childId =>
        {
            Task? child;
            lock (_dictLock)
                child = _allTasks.TryGetValue(childId, out var x) ? x : null;

            if (child != null)
                DeleteSubTasks(child);

            lock (_dictLock)
                _allTasks.Remove(childId);
        });

        task.SubTaskIds.Clear();
    }

    public void UpdateIsCompleteUpwards(Task task)
    {
        Task? current = task;

        while (current.ParentId != null)
        {
            Task? parent = GetTaskFromId(current.ParentId.Value);
            if (parent == null) break;

            // parent must be complete only if all children are complete
            bool allCompleted = true;
            Parallel.ForEach(parent.SubTaskIds, (childId, state) =>
            {
                Task child;
                lock (_dictLock)
                    child = _allTasks[childId];

                if (!child.IsCompleted)
                {
                    allCompleted = false;
                    state.Stop();
                }
            });

            parent.IsCompleted = allCompleted;

            // move up
            current = parent;
        }
    }



    public void Save(string path)
    {
        if (Path.GetExtension(path).ToLower() != ".dat")
            path += ".dat";

        using FileStream fs = File.Open(path, FileMode.Create);
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

            // created at
            bw.Write(t.CreatedAt.Ticks); // integer 

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

    public static TaskManager? LoadBinary(string path)
    {

        if (Path.GetExtension(path).ToLower() != ".dat")
            path += ".dat";

        if (!File.Exists(path)) return null;

        TaskManager manager = new();

        manager._rootTasks.Clear();
        manager._allTasks.Clear();

        using FileStream fs = File.Open(path, FileMode.Open);
        using BinaryReader br = new BinaryReader(fs);

        // next id
        manager._nextId = br.ReadInt32();

        // how many tasks
        int count = br.ReadInt32();

        // temporary list for parent tasks
        List<Task> loaded = new(count); // capacity

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

            DateTime created = new DateTime(br.ReadInt64());

            int subCount = br.ReadInt32();
            List<int> subIds = new(subCount);
            for (int j = 0; j < subCount; j++)
                subIds.Add(br.ReadInt32());

            Task t;

            if (isRecurring)
            {
                int interval = br.ReadInt32();
                t = new RecurringTask(id, title, description, interval, isCompleted, parentId, due, created);
            }
            else
            {
                t = new Task(id, title, description, isCompleted, parentId, due, created);
            }

            t.SubTaskIds = subIds;
            loaded.Add(t);
        }

        // rebuild dictionary and root tasks in parallel
        // locks to make this thread safe
        object dictLock = new();
        object listLock = new();

        Parallel.ForEach(loaded, t =>
        {
            lock (dictLock)
                manager._allTasks[t.Id] = t;

            if (t.ParentId == null)
            {
                lock (listLock)
                    manager._rootTasks.Add(t);
            }
        });

        return manager;
    }
}
