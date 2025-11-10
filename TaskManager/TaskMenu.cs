public class TaskMenu : Menu
{
    private List<Task> _tasks;

    public TaskMenu(List<Task> tasks, string prompt, string title)
        : base(tasks.Select(t => t.Title).ToArray(),
               prompt, title)
    {
        _tasks = tasks;
    }

    public int RunAndGetTaskId()
    {
        int selectedIndex = base.Run();
        return _tasks[selectedIndex].Id;
    }
}
