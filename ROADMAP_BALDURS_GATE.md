# Roadmap Migrasi Visual RPG (Baldur's Gate Style)

Dokumen ini adalah cetak biru teknis dan panduan bertahap bagi tim untuk memigrasikan engine logika D&D C# (saat ini CLI) menuju game RPG visual isometrik berbasis game engine.

---

## 1. Visi Proyek

Membangun game RPG taktis *turn-based* dengan estetika visual isometrik klasik (ala *Baldur's Gate 1 & 2* atau *Pillars of Eternity*) atau 3D top-down (ala *Baldur's Gate 3*), di mana seluruh aturan D&D 5e yang telah dibuat di repository ini berfungsi sebagai otak (backend).

### Prinsip Arsitektur: Headless Core + GUI View
- **Core C# (Project Saat Ini):** Tetap menjadi *Class Library* murni (`DnD.dll`). Bertanggung jawab atas:
  - Validasi movement & aksi turn (`TurnResources`, `TurnManager`).
  - Matematika koordinat dan jangkauan senjata (`BattleGrid`).
  - Mekanika dadu d20, advantage/disadvantage, dan saving throw (`StatusEffectSystem`, `SpellSystem`).
  - Manajemen anggota party 4-karakter (`Party`).
  - Graph dialog dan skill check (`DialogueEngine`).
- **Game Engine (Frontend / Visualizer):** Hanya bertugas sebagai "layar dan kontroler":
  - Membaca koordinat grid logic lalu merender sprite di layar.
  - Menerima klik mouse/keyboard lalu memanggil fungsi di Core C#.
  - Memutar animasi serangan, partikel sihir (*Fireball*), sfx, dan musik latar.

---

## 2. Rekomendasi Game Engine

### Pilihan Utama: **Godot Engine 4 (.NET / C#)** *(Direkomendasikan)*
- **Alasan:**
  1. Menggunakan **.NET 8 C#**, sama persis dengan versi target repo ini (`net8.0`). Library logika saat ini bisa langsung di-referensikan tanpa konversi.
  2. Sangat ringan (installer hanya ~60 MB, tanpa instalasi berat).
  3. Fitur **TileMap 2D Isometrik** bawaan sangat matang dan mudah diintegrasikan dengan koordinat `BattleGrid(x, y)`.
  4. Gratis selamanya, open source, tanpa biaya royalti.

### Pilihan Alternatif: **Unity (C#)**
- Cocok jika tim sudah punya pengalaman Unity atau ingin langsung menggunakan aset 3D siap pakai dari Unity Asset Store.

---

## 3. Tahapan Implementasi (Milestones)

### Milestone 1: Setup Proyek & Bridge Logic
- **Target:** Engine grafis dapat membaca class dari repository ini.
- **Langkah Kerja:**
  1. Buat project baru di Godot 4 (.NET) dalam subfolder `/Client` atau repo terpisah.
  2. Tambahkan reference ke project core (`DnD.csproj`) atau compile `DnD.dll` dan masukkan ke engine.
  3. Buat script bridge sederhana: panggil `DiceRoller.Roll(DiceType.D20)` di tombol GUI engine dan pastikan angka muncul di label UI.

### Milestone 2: Peta Isometrik & Sinkronisasi Grid
- **Target:** Visualisasi visual dari `BattleGrid`.
- **Langkah Kerja:**
  1. Buat node `TileMap` isometrik berukuran 10x10 petak (1 tile = 5 feet).
  2. Hubungkan fungsi `BattleGrid.GetEntityAt(x, y)`:
     - Render ground tile untuk petak kosong.
     - Render dinding/batu untuk petak obstacle (`#`).
     - Tempatkan token/sprite karakter di petak `P` dan musuh di `E`.
  3. Implementasikan mouse-hover grid: saat mouse mengarah ke petak, petak bersinar (highlight) dan menampilkan jarak *feet*.

### Milestone 3: Sistem Pergerakan & Kontrol Mouse
- **Target:** Klik mouse untuk memindahkan party member.
- **Langkah Kerja:**
  1. Klik karakter untuk memilih (select active character).
  2. Tampilkan jangkauan gerak (overlay warna biru = area yang bisa dijangkau sesuai sisa `MovementRemaining`).
  3. Klik petak tujuan: panggil `grid.MoveEntity(...)`.
  4. Jika memicu *Opportunity Attack*, tampilkan animasi tebasan reaksi musuh sebelum karakter selesai melangkah.

### Milestone 4: Combat UI & Action Bar
- **Target:** Tampilan HUD pertarungan ala RPG taktis.
- **Komponen UI:**
  1. **Turn Order Bar (Atas Layar):** Potret karakter dan musuh berjejer sesuai urutan inisiatif dari `TurnManager.InitiativeScores`.
  2. **Action Bar (Bawah Layar):**
     - Indikator resource bulat: Hijau (Action), Oranye (Bonus Action), Biru (Reaction), Meteran Gerak (ft).
     - Tombol aksi: Attack, Dash, Dodge, Disengage, Second Wind.
     - Tombol Spellbook: Daftar spell (Cantrip gratis & Spell slot level 1–3).
  3. **Visual AoE Targeting:** Saat memilih *Fireball*, muncul lingkaran merah 15ft di bawah kursor mouse sebelum klik cast.
  4. **Floating Combat Text:** Angka damage merah (`-14`) atau teks status (`BLINDED`, `STUNNED`, `CRITICAL HIT`) melayang di atas kepala karakter.

### Milestone 5: Layar Dialog & Animasi Dadu
- **Target:** Kotak dialog cerita dengan lemparan dadu 3D/2D saat skill check.
- **Langkah Kerja:**
  1. **Dialogue Box UI:** Panel dialog di bawah layar menampilkan nama pembicara, potret NPC, dan teks narasi dari `DialogueNode`.
  2. **Tombol Opsi:** Daftar pilihan respon, lengkap dengan penanda check, misal: `[Persuasion DC 12] Tolong biarkan kami lewat.`
  3. **Dice Rolling Screen:** Saat opsi skill check diklik, muncul modal lemparan dadu d20 yang berputar dan menampilkan bonus stat sebelum menentukan berhasil atau gagal cabang ceritanya.

### Milestone 6: Audio, Polishing, & Game Loop
- **Langkah Kerja:**
  1. Efek suara (SFX): ayunan pedang, benturan perisai, ledakan spell, klik UI.
  2. BGM pertarungan dan eksplorasi hutan.
  3. Layar Game Over dan Victory saat quest bos selesai.
  4. Menu Save/Load terhubung ke `SaveSystem.cs`.

---

## 4. Rekomendasi Sumber Aset Gratis (Royalty-Free)

Untuk tahap prototipe visual tanpa biaya:

1. **Sprite & Tilemap Isometrik 2D:**
   - [itch.io Free Isometric Game Assets](https://itch.io/game-assets/free/tag-isometric)
   - [Kenney.nl (Kenney Game Assets)](https://kenney.nl/assets) - Asset pack 2D/3D gratis domain publik (CC0).
   - [OpenGameArt.org](https://opengameart.org/) - Sprite fantasi, icon spell, dan potret karakter RPG.
2. **Icon & UI:**
   - [Game-icons.net](https://game-icons.net/) - Ribuan ikon skill/spell RPG format SVG/PNG gratis (CC BY 3.0).
3. **Audio & Sound Effects:**
   - [Freesound.org](https://freesound.org/) - Efek suara dadu, pertarungan, dan sihir.
   - [Sonniss GDC Audio Archives](https://sonniss.com/gameaudioarchive) - Sound library berkualitas tinggi.

---

## 5. Saran Pembagian Tugas Tim

| Role / Posisi | Tanggung Jawab Utama |
|---|---|
| **Engine & Scene Lead** | Membuat scene di Godot/Unity, setup kamera isometrik, TileMap, dan navigasi klik mouse. |
| **Bridge & Gameplay Dev** | Menghubungkan event C# core (`GameEvents`, `BattleGrid`, `TurnManager`) ke visual scene & UI. |
| **UI / UX Designer** | Mendesain layout Action Bar, Turn Tracker, Health Bar, dan Dialogue Box. |
| **Art & Sound Specialist** | Mencari/membuat aset sprite 2D, animasi frame, icon kemampuan, BGM, dan SFX. |
| **Story & Level Designer** | Merancang layout rintangan `BattleGrid` dan menulis percabangan dialog/quest di `DialogueEngine`. |
