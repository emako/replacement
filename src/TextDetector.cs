namespace Replacement;

internal static class TextDetector
{
    public static bool IsTextFile(string path)
    {
        // otherwise, read the first 16KB, check if we can get something.
        using FileStream s = new(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        const int bufferLength = 16 * 1024;
        var buffer = new byte[bufferLength];
        var size = s.Read(buffer, 0, bufferLength);

        return IsText(buffer, size);
    }

    private static bool IsText(IReadOnlyList<byte> buffer, int size)
    {
        for (var i = 1; i < size; i++)
        {
            if (buffer[i - 1] == 0 && buffer[i] == 0)
            {
                return false;
            }
        }
        return true;
    }
}
