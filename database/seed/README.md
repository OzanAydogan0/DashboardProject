# Temiz SQLite Seed

`dashboard.seed.db`, yeni Docker kurulumlarında kullanılan yalnızca şema içeren
başlangıç veritabanıdır. Canlı veya demo kayıt içermez.

Sürüm 1.0.0 doğrulama özeti:

- 13 uygulama tablosu, tümünde 0 satır
- 24 index, 14 trigger ve 4 view
- `PRAGMA integrity_check`: `ok`
- `PRAGMA foreign_key_check`: 0 ihlal
- 444 uzun, şema dışı kaynak değerine karşı kalıntı taraması: 0 eşleşme
- E-posta ve BCrypt hash kalıbı taraması: 0 eşleşme
- SHA-256:
  `A8165F4C37F80430408F9290189E158CBD17E6B817884283A2EC0EEA19052EB8`

Bu dosya çalışma veritabanı olarak doğrudan kullanılmaz. `db-init`, ilk
kurulumda dosyayı kalıcı Docker volume'una kopyalar; API de kullanıcı tablosu
boşsa ilk yöneticiyi yalnızca `BootstrapAdmin` ortam ayarlarından oluşturur.

Canlı `database/dashboard.db` dosyasını bu klasöre kopyalamayın. Yeni seed
üretilirse satır sayıları, bütünlük, hassas veri taraması ve checksum yeniden
doğrulanmalıdır.
