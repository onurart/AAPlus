# 🎯 AAPlus — Dot Puzzle Game

Noktaları dönen bir daireye çarpışmadan ateşle! Bu hızlı tek dokunuşlu bulmaca mücadelesinde reflekslerinizi, zamanlamanızı ve hassasiyetinizi test edin.

**Bir hata ve oyun biter — odaklanmayı başarabilir misiniz?**

## 🎮 Özellikler

- 🎯 **500 Seviye** — Kademeli zorluk artışı
- ⚡ **5 Zorluk Kademesi** — Çok Kolay → Çok Zor
- 📱 **Tek Dokunuş** — Basit ama bağımlılık yapıcı
- 🏆 **Skor Takibi** — En iyi skor ve seviye kaydı
- 📳 **Haptic Feedback** — Dokunmatik geri bildirim
- 🎨 **Minimalist Tasarım** — Şık ve göz yormayan
- 📵 **Çevrimdışı** — İnternet gerektirmez
- 🚫 **Reklamsız** — Tamamen ücretsiz

## 🛠️ Teknoloji

| Teknoloji | Kullanım |
|-----------|----------|
| **.NET MAUI** | Cross-platform framework |
| **SkiaSharp** | 2D oyun rendering (60 FPS) |
| **CommunityToolkit.Mvvm** | MVVM pattern |
| **C# 12** | Oyun motoru |

## 📱 Platform Desteği

- ✅ Android (API 24+)
- ✅ iOS (15.0+)
- ✅ macOS (Mac Catalyst)

## 🚀 Çalıştırma

### Mac'te Test
```bash
chmod +x run.sh
./run.sh
```

### Android Release Build (AAB)
```bash
chmod +x release.sh
./release.sh
```

## 📂 Proje Yapısı

```
AAPlus/
├── Services/
│   ├── PinGameEngine.cs      # 500 seviye oyun motoru
│   ├── GameDataService.cs     # Kalıcı skor kaydetme
│   └── AudioHapticService.cs  # Haptic feedback
├── Renderers/
│   ├── PinGameRenderer.cs     # Oyun çizimi (SkiaSharp)
│   └── MainMenuRenderer.cs    # Ana menü çizimi
├── ViewModels/
│   ├── PinGameViewModel.cs    # Oyun mantığı
│   └── MainMenuViewModel.cs   # Menü mantığı
├── Views/
│   ├── PinGamePage.xaml       # Oyun sayfası
│   └── MainMenuPage.xaml      # Ana menü
├── run.sh                     # Mac test scripti
├── release.sh                 # Google Play build scripti
├── PRIVACY_POLICY.md          # Gizlilik politikası
└── STORE_LISTING.md           # Mağaza açıklaması
```

## 📊 Zorluk Sistemi

| Zorluk | Seviyeler | Hız | Özellikler |
|--------|-----------|-----|------------|
| 🟢 Çok Kolay | 1-50 | Yavaş | Basit dönüş |
| 🟡 Kolay | 51-150 | Orta | Yön değişimi |
| 🟠 Orta | 151-300 | Hızlı | Salınım + yön değişimi |
| 🔴 Zor | 301-400 | Çok Hızlı | Kaotik salınım |
| 🟣 Çok Zor | 401-500 | Aşırı | Maksimum kaos |

## 📄 Lisans

MIT License

## 📧 İletişim

Email: onurumutluoglu@gmail.com
