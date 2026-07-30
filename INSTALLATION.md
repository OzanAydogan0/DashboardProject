# Kurulum

Bu belge Docker Compose ile yeni bir sunucuya üretim kurulumu içindir.

## 1. Ön koşullar

- 64-bit Linux sunucu veya Docker Desktop çalıştırabilen Windows/macOS
- Güncel Docker Engine ve Docker Compose v2
- Uygulamaya ayrılmış kalıcı disk alanı
- Üretimde HTTPS sağlayan bir reverse proxy veya yük dengeleyici
- Satır içermediği doğrulanmış `database/seed/dashboard.seed.db`
- QuestPDF için uygun üretim lisansı/uygunluk teyidi

Sunucuda doğrulayın:

```text
docker version
docker compose version
```

## 2. Paketi yerleştirme

Projeyi yalnız yetkili işletim kullanıcısının okuyabildiği bir dizine çıkarın. Şu dosyaların bulunduğunu doğrulayın:

```text
compose.yaml
.env.example
backend/dashboardapi/Dockerfile
frontend/Dockerfile
frontend/nginx.conf
database/seed/dashboard.seed.db
```

`database/seed/dashboard.seed.db` yalnız şema yapısını içeren temiz teslim seed'idir. Satır sayılarının sıfır ve kişisel/gizli metin taramasının temiz olduğuna dair teslim kanıtını kontrol edin. Bu sürüm için beklenen SHA-256:

```text
A8165F4C37F80430408F9290189E158CBD17E6B817884283A2EC0EEA19052EB8
```

PowerShell'de `Get-FileHash database/seed/dashboard.seed.db -Algorithm SHA256`, Bash'te `sha256sum database/seed/dashboard.seed.db` ile doğrulayın. `db-init` de kopyalamadan önce `.env` içindeki `SEED_SHA256` ile otomatik karşılaştırır.

Eski `database/dashboard.db` gerçek çalışma verisi, kullanıcı kayıtları, parola hash'leri ve denetim kayıtları içerebilir. Bu dosya Docker seed'i değildir; image'a eklenmemeli veya müşteri ortamına normal kurulumla kopyalanmamalıdır. Gerekliyse ayrı bir veri sınıflandırma, sanitizasyon, yetkili aktarım ve migration planı uygulanmalıdır.

## 3. Yapılandırma ve secret

`.env.example` dosyasını `.env` adıyla kopyalayın.

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

`.env` içinde:

- `JWT_SECRET`: Yukarıda üretilen rastgele değer. Eski veya örnek anahtar kullanmayın.
- `BOOTSTRAP_ADMIN_EMAIL`: İlk sistem yöneticisinin kurumsal e-posta adresi.
- `BOOTSTRAP_ADMIN_FULL_NAME`: İlk sistem yöneticisinin adı.
- `BOOTSTRAP_ADMIN_PASSWORD`: JWT secret'tan farklı, en az 12 karakter; büyük/küçük harf, rakam ve özel karakter içeren parola.
- `QUESTPDF_LICENSE`: Yalnız `Community`, `Professional` veya `Enterprise`. Seçimden önce müşteri uygunluğunu ya da satın alınan lisansı doğrulayın.
- `ASPNETCORE_ENVIRONMENT`: Teslim ortamında `Production` olarak kalmalıdır; lisans veya güvenlik kontrollerini aşmak için değiştirmeyin.
- `CORS_ALLOWED_ORIGINS`: Dışarıdan erişilen tam origin; örneğin `https://dashboard.example.com`.
- `APP_BIND_ADDRESS`: Reverse proxy aynı makinedeyse tercihen `127.0.0.1`, doğrudan erişim gerekiyorsa kontrollü biçimde `0.0.0.0`.
- `APP_PORT`: Host üzerinde kullanılacak port.
- `DATA_VOLUME_NAME`: Aynı sunucudaki diğer kurulumlarla çakışmayacak volume adı.
- `APP_NETWORK_SUBNET`: Mevcut Docker ağlarıyla çakışmayan özel subnet.
- `FRONTEND_INTERNAL_IP`: Bu subnet içindeki sabit Nginx adresi.
- `REVERSE_PROXY_KNOWN_PROXIES`: API'nin forwarded header kabul edeceği IP listesi. İlk değer `FRONTEND_INTERNAL_IP` ile aynı kalmalıdır.
- `REVERSE_PROXY_FORWARD_LIMIT`: Doğrudan Compose frontend erişiminde `1` kalmalıdır. Ek güvenilir TLS proxy zinciri için aşağıdaki ağ bölümüne bakın.

Linux üzerinde `.env` erişimini sınırlandırın:

```bash
chmod 600 .env
```

`.env` veya `docker compose config` çıktısını destek kaydına, e-postaya ya da Git'e eklemeyin.

## 4. Statik kontrol ve image oluşturma

```text
docker compose config --quiet
docker compose build --pull
```

İlk komut boş zorunlu değerlerde hata verir. İkinci komut API ve frontend image'larını yerel olarak oluşturur.

## 5. İlk çalıştırma

```text
docker compose up -d --wait
docker compose ps
```

İlk açılışta `db-init`:

1. `dashboard-project-data` volume'unu kontrol eder.
2. Volume'da `dashboard.db` yoksa `database/seed/dashboard.seed.db` temiz seed'ini kopyalar.
3. Dosya sahipliğini non-root API kullanıcısına verir.
4. Volume doluysa mevcut veriyi değiştirmez.

API ilk açılışta `users` tablosu boşsa `.env` içindeki üç `BOOTSTRAP_ADMIN_*` değeriyle ilk sistem yöneticisini oluşturur. Herhangi bir kullanıcı varsa bu işlem atlanır. Boş veritabanında üç değerden biri eksik veya parola politikaya aykırıysa API güvenli biçimde başlamaz.

İlk girişten hemen sonra parolayı kurumsal parola politikasına göre değiştirin. Ardından üç `BOOTSTRAP_ADMIN_*` satırındaki değerleri `.env` dosyasından temizleyip container'ları yeniden oluşturun:

```text
docker compose up -d --wait --force-recreate api frontend
docker compose ps
```

Mevcut kullanıcı bulunan veritabanı boş bootstrap ayarlarıyla açılmalıdır. Bootstrap değerlerini log veya teslim belgesine yazmayın.

Başlangıç loglarını kontrol edin:

```text
docker compose logs db-init
docker compose logs --tail 200 api frontend
```

## 6. Kabul kontrolü

```text
curl --fail http://localhost:8080/healthz
curl --fail http://localhost:8080/api/health
```

Tarayıcıdan giriş, yetki seviyeleri, proje listeleme, Excel içe/dışa aktarma ve PDF üretme senaryolarını müşteri kabul hesabıyla doğrulayın. Teslim parolalarını kaynak kod veya doküman içinde paylaşmayın; güvenli ve ayrı bir kanal kullanın.

## 7. HTTPS ve ağ

Container paketi HTTP/8080 sunar. Üretimde TLS'yi kurumsal reverse proxy veya yük dengeleyicide sonlandırın:

- Dışarı yalnız 443/TCP açın.
- `APP_BIND_ADDRESS=127.0.0.1` kullanabiliyorsanız host portunu yerel arayüzle sınırlayın.
- Reverse proxy'de istemci IP'si, host ve protokol başlıklarını iletin.
- `FRONTEND_INTERNAL_IP` ile Compose network IP'sini birlikte tutun; API portunu host'a publish etmeyin.
- `CORS_ALLOWED_ORIGINS` değerini gerçek `https://...` origin ile güncelleyin.
- Sertifika yenilemesini ve güvenlik başlıklarını kabul testine dahil edin.

API login rate limit'i gerçek istemci IP'sine göre uygular. İstemci ile Compose
frontend arasına tek bir kurumsal TLS proxy eklenirse o proxy, dışarıdan gelen
`X-Forwarded-For` değerini kabul etmemeli; header'ı bağlantının gerçek istemci
IP'siyle yeniden oluşturmalıdır. Ardından proxy'nin iç IP adresini güvenilir
listeye ekleyin ve iki forwarded değerinin işlenmesine izin verin:

```dotenv
REVERSE_PROXY_KNOWN_PROXIES=172.30.0.3;10.20.30.40
REVERSE_PROXY_FORWARD_LIMIT=2
```

Buradaki `10.20.30.40` örneğini kurumsal proxy'nin gerçek iç IP'siyle
değiştirin. Daha uzun proxy zincirlerinde IP sırası ve hop sayısı ağ ekibi
tarafından doğrulanmadan limiti artırmayın. Güvenilmeyen adres eklemek veya tüm
proxy'lere güvenmek, istemcinin rate limit'i sahte header ile aşmasına neden
olabilir.

Yapılandırma değişikliğinden sonra:

```text
docker compose up -d --wait --force-recreate
docker compose ps
```

## 8. Teslim kaydı

Kurulan Git etiketi/image sürümü, kurulum tarihi, sunucu adı, volume adı, yedek konumu ve kabul testi sonucunu teslim tutanağına yazın. [DELIVERY_CHECKLIST.md](DELIVERY_CHECKLIST.md) tamamlanmadan üretim teslimini kapatmayın.
