using static System.Console;

public class TaskApp
{
    string logo = @"
------------------------------------------------------------------------------------------------------------------------
|                                                                                                                      |
|  /$$$$$$$$                  /$$             /$$      /$$                                                             |
| |__  $$__/                 | $$            | $$$    /$$$                                                             |
|    | $$  /$$$$$$   /$$$$$$$| $$   /$$      | $$$$  /$$$$  /$$$$$$  /$$$$$$$   /$$$$$$   /$$$$$$   /$$$$$$   /$$$$$$  |
|    | $$ |____  $$ /$$_____/| $$  /$$/      | $$ $$/$$ $$ |____  $$| $$__  $$ |____  $$ /$$__  $$ /$$__  $$ /$$__  $$ |
|    | $$  /$$$$$$$|  $$$$$$ | $$$$$$/       | $$  $$$| $$  /$$$$$$$| $$  \ $$  /$$$$$$$| $$  \ $$| $$$$$$$$| $$  \__/ |
|    | $$ /$$__  $$ \____  $$| $$_  $$       | $$\  $ | $$ /$$__  $$| $$  | $$ /$$__  $$| $$  | $$| $$_____/| $$       |
|    | $$|  $$$$$$$ /$$$$$$$/| $$ \  $$      | $$ \/  | $$|  $$$$$$$| $$  | $$|  $$$$$$$|  $$$$$$$|  $$$$$$$| $$       |
|    |__/ \_______/|_______/ |__/  \__/      |__/     |__/ \_______/|__/  |__/ \_______/ \____  $$ \_______/|__/       |
|                                                                                        /$$  \ $$                     |
|                                                                                       |  $$$$$$/                     |
|                                                                                        \______/                      |
|                                                                                                                      |
|          (Use the arrow or w and s keys to cycle though the options, and press enter to select an option.)           |
------------------------------------------------------------------------------------------------------------------------";

    private string _separator = "-----------------------------------------------------------------------------";
    private string _dateFormat = "yyyy-MM-dd";

    private string _path = "";
    private TaskManager _manager = new();
    private bool _unsavedChanges = false;

    public void Run(string[] args)
    {
        HandleArgs(args);

        MenuSettings settings = MenuSettings.Load();
        BackgroundColor = settings.Background;
        ForegroundColor = settings.Foreground;

        // main loop
        while (true)
        {
            MainMenu();
        }

    }

    private void HandleArgs(string[] args)
    {
        if (args.Length == 0)
        {
            WriteLine("ERROR: Use '-o [file name] [file path]' to open or '-n [file name] [file path]' to create new.");
            Exit();
        }

        string command = args[0];

        if (args.Length < 2)
        {
            WriteLine("ERROR: You must specify the file path.");
            Exit();
        }

        string filePath = args[1];
        if (Path.GetExtension(filePath).ToLower() != ".dat")
            filePath += ".dat";

        switch (command)
        {
            case "-o":
                if (!File.Exists(filePath))
                {
                    WriteLine("ERROR: File not found.");
                    Exit();
                }

                TaskManager? loaded = TaskManager.LoadBinary(filePath);
                if (loaded == null)
                {
                    WriteLine("ERROR: Failed to load project.");
                    Exit();
                }
                _path = filePath;

                _manager = loaded;
                break;
            case "-n":
                if (File.Exists(filePath))
                {
                    WriteLine("This file already exists. Would you like to overwrite it?");
                    Write("y / n: ");
                    string input = ReadLine().Trim().ToLower();
                    if (input != "y")
                    {
                        WriteLine("Cancelled.");
                        Exit();
                    }

                }
                _path = filePath;
                _manager.Save(filePath);
                break;
            default:
                WriteLine("ERROR: Unknown command. Use '-o [file name] [file path]' or '-n [file name] [file path]'.");
                Exit();
                break;
        }
    }


    private void MainMenu()
    {
        Title = "Task Manager";
        string prompt = logo;
        (string, string)[] options = [("Tasks", "view and manage tasks"), ("Display", "select display colours"), ("Exit", "")];

        Menu menu = new Menu(options, prompt);
        int selection = menu.Run();

        switch (selection)
        {
            case 0:
                ManageTasks();
                break;
            case 1:
                DisplaySettings();
                break;
            case 2:
                Exit();
                break;
        }
    }

    private void ManageTasks(Task? selectedTask = null)
    {
        int index = 0; // for highlighting correct task in menu (going back highlights parent, creating new tasks highlights it, ...)

        TaskSort sort = TaskSort.None;
        TaskFilter filter = TaskFilter.None;
        while (true)
        {
            Title = "Task Manager";
            // as parallel to use multiple threads
            IEnumerable<Task> baseTasks = (selectedTask == null ? _manager.RootTasks : _manager.GetSubTasks(selectedTask)).AsParallel();

            // filter
            switch (filter)
            {
                case TaskFilter.Incomplete:
                    baseTasks = baseTasks.Where(t => !t.IsCompleted).AsParallel();
                    break;
                case TaskFilter.Complete:
                    baseTasks = baseTasks.Where(t => t.IsCompleted).AsParallel();
                    break;
                case TaskFilter.Overdue:
                    baseTasks = baseTasks.Where(t => t.DueDate != null && t.DueDate < DateTime.Now).AsParallel();
                    break;
                case TaskFilter.Recurring:
                    baseTasks = baseTasks.Where(t => t is RecurringTask).AsParallel();
                    break;
            }

            // sort
            switch (sort)
            {
                case TaskSort.ByTitle:
                    baseTasks = baseTasks.OrderBy(t => t.Title).AsParallel();
                    break;
                case TaskSort.ByDueDate:
                    baseTasks = baseTasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue).AsParallel(); // no due date at end
                    break;
            }

            List<Task> tasks = baseTasks.ToList();

            string selectedTaskPath = TaskPath(selectedTask);
            string prompt = TaskMenu(selectedTask, filter, sort);


            Dictionary<ConsoleKey, int> options = new Dictionary<ConsoleKey, int> // negative values to distinguish them from list index
            {
                { ConsoleKey.D1, -1 }, // new
                { ConsoleKey.D2, -2 }, // edit
                { ConsoleKey.D3, -3 }, // mark as complete
                { ConsoleKey.Backspace, -4 }, // go back
                { ConsoleKey.Delete, -5 }, // delete
                { ConsoleKey.D0, -6 }, // save changes
                { ConsoleKey.Escape, -7 }, // main menu
                { ConsoleKey.F10, -10 }, // clear sorting and filters
                { ConsoleKey.F1, -11 }, // sort by title
                { ConsoleKey.F2, -12 }, // sort by due date
                { ConsoleKey.F3, -13 }, // filter complete / incomplete
                { ConsoleKey.F4, -14 }, // filter overdue
                { ConsoleKey.F5, -15 }, // filter recurring
            };

            (string title, string info)[] tasksInfo = tasks.Select(t => (t.Title, TaskInfo(t))).ToArray(); // tuple
            Menu menu = new(tasksInfo, prompt, index);
            int selection = menu.Run(options);


            if (selection >= 0 && tasks.Count > 0)
            {
                index = 0;
                selectedTask = tasks[selection];
                continue;
            }
            else
            {
                switch (selection)
                {
                    case -1:
                        bool created = CreateNewTask(selectedTask);
                        if (created)
                        {
                            index = tasks.Count; // highlight new task if it's created
                            _unsavedChanges = true;
                        }
                        break;
                    case -2:
                        if (tasks.Count == 0) break;
                        index = menu.SelectedIndex;
                        EditTask(tasks[menu.SelectedIndex]); // index from menu
                        _unsavedChanges = true;
                        break;
                    case -3:
                        if (tasks.Count == 0) break;
                        index = menu.SelectedIndex;
                        ToggleTaskIsCompleted(tasks[menu.SelectedIndex]);
                        _unsavedChanges = true;
                        break;
                    case -4:
                        if (selectedTask == null) // no task selected at rool level, return to main menu
                        {
                            return;
                        }
                        if (selectedTask.ParentId == null) // go to root level tasks
                        {
                            index = _manager.RootTasks.FindIndex(t => t == selectedTask);
                            selectedTask = null;
                        }
                        else // go back to parent task
                        {
                            index = _manager.GetTaskFromId(selectedTask.ParentId.Value).SubTaskIds.FindIndex(t => t == selectedTask.Id);
                            selectedTask = _manager.GetTaskFromId(selectedTask.ParentId.Value);
                        }
                        break;
                    case -5:
                        if (tasks.Count == 0) break;
                        index = menu.SelectedIndex;
                        DeleteTask(tasks[menu.SelectedIndex]);
                        _unsavedChanges = true;
                        break;
                    case -6:
                        _manager.Save(_path);
                        index = menu.SelectedIndex;
                        _unsavedChanges = false;
                        break;
                    case -7: // return (to MainMenu())
                        return;
                    case -10:
                        sort = TaskSort.None;
                        filter = TaskFilter.None;
                        index = 0;
                        break;
                    case -11:
                        if (sort == TaskSort.ByTitle) // toggle
                        {
                            sort = TaskSort.None;
                            break;
                        }
                        sort = TaskSort.ByTitle;
                        index = 0;
                        break;
                    case -12:
                        if (sort == TaskSort.ByDueDate)
                        {
                            sort = TaskSort.None;
                            break;
                        }
                        sort = TaskSort.ByDueDate;
                        index = 0;
                        break;
                    case -13:
                        // cycle between none complete and incomplete filter
                        if (filter != TaskFilter.Complete && filter != TaskFilter.Incomplete)
                        {
                            filter = TaskFilter.Complete;
                            index = 0;
                            break;
                        }
                        if (filter == TaskFilter.Complete)
                        {
                            filter = TaskFilter.Incomplete;
                            index = 0;
                            break;
                        }
                        if (filter == TaskFilter.Incomplete)
                        {
                            filter = TaskFilter.None;
                            index = 0;
                            break;
                        }
                        filter = TaskFilter.Complete;
                        index = 0;
                        break;
                    case -14:
                        if (filter == TaskFilter.Overdue)
                        {
                            filter = TaskFilter.None;
                            index = 0;
                            break;
                        }
                        filter = TaskFilter.Overdue;
                        index = 0;
                        break;
                    case -15:
                        if (filter == TaskFilter.Recurring)
                        {
                            filter = TaskFilter.None;
                            index = 0;
                            break;
                        }
                        filter = TaskFilter.Recurring;
                        index = 0;
                        break;
                }
            }
        }
    }

    private void DisplaySettings()
    {
        Title = "Display Settings";
        (string, string)[] options = [("Background", "change background colour"),
            ("Foreground", "change foreground colour"), ("Selection Background", "change selection background colour"),
            ("Selection Foreground", "change selection foreground colour"), ("Main Menu", "return to main menu")];
        string prompt = "Select Theme";


        while (true)
        {
            Menu menu = new(options, prompt);
            MenuSettings settings = MenuSettings.Load();
            int selection = menu.Run();
            switch (selection)
            {
                case 0:
                    settings.Background = SelectColour("Select Background Colour");
                    settings.Save();
                    break;
                case 1:
                    settings.Foreground = SelectColour("Select Foreground Colour");
                    settings.Save();
                    break;
                case 2:
                    settings.SelectionBackground = SelectColour("Select Selection Background Colour");
                    settings.Save();
                    break;
                case 3:
                    settings.SelectionForeground = SelectColour("Select Selection Foreground Colour");
                    settings.Save();
                    break;
                case 4:
                    BackgroundColor = settings.Background;
                    ForegroundColor = settings.Foreground;
                    return;
            }
            BackgroundColor = settings.Background;
            ForegroundColor = settings.Foreground;
        }
    }

    private ConsoleColor SelectColour(string prompt)
    {
        (string, string)[] options = [("Black", ""), ("White", ""), ("Gray", ""), ("Dark Gray", ""), ("Red", ""),
            ("Dark Red", ""), ("Yellow", ""), ("Dark Yellow", ""), ("Green", ""), ("Dark Green", ""), ("Cyan", ""),
            ("Dark Cyan", ""), ("Blue", ""), ("Dark Blue", ""), ("Magenta", ""), ("Dark Magenta", "") ];
        Menu menu = new(options, prompt);
        MenuSettings settings = new();

        int selection = menu.Run();

        switch (selection)
        {
            case 0:
                return ConsoleColor.Black;
            case 1:
                return ConsoleColor.White;
            case 2:
                return ConsoleColor.Gray;
            case 3:
                return ConsoleColor.DarkGray;
            case 4:
                return ConsoleColor.Red;
            case 5:
                return ConsoleColor.DarkRed;
            case 6:
                return ConsoleColor.Yellow;
            case 7:
                return ConsoleColor.DarkYellow;
            case 8:
                return ConsoleColor.Green;
            case 9:
                return ConsoleColor.DarkGreen;
            case 10:
                return ConsoleColor.Cyan;
            case 11:
                return ConsoleColor.DarkCyan;
            case 12:
                return ConsoleColor.Blue;
            case 13:
                return ConsoleColor.DarkBlue;
            case 14:
                return ConsoleColor.Magenta;
            case 15:
                return ConsoleColor.DarkMagenta;
            default:
                return ConsoleColor.Black;
        }
    }

    private void Exit()
    {
        if (_unsavedChanges)
        {
            (string, string)[] options = [("save and exit", ""), ("exit without saving", ""), ("cancel", "")];
            string prompt = "There are unsaved changes.";
            Menu menu = new(options, prompt);

            int selection = menu.Run();
            switch (selection)
            {
                case 0:
                    _manager.Save(_path);
                    break;
                case 2:
                    return;
            }
        }
        Environment.Exit(0);
    }


    private bool CreateNewTask(Task? parent)
    {
        Title = "New Task";
        Clear();
        WriteLine("------------");
        WriteLine("| New Task |");
        WriteLine("------------");

        WriteLine("Task Title: ");
        string title = ReadLine().Trim();
        if (string.IsNullOrEmpty(title)) return false;
        WriteLine("");

        WriteLine("Task Description: ");
        string description = ReadLine().Trim();
        WriteLine("");

        DateTime? dueDate = DateInput();

        int? interval = RecurringTaskInput("");

        Task task = _manager.AddTask(title, description, parent, dueDate, interval);
        _manager.UpdateIsCompleteUpwards(task);
        return true;
    }

    private void EditTask(Task task)
    {
        Title = "Edit task";
        Clear();
        WriteLine(_separator);
        WriteLine("New title: ");
        string title = ConsoleInput.ReadLineWithEdit(task.Title).Trim();
        if (string.IsNullOrEmpty(title)) return;
        WriteLine("\n"); // instead of two write lines

        WriteLine("New description: ");
        string description = ConsoleInput.ReadLineWithEdit(task.Description).Trim();
        WriteLine("\n");

        DateTime? dueDate = DateInput(task.DueDate?.ToString("yyyy-MM-dd"));

        if (task is RecurringTask recurringTask) // polymorphism
        {
            string currentInterval = recurringTask.IntervalDays.ToString();
            int? newInterval = RecurringTaskInput(currentInterval);
            if (newInterval.HasValue)
            {
                recurringTask.IntervalDays = newInterval.Value;
            }

        }

        task.Title = title;
        task.Description = description;
        task.DueDate = dueDate;
    }

    private void DeleteTask(Task task)
    {
        _manager.DeleteTask(task);
    }

    private void ToggleTaskIsCompleted(Task task)
    {
        task.IsCompleted = !task.IsCompleted;
        _manager.UpdateIsCompleteUpwards(task);
    }

    // menu options and selected task info
    private string TaskMenu(Task? task, TaskFilter filter, TaskSort sort)
    {
        string menu = $@"
___________                               ___________________
| Options |                               | Sort and Filter |
---------------------------------------   -----------------------------------
|   [ ENTER ]     - open task         |   | {(sort == TaskSort.ByTitle ? ">" : " ")} [ F1 ] - sort by title        |
|   [ 1 ]         - new task          |   | {(sort == TaskSort.ByDueDate ? ">" : " ")} [ F2 ] - sort by due date     |
|   [ 2 ]         - edit task         |   |---------------------------------|
|   [ 3 ]         - mark as completed |   | {(filter == TaskFilter.Complete || filter == TaskFilter.Incomplete ? ">" : " ")} [ F3 ] - filter {(filter != TaskFilter.Complete && filter != TaskFilter.Incomplete ? "complete  " : (filter == TaskFilter.Complete ? "complete  " : "incomplete"))}    |
|   [ BACKSPACE ] - go back           |   | {(filter == TaskFilter.Overdue ? ">" : " ")} [ F4 ] - filter overdue       |
|   [ DELETE ]    - delete task       |   | {(filter == TaskFilter.Recurring ? ">" : " ")} [ F5 ] - filter recurring     |
|   [ 0 ]         - save changes{(_unsavedChanges ? "*" : " ")}     |   |---------------------------------|
|   [ ESC ]       - main menu         |   |   [ F10 ] - clear               |
---------------------------------------   -----------------------------------
{(task != null ?
$"_____________\n| Task Info |\n{_separator}\nSelected: {TaskPath(task)}\n{_separator}" +
$"\nDescription: {task.Description}\n{_separator}" +
$"\nCompleted: {(task.IsCompleted ? "Yes" : "No")}" +
$"\nRepeats: {(task is RecurringTask r ? $"every {r.IntervalDays} {(r.IntervalDays > 1 ? "days" : "day")}" : "No")}" +
$"\nDue at: {(task.DueDate != null ? task.DueDate?.ToString(_dateFormat) + $" | {(task.DueDate.Value - task.CreatedAt).Days} days" : "-")}" +
$"\nCreated at: {task.CreatedAt.ToString(_dateFormat)}" +
$"\n{_separator}" +
"\n_____________\n| Sub Tasks |" : "_________\n| Tasks |")}
{_separator}";

        return menu;
    }


    private string TaskPath(Task? task)
    {
        if (task == null) return "-";

        List<Task> parents = new();

        Task? current = task;

        while (current.ParentId != null)
        {
            current = _manager.GetTaskFromId(current.ParentId.Value); // move up

            if (current == null)
                break;

            parents.Insert(0, current); // insert at start to maintain order
        }

        string taskPath = "";
        foreach (Task p in parents)
        {
            taskPath += $"{p.Title} > ";
        }

        return $"{taskPath}[ {task.Title} ]";
    }

    private string TaskInfo(Task task)
    {
        string i = $"| Sub Tasks: {task.SubTaskIds.Count} | Completed: {(task.IsCompleted ? "Yes" : "No")} | Due: {(task.DueDate.HasValue ? $"{(task.DueDate.Value - DateTime.Now).Days} days" : "-")}";
        return i;
    }

    private DateTime? DateInput(string? edit = null)
    {
        DateTime? dueDate = null;
        if (edit == null) edit = "";

        WriteLine($"(optional) Due Date ({_dateFormat}): ");
        string input = ConsoleInput.ReadLineWithEdit(edit).Trim().Replace(" ", "-");

        WriteLine();

        if (string.IsNullOrEmpty(input))
        {
            Write("No due date set.");
            ReadKey(true);
            WriteLine();
        }
        else
        {
            if (DateTime.TryParse(input, out DateTime parsedDate))
            {
                dueDate = parsedDate;
            }
            else
            {
                Write("Invalid date format. No due date set.");
                ReadKey(true);
                WriteLine();
            }
        }
        return dueDate;
    }

    private int? RecurringTaskInput(string? edit = null)
    {
        int? interval = null;
        if (edit == null) edit = "";

        WriteLine($"(optional) Repeats every ? days: ");
        string input = ConsoleInput.ReadLineWithEdit(edit);

        WriteLine();

        if (string.IsNullOrEmpty(input))
        {
            WriteLine("No interval set.");
            ReadKey(true);
        }
        else
        {
            if (int.TryParse(input, out int parsedInterval))
            {
                if (parsedInterval < 1)
                {
                    Write("Interval must be greater than zero days. No interval set.");
                    ReadKey(true);
                    WriteLine();
                    return null;
                }
                interval = parsedInterval;
            }
            else
            {
                Write("Invalid format. No interval set.");
                ReadKey(true);
                WriteLine();
            }
        }
        return interval;
    }
}