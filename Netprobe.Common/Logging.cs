//CsvLogger, TransferStats ve metrik hesapları

using System.Globalization;

namespace Netprobe.Common;

public enum EventKind
{
    PacketSent,
    AckReceived,
    Timeout,
    Retransmission,
    DuplicateDropped,
    TransferComplete,
    TransferFailed
}

public sealed record LogEntry(
    EventKind Kind,
    uint      SequenceNum,
    DateTime  Timestamp,
    string    Detail = ""
);

public sealed class CsvLogger : IDisposable
{
    private readonly StreamWriter _writer;

    public CsvLogger(string path)
    {
        _writer = new StreamWriter(path, append: false);
        _writer.WriteLine("Timestamp,Event,SequenceNum,Detail");
    }

    public void Log(LogEntry entry)
    {
        _writer.WriteLine(
            $"{entry.Timestamp:O}," +
            $"{entry.Kind}," +
            $"{entry.SequenceNum}," +
            $"\"{entry.Detail}\"");
    }

    public void Log(EventKind kind, uint seq, string detail = "")
        => Log(new LogEntry(kind, seq, DateTime.UtcNow, detail));

    public void Flush() => _writer.Flush();

    public void Dispose() => _writer.Dispose();
}

public sealed class TransferStats
{
    public int      TotalPackets      { get; set; }
    public int      SentPackets       { get; set; }
    public int      AckedPackets      { get; set; }
    public int      TimeoutCount      { get; set; }
    public int      RetransmitCount   { get; set; }
    public int      FailedPackets     { get; set; }
    public int      DuplicatesDropped { get; set; }
    public long     FileSizeBytes     { get; set; }
    public TimeSpan Duration          { get; set; }

    public double ThroughputMbps =>
        Duration.TotalSeconds > 0
            ? (SentPackets * ProtocolConfig.DefaultPayloadSize * 8.0)
              / (Duration.TotalSeconds * 1_000_000)
            : 0;

    public double GoodputMbps =>
        Duration.TotalSeconds > 0
            ? (FileSizeBytes * 8.0) / (Duration.TotalSeconds * 1_000_000)
            : 0;

    public double PacketLossRate =>
        SentPackets > 0
            ? (double)FailedPackets / SentPackets
            : 0;

    public double RetransmitRate =>
        SentPackets > 0
            ? (double)RetransmitCount / SentPackets
            : 0;

    public void Print()
    {
        Console.WriteLine();
        Console.WriteLine("═══════════════ TRANSFER STATS ═══════════════");
        Console.WriteLine($"  Toplam paket       : {TotalPackets}");
        Console.WriteLine($"  Gönderilen         : {SentPackets}");
        Console.WriteLine($"  ACK alınan         : {AckedPackets}");
        Console.WriteLine($"  Timeout sayısı     : {TimeoutCount}");
        Console.WriteLine($"  Retransmit sayısı  : {RetransmitCount}");
        Console.WriteLine($"  Başarısız paket    : {FailedPackets}");
        Console.WriteLine($"  Duplicate drop     : {DuplicatesDropped}");
        Console.WriteLine($"  Süre               : {Duration.TotalSeconds:F2} s");
        Console.WriteLine($"  Throughput         : {ThroughputMbps:F3} Mbps");
        Console.WriteLine($"  Goodput            : {GoodputMbps:F3} Mbps");
        Console.WriteLine($"  Paket kayıp oranı  : {PacketLossRate:P2}");
        Console.WriteLine($"  Retransmit oranı   : {RetransmitRate:P2}");
        Console.WriteLine("═══════════════════════════════════════════════");
    }
}
