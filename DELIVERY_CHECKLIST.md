# Teslim Kontrol Listesi

İşaretlenmemiş zorunlu madde varken üretim teslimi tamamlanmış kabul edilmemelidir.

## Sürüm ve kalite

- [ ] Teslim commit'i onaylandı; çalışma ağacında açıklanmamış değişiklik yok.
- [ ] Commit için imzalı veya korumalı `v1.0.0` etiketi oluşturuldu.
- [ ] Teslim paketi çalışma klasöründen ZIP'lenmedi; onaylı tag'den `git archive` ile history'siz üretildi.
- [ ] Etiket öncesi teslim adayı kullanıldıysa `scripts/New-DeliveryArchive.ps1` ile üretildi ve bunun imzalı release yerine geçmediği kayda alındı.
- [ ] Arşiv içeriği `.git`, `.env`, eski DB/SQL, backup, `node_modules`, `bin`, `obj` ve `dist` için tarandı.
- [ ] Arşiv SHA-256 değeri teslim tutanağına yazıldı ve alıcı tarafından doğrulandı.
- [ ] GitHub Actions backend test, frontend lint/build ve container build adımları yeşil.
- [ ] `docker compose config --quiet` ve `docker compose build --pull` temiz makinede başarılı.
- [ ] Temiz volume ile `docker compose up -d --wait` başarılı.
- [ ] Giriş, roller, proje, risk, aksiyon, Excel ve PDF kabul senaryoları çalıştırıldı.
- [ ] Bilinen sınırlamalar ve kabul edilen riskler teslim tutanağına yazıldı.

## Veri

- [ ] Public Git geçmişindeki eski çalışma DB'si için güvenlik/gizlilik ve kişisel veri olay değerlendirmesi tamamlandı.
- [ ] `database/seed/dashboard.seed.db` içindeki tüm uygulama tablolarının satır sayısı sıfır ve şema/view/index/trigger bütünlüğü doğrulandı.
- [ ] Temiz seed kişisel veri, parola hash'i, token ve müşteri metni için tarandı; sonuç teslim kanıtına eklendi.
- [ ] Seed SHA-256 değeri `.env`, release kaydı ve teslim dosyasıyla eşleşiyor.
- [ ] Eski `database/dashboard.db` Docker build context'i, image, release arşivi ve normal kurulum akışının dışında bırakıldı.
- [ ] Demo/gizli INSERT içerebilen `database/SQLiteQuery*.sql` çalışma dosyaları release arşivi ve image dışında bırakıldı.
- [ ] Eski çalışma verisi aktarılacaksa veri sahibi sınıflandırması, sanitizasyon, hukuki/iş onayı ve ayrı güvenli migration planı tamamlandı.
- [ ] Varsayılan/test hesapları kaldırıldı veya parolaları güvenli kanaldan teslim edilip ilk girişte değiştirildi.
- [ ] Seed, varsa migration girdisi, dışa aktarımlar ve loglar secret/token/parola açısından kontrol edildi.
- [ ] İlk üretim yedeği alındı; farklı ve şifreli konuma kopyalandı.
- [ ] Geri yükleme tatbikatı yapıldı ve süre/sonuç kaydedildi.
- [ ] `docker compose down --volumes` veri kaybı riski işletim ekibine anlatıldı.

## Güvenlik ve işletim

- [ ] Public geçmişte bulunan eski JWT secret rotate edildi; hiçbir ortamda geçerli olmadığı doğrulandı.
- [ ] Müşteriye `.git` geçmişi verilmedi; history'siz snapshot veya yeni private repo kullanıldı.
- [ ] Geçmiş rewrite edildiyse açık yetki, force-push etkisi, fork/clone/cache ve erişim değerlendirmesi kayda alındı.
- [ ] Her ortam için benzersiz, kriptografik rastgele `JWT_SECRET` üretildi.
- [ ] İlk yönetici e-posta/ad/parolası sabit değer olmadan `BOOTSTRAP_ADMIN_*` secret'larıyla sağlandı.
- [ ] Temiz kurulumda ilk yönetici oluşturma, giriş ve hemen parola değiştirme akışı doğrulandı.
- [ ] İlk girişten sonra üç `BOOTSTRAP_ADMIN_*` değeri `.env` dosyasından silindi; mevcut kullanıcı DB'sinin bunlar olmadan yeniden başladığı doğrulandı.
- [ ] `.env` Git dışında ve yalnız yetkili işletim hesabınca okunabilir.
- [ ] Uygulama HTTPS arkasında; dışarı yalnız gerekli portlar açık.
- [ ] `CORS_ALLOWED_ORIGINS` gerçek üretim origin'i ile sınırlandırıldı.
- [ ] Yönetici erişimi ve ayrılan personel hesap kapatma süreci test edildi.
- [ ] Zafiyet taraması ve secret taraması yapıldı; yüksek/kritik bulgular kapatıldı veya yazılı kabul edildi.
- [ ] Merkezi log, disk kapasitesi, sağlık kontrolü ve alarm sahipleri tanımlandı.
- [ ] Yedek saklama, secret rotasyonu, güncelleme ve rollback sorumluları belirlendi.

## Lisans ve hukuk

- [ ] `THIRD_PARTY_NOTICES.md` ile kilit dosyaları güncel sürümlere göre karşılaştırıldı.
- [ ] QuestPDF Community uygunluğu yazılı doğrulandı veya Professional/Enterprise lisans kanıtı alındı.
- [ ] `QUESTPDF_LICENSE` yalnız doğrulanan üretim lisansına göre ayarlandı; `Evaluation` kullanılmıyor.
- [ ] Müşteriye kaynak kod, üçüncü taraf bildirimleri ve gerekli lisans metinleri teslim edildi.
- [ ] Marka, görsel, örnek veri ve diğer içeriklerin kullanım hakkı doğrulandı.

## Devir ve kabul

- [ ] Kurulum URL'si, sürüm, image digest'leri, sunucu ve volume adı kaydedildi.
- [ ] Yönetici hesabı/parolası dokümandan ayrı güvenli kanalla devredildi.
- [ ] Kurulum, işletim ve acil durum rehberleri müşteri teknik ekibiyle gözden geçirildi.
- [ ] Destek kapsamı, SLA, garanti süresi ve değişiklik talebi süreci yazılı hale getirildi.
- [ ] Müşteri kullanıcı kabul testi ve teslim tutanağını imzaladı.
