# Proje Yönetim Dashboard

React tabanlı kullanıcı arayüzü, .NET 10 API ve SQLite veritabanından oluşan proje yönetim uygulaması. Teslim paketi Docker Compose ile tek giriş noktası üzerinden çalışır:

- `frontend`: Nginx üzerinde statik React uygulaması; dış dünyaya açılan tek servis.
- `api`: .NET 10 API; yalnız Compose iç ağına açıktır.
- `db-init`: İlk kurulumda teslim veritabanını kalıcı Docker volume'una alır.
- `db-tools`: Yedekleme ve geri yükleme işlemleri için isteğe bağlı araç servisi.

## Hızlı başlangıç

Gereksinimler:

- Docker Engine veya Docker Desktop
- Docker Compose v2 (`docker compose`)
- En az 2 GB boş bellek ve 2 GB boş disk alanı

PowerShell:

```powershell
Copy-Item .env.example .env
$jwtSecret = [Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(48))
$adminPassword = "$([Convert]::ToBase64String([Security.Cryptography.RandomNumberGenerator]::GetBytes(36)))Aa1!"
$jwtSecret
$adminPassword
```

Bash:

```bash
cp .env.example .env
openssl rand -base64 48
printf '%sAa1!\n' "$(openssl rand -base64 36 | tr -d '\n')"
```

Üretilen değeri `.env` içindeki boş `JWT_SECRET` alanına yazın ve ilk yönetici için bağımsız, güçlü bir `BOOTSTRAP_ADMIN_PASSWORD` üretin. `BOOTSTRAP_ADMIN_EMAIL` ile `BOOTSTRAP_ADMIN_FULL_NAME` alanlarını doldurun. Kurumunuzun QuestPDF lisans uygunluğunu doğrulayıp `QUESTPDF_LICENSE` alanını yalnız `Community`, `Professional` veya `Enterprise` değerlerinden biriyle doldurun. Ardından:

```text
docker compose config --quiet
docker compose build --pull
docker compose up -d --wait
```

Uygulama varsayılan olarak `http://localhost:8080` adresindedir. Sağlık kontrolleri:

```text
docker compose ps
curl http://localhost:8080/healthz
curl http://localhost:8080/api/health
```

> `.env` dosyasını, JWT anahtarını, müşteri verisini veya yönetici parolasını Git'e eklemeyin. `docker compose config` çıktısı secret değerlerini düz metin gösterebilir; bu çıktıyı paylaşmayın.

## Teslim arşivi ve Git geçmişi

Çalışma dizinini `Compress-Archive *`, Dosya Gezgini veya benzeri bir yöntemle doğrudan paketlemeyin. Ignore edilmiş eski `database/dashboard.db` ve `database/SQLiteQuery*.sql` dosyaları diskte kalabilir ve böyle bir ZIP'e sızabilir.

Mevcut public Git geçmişinde çalışma veritabanı ve eski JWT secret bulunduğu kabul edilmelidir. Bu nedenle:

- Eski JWT secret **teslimden önce rotate edilmeli** ve artık hiçbir ortamda geçerli olmamalıdır.
- Veri/kişisel veri sızıntısı için yetkili güvenlik ve gizlilik değerlendirmesi yapılmalıdır.
- Müşteriye `.git` geçmişi verilmemelidir.
- Teslim tercihen onaylı release commit/tag'inden history'siz `git archive` veya bu temiz snapshot'tan açılan yeni private repo ile yapılmalıdır.
- Geçmiş korunacaksa history rewrite/force-push ancak açık yetki, fork/clone/cache ve erişim etkisi değerlendirmesiyle yapılmalıdır. Public repoyu sonradan private yapmak ya da geçmişi rewrite etmek önceden alınmış kopyaları geri çağırmaz.

Önerilen history'siz paket:

```text
git status --short
git archive --format=zip --output DashboardProject-v1.0.0.zip v1.0.0
```

İmzalı release etiketi oluşturulmadan önce denetimli bir teslim adayı üretmek
için:

```powershell
powershell -NoProfile -ExecutionPolicy Bypass -File scripts\New-DeliveryArchive.ps1
```

Bu komut Git tarafından izlenen ve ignore edilmeyen mevcut dosyalardan
`release/DashboardProject-v1.0.0-source.zip` üretir; yerel DB, `.env`, demo SQL,
build çıktısı ve Git geçmişi pakete girerse işlemi durdurur. Aynı klasörde
`SHA256SUMS.txt` oluşturur. Bu çalışma ağacı snapshot'ı, onaylı release
commit'i ve imzalı etiketin yerine geçmez; son kabul için etiketten yeniden
arşiv alınmalıdır.

PowerShell ile içerik ve hash kontrolü:

```powershell
$entries = tar -tf DashboardProject-v1.0.0.zip
$forbidden = $entries | Select-String -Pattern '(^|/)(\.git/|\.env$|database/dashboard\.db$|database/SQLiteQuery[^/]*\.sql$|backups/.*\.(db|sqlite|zip|bak|tar|gz)$|node_modules/|bin/|obj/|dist/)'
if ($forbidden) { throw "Teslim arşivinde yasaklı dosya bulundu: $forbidden" }
Get-FileHash DashboardProject-v1.0.0.zip -Algorithm SHA256
```

Bash ile:

```bash
entries="$(unzip -Z1 DashboardProject-v1.0.0.zip)"
if printf '%s\n' "$entries" | grep -E '(^|/)(\.git/|\.env$|database/dashboard\.db$|database/SQLiteQuery[^/]*\.sql$|backups/.*\.(db|sqlite|zip|bak|tar|gz)$|node_modules/|bin/|obj/|dist/)'; then
  echo "Teslim arşivinde yasaklı dosya bulundu." >&2
  exit 1
fi
sha256sum DashboardProject-v1.0.0.zip
```

Arşiv SHA-256 değerini, release commit/tag kimliğini ve içerik taraması sonucunu teslim tutanağına yazın.

## Veritabanı davranışı

`database/seed/dashboard.seed.db` yalnız şema, view, index ve trigger yapılarını içeren temiz başlangıç veritabanıdır. `db-init`, dosyanın SHA-256 değerini doğrular ve yalnız ilk kurulumda `dashboard-project-data` adlı kalıcı volume'a kopyalar. Volume doluysa sonraki açılışlarda seed çalışan veritabanının üzerine yazılmaz.

API, `users` tablosu boşken ilk yöneticiyi yalnız `BOOTSTRAP_ADMIN_EMAIL`, `BOOTSTRAP_ADMIN_FULL_NAME` ve `BOOTSTRAP_ADMIN_PASSWORD` değerlerinden oluşturur. Herhangi bir kullanıcı varsa bootstrap atlanır; pakette sabit veya varsayılan parola yoktur.

İlk giriş ve parola değişikliği tamamlanınca üç `BOOTSTRAP_ADMIN_*` değerini `.env` dosyasından silip API'yi yeniden oluşturun. Mevcut kullanıcı bulunan veritabanı bu değerler olmadan açılır.

Eski `database/dashboard.db` çalışma verisi kullanıcı, parola hash'i, proje ve denetim kayıtları içerdiği için image'a veya yeni kurulum seed'ine alınmaz. Bu dosya gerekiyorsa yalnız veri sahibi onayı, sanitizasyon ve ayrı güvenli aktarım/migration süreciyle ele alınmalıdır. Ayrıntılar [DELIVERY_CHECKLIST.md](DELIVERY_CHECKLIST.md) içindedir.

> `docker compose down --volumes` çalışan veritabanı volume'unu kalıcı olarak siler. Sonraki açılış temiz seed'den başlar. Normal durdurma için `docker compose down` kullanın.

## Dokümantasyon

- [INSTALLATION.md](INSTALLATION.md): sıfırdan kurulum, üretim ve doğrulama
- [OPERATIONS.md](OPERATIONS.md): yedekleme, geri yükleme, loglar ve güncelleme
- [DELIVERY_CHECKLIST.md](DELIVERY_CHECKLIST.md): teslim öncesi zorunlu kontroller
- [THIRD_PARTY_NOTICES.md](THIRD_PARTY_NOTICES.md): doğrudan bağımlılıklar ve lisans notları
- [CHANGELOG.md](CHANGELOG.md): sürüm değişiklikleri

## Yerel geliştirme

Backend:

```text
dotnet restore backend/DashboardApi.Tests/DashboardApi.Tests.csproj
dotnet test backend/DashboardApi.Tests/DashboardApi.Tests.csproj -c Release
```

Frontend:

```text
cd frontend
npm ci
npm run lint
npm test
npm run build
```

Container doğrulaması:

```text
docker compose config --quiet
docker compose build
```
