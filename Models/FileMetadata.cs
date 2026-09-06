namespace P2PFileShare.Models;

public class FileMetadata
{
    public string FileName { get; set; } = string.Empty;
    public long FileSize { get; set; }
    public string Checksum { get; set; } = string.Empty;
    public int TcpPort { get; set; }
}