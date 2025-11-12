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
    Title = $"{selectedTask?.Title ?? "Tasks"}"; // default title if no selected task
    int index = 0; // for selecting task when going back
    while (true)
    {

        List<Task> tasks = selectedTask == null ? manager.RootTasks : selectedTask.SubTasks;

        string selectedTaskPath = GetTaskPath(selectedTask);
        string prompt = $@"{seperator}
[ ENTER ] - open task
[ 1 ]     - new task
[ 2 ]     - edit task
[ 3 ]     - delete task
[ 4 ]     - go back
[ 5 ]     - main menu
{seperator}

{seperator}
Selected Task: {selectedTaskPath}
{seperator}
{(selectedTask != null ? $"Created at: {selectedTask.CreatedAt.ToString("yyyy-MM-dd")}\nDue: {(selectedTask.DueDate != null ? selectedTask.DueDate?.ToString("yyyy-MM-dd") : "-")}" : "")}
{seperator}

{(selectedTask == null ? "---------\n| Tasks |\n---------" : "-------------\n| Sub Tasks |\n-------------")}
"; // display menu and selected task info


        Dictionary<ConsoleKey, int> options = new Dictionary<ConsoleKey, int> // negative values to distinguish them from list index
        {
            { ConsoleKey.D1, -1 },
            { ConsoleKey.D2, -2 },
            { ConsoleKey.D3, -3 },
            { ConsoleKey.D4, -4 },
            { ConsoleKey.D5, -5 },
        };

        (string title, string info)[] tasksInfo = tasks.Select(t => (t.Title, $"| Due: {(t.DueDate.HasValue ? t.DueDate?.ToString("yyyy-MM-dd") : "-")} |")).ToArray();
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

    Write("Task Title: ");
    string title = ReadLine().Trim();
    if (string.IsNullOrEmpty(title)) return;

    DateTime? dueDate = null;
    Write("Due Date (yyyy-mm-dd): ");
    string dueDateInput = ReadLine().Trim();

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

    if (string.IsNullOrEmpty(title)) return;

    manager.AddTask(title, parent, dueDate);
}

void EditTask(Task task)
{
    WriteLine(seperator);
    WriteLine("New title: ");
    string title = Menu.ReadLineWithEdit(task.Title).Trim();

    if (string.IsNullOrEmpty(title)) return;

    task.Title = title;
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