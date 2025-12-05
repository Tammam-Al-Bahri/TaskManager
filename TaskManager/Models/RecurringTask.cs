public class RecurringTask : Task
{
    private int _intervalDays;
    public int IntervalDays
    { 
        get
        {
            return _intervalDays;
        } 
        set
        {
            if (value < 1)
            {
                throw new ArgumentException("Interval must be at least 1 day"); // error if interval < 1 day
            }
            _intervalDays = value;
        }
    }

    public RecurringTask(int id, string title, string description, int intervalDays, bool isCompleted, int? parentId = null, DateTime? dueDate = null, DateTime? createdAt = null)
        : base(id, title, description, isCompleted, parentId, dueDate, createdAt)
    {
        _intervalDays = intervalDays;
    }
}