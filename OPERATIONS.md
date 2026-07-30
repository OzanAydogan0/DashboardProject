# İşletim Rehberi

## Günlük durum

```text
docker compose ps
docker compose logs --tail 200 api frontend
curl --fail http://localhost:8080/healthz
curl --fail http://localhost:8080/api/health
```

Canlı log takibi:

```text
docker compose logs --follow --tail 100 api frontend
```

API host'a doğrudan publish edilmez. İstemci IP/protokol başlıkları yalnız Compose ağındaki sabit `FRONTEND_INTERNAL_IP` adresinden kabul edilir. Subnet/IP değiştirilecekse `.env` içindeki `APP_NETWORK_SUBNET` ve `FRONTEND_INTERNAL_IP` birlikte güncellenmeli, ardından container'lar yeniden oluşturulmalıdır.

Compose, container loglarını dosya başına 10 MB ve en fazla 5 dosya ile sınırlar. Kurumsal ortamda logları merkezi log sistemine ayrıca yönlendirin.

## Başlatma ve durdurma

```text
docker compose stop
docker compose start
```

Yapılandırmayı yeniden uygulamak için:

```text
docker compose up -d --wait
```

Normal kapatma:

```text
docker compose down
```

> `docker compose down --volumes` veya `docker compose down -v` veritabanını kalıcı olarak siler. Doğrulanmış yedek ve açık değişiklik onayı olmadan kullanmayın.

## SQLite yedeği

Tutarlı dosya yedeği için önce API'yi durdurun. Frontend açık kalabilir ancak bu sırada API istekleri başarısız olur.

PowerShell:

```powershell
New-Item -ItemType Directory -Force backups | Out-Null
$stamp = Get-Date -Format "yyyyMMdd-HHmmss"
docker compose stop api
docker compose run --rm --no-deps db-tools sh -ec "cp /data/dashboard.db /backups/dashboard-$stamp.db"
docker compose up -d --wait api
```

Bash:

```bash
mkdir -p backups
stamp="$(date +%Y%m%d-%H%M%S)"
docker compose stop api
docker compose run --rm --no-deps db-tools sh -ec "cp /data/dashboard.db /backups/dashboard-$stamp.db"
docker compose up -d --wait api
```

Yedeğin sıfırdan büyük olduğunu ve ayrı, şifreli bir depoya kopyalandığını doğrulayın. En az bir geri yükleme tatbikatı yapılmamış yedek planı tamamlanmış sayılmaz.

Önerilen asgari politika:

- Günlük yedek: 14 gün
- Haftalık yedek: 8 hafta
- Aylık yedek: 12 ay
- Müşteri politikasına göre şifreleme, erişim kaydı ve farklı lokasyon

## Geri yükleme

Aşağıdaki örnekte dosya adını gerçek yedek adıyla değiştirin:

```text
docker compose stop api
docker compose run --rm --no-deps db-tools sh -ec 'test -s /backups/dashboard-YYYYMMDD-HHMMSS.db; rm -f /data/dashboard.db-wal /data/dashboard.db-shm; cp /backups/dashboard-YYYYMMDD-HHMMSS.db /data/dashboard.db; chown "$SQLITE_UID:$SQLITE_GID" /data/dashboard.db; chmod 0600 /data/dashboard.db'
docker compose up -d --wait api
docker compose ps
curl --fail http://localhost:8080/api/health
```

Geri yüklemeden önce mevcut veritabanının ayrıca acil yedeğini alın. Uygulama sürümü ile veritabanı şema sürümünün uyumlu olduğunu doğrulayın.

## Sürüm güncelleme

1. [CHANGELOG.md](CHANGELOG.md) ve varsa şema güncelleme notlarını okuyun.
2. Veritabanı yedeği alın ve yedeği doğrulayın.
3. Yeni, imzalı Git etiketi veya teslim arşivine geçin.
4. Yapılandırmayı ve image'ları doğrulayın.

```text
docker compose config --quiet
docker compose build --pull
docker compose up -d --wait
docker compose ps
```

Named volume güncellemede korunur. `db-init`, dolu volume'u seed ile değiştirmez. Şema değişikliği varsa yalnız ilgili sürümün belgelenmiş migration adımlarını uygulayın.

Geri dönüş için önce eski uygulama image/tag'ine, ardından gerekliyse o sürümle uyumlu veritabanı yedeğine dönün.

## Secret ve oturum yönetimi

`JWT_SECRET` değiştirildiğinde mevcut tüm oturum token'ları geçersiz olur. Planlı rotasyonda:

1. Bakım penceresini duyurun.
2. Yeni rastgele secret üretin.
3. `.env` içindeki değeri değiştirin.
4. `docker compose up -d --wait --force-recreate api` çalıştırın.
5. Giriş ve yetki testlerini tekrarlayın.

Secret'ları yedek dosyasına, loglara veya destek kayıtlarına yazmayın.

`BOOTSTRAP_ADMIN_*` değerleri yalnız `users` tablosu boşken hesap oluşturur. Kullanıcı bulunan bir veritabanında başlangıç hesabını veya parolasını değiştirmez. İlk yönetici oluşturulup giriş ve parola değişikliği doğrulandıktan sonra üç bootstrap değerini `.env` dosyasından silin ve `docker compose up -d --wait --force-recreate api frontend` çalıştırın. Düz metin bootstrap parolasını süresiz saklamayın.

Volume silinip temiz kurulum yapılacaksa yeni ve bağımsız bootstrap değerlerini geçici olarak yeniden sağlayın. Boş veritabanı eksik veya zayıf bootstrap ayarlarıyla başlamaz.

## Seed'e sıfırlama

Bu işlem volume'daki tüm canlı veriyi siler ve bir sonraki açılışta şema-only `database/seed/dashboard.seed.db` dosyasını yeniden kopyalar:

```text
docker compose down --volumes
docker compose up -d --wait
```

Bu komut yalnız açık veri silme onayı, doğrulanmış yedek ve doğru proje/volume kontrolünden sonra kullanılmalıdır. Temiz seed'in checksum'ını ve satır içermediğini, ayrıca ilk yönetici bootstrap ayarlarının hazır olduğunu yeniden doğrulayın.
