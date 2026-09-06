using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using P2PFileShare.Models;
using Spectre.Console;

namespace P2PFileShare.Network;

public class Sender
{
    public async Task<FileMetadata> PrepareFileAsync(string filePath, int port)
    {
        var fileInfo = new FileInfo(filePath);
        if (!fileInfo.Exists)
            throw new FileNotFoundException("Файл не найден!", filePath);

        AnsiConsole.MarkupLine("[grey]Расчет SHA-256 хеша файла...[/]");
        using var sha256 = SHA256.Create();
        await using var stream = File.OpenRead(filePath);
        byte[] hashBytes = await sha256.ComputeHashAsync(stream);
        string hash = Convert.ToHexString(hashBytes);

        return new FileMetadata
        {
            FileName = fileInfo.Name,
            FileSize = fileInfo.Length,
            Checksum = hash,
            TcpPort = port
        };
    }

    public async Task StartStreamingAsync(string filePath, int port, CancellationToken ct)
    {
        var listener = new TcpListener(IPAddress.Any, port);
        listener.Start();

        try
        {
            using var client = await listener.AcceptTcpClientAsync(ct);
            await using var networkStream = client.GetStream();
            await using var fileStream = File.OpenRead(filePath);

            byte[] buffer = new byte[64 * 1024]; // 64 KB Чанк
            int bytesRead;

            while ((bytesRead = await fileStream.ReadAsync(buffer, 0, buffer.Length, ct)) > 0)
            {
                await networkStream.WriteAsync(buffer.AsMemory(0, bytesRead), ct);
            }
        }
        finally
        {
            listener.Stop();
        }
    }
}