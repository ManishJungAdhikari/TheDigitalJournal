using System.IO;
using Markdig;
using Microsoft.Maui.Storage;

namespace TheDigitalJournal.Utils;

public static class DbConstants
{
    public const string DatabaseFilename = "journaldatakhana.db";

    public static string DatabasePath =>
        Path.Combine(FileSystem.AppDataDirectory, DatabaseFilename);

    public static string MarkdownToHtml(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown)) return string.Empty;
        var pipeline = new MarkdownPipelineBuilder().UseAdvancedExtensions().Build();
        return Markdown.ToHtml(markdown, pipeline);
    }
}