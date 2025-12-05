using System.Text;
using static System.Console;

public static class ConsoleInput
{
    // to keep original text when editing
    public static string ReadLineWithEdit(string initial)
    {
        // much faster (~1 ms) with sb
        StringBuilder buffer = new StringBuilder(initial);
        int cursorPos = initial.Length;

        Write(initial);

        ConsoleKey keyPressed = new();
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

    public static void RewriteBuffer(StringBuilder buffer, int cursorPos)
    {
        int left = CursorLeft;
        int top = CursorTop;
        CursorLeft = 0;
        Write(new string(' ', BufferWidth));
        CursorLeft = 0;
        Write(buffer.ToString());
        CursorLeft = cursorPos;
    }
}