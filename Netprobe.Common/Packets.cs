using System.Security.Cryptography;
using System.Text;

namespace NetProbe.Common;  
// Datapacket, InitPacket, AckPacket gibi yapılar 
//UDP sadece byte taşımaktadır, burada yapılan şey taşınan byte'a anlam katılmasıdır. 

public enum PacketType : byte
{
    Data   = 0x01,
    Ack    = 0x02,
    Init   = 0x03,
    Fin    = 0x04,
    FinAck = 0x05
}

//----------------Veri Paketi----------------------
//Yapı: type(1) | seq(4) | totalPackets(4) | payloadLen(4) | checksum(32) | payload
public sealed class DataPacket
{                               //property tanımlamalarında init kullanılması immutable yapı sağlar
                                //bir kez set edilirse o değer bir daha değiştirilemez
    public PacketType Type{get;init;}=PacketType.Data;
    public uint SequenceNum{get;init;}
    public uint TotalPackets{get;init;}
    public byte [] Payload{get;init;}=Array.Empty<byte>();
    public byte[] Checksum{get;private set;}=Array.Empty<byte>();

    
    // SHA-256 checksum hesapla
    public void ComputeChecksum()
    {
        Checksum = SHA256.HashData(Payload);
    }

    // Checksum doğrula
    public bool VerifyChecksum()
    {
        return Checksum.Length == 32 && Checksum.SequenceEqual(SHA256.HashData(Payload));
    }


    public byte [] Serialize()
{
    ComputeChecksum();

    int totalLen=1+4+4+4+Payload.Length;
    var buff=new byte[totalLen];
    int pos=0;

    buff[pos++]=(byte)Type;
    BitConverter.GetBytes(SequenceNum).CopyTo(buff,pos);
    pos+=4;
    
    BitConverter.GetBytes(TotalPackets).CopyTo(buff,pos);
    pos+=4;
    
    BitConverter.GetBytes(Payload.Length).CopyTo(buff,pos);
    pos+=4;
    
    Checksum.CopyTo(buff,pos);
    pos+=32;
   
    Payload.CopyTo(buff,pos);

    return buff;
}

//Byte dizisi C# nesnesine çevrilir
public static DataPacket Deserialize(byte[] data)
{
    int pos=0;
    var type=(PacketType)data[pos++];
    var seq=BitConverter.ToInt32(data,pos);pos+=4;
    var total=BitConverter.ToInt32(data,pos);pos+=4;
    var payloadLen=BitConverter.ToInt32(data,pos);pos+=4;
    var checksum=data[pos..(pos+32)];pos+=32;
    var payLoad=data[pos..(pos+payloadLen)];

    return new DataPacket
    {
        Type=type,
        SequenceNum=(uint)seq,
        TotalPackets=(uint)total,
        Payload=payLoad,
        Checksum=checksum,
    };

}


}


//------------------------ACK Paketi----------------------------
//Yapı: type(1) | ackNum(4) | status(1) | checksum(32) 

public sealed class AckPacket
{
    public PacketType Type{get; init;}=PacketType.Ack;
    public uint AckNum{get;init;}
    public bool Success{get;init;} =true;

    private static readonly byte[] EmptyPayload="ACK"u8.ToArray(); 

    public byte[] Serialize()
    {
        var checksum=SHA256.HashData(EmptyPayload);
        var buff=new byte[1+4+1+32];
        int pos=0;
        
        buff[pos++]=(byte)Type;
        BitConverter.GetBytes(AckNum).CopyTo(buff,pos); pos+=4;
        buff[pos++]=(byte)(Success?1:0);
        checksum.CopyTo(buff,pos);

        return buff;
    }

    public static AckPacket Deserialize(byte[] data)
    {
        int pos = 0;
        var type    = (PacketType)data[pos++];
        var ackNum  = BitConverter.ToUInt32(data, pos); pos += 4;
        var success = data[pos] == 1;

        return new AckPacket { Type = type, AckNum = ackNum, Success = success };
    }
}


//------------------------Init Paketi----------------------------
//Aktarım başlamadan önce gönderilir: dosya adı + toplam paket sayısı + dosya boyutu
public sealed class InitPacket
{
    public PacketType Type         { get; init; } = PacketType.Init;
    public string     FileName     { get; init; } = "";
    public uint       TotalPackets { get; init; }
    public long       FileSize     { get; init; }

    public byte[] Serialize()
    {
        var nameBytes = Encoding.UTF8.GetBytes(FileName);
        var buff = new byte[1 + 4 + 8 + 4 + nameBytes.Length];
        int pos = 0;

        buff[pos++] = (byte)Type;
        BitConverter.GetBytes(TotalPackets).CopyTo(buff, pos); pos += 4;
        BitConverter.GetBytes(FileSize).CopyTo(buff, pos);     pos += 8;
        BitConverter.GetBytes(nameBytes.Length).CopyTo(buff, pos); pos += 4;
        nameBytes.CopyTo(buff, pos);

        return buff;
    }

    public static InitPacket Deserialize(byte[] data)
    {
        int pos = 0;
        var type    = (PacketType)data[pos++];
        var total   = BitConverter.ToUInt32(data, pos); pos += 4;
        var size    = BitConverter.ToInt64(data, pos);  pos += 8;
        var nameLen = BitConverter.ToInt32(data, pos);  pos += 4;
        var name    = Encoding.UTF8.GetString(data, pos, nameLen);

        return new InitPacket 
        { 
            Type = type, 
            TotalPackets = total, 
            FileSize = size, 
            FileName = name 
        };
    }
}