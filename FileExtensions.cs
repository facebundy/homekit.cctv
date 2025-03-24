namespace homekit.cctv;

public static class FileExtensions
{
    public static long GetDirectorySize(string directoryPath)
    {
        long size = 0;

        try
        {
            // Get all file sizes in the directory
            foreach (string file in Directory.GetFiles(directoryPath))
            {
                FileInfo fileInfo = new FileInfo(file);
                size += fileInfo.Length;
            }

            // Recursively get size of all subdirectories
            foreach (string dir in Directory.GetDirectories(directoryPath))
            {
                size += GetDirectorySize(dir);
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error accessing {directoryPath}: {ex.Message}");
        }

        return size;
    }

    public static string FormatSize(long bytes)
    {
        string[] sizes = { "B", "KB", "MB", "GB", "TB" };
        int order = 0;
        double len = bytes;
        while (len >= 1024 && order < sizes.Length - 1)
        {
            order++;
            len /= 1024;
        }
        return string.Format("{0:0.##} {1}", len, sizes[order]);
    }
}