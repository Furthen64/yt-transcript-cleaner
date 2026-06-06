using System.Text;
using TextCopy;

namespace YtTranscriptCleaner;

internal sealed record AppOptions(
    string? InputPath,
    string? OutputPath,
    bool KeepHeadings,
    bool NoClipboard,
    int ParagraphSentences);

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            var options = ParseArgs(args);
            var rawText = ReadInput(options);

            if (string.IsNullOrWhiteSpace(rawText))
            {
                throw new InvalidOperationException("No input transcript text found. Copy the YouTube transcript first, or pass -InputPath.");
            }

            var cleanedText = TranscriptCleaner.ConvertFromYoutubeTranscriptCopy(
                rawText,
                options.KeepHeadings,
                options.ParagraphSentences);

            if (!string.IsNullOrWhiteSpace(options.OutputPath))
            {
                File.WriteAllText(options.OutputPath, cleanedText, Encoding.UTF8);
            }

            if (!options.NoClipboard)
            {
                ClipboardService.SetText(cleanedText);
            }

            Console.WriteLine(cleanedText);
            return 0;
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
    }

    private static string ReadInput(AppOptions options)
    {
        if (!string.IsNullOrWhiteSpace(options.InputPath))
        {
            if (!File.Exists(options.InputPath))
            {
                throw new FileNotFoundException($"Input file not found: {options.InputPath}");
            }

            return File.ReadAllText(options.InputPath, Encoding.UTF8);
        }

        return ClipboardService.GetText() ?? string.Empty;
    }

    private static AppOptions ParseArgs(string[] args)
    {
        string? inputPath = null;
        string? outputPath = null;
        var keepHeadings = false;
        var noClipboard = false;
        var paragraphSentences = 0;

        for (var i = 0; i < args.Length; i++)
        {
            var arg = args[i];
            switch (arg.ToLowerInvariant())
            {
                case "--input-path":
                case "-inputpath":
                    inputPath = ReadValue(args, ref i, arg);
                    break;
                case "--output-path":
                case "-outputpath":
                    outputPath = ReadValue(args, ref i, arg);
                    break;
                case "--keep-headings":
                case "-keepheadings":
                    keepHeadings = true;
                    break;
                case "--no-clipboard":
                case "-noclipboard":
                    noClipboard = true;
                    break;
                case "--paragraph-sentences":
                case "-paragraphsentences":
                    var value = ReadValue(args, ref i, arg);
                    if (!int.TryParse(value, out paragraphSentences) || paragraphSentences is < 0 or > 50)
                    {
                        throw new ArgumentException("ParagraphSentences must be an integer between 0 and 50.");
                    }
                    break;
                default:
                    throw new ArgumentException($"Unknown argument: {arg}");
            }
        }

        return new AppOptions(inputPath, outputPath, keepHeadings, noClipboard, paragraphSentences);
    }

    private static string ReadValue(string[] args, ref int index, string option)
    {
        if (index + 1 >= args.Length)
        {
            throw new ArgumentException($"Missing value for {option}.");
        }

        index++;
        return args[index];
    }
}
