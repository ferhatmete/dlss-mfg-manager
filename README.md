# DLSS MFG Manager

> Türkçe belge · [English documentation](README.en.md)

DLSS MFG Manager, Windows 10/11 üzerinde oyun bazında `dlssg_for_sm86` kurulumu yönetmek için hazırlanmış, kaynak kodu yayımlanan bir .NET 8 WinForms aracıdır. RTX 20 (SM75) ve RTX 30 (SM86) serisi kartları hedefler.

> [!CAUTION]
> **Yalnızca çevrimdışı / tek oyunculu oyunlarda kullanın.** Anti-cheat bulunan veya çevrimiçi oyunlarda proxy DLL kullanmak hesap yaptırımı ya da kalıcı ban ile sonuçlanabilir. Oyun listesindeki bir kayıt uyumluluk garantisi değildir. Her oyunu kurulumdan önce güncel bilgilerle ayrıca kontrol edin.

## Bu uygulama ne yapar?

- Steam ve Epic Games kütüphanelerini otomatik tarar.
- Oyunun gerçek render `.exe` dosyasını elle seçmenize izin verir.
- Seçtiğiniz `version.dll` ve `dlssg_sm86.ini` dosyalarını oyun klasörüne kurar.
- Aynı adlı mevcut dosyaları değiştirmeden önce yedekler.
- DLL dosyasını SHA-256 ile ve INI ayarını içerik üzerinden kontrol eder.
- 2X, 3X ve 4X için `MaxGeneratedFrames` değerini otomatik ayarlar.
- Oyunu başlatmadan önce eksik ya da değiştirilmiş dosyaları isteğe bağlı onarır.
- Kurduğu dosyaları kaldırır ve varsa ilk yedekteki orijinal dosyaları geri yükler.
- Oyun listenizi ve tercihlerinizi kalıcı olarak saklar.
- Seçili oyunun Steam kapak görselini indirip saydam bir kahraman arka planı olarak gösterebilir.
- Türkçe ve İngilizce arayüz, modern koyu tema, desteklenen kartlar ve aranabilir oyun kataloğu sunar.

## Uygulamanın yapmadıkları

- `dlssg_for_sm86` DLL/INI dosyalarını içermez veya internetten indirmez.
- Bir oyunun kesin uyumlu olduğunu garanti etmez.
- Anti-cheat korumasını devre dışı bırakmaz veya atlatmaz.
- Oyun kayıt dosyalarına, sürücülere ya da Windows sistem dosyalarına müdahale etmez.
- NVIDIA, Microsoft, oyun geliştiricileri veya aşağıda belirtilen upstream projelerle bağlantılı değildir.

## Sistem gereksinimleri

- Windows 10 veya Windows 11, x64
- RTX 20 serisi (SM75) ya da RTX 30 serisi (SM86) NVIDIA ekran kartı
- Güncel NVIDIA sürücüsü
- DLSS Frame Generation entegrasyonu bulunan ve uyumlu çalışan bir DirectX 12 oyun
- Yönetilecek oyun klasöründe yazma izni

Release içindeki EXE self-contained olarak yayınlanır; hedef bilgisayarda ayrıca .NET kurulması gerekmez.

## İndirme ve bütünlük kontrolü

1. GitHub sayfasındaki **Releases** bölümünü açın.
2. En yeni sürümden `DlssMfgManager.exe` dosyasını indirin.
3. Aynı release içindeki `SHA256SUMS.txt` ile dosya özetini karşılaştırın.

PowerShell ile kontrol:

```powershell
Get-FileHash .\DlssMfgManager.exe -Algorithm SHA256
```

v1.3.0 Windows x64 EXE SHA-256 değeri:

```text
3A5992DF1E99248F0E1E1870A24F474C13FB45453EC34F44E2942617C1DE67D2
```

Uygulama dijital olarak imzalanmamıştır. Bu nedenle Windows SmartScreen ilk çalıştırmada “Bilinmeyen yayıncı” uyarısı gösterebilir. Yalnızca bu GitHub deposunun Releases bölümünden indirin ve hash değerini kontrol edin.

## Gerekli Frame Generation dosyalarını edinme

Bu uygulama üçüncü taraf runtime dosyalarını yeniden dağıtmaz. Güncel dosyaları ana upstream projeden edinin:

- [`sdli1995/dlssg_for_sm86`](https://github.com/sdli1995/dlssg_for_sm86)
- [Upstream İngilizce README](https://github.com/sdli1995/dlssg_for_sm86/blob/main/README.en.md)
- [Upstream kurulum belgesi](https://github.com/sdli1995/dlssg_for_sm86/blob/main/docs/INSTALL.en.md)
- [Upstream releases](https://github.com/sdli1995/dlssg_for_sm86/releases)

Yönetici için gereken iki dosya:

```text
version.dll
dlssg_sm86.ini
```

Upstream sürüm notlarını, imza/hash bilgilerini ve `THIRD_PARTY_NOTICES.txt` dosyasını mutlaka okuyun. Başka sitelerden yeniden paketlenmiş DLL indirmeyin.

## Adım adım kullanım

### 1. Uygulamayı hazırlayın

1. Release’ten `DlssMfgManager.exe` dosyasını indirin.
2. Yazma izniniz olan kalıcı bir klasöre taşıyın.
3. EXE hash değerini doğrulayın.
4. Oyunu ve launcher’ı tamamen kapatın.

### 2. Frame Gen dosyalarını seçin

Ana ekrandaki **Frame Gen Dosyalarını Seç** düğmesine basın ve upstream paketten gelen `version.dll` ile `dlssg_sm86.ini` dosyalarını birlikte seçin.

Seçilen dosyalar şu konuma kopyalanır:

```text
%LOCALAPPDATA%\DlssMfgManager\Package
```

Dosyaları kopyalamadan mevcut klasörlerinden kullanmak isterseniz **Klasörden Kullan** seçeneğini kullanabilirsiniz. Dosyaları sonradan taşırsanız bu yol geçersiz olur.

### 3. Oyun ekleyin

İki yöntem vardır:

- **Oyunları Otomatik Tara:** Steam’in bütün kütüphanelerini ve Epic Games manifestlerini tarar. Bulunan adaylar önce onay penceresinde gösterilir.
- **Oyun Ekle:** Oyunun gerçek render `.exe` dosyasını elle seçtirir.

Launcher EXE yerine genellikle `Binaries\Win64`, `bin\x64` veya oyuna özgü benzer bir klasördeki asıl oyun EXE’sini seçmeniz gerekir. Otomatik tarama sonucu yalnızca bir tahmindir; yolu kurulumdan önce kontrol edin.

### 4. Uyumluluğu internetten kontrol edin

**Frame Gen Oyun Listesi** çevrimdışı bir başvuru kataloğudur ve eksik veya güncelliğini yitirmiş olabilir. Listede bulunmayan bir oyun yine de çalışabilir; listede bulunan bir oyun da güncelleme sonrasında çalışmayabilir.

Kurulumdan önce:

1. Oyunun yerel/offline çalışabildiğini doğrulayın.
2. Anti-cheat veya çevrimiçi bileşen kullanmadığını kontrol edin.
3. Güncel `dlssg_for_sm86` sorunlarını ve oyun özelindeki kurulum notlarını araştırın.
4. Gerekirse katalog penceresindeki **Seçili Oyunu İnternette Kontrol Et** düğmesini kullanın.

### 5. Oyun görselini kullanın (isteğe bağlı)

**İnternetten oyun görseli kullan** seçiliyken uygulama Steam oyun kimliğini otomatik taramadan alır; kimlik bilinmiyorsa oyun adıyla Steam mağaza araması yapar. Bulunan kapak görseli seçili oyun alanında koyu ve saydam bir arka plan olarak gösterilir. **Görseli Yenile** ile önbelleği yenileyebilirsiniz. Bu özellik kapatıldığında uygulama yalnızca yerel renk geçişini kullanır.

### 6. MFG seviyesini seçin

| Arayüz | INI değeri | Anlamı |
|---|---:|---|
| 2X | `MaxGeneratedFrames=1` | Her gerçek kare için en fazla 1 üretilen kare |
| 3X | `MaxGeneratedFrames=2` | Her gerçek kare için en fazla 2 üretilen kare |
| 4X | `MaxGeneratedFrames=3` | Her gerçek kare için en fazla 3 üretilen kare |

Bu değer bir üst sınırdır. Gerçekte kullanılabilen seviye oyunun entegrasyonuna ve upstream runtime’a bağlıdır.

### 7. Kurun ve başlatın

1. **Kur / Güncelle** düğmesine basın.
2. İnternet/anti-cheat uyarısını okuyup onaylayın.
3. Kurulum durumu **Hazır** olunca **Oyunu Başlat** düğmesine basın.
4. Oyunun grafik ayarlarından DLSS Frame Generation özelliğini açın.

**Başlatmadan önce otomatik kontrol et ve onar** seçiliyse uygulama her başlatmada dosya varlığını, DLL hash değerini ve INI ayarını kontrol eder. Dosya silinmiş veya değişmişse kaynak paketinizden yeniden kurar. Böylece her oyun açılışında dosyaları elle kopyalamanız gerekmez.

### 8. Kaldırın

Kırmızı **Frame Gen Dosyalarını Sil** düğmesi:

- Yöneticinin kurduğu `version.dll` ve `dlssg_sm86.ini` dosyalarını kaldırır.
- Kurulumdan önce aynı adlı dosyalar mevcutsa ilk yedekten geri yükler.
- Kullanıcı tarafından sonradan değiştirilmiş görünen dosyaları güvenlik amacıyla otomatik silmez.

Yedekler oyun klasöründeki şu dizinde tutulur:

```text
.dlss-mfg-manager-backups\yyyyMMdd_HHmmss_fff\
```

## Düğmeler ve durumlar

- **Mavi – Kur / Güncelle:** Frame Gen dosyalarını kurar, INI seviyesini günceller.
- **Kırmızı – Frame Gen Dosyalarını Sil:** Kurulumu kaldırır ve mümkünse orijinalleri geri yükler.
- **Yeşil – Oyunu Başlat:** Otomatik kontrol seçiliyse onarır, ardından oyunu kendi klasöründe başlatır.
- **Hazır:** Dosya ve ayarlar beklenen durumda.
- **Kurulu değil:** Yönetilen dosyalardan biri veya tamamı yok.
- **Onarım gerekli:** DLL hash’i ya da INI ayarı beklenen değerle eşleşmiyor.
- **Kontrol edilemedi:** Oyun EXE yolu veya kaynak dosyalar erişilebilir değil.

İşlem düğmeleri pencere küçültüldüğünde de tam adlarını gösterecek biçimde orantılı olarak yerleşir.

## Önemli güvenlik uyarıları

- Anti-cheat bulunan veya çevrimiçi oyunlarda kullanmayın.
- Oyunu ve launcher’ı kurulum/güncelleme/kaldırma sırasında kapalı tutun.
- Her oyun güncellemesinden sonra uyumluluğu tekrar kontrol edin.
- Üçüncü taraf DLL’leri yalnızca güvenilir upstream kaynaktan alın ve hash/imza kontrolü yapın.
- Yönetici veya upstream runtime için “virüs korumasını tamamen kapatın” diyen kılavuzları izlemeyin.
- Bir oyun klasöründe zaten `version.dll` varsa bunun hangi moda veya araca ait olduğunu öğrenmeden üzerine yazmayın. Uygulama yedek alır ancak modlar birbirleriyle çakışabilir.
- Farklı proxy adı (`winmm.dll`, `dxgi.dll`, `dbghelp.dll` vb.) isteyen oyunlarda upstream’in oyun özelindeki talimatlarını izleyin. Bu sürüm doğrudan `version.dll` yöntemini yönetir.
- Düşük temel FPS, yüksek MFG seviyelerinde görüntü bozulması ve gecikmeyi artırabilir.
- Kullanım riski kullanıcıya aittir; hesap, oyun dosyası, kararlılık veya performans garantisi verilmez.

Daha fazla güvenlik bilgisi için [SECURITY.md](SECURITY.md) dosyasını okuyun.

## Sorun giderme

### Oyun listede yok

**Oyun Ekle** ile gerçek render EXE’sini seçin. Katalog kesin uyumluluk listesi değildir. Oyunun adını, güncel sürümünü ve `dlssg_for_sm86` uyumluluğunu internette ayrıca araştırın.

### Frame Generation seçeneği görünmüyor

- Oyunun DLSS Frame Generation entegrasyonuna sahip olduğunu doğrulayın.
- DX12 modunu kullandığınızdan emin olun.
- Launcher yerine gerçek render EXE klasörünü seçin.
- Upstream loglarını ve güncel kurulum belgesini inceleyin.
- Dosya yolu, NVIDIA sürücüsü ve upstream runtime sürümünü kontrol edin.

### Oyun açılmıyor veya çöküyor

1. Oyunu kapatın.
2. **Frame Gen Dosyalarını Sil** ile kurulumu kaldırın.
3. Oyun dosyalarını launcher üzerinden doğrulayın.
4. Upstream sorunlar sayfasında oyun/sürüm için bilinen problemi arayın.

### Windows EXE’yi engelliyor

İndirme kaynağının bu deponun Releases bölümü olduğunu ve SHA-256 değerinin eşleştiğini doğrulayın. EXE kod imzalı değildir; SmartScreen uyarısı tek başına zararlı yazılım kanıtı değildir, fakat hash eşleşmiyorsa dosyayı çalıştırmayın.

## Saklanan veriler ve gizlilik

Oyun listesi ve tercihler:

```text
%LOCALAPPDATA%\DlssMfgManager\settings.json
```

İçe aktarılan kaynak paketi:

```text
%LOCALAPPDATA%\DlssMfgManager\Package
```

İndirilen oyun görselleri:

```text
%LOCALAPPDATA%\DlssMfgManager\ArtworkCache
```

Uygulama telemetri göndermez, hesap bilgisi istemez ve otomatik olarak DLL indirmez. Oyun görseli özelliği açıksa Steam AppID bilinmeyen kayıtlar için oyun adı Steam mağaza arama hizmetine gönderilir; görsel yalnızca Steam'in HTTPS görsel alan adlarından alınır ve yerelde önbelleğe kaydedilir. Özelliği ana ekrandan kapatabilirsiniz. **İnternette kontrol et** düğmesi yalnızca varsayılan tarayıcıda hazırlanmış bir arama açar.

## Kaynaktan derleme

Gerekenler:

- Windows 10/11 x64
- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)

```powershell
dotnet build .\DlssMfgManager.sln --configuration Release
```

Tek, self-contained Windows EXE oluşturmak için:

```powershell
.\publish.ps1
```

Çıktı:

```text
outputs\win-x64-v1.3\DlssMfgManager.exe
```

## Upstream projeler ve teşekkür

- [`sdli1995/dlssg_for_sm86`](https://github.com/sdli1995/dlssg_for_sm86) — yöneticinin kullandığı `version.dll` ve `dlssg_sm86.ini` biçiminin ana upstream kaynağı.
- [`Coldwood1026/dlssg_for_sm75`](https://github.com/Coldwood1026/dlssg_for_sm75) — upstream tarafından belirtilen RTX 20 / SM75 uyarlama kaynağı.
- [`Nukem9/dlssg-to-fsr3`](https://github.com/Nukem9/dlssg-to-fsr3) — ana upstream’in üçüncü taraf bildiriminde belirtilen GPLv3 proje/loader kökeni.

Ayrıntılı atıf ve lisans sınırları için [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md) dosyasına bakın.

## Bağımsızlık bildirimi

DLSS MFG Manager bağımsız bir topluluk aracıdır. NVIDIA, Microsoft, Valve, Epic Games, oyun geliştiricileri veya yukarıdaki upstream proje sahipleri tarafından hazırlanmış, onaylanmış ya da desteklenmiş değildir. Ürün ve oyun adları ilgili sahiplerinin ticari markalarıdır.
