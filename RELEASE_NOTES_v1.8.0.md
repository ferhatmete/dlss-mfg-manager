# DLSS MFG Manager v1.8.0

## Türkçe

- Gerçek Windows penceresinde görülen siyah/kırık kart ve düğme kenarları kaldırıldı.
- Yuvarlak kartlarda Win32 `Region` kesimi kullanılmıyor; saydam köşeler önce gerçek arka planı çiziyor.
- Düğmelerdeki siyah kanca/çizgi artefaktları giderildi ve odak çizgisi arayüz rengine uyarlandı.
- Kütüphanedeki eksik oyun görselleri arayüzü durdurmadan, aynı anda en fazla iki istekle arka planda yükleniyor.
- Steam mağaza verisi sonuç vermezse SteamDB'yi taramak yerine SteamDB'nin de kullandığı resmi Steam CDN yolları deneniyor.
- Oyunları yeniden taramak, listede bulunan oyunların eksik Steam AppID bilgisini de tamamlıyor.
- 980×680 ve 1360×820 boyutlarında hem bitmap hem gerçek Win32 pencere görüntüsüyle doğrulama yapıldı.

> SteamDB genel bir API sunmadığını ve otomatik taramaya izin vermediğini belirtir. Bu sürüm SteamDB sitesini scrape etmez; görselleri yalnızca Steam HTTPS alan adlarından alır.

## English

- Removes the black/broken card and button edges that appeared in the real Windows window.
- Rounded cards no longer use binary Win32 `Region` clipping; transparent corners paint the real parent surface first.
- Eliminates black hook/line artifacts around buttons and replaces the native dotted focus cue with a theme-matched ring.
- Loads missing library artwork progressively in the background with a maximum of two concurrent requests.
- Falls back to official Steam CDN paths also linked by SteamDB, without crawling SteamDB when store metadata is unavailable.
- A fresh game scan now restores missing Steam AppIDs on games already present in the library.
- Verifies the layout at 980×680 and 1360×820 using both bitmap and real Win32-window captures.

> SteamDB states that it has no public API and disallows automated crawling. This release does not scrape SteamDB; artwork is accepted only from Steam HTTPS domains.
