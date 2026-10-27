# LinkGuard

Outlook'ta bir bağlantıya tıklandığında, tarayıcı açılmadan önce gidilecek adresi gösterip onay isteyen küçük bir Windows aracı. Amaç phishing bağlantılarını açmadan fark etmek.

## Ne yapar

- Gidilecek alan adını büyük, tam adresi altında gösterir.
- **Microsoft Safe Links** (`*.safelinks.protection.outlook.com`) sarmalını açıp gerçek hedefi gösterir.
- Şüpheli durumlarda kırmızı uyarı verir:
  - `https` değil, `http`
  - punycode / Latin dışı karakterli (benzer görünen) alan adı
  - alan adı yerine çıplak IP adresi
  - `kullanici@gercek-site` biçiminde `@` hilesi
- Varsayılan düğme **İptal**'dir; yanlışlıkla Enter'a basmak bağlantıyı açmaz.
- Onay penceresi yalnızca bağlantı **Outlook'tan** (`OUTLOOK.EXE` / yeni Outlook `olk.exe`) geldiğinde çıkar. Diğer uygulamalardan gelen bağlantılar doğrudan tarayıcıya gider.
- Onaylanan bağlantı Chrome'da açılır (Chrome yoksa Edge).

## Nasıl çalışır

Windows'a kullanıcı düzeyinde (HKCU) bir "tarayıcı" olarak kaydolur. http/https için varsayılan uygulama olarak seçildiğinde tüm bağlantılar önce LinkGuard'a gelir. LinkGuard çağıran işlemi kontrol eder: Outlook'sa onay sorar, değilse bağlantıyı gerçek tarayıcıya iletir.

Yönetici yetkisi ve ek yazılım gerekmez. .NET Framework 4.x ile birlikte gelen `csc.exe` ile kurulum sırasında derlenir.

## Kurulum

```powershell
git clone https://github.com/alperyardimci/linkguard.git
cd linkguard
powershell -ExecutionPolicy Bypass -File .\install.ps1
```

Ardından:

1. Açılan **Ayarlar → Varsayılan uygulamalar → LinkGuard** sayfasında **HTTP** ve **HTTPS** için LinkGuard'ı seçin. (Windows bu ayarın programla değiştirilmesine izin vermez.)
2. Outlook'ta **Dosya → Seçenekler → Gelişmiş → "Outlook'taki köprüleri şurada aç"** ayarını **Varsayılan tarayıcı** yapın. Ayar "Microsoft Edge" kalırsa Outlook LinkGuard'ı atlar.

## Kaldırma

Önce varsayılan tarayıcıyı tekrar Chrome/Edge yapın, sonra:

```powershell
powershell -ExecutionPolicy Bypass -File .\uninstall.ps1
```

## Notlar

- Kurumsal cihazlarda varsayılan tarayıcı grup ilkesiyle zorlanıyorsa ayar geri dönebilir; imzasız uygulamalar AppLocker/WDAC ile engellenebilir.
- Bu araç farkındalık katmanıdır. Defender for Office 365 Safe Links gibi sunucu tarafı korumaların yerini tutmaz.
