using System.Text;
using System.Threading.Tasks;
using static System.Console;

string logo = @"
--------------------------------------------------------------------------------------------------------------------

  /$$$$$$$$                  /$$             /$$      /$$                                                            
 |__  $$__/                 | $$            | $$$    /$$$                                                            
    | $$  /$$$$$$   /$$$$$$$| $$   /$$      | $$$$  /$$$$  /$$$$$$  /$$$$$$$   /$$$$$$   /$$$$$$   /$$$$$$   /$$$$$$ 
    | $$ |____  $$ /$$_____/| $$  /$$/      | $$ $$/$$ $$ |____  $$| $$__  $$ |____  $$ /$$__  $$ /$$__  $$ /$$__  $$
    | $$  /$$$$$$$|  $$$$$$ | $$$$$$/       | $$  $$$| $$  /$$$$$$$| $$  \ $$  /$$$$$$$| $$  \ $$| $$$$$$$$| $$  \__/
    | $$ /$$__  $$ \____  $$| $$_  $$       | $$\  $ | $$ /$$__  $$| $$  | $$ /$$__  $$| $$  | $$| $$_____/| $$      
    | $$|  $$$$$$$ /$$$$$$$/| $$ \  $$      | $$ \/  | $$|  $$$$$$$| $$  | $$|  $$$$$$$|  $$$$$$$|  $$$$$$$| $$      
    |__/ \_______/|_______/ |__/  \__/      |__/     |__/ \_______/|__/  |__/ \_______/ \____  $$ \_______/|__/      
                                                                                        /$$  \ $$                    
                                                                                       |  $$$$$$/                    
                                                                                        \______/                     

(Use the arrow or w and s keys to cycle though the options, and press enter to select an option.)
--------------------------------------------------------------------------------------------------------------------";

string separator = "------------------------------------------";
string dateFormat = "yyyy-MM-dd";

string path = "";
TaskManager manager = new();

HandleArgs();

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
    int selectedIndex = menu.Run();

    switch (selectedIndex)
    {
        case 0:
            ViewTasks();
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

void ViewTasks(Task? selectedTask = null)
{
    int index = 0; // for selecting task when going back
    while (true)
    {
        Title = $"{selectedTask?.Title ?? "Tasks"}"; // default title if no selected task

        List<Task> tasks = selectedTask == null ? manager.RootTasks : manager.GetSubTasks(selectedTask);

        string selectedTaskPath = TaskPath(selectedTask);
        string prompt = $@"
-----------                          -----------
| Options |                          | Filters |
----------------------------------   ----------------------------------
| [ ENTER ]  - open task         |   | [ ] - 
| [ 1 ]      - new task          |   |
| [ 2 ]      - edit task         |   |
| [ 3 ]      - mark as completed |   |
| [ 4 ]      - go back           |   |
| [ DELETE ] - delete task       |   |
| [ 0 ]      - save changes      |   |
| [ ESC ]    - main menu         |   |
----------------------------------   ----------------------------------
{(selectedTask != null ? 
$"-------------\n| Task Info |\n{separator}\nSelected: {selectedTaskPath}\n---------" +
$"\nDescription: {selectedTask.Description}\n{separator}" +
$"\nCompleted: {(selectedTask.IsCompleted ? "Yes" : "No")}" +
$"\nRepeats: {(selectedTask is RecurringTask r ? $"every {r.IntervalDays} {(r.IntervalDays > 1 ? "days" : "day")}" : "No")}" +
$"\nDue at: {(selectedTask.DueDate != null ? selectedTask.DueDate?.ToString(dateFormat) + $" | {(selectedTask.DueDate.Value - selectedTask.CreatedAt).Days} days" : "-")}" +
$"\nCreated at: {selectedTask.CreatedAt.ToString(dateFormat)}" +
$"\n{separator}" +
"\n-------------\n| Sub Tasks |" : "---------\n| Tasks |")}
{separator}"; // display menu and selected task info


        Dictionary<ConsoleKey, int> options = new Dictionary<ConsoleKey, int> // negative values to distinguish them from list index
        {
            { ConsoleKey.D1, -1 },
            { ConsoleKey.D2, -2 },
            { ConsoleKey.D3, -3 },
            { ConsoleKey.D4, -4 },
            { ConsoleKey.Delete, -5 },
            { ConsoleKey.D0, -6 },
            { ConsoleKey.Escape, -7 },
        };

        (string title, string info)[] tasksInfo = tasks.Select(t => (t.Title, $"| ID: {t.Id} | Parent ID: {t.ParentId} | Completed: {(t.IsCompleted ? "Yes" : $"No {($"| Due: {(t.DueDate.HasValue ? $"{(t.DueDate.Value - DateTime.Now).Days} days" : "-")}")}")} |")).ToArray();
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
                    bool created = CreateNewTask(selectedTask); // create sub task
                    if (created) index = tasks.Count - 1; // highlight new task if it's created
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
                case -7:
                    return;
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


bool CreateNewTask(Task? parent = null)
{
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

    int? interval = RecurringTaskInput("1");

    Task task = manager.AddTask(title, description, parent, dueDate, interval);
    manager.UpdateIsCompleteUpwards(task);
    return true;
}

void EditTask(Task task)
{
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