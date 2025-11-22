using static System.Console;

public class Menu
{
    private int _selectedIndex;

    private string _prompt;
    private (string title, string info)[] _options;
    public int SelectedIndex { get { return _selectedIndex; } }

    private MenuSettings settings = MenuSettings.Load();

    public Menu((string title, string info)[] options, string prompt, int? selectedIndex = null)
    {
        if (selectedIndex.HasValue)
        {
            // clamp
            _selectedIndex = Math.Clamp(selectedIndex.Value, 0, options.Length > 0 ? options.Length - 1 : 0);
        }
        else
        {
            _selectedIndex = 0;
        }


        _prompt = prompt;
        _options = options;
    }


    private void Display()
    {
        WriteLine(_prompt);
        if (_options.Length == 0)
        {
            WriteLine("-empty-");
            return;
        }
        else
        {
            for (int i = 0; i < _options.Length; i++)
            {
                string currentOption = _options[i].title;
                string prefix;
                string info;

                if (i == _selectedIndex)
                {
                    prefix = ">";
                    info = _options[i].info;
                    ForegroundColor = settings.SelectionForeground;
                    BackgroundColor = settings.SelectionBackground;
                }
                else
                {
                    prefix = " ";
                    info = "";
                    ForegroundColor = settings.Foreground;
                    BackgroundColor = settings.Background;
                }

                Write($"{prefix} [ {currentOption} ]");
                ForegroundColor = settings.Foreground;
                BackgroundColor = settings.Background;
                WriteLine((string.IsNullOrEmpty(info) ? "" : $" - {info}"));
            }
        }
    }

    public int Run(Dictionary<ConsoleKey, int>? keyMap = null)
    {
        CursorVisible = false;

        ConsoleKey keyPressed = new();

        Clear();
        Display();

        while (keyPressed != ConsoleKey.Enter)
        {
            ConsoleKeyInfo keyInfo = ReadKey(true);
            keyPressed = keyInfo.Key;


            if (keyPressed == ConsoleKey.UpArrow || keyPressed == ConsoleKey.W)
            {
                if (_selectedIndex == 0)
                {
                    _selectedIndex = _options.Length - 1;
                }
                else
                {
                    _selectedIndex--;
                }
            }
            else if (keyPressed == ConsoleKey.DownArrow || keyPressed == ConsoleKey.S)
            {
                if (_selectedIndex == _options.Length - 1)
                {
                    _selectedIndex = 0;
                }
                else
                {
                    _selectedIndex++;
                }
            }
            else if (keyMap != null && keyMap.TryGetValue(keyPressed, out int mappedValue))
            {
                CursorVisible = true;
                return mappedValue; // options for switch statement
            }
            if (keyPressed == ConsoleKey.UpArrow || keyPressed == ConsoleKey.W || keyPressed == ConsoleKey.DownArrow || keyPressed == ConsoleKey.S || (keyMap != null && keyMap.TryGetValue(keyPressed, out int m)))
            {
                Clear();
                Display();
            }
        }

        CursorVisible = true;
        return _selectedIndex;
    }
}

