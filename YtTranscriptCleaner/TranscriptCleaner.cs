using System.Text.RegularExpressions;

namespace YtTranscriptCleaner;

public static class TranscriptCleaner
{
    private static readonly Regex TimestampRegex = new(@"^\s*(?:(?:\d{1,2}:)?\d{1,2}:\d{2})(?:[.,]\d{1,3})?\s*$", RegexOptions.Compiled);
    private static readonly Regex WordRegex = new(@"[\p{L}\p{N}][\p{L}\p{N}'’\-]*", RegexOptions.Compiled);
    private static readonly Regex SentenceRegex = new(@".+?(?:[.!?]+[\""”']?)(?=\s+|$)|.+$", RegexOptions.Compiled);

    private static readonly HashSet<string> SmallWords =
    [
        "a", "an", "and", "as", "at", "but", "by", "for", "from", "in", "into", "is", "of", "on",
        "or", "so", "the", "to", "with", "without", "vs", "via"
    ];

    public static bool IsTimestampLine(string line) => TimestampRegex.IsMatch(line);

    public static bool IsHeadingLikeLine(string line)
    {
        var trimmed = line.Trim();
        if (trimmed.Length == 0 || trimmed.Length > 90)
        {
            return false;
        }

        if (trimmed.EndsWith(".", StringComparison.Ordinal))
        {
            return false;
        }

        if (trimmed.Contains(',') || trimmed.Contains(';') || trimmed.Contains('(') || trimmed.Contains(')'))
        {
            return false;
        }

        var words = WordRegex.Matches(trimmed).Select(m => m.Value).ToArray();
        if (words.Length == 0 || words.Length > 12)
        {
            return false;
        }

        var meaningfulCount = 0;
        var titleCaseMeaningfulCount = 0;

        foreach (var word in words)
        {
            if (SmallWords.Contains(word))
            {
                continue;
            }

            meaningfulCount++;

            if (char.IsUpper(word[0]) || word.All(char.IsUpper) || word.All(char.IsDigit))
            {
                titleCaseMeaningfulCount++;
            }
        }

        if (meaningfulCount == 0)
        {
            return false;
        }

        var ratio = (double)titleCaseMeaningfulCount / meaningfulCount;
        return ratio >= 0.75;
    }

    public static string JoinTranscriptLines(IEnumerable<string> lines)
    {
        var joined = string.Join(' ', lines);
        if (string.IsNullOrWhiteSpace(joined))
        {
            return string.Empty;
        }

        joined = Regex.Replace(joined, @"\s+", " ").Trim();
        joined = Regex.Replace(joined, @"\s+([,.;:!?])", "$1");
        joined = Regex.Replace(joined, "“\\s+", "“");
        joined = Regex.Replace(joined, @"\s+”", "”");
        joined = Regex.Replace(joined, "\"\\s+", "\"");
        joined = Regex.Replace(joined, @"\s+'", "'");

        return joined;
    }

    public static string SplitTranscriptParagraphs(string text, int sentencesPerParagraph)
    {
        if (sentencesPerParagraph <= 0 || string.IsNullOrWhiteSpace(text))
        {
            return text;
        }

        var matches = SentenceRegex.Matches(text);
        var paragraphs = new List<string>();
        var currentSentences = new List<string>();

        foreach (Match match in matches)
        {
            var sentence = match.Value.Trim();
            if (sentence.Length == 0)
            {
                continue;
            }

            currentSentences.Add(sentence);
            if (currentSentences.Count >= sentencesPerParagraph)
            {
                paragraphs.Add(string.Join(' ', currentSentences));
                currentSentences.Clear();
            }
        }

        if (currentSentences.Count > 0)
        {
            paragraphs.Add(string.Join(' ', currentSentences));
        }

        return string.Join(Environment.NewLine + Environment.NewLine, paragraphs);
    }

    public static string ConvertFromYoutubeTranscriptCopy(string text, bool preserveHeadings, int sentencesPerParagraph)
    {
        var lines = text
            .Split(["\r\n", "\n"], StringSplitOptions.None)
            .Select(l => Regex.Replace(l.Trim(), @"\s+", " "))
            .Where(l => l.Length > 0)
            .ToList();

        var captionLines = new List<string>();
        var blocks = new List<string>();

        for (var index = 0; index < lines.Count; index++)
        {
            var line = lines[index];

            if (IsTimestampLine(line))
            {
                continue;
            }

            var previousLine = index > 0 ? lines[index - 1] : string.Empty;
            var nextLine = index + 1 < lines.Count ? lines[index + 1] : string.Empty;

            var previousIsTimestamp = previousLine.Length > 0 && IsTimestampLine(previousLine);
            var nextIsTimestamp = nextLine.Length > 0 && IsTimestampLine(nextLine);

            var isLikelyHeading = !previousIsTimestamp && nextIsTimestamp && IsHeadingLikeLine(line);
            if (isLikelyHeading)
            {
                if (preserveHeadings)
                {
                    var paragraph = JoinTranscriptLines(captionLines);
                    if (paragraph.Length > 0)
                    {
                        blocks.Add(SplitTranscriptParagraphs(paragraph, sentencesPerParagraph));
                        captionLines.Clear();
                    }

                    blocks.Add($"## {line}");
                }

                continue;
            }

            captionLines.Add(line);
        }

        var remaining = JoinTranscriptLines(captionLines);
        if (remaining.Length > 0)
        {
            blocks.Add(SplitTranscriptParagraphs(remaining, sentencesPerParagraph));
        }

        return string.Join(Environment.NewLine + Environment.NewLine, blocks.Where(b => !string.IsNullOrWhiteSpace(b))).Trim();
    }
}
