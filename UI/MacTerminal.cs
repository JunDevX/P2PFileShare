using Spectre.Console;

namespace P2PFileShare.UI;

public static class MacTerminal
{
    public static void RenderHeader(string title)
    {
        AnsiConsole.Clear();
        
        var grid = new Grid();
        grid.AddColumn();
        
        var titleTable = new Table().Border(TableBorder.None).HideHeaders();
        titleTable.AddColumn("Controls");
        titleTable.AddColumn("Title");
        titleTable.AddRow("[red]●[/] [yellow]●[/] [green]●[/]", $"[bold white]{title}[/]");

        var panel = new Panel(titleTable)
        {
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(Color.Grey39),
            Padding = new Padding(1, 0, 1, 0),
            Expand = true
        };

        AnsiConsole.Write(panel);
        AnsiConsole.WriteLine();
    }

    public static void ShowSuccess(string message)
    {
        AnsiConsole.MarkupLine($"[bold green]✔[/] [white]{Markup.Escape(message)}[/]");
    }

    public static void ShowError(string message)
    {
        AnsiConsole.MarkupLine($"[bold red]✖[/] [white]{Markup.Escape(message)}[/]");
    }
}