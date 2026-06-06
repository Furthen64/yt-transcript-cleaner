namespace YtTranscriptCleaner.Tests;

public class TranscriptCleanerTests
{
    [Theory]
    [InlineData("0:02")]
    [InlineData("12:34")]
    [InlineData("1:02:03")]
    [InlineData("12:34.567")]
    public void IsTimestampLine_MatchesSupportedFormats(string line)
    {
        Assert.True(TranscriptCleaner.IsTimestampLine(line));
    }

    [Fact]
    public void ConvertFromYoutubeTranscriptCopy_RemovesTimestampsAndLikelyHeadingsByDefault()
    {
        const string input = """
            Hello everyone
            0:02
            today we talk about
            Introduction
            0:04
            0:06
            clean transcripts.
            """;

        var cleaned = TranscriptCleaner.ConvertFromYoutubeTranscriptCopy(input, preserveHeadings: false, sentencesPerParagraph: 0);

        Assert.Equal("Hello everyone today we talk about clean transcripts.", cleaned);
    }

    [Fact]
    public void ConvertFromYoutubeTranscriptCopy_PreservesHeadingWhenRequested()
    {
        const string input = """
            Hello everyone
            0:02
            this is the intro
            Chapter One
            0:04
            this is the body.
            """;

        var cleaned = TranscriptCleaner.ConvertFromYoutubeTranscriptCopy(input, preserveHeadings: true, sentencesPerParagraph: 0);

        Assert.Equal($"Hello everyone this is the intro{Environment.NewLine}{Environment.NewLine}## Chapter One{Environment.NewLine}{Environment.NewLine}this is the body.", cleaned);
    }

    [Fact]
    public void SplitTranscriptParagraphs_SplitsBySentenceCount()
    {
        const string input = "First sentence. Second sentence. Third sentence.";

        var cleaned = TranscriptCleaner.SplitTranscriptParagraphs(input, 2);

        Assert.Equal($"First sentence. Second sentence.{Environment.NewLine}{Environment.NewLine}Third sentence.", cleaned);
    }
}
