# DANO — STRAFTAT MOD フレームワーク

EXILED (SCP:SL) に触発された、STRAFTAT 用の BepInEx 5 ベース MOD フレームワーク。
プラグイン開発者は `Plugin<TConfig>` を継承し `[DANOPlugin]` 属性を付けるだけで MOD が作れる。

## プロジェクト構成

```
DANO.sln
├── DANO.Core/              ← フレームワーク本体（BepInEx プラグイン）
│   ├── DANOLoader.cs           エントリーポイント（BepInPlugin）
│   ├── ConnectionMonitor.cs    イベント検出の中核コンポーネント（ポーリング+直接フック、Harmony 併用）
│   ├── API/                    公開 API（プラグイン開発者が直接使う全クラス）
│   │   ├── Player.cs               プレイヤーラッパー（EXILED 風、アクションメソッド含む）
│   │   ├── Item.cs                 アイテムラッパー（EXILED 風）
│   │   ├── DanoWeapon.cs           武器ラッパー基底クラス（継承階層、キャスト可能）
│   │   ├── DanoGun.cs              Gun ラッパー
│   │   ├── DanoShotgun.cs          Shotgun ラッパー（PelletCount, Spread）
│   │   ├── DanoMinigun.cs          Minigun ラッパー（SpinUpTime, RotationSpeed）
│   │   ├── DanoChargeGun.cs        ChargeGun ラッパー（MaxChargeTime, Radius）
│   │   ├── DanoBeamGun.cs          BeamGun ラッパー（LaunchForce, Radius）
│   │   ├── DanoLargeRaycastGun.cs  LargeRaycastGun ラッパー（BulletRadius, BoxDimensions）
│   │   ├── DanoDualLauncher.cs     DualLauncher ラッパー（各弾種フラグ）
│   │   ├── DanoBumpGun.cs          BumpGun ラッパー（LaunchForce, Knockback）
│   │   ├── DanoRepulsiveGun.cs     RepulsiveGun ラッパー（RepulseForce）
│   │   ├── DanoTaser.cs            Taser ラッパー（ChargeTime, StunTime）
│   │   ├── DanoMeleeWeapon.cs      MeleeWeapon ラッパー（攻撃ダメージ、ノックバック）
│   │   ├── DanoFlashLight.cs       FlashLight ラッパー（IsOn）
│   │   ├── DanoPropeller.cs        Propeller ラッパー（FlySpeed, IsFlying）
│   │   ├── DanoWeaponHandSpawner.cs WeaponHandSpawner ラッパー（地雷/クレイモア判定）
│   │   ├── DanoGrenade.cs          グレネードラッパー（PhysicsGrenade 隠蔽）
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
│   │   ├── PlayerEvents.cs         Spawned, Damaging(Cancel可)/Damaged, Died, Firing(Cancel可)/Fired
│   │   ├── GameEvents.cs           RoundStarted, RoundEnded, MatchEnded
│   │   ├── ChatEvents.cs           MessageSending(Cancel可)/MessageSent, MessageReceived
│   │   ├── ConnectionEvents.cs     PlayerConnected, PlayerDisconnected
│   │   ├── ItemEvents.cs           PickingUp(Cancel可)/PickedUp, Dropping(Cancel可)/Dropped
│   │   ├── TeamEvents.cs           TeamChanged
│   │   ├── WeaponEvents.cs         Reloading(Cancel可)/Reloaded
│   │   ├── GrenadeEvents.cs        GrenadeExploded
│   │   ├── DoorEvents.cs           Interacting(Cancel可)/Interacted
│   │   ├── MovementEvents.cs       Sprint/Crouch/Slide/Grounded/Lean/Aim 状態変化
│   │   ├── MapEvents.cs            MapChanged
│   │   ├── ScoreEvents.cs          MatchScoreChanged, RoundScoreChanged
│   │   └── PauseEvents.cs          GamePaused, GameResumed
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
| `Patches/` | `DANO.Patches` | **internal** — Harmony パッチ（PatchAll 方式で動作確認済み） |
| `UI/` | `DANO.UI` | **internal** — UI 内部実装。HudAPI 経由でのみ操作 |
| ルート | `DANO` | DANOLoader, DANOSentinel, ConnectionMonitor 等 |

### ラッパークラス（EXILED の Features 相当）

- **`API/Player.cs`** — ゲームの `ClientInstance` を隠蔽。`Player.Get(id)`, `Player.Local`, `Player.List` でアクセス
- **`API/Item.cs`** — ゲームの `ItemBehaviour` を隠蔽。`Item.Get(ib)`, `Item.List` でアクセス
- **`API/DanoWeapon.cs`** — 武器ラッパー基底クラス。`DanoWeapon.Get(weapon)` で具象型を返す（キャスト可能）
  - 14 サブクラス: `DanoGun`, `DanoShotgun`, `DanoMinigun`, `DanoChargeGun`, `DanoBeamGun`, `DanoLargeRaycastGun`, `DanoDualLauncher`, `DanoBumpGun`, `DanoRepulsiveGun`, `DanoTaser`, `DanoMeleeWeapon`, `DanoFlashLight`, `DanoPropeller`, `DanoWeaponHandSpawner`
  - 使い方: `if (weapon is DanoShotgun sg) { int pellets = sg.PelletCount; }`
- **`API/DanoGrenade.cs`** — グレネードラッパー。`DanoGrenade.Get(g)`, `DanoGrenade.List`, `DanoGrenade.Active` でアクセス
- **`API/DanoDoor.cs`** — ドアラッパー。`DanoDoor.Get(d)`, `DanoDoor.List` でアクセス
- ラッパーは `DANO.API` namespace に配置する
- ゲームに同名クラスが存在する場合（例: グローバル `Player` クラス）、DANO.Core 内部では `API.Player` でフル修飾する
- プラグイン側は `using DANO.API;` で namespace スコープにより解決される

### イベント命名規則（EXILED 準拠 -ing/-ed ペア方式）

| パターン | 命名 | Cancel | 例 |
|---------|------|--------|-----|
| これから発生する（事前） | `〇〇ingEvent` | **可能** | `PlayerDamagingEvent`, `WeaponFiringEvent` |
| 発生した（事後・通知のみ） | `〇〇edEvent` | 不可 | `PlayerDamagedEvent`, `WeaponFiredEvent` |
| 変化のみ（ing/ed の区別なし） | `〇〇ChangedEvent` | 不可 | `TeamChangedEvent`, `MapChangedEvent` |
| 一時点の出来事（区別不要） | `〇〇Event` | 不可 | `PlayerDiedEvent`, `RoundStartedEvent` |

**ペア一覧:**

| -ing（Cancel可） | -ed（通知のみ） |
|----------------|----------------|
| `PlayerDamagingEvent` | `PlayerDamagedEvent` |
| `WeaponFiringEvent` | `WeaponFiredEvent` |
| `WeaponReloadingEvent` | `WeaponReloadedEvent` |
| `ItemPickingUpEvent` | `ItemPickedUpEvent` |
| `ItemDroppingEvent` | `ItemDroppedEvent` |
| `DoorInteractingEvent` | `DoorInteractedEvent` |
| `ChatMessageSendingEvent` | `ChatMessageSentEvent` |

### イベントクラスルール

- イベントのプロパティには**ラッパー型**（`API.Player`, `API.Item`）を使う。生ゲーム型（`ClientInstance`, `PlayerHealth` 等）は露出しない
- コンストラクタは `internal` にし、生ゲーム型を受け取って内部でラップする
- Cancel 可能なイベントには `public bool Cancel { get; set; }` を持たせる
- 値の書き換えが必要なプロパティ（`Damage` 等）は `{ get; set; }` にする

### イベント検出方式（v0.4.1 ハイブリッド方式）

- **Harmony PatchAll()** は `[HarmonyPatch]` 属性方式で動作する（v0.3.0 で失敗した手動 `harmony.Patch()` とは異なる）
- **ServerRpc メソッド**（`PlayerHealth.RemoveHealth` 等）は FishNet が IL 書き換えするため Harmony パッチ不可 → ポーリングで検出
- **private メソッド**（`Gun.Fire()` 等）は Mono JIT 最適化で発火しない場合あり → 上位の public/virtual メソッドをパッチ
- Cancel 可能なイベントは「状態巻き戻し」方式（HP 回復、弾数復元、ドア開閉復元）

| 検出方式 | 対象イベント |
|----------|-------------|
| Harmony パッチ | ItemPickingUp(Cancel可)/PickedUp, ItemDropping(Cancel可)/Dropped, WeaponFiring(Cancel可)/Fired, WeaponReloading(Cancel可)/Reloaded, DoorInteracting(Cancel可)/Interacted |
| ポーリング | PlayerDamaging(Cancel可)/Damaged, PlayerDied, PlayerSpawned, TeamChanged, RoundStarted/Ended, GameStarted, SpawnPhase, MatchEnded, GrenadeExploded, PlayerConnected/Disconnected, Movement状態変化(Sprint/Crouch/Slide/Grounded/Lean/Aim), MapChanged, MatchScoreChanged, RoundScoreChanged, GamePaused/Resumed |
| 直接フック | ChatMessageSending(Cancel可)/Sent, ChatMessageReceived |

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

- **BepInEx 5.4.23.4** (Mono) / **.NET Framework 4.7.2** — Harmony PatchAll() 方式で動作（手動 Patch() は不可）
- **Unity 2021.3.45** / **FishNet** (ネットワーク) / **TextMeshPro** (UI テキスト)
- **Newtonsoft.Json** (設定ファイル)
- **ComputerysModdingUtilities** — DANO は非バニラ互換（専用 matchmaking pool）

## 判明している制約・注意点

### BepInEx MonoBehaviour が死ぬ
STRAFTAT は BepInEx の管理 GameObject を破棄する。
→ `DANOLoader` 上の `Update()`, `StartCoroutine()`, `Start()` は**全て動かない**。
→ 自前の `[DANO]` GameObject (`HideFlags.HideAndDontSave` + `DontDestroyOnLoad`) に `DANOSentinel` コンポーネントを付けて回避。

### Harmony の制約（STRAFTAT 固有）
- **PatchAll() + `[HarmonyPatch]` 属性方式** → 動作する（v0.4.1 で確認）
- **手動 `harmony.Patch()`** → v0.3.0 で全パッチ発火せず（原因不明、属性方式に統一）
- **ServerRpc メソッド**（`PlayerHealth.RemoveHealth` 等）→ FishNet が IL 書き換えするため Harmony パッチ不可
- **一部 private メソッド**（`Gun.Fire()` 等）→ Mono JIT 最適化（インライン化）で発火しない場合あり
→ パッチ可能なメソッドは `Patches/HarmonyPatches.cs` で、不可能なものは `ConnectionMonitor` のポーリングで検出

### STRAFTAT 武器クラス階層
全 14 サブクラスが `Weapon` を直接継承（`Gun` からの継承ではない）:
`Gun`, `Shotgun`, `Minigun`, `ChargeGun`, `BeamGun`, `LargeRaycastGun`, `DualLauncher`, `BumpGun`, `RepulsiveGun`, `Taser`, `MeleeWeapon`, `FlashLight`, `Propeller`, `WeaponHandSpawner`
→ 武器パッチは `Weapon.WeaponUpdate()` をフックし、全サブクラスを一括検出する

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
   ├── PluginLoader.ScanAndPrepare() — DANO/mods/ の DLL をスキャン、インスタンス化
   └── [DANO] GameObject 作成（HideFlags + DontDestroyOnLoad）
       ├── DANOSentinel — SteamLobby.Instance 検出 → TryEnableAll
       └── ConnectionMonitor — 全イベント検出（ポーリング + 直接フック）

2. DANOSentinel.Update() — SteamLobby.Instance をポーリング
   └── 検出 → PluginLoader.TryEnableAll()（Dependencies トポロジカルソート済み）
       └── 各プラグインの InternalEnable() → OnEnabled()
```

## 公開 API（プラグイン開発者向け）

### ラッパー
- `Player.Get(id) / Player.Local / Player.List` — `.Name`, `.Health`, `.IsAlive`, `.TeamId`, `.Position`, `.Kill()`, `.GetHeldItem()`
  - アクション: `.Damage()`, `.Heal()`, `.HealFull()`, `.Teleport()`, `.SetTeam()`, `.Freeze()`, `.Unfreeze()`, `.Stun()`, `.AddForce()`, `.Kick()`, `.Respawn()`, `.PlayAnimation()`, `.SetReady()`
  - 移動状態: `.IsSprinting`, `.IsWalking`, `.IsCrouching`, `.IsGrounded`, `.IsSliding`, `.IsLeaning`, `.IsAiming`, `.IsScopeAiming`, `.Speed`, `.CanMove`
- `Item.Get(ib) / Item.List` — `.Name`, `.Ammo`, `.IsHeld`, `.Holder`, `.Weapon`
- `DanoWeapon.Get(weapon) / FromItem(item)` — 基底: `.Damage`, `.FireRate`, `.Ammo`, `.IsGun`, `.IsMelee`, `.ReloadTime`, `.WeaponType`
  - キャスト: `DanoGun`(ReloadTime), `DanoShotgun`(PelletCount, Spread), `DanoMinigun`(SpinUpTime, RotationSpeed), `DanoChargeGun`(MaxChargeTime, Radius), `DanoBeamGun`(LaunchForce, Radius), `DanoLargeRaycastGun`(BulletRadius), `DanoDualLauncher`(各弾種フラグ), `DanoBumpGun`(LaunchForce), `DanoRepulsiveGun`(RepulseForce), `DanoTaser`(ChargeTime, StunTime), `DanoMeleeWeapon`(BaseAttackDamage, PlayerKnockback, HitsAmount), `DanoFlashLight`(IsOn), `DanoPropeller`(FlySpeed, IsFlying), `DanoWeaponHandSpawner`(IsProximityMine, IsClaymore)
- `DanoGrenade.Get(g) / DanoGrenade.List / DanoGrenade.Active` — `.Name`, `.Position`, `.ExplosionRadius`, `.IsFragGrenade`, `.IsStunGrenade`, `.StunTime`, `.Velocity`, `.IsActive`
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
- Player: `PlayerSpawnedEvent`, `PlayerDamagingEvent`(Cancel可)→`PlayerDamagedEvent`, `PlayerDiedEvent`
- Weapon: `WeaponFiringEvent`(Cancel可)→`WeaponFiredEvent`, `WeaponReloadingEvent`(Cancel可)→`WeaponReloadedEvent`
- Item: `ItemPickingUpEvent`(Cancel可)→`ItemPickedUpEvent`, `ItemDroppingEvent`(Cancel可)→`ItemDroppedEvent`
- Door: `DoorInteractingEvent`(Cancel可)→`DoorInteractedEvent`
- Chat: `ChatMessageSendingEvent`(Cancel可)→`ChatMessageSentEvent`, `ChatMessageReceivedEvent`
- Game: `RoundStartedEvent`, `RoundEndedEvent`, `MatchEndedEvent`, `SpawnPhaseStartedEvent`, `GameStartedEvent`
- Connection: `PlayerConnectedEvent`, `PlayerDisconnectedEvent`
- Team: `TeamChangedEvent`
- Grenade: `GrenadeExplodedEvent`
- Movement: `PlayerSprintChangedEvent`, `PlayerCrouchChangedEvent`, `PlayerSlideChangedEvent`, `PlayerGroundedChangedEvent`, `PlayerLeanChangedEvent`, `PlayerAimChangedEvent`
- Map: `MapChangedEvent`
- Score: `MatchScoreChangedEvent`, `RoundScoreChangedEvent`
- Pause: `GamePausedEvent`, `GameResumedEvent`

### プラグインシステム
- `Plugin<TConfig>` 基底クラス — `OnEnabled()`, `OnDisabled()`, `Log`, `Logger`, `Config`
- `[DANOPlugin(id, version, author, description, Dependencies)]` 属性
- 依存関係はトポロジカルソートで自動解決
- 優先度付きイベント — `Subscribe<T>(handler, priority)` (小さいほど先に実行)
