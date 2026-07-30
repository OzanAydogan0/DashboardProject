# Dashboard Frontend

Proje Yönetim Dashboard uygulamasının React 19 ve Vite 8 tabanlı kullanıcı arayüzü.

## Gereksinimler

- Node.js 24
- `npm ci` kullanabilen güncel npm
- Yerel geliştirmede varsayılan olarak `http://localhost:5074` üzerinde çalışan API

## Geliştirme

```text
npm ci
npm run dev
```

Uygulama API çağrılarını aynı-origin `/api` yoluna yapar. Vite geliştirme sunucusu bu yolu `http://localhost:5074` adresine proxy eder. Farklı bir yerel API adresi gerekiyorsa `vite.config.js` içindeki geliştirme proxy hedefini yerel çalışma için güncelleyin.

## Kalite kontrolleri

```text
npm run lint
npm test
npm run build
```

Testler istemci router davranışını, lint statik kod kurallarını ve build üretim paketini doğrular.

## Üretim

Üretim image'ı multi-stage [Dockerfile](Dockerfile) ile oluşturulur. Build sırasında `VITE_API_URL=/api` kullanılır; Nginx `/api/` prefix'ini kaldırıp iç ağdaki .NET API'ye iletir ve diğer yollar için SPA fallback uygular.

Kök dizindeki [README](../README.md), [kurulum](../INSTALLATION.md) ve [işletim](../OPERATIONS.md) belgeleri teslim sürecinin bağlayıcı adımlarını içerir.
