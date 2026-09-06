using P2PFileShare.Network;
using P2PFileShare.UI;
using Spectre.Console;

namespace P2PFileShare;

class Program
{
    static async Task Main(string[] args)
    {
        Console.Title = "macOS Terminal — P2P File Share";
        
        MacTerminal.RenderHeader("P2P FILE SHARE ENGINE v1.0");

        var mode = AnsiConsole.Prompt(
            new SelectionPrompt<string>()
                .Title("[bold cyan]Выберите режим работы:[/]")
                .PageSize(10)
                .AddChoices(new[] { "1. Раздать файл (Sender)", "2. Принять файл (Receiver)", "3. Выход" })
        );

        var cts = new CancellationTokenSource();

        try
        {
            if (mode.StartsWith("1"))
            {
                string filePath = AnsiConsole.Ask<string>("Введите [cyan]путь к файлу[/]:").Trim('"');
                int tcpPort = 9999;

                var sender = new Sender();
                var metadata = await sender.PrepareFileAsync(filePath, tcpPort);

                MacTerminal.RenderHeader($"P2P SENDER — {metadata.FileName}");
                AnsiConsole.MarkupLine($"[green]Файл готов к передаче![/] Размер: [yellow]{metadata.FileSize} байт[/]");
                AnsiConsole.MarkupLine("[bold magenta]Трансляция в сеть... Ожидание подключения...[/]\n");

                var discovery = new DiscoveryService();
                
                var broadcastTask = discovery.BroadcastFileAvailabilityAsync(metadata, cts.Token);
                var streamTask = sender.StartStreamingAsync(filePath, tcpPort, cts.Token);

                await streamTask;
                cts.Cancel();
                
                MacTerminal.ShowSuccess("Файл успешно передан!");
            }
            else if (mode.StartsWith("2"))
            {
                MacTerminal.RenderHeader("P2P RECEIVER — Поиск раздач...");
                AnsiConsole.MarkupLine("[bold cyan]Сканирование локальной сети...[/]\n");

                var discovery = new DiscoveryService();
                var (metadata, senderIp) = await discovery.ListenForDiscoveredFilesAsync(cts.Token);

                AnsiConsole.MarkupLine($"[green]Найден файл![/] [yellow]{metadata.FileName}[/] от IP: [bold white]{senderIp}[/]\n");
                
                if (AnsiConsole.Confirm("Начать скачивание?"))
                {
                    var receiver = new Receiver();
                    await receiver.DownloadFileAsync(senderIp, metadata, Environment.CurrentDirectory, cts.Token);
                    MacTerminal.ShowSuccess($"Файл сохранен: {metadata.FileName}");
                }
            }
        }
        catch (Exception ex)
        {
            MacTerminal.ShowError(ex.Message);
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Нажмите любую клавишу для выхода...[/]");
        Console.ReadKey();
    }
}