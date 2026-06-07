//UDP Sunucu - Dosya Alıcı

using System.Net;
using System.Net.Sockets;
using System.Security.Cryptography;
using Netprobe.Common;

Console.WriteLine("╔══════════════════════════════╗");
Console.WriteLine("║     NetProbe — SERVER        ║");
Console.WriteLine("╚══════════════════════════════╝");
Console.Write($"Dinlenecek port [{ProtocolConfig.DefaultServerPort}]: ");
var portInput = Console.ReadLine();
int port = int.TryParse(portInput, out var p) ? p : ProtocolConfig.DefaultServerPort;

await RunServerAsync(port);

// ────────────────────────────────────────────────────────────────────────────

static async Task RunServerAsync(int port)
{
    using var udp = new UdpClient(port);
    udp.Client.ReceiveBufferSize = 68000;
    Console.WriteLine($"[SERVER] {port} portunda dinleniyor...\n");

    while (true)
    {
        try
        {
            await AcceptTransferAsync(udp);
        }
        catch (SocketException ex)
        {
            Console.WriteLine($"[SERVER] Bağlantı hatası: {ex.Message}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[SERVER] Hata: {ex.Message}");
        }
        
        Console.WriteLine("\n[SERVER] Yeni aktarım bekleniyor...\n");
    }
}

static async Task AcceptTransferAsync(UdpClient udp)
{
    //----------------------1. Init paketini bekle--------------------------------
    Console.WriteLine("[SERVER] Init paketi bekleniyor...");
    UdpReceiveResult result;
    InitPacket init;

    while (true)
    {
        result = await udp.ReceiveAsync();
        if (result.Buffer[0] != (byte)PacketType.Init) continue;
        init = InitPacket.Deserialize(result.Buffer);
        break;
    }

    var clientEp = result.RemoteEndPoint;
    Console.WriteLine($"[SERVER] '{init.FileName}' — {init.TotalPackets} paket, {init.FileSize} byte");
    Console.WriteLine($"[SERVER] İstemci: {clientEp}");

    var initAck = new AckPacket { AckNum = 0, Success = true };
    await udp.SendAsync(initAck.Serialize(), clientEp);

    //------------------2. Veri paketlerini al--------------------------
    var outputDir = "received";
    Directory.CreateDirectory(outputDir);
    var outputPath = Path.Combine(outputDir, Path.GetFileName(init.FileName));

    var stats       = new TransferStats { TotalPackets = (int)init.TotalPackets, FileSizeBytes = init.FileSize };
    var receivedSeqs= new HashSet<uint>();
    var chunks      = new Dictionary<uint, byte[]>();
    var startTime   = DateTime.UtcNow;

    while (receivedSeqs.Count < init.TotalPackets)
    {
        result = await udp.ReceiveAsync();
        var buf = result.Buffer;

        if (buf[0] == (byte)PacketType.Fin)
        {
            var finAck = new AckPacket { Type = PacketType.FinAck, AckNum = 0 };
            await udp.SendAsync(finAck.Serialize(), clientEp);
            break;
        }

        if (buf[0] != (byte)PacketType.Data) continue;

        DataPacket pkt;
        try { pkt = DataPacket.Deserialize(buf); }
        catch { Console.WriteLine("[SERVER] Bozuk paket — atlanıyor"); continue; }

        // Duplicate kontrolü
        if (receivedSeqs.Contains(pkt.SequenceNum))
        {
            stats.DuplicatesDropped++;
            Console.WriteLine($"[SERVER] Duplicate #{pkt.SequenceNum} — yoksayılıyor");
            var dupAck = new AckPacket { AckNum = pkt.SequenceNum, Success = true };
            await udp.SendAsync(dupAck.Serialize(), clientEp);
            continue;
        }

        // Checksum doğrulama
        if (!pkt.VerifyChecksum())
        {
            Console.WriteLine($"[SERVER] Checksum HATASI #{pkt.SequenceNum}");
            var nack = new AckPacket { AckNum = pkt.SequenceNum, Success = false };
            await udp.SendAsync(nack.Serialize(), clientEp);
            continue;
        }

        // Başarılı paket
        receivedSeqs.Add(pkt.SequenceNum);
        chunks[pkt.SequenceNum] = pkt.Payload;
        stats.AckedPackets++;
        Console.WriteLine($"[SERVER] Paket #{pkt.SequenceNum}/{init.TotalPackets - 1} OK");

        var ack = new AckPacket { AckNum = pkt.SequenceNum, Success = true };
        await udp.SendAsync(ack.Serialize(), clientEp);
    }

    stats.Duration = DateTime.UtcNow - startTime;

    //---------- 3.Dosyayı birleştir---------------------------
    using (var fs = File.Create(outputPath))
    {
        for (uint i = 0; i < init.TotalPackets; i++)
        {
            if (chunks.TryGetValue(i, out var chunk))
                await fs.WriteAsync(chunk);
            else
                Console.WriteLine($"[SERVER] UYARI: Paket #{i} eksik!");
        }
    }

    // ── 4. Bütünlük kontrolü ─────────────────────────────────────────
    var receivedHash = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(outputPath)));
    Console.WriteLine($"\n[SERVER] Dosya yazıldı: {outputPath}");
    Console.WriteLine($"[SERVER] SHA256: {receivedHash}");

    stats.Print();

    var fin = new AckPacket { Type = PacketType.FinAck, AckNum = 0 };
    await udp.SendAsync(fin.Serialize(), clientEp);
}

