# Security and safe use / Güvenlik ve güvenli kullanım

## Türkçe

### Anti-cheat ve çevrimiçi oyunlar

DLSS MFG Manager ile yönetilen proxy DLL dosyalarını anti-cheat bulunan veya çevrimiçi oyunlarda kullanmayın. Bir dosyanın teknik olarak yüklenebilmesi güvenli veya izinli olduğu anlamına gelmez. Hesap yaptırımı ve kalıcı ban riski vardır.

### İndirme güvenliği

- Yöneticiyi yalnızca bu deponun GitHub Releases bölümünden indirin.
- Release içindeki `SHA256SUMS.txt` ile EXE hash değerini karşılaştırın.
- `dlssg_for_sm86` dosyalarını yalnızca upstream deposundan alın.
- Kaynağı belirsiz yeniden paketleri çalıştırmayın.
- Hash uyuşmuyorsa dosyayı çalıştırmayın.

### Oyun dosyaları

- Kurulum, güncelleme ve kaldırma sırasında oyun ile launcher kapalı olmalıdır.
- Oyun klasöründe önceden bulunan `version.dll` dosyasının hangi araca ait olduğunu kontrol edin.
- Uygulama yedek alsa da kritik oyun/mod klasörlerinin harici yedeğini tutun.
- Bir sorun halinde önce kırmızı **Frame Gen Dosyalarını Sil** düğmesini kullanın, ardından launcher üzerinden oyun dosyalarını doğrulayın.

### Güvenlik sorunu bildirme

Bir güvenlik açığı bildirirken GitHub Issues alanına kişisel bilgi, hesap bilgisi, erişim anahtarı, özel oyun dosyası veya tam yerel kullanıcı yolu eklemeyin. Raporlarda uygulama sürümünü, Windows sürümünü ve kişisel bilgi içermeyen yeniden üretme adımlarını paylaşın.

## English

### Anti-cheat and online games

Do not use proxy DLL files managed by DLSS MFG Manager in games with anti-cheat or online services. A file being technically loadable does not mean its use is safe or permitted. Account penalties and permanent bans are possible.

### Download safety

- Download the manager only from this repository's GitHub Releases section.
- Compare the EXE hash with `SHA256SUMS.txt` from the same release.
- Obtain `dlssg_for_sm86` files only from the upstream repository.
- Do not run repackaged files from unknown sources.
- Never run a file whose hash does not match.

### Game files

- Keep the game and launcher closed during installation, update, and removal.
- Identify any existing `version.dll` in the game directory before replacing it.
- The application creates backups, but you should keep an independent backup of critical game/mod directories.
- If a problem occurs, use the red **Remove Frame Gen Files** button first, then verify game files through the launcher.

### Reporting a security issue

Do not include personal information, account credentials, access tokens, private game files, or complete local user paths in a public GitHub Issue. Reports should include the application version, Windows version, and sanitized reproduction steps.

