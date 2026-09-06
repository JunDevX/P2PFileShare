using System.IO;
using System.Net.Sockets;
using System.Security.Cryptography;
using P2PFileShare.Models;
using Spectre.Console;

namespace P2PFileShare.Network;

public class Receiver
{
    public async Task DownloadFileAsync(string senderIp, FileMetadata metadata, string saveDirectory, CancellationToken ct)
    {
        string outputPath = Path.Combine(saveDirectory, metadata.FileName);
        using var client = new TcpClient();
        
        await client.ConnectAsync(senderIp, metadata.TcpPort, ct);
        await using var networkStream = client.GetStream();
        await using var fileStream = File.Create(outputPath);

        await AnsiConsole.Progress()
            .Columns(
                new TaskDescriptionColumn(),
                new ProgressBarColumn(),
                new PercentageColumn(),
                new DownloadedColumn(),
                new RemainingTimeColumn()
            )
            .StartAsync(async ctx =>
            {
                var downloadTask = ctx.AddTask($"[cyan]Загрузка {metadata.FileName}[/]", maxValue: metadata.FileSize);
                
                byte[] buffer = new byte[64 * 1024];
                long totalRead = 0;
                int bytesRead;

                while ((bytesRead = await networkStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
                {
                    await fileStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
                    totalRead += bytesRead;
                    downloadTask.Value = totalRead;
                }
            });

        // Проверка целосности по SHA-256
        fileStream.Close();
        AnsiConsole.MarkupLine("[grey]Проверка целостности SHA-256...[/]");
        
        using var sha256 = SHA256.Create();
        await using var checkStream = File.OpenRead(outputPath);
        byte[] downloadedHash = await sha256.ComputeHashAsync(checkStream, ct);
        string downloadedHashStr = Convert.ToHexString(downloadedHash);

        if (!downloadedHashStr.Equals(metadata.Checksum, StringComparison.OrdinalIgnoreCase))
        {
            throw new Exception("Хеш не совпал! Файл поврежден при передаче.");
        }
    }
}