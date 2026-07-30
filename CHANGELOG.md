# Değişiklik Günlüğü

Bu proje [Keep a Changelog](https://keepachangelog.com/tr/1.1.0/) yaklaşımını ve anlamsal sürümlemeyi izler.

## [Unreleased]

- Teslim kabul testi ve müşteri ortamı doğrulaması bekleniyor.

## [1.0.0] - 2026-07-30

### Eklendi

- .NET 10 API için multi-stage, non-root Docker image
- React üretim build'i ve unprivileged Nginx runtime image'ı
- SPA fallback, aynı-origin `/api` proxy ve container sağlık kontrolleri
- Kalıcı SQLite named volume ve yalnız ilk açılışta çalışan seed başlatıcısı
- Yedekleme/geri yükleme için isteğe bağlı `db-tools` servisi
- Secret doğrulamalı Docker Compose üretim yapılandırması
- Push ve pull request'ler için backend, frontend ve container CI kontrolleri
- Teslim image'ları için yüksek/kritik bulguda CI'ı durduran container zafiyet taraması
- Kurulum, işletim, lisans ve teslim kontrol dokümantasyonu
- Satır içermeyen, bütünlük ve checksum kontrolü yapılmış SQLite şema seed'i
- Boş veritabanında geçici secret'larla çalışan ilk sistem yöneticisi bootstrap akışı

### Değiştirildi

- Excel içe/dışa aktarma altyapısı, ticari olmayan EPPlus kullanımından MIT lisanslı ClosedXML'e geçirildi.
- Frontend API erişimi aynı-origin `/api` proxy üzerinden çalışacak şekilde düzenlendi.
- Hassas çalışma veritabanı ve demo SQL dosyaları release/build context'inden çıkarıldı.

### Güvenlik

- API host portundan doğrudan yayınlanmıyor.
- Uygulama container'ları non-root çalışıyor; Linux capability'leri düşürülüyor.
- JWT secret ve QuestPDF üretim lisansı açıkça seçilmeden Compose yapılandırması geçmiyor.
- Nginx güvenlik başlıkları, log rotasyonu ve yalnız gerekli yazılabilir volume/tmp alanları eklendi.
- Yetkilendirme ve proje sahipliği kontrolleri sıkılaştırılarak IDOR türü çapraz proje erişimleri kapatıldı.
- Kullanıcı rolü, durumu veya parolası değiştiğinde eski JWT'lerin geçersiz kalmasını sağlayan token iptal kontrolü eklendi.
- Login rate limit'i güvenilir proxy'den alınan gerçek istemci IP'sine göre partition edildi.
- Forwarded header'lar yalnız sabit iç ağ Nginx proxy adresinden kabul ediliyor.
- Güvenilir çoklu proxy zincirleri için doğrulamalı, yapılandırılabilir forwarded-header hop sınırı eklendi.
- Son aktif sistem yöneticisinin pasifleştirilmesi, rolünün düşürülmesi veya silinmesi engellendi.
- İlk yönetici ve kullanıcı parola işlemlerinde güçlü parola politikası uygulandı.
- JWT/CORS/connection string/lisans ayarları production ortamında fail-fast doğrulanıyor.

### Kaldırıldı

- Backend karşılığı olmayan ve kullanıcıya yanlış başarı bildirimi veren sahte parola sıfırlama akışı kaldırıldı.
