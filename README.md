# 🚀 Netprobe - Proje Kurulum ve Çalıştırma Kılavuzu

Bu proje, VS Code ortamında .NET CLI kullanılarak çok katmanlı (Multi-project) mimaride geliştirilmiştir. Projeyi yerel ortamınızda sorunsuz bir şekilde ayağa kaldırmak için aşağıdaki adımları sırasıyla takip ediniz.

---

## 🛑 Önemli Önkoşullar (İlk Önce Burası!)

1. **Klasör Yoluna Dikkat Edin:** Projeyi klonladığınız veya açtığınız bilgisayardaki klasör yollarında **boşluk karakteri** ve **Türkçe karakterler** (`ı`, `ğ`, `ş`, `ç`, `ö`, `ü`) **KESİNLİKLE OLMAMALIDIR**. 
   * ❌ `C:\Users\Adiniz\Desktop\Bilgisayar Ağları Projesi` (HATA VERİR)
   * `C:\Users\Adiniz\Desktop\Netprobe_Project` (SORUNSUZ ÇALIŞIR)
2. Bilgisayarınızda **.NET SDK** yüklü olmalıdır. (Terminalde `dotnet --version` yazarak kontrol edebilirsiniz).
3. VS Code üzerinde **C# Dev Kit** eklentisinin kurulu olduğundan emin olun.

---

## 🛠️ Hızlı Kurulum ve İçe Aktarma (Import) Adımları

Projeyi GitHub'dan çektikten sonra **VS Code** ile ana klasörü açın ve entegre terminali (`Ctrl + \``) açarak sırasıyla şu komutları çalıştırın:

### 1. Paketleri ve Bağımlılıkları Geri Yükleyin (Restore)
Projenin ihtiyaç duyduğu tüm kütüphaneleri ve `.csproj` bağımlılıklarını sisteme tanıtmak için:
```bash
dotnet restore