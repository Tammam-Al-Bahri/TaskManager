using System.Text.Json;

public class MenuSettings
{
    public ConsoleColor Background { get; set; } = ConsoleColor.Gray;
    public ConsoleColor Foreground { get; set; } = ConsoleColor.DarkGreen;
    public ConsoleColor SelectionBackground { get; set; } = ConsoleColor.DarkGreen;
    public ConsoleColor SelectionForeground { get; set; } = ConsoleColor.Gray;


    private static readonly string SettingsFile = "menu-settings.json";

    // save current settings to file
    public void Save()
    {
        string json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SettingsFile, json);
    }

    // load settings from file
    public static MenuSettings Load()
    {
        if (!File.Exists(SettingsFile))
            return new MenuSettings(); // if file missing

        string json = File.ReadAllText(SettingsFile);
        try
        {
            return JsonSerializer.Deserialize<MenuSettings>(json) ?? new MenuSettings(); // new if null
        }
        catch
        {
            return new MenuSettings(); // if file corrupted
        }
    }
}
