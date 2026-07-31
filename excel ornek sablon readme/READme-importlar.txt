# Excel ile Toplu Veri İçe Aktarma

Sistemde proje, risk, sorun ve aksiyon kayıtları Excel dosyası kullanılarak toplu olarak oluşturulabilir.

---

## Genel kullanım kuralları

* Dosyanızı .xlsx biçiminde kaydedin. Eski .xls biçimini kullanmayın.
* Sistem yalnızca Excel dosyasındaki ilk çalışma sayfasını okur.
* Birinci satır sütun başlıklarını, ikinci ve sonraki satırlar kayıtları içermelidir.
* Her satır ayrı bir kayıt olarak değerlendirilir.
* Birleştirilmiş hücre kullanmayın.
* Tarihleri Excel tarih hücresi olarak veya 2027-03-15 / 15.03.2027 biçiminde yazın.
* Yüzde alanlarına % işareti olmadan 35 gibi bir sayı yazılması önerilir.
* Risk, sorun ve aksiyon dosyalarında sütunların sırası değişebilir; ancak başlık adları doğru olmalıdır.
* Proje dosyasında sütun sırası sabittir ve değiştirilmemelidir.
* İşlem sonunda eklenen ve hatalı bulunan satırların sayısı gösterilir.
* Kısmi başarı durumunda, başarıyla eklenen satırları dosyadan çıkarın ve yalnızca hatalı satırları düzelterek tekrar yükleyin. Aksi durumda aynı risk, sorun veya aksiyon yeniden oluşturulabilir.

---

## Sorumlu kullanıcı nasıl belirtilir?

Risk, sorun ve aksiyonların Sorumlu alanında aşağıdaki bilgilerden biri kullanılabilir:

* Kullanıcı ID’si
* Kullanıcının e-posta adresi
* Sistemde yalnızca bir kişiyle eşleşen tam adı

**Örnek:**

```text
USR-0042
yonetici@firma.com
Ahmet Yılmaz
```

> Aynı ad ve soyada sahip birden fazla kullanıcı varsa kullanıcı ID’si veya e-posta adresi kullanılmalıdır.

> Sistem Yöneticisi ve Üst Yönetim rolündeki kullanıcılar risk, sorun veya aksiyon sorumlusu olarak atanamaz.

---

## 1. Proje Excel formatı

Proje Excel içe aktarma işlemini yalnızca Sistem Yöneticisi yapabilir.

> Proje dosyası sütunları isimlerine göre değil, Excel’deki konumlarına göre okunur. Bu nedenle aşağıdaki A–P sırası kesinlikle değiştirilmemelidir.

| Sütun | Başlık               | Kullanım                                                                                                     |
| ----- | -------------------- | ------------------------------------------------------------------------------------------------------------ |
| A     | Proje Kodu           | Zorunludur ve benzersiz olmalıdır.                                                                           |
| B     | Proje Adı            | Zorunludur.                                                                                                  |
| C     | Müşteri Adı          | Zorunludur. Müşteri sistemde yoksa otomatik oluşturulur.                                                     |
| D     | PM E-posta           | Zorunludur. Sistemde kayıtlı kullanıcının e-postası veya kullanıcı ID’si yazılmalıdır. Tam ad kabul edilmez. |
| E     | Başlangıç Tarihi     | Geçerli bir tarih yazılması önerilir.                                                                        |
| F     | Bitiş Tarihi         | Başlangıç tarihinden önce olamaz.                                                                            |
| G     | BAC                  | Projenin toplam bütçesidir. Sıfır veya pozitif sayı olmalıdır. Boşsa 0 kullanılır.                           |
| H     | Para Birimi          | TRY, USD veya EUR. Boşsa TRY kullanılır.                                                                     |
| I     | Gizlilik             | Şirket İçi, Hizmete Özel veya Gizli.                                                                         |
| J     | Raporlama Sıklığı    | Haftalık, Aylık veya Üç Aylık. Boşsa Aylık kullanılır.                                                       |
| K     | Durum                | Mutlaka doldurulmalıdır. Geçerli değerler: Taslak, Aktif, Beklemede, Tamamlandı, Pasif.                      |
| L     | Açıklama             | İsteğe bağlıdır.                                                                                             |
| M     | Sağlık               | İyi, Orta, Kritik, Belirsiz. Renk karşılıkları olan Yeşil, Sarı, Kırmızı, Gri de kabul edilir.               |
| N     | Planlanan İlerleme   | 0–100 arasında sayı olmalıdır. Boşsa 0 kullanılır.                                                           |
| O     | Gerçekleşen İlerleme | 0–100 arasında sayı olmalıdır. Boşsa 0 kullanılır.                                                           |
| P     | Aktiflik             | Aktif proje için 1 veya Aktif; pasif proje için 0 veya Pasif.                                                |

### Örnek proje satırı

```text
Proje Kodu	Proje Adı	Müşteri Adı	PM E-posta	Başlangıç Tarihi	Bitiş Tarihi	BAC	Para Birimi	Gizlilik	Raporlama Sıklığı	Durum	Açıklama	Sağlık	Planlanan İlerleme	Gerçekleşen İlerleme	Aktiflik
PRJ-2027-001	Dijital Dönüşüm Projesi	Örnek Müşteri A.Ş.	yonetici@firma.com	2027-01-15	2027-12-31	500000	TRY	Şirket İçi	Aylık	Aktif	Kurumsal dönüşüm projesi	İyi	40	30	1
```

### Proje aktarımıyla ilgili notlar

* Teknik proje ID’si sistem tarafından otomatik üretilir; Excel’e eklenmez.
* Proje kodu sistemde zaten varsa ilgili satır aktarılmaz.
* Aynı dosyada aynı proje kodunu birden fazla kez kullanmayın.
* Program sütunu bulunmaz. Sistem aktif programı kullanır; aktif program yoksa Genel Program oluşturur.
* Müşteri adı sistemde yoksa yeni müşteri otomatik oluşturulur.
* Veri satırlarından sonraki gereksiz veya biçimlendirilmiş boş satırları silin.

---

## 2. Risk Excel formatı

Risk içe aktarma işlemi, proje içinde yazma yetkisi bulunan kullanıcılar tarafından yapılabilir.

Aşağıdaki başlıkların tamamı Excel dosyasında bulunmalıdır:

| Başlık             | Kullanım                                                          |
| ------------------ | ----------------------------------------------------------------- |
| Risk Başlığı       | Zorunludur. Riskin kısa ve anlaşılır adıdır.                      |
| Kategori           | Zorunludur. Örneğin Teknik, Finansal, Takvim veya Kaynak.         |
| Olasılık           | 1–5 arasında tam sayı olmalıdır.                                  |
| Etki               | 1–5 arasında tam sayı olmalıdır.                                  |
| Durum              | Açık, İzleniyor, Azaltıldı veya Kapalı. Boşsa Açık kullanılır.    |
| Azaltım / Müdahale | Başlık zorunludur; hücre boş bırakılırsa Belirtilmedi kaydedilir. |
| Sorumlu            | Kullanıcı ID’si, e-posta veya benzersiz tam ad olabilir.          |
| Bitiş Tarihi       | Geçerli bir tarih olmalıdır.                                      |

### Örnek risk satırı

```text
Risk Başlığı	Kategori	Olasılık	Etki	Durum	Azaltım / Müdahale	Sorumlu	Bitiş Tarihi
Kritik bileşenin geç teslim edilmesi	Takvim	4	5	İzleniyor	Alternatif tedarikçi belirlenecek.	yonetici@firma.com	2027-03-15
```

> Risk ID’si ve risk skoru otomatik oluşturulur. Excel dosyasına ID veya Skor sütunu eklemeniz gerekmez.

Risk skoru şu şekilde hesaplanır:

```text
Risk Skoru = Olasılık × Etki
```

---

## 3. Sorun Excel formatı

Sorun içe aktarma işlemi, proje içinde yazma yetkisi bulunan kullanıcılar tarafından yapılabilir.

| Başlık        | Kullanım                                                 |
| ------------- | -------------------------------------------------------- |
| Sorun Tanımı  | Zorunludur.                                              |
| Öncelik       | Düşük, Orta, Yüksek veya Kritik.                         |
| Etki          | Düşük, Orta, Yüksek veya Kritik.                         |
| Durum         | Açık, Devam Ediyor, Çözüldü veya Kapalı.                 |
| Sorumlu       | Kullanıcı ID’si, e-posta veya benzersiz tam ad olabilir. |
| Hedef Tarihi  | Geçerli bir tarih olmalıdır.                             |
| Kök Neden     | İsteğe bağlıdır.                                         |
| Çözüm         | İsteğe bağlıdır.                                         |
| Bağlı Risk ID | İsteğe bağlıdır. Aynı projeye ait risk ID’si olmalıdır.  |

### Örnek sorun satırı

```text
Sorun Tanımı	Öncelik	Etki	Durum	Sorumlu	Hedef Tarihi	Kök Neden	Çözüm	Bağlı Risk ID
Entegrasyon ortamında kapasite yetersizliği	Yüksek	Kritik	Devam Ediyor	yonetici@firma.com	2027-02-15	Sunucu kaynakları yetersiz kaldı.	Ek kapasite devreye alınacak.	RSK-0001
```

### Sorun aktarımıyla ilgili notlar

* Sorun ID’si sistem tarafından otomatik oluşturulur.
* Bağlı Risk ID kullanılırsa riskin seçili projeye ait olması gerekir.
* Başka projeye ait veya bulunamayan risk ID’si kullanılan satır aktarılmaz.
* Bir riske birden fazla sorun bağlanabilir.

---

## 4. Aksiyon Excel formatı

Aksiyon içe aktarma işlemi, proje içinde yazma yetkisi bulunan kullanıcılar tarafından yapılabilir.

| Başlık          | Kullanım                                                     |
| --------------- | ------------------------------------------------------------ |
| Aksiyon Tanımı  | Zorunludur. Yapılacak işin kısa ve açık açıklamasıdır.       |
| Kaynak Türü     | Risk, Sorun, Kilometre Taşı, PIR, Yönetim Kararı veya Diğer. |
| Kaynak Referans | İsteğe bağlı bir referans numarası veya açıklamadır.         |
| Öncelik         | Düşük, Orta, Yüksek veya Kritik.                             |
| Durum           | Açık, Devam Ediyor, Tamamlandı veya İptal.                   |
| İlerleme %      | 0–100 arasında sayı olmalıdır.                               |
| Sorumlu         | Kullanıcı ID’si, e-posta veya benzersiz tam ad olabilir.     |
| Hedef Tarihi    | Geçerli bir tarih olmalıdır.                                 |
| Bağlı Risk ID   | İsteğe bağlıdır. Aynı projedeki bir riskin ID’sidir.         |
| Bağlı Sorun ID  | İsteğe bağlıdır. Aynı projedeki bir sorunun ID’sidir.        |

### Örnek aksiyon satırları

```text
Aksiyon Tanımı	Kaynak Türü	Kaynak Referans	Öncelik	Durum	İlerleme %	Sorumlu	Hedef Tarihi	Bağlı Risk ID	Bağlı Sorun ID
Alternatif tedarikçi listesini hazırla	Risk		Yüksek	Açık	0	yonetici@firma.com	2027-03-20	RSK-0001	
Sunucu kapasitesini artır	Sorun		Kritik	Devam Ediyor	35	yonetici@firma.com	2027-03-25		ISS-0001
Yönetim kararını ekiple paylaş	Yönetim Kararı	YK-2027-04	Orta	Tamamlandı	100	yonetici@firma.com	2027-04-10		
```

### Aksiyon bağlantı kuralları

* Aynı satırda hem Bağlı Risk ID hem de Bağlı Sorun ID kullanılamaz.
* Bağlanan risk veya sorun seçili projeye ait olmalıdır.
* Gerçek bir risk veya sorun bağlantısı kurmak için ilgili Bağlı ... ID sütununu kullanın. Yalnızca Kaynak Referans yazılması kayıtlar arasında gerçek bağlantı oluşturmaz.
* Bağlı Risk ID girildiğinde kaynak türü ve referansı sistem tarafından risk bilgisine göre düzenlenir.
* Bağlı Sorun ID girildiğinde kaynak türü ve referansı sistem tarafından sorun bilgisine göre düzenlenir.
* Aynı riske veya soruna birden fazla aksiyon bağlanabilir.
* Durum Tamamlandı ise ilerleme 100 olmalıdır.
* İlerleme 100 ise durum Tamamlandı olmalıdır.

---

## Sık karşılaşılan hatalar

### Eksik veya yanlış sütun adı

Risk, sorun ve aksiyon dosyalarında başlıklar sistem tarafından isimlerine göre bulunur. Başlıkları dokümanda gösterildiği şekilde yazın.

### Proje sütunlarının sırasını değiştirmek

Proje dosyasındaki alanlar A–P sütun konumlarına göre okunur. Araya yeni sütun eklemeyin ve mevcut sütunların yerini değiştirmeyin.

### Geçersiz sorumlu kullanıcı

Kullanıcı sistemde bulunmuyor, tam ad birden fazla kullanıcıyla eşleşiyor veya kullanıcının rolü sorumlu olarak atanmaya uygun olmayabilir. Kullanıcı ID’si veya e-posta kullanılması önerilir.

### Başka projeye ait bağlantı kullanmak

Sorun veya aksiyonda kullanılan risk/sorun ID’si seçili projeye ait olmalıdır.

### Geçersiz seçenek kullanmak

Durum, öncelik, etki, para birimi ve benzeri alanlarda yalnızca bu dokümanda belirtilen değerleri kullanın.

Örneğin aksiyon durumunda İptal Edildi yerine İptal yazılmalıdır.

### Kısmi başarıdan sonra dosyanın tamamını yeniden yüklemek

Geçerli satırlar ilk işlemde eklenmiş olabilir. Sonuç ekranındaki hatalı satırları düzeltin ve yalnızca bu satırları yeniden yükleyin.

### Eski Excel biçimi kullanmak

Dosyayı .xls yerine .xlsx biçiminde kaydedin.
