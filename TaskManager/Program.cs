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

MainMenu();

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

        string title = "Tasks";
        string prompt = @"[ ENTER ] - open selected task
[ 1 ] - new task
[ 2 ] - edit selected task
[ 3 ] - delete selected task
[ 4 ] - go back
[ 5 ] - main menu
-----------------------------";

        TaskMenu menu = new(tasks, prompt, title);

        Dictionary<ConsoleKey, int> keyMap = new Dictionary<ConsoleKey, int>
        {
            { ConsoleKey.D1, -1 },
            { ConsoleKey.D2, -2 },
            { ConsoleKey.D3, -3 },
            { ConsoleKey.D4, -4 },
            { ConsoleKey.D5, -5 },
        };

        int result = menu.Run(keyMap);

        if (result >= 0)
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
                    EditTask(tasks[menu.SelectedIndex].Id);
                    break;
                case -3:
                    DeleteTask(tasks[menu.SelectedIndex].Id);
                    break;
                case -4:
                    Task parent = tasks[menu.SelectedIndex].Parent;
                    if (parent == null) return;
                    taskId = parent.Id;
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

    WriteLine();

    manager.AddTask(title, parentId.HasValue ? manager.GetTaskById(parentId.Value) : null);

    ViewTasks(parentId.HasValue ? parentId.Value : null);
}

void EditTask(int taskId)
{
    // TO DO
    Console.Write(taskId);
    ReadKey(true);
    ViewTasks();

}

void DeleteTask(int taskId)
{
    // TODO:
    // bool success = DeleteTask(taskId);
    //if (success)
    //{
    //    Write("Task deleted successfully.");
    //}
    //else
    //{
    //    Write("Task not found.");
    //}
    ReadKey(true);
    ViewTasks();
}