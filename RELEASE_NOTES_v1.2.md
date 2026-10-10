# DLSS MFG Manager v1.2.0

## Türkçe

İlk public GitHub sürümü.

### Öne çıkanlar

- Steam ve Epic Games kurulumlarını otomatik tarama
- Elle gerçek oyun EXE’si ekleme
- `version.dll` ve `dlssg_sm86.ini` dosyalarını kullanıcıdan güvenli biçimde seçme
- Kurulum öncesi otomatik yedekleme
- SHA-256 tabanlı dosya kontrolü ve başlatma öncesi otomatik onarım
- 2X / 3X / 4X MFG üst sınırı ayarı
- Tek tıkla kurma, güncelleme, oyunu başlatma ve Frame Gen dosyalarını kaldırma
- Kaldırma sırasında orijinal dosyaları geri yükleme
- Kalıcı oyun listesi ve ayarlar
- Türkçe / İngilizce arayüz
- Koyu tema ve dar pencerede tam düğme adlarını koruyan duyarlı düzen
- Desteklenen RTX 20/30 kartları penceresi
- Aranabilir Frame Generation oyun kataloğu ve internetten doğrulama uyarıları
- Belirgin anti-cheat / çevrimiçi oyun uyarıları

### İndirme

Normal kullanım için `DlssMfgManager.exe` dosyasını indirin. EXE self-contained olduğu için ayrıca .NET kurulumu gerekmez.

`dlssg_for_sm86` runtime dosyaları bu release’e dahil değildir. `version.dll` ve `dlssg_sm86.ini` dosyalarını ana upstream projeden ayrıca edinmelisiniz:

https://github.com/sdli1995/dlssg_for_sm86

### Çok önemli

Yalnızca çevrimdışı / tek oyunculu oyunlarda kullanın. Anti-cheat veya çevrimiçi oyunlarda proxy DLL kullanımı hesap yaptırımı ya da kalıcı ban riski taşır. Oyun kataloğu uyumluluk garantisi değildir; her oyunu güncel bilgilerle ayrıca kontrol edin.

## English

First public GitHub release.

### Highlights

- Automatic Steam and Epic Games scanning
- Manual selection of the real game executable
- Safe user selection of `version.dll` and `dlssg_sm86.ini`
- Automatic backup before installation
- SHA-256 file verification and optional pre-launch repair
- 2X / 3X / 4X MFG ceiling configuration
- One-click install, update, launch, and Frame Gen file removal
- Original-file restoration during removal
- Persistent game list and settings
- Turkish / English UI
- Dark theme and responsive action buttons that preserve full labels
- Supported RTX 20/30 GPU window
- Searchable Frame Generation game catalog with online-verification warnings
- Prominent anti-cheat / online-game warnings

### Download

Download `DlssMfgManager.exe` for normal use. It is self-contained and does not require a separate .NET installation.

`dlssg_for_sm86` runtime files are not included. Obtain `version.dll` and `dlssg_sm86.ini` separately from the primary upstream project:

https://github.com/sdli1995/dlssg_for_sm86

### Very important

Use only with offline / single-player games. Loading a proxy DLL in an anti-cheat or online game may result in account penalties or a permanent ban. The built-in catalog is not a compatibility guarantee; always verify current information for each game.

## Checksums

```text
FDAE31E1FB95D658A070DC54413DF2054FA565FF4BA39581F3FF24FA5A0C5D39  DlssMfgManager.exe
```
