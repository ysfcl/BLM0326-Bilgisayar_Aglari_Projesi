// Port:9000 timeout, payload boyutu sabitleri

namespace Netprobe.Common;

public static class ProtocolConfig
{
    // Ağ Ayarları
    public const int    DefaultServerPort  = 9000;
    public const string DefaultServerHost  = "127.0.0.1";

    // Paket Boyutu
    public const int DefaultPayloadSize = 1024;   // 1 KB
    public const int MaxUdpPacketSize  = 65507;   // UDP max

    // Güvenilirlik Mekanizması
    public const int    MaxRetransmissions = 5;
    public const int    DefaultTimeoutMs   = 500;
    public const double ArtificialLossRate = 0.0;

    // Buffer
    public const int ReceiveBufferSize = 68000;
}
