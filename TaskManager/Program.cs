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

(Use the arrow or w and s keys to cycle though the options, and press enter to select an option)
--------------------------------------------------------------------------------------------------------------------";

TaskManager manager = new();

MainMenu(); // start the program

void MainMenu()
{
    string title = "Task Manager";
    string prompt = logo;
    string[] options = ["Tasks", "Account", "Help", "Exit"];

    Menu menu = new Menu(options, prompt, title);
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

void ViewTasks(int? taskId = null)
{
    while (true)
    {

        List<Task> tasks = taskId.HasValue ? manager.GetTaskById(taskId.Value).SubTasks : manager.RootTasks;

        Task? parent = null;

        if (taskId.HasValue)
        {
            Task current = manager.GetTaskById(taskId.Value);
            parent = current.Parent;
        }

        string title = "Tasks";
        string prompt = @"[ ENTER ] - open selected task
[ 1 ] - new task
[ 2 ] - edit selected task
[ 3 ] - delete selected task
[ 4 ] - go back
[ 5 ] - main menu
-----------------------------";
        string selectedTaskInfo = taskId.HasValue ? TaskInfo(taskId.Value) : "none";

        TaskMenu menu = new(tasks, $"{prompt}\nSelected task: {selectedTaskInfo}\n", title);

        Dictionary<ConsoleKey, int> keyMap = new Dictionary<ConsoleKey, int> // options mapped to keys
        {
            { ConsoleKey.D1, -1 },
            { ConsoleKey.D2, -2 },
            { ConsoleKey.D3, -3 },
            { ConsoleKey.D4, -4 },
            { ConsoleKey.D5, -5 },
        };

        int result = menu.Run(keyMap);

        if (result >= 0 && tasks.Count > 0)
        {
            int id = tasks[result].Id;
            taskId = id;
            continue;
        }
        else
        {
            switch (result)
            {
                case -1:
                    CreateNewTask(taskId.HasValue ? taskId.Value : null);
                    break;
                case -2:
                    if (tasks.Count == 0) break;
                    EditTask(tasks[menu.SelectedIndex].Id);
                    break;
                case -3:
                    if (tasks.Count == 0) break;
                    DeleteTask(tasks[menu.SelectedIndex].Id);
                    break;
                case -4:
                    if(!taskId.HasValue) // no task selected at rool level, go back to main menu
                    {
                        MainMenu();
                    }
                    if (parent != null) // go back to parent task
                    {
                        taskId = parent.Id;
                    }
                    else // go to root level tasks
                    {
                        taskId = null;
                    }
                        break;
                case -5:
                    MainMenu();
                    break;
            }
        }
    }

}

void Account()
{

}

void DisplayHelpInfo()
{
    Clear();
    WriteLine("Help Info:");
    ReadKey(true);
    MainMenu();
}

void Exit()
{
    Environment.Exit(0);
}


void CreateNewTask(int? parentId = null)
{
    Write("Task Title: ");
    string title = ReadLine().Trim();
    if (string.IsNullOrEmpty(title)) return;

    manager.AddTask(title, parentId.HasValue ? manager.GetTaskById(parentId.Value) : null);
}

void EditTask(int taskId)
{
    Write("New title: ");
    string title = ReadLine().Trim();
    if (string.IsNullOrEmpty(title)) return;

    Task task = manager.GetTaskById(taskId);
    task.Title = title;
}

void DeleteTask(int taskId)
{
    Task task = manager.GetTaskById(taskId);
    if (task == null) return;

    manager.DeleteTask(task);
}

string TaskInfo(int taskId)
{
    Task task = manager.GetTaskById(taskId);
    return $"{task.Title}";
}