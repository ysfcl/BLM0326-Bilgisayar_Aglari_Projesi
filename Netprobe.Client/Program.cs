using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Netprobe.Common;

Console.WriteLine("╔══════════════════════════════╗");
Console.WriteLine("║     NetProbe — CLIENT        ║");
Console.WriteLine("╚══════════════════════════════╝");

// Kullanıcıdan parametreleri al
Console.Write("Gönderilecek dosya yolu: ");
var filePath = Console.ReadLine()?.Trim() ?? "";
if (!File.Exists(filePath)) { Console.WriteLine("Dosya bulunamadı!"); return; }

Console.Write($"Sunucu adresi [{ProtocolConfig.DefaultServerHost}]: ");
var host = Console.ReadLine()?.Trim();
if (string.IsNullOrEmpty(host)) host = ProtocolConfig.DefaultServerHost;

Console.Write($"Sunucu portu [{ProtocolConfig.DefaultServerPort}]: ");
var portStr = Console.ReadLine();
int serverPort = int.TryParse(portStr, out var pp) ? pp : ProtocolConfig.DefaultServerPort;

Console.Write($"Payload boyutu (byte) [{ProtocolConfig.DefaultPayloadSize}]: ");
var pszStr = Console.ReadLine();
int payloadSize = int.TryParse(pszStr, out var psz) && psz > 0 ? psz : ProtocolConfig.DefaultPayloadSize;

Console.Write($"Timeout (ms) [{ProtocolConfig.DefaultTimeoutMs}]: ");
var toStr = Console.ReadLine();
int timeoutMs = int.TryParse(toStr, out var to) && to > 0 ? to : ProtocolConfig.DefaultTimeoutMs;

Console.Write($"Yapay kayıp oranı (0.0-1.0) [0.0]: ");
var lossStr = Console.ReadLine();
double lossRate = double.TryParse(lossStr, System.Globalization.NumberStyles.Float,
    System.Globalization.CultureInfo.InvariantCulture, out var lr) ? Math.Clamp(lr, 0, 1) : 0;

Console.WriteLine($"\n[CLIENT] '{Path.GetFileName(filePath)}' → {host}:{serverPort}");
Console.WriteLine($"[CLIENT] payload={payloadSize}B  timeout={timeoutMs}ms  loss={lossRate:P0}\n");

await SendFileAsync(filePath, host, serverPort, payloadSize, timeoutMs, lossRate);

//---------------------------------------------------

static async Task SendFileAsync(
    string filePath, string host, int serverPort,
    int payloadSize, int timeoutMs, double lossRate)
{
    var fileBytes   = File.ReadAllBytes(filePath);
    var fileHash    = Convert.ToHexString(SHA256.HashData(fileBytes));
    var totalPkts   = (uint)Math.Ceiling((double)fileBytes.Length / payloadSize);
    var serverEp    = new IPEndPoint(IPAddress.Parse(host), serverPort);
    var rng         = new Random();

    using var udp = new UdpClient();
    udp.Client.ReceiveBufferSize = ProtocolConfig.ReceiveBufferSize;
    udp.Connect(serverEp);

    var stats = new TransferStats
    {
        TotalPackets  = (int)totalPkts,
        FileSizeBytes = fileBytes.Length
    };

    Console.WriteLine($"[CLIENT] Dosya SHA256: {fileHash}\n");

    //------------------1. Init gönder--------------------------
    var init = new InitPacket
    {
        FileName     = Path.GetFileName(filePath),
        TotalPackets = totalPkts,
        FileSize     = fileBytes.Length
    };

    bool initAcked = false;
    for (int attempt = 0; attempt < ProtocolConfig.MaxRetransmissions && !initAcked; attempt++)
    {
        await udp.SendAsync(init.Serialize());
        initAcked = await WaitForAckAsync(udp, 0, timeoutMs);
        if (!initAcked) Console.WriteLine("[CLIENT] Init ACK gelmedi, tekrar gönderiliyor...");
    }
    if (!initAcked) { Console.WriteLine("[CLIENT] HATA: Sunucuya ulaşılamadı!"); return; }
    Console.WriteLine("[CLIENT] ✓ Init ACK alındı, aktarım başlıyor...\n");

    //------------------2. Stop-and-Wait döngüsü--------------------------
    var startTime = DateTime.UtcNow;

    for (uint seq = 0; seq < totalPkts; seq++)
    {
        int    offset   = (int)seq * payloadSize;
        int    len      = Math.Min(payloadSize, fileBytes.Length - offset);
        var    payload  = fileBytes[offset..(offset + len)];

        var pkt = new DataPacket
        {
            SequenceNum  = seq,
            TotalPackets = totalPkts,
            Payload      = payload
        };

        bool acked         = false;
        int  retransmits   = 0;

        while (!acked && retransmits <= ProtocolConfig.MaxRetransmissions)
        {
            // Yapay kayıp simülasyonu
            if (lossRate > 0 && rng.NextDouble() < lossRate)
            {
                Console.WriteLine($"[CLIENT] [SIMULATED LOSS] #{seq}");
                stats.TimeoutCount++;
            }
            else
            {
                var bytes = pkt.Serialize();
                await udp.SendAsync(bytes);
                stats.SentPackets++;
            }

            var ackResult = await WaitForAckAsync(udp, seq, timeoutMs);

            if (ackResult)
            {
                acked = true;
                stats.AckedPackets++;
                Console.WriteLine($"[CLIENT] ✓ #{seq}/{totalPkts - 1}  ({len} byte)");
            }
            else
            {
                retransmits++;
                stats.TimeoutCount++;

                if (retransmits <= ProtocolConfig.MaxRetransmissions)
                {
                    stats.RetransmitCount++;
                    Console.WriteLine($"[CLIENT] ⟳ #{seq} timeout — retransmit {retransmits}/{ProtocolConfig.MaxRetransmissions}");
                }
            }
        }

        if (!acked)
        {
            stats.FailedPackets++;
            Console.WriteLine($"[CLIENT] ✗ #{seq} BAŞARISIZ");
        }
    }

    stats.Duration = DateTime.UtcNow - startTime;

    //---------- 3.FIN gönderimi -----------------------------
    await udp.SendAsync(new byte[] { (byte)PacketType.Fin });
    Console.WriteLine("\n[CLIENT] FIN gönderildi.");

    Console.WriteLine($"[CLIENT] Kaynak SHA256: {fileHash}");
    stats.Print();
}

static async Task<bool> WaitForAckAsync(UdpClient udp, uint expectedSeq, int timeoutMs)
{
    using var cts = new CancellationTokenSource(timeoutMs);
    try
    {
        while (true)
        {
            var result = await udp.ReceiveAsync(cts.Token);
            var buf    = result.Buffer;
            if (buf.Length < 6) continue;

            var pktType = (PacketType)buf[0];
            if (pktType == PacketType.Ack || pktType == PacketType.FinAck)
            {
                var ack = AckPacket.Deserialize(buf);
                if (ack.AckNum == expectedSeq && ack.Success) return true;
                if (!ack.Success) return false;
            }
        }
    }
    catch (OperationCanceledException) { return false; }
    catch { return false; }
}

