# Store Review Intelligence Agent

BtcTurk'ün mobil uygulamalarının (Global, Kripto, Hisse ve gelecekte eklenecek diğer uygulamaların) App Store ve Google Play yorumlarını resmi API'ler üzerinden toplayan, yapay zeka ile analiz eden (sentiment, kategori, öncelik skoru) ve sonuçları Slack üzerinden raporlayan iç sistem.

Üçüncü parti bir izleme aracının yerine geçmek üzere geliştirilmiştir.

---

## İçindekiler

- [Mimari](#mimari)
- [Teknoloji Stack'i](#teknoloji-stacki)
- [Proje Yapısı](#proje-yapısı)
- [Kurulum](#kurulum)
- [Çalıştırma](#çalıştırma)
- [Test](#test)
- [Hangfire Dashboard](#hangfire-dashboard)
- [Health Check](#health-check)
- [Mock ve Demo Modları](#mock-ve-demo-modları)
- [Veri Modeli](#veri-modeli)
- [Dayanıklılık ve Güvenlik Önlemleri](#dayanıklılık-ve-güvenlik-önlemleri)
- [Çoklu Dil Desteği](#çoklu-dil-desteği)
- [Bilinen Teknik Borçlar](#bilinen-teknik-borçlar)
- [Proje Durumu](#proje-durumu)
- [Katkı / Commit Standardı](#katkı--commit-standardı)

---

## Mimari

Sistem, beş katmandan oluşan uçtan uca bir veri hattı olarak tasarlanmıştır:

```
Veri Kaynakları (App Store Connect API / Google Play Developer API)
        │
        ▼
.NET Ingestion Servisi (Hangfire ile zamanlanmış — günlük + haftalık)
        │
        ▼
AI Analiz Motoru (Claude Sonnet 4.6 — sentiment/kategori/öncelik, çoklu dil destekli)
        │
        ▼
MongoDB (uygulama kayıtları, yorumlar, senkronizasyon durumu, alert geçmişi)
        │
        ▼
Slack Bot (günlük özet + haftalık özet + kritik uyarılar)
```

### Temel tasarım prensibi: "SaaS benzeri" dinamik uygulama yönetimi

Uygulamalar (Kripto/Hisse/Global/vb.) koda gömülü sabit değerler **değildir**. MongoDB'deki `apps` koleksiyonunda çalışma zamanında tanımlı kayıtlar olarak tutulur. Yeni bir uygulama eklemek kod değişikliği gerektirmez — yalnızca bu koleksiyona yeni bir kayıt eklenmesi yeterlidir.

### Provider soyutlaması

Tüm veri kaynakları (`IReviewProvider` interface'i) mock ve gerçek implementasyonlarla değiştirilebilir şekilde tasarlanmıştır:

```
IReviewProvider
    │
    ├── MockReviewProvider        → sabit JSON dosyasından okur (test/geliştirme)
    ├── LiveDemoReviewProvider    → her çağrıda taze veri üretir (sunum/demo amaçlı, varsayılan kapalı)
    ├── AppStoreReviewProvider    → gerçek App Store Connect API
    └── GooglePlayReviewProvider  → gerçek Google Play Developer API
```

Aynı prensip AI analiz ve Slack bildirim katmanlarında da uygulanmıştır:

```
ISentimentAnalyzer                          ISlackNotifier
    │                                            │
    ├── MockSentimentAnalyzer                    ├── ConsoleSlackNotifier (mock, geliştirme)
    │   (rating bazlı kaba tahmin, maliyetsiz)   │
    └── AnthropicSentimentAnalyzer               └── SlackApiNotifier
        (gerçek Claude Sonnet 4.6 API çağrısı)       (gerçek Slack chat.postMessage — AKTİF)
```

Bu sayede gerçek API anahtarları olmadan da sistemin tamamı uçtan uca test edilebilir; anahtarlar geldiğinde yalnızca dependency injection kaydı değişir, iş mantığı aynı kalır. **AI ve Slack entegrasyonları artık gerçek servislere bağlıdır**, yalnızca App Store/Google Play tarafı mock veriyle çalışmaktadır (bkz. [Proje Durumu](#proje-durumu)).

---

## Teknoloji Stack'i

| Katman | Teknoloji | Notlar |
|---|---|---|
| Runtime / Dil | .NET 8, C# | |
| Veritabanı | MongoDB (Docker) | `MongoDB.Driver` ile erişim |
| Zamanlama | Hangfire (Hangfire.Mongo) | Web dashboard dahil, iki recurring job |
| AI Analiz | Anthropic API — Claude Sonnet 4.6 | Model: `claude-sonnet-4-6`, çoklu dil destekli |
| Dayanıklılık | Polly | Retry/backoff (2sn/4sn/8sn, 3 deneme) |
| Loglama | Serilog | Console + günlük rotasyonlu dosya |
| Bildirim | Slack (Block Kit) | `SlackApiNotifier` ile gerçek `chat.postMessage` entegrasyonu |
| Sağlık Kontrolü | ASP.NET Core HealthChecks | MongoDB bağlantı kontrolü, `/health` endpoint'i |
| Test | xUnit | 27 test |

---

## Proje Yapısı

```
store_review_agent/
├── ReviewAgent.Worker/          → Ana host, Hangfire, DI kurulumu, Program.cs
│   └── Jobs/
│       ├── IngestionJob.cs              → Zamanlanmış ana ingestion akışı (günlük)
│       ├── WeeklySummaryJob.cs          → Haftalık özet job'ı
│       ├── IngestionStatsCalculator.cs  → İstatistik hesaplama (test edilebilir, izole)
│       └── ReviewBatchLimiter.cs        → Tur başına yorum sayısı sınırlama
├── ReviewAgent.Connectors/      → App Store & Google Play provider'ları
│   ├── AppStore/
│   ├── GooglePlay/
│   ├── MockData/                → Mock/demo JSON veri setleri
│   └── Resilience/               → Polly retry politikaları
├── ReviewAgent.AI/              → Sentiment analiz interface + implementasyonlar
├── ReviewAgent.Data/            → MongoDB modelleri ve repository'ler
├── ReviewAgent.Slack/           → Block Kit mesaj builder'ları + notifier'lar
├── ReviewAgent.Tests/           → xUnit testleri
└── docker-compose.yml           → Local MongoDB kurulumu
```

---

## Kurulum

### Gereksinimler

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker Desktop](https://www.docker.com/products/docker-desktop/) (WSL2 backend, Windows'ta sanallaştırma desteği açık olmalı)
- MongoDB Compass (opsiyonel, veriyi görsel incelemek için önerilir)

### 1. Repoyu klonla

```powershell
git clone <repo-url>
cd store_review_agent
```

### 2. MongoDB'yi ayağa kaldır

```powershell
docker compose up -d
docker ps   # store_review_agent_mongo container'ının Up olduğunu doğrula
```

### 3. Solution'ı derle

```powershell
dotnet build
```

### 4. Secrets kurulumu

Proje, API anahtarlarını **kesinlikle koda veya `appsettings.json`'a yazmaz**. Development ortamında `dotnet user-secrets` kullanılır:

```powershell
cd ReviewAgent.Worker
dotnet user-secrets init
dotnet user-secrets set "Anthropic:ApiKey" "<anthropic-api-key>"
dotnet user-secrets set "Slack:BotToken" "<slack-bot-token>"
dotnet user-secrets set "Slack:DefaultChannelId" "<slack-channel-id>"
```

App Store Connect ve Google Play credential'ları henüz production'a bağlanmadı (bkz. [Proje Durumu](#proje-durumu)); geldiğinde aynı şekilde `user-secrets` ile eklenecek.

> **Not:** `user-secrets`, proje klasörünün tamamen dışında (`%APPDATA%\Microsoft\UserSecrets\<proje-id>\`) tutulur, repoya asla girmez. `.gitignore` içinde ayrıca `appsettings.Development.json`, `*.p8` ve `secrets/` gibi girişler de bulunur.

Anthropic veya Slack key'leri tanımlı değilse sistem otomatik olarak sırasıyla `MockSentimentAnalyzer` / `ConsoleSlackNotifier`'a düşer, çökmez.

---

## Çalıştırma

```powershell
dotnet run --project ReviewAgent.Worker
```

İlk çalıştırmada:
1. MongoDB index'leri oluşturulur (`EnsureIndexesAsync`) — `reviews` ve `alert_log` üzerinde unique index'ler dahil
2. Uygulama kayıtları seed edilir (`SeedData.RunAsync` — Bithero test uygulaması dahil)
3. Hangfire recurring job'ları tanımlanır:
   - `ingestion-job` — her 5 dakikada bir (`*/5 * * * *`)
   - `weekly-summary-job` — her Pazartesi 09:00 (`0 9 * * 1`)
4. Web sunucusu `http://localhost:5000` üzerinde ayağa kalkar

Durdurmak için: `Ctrl+C`

---

## Test

```powershell
dotnet test
```

27 test; JWT üretimi, App Store/Google Play response parsing, MongoDB idempotency (upsert davranışı), Slack mesaj builder'ları, retry politikaları, mock provider'ları, istatistik hesaplama (`IngestionStatsCalculator`) ve tur başına yorum limiti (`ReviewBatchLimiter`) mantığını kapsar.

---

## Hangfire Dashboard

Worker çalışırken:

```
http://localhost:5000/hangfire
```

- **Tekrarlayan İşler** → `ingestion-job` ve `weekly-summary-job` tanımları (elle "Şimdi Tetikle" ile test edilebilir)
- **Canlı Grafik / Geçmiş Grafiği** → job'ların çalışma zamanları
- **İşler** → geçmiş çalıştırmalar, başarı/hata durumu

> Local MongoDB standalone (replica set değil) olduğu için Hangfire.Mongo change stream yerine **polling** moduna düşer. Bu, işlevi etkilemez, yalnızca MongoDB loglarında zararsız bir uyarı olarak görünür.

---

## Health Check

```
http://localhost:5000/health
```

MongoDB bağlantısını kontrol eden, JSON formatında yanıt veren bir health check endpoint'i:

```json
{"status":"Healthy","checks":[{"name":"mongodb","status":"Healthy","description":null}]}
```

MongoDB erişilemez durumdaysa `HTTP 503` ve `"status":"Unhealthy"` döner — bu davranış, MongoDB container'ı bilinçli olarak durdurularak test edilmiş ve doğrulanmıştır.

---

## Mock ve Demo Modları

### `MockReviewProvider`

`ReviewAgent.Connectors/MockData/reviews_appstore.json` ve `reviews_googleplay.json` dosyalarından, her biri 30'ar adet olmak üzere toplam 60 gerçekçi örnek yorum okur. Gerçek API anahtarları gelene kadar tüm geliştirme ve test bu veri seti üzerinden yapılır. Bu 60 kaydın tamamı, geriye dönük bir backfill işlemiyle gerçek Claude Sonnet 4.6 ile analiz edilmiştir.

### `LiveDemoReviewProvider` — yalnızca sunum amaçlı

`reviews_live_demo.json` şablonundaki 3 örnek yorumu, **her çağrıldığında güncel zaman damgası ve benzersiz ID ile yeniden üretir**. Bu sayede `sync_state` filtresine takılmaz, her ingestion turunda "yeni gelmiş" gibi davranır — Hangfire dashboard'unda canlı hareket görmek için kullanılır.

**Varsayılan olarak kapalıdır** (`ReviewAgent.Worker/Program.cs` içinde `liveDemoProvider: null`). Açmak için:

```csharp
// ReviewAgent.Worker/Program.cs içinde IngestionJob factory'sinde:
LiveDemoReviewProvider liveDemoProvider = new(
    Path.Combine(AppContext.BaseDirectory, "MockData", "reviews_live_demo.json"));
// ... ve liveDemoProvider: null yerine liveDemoProvider ver
```

> **Dikkat:** Canlı demo modu açıkken, her Hangfire turu gerçek bir Anthropic API çağrısı VE gerçek bir Slack mesajı tetikler (3 yorum = 3 çağrı + Slack bildirimi). Sunum sonrası tekrar kapatılmalıdır.

---

## Veri Modeli

MongoDB'deki dört koleksiyon:

- **`apps`** — kayıtlı uygulamalar (isim, mağaza credential referansları, Slack kanalı, aktiflik durumu)
- **`reviews`** — ham yorum + AI analiz sonucu (tek doküman, denormalize). `externalReviewId + platform + appId` üzerinde unique index (idempotency garantisi)
- **`sync_state`** — her (uygulama, platform) çifti için son senkronizasyon zamanı; incremental ingestion'ı sağlar. Tur başına yorum limiti aşıldığında, işlenen en son yorumun tarihiyle güncellenir (`DateTime.UtcNow` ile değil) — böylece sınırı aşan yorumlar kaybolmaz, sıradaki tura düzgünce aktarılır
- **`alert_log`** — kritik öncelikli (skor ≥ 4) yorumlar için gönderilen anlık uyarıları kaydeder, aynı yorum için tekrar bildirim gitmesini engeller

Hangfire kendi verilerini ayrı bir veritabanında (`review_agent_hangfire`) tutar.

---

## Dayanıklılık ve Güvenlik Önlemleri

Sistem, gerçek dış servislerle (App Store, Google Play, Anthropic, Slack) çalışırken karşılaşılabilecek sorunlara karşı çok katmanlı bir koruma stratejisi izler:

| Önlem | Katman | Açıklama |
|---|---|---|
| **Retry / backoff** | Connectors, AI | Polly ile geçici ağ hatalarında 3 deneme, üstel bekleme (2sn/4sn/8sn) |
| **Yorum bazlı hata izolasyonu** | `IngestionJob` | Bir yorumun analizi başarısız olursa yalnızca o yorum atlanır (loglanarak), turun geri kalanı etkilenmez |
| **Tur başına yorum limiti** | `IngestionJob` / `ReviewBatchLimiter` | Bir turda en fazla 50 yorum işlenir (`MaxReviewsPerRun`); aşan kısım veri kaybı olmadan sıradaki tura bırakılır |
| **Idempotency** | `reviews`, `apps`, `alert_log` koleksiyonları | Unique index'ler sayesinde aynı kayıt tekrar tekrar işlenmez |
| **Health check** | Worker (web host) | `/health` endpoint'i, MongoDB bağlantısını canlı olarak raporlar |
| **Güvenli fallback** | AI, Slack | API anahtarı tanımlı değilse sistem çökmek yerine otomatik olarak mock implementasyona döner |

---

## Çoklu Dil Desteği

BtcTurk uygulamaları global App Store/Google Play üzerinden erişilebilir olduğu için, yorumlar Türkçe dışında dillerde de (İngilizce, Arapça vb.) gelebilir. AI analiz prompt'u, girdi metninin dilinden bağımsız olarak **`summary` alanını her zaman Türkçe üretecek** şekilde tasarlanmıştır — bu, Slack raporlarının dil tutarlılığını (Türkçe konuşan ekip için) garanti eder. `sentiment`/`category`/`priority_score` alanları dilden etkilenmez.

Bu davranış, gerçek Claude Sonnet 4.6 çağrılarıyla (İngilizce ve Arapça örnek yorumlarla) doğrulanmıştır.

---

## Bilinen Teknik Borçlar

- **`GoogleCredential.FromFile` deprecated uyarısı** (`GooglePlayReviewProvider.cs`): Google'ın önerdiği `CredentialFactory` yöntemi, kullanılan paket sürümünde henüz stabil olmadığı için ertelendi. `#pragma warning disable CS0618` ile bilinçli olarak işaretlendi. Gerçek Google Play credential'ları entegre edilirken tekrar değerlendirilecek.
- **Kritik alert mesajlarında gerçek yorum linki (`ReviewUrl`) henüz yok**: Mock veride gerçek bir App Store/Google Play linki üretilemediği için şu an `null`. Gerçek credential'lar geldiğinde, yorumun mağaza sayfasına giden linki oluşturulacak.

---

## Proje Durumu

| Bileşen | Durum |
|---|---|
| Solution iskeleti (6 proje) | ✅ Tamamlandı |
| Docker + MongoDB | ✅ Tamamlandı |
| App Store Connect connector | ✅ Kod hazır, gerçek credential bekleniyor |
| Google Play connector | ✅ Kod hazır, gerçek credential bekleniyor |
| MongoDB veri katmanı (apps/reviews/sync_state/alert_log) | ✅ Tamamlandı, idempotency doğrulandı |
| AI analiz (Claude Sonnet 4.6) | ✅ Tamamlandı, gerçek veriyle doğrulandı |
| Mock veri setinin geriye dönük AI analizi (backfill) | ✅ Tamamlandı (60/60 kayıt) |
| Çoklu dil desteği (özet her zaman Türkçe) | ✅ Tamamlandı, gerçek testle doğrulandı |
| Slack mesaj formatlama | ✅ Tamamlandı, görsel doğrulandı |
| **Gerçek Slack bildirimi (`SlackApiNotifier`)** | ✅ Tamamlandı, gerçek kanala mesaj gidiyor |
| Kritik alert mekanizması (`alert_log`) | ✅ Tamamlandı, tekrar gönderim engelleniyor |
| Günlük ingestion job'ı | ✅ Tamamlandı |
| **Haftalık özet job'ı** | ✅ Tamamlandı |
| Hangfire zamanlama + dashboard | ✅ Tamamlandı |
| Polly retry/backoff | ✅ Tamamlandı |
| **Yorum bazlı hata izolasyonu** | ✅ Tamamlandı |
| **Tur başına yorum limiti (rate limiting)** | ✅ Tamamlandı |
| **Health check endpoint** | ✅ Tamamlandı, test edilmiş |
| Serilog loglama | ✅ Tamamlandı |
| Gerçek App Store/Google Play verisi | ⏳ Bithero credential'ları bekleniyor |

---

## Katkı / Commit Standardı

Commit mesajları [Conventional Commits](https://www.conventionalcommits.org/) formatında, İngilizce tip prefix'i + Türkçe açıklama ile yazılır:

```
feat(connectors): App Store JWT kimlik dogrulamasi ekle
fix(data): _id: null duplicate key hatasini duzelt
refactor(worker): IngestionJob'i DI ile yeniden yapilandir
```