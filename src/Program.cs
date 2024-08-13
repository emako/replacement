using MiniExcelLibs;
using UtfUnknown;

namespace Replacement;

internal class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine("[INF] Welcome to Replacement.");
        Console.WriteLine($"[INF] Usage: Just prepare your `replacement.xlsx` in working directory.");
        Console.WriteLine($"[INF] Command: {string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}");
        Console.WriteLine($"[INF] WorkingDirectory: {Environment.CurrentDirectory}");
        Console.WriteLine($"[INF] Default mode is line replacement."); // TODO: Add regex replacement mode.

        if (!File.Exists("replacement.xlsx"))
        {
            Console.Error.WriteLine("[ERR] replacement.xlsx not found.");
            Environment.ExitCode = 1;
            _ = Console.ReadKey();
            return;
        }

        string? sheetName = MiniExcel.GetSheetNames("replacement.xlsx").FirstOrDefault();

        if (sheetName == null)
        {
            Console.Error.WriteLine("[ERR] replacement.xlsx file does not have any sheet.");
            Environment.ExitCode = 1;
            _ = Console.ReadKey();
            return;
        }

        IEnumerable<dynamic> data = MiniExcel.Query("replacement.xlsx", sheetName: sheetName);
        IEnumerable<(string? From, string? To)> replacements = data.Select((dynamic lineItem) =>
        {
            string? firstColumn = null;
            string? secondColumn = null;

            foreach (KeyValuePair<string?, object> item in lineItem)
            {
                if (firstColumn is null)
                {
                    firstColumn = item.Value?.ToString() ?? string.Empty;
                }
                else if (secondColumn is null)
                {
                    secondColumn = item.Value?.ToString() ?? string.Empty;
                }
                else
                {
                    break;
                }
            }
            return (From: firstColumn, To: secondColumn);
        })
        .Where(replacement => replacement.From is not null && replacement.To is not null)
        .ToArray(); // ToArray is important to avoid multiple enumeration.

        foreach (string filePath in Directory.EnumerateFiles(".", "*", SearchOption.AllDirectories))
        {
            try
            {
                if (TextDetector.IsTextFile(filePath))
                {
                    DetectionResult result = CharsetDetector.DetectFromFile(filePath);
                    string text = File.ReadAllText(filePath, result.Detected.Encoding);

                    Console.WriteLine($"[INF] File `{filePath}` text detected {result.Detected.Encoding.EncodingName}.");

                    foreach (var (from, to) in replacements)
                    {
                        text = text.Replace(from!, to!);
                    }

                    File.WriteAllText(filePath, text, result.Detected.Encoding);
                }
            }
            catch (Exception e)
            {
                Console.Error.WriteLine($"[ERR] Error with file `{filePath}`, {e.Message}.");
            }
        }

        _ = Console.ReadKey();
    }
}
