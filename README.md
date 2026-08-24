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
- [Mock ve Demo Modları](#mock-ve-demo-modları)
- [Veri Modeli](#veri-modeli)
- [Bilinen Teknik Borçlar](#bilinen-teknik-borçlar)
- [Proje Durumu](#proje-durumu)

---

## Mimari

Sistem, beş katmandan oluşan uçtan uca bir veri hattı olarak tasarlanmıştır:

```
Veri Kaynakları (App Store Connect API / Google Play Developer API)
        │
        ▼
.NET Ingestion Servisi (Hangfire ile zamanlanmış)
        │
        ▼
AI Analiz Motoru (Claude Sonnet 4.6 — sentiment/kategori/öncelik)
        │
        ▼
MongoDB (uygulama kayıtları, yorumlar, senkronizasyon durumu)
        │
        ▼
Slack Bot (günlük özet + kritik uyarılar)
```

### Temel tasarım prensibi: "SaaS benzeri" dinamik uygulama yönetimi

Uygulamalar (Kripto/Hisse/Global/vb.) koda gömülü sabit değerler **değildir**. MongoDB'deki `apps` koleksiyonunda çalışma zamanında tanımlı kayıtlar olarak tutulur. Yeni bir uygulama eklemek kod değişikliği gerektirmez — yalnızca bu koleksiyona yeni bir kayıt eklenmesi yeterlidir.

### Provider soyutlaması

Tüm veri kaynakları (`IReviewProvider` interface'i) mock ve gerçek implementasyonlarla değiştirilebilir şekilde tasarlanmıştır:

```
IReviewProvider
    │
    ├── MockReviewProvider        → sabit JSON dosyasından okur (test/geliştirme)
    ├── LiveDemoReviewProvider    → her çağrıldığında taze veri üretir (sunum/demo amaçlı)
    ├── AppStoreReviewProvider    → gerçek App Store Connect API
    └── GooglePlayReviewProvider  → gerçek Google Play Developer API
```

Aynı prensip AI analiz katmanında da (`ISentimentAnalyzer`) uygulanmıştır:

```
ISentimentAnalyzer
    │
    ├── MockSentimentAnalyzer        → rating bazlı kaba tahmin (maliyetsiz)
    └── AnthropicSentimentAnalyzer   → gerçek Claude Sonnet 4.6 API çağrısı
```

Bu sayede gerçek API anahtarları olmadan da sistemin tamamı uçtan uca test edilebilir; anahtarlar geldiğinde yalnızca dependency injection kaydı değişir, iş mantığı aynı kalır.

---

## Teknoloji Stack'i

| Katman | Teknoloji | Notlar |
|---|---|---|
| Runtime / Dil | .NET 8, C# | |
| Veritabanı | MongoDB (Docker) | `MongoDB.Driver` ile erişim |
| Zamanlama | Hangfire (Hangfire.Mongo) | Web dashboard dahil |
| AI Analiz | Anthropic API — Claude Sonnet 4.6 | Model: `claude-sonnet-4-6` |
| Dayanıklılık | Polly | Retry/backoff (2sn/4sn/8sn, 3 deneme) |
| Loglama | Serilog | Console + günlük rotasyonlu dosya |
| Bildirim | Slack (Block Kit) | Şu an mock notifier (`ConsoleSlackNotifier`) |
| Test | xUnit | |

---

## Proje Yapısı

```
store_review_agent/
├── ReviewAgent.Worker/          → Ana host, Hangfire, DI kurulumu, Program.cs
│   └── Jobs/IngestionJob.cs     → Zamanlanmış ana iş akışı
├── ReviewAgent.Connectors/      → App Store & Google Play provider'ları
│   ├── AppStore/
│   ├── GooglePlay/
│   ├── MockData/                → Mock/demo JSON veri setleri
│   └── Resilience/              → Polly retry politikaları
├── ReviewAgent.AI/              → Sentiment analiz interface + implementasyonlar
├── ReviewAgent.Data/            → MongoDB modelleri ve repository'ler
├── ReviewAgent.Slack/           → Block Kit mesaj builder'ları
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
```

App Store Connect ve Google Play credential'ları henüz production'a bağlanmadı (bkz. [Proje Durumu](#proje-durumu)); geldiğinde aynı şekilde `user-secrets` ile eklenecek.

> **Not:** `user-secrets`, proje klasörünün tamamen dışında (`%APPDATA%\Microsoft\UserSecrets\<proje-id>\`) tutulur, repoya asla girmez. `.gitignore` içinde ayrıca `appsettings.Development.json`, `*.p8` ve `secrets/` gibi girişler de bulunur.

Anthropic key tanımlı değilse sistem otomatik olarak `MockSentimentAnalyzer`'a düşer, çökmez.

---

## Çalıştırma

```powershell
dotnet run --project ReviewAgent.Worker
```

İlk çalıştırmada:
1. MongoDB index'leri oluşturulur (`EnsureIndexesAsync`)
2. Uygulama kayıtları seed edilir (`SeedData.RunAsync` — Bithero test uygulaması dahil)
3. Hangfire recurring job (`ingestion-job`) tanımlanır, varsayılan olarak **her 5 dakikada bir** çalışır
4. Web sunucusu `http://localhost:5000` üzerinde ayağa kalkar

Durdurmak için: `Ctrl+C`

---

## Test

```powershell
dotnet test
```

Testler; JWT üretimi, App Store/Google Play response parsing, MongoDB idempotency (upsert davranışı), Slack mesaj builder'ları, retry politikaları ve mock provider'ları kapsar.

---

## Hangfire Dashboard

Worker çalışırken:

```
http://localhost:5000/hangfire
```

- **Tekrarlayan İşler** → `ingestion-job` tanımı
- **Canlı Grafik / Geçmiş Grafiği** → job'ların çalışma zamanları
- **İşler** → geçmiş çalıştırmalar, başarı/hata durumu

> Local MongoDB standalone (replica set değil) olduğu için Hangfire.Mongo change stream yerine **polling** moduna düşer. Bu, işlevi etkilemez, yalnızca MongoDB loglarında zararsız bir uyarı olarak görünür.

---

## Mock ve Demo Modları

### `MockReviewProvider`

`ReviewAgent.Connectors/MockData/reviews_appstore.json` ve `reviews_googleplay.json` dosyalarından, her biri 30'ar adet olmak üzere toplam 60 gerçekçi örnek yorum okur. Gerçek API anahtarları gelene kadar tüm geliştirme ve test bu veri seti üzerinden yapılır.

### `LiveDemoReviewProvider` — yalnızca sunum amaçlı

`reviews_live_demo.json` şablonundaki 3 örnek yorumu, **her çağrıldığında güncel zaman damgası ve benzersiz ID ile yeniden üretir**. Bu sayede `sync_state` filtresine takılmaz, her ingestion turunda "yeni gelmiş" gibi davranır — Hangfire dashboard'unda canlı hareket görmek için kullanılır.

**Varsayılan olarak kapalıdır** (`ReviewAgent.Worker/Program.cs` içinde `liveDemoProvider: null`). Açmak için:

```csharp
// ReviewAgent.Worker/Program.cs içinde IngestionJob factory'sinde:
LiveDemoReviewProvider liveDemoProvider = new(
    Path.Combine(AppContext.BaseDirectory, "MockData", "reviews_live_demo.json"));
// ... ve liveDemoProvider: null yerine liveDemoProvider ver
```

> **Dikkat:** Canlı demo modu açıkken, her Hangfire turu gerçek bir Anthropic API çağrısı tetikler (3 yorum = 3 çağrı). Sunum sonrası tekrar kapatılmalıdır.

---

## Veri Modeli

MongoDB'deki dört koleksiyon:

- **`apps`** — kayıtlı uygulamalar (isim, mağaza credential referansları, Slack kanalı, aktiflik durumu)
- **`reviews`** — ham yorum + AI analiz sonucu (tek doküman, denormalize). `externalReviewId + platform + appId` üzerinde unique index (idempotency garantisi)
- **`sync_state`** — her (uygulama, platform) çifti için son senkronizasyon zamanı; incremental ingestion'ı sağlar
- **`alert_log`** — kritik öncelikli (skor ≥ 4) yorumlar için gönderilen anlık uyarıları kaydeder, aynı yorum için tekrar bildirim gitmesini engeller

Hangfire kendi verilerini ayrı bir veritabanında (`review_agent_hangfire`) tutar.

---

## Bilinen Teknik Borçlar

- **`GoogleCredential.FromFile` deprecated uyarısı** (`GooglePlayReviewProvider.cs`): Google'ın önerdiği `CredentialFactory` yöntemi, kullanılan paket sürümünde henüz stabil olmadığı için ertelendi. `#pragma warning disable CS0618` ile bilinçli olarak işaretlendi. Gerçek Google Play credential'ları entegre edilirken tekrar değerlendirilecek.
- **Slack entegrasyonu mock (`ConsoleSlackNotifier`)**: Gerçek Slack bot token'ı geldiğinde `chat.postMessage` API'sine istek atan bir implementasyon yazılacak; mesaj formatı (Block Kit) zaten hazır ve görsel olarak doğrulanmış durumda.

---

## Proje Durumu

| Bileşen | Durum |
|---|---|
| Solution iskeleti (6 proje) | ✅ Tamamlandı |
| Docker + MongoDB | ✅ Tamamlandı |
| App Store Connect connector | ✅ Kod hazır, gerçek credential bekleniyor |
| Google Play connector | ✅ Kod hazır, gerçek credential bekleniyor |
| MongoDB veri katmanı (apps/reviews/sync_state/alert_log) | ✅ Tamamlandı, idempotency doğrulandı |
| AI analiz (Claude Sonnet 4.6) | ✅ Tamamlandı ve gerçek veriyle doğrulandı |
| Mock veri setinin geriye dönük AI analizi (backfill) | ✅ Tamamlandı (60/60 kayıt) |
| Slack mesaj formatlama | ✅ Tamamlandı, görsel doğrulandı (mock notifier ile) |
| Kritik alert mekanizması (`alert_log`) | ✅ Tamamlandı, tekrar gönderim engelleniyor |
| Hangfire zamanlama + dashboard | ✅ Tamamlandı |
| Polly retry/backoff | ✅ Tamamlandı |
| Serilog loglama | ✅ Tamamlandı |
| Gerçek Slack bildirimi | ⏳ Token bekleniyor |
| Gerçek App Store/Google Play verisi | ⏳ Bithero credential'ları bekleniyor |

---

## Katkı / Commit Standardı

Commit mesajları [Conventional Commits](https://www.conventionalcommits.org/) formatında, İngilizce tip prefix'i + Türkçe açıklama ile yazılır:

```
feat(connectors): App Store JWT kimlik dogrulamasi ekle
fix(data): _id: null duplicate key hatasini duzelt
refactor(worker): IngestionJob'i DI ile yeniden yapilandir
```
