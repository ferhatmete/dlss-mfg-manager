# DLSS MFG Manager v1.9.0

## Türkçe

- **PC'yi Tara** artık Steam, Xbox/Microsoft Store, Epic, GOG, EA app, Ubisoft Connect, Battle.net, Rockstar, Amazon Games, Riot Games, itch.io ve yaygın yerel oyun klasörlerini tarar.
- Xbox PC oyunlarında `MicrosoftGame.config` içindeki gerçek EXE bilgisi okunur.
- GOG kayıtları ve Windows kurulu-program kayıtları ek kaynak olarak kullanılır.
- Steam `common` altındaki oyunlar güncel bir appmanifest olmasa bile yerel aday olarak bulunabilir.
- Yeni **Klasör Tara** düğmesi launcher kullanmayan, taşınabilir veya özel klasörde bulunan oyunları tarar; seçilen kök sonraki taramalar için saklanır.
- Tarama, dosyanın edinilme biçimini veya lisans durumunu değerlendirmez. Erişilebilir yerel x64 oyun EXE adaylarını aynı teknik kurallarla inceler.
- Launcher, editör, oyun motoru, anti-cheat servisi, SDK, redistributable ve yardımcı programlar için yanlış-pozitif filtreleri eklendi.
- Epic manifestlerinde launcher EXE yerine gerçek oyun/render EXE'si tercih edilir.
- Frame Gen ayrıntı alanı daha kompakt hale getirildi; oyun kartı kütüphanesi pencerenin %60'ını kullanır.
- Ayrı **Görseli Yenile** düğmesi kaldırıldı. Steam görselleri resmi Steam CDN üzerinden otomatik bulunur ve önbelleğe alınır.
- Steam görseli bulunamayan yerel oyunlara EXE ikonu ve oyun adından otomatik kapak oluşturulur; boş kart bırakılmaz.

> Hiçbir Windows uygulaması ortak manifesti olmayan her taşınabilir oyunu otomatik olarak yüzde yüz bulamaz. Bilinen platformlar otomatik taranır; diğer konumlar için **Klasör Tara** kullanılır. Sonuç ekranındaki EXE yolunu her zaman kontrol edin.

## English

- **Scan PC** now covers Steam, Xbox/Microsoft Store, Epic, GOG, EA app, Ubisoft Connect, Battle.net, Rockstar, Amazon Games, Riot Games, itch.io, and common local game-library roots.
- Reads the real executable from `MicrosoftGame.config` for Xbox PC installations.
- Uses GOG records and Windows installed-program records as additional discovery sources.
- Finds local games under Steam `common` even when no current appmanifest is present.
- Adds **Scan Folder** for launcher-free, portable, and custom-location libraries; selected roots are remembered for later scans.
- Discovery does not judge a file's source or licence state. Accessible local x64 game-executable candidates use the same technical checks.
- Adds false-positive filters for launchers, editors, engines, anti-cheat services, SDKs, redistributables, and helper tools.
- Epic manifests now prefer the actual game/render executable over launcher executables.
- The Frame Gen details area is now compact, leaving 60% of the content height for the game-card library.
- The separate **Refresh Artwork** action is gone. Steam artwork is resolved and cached automatically from the official Steam CDN.
- Local games with no Steam artwork receive an automatically generated cover based on the executable icon and game title.

> No Windows application can automatically find every portable game with no shared manifest. Known platforms are scanned automatically; use **Scan Folder** for other locations and always verify the executable path in the results screen.
