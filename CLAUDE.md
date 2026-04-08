# DANO — STRAFTAT MOD フレームワーク

EXILED (SCP:SL) に触発された、STRAFTAT 用の BepInEx 5 ベース MOD フレームワーク。
プラグイン開発者は `Plugin<TConfig>` を継承し `[DANOPlugin]` 属性を付けるだけで MOD が作れる。

## プロジェクト構成

```
DANO.sln
├── DANO.Core/              ← フレームワーク本体（BepInEx プラグイン）
│   ├── DANOLoader.cs           エントリーポイント（BepInPlugin）
│   ├── ConnectionMonitor.cs    プレイヤー切断ポーリング検出
│   ├── API/                    公開 API（プラグイン開発者が直接使う全クラス）
│   │   ├── Player.cs               プレイヤーラッパー（EXILED 風、アクションメソッド含む）
│   │   ├── Item.cs                 アイテムラッパー（EXILED 風）
│   │   ├── DanoWeapon.cs           武器ラッパー（ダメージ、発射レート、精度等）
│   │   ├── PlayerAPI.cs            プレイヤー便利メソッド（薄いラッパー）
│   │   ├── ItemAPI.cs              アイテム便利メソッド（薄いラッパー）
│   │   ├── HudAPI.cs               HUD / ヒント / メッセージ
│   │   ├── TeamAPI.cs              チーム情報
│   │   ├── ScoreAPI.cs             スコア情報・操作
│   │   ├── ServerAPI.cs            サーバー情報（プレイヤー数、ロビー等）
│   │   ├── RoundAPI.cs             ラウンド情報（テイク、スコア等）
│   │   ├── MapAPI.cs               マップ情報（現在マップ、プレイリスト等）
│   │   ├── GameAPI.cs              ゲーム制御（ラウンド終了、チームシャッフル等）
│   │   ├── DanoDoor.cs             ドアラッパー（Open/Close/Toggle）
│   │   ├── DanoTimer.cs            遅延実行・繰り返しタイマーユーティリティ
│   │   ├── CommandManager.cs       チャットコマンド登録・実行（権限システム付き）
│   │   └── CommandContext.cs       コマンド実行コンテキスト
│   ├── Events/                 イベントデータクラス + EventBus
│   │   ├── EventBus.cs             イベントバス本体（優先度付き Subscribe）
│   │   ├── PlayerEvents.cs         Spawned, Damaged, Died, WeaponFired
│   │   ├── GameEvents.cs           RoundStarted, RoundEnded, MatchEnded
│   │   ├── ChatEvents.cs           ChatMessageSending, ChatMessageReceived
│   │   ├── ConnectionEvents.cs     PlayerConnected, PlayerDisconnected
│   │   ├── ItemEvents.cs           ItemPickedUp, ItemDropped
│   │   ├── TeamEvents.cs           TeamChanged
│   │   ├── WeaponEvents.cs         WeaponReload, MeleeHit
│   │   ├── GrenadeEvents.cs        GrenadeExploded
│   │   └── DoorEvents.cs           DoorInteract（Cancel可）
│   ├── Patches/                Harmony パッチ（内部実装、プラグインに非公開）
│   ├── Plugin/                 プラグインシステム（Plugin<T>, Loader, Config, Attribute, Logger）
│   └── UI/                     UI 内部実装（DANOCanvas, HintController, Elements/）
└── DANO.Template/          ← プラグイン開発者向けテンプレート
    └── MyPlugin.cs             サンプルプラグイン
```

## 命名・配置規約（EXILED 準拠）

### namespace ルール

| ディレクトリ | namespace | 公開レベル |
|------------|-----------|-----------|
| `API/` | `DANO.API` | **public** — プラグイン開発者が `using DANO.API;` で全 API にアクセス |
| `Events/` | `DANO.Events` | **public** — イベントデータクラスと EventBus |
| `Plugin/` | `DANO.Plugin` | **public** — `Plugin<T>`, `PluginConfig`, `DANOPluginAttribute` |
| `Patches/` | `DANO.Patches` | **internal** — Harmony パッチ実装詳細。プラグインには非公開 |
| `UI/` | `DANO.UI` | **internal** — UI 内部実装。HudAPI 経由でのみ操作 |
| ルート | `DANO` | DANOLoader, DANOSentinel, ConnectionMonitor 等 |

### ラッパークラス（EXILED の Features 相当）

- **`API/Player.cs`** — ゲームの `ClientInstance` を隠蔽。`Player.Get(id)`, `Player.Local`, `Player.List` でアクセス
- **`API/Item.cs`** — ゲームの `ItemBehaviour` を隠蔽。`Item.Get(ib)`, `Item.List` でアクセス
- ラッパーは `DANO.API` namespace に配置する
- ゲームに同名クラスが存在する場合（例: グローバル `Player` クラス）、DANO.Core 内部では `API.Player` でフル修飾する
- プラグイン側は `using DANO.API;` で namespace スコープにより解決される

### イベント命名規則

| パターン | 命名 | 例 |
|---------|------|-----|
| 発生した | `〇〇Event` | `PlayerDiedEvent`, `RoundStartedEvent` |
| これから発生する（Cancel 可） | `〇〇Event` + `Cancel` プロパティ | `PlayerDamagedEvent`, `ChatMessageSendingEvent` |
| 進行形（ing）は Cancel 可を示唆 | `〇〇ingEvent` | `ChatMessageSendingEvent` |

### イベントクラスルール

- イベントのプロパティには**ラッパー型**（`API.Player`, `API.Item`）を使う。生ゲーム型（`ClientInstance`, `PlayerHealth` 等）は露出しない
- コンストラクタは `internal` にし、生ゲーム型を受け取って内部でラップする
- Cancel 可能なイベントには `public bool Cancel { get; set; }` を持たせる
- 値の書き換えが必要なプロパティ（`Damage` 等）は `{ get; set; }` にする

### パッチ命名規則

- ファイル名: `〇〇Patch.cs`（パッチ対象のクラス名ベース）
- FishNet ハッシュ名パッチは `TryApply()` パターンで手動適用（`GameManagerPatch`, `ClientInstancePatch` 参照）
- 通常メソッドパッチは `[HarmonyPatch]` 属性 + `DANOLoader.TryPatch()` で登録

### API クラスルール

- static ユーティリティクラス（`PlayerAPI`, `ItemAPI` 等）はラッパーへの薄い委譲のみ
- 主要ロジックはラッパークラス（`Player`, `Item`）に置く
- プラグイン開発者は `Player.Get(id).Health` のように直接ラッパーを使うのが推奨パス

## ビルド

```bash
dotnet build DANO.Core/DANO.Core.csproj -c Debug
dotnet build DANO.Template/DANO.Template.csproj -c Debug
```

- DANO.Core → `STRAFTAT/BepInEx/plugins/DANO/DANO.Core.dll`
- DANO.Template → `STRAFTAT/DANO/mods/MyPlugin.dll`

## 重要なパス

| パス | 説明 |
|------|------|
| `D:\SteamLibrary\steamapps\common\STRAFTAT\` | ゲームルート |
| `STRAFTAT\BepInEx\plugins\DANO\` | DANO.Core.dll の配置先 |
| `STRAFTAT\DANO\mods\` | ユーザープラグイン DLL の配置先 |
| `STRAFTAT\DANO\configs\` | プラグイン設定 JSON の保存先 |
| `D:\RiderWorks\STRAFTAT_Libs\` | 参照 DLL 置き場 |
| `D:\RiderWorks\STRAFTAT_Libs\publicized\` | publicized Assembly-CSharp |
| `D:\RiderWorks\STRAFTAT_Source\` | ilspycmd でデコンパイルした .cs ファイル群 |

## 技術スタック

- **BepInEx 5.4.23.4** (Mono) / **HarmonyX** / **.NET Framework 4.7.2**
- **Unity 2021.3.45** / **FishNet** (ネットワーク) / **TextMeshPro** (UI テキスト)
- **Newtonsoft.Json** (設定ファイル)
- **ComputerysModdingUtilities** — DANO は非バニラ互換（専用 matchmaking pool）

## 判明している制約・注意点

### BepInEx MonoBehaviour が死ぬ
STRAFTAT は BepInEx の管理 GameObject を破棄する。
→ `DANOLoader` 上の `Update()`, `StartCoroutine()`, `Start()` は**全て動かない**。
→ 自前の `[DANO]` GameObject (`HideFlags.HideAndDontSave` + `DontDestroyOnLoad`) に `DANOSentinel` コンポーネントを付けて回避。

### Harmony と Unity ライフサイクルメソッド
Unity の `Start()`, `Update()` 等は内部ネイティブキャッシュ経由で呼ばれるため、
Harmony の Prefix/Postfix が**適用成功しても発火しない**場合がある。
→ ライフサイクルメソッドのパッチは避ける。通常のゲームメソッド（`RemoveHealth`, `Fire` 等）は問題なく動く。

### FishNet 生成メソッド名
`RpcLogic___ObserversRoundSpawn_2166136261` のようなハッシュ付きメソッドは
バージョン間で変わる可能性があるため、`AccessTools.Method` で存在確認してから手動適用する。
→ `GameManagerPatch.TryApply()` 参照

### TMP リッチテキスト
`<color=cyan>` のような名前付きカラーは TMP で動かない場合がある。
→ `<color=#00FFFF>` のように hex 形式を使うこと。

### グローバル名前空間の衝突
STRAFTAT には `Player`, `Item` 等のグローバルクラスが存在する。
DANO.Core 内部では `API.Player`, `API.Item` とフル修飾すること。
プラグイン側では `using DANO.API;` の namespace スコープで自動解決される。

## プラグイン初期化フロー

```
1. BepInEx Chainloader → DANOLoader.Awake()
   ├── EventBus.Initialize()
   ├── CommandManager.Initialize()
   ├── DANOCanvas / HintController 作成
   ├── Harmony パッチ適用（個別 try-catch）
   ├── PluginLoader.ScanAndPrepare() — DANO/mods/ の DLL をスキャン、インスタンス化
   └── [DANO] GameObject 作成（HideFlags + DontDestroyOnLoad）
       ├── DANOSentinel — SteamLobby.Instance 検出 → TryEnableAll
       └── ConnectionMonitor — プレイヤー切断ポーリング

2. DANOSentinel.Update() — SteamLobby.Instance をポーリング
   └── 検出 → PluginLoader.TryEnableAll()（Dependencies トポロジカルソート済み）
       └── 各プラグインの InternalEnable() → OnEnabled()
```

## 公開 API（プラグイン開発者向け）

### ラッパー
- `Player.Get(id) / Player.Local / Player.List` — `.Name`, `.Health`, `.IsAlive`, `.TeamId`, `.Position`, `.Kill()`, `.GetHeldItem()`
  - アクション: `.Damage()`, `.Heal()`, `.HealFull()`, `.Teleport()`, `.SetTeam()`, `.Freeze()`, `.Unfreeze()`, `.Stun()`, `.AddForce()`, `.Kick()`, `.Respawn()`, `.PlayAnimation()`, `.SetReady()`
  - 移動状態: `.IsSprinting`, `.IsWalking`, `.IsCrouching`, `.IsGrounded`, `.IsSliding`, `.Speed`, `.CanMove`
- `Item.Get(ib) / Item.List` — `.Name`, `.Ammo`, `.IsHeld`, `.Holder`, `.Weapon`
- `DanoWeapon.Get(weapon) / FromItem(item)` — `.Damage`, `.FireRate`, `.Ammo`, `.IsGun`, `.IsMelee`, `.ReloadTime`
- `DanoDoor.Get(door) / DanoDoor.List` — `.IsOpen`, `.Toggle()`, `.Open()`, `.Close()`

### 便利メソッド
- `PlayerAPI.All / Local / Get(id) / GetByName(name) / GetBySteamId(steamId) / IsAlive(id) / GetTeamId(id) / Alive / Count / IsHost(player)`
- `ItemAPI.GetAll() / GetHeldItem(player) / IsHeld(item) / GetHolder(item)`
- `HudAPI.ShowHint() / Broadcast() / LocalMessage() / CreateTextLabel() / CreateProgressBar() / CreatePanel()`
- `TeamAPI.GetTeamId() / GetTeamMembers() / GetActiveTeams() / AreAllies() / IsTeamMode()`
- `ScoreAPI.GetMatchScore() / GetRoundScore() / GetCurrentTake() / GetRoundScoreToWin() / AddMatchPoints() / AddRoundScore() / ResetRound()`
- `ServerAPI.MaxPlayers / PlayerCount / LobbyName / GameStarted / IsPaused`
- `RoundAPI.CurrentTake / ScoreToWin / AlivePlayers / AliveCount`
- `MapAPI.CurrentMap / IsLoading / Playlist`
- `GameAPI.ForceEndRound() / ProgressToNextTake() / ResetGame() / ScrambleTeams() / KickPlayer() / AlivePlayers`

### コマンド
- `CommandManager.Register(name, handler, description, hostOnly) / Unregister(name)`
- `CommandContext.Sender / Args / RawArgs / Reply(msg)`

### イベント
- `EventBus.Subscribe<T>(handler, priority) / Unsubscribe<T>() / Raise<T>()`
- Player: `PlayerSpawnedEvent`, `PlayerDamagedEvent`(Cancel可), `PlayerDiedEvent`, `WeaponFiredEvent`(Cancel可)
- Game: `RoundStartedEvent`, `RoundEndedEvent`, `MatchEndedEvent`
- Chat: `ChatMessageSendingEvent`(Cancel可), `ChatMessageReceivedEvent`
- Connection: `PlayerConnectedEvent`, `PlayerDisconnectedEvent`
- Item: `ItemPickedUpEvent`, `ItemDroppedEvent`
- Team: `TeamChangedEvent`
- Weapon: `WeaponReloadEvent`, `MeleeHitEvent`
- Grenade: `GrenadeExplodedEvent`
- Door: `DoorInteractEvent`(Cancel可)
- Game(追加): `SpawnPhaseStartedEvent`, `GameStartedEvent`

### プラグインシステム
- `Plugin<TConfig>` 基底クラス — `OnEnabled()`, `OnDisabled()`, `Log`, `Logger`, `Config`
- `[DANOPlugin(id, version, author, description, Dependencies)]` 属性
- 依存関係はトポロジカルソートで自動解決
- 優先度付きイベント — `Subscribe<T>(handler, priority)` (小さいほど先に実行)
