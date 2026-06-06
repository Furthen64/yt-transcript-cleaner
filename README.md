# yt-transcript-cleaner
You copy paste the transcript into this app, get a nicer formatted output

## Run

```bash
dotnet run --project ./YtTranscriptCleaner/YtTranscriptCleaner.csproj -- --input-path ./raw.txt --output-path ./clean.txt --no-clipboard
```

Options:
- `--input-path` (or `-InputPath`)
- `--output-path` (or `-OutputPath`)
- `--keep-headings` (or `-KeepHeadings`)
- `--no-clipboard` (or `-NoClipboard`)
- `--paragraph-sentences` (or `-ParagraphSentences`, 0-50)
