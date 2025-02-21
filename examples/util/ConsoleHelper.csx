using System.Threading;

public static class ConsoleHelper
{
    public static readonly object _consoleLock = new object();

    public static void WriteLineColored(
        string message,
        ConsoleColor foreground,
        ConsoleColor? background = null
    )
    {
        lock (_consoleLock)
        {
            var originalForeground = Console.ForegroundColor;
            var originalBackground = Console.BackgroundColor;

            Console.ForegroundColor = foreground;
            if (background.HasValue)
                Console.BackgroundColor = background.Value;

            Console.WriteLine(message);

            Console.ForegroundColor = originalForeground;
            Console.BackgroundColor = originalBackground;
        }
    }

    public static void WriteColored(
        string message,
        ConsoleColor foreground,
        ConsoleColor? background = null
    )
    {
        lock (_consoleLock)
        {
            var originalForeground = Console.ForegroundColor;
            var originalBackground = Console.BackgroundColor;

            Console.ForegroundColor = foreground;
            if (background.HasValue)
                Console.BackgroundColor = background.Value;

            Console.Write(message);

            Console.ForegroundColor = originalForeground;
            Console.BackgroundColor = originalBackground;
        }
    }

    public static void WriteLine(
        string message = ""
    )
    {
        lock (_consoleLock)
        {
            Console.WriteLine(message);
        }
    }

    // Convenience methods for common message types
    public static void Success(string message) => WriteLineColored(message, ConsoleColor.Green);

    public static void Error(string message) => WriteLineColored(message, ConsoleColor.Red);

    public static void Warning(string message) => WriteLineColored(message, ConsoleColor.Yellow);

    public static void Info(string message) => WriteLineColored(message, ConsoleColor.Cyan);
}

// Gets the best human-readable size string for a given size in bytes
public static string GetBestSizeString(long size)
{
    if (size > 1024L * 1024L * 1024L * 1024L)  // TB
    {
        return $"{size / (1024.0 * 1024 * 1024 * 1024):N2} TB";
    }
    else if (size > 1024L * 1024L * 1024L)  // GB
    {
        return $"{size / (1024.0 * 1024 * 1024):N2} GB";
    }
    else if (size > 1024L * 1024L)  // MB
    {
        return $"{size / (1024.0 * 1024):N2} MB";
    }
    else if (size > 1024L)  // KB
    {
        return $"{size / 1024.0:N2} KB";
    }
    else  // Bytes
    {
        return $"{size:N0} B";
    }
}

// Pads a string with spaces to center it within a given width useful for if you are putting text in a box
public static string PadCenter(this string text, int width)
{
    if (text.Length >= width) return text;
    int leftPadding = (width - text.Length) / 2;
    int rightPadding = width - text.Length - leftPadding;
    return new string(' ', leftPadding) + text + new string(' ', rightPadding);
}


// Spinner animation for console output, it should be ran in a task and stopped when the operation is done
public static readonly string[] _spinnerFrames = {
    "[ / ]", "[ - ]", "[ \\ ]", "[ | ]",
};

private static void RunSpinner(CancellationToken token, string[] messages)
{
    const int animationDelay = 80;
    var spinnerIndex = 0;
    var messagesIndex = 0;
    var loopsPerMessage = 4;
    var loops = 0;

    Console.CursorVisible = false;

    while (!token.IsCancellationRequested)
    {
        lock (ConsoleHelper._consoleLock)
        {
            var frame = _spinnerFrames[spinnerIndex];
            Console.SetCursorPosition(0, Console.CursorTop);
            ConsoleHelper.WriteColored(frame, ConsoleColor.Cyan);
            Console.Write($" {messages[messagesIndex % messages.Length]}");
            Console.Write(new string(' ', Console.WindowWidth - frame.Length - messages[messagesIndex % messages.Length].Length - 1));
        }

        if (spinnerIndex == _spinnerFrames.Length - 1)
        {
            loops++;
        }

        if (loops >= loopsPerMessage)
        {
            messagesIndex = (messagesIndex + 1) % messages.Length;
            loops = 0;
        }

        spinnerIndex = (spinnerIndex + 1) % _spinnerFrames.Length;
        Thread.Sleep(animationDelay);
    }

    Console.CursorVisible = true;
}