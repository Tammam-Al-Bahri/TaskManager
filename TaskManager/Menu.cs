using static System.Console;

public class Menu
{
    protected int _selectedIndex;

    protected string _prompt;
    protected string[] _options;
    public int SelectedIndex { get { return _selectedIndex; } }

    public Menu(string[] options, string prompt, string title)
    {
        _selectedIndex = 0;

        _prompt = prompt;
        _options = options;

        Title = title;
    }


    protected virtual void Display()
    {
        WriteLine(_prompt);
        for (int i = 0; i < _options.Length; i++)
        {
            string currentOption = _options[i];
            string prefix;

            if (i == _selectedIndex)
            {
                prefix = ">";
                ForegroundColor = ConsoleColor.Black;
                BackgroundColor = ConsoleColor.White;
            }
            else
            {
                prefix = " ";
                ForegroundColor = ConsoleColor.White;
                BackgroundColor = ConsoleColor.Black;
            }

            WriteLine($"{prefix} [ {currentOption} ]");
        }
        ResetColor();
    }

    public virtual int Run(Dictionary<ConsoleKey, int>? keyMap = null)
    {
        CursorVisible = false;

        ConsoleKey keyPressed = new();

        while (keyPressed != ConsoleKey.Enter)
        {
            Clear();
            Display();

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
        }

        CursorVisible = true;

        return _selectedIndex;
    }
}