static class CS
{
    /*List<(string, ConsoleColor)> data;

    public CS(string s, ConsoleColor color)
    {
        data = [(s, color)];
    }
    public CS(string s)
    {
        data = [(s, ConsoleColor.Gray)];
    }

    public void Append(string s, ConsoleColor color)
    {
        data.Add((s, color));
    }
    public void Append(string s)
    {
        data.Add((s, ConsoleColor.Gray));
    }

    public void Write()
    {
        foreach (var (s, color) in data)
        {
            Console.ForegroundColor = color;
            Console.Write(s);
        }
        Console.ResetColor();
    }
    public void WriteLine()
    {
        Write();
        Console.WriteLine();
    }*/

    static ConsoleColor[] debugColors = null;
    //static ConsoleColor[] debugColors = [ConsoleColor.Red, ConsoleColor.DarkRed, ConsoleColor.Magenta, ConsoleColor.DarkMagenta, ConsoleColor.Cyan, ConsoleColor.DarkCyan];
    //static ConsoleColor[] debugColors = [ConsoleColor.Red, ConsoleColor.Yellow];

    public static void Write(string s, ConsoleColor color)
    {
        if (debugColors == null || debugColors.Contains(color))
        {
            Console.ForegroundColor = color;
            Console.Write(s);
            Console.ResetColor();
        }
    }
    public static void WriteLine(string s, ConsoleColor color)
    {
        if (debugColors == null || debugColors.Contains(color))
        {
            Console.ForegroundColor = color;
            Console.WriteLine(s);
            Console.ResetColor();
        }
    }
}
