# KorOglan'ın Windows Bildirim Yöneticisi

Windows bildirimlerini çoklu monitör kurulumlarında doğru monitörde göstermek için geliştirilmiş bir masaüstü uygulaması.

## Özellikler

- **Akıllı Bildirim Yönlendirme**: Bildirimleri kaynak uygulamanın bulunduğu monitöre otomatik olarak yönlendirir
- **Gerçek Zamanlı Yakalama**: Windows API hooks kullanarak native bildirimleri anında yakalar
- **Çoklu Monitör Desteği**: Sınırsız sayıda monitör konfigürasyonunu destekler
- **Windows Varsayılan Pozisyon Koruması**: Bildirimleri taşırken Windows'un varsayılan konumlandırmasını korur
- **Sistem Tepsisi Entegrasyonu**: Arka planda çalışır ve sistem tepsisinden kontrol edilir
- **Çoklu Dil Desteği**: Türkçe ve İngilizce arayüz
- **Debug Logging**: İsteğe bağlı detaylı debug log sistemi

## Sistem Gereksinimleri

- Windows 10/11 (64-bit)
- .NET 9.0 Runtime
- Yönetici ayrıcalıkları (Windows API erişimi için gerekli)
- Çoklu monitör kurulumu

## Kurulum

1. [Releases](../../releases) sayfasından en son sürümü indirin
2. `WindowsNotificationManager.exe` dosyasını çalıştırın
3. Windows tarafından yönetici izni istediğinde "Evet" deyin
4. Uygulama otomatik olarak sistem tepsisinde başlayacak

## Kullanım

### İlk Çalıştırma

- Uygulama başladıktan sonra sistem tepsisinde çalışmaya devam eder
- Sistem tepsisi ikonuna çift tıklayarak ana pencereyi açabilirsiniz
- Ana pencerede monitör bilgileri ve aktif pencereler görüntülenir

### Ayarlar

Ana penceredeki ayarlar bölümünden:

- **Windows ile başlat**: Uygulamanın sistem başlangıcında otomatik çalışması
- **Debug log oluştur**: Sorun giderme için detaylı log kaydı

### Nasıl Çalışır

1. Uygulama tüm açık pencereleri sürekli takip eder
2. Bir bildirim geldiğinde, kaynak uygulamanın hangi monitörde olduğunu tespit eder
3. Bildirimi o monitöre otomatik olarak taşır
4. Windows'un varsayılan pozisyonlandırmasını koruyarak doğal görünümü sağlar

## Teknik Detaylar

### Mimari

- **WindowsAPIHook** (`src/Core/`): WinEvent hooks ile bildirim yakalama
- **MonitorManager** (`src/Core/`): Çoklu monitör yönetimi
- **WindowTracker** (`src/Core/`): Pencere pozisyon takibi
- **NotificationRouter** (`src/Core/`): Bildirim yönlendirme mantığı
- **NotificationService** (`src/Services/`): Ana orkestratör servis

### API Entegrasyonu

- `SetWinEventHook` - Sistem çapında pencere olaylarını yakalama
- `SetWindowPos` - Bildirim pozisyonunu değiştirme
- `EnumDisplayMonitors` - Monitör numaralandırma
- `MonitorFromWindow` - Pencere-monitör eşleştirme
- `GetForegroundWindow` - Aktif pencere tespiti
- `GetWindowRect` - Pencere boyut ve pozisyonu
- `GetMonitorInfo` - Monitör detay bilgileri

### Windows Sürüm Desteği

- **Windows 10**: `ShellExperienceHost.exe` süreciyle çalışır
- **Windows 11**: `explorer.exe` süreciyle çalışır
- Otomatik sürüm algılama ve uyum sağlama

## Geliştirici Bilgileri

### Derleme

```bash
dotnet build
```

### Çalıştırma (Debug) - Yönetici Gerekli

```bash
# ÖNEMLI: Yüksek ayrıcalıklı komut istemi/PowerShell'de çalıştırın
dotnet run
```

### Yayın İçin Derleme

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

### Debug Logging

Debug logları `notification_debug.log` dosyasında saklanır (exe dosyasının yanında oluşturulur).

## Katkıda Bulunma

1. Bu repository'yi fork edin
2. Feature branch'i oluşturun (`git checkout -b feature/yeni-ozellik`)
3. Değişikliklerinizi commit edin (`git commit -m 'Yeni özellik: açıklama'`)
4. Branch'i push edin (`git push origin feature/yeni-ozellik`)
5. Pull Request oluşturun

## Lisans

Bu proje MIT lisansı altında yayınlanmıştır. Detaylar için `LICENSE` dosyasını inceleyiniz.

## İletişim

- **Twitter/X**: [@KorOglan](https://x.com/KorOglan)
- **GitHub**: [KilimcininKorOglu](https://github.com/KilimcininKorOglu)

## Sorun Bildirimi

Herhangi bir sorunla karşılaştığınızda:

1. Debug logging'i açın
2. Sorunu yeniden oluşturun
3. Log dosyasını ekleyerek [Issues](../../issues) sayfasında sorun bildirin

---

**Not**: Bu uygulama sistem seviyesinde Windows API'leri kullandığı için yönetici ayrıcalıkları gerektirir. Bu tamamen güvenlidir ve sadece bildirim yönlendirme işlemi için kullanılır.
