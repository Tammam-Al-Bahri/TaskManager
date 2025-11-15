using System;
using System.Text;
using System.Threading.Tasks;
using static System.Console;

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

string separator = "-------------------------------------------------------------------------";
string dateFormat = "yyyy-MM-dd";

string path = "";
TaskManager manager = new();

HandleArgs();

BackgroundColor = Menu.background;
ForegroundColor = Menu.foreground;

// start the program
while (true)
{
    MainMenu();
}

void HandleArgs()
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
            path = filePath;

            manager = loaded;
            break;

        case "-n":
            if(File.Exists(filePath))
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

            path = filePath;
            manager.Save(filePath);
            break;

        default:
            WriteLine("ERROR: Unknown command. Use '-o [file name] [file path]' or '-n [file name] [file path]'.");
            Exit();
            break;
    }
}


void MainMenu()
{
    Title = "Task Manager";
    string prompt = logo;
    (string, string)[] options = [("Tasks", "view and manage tasks"), ("Account", "change password"), ("Help", ""), ("Exit", "")];

    Menu menu = new Menu(options, prompt);
    int selection = menu.Run();


    switch (selection)
    {
        case 0:
            ManageTasks();
            break;
        case 1:
            Account();
            break;
        case 2:
            DisplayHelpInfo();
            break;
        case 3:
            Exit();
            break;
    }
}

void ManageTasks(Task? selectedTask = null)
{
    int index = 0; // for highlighting correct task in menu (going back highlights parent, creating new tasks highlights it, ...)

    TaskSort sort = TaskSort.None;
    TaskFilter filter = TaskFilter.None;
    while (true)
    {
        Title = $"{selectedTask?.Title ?? "Tasks"}"; // ?? for default window title if no selected task

        // as parallel to use multiple threads
        IEnumerable<Task> baseTasks = (selectedTask == null ? manager.RootTasks : manager.GetSubTasks(selectedTask)).AsParallel();

        // filter
        switch (filter)
        {
            case TaskFilter.Incomplete:
                baseTasks = baseTasks.Where(t => !t.IsCompleted);
                break;
            case TaskFilter.Complete:
                baseTasks = baseTasks.Where(t => t.IsCompleted);
                break;
            case TaskFilter.Overdue:
                baseTasks = baseTasks.Where(t => t.DueDate != null && t.DueDate < DateTime.Now);
                break;
            case TaskFilter.Recurring:
                baseTasks = baseTasks.Where(t => t is RecurringTask);
                break;
        }

        // sort
        switch (sort)
        {
            case TaskSort.ByTitle:
                baseTasks = baseTasks.OrderBy(t => t.Title);
                break;
            case TaskSort.ByDueDate:
                baseTasks = baseTasks.OrderBy(t => t.DueDate ?? DateTime.MaxValue); // no due date at end
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
            { ConsoleKey.D4, -4 }, // go back
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
                    if (created) index = tasks.Count; // highlight new task if it's created
                    break;
                case -2:
                    if (tasks.Count == 0) break;
                    index = menu.SelectedIndex;
                    EditTask(tasks[menu.SelectedIndex]); // index from menu
                    break;
                case -3:
                    if (tasks.Count == 0) break;
                    index = menu.SelectedIndex;
                    ToggleTaskIsCompleted(tasks[menu.SelectedIndex]);
                    break;
                case -4:
                    if(selectedTask == null) // no task selected at rool level, return to main menu
                    {
                        return;
                    }
                    if (selectedTask.ParentId == null) // go to root level tasks
                    {
                        index = manager.RootTasks.FindIndex(t => t == selectedTask);
                        selectedTask = null;
                    }
                    else // go back to parent task
                    {
                        index = manager.GetTaskFromId(selectedTask.ParentId.Value).SubTaskIds.FindIndex(t => t == selectedTask.Id);
                        selectedTask = manager.GetTaskFromId(selectedTask.ParentId.Value);
                    }
                    break;
                case -5:
                    if (tasks.Count == 0) break;
                    index = menu.SelectedIndex;
                    DeleteTask(tasks[menu.SelectedIndex]);
                    break;
                case -6:
                    manager.Save(path);
                    index = menu.SelectedIndex;
                    break;
                case -7: // return (to MainMenu())
                    return;
                case -10:
                    sort = TaskSort.None;
                    filter = TaskFilter.None;
                    index = 0;
                    break;
                case -11:
                    if(sort == TaskSort.ByTitle) // toggle
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
                    if(filter == TaskFilter.Overdue)
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

void Account() // TODO (probably rename this method)
{
    // yeah I don't think I'm gonna make user accounts
}

void DisplayHelpInfo() // TODO
{
    Clear();
    WriteLine("Help Info:");
    ReadKey(true);
}

void Exit()
{
    Environment.Exit(0);
}


bool CreateNewTask(Task? parent)
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

    Task task = manager.AddTask(title, description, parent, dueDate, interval);
    manager.UpdateIsCompleteUpwards(task);
    return true;
}

void EditTask(Task task)
{
    Title = "Edit task";
    Clear();
    WriteLine(separator);
    WriteLine("New title: ");
    string title = ReadLineWithEdit(task.Title).Trim();
    if (string.IsNullOrEmpty(title)) return;
    WriteLine("\n"); // instead of two write lines

    WriteLine("New description: ");
    string description = ReadLineWithEdit(task.Description).Trim();
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

void DeleteTask(Task task)
{
    manager.DeleteTask(task);
}

void ToggleTaskIsCompleted(Task task)
{
    task.IsCompleted = !task.IsCompleted;
    manager.UpdateIsCompleteUpwards(task);
}

// menu options and selected task info
string TaskMenu(Task? task, TaskFilter filter, TaskSort sort)
{
    string menu = $@"
___________                            ___________________
| Options |                            | Sort and Filter |
------------------------------------   -----------------------------------
|   [ ENTER ]  - open task         |   | {(sort == TaskSort.ByTitle ? ">" : " ")} [ F1 ] - sort by title        |
|   [ 1 ]      - new task          |   | {(sort == TaskSort.ByDueDate ? ">" : " ")} [ F2 ] - sort by due date     |
|   [ 2 ]      - edit task         |   |---------------------------------|
|   [ 3 ]      - mark as completed |   | {(filter == TaskFilter.Complete || filter == TaskFilter.Incomplete ? ">" : " ")} [ F3 ] - filter {(filter != TaskFilter.Complete && filter != TaskFilter.Incomplete ? "complete  " : (filter == TaskFilter.Complete ? "complete  " : "incomplete"))}    |
|   [ 4 ]      - go back           |   | {(filter == TaskFilter.Overdue ? ">" : " ")} [ F4 ] - filter overdue       |
|   [ DELETE ] - delete task       |   | {(filter == TaskFilter.Recurring ? ">" : " ")} [ F5 ] - filter recurring     |
|   [ 0 ]      - save changes      |   |---------------------------------|
|   [ ESC ]    - main menu         |   |   [ F10 ] - clear               |
------------------------------------   -----------------------------------
{(task != null ?
$"_____________\n| Task Info |\n{separator}\nSelected: {task}\n{separator}" +
$"\nDescription: {task.Description}\n{separator}" +
$"\nCompleted: {(task.IsCompleted ? "Yes" : "No")}" +
$"\nRepeats: {(task is RecurringTask r ? $"every {r.IntervalDays} {(r.IntervalDays > 1 ? "days" : "day")}" : "No")}" +
$"\nDue at: {(task.DueDate != null ? task.DueDate?.ToString(dateFormat) + $" | {(task.DueDate.Value - task.CreatedAt).Days} days" : "-")}" +
$"\nCreated at: {task.CreatedAt.ToString(dateFormat)}" +
$"\n{separator}" +
"\n_____________\n| Sub Tasks |" : "_________\n| Tasks |")}
{separator}";

    return menu;
}


string TaskPath(Task? task)
{
    if (task == null) return "-";

    List<Task> parents = new();

    Task? current = task;

    while (current.ParentId != null)
    {
        current = manager.GetTaskFromId(current.ParentId.Value); // move up

        if (current == null)
            break;

        parents.Insert(0, current); // insert at start to maintain order
    }

    string taskPath = "";
    foreach(Task p in parents)
    {
        taskPath += $"{p.Title} > ";
    }

    return $"{taskPath}[ {task.Title} ]";
}

string TaskInfo(Task task)
{
    string i = $"| Completed: {(task.IsCompleted ? "Yes" : "No")} | Due: {(task.DueDate.HasValue ? $"{(task.DueDate.Value - DateTime.Now).Days} days" : "-")}";
    return i;
}

DateTime? DateInput(string? edit = null)
{
    DateTime? dueDate = null;
    if (edit == null) edit = "";

    WriteLine($"(optional) Due Date ({dateFormat}): ");
    string input = ReadLineWithEdit(edit);

    WriteLine();

    if (string.IsNullOrEmpty(input))
    {
        WriteLine("No due date set.");
        ReadKey(true);
    }
    else
    {
        if (DateTime.TryParse(input, out DateTime parsedDate))
        {
            dueDate = parsedDate;
        }
        else
        {
            WriteLine("Invalid date format. No due date set.");
            ReadKey(true);  
        }
    }
    return dueDate;
}

int? RecurringTaskInput(string? edit = null)
{
    int? interval = null;
    if (edit == null) edit = "";

    WriteLine($"(optional) Repeats every ? days: ");
    string input = ReadLineWithEdit(edit);

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
                WriteLine("Interval must be greater than zero days. No interval set.");
                ReadKey(true);
                return null;
            }
            interval = parsedInterval;
        }
        else
        {
            WriteLine("Invalid format. No interval set.");
            ReadKey(true);
        }
    }
    return interval;
}

string ReadLineWithEdit(string initial) // keeps original text when editing
{
    StringBuilder buffer = new StringBuilder(initial);
    int cursorPos = initial.Length;

    Write(initial);

    ConsoleKey keyPressed = new();
    ConsoleKeyInfo key;
    while (keyPressed != ConsoleKey.Enter)
    {
        ConsoleKeyInfo keyInfo = ReadKey(true);
        keyPressed = keyInfo.Key;

        switch (keyPressed)
        {
            case ConsoleKey.LeftArrow:
                if (cursorPos > 0)
                {
                    cursorPos--;
                    CursorLeft--;
                }
                break;
            case ConsoleKey.RightArrow:
                if (cursorPos < buffer.Length)
                {
                    cursorPos++;
                    CursorLeft++;
                }
                break;
            case ConsoleKey.Backspace:
                if (cursorPos > 0)
                {
                    buffer.Remove(cursorPos - 1, 1);
                    cursorPos--;
                    RewriteBuffer(buffer, cursorPos);
                }
                break;
            case ConsoleKey.Delete:
                if (cursorPos < buffer.Length)
                {
                    buffer.Remove(cursorPos, 1);
                    RewriteBuffer(buffer, cursorPos);
                }
                break;
            default:
                if (!char.IsControl(keyInfo.KeyChar))
                {
                    buffer.Insert(cursorPos, keyInfo.KeyChar);
                    cursorPos++;
                    RewriteBuffer(buffer, cursorPos);
                }
                break;
        }
    }

    return buffer.ToString();
}

void RewriteBuffer(StringBuilder buffer, int cursorPos)
{
    int left = CursorLeft;
    int top = CursorTop;
    CursorLeft = 0;
    Write(new string(' ', BufferWidth));
    CursorLeft = 0;
    Write(buffer.ToString());
    CursorLeft = cursorPos;
}

// for ManageTasks menu
enum TaskSort { None, ByTitle, ByDueDate }
enum TaskFilter { None, Incomplete, Complete, Overdue, Recurring }