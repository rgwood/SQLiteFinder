using System.Text;
using static Spectre.Console.AnsiConsole;

const bool LogExceptions = false;
byte[] expectedBytes = Encoding.ASCII.GetBytes("SQLite format 3\0");
string startingDir = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

long fileCount = 0;
long foundCount = 0;

foreach (var path in GetFiles(startingDir))
{
    fileCount++;

    if (CheckIfSQLiteDB(path))
    {
        WriteLine(path);
        foundCount++;
    }
}

MarkupLine($"[green]Done![/] Found [blue]{foundCount}[/] SQLite databases in [blue]{fileCount}[/] files");

bool CheckIfSQLiteDB(string fullPath)
{
    try
    {
        // TODO: is there an API we can check to identify cloud disk providers in a less hacky way?
        if (fullPath.Contains("iCloud") || fullPath.Contains("OneDrive")) // don't want to download entire remote drive
            return false;

        using Stream stream = new FileStream(fullPath, FileMode.Open);

        Span<byte> firstBytes = stackalloc byte[16];
        if (stream.Read(firstBytes) != 16)
        {
            return false;
        }

        if (firstBytes.SequenceEqual(expectedBytes))
        {
            return true;
        }
    }
    catch (Exception ex)
    {
        if (LogExceptions)
        {
            MarkupLine($"[red]Exception thrown when checking[/] {fullPath}");
            WriteException(ex);
        }
    }

    return false;
}

// Because a recursive Directory.EnumerateFiles throws if any subdirectory cannot be accessed
static IEnumerable<string> GetFiles(string path)
{
    Queue<string> queue = new();
    queue.Enqueue(path);
    while (queue.Count > 0)
    {
        path = queue.Dequeue();
        try
        {
            foreach (string subDir in Directory.GetDirectories(path))
            {
                queue.Enqueue(subDir);
            }
        }
        catch (Exception ex)
        {
            if(LogExceptions)
                WriteException(ex);
        }
        string[]? files = null;
        try
        {
            files = Directory.GetFiles(path);
        }
        catch (Exception ex)
        {
            if (LogExceptions)
                WriteException(ex);
        }
        if (files != null)
        {
            for (int i = 0; i < files.Length; i++)
            {
                yield return files[i];
            }
        }
    }
}