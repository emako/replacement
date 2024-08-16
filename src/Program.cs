using MiniExcelLibs;
using MiniExcelLibs.OpenXml;
using UtfUnknown;

namespace Replacement;

internal class Program
{
    public static void Main(string[] args)
    {
        Console.WriteLine($"[INF] Welcome to Replacement Build {cmdwtf.BuildTimestamp.BuildTime:yyyy.MM.dd HH:mm:ss.fff}.");
        Console.WriteLine($"[INF] Usage: Prepare a sheet in the `replacement.xlsx` file, with the first column as 'Before Replacement' and the second column as 'After Replacement'.");
        Console.WriteLine($"[INF] Arguments: \"{string.Join(" ", args.Select(a => a.Contains(' ') ? $"\"{a}\"" : a))}\"");
        Console.WriteLine($"[INF] WorkingDirectory: \"{Environment.CurrentDirectory}\"");
        Console.WriteLine($"[INF] Tips: Regex replacement mode not supported nowaday.");

        if (!File.Exists("replacement.xlsx"))
        {
            Console.Error.WriteLine("[WRN] File `replacement.xlsx` not found.");
            Console.Error.WriteLine("[INF] Do you want to create `replacement.xlsx` now? (Y/N)");

            ConsoleKeyInfo key = Console.ReadKey();

            if (key.KeyChar == 'Y' || key.KeyChar == 'y')
            {
                Console.WriteLine();

                MiniExcel.SaveAs(
                    path: "replacement.xlsx",
                    value: new[] { new { Column1 = default(string) } },
                    printHeader: false,
                    sheetName: "Sheet1",
                    excelType: ExcelType.XLSX,
                    configuration: new OpenXmlConfiguration() { FastMode = true, AutoFilter = false, TableStyles = TableStyles.None },
                    overwriteFile: true
                );
                Console.WriteLine("[INF] File `replacement.xlsx` created.");
            }
            else
            {
                Environment.ExitCode = 1;
            }

            Console.Error.WriteLine("[INF] Press any key to continue . . .");
            _ = Console.ReadKey();
            return;
        }

        string? sheetName = MiniExcel.GetSheetNames("replacement.xlsx").FirstOrDefault();

        if (sheetName == null)
        {
            Console.Error.WriteLine("[ERR] File `replacement.xlsx` does not have any sheet.");
            Environment.ExitCode = 1;

            Console.Error.WriteLine("[INF] Press any key to continue . . .");
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

            if (string.IsNullOrEmpty(firstColumn))
            {
                Console.WriteLine($"[WRN] Replacement detected from \"{firstColumn}\" to \"{secondColumn}\" will be skipped.");
            }
            else
            {
                Console.WriteLine($"[INF] Replacement detected from \"{firstColumn}\" to \"{secondColumn}\".");
            }

            return (From: firstColumn, To: secondColumn);
        })
        .Where(replacement => !string.IsNullOrEmpty(replacement.From) && replacement.To is not null)
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

        Console.Error.WriteLine("[INF] Finished.");
        Console.Error.WriteLine("[INF] Press any key to continue . . .");
        _ = Console.ReadKey();
    }
}

file static class TextDetector
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
