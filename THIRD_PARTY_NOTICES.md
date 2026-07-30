# Üçüncü Taraf Bileşen Bildirimleri

Bu dosya doğrudan uygulama bağımlılıklarının teslim envanteridir; lisans metinlerinin veya hukuk incelemesinin yerine geçmez. Kesin sürümler `frontend/package-lock.json`, `backend/dashboardapi/dashboardapi.csproj` ve oluşturulan image içindeki restore çıktısıdır.

## Backend

| Bileşen | Sürüm | Lisans / not |
|---|---:|---|
| .NET / ASP.NET Core | 10.0 | MIT; Microsoft üçüncü taraf bildirimleri ayrıca geçerlidir |
| BCrypt.Net-Next | 4.2.0 | MIT |
| ClosedXML | 0.105.1 | MIT |
| Microsoft.AspNetCore.Authentication.JwtBearer | 10.0.10 | MIT |
| Microsoft.AspNetCore.OpenApi | 10.0.10 | MIT |
| Microsoft.EntityFrameworkCore.Design | 10.0.10 | MIT; build/design dependency |
| Microsoft.EntityFrameworkCore.Sqlite | 10.0.10 | MIT |
| Microsoft.EntityFrameworkCore.Tools | 10.0.10 | MIT; build/design dependency |
| Microsoft.OpenApi | 2.11.0 | MIT |
| QuestPDF | 2026.7.1 | Dual lisans; aşağıdaki zorunlu kontrole bakın |
| SQLitePCLRaw.bundle_e_sqlite3 | 2.1.12 | Apache-2.0; paket içi SQLite bildirimleri ayrıca geçerlidir |

### QuestPDF üretim lisansı

Uygulama QuestPDF ile PDF üretir. Bu sürümün Community lisansı her kurum için otomatik olarak uygun değildir. Üretim sahibi:

1. Müşterinin Community uygunluğunu güncel paket lisans metnine göre yazılı olarak doğrulamalı veya uygun Professional/Enterprise lisansını satın almalıdır.
2. `.env` içindeki `QUESTPDF_LICENSE` değerini yalnız bu karara göre `Community`, `Professional` veya `Enterprise` yapmalıdır.
3. Lisans kanıtını teslim dosyasında saklamalıdır.

`Evaluation` üretim dağıtımı için seçenek değildir. Güncel ve bağlayıcı metin NuGet paketindeki `LICENSE.md` dosyasıdır.

## Frontend

| Bileşen | Kilitli sürüm | Lisans |
|---|---:|---|
| axios | 1.18.1 | MIT |
| React | 19.2.7 | MIT |
| React DOM | 19.2.7 | MIT |
| Recharts | 3.10.0 | MIT |

Build araçları Vite, ESLint ve ilgili React eklentilerini de içerir; kesin sürümler ve transitive bağımlılıklar `frontend/package-lock.json` içindedir.

## CI güvenlik aracı

| Bileşen | Sürüm | Lisans / kullanım |
|---|---:|---|
| Aqua Security Trivy Action | 0.36.0 (commit sabitlenmiş) | Apache-2.0; yalnız CI image zafiyet taraması |

## Container tabanları

| Image | Kullanım |
|---|---|
| `mcr.microsoft.com/dotnet/sdk:10.0` | Yalnız build aşaması |
| `mcr.microsoft.com/dotnet/aspnet:10.0` | API runtime |
| `node:24-alpine` | Yalnız frontend build aşaması |
| `nginxinc/nginx-unprivileged:1.29-alpine` | Frontend runtime |
| `alpine:3.22` | SQLite init ve operasyon araçları |

API runtime image'ına Arial uyumlu PDF çıktısı için SIL Open Font License 1.1 kapsamındaki Liberation Fonts ve dağıtımın `fontconfig`/`curl` paketleri kurulur. Image içindeki işletim sistemi paket bildirimleri ayrıca geçerlidir.

Teslimde kullanılan image digest'leri `docker image inspect` ile kayda alınmalıdır. Her sürümde bağımlılık ve container güvenlik taraması tekrarlanmalıdır.
