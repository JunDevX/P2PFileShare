using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using P2PFileShare.Models;

namespace P2PFileShare.Network;

public class DiscoveryService
{
    private const int BroadcastPort = 8888;

    // Раздающий отправляет пакеты в сеть
    public async Task BroadcastFileAvailabilityAsync(FileMetadata metadata, CancellationToken ct)
    {
        using var udpClient = new UdpClient();
        udpClient.EnableBroadcast = true;
        var endPoint = new IPEndPoint(IPAddress.Broadcast, BroadcastPort);

        string json = JsonSerializer.Serialize(metadata);
        byte[] bytes = Encoding.UTF8.GetBytes(json);

        while (!ct.IsCancellationRequested)
        {
            await udpClient.SendAsync(bytes, bytes.Length, endPoint);
            await Task.Delay(1000, ct); // Маяк каждые 1 сек
        }
    }

    // Принимающий прослушивает сеть в поиске файлов
    public async Task<(FileMetadata metadata, string senderIp)> ListenForDiscoveredFilesAsync(CancellationToken ct)
    {
        using var udpClient = new UdpClient(BroadcastPort);
        udpClient.EnableBroadcast = true;

        while (!ct.IsCancellationRequested)
        {
            var result = await udpClient.ReceiveAsync(ct);
            string json = Encoding.UTF8.GetString(result.Buffer);
            
            try
            {
                var metadata = JsonSerializer.Deserialize<FileMetadata>(json);
                if (metadata != null)
                {
                    return (metadata, result.RemoteEndPoint.Address.ToString());
                }
            }
            catch
            {
                // Игнорируем некорректные пакеты
            }
        }

        throw new OperationCanceledException();
    }
}