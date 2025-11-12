using System.Text;
using static System.Console;

//foreach (string arg in args)
//{
//    WriteLine(arg);
//}


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

string seperator = "----------------------------------------------------------";
string dateFormat = "yyyy-MM-dd";

TaskManager manager = new();

// start the program
while (true)
{
    MainMenu();
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

        List<Task> tasks = selectedTask == null ? manager.RootTasks : selectedTask.SubTasks;

        string selectedTaskPath = GetTaskPath(selectedTask);
        string prompt = $@"-----------
| Options |
{seperator}
[ ENTER ] - open task
[ 1 ]     - new task
[ 2 ]     - edit task
[ 3 ]     - delete task
[ 4 ]     - go back
[ 5 ]     - main menu
{seperator}
{(selectedTask != null ? 
$"-------------\n| Task Info |\n{seperator}\nSelected: {selectedTaskPath}\n---------" +
$"\n{selectedTask.Description}\n{seperator}" +
$"\nDue at: {(selectedTask.DueDate != null ? selectedTask.DueDate?.ToString(dateFormat) +$" | {(selectedTask.DueDate.Value - selectedTask.CreatedAt).Days} days" : "-")}" +
$"\nCreated at: {selectedTask.CreatedAt.ToString(dateFormat)}" +
$"\n{seperator}" +
"\n-------------\n| Sub Tasks |" : "---------\n| Tasks |")}
{seperator}"; // display menu and selected task info


        Dictionary<ConsoleKey, int> options = new Dictionary<ConsoleKey, int> // negative values to distinguish them from list index
        {
            { ConsoleKey.D1, -1 },
            { ConsoleKey.D2, -2 },
            { ConsoleKey.D3, -3 },
            { ConsoleKey.D4, -4 },
            { ConsoleKey.D5, -5 },
        };

        (string title, string info)[] tasksInfo = tasks.Select(t => (t.Title, $"| Due: {(t.DueDate.HasValue ? $"{(t.DueDate.Value - DateTime.Now).Days} days" : "-")} |")).ToArray();
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
                    CreateNewTask(selectedTask); // create sub task
                    index = tasks.Count - 1;
                    break;
                case -2:
                    if (tasks.Count == 0) break;
                    index = menu.SelectedIndex;
                    EditTask(tasks[menu.SelectedIndex]); // index from menu
                    break;
                case -3:
                    if (tasks.Count == 0) break;
                    DeleteTask(tasks[menu.SelectedIndex]);
                    break;
                case -4:
                    if(selectedTask == null) // no task selected at rool level, return to main menu
                    {
                        return;
                    }
                    if (selectedTask.Parent == null) // go to root level tasks
                    {
                        index = manager.RootTasks.FindIndex(t => t == selectedTask);
                        selectedTask = null;
                    }
                    else // go back to parent task
                    {
                        index = selectedTask.Parent.SubTasks.FindIndex(t => t == selectedTask);
                        selectedTask = selectedTask.Parent;
                    }
                    break;
                case -5:
                    return;
            }
        }
    }

}

void Account() // TODO (probably rename this method)
{

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


void CreateNewTask(Task? parent = null)
{
    Clear();
    WriteLine("------------");
    WriteLine("| New Task |");
    WriteLine("------------");

    WriteLine("Task Title: ");
    string title = ReadLine().Trim();
    if (string.IsNullOrEmpty(title)) return;
    WriteLine("");

    WriteLine("Task Description: ");
    string description = ReadLine().Trim();
    WriteLine("");

    DateTime? dueDate = DateInput();

    manager.AddTask(title, description, parent, dueDate);
}

void EditTask(Task task)
{
    Clear();
    WriteLine(seperator);
    WriteLine("New title: ");
    string title = ReadLineWithEdit(task.Title).Trim();
    if (string.IsNullOrEmpty(title)) return;
    WriteLine("\n"); // instead of two write lines

    WriteLine("New description: ");
    string description = ReadLineWithEdit(task.Description).Trim();
    WriteLine("\n");

    DateTime? dueDate = DateInput(task.DueDate?.ToString("yyyy-MM-dd"));

    task.Title = title;
    task.Description = description;
    task.DueDate = dueDate;
}

void DeleteTask(Task task)
{
    manager.DeleteTask(task);
}

string GetTaskPath(Task? task)
{
    if (task == null)
    {
        return "-";
    }

    List<Task> parents = new();

    Task? current = task.Parent;
    while (current != null)
    {
        parents.Insert(0, current); // insert at start to maintain order
        current = current.Parent;
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

    WriteLine($"Due Date ({dateFormat}): ");
    string dueDateInput = ReadLineWithEdit(edit);

    WriteLine();

    if (string.IsNullOrEmpty(dueDateInput))
    {
        WriteLine("No due date set.");
        ReadKey(true);
    }
    else
    {
        if (DateTime.TryParse(dueDateInput, out DateTime parsedDate))
        {
            dueDate = parsedDate;
        }
        else
        {
            Write("Invalid date format. No due date set.");
            ReadKey(true);
        }
    }
    return dueDate;
}

string ReadLineWithEdit(string initial)
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