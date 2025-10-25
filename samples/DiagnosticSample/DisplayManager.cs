using DiagnosticSample.Models;
using Spectre.Console;

namespace DiagnosticSample.Display;

/// <summary>
/// Handles all Spectre.Console UI rendering and display logic
/// </summary>
public static class DisplayManager
{
    public static void ShowHeader()
    {
        AnsiConsole.Write(
            new FigletText("Diagnostic Sample")
                .LeftJustified()
                .Color(Color.Green));

        AnsiConsole.MarkupLine("[dim]OpenRouter.NET - Complete Diagnostic & Testing Tool[/]\n");
    }

    public static void ShowOutputDirectory(string outputDir)
    {
        AnsiConsole.MarkupLine($"\n[bold green]📁 Output directory:[/] [cyan]{outputDir}[/]\n");
    }

    public static Table CreateResponseDisplay(
        string content,
        TimeSpan elapsed,
        bool receivedFirstToken,
        int artifactsCount,
        string currentArtifact,
        string currentTool)
    {
        var table = new Table().Border(TableBorder.None);
        table.AddColumn(new TableColumn("").NoWrap());

        var status = receivedFirstToken
            ? $"[green]● Streaming[/]"
            : $"[yellow]● Waiting for response...[/]";

        var artifactInfo = artifactsCount > 0 ? $" [blue]📦 {artifactsCount} artifact(s)[/]" : "";

        if (!string.IsNullOrEmpty(currentArtifact))
        {
            artifactInfo += $" [yellow]✏️  Creating: {currentArtifact}[/]";
        }

        if (!string.IsNullOrEmpty(currentTool))
        {
            artifactInfo += $" [cyan]{currentTool}[/]";
        }

        table.AddRow($"[bold yellow]Assistant:[/] {status} [dim]({elapsed.TotalSeconds:F1}s)[/]{artifactInfo}");
        table.AddRow("");

        if (!string.IsNullOrEmpty(content))
        {
            table.AddRow(content.Replace("[", "[[").Replace("]", "]]"));
        }

        return table;
    }

    public static void ShowCompletionMessage()
    {
        AnsiConsole.MarkupLine("\n[bold green]✅ Streaming Complete![/]\n");
    }

    public static void ShowStatisticsTable(DiagnosticResult result)
    {
        var table = new Table();
        table.Border(TableBorder.Rounded);
        table.AddColumn("[bold]Metric[/]");
        table.AddColumn("[bold]Value[/]");

        table.AddRow("Total chunks", $"[cyan]{result.ChunkCount}[/]");
        table.AddRow("Text chunks", $"[cyan]{result.TextChunks}[/]");
        table.AddRow("Artifact events", $"[cyan]{result.ArtifactEvents}[/]");
        table.AddRow("Artifacts created", $"[green]{result.Artifacts.Count}[/]");
        table.AddRow("Server tools executed", $"[yellow]{result.ServerToolCalls.Count(t => t.State == "Completed" || t.State == "Error")}[/]");
        table.AddRow("Client tools called", $"[blue]{result.ClientToolCalls.Count}[/]");
        table.AddRow("TTFT", $"[yellow]{result.TimeToFirstToken?.TotalMilliseconds:F0}ms[/]");

        AnsiConsole.Write(table);
    }

    public static void ShowServerToolCalls(DiagnosticResult result)
    {
        if (!result.ServerToolCalls.Any())
            return;

        AnsiConsole.MarkupLine($"\n[bold yellow]🔧 Server Tool Calls:[/]");
        foreach (var call in result.ServerToolCalls.Where(t => t.State == "Completed" || t.State == "Error"))
        {
            var statusIcon = call.State == "Completed" ? "✅" : "❌";
            var color = call.State == "Completed" ? "green" : "red";
            AnsiConsole.MarkupLine($"   {statusIcon} [{color}]{call.Name}[/] [dim]({call.Duration?.TotalMilliseconds:F0}ms)[/]");

            if (call.State == "Completed")
            {
                var resultPreview = call.Result ?? "";
                if (resultPreview.Length > 0)
                {
                    AnsiConsole.MarkupLine($"      [dim]Result: {resultPreview.Substring(0, Math.Min(100, resultPreview.Length))}[/]");
                }
            }
            else
            {
                AnsiConsole.MarkupLine($"      [red]Error: {call.Error}[/]");
            }
        }
    }

    public static void ShowClientToolCalls(DiagnosticResult result)
    {
        if (!result.ClientToolCalls.Any())
            return;

        AnsiConsole.MarkupLine($"\n[bold blue]📢 Client Tool Calls:[/]");
        foreach (var call in result.ClientToolCalls)
        {
            AnsiConsole.MarkupLine($"   • [cyan]{call.Name}[/]");
            if (call.Arguments.Length > 0)
            {
                AnsiConsole.MarkupLine($"      [dim]Args: {call.Arguments.Substring(0, Math.Min(100, call.Arguments.Length))}[/]");
            }
        }
    }

    public static void ShowArtifacts(DiagnosticResult result)
    {
        if (result.Artifacts.Count == 0)
            return;

        AnsiConsole.MarkupLine($"\n[bold blue]📦 Artifacts Created:[/]");
        foreach (var artifact in result.Artifacts)
        {
            AnsiConsole.MarkupLine($"   • [cyan]{artifact.Title}[/] ({artifact.Type}) - {artifact.Content.Length} chars");

            var preview = artifact.Content.Length > 200
                ? artifact.Content.Substring(0, 200) + "..."
                : artifact.Content;

            var panel = new Panel(preview.Replace("[", "[[").Replace("]", "]]"))
            {
                Header = new PanelHeader($"Preview: {artifact.Title}"),
                Border = BoxBorder.Rounded,
                Padding = new Padding(1)
            };
            AnsiConsole.Write(panel);
        }
    }

    public static void ShowOutputFiles(string outputDir, DiagnosticResult result, bool hasSystemPrompt)
    {
        AnsiConsole.MarkupLine($"\n[bold green]📁 All files saved to:[/] [cyan]{outputDir}[/]");
        AnsiConsole.MarkupLine($"   • [dim]raw_stream.txt[/] - Complete SSE stream");
        AnsiConsole.MarkupLine($"   • [dim]raw_text.txt[/] - Raw text content (character-by-character)");
        AnsiConsole.MarkupLine($"   • [dim]parsed_events.txt[/] - Event log");
        AnsiConsole.MarkupLine($"   • [dim]summary.txt[/] - Statistics & full text");

        if (hasSystemPrompt)
        {
            AnsiConsole.MarkupLine($"   • [dim]system_prompt.txt[/] - Generated system prompt");
        }

        if (result.Artifacts.Count > 0)
        {
            AnsiConsole.MarkupLine($"   • [dim]artifacts/[/] - {result.Artifacts.Count} extracted file(s)");
        }
    }

    public static void ShowError(Exception ex)
    {
        AnsiConsole.MarkupLine($"\n[red]❌ Error: {ex.GetType().Name.Replace("[", "[[").Replace("]", "]]")}[/]");
        AnsiConsole.WriteLine($"   {ex.Message}");
    }

    public static void ShowDoneMessage()
    {
        AnsiConsole.MarkupLine("\n[green]✅ Done![/]");
    }
}
