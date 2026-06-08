# NetProbe: UDP Tabanlı Güvenilir Dosya Aktarımı, Trafik İzleme ve Ağ Performans Analiz Platformu

NetProbe, UDP (User Datagram Protocol) üzerinde uygulama katmanında çalışan güvenilir bir dosya aktarım protokolü, trafik izleme ve performans analiz platformudur. Bursa Teknik Üniversitesi Bilgisayar Mühendisliği Bölümü Bilgisayar Ağları Dersi Dönem Projesi kapsamında geliştirilmiştir.

Sistem; paketlerin bölünmesi, sıralanması, ACK (Acknowledgement) mekanizması, timeout yönetimi ve retransmission (yeniden gönderim) özelliklerini barındıran özgün bir uygulama katmanı protokolü mimarisine sahiptir.

---

## 🚀 Özellikler & Protokol Tasarımı

### 1. Güvenilir Veri Aktarımı (Reliable Data Transfer)
* **Parçalama ve Birleştirme:** Büyük dosyalar byte dizileri halinde dinamik paket boyutlarına bölünür ve alıcı tarafta bozulmadan sırasıyla birleştirilir.
* **Paket Numaralandırma:** Her pakete özgün bir `Sequence Number` atanarak paket sırası ve duplicate (çift) paket kontrolü sağlanır.
* **ACK Mekanizması:** Alıcı veri paketini başarıyla aldığında göndericiye ilgili paket numarasına ait ACK mesajı döner.
* **Timeout & Retransmission:** Belirlenen süre zarfında ACK'i gelmeyen paketler için otomatik timeout tetiklenir ve paket yeniden gönderilir (Max: 5 deneme).
* **Bütünlük Kontrolü (Integrity Check):** Aktarım sonunda dosyanın eksiksiz ve hatasız iletildiğini doğrulamak için **MD5/SHA-256 Checksum** mekanizması kullanılır.

### 2. Trafik İzleme ve Olay Kayıt (Logging) Sistemi
Aktarım sırasında gerçekleşen tüm ağ olayları gerçek zamanlı olarak loglanır ve performans analizi için saklanır:
* Paket gönderim ve ACK alınma zaman damgaları (timestamp)
* Timeout oluşumları ve yeniden gönderim sayıları
* Başarılı/başarısız paket istatistikleri

### 3. Ağ Performans Metrikleri
Toplanan loğlar üzerinden aşağıdaki metrikler hesaplanarak sistem performansı analiz edilir:
* **Throughput & Goodput** oranları
* **Packet Loss Rate** (Paket Kayıp Oranı)
* **Completion Time** (Toplam Aktarım Süresi)

---

## 📂 Proje Yapısı

Proje, bağımlılıkları minimize etmek ve modülerliği sağlamak amacıyla 3 ana katmandan oluşmaktadır:

📂 NetProbe/
├── 📂 NetProbe.Common/      # Ortak veri yapıları, Paket modeli ve Checksum araçları
├── 📂 NetProbe.Client/      # Dosya gönderimini başlatan, timeout ve log tutan İstemci
├── 📂 NetProbe.Server/      # UDP soketini dinleyen, ACK üreten ve dosyayı birleştiren Sunucu
├── 📂 NetProbe.Analysis/    # Log verilerinden grafik ve metrik üreten yardımcı modül
├── 📄 NetProbe.sln          # Çözüm (Solution) dosyası
└── 📄 README.md             # Proje dokümantasyonu

---

### Proje Yapımcıları
* Yusuf Çil
* Asım Burak Öztürk
