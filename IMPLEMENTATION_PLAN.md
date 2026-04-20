# DANO 実装計画

# v0.2 機能追加 [完了]

## コンテキスト

DANO v0.1.0 で基盤（プラグインシステム、イベントバス、Harmony パッチ、HUD/Player API、UI）が完成。
v0.2 では未発火イベントの接続、新イベント追加、コマンドシステム、API 拡充、プラグイン改善を段階的に実装する。

---

## Phase 1: 未発火イベントの接続

定義済みだが `Raise()` されていない 2 イベントをパッチで接続する。

### 1A. PlayerSpawnedEvent

| 項目 | 内容 |
|------|------|
| パッチ対象 | `PlayerManager.SpawnPlayer(int, int, Vector3, Quaternion)` (private, 通常メソッド) |
| フック種類 | Postfix |
| 新ファイル | `DANO.Core/Patches/PlayerManagerPatch.cs` |
| 変更ファイル | `DANOLoader.cs` — `ApplyPatches()` に追加 |
| リスク | private メソッドだが publicized DLL でアクセス可。2-param overload と区別するためパラメータ型指定必須 |

### 1B. MatchEndedEvent

| 項目 | 内容 |
|------|------|
| パッチ対象 | `SceneMotor.ServerEndGameScene()` (public, 通常メソッド) |
| フック種類 | Prefix |
| 新ファイル | `DANO.Core/Patches/SceneMotorPatch.cs` |
| 変更ファイル | `DANOLoader.cs` |
| ロジック | `ScoreManager.Instance.Points` から最高スコアのチームを算出 → `MatchEndedEvent(winningTeamId)` |
| 注意 | サーバー（ホスト）側でのみ発火。要ドキュメント |

---

## Phase 2: 新イベント追加

### 2A. チャットイベント

**新ファイル:** `Events/ChatEvents.cs`, `Patches/ChatPatch.cs`

| イベント | パッチ対象 | フック | Cancel可 |
|----------|-----------|--------|----------|
| `ChatMessageSendingEvent` (Message 書換可) | `ChatBroadcast.SendMessage()` | Prefix | ✅ |
| `ChatMessageReceivedEvent` | `ChatBroadcast.OnMessageReceived(Message)` | Prefix | ❌ |

### 2B. 接続/切断イベント

**新ファイル:** `Events/ConnectionEvents.cs`, `Patches/ClientInstancePatch.cs`, `ConnectionMonitor.cs`

| イベント | 検出方法 | 備考 |
|----------|---------|------|
| `PlayerConnectedEvent` (PlayerId, PlayerName, SteamId) | `ClientInstance.RpcLogic___AddNewPlayer_*` Postfix | FishNet ハッシュ名 → TryPatch パターン |
| `PlayerDisconnectedEvent` (PlayerId, PlayerName) | `ConnectionMonitor` で `playerInstances` をポーリング | `OnDisable` は Unity ライフサイクルでパッチ不可 |

### 2C. アイテムイベント

**新ファイル:** `Events/ItemEvents.cs`, `Patches/ItemPatch.cs`

| イベント | パッチ対象 | フック |
|----------|-----------|--------|
| `ItemPickedUpEvent` (Item, WeaponName, IsOwner, RightHand) | `ItemBehaviour.OnGrab(bool, bool)` | Postfix |
| `ItemDroppedEvent` (Item, WeaponName) | `ItemBehaviour.OnDrop(Camera)` | Prefix |

### 2D. チーム変更イベント

**新ファイル:** `Events/TeamEvents.cs`, `Patches/ScoreManagerPatch.cs`

| イベント | パッチ対象 | フック |
|----------|-----------|--------|
| `TeamChangedEvent` (PlayerId, OldTeamId, NewTeamId) | `ScoreManager.SetTeamId(int, int)` | Prefix+Postfix |

---

## Phase 3: コマンドシステム

チャットメッセージの `/` プレフィックスをインターセプトしてプラグインコマンドを実行。

**新ファイル:**
- `Commands/CommandManager.cs` — `Register(name, handler, description)`, `Unregister(name)`, `TryExecute(input)`
- `Commands/CommandContext.cs` — `CommandName`, `Args[]`, `RawArgs`, `Sender`, `Reply(msg)`

**統合:** Phase 2A の `ChatPatch` SendMessage Prefix 内で `/` 判定 → `CommandManager.TryExecute()` → handled なら送信キャンセル

**組み込みコマンド:** `/help` — 登録済みコマンド一覧表示

**プラグイン使用例:**
```csharp
CommandManager.Register("heal", ctx => {
    var hp = PlayerAPI.GetHealth(PlayerAPI.Local);
    if (hp != null) hp.Health = 100;
    ctx.Reply("Healed!");
}, "体力を全回復");
```

---

## Phase 4: API 拡充

### 4A. ItemAPI (`API/ItemAPI.cs`)
- `GetAll()`, `GetHeldItem(player, rightHand)`, `GetWeaponName(item)`, `GetCurrentAmmo(item)`, `GetMaxAmmo(item)`, `IsHeld(item)`, `GetHolder(item)`

### 4B. TeamAPI (`API/TeamAPI.cs`)
- `GetActiveTeams()`, `GetTeamMembers(teamId)`, `GetTeamId(playerId)`, `AreAllies(a, b)`, `IsTeamMode()`

### 4C. ScoreAPI (`API/ScoreAPI.cs`)
- `GetMatchScore(teamId)`, `GetRoundScore(teamId)`, `GetCurrentTake()`, `GetRoundScoreToWin()`, `HasTeamWonRound(teamId)`

---

## Phase 5: プラグイン改善

### 5A. DANOLogger (`Plugin/DANOLogger.cs`)
- `Debug()`, `Info()`, `Warning()`, `Error()` の構造化ラッパー
- `Plugin<T>` に `protected DANOLogger Log` として追加

### 5B. 優先度付きイベント
- `EventBus.Subscribe<T>(handler, priority)` オーバーロード追加
- priority 小 → 先に実行。既存 API は priority=0 のまま互換性維持

### 5C. プラグイン依存関係
- `[DANOPlugin]` に `Dependencies = new[] { "other-mod" }` 追加
- `PluginLoader` でトポロジカルソート。依存先が無ければスキップ+ログ

---

## 実装順序と依存関係

```
Phase 1 (依存なし、ここから開始)
  ├── 1A. PlayerSpawnedEvent
  └── 1B. MatchEndedEvent

Phase 2 (Phase 1 と並行可)
  ├── 2A. ChatEvents
  ├── 2B. ConnectionEvents + ConnectionMonitor
  ├── 2C. ItemEvents
  └── 2D. TeamEvents

Phase 3 (Phase 2A に依存)
  └── CommandSystem

Phase 4 (依存なし、Phase 3 と並行可)
  ├── 4A. ItemAPI
  ├── 4B. TeamAPI
  └── 4C. ScoreAPI

Phase 5 (全フェーズ安定後)
  ├── 5A. DANOLogger
  ├── 5B. Priority EventBus
  └── 5C. Plugin Dependencies
```

---

## 新規ファイル一覧

| ファイル | Phase |
|----------|-------|
| `Patches/PlayerManagerPatch.cs` | 1 |
| `Patches/SceneMotorPatch.cs` | 1 |
| `Events/ChatEvents.cs` | 2 |
| `Events/ConnectionEvents.cs` | 2 |
| `Events/ItemEvents.cs` | 2 |
| `Events/TeamEvents.cs` | 2 |
| `Patches/ChatPatch.cs` | 2 |
| `Patches/ClientInstancePatch.cs` | 2 |
| `Patches/ItemPatch.cs` | 2 |
| `Patches/ScoreManagerPatch.cs` | 2 |
| `ConnectionMonitor.cs` | 2 |
| `Commands/CommandManager.cs` | 3 |
| `Commands/CommandContext.cs` | 3 |
| `API/ItemAPI.cs` | 4 |
| `API/TeamAPI.cs` | 4 |
| `API/ScoreAPI.cs` | 4 |
| `Plugin/DANOLogger.cs` | 5 |

## 変更ファイル

| ファイル | Phase | 変更内容 |
|----------|-------|---------|
| `DANOLoader.cs` | 1-3 | パッチ追加、ConnectionMonitor、CommandManager 初期化 |
| `Events/EventBus.cs` | 5 | 優先度付き Subscribe |
| `Plugin/Plugin.cs` | 5 | DANOLogger プロパティ追加 |
| `Plugin/PluginAttribute.cs` | 5 | Dependencies プロパティ追加 |
| `Plugin/PluginLoader.cs` | 5 | トポロジカルソート追加 |

---

## パッチ一覧

| パッチ対象 | フック | イベント | FishNet RPC? |
|-----------|--------|---------|-------------|
| `PlayerManager.SpawnPlayer(int,int,Vector3,Quaternion)` | Postfix | PlayerSpawnedEvent | No |
| `SceneMotor.ServerEndGameScene()` | Prefix | MatchEndedEvent | No |
| `ChatBroadcast.SendMessage()` | Prefix | ChatMessageSendingEvent + コマンド | No |
| `ChatBroadcast.OnMessageReceived(Message)` | Prefix | ChatMessageReceivedEvent | No |
| `ClientInstance.RpcLogic___AddNewPlayer_*` | Postfix | PlayerConnectedEvent | Yes (ハッシュ名) |
| `ItemBehaviour.OnGrab(bool,bool)` | Postfix | ItemPickedUpEvent | No |
| `ItemBehaviour.OnDrop(Camera)` | Prefix | ItemDroppedEvent | No |
| `ScoreManager.SetTeamId(int,int)` | Prefix+Postfix | TeamChangedEvent | No |

---

## 検証方法

1. `dotnet build DANO.Core/DANO.Core.csproj -c Debug` が成功すること
2. DANO.Template でサンプルプラグインを書き、各イベントの Subscribe + ログ出力で動作確認
3. ゲーム内でチャット送信 → ChatMessageSendingEvent 発火確認
4. `/help` コマンドが動作すること
5. プレイヤー接続/切断でイベント発火確認
6. アイテム拾得/投棄でイベント発火確認

---

## リスク領域

1. **FishNet ハッシュ名** — `TryPatch` パターンで存在確認+フォールバック
2. **切断検出** — ポーリング方式（`ConnectionMonitor`）、フレーム毎の HashSet 比較（プレイヤー数が少ないため負荷は無視できる）
3. **MatchEndedEvent サーバー限定** — ホストでのみ発火。クライアント側は VictoryScene ロード検出で将来対応可能
4. **ChatBroadcast private フィールド** — publicized DLL でアクセス可
5. **Phase 5 EventBus 変更** — 既存 API との後方互換性を維持（デフォルト priority=0）

---

# v0.2.1 EXILED 風ラッパーリファクタ [完了]

## コンテキスト

v0.2.0 のイベント・API は生ゲーム型（ClientInstance, PlayerHealth, ItemBehaviour 等）を直接露出しており、
プラグイン開発者にとって使いにくかった。EXILED の Player/Item パターンに倣いラッパークラスを導入。

## 実装内容

### 新規ファイル
- `API/Player.cs` — ClientInstance ラッパー（キャッシュ付き、Id/Name/Health/Position 等）
- `API/Item.cs` — ItemBehaviour ラッパー（Name/Ammo/Holder 等）

### 変更ファイル
- `Events/PlayerEvents.cs` — 全4イベントをラッパー型に変更
- `Events/ItemEvents.cs` — 全2イベントをラッパー型に変更
- `API/PlayerAPI.cs`, `API/ItemAPI.cs` — ラッパーベースに書き換え
- `Commands/CommandManager.cs`, `Commands/CommandContext.cs` — namespace を `DANO.API` に統合
- `ConnectionMonitor.cs` — 切断時キャッシュ無効化追加
- `DANO.Template/MyPlugin.cs` — 新 API デモに更新

### 注意点
- ゲームに `Player` クラスが存在するため、DANO.Core 内部では `API.Player` でフル修飾が必要
- Commands は `DANO.API` namespace に統合済み（`using DANO.API;` だけで全 API にアクセス可）

---

# v0.2.2 API・ラッパー・イベント拡張 [完了]

## コンテキスト

EXILED 風の基盤が整った後、武器・サーバー・ラウンド・マップ等の追加 API とイベントを大量に実装。

## 新規ファイル

| ファイル | 内容 |
|----------|------|
| `API/DanoWeapon.cs` | Weapon ラッパー（ダメージ、発射レート、精度、リロード等の全プロパティ） |
| `API/ServerAPI.cs` | MaxPlayers, PlayerCount, LobbyName, GameStarted, IsPaused 等 |
| `API/RoundAPI.cs` | CurrentTake, ScoreToWin, AlivePlayers, AliveCount, GetMatchScore 等 |
| `API/MapAPI.cs` | CurrentMap, IsLoading, IsTestMap, Playlist, PlayedMaps |
| `Events/WeaponEvents.cs` | WeaponReloadEvent, MeleeHitEvent |
| `Events/GrenadeEvents.cs` | GrenadeExplodedEvent（位置、半径、種類） |
| `Patches/WeaponPatch.cs` | Weapon.OnReload Postfix, MeleeWeapon.HitServer Prefix |
| `Patches/GrenadePatch.cs` | PhysicsGrenade.HandleExplosion FishNet TryApply パターン |

---

# v0.2.3 Player アクション + GameAPI + ScoreAPI 拡張 [完了]

## コンテキスト

ラッパー・イベントに加え、プレイヤーへのアクション実行メソッドとゲーム制御 API を追加。

## Player アクションメソッド（Player.cs に追加）

| メソッド | 説明 |
|----------|------|
| `Damage(float)` | ダメージを与える |
| `Heal(float)` | 指定量回復 |
| `HealFull()` | 全回復 |
| `Teleport(Vector3)` | 座標テレポート |
| `Teleport(Vector3, Quaternion)` | 座標+回転テレポート |
| `SetTeam(int)` | チーム変更 |
| `Freeze()` | 移動無効化 |
| `Unfreeze()` | 移動再有効化 |
| `Stun(float)` | 一定時間スタン |
| `AddForce(Vector3, float)` | 物理力を加える |
| `Kick(string)` | サーバーからキック |

## 新規ファイル

| ファイル | 内容 |
|----------|------|
| `API/GameAPI.cs` | ForceEndRound, ProgressToNextTake, ResetGame, ScrambleTeams, KickPlayer, GetAliveTeams, AlivePlayers 等 |

## ScoreAPI 追加メソッド

| メソッド | 説明 |
|----------|------|
| `AddMatchPoints(teamId, points)` | マッチポイント加算 |
| `AddRoundScore(playerId, points)` | ラウンドスコア加算 |
| `ResetRound()` | ラウンドスコアリセット |
| `SetRoundIndex(index)` | テイク番号設定 |
| `ResetTeams()` | チーム割り当て全リセット |

## 注意: Template と Assembly-CSharp

Template（プラグイン側）は Assembly-CSharp を参照しない設計。
`Player.Get(id)` は内部で `ClientInstance` を使うため、プラグインからは `PlayerAPI.Get(id)` を使う。
`ev.Player` 等のイベントプロパティ経由で Player を取得する場合はこの問題は起きない。

## 変更ファイル

| ファイル | 変更内容 |
|----------|---------|
| `API/Player.cs` | 移動プロパティ追加（IsSprinting, IsWalking, IsCrouching, IsGrounded, Speed, CanMove 等） |
| `API/Item.cs` | Weapon プロパティ追加（→ DanoWeapon） |
| `DANOLoader.cs` | WeaponReloadPatch, MeleeHitPatch, GrenadePatch.TryApply 登録 |
| `DANO.Template/MyPlugin.cs` | 新 API デモ（/info, /weapon コマンド、リロード・グレネード・接続イベント） |

## パッチ一覧（追加分）

| パッチ対象 | フック | イベント | FishNet RPC? |
|-----------|--------|---------|-------------|
| `Weapon.OnReload()` | Postfix | WeaponReloadEvent | No |
| `MeleeWeapon.HitServer()` | Prefix | MeleeHitEvent | No |
| `PhysicsGrenade.RpcLogic___HandleExplosion_*` | Prefix | GrenadeExplodedEvent | Yes (ハッシュ名) |

---

# v0.2.4 Player 追加メソッド + DanoDoor + ゲームイベント [完了]

## Player 追加メソッド

| メソッド | 説明 |
|----------|------|
| `Respawn()` | リスポーン（ローカルプレイヤーのみ） |
| `PlayAnimation(int)` | アニメーション再生（エモート等） |
| `SetReady(bool)` | レディ状態設定 |

## 新規ファイル

| ファイル | 内容 |
|----------|------|
| `API/DanoDoor.cs` | ドアラッパー（IsOpen, Toggle, Open, Close, List） |
| `Events/DoorEvents.cs` | DoorInteractEvent（Cancel可、操作プレイヤー、開閉前状態） |
| `Patches/DoorPatch.cs` | Door.OnInteract Prefix |
| `Patches/PauseManagerPatch.cs` | PauseManager.InvokeBeforeSpawn Postfix → SpawnPhaseStartedEvent |
| `Patches/GameStartPatch.cs` | GameManager.StartGame Postfix → GameStartedEvent |

## 新規イベント

| イベント | 説明 |
|----------|------|
| `DoorInteractEvent` | ドア操作時（Cancel可、WasOpen で操作前状態を取得） |
| `SpawnPhaseStartedEvent` | ラウンド間のスポーンフェーズ開始時 |
| `GameStartedEvent` | ロビーからゲームへ遷移した時 |

## 注意: FishNet SyncVar プロパティ

Door.SyncAccessor_isOpen は FishNet SyncVar のため C# から直接アクセスできない。
`sync___get_value_isOpen()` / `sync___set_value_isOpen(bool, bool)` メソッドを使う。

---

# v0.2.5 Timer・PlayerAPI 拡張・コマンド権限 [完了]

## 新規ファイル

| ファイル | 内容 |
|----------|------|
| `API/DanoTimer.cs` | 遅延実行・繰り返しタイマーユーティリティ（After, Every, AfterThenEvery, NextFrame, Cancel） |

## 変更ファイル

| ファイル | 変更内容 |
|----------|---------|
| `API/PlayerAPI.cs` | GetByName, GetBySteamId, Count, Alive, GetTeamMembers, IsHost 追加 |
| `API/Player.cs` | IsHost プロパティ追加 |
| `Commands/CommandManager.cs` | Register に hostOnly パラメータ追加、TryExecute で権限チェック、help に [HOST] マーク |
| `Commands/CommandInfo.cs` | HostOnly プロパティ追加 |
| `ConnectionMonitor.cs` | DanoTimer.Tick() を Update に追加 |
| `DANO.Template/MyPlugin.cs` | /find コマンド、Timer デモ、hostOnly フラグのデモ追加 |

## DanoTimer API

| メソッド | 説明 |
|----------|------|
| `After(delay, callback)` | 指定秒後に一度だけ実行 |
| `Every(interval, callback, count)` | 指定間隔で繰り返し（-1=無限） |
| `AfterThenEvery(delay, interval, callback, count)` | 遅延開始+繰り返し |
| `NextFrame(callback)` | 次フレームで実行 |
| `Cancel(id) / CancelAll()` | タイマーキャンセル |

## コマンド権限システム

- `Register(name, handler, desc, hostOnly: true)` でホスト限定コマンドを登録
- 非ホストが実行するとエラーメッセージを表示
- `/help` で `[HOST]` マークを表示

---

# v0.2.6 チャット・イベント検出の大幅修正 [完了]

## 発見: FishNet [ServerRpc] は Harmony を完全にバイパスする

FishNet の `[ServerRpc]` / `[ObserversRpc]` メソッドは Harmony パッチが「適用成功」と報告されるが、
**実行時に Prefix/Postfix が一切発火しない**。`RpcLogic___` 生成メソッドを直接パッチしても同様。
FishNet の IL 書き換えが Harmony のトランポリンをバイパスするため。

これにより、以下のパッチは**名目上は適用されているが実際には動かない**:
- `PlayerHealth.RpcLogic___RemoveHealth_431000436`
- `PlayerHealth.RpcLogic___ChangeKilledState_1140765316`

Unity ライフサイクルメソッド（`Start`, `Update`）も内部ネイティブキャッシュ経由のため Harmony パッチが発火しない:
- `LobbyChatUILogic.Start` Postfix — 適用成功するが発火せず

## アーキテクチャ方針: Harmony → ポーリング/直接フック

STRAFTAT では **状態ポーリング** と **直接フック** が最も信頼性の高い検出方法。

| 検出対象 | 旧方式（Harmony） | 新方式 |
|----------|------------------|--------|
| チャット入力 | ChatBroadcast.SendMessage Prefix | `TMP_InputField.onSubmit` 直接リスナー |
| ダメージ | PlayerHealth.RemoveHealth Prefix | `PlayerHealth.health` 毎フレームポーリング |
| 死亡 | PlayerHealth.ChangeKilledState Prefix | `isKilled \|\| hp <= 0` ポーリング |
| 切断 | （元からポーリング） | `playerInstances` ポーリング |

通常メソッド（`Gun.Fire`, `ItemBehaviour.OnGrab`, `Door.OnInteract` 等）は Harmony で正常にパッチ可能なはず（要検証）。

## 実装内容

### ConnectionMonitor.cs — 中核コンポーネントに昇格

| 機能 | 説明 |
|------|------|
| F1 コンソール | OnGUI ベース、チャット非依存のコマンド入力（`/` プレフィックス自動補完） |
| チャット onSubmit フック | `LobbyChatUILogic.inputField` を Traverse で取得、`onSubmit.AddListener` で直接フック |
| ヘルス/死亡ポーリング | `FindObjectsOfType<PlayerHealth>()` を 0.5 秒キャッシュ、HP 減少とフラグ変化を検出 |
| `/diag` コマンド | シーン内コンポーネントの存在状態をリアルタイム診断 |

### ヘルスモニタの注意点

- ゲームは死亡時に HP を **-2000** に設定する → ダメージ量 >= 500f はスキップ
- `ClientInstance.GetComponentInChildren<PlayerHealth>()` は **null を返す**（別 GameObject 階層）
  → `FindObjectsOfType<PlayerHealth>()` で全 PlayerHealth を取得する

### チャットフックの注意点

- `LobbyChatUILogic` はロビーでもゲーム内でも存在し続ける
- `ChatBroadcast` は演習モードでは存在しない
- inputField が破棄された場合（シーン遷移等）は自動で再フックする

## 組み込みコマンド（追加分）

| コマンド | 説明 |
|----------|------|
| `/diag` | シーン内コンポーネント診断（MatchChat, ChatBroadcast, PlayerHealth, ChatHook 状態等） |
| `/patchtest` | 自分に 1 ダメージを与えてパッチ発火をテスト（デバッグ用） |

## 未検証・次セッションの課題

1. ~~**不要な Harmony パッチの整理**~~ → v0.3.0 で対応済み
2. ~~**チャット重複イベント防止**~~ → v0.3.0 で対応済み
3. **通常 Harmony パッチの動作確認** — 全パッチに初回発火ログを追加済み。ゲーム内で以下を実行して BepInEx ログを確認:
   - 射撃 → `[GunFirePatch] Prefix 初回発火確認！`
   - アイテム拾得/投棄 → `[ItemGrabPatch]` / `[ItemDropPatch]`
   - リロード → `[WeaponReloadPatch]`
   - 近接攻撃 → `[MeleeHitPatch]`
   - ドア操作 → `[DoorInteractPatch]`
   - スポーン → `[PlayerManagerPatch]`
   - チーム変更 → `[ScoreManagerPatch]`
   - ゲーム開始 → `[GameStartPatch]`
   - スポーンフェーズ → `[SpawnPhasePatch]`
   - マッチ終了 → `[SceneMotorPatch]`
   - ラウンド開始/終了 → `[GameManagerPatch] RoundSpawn/EndRound`（FishNet RPC — 動かない可能性）
   - チャット送信 → `[ChatSendPatch]` / `[ChatBroadcastSendPatch]`
4. **動かないパッチのポーリング代替** — 上記で発火しないものがあれば、ConnectionMonitor のポーリング方式に切り替える
5. **演習モード vs マルチプレイの差異** — 演習モード（`testMap=True`）ではラウンドフロー（StartGame, InvokeBeforeSpawn）がスキップされる。マルチプレイでは動く可能性あり

---

# v0.3.0 パッチ整理・重複防止・発火検証準備 [完了]

## コンテキスト

v0.2.6 で FishNet RPC が Harmony をバイパスすることが判明し、ポーリング方式に切り替えた。
しかし、古い Harmony パッチが残ったままだったため、デッドコードの削除・重複イベントの防止・
未検証パッチの発火確認準備を実施。

## 実施内容

### 削除した Harmony パッチ登録

| パッチ | 理由 |
|--------|------|
| `PlayerHealthPatches.TryApply()` | FishNet [ServerRpc] — ConnectionMonitor ポーリングで代替済み |
| `ChatInitPatch` (LobbyChatUILogic.Start) | Unity ライフサイクル — ConnectionMonitor で直接フック済み |
| `MatchChatPatch` (MatchChat.Update) | Unity ライフサイクル — ConnectionMonitor onSubmit で代替済み |

### 削除したデッドコード

| コード | 理由 |
|--------|------|
| `ChatPatches._lobbyManager` / `SendLobbyChat()` | ChatInitPatch 削除で設定されなくなり、どこからも呼ばれていない |
| `ChatInitPatch` クラス | DANOLoader から登録削除済み |
| `MatchChatPatch` クラス | DANOLoader から登録削除済み |

### チャット重複イベント防止

- `ChatSendPatch` / `ChatBroadcastSendPatch` から `ChatMessageSendingEvent` の発火を削除
- コマンドインターセプトのバックアップのみ残す
- `ChatMessageSendingEvent` は `ConnectionMonitor.OnChatSubmit` が一元管理

### FishNet RPC パッチ注記

- `GameManagerPatch`, `ClientInstancePatch`, `GrenadePatch` に「FishNet RPC のため発火しない可能性」の注記を追加
- ログメッセージにもその旨を明記

### 全パッチに初回発火ログ追加

対象: GunFirePatch, ItemGrabPatch, ItemDropPatch, WeaponReloadPatch, MeleeHitPatch,
DoorInteractPatch, PlayerManagerPatch, ScoreManagerPatch, SceneMotorPatch,
GameStartPatch, SpawnPhasePatch, ChatSendPatch, ChatBroadcastSendPatch,
GameManagerPatch (RoundSpawn/EndRound), ClientInstancePatch, GrenadePatch

各パッチの Prefix/Postfix に `_logged` フラグ付きの初回発火ログを追加。
ゲーム起動後に BepInEx ログで「初回発火確認！」メッセージの有無を確認することで、
どのパッチが実際に動作しているか判別可能。

→ **結果: 全パッチが発火しないことを確認（通常メソッド含む）。v0.4.0 で Harmony 全廃へ。**

---

# v0.4.0 Harmony 全廃 — ポーリング/直接フック方式への全面切り替え [完了]

## コンテキスト

v0.3.0 のゲーム内テストで、Harmony パッチは STRAFTAT では**通常メソッドを含め全て実行時に発火しない**
ことが確認された。パッチ「適用成功」と報告されるが、FishNet の IL 書き換えやゲーム側のアセンブリ処理が
Harmony のトランポリンを無効化していると推定。

一方、以下の方式は動作確認済み：
- `TMP_InputField.onSubmit.AddListener` — 直接フック ✅
- `FindObjectsOfType` + 状態ポーリング — HealthMonitor ✅
- `ClientInstance.playerInstances` 比較 — ConnectionMonitor ✅

**方針:** Harmony パッチを全て削除し、全イベント検出をポーリング/直接フックに切り替え。

## 削除したファイル

`Patches/` ディレクトリ全体（14ファイル）:
ChatPatch.cs, ClientInstancePatch.cs, DoorPatch.cs, GameManagerPatch.cs,
GameStartPatch.cs, GrenadePatch.cs, GunPatch.cs, ItemPatch.cs,
PauseManagerPatch.cs, PlayerHealthPatch.cs, PlayerManagerPatch.cs,
SceneMotorPatch.cs, ScoreManagerPatch.cs, WeaponPatch.cs

## ConnectionMonitor — 中核コンポーネントに昇格

全イベント検出を `ConnectionMonitor` のポーリング/直接フックに統合。

### オブジェクトキャッシュ（0.5秒毎に一括更新）

`FindObjectsOfType` は重いため、全型を 0.5 秒間隔でキャッシュ:
PlayerHealth[], ItemBehaviour[], Weapon[], Door[], PhysicsGrenade[]

### ポーリングモニタ一覧

| モニタ | 監視対象 | 検出方法 |
|--------|---------|---------|
| TickHealthMonitor | `PlayerHealth.health`, `isKilled` | HP 減少 → ダメージ、isKilled/HP≤0 → 死亡 |
| TickConnectionMonitor | `ClientInstance.playerInstances` | 新規 ID → 接続、消失 ID → 切断 |
| TickItemMonitor | `ItemBehaviour.isTaken` | false→true → 拾得、true→false → 投棄 |
| TickWeaponMonitor | `Weapon.currentAmmo`, `isReloading` | 弾数減少 → 射撃、isReloading 遷移 → リロード |
| TickDoorMonitor | `Door.isOpen` (SyncVar) | 開閉状態変化 → ドア操作 |
| TickTeamMonitor | `ScoreManager.PlayerIdToTeamId` | 値変化 → チーム変更 |
| TickSpawnMonitor | `GameManager.alivePlayers` | 新規 ID → スポーン |
| TickRoundMonitor | TakeIndex, roundWasWon, gameStarted 等 | 各状態の遷移を検出 |
| TickGrenadeMonitor | `PhysicsGrenade.enabled` | enabled→false → 爆発 |

### 直接フック

| フック | 方式 |
|--------|------|
| チャット送信 | `TMP_InputField.onSubmit.AddListener` (既存) |
| チャット受信 | `lobbyManager.evtChatMsgReceived.AddListener` (新規) |

### Cancel（巻き戻し）方式

ポーリングは事後検出のため、Cancel は「状態巻き戻し」で実現:

| イベント | 巻き戻し処理 |
|----------|-------------|
| PlayerDamagedEvent | `ph.health = prevHp` で HP 回復 |
| WeaponFiredEvent | `weapon.currentAmmo = prevAmmo` で弾数復元 |
| DoorInteractEvent | `door.sync___set_value_isOpen(wasOpen, true)` で開閉復元 |
| ChatMessageSendingEvent | `inputField.text = ""` でテキストクリア（直接フック） |

### 削除したイベント

| イベント | 理由 |
|----------|------|
| MeleeHitEvent | ヒット判定が瞬間的で状態が残らず、ポーリングでの信頼性ある検出方法がない |

## 変更ファイル

| ファイル | 変更内容 |
|----------|---------|
| `DANOLoader.cs` | ApplyPatches() 全削除、Harmony 使用削除、バージョン 0.4.0 |
| `ConnectionMonitor.cs` | 全面書き換え: 9 つのポーリングモニタ + チャット受信フック |
| `Events/PlayerEvents.cs` | WeaponFiredEvent コンストラクタ変更（ポーリング用）、Cancel→巻き戻し方式 |
| `Events/DoorEvents.cs` | コンストラクタ変更（ポーリング用）、Cancel→巻き戻し方式 |
| `Events/ItemEvents.cs` | コンストラクタ変更（ポーリング用） |
| `Events/WeaponEvents.cs` | MeleeHitEvent 削除 |
| `CLAUDE.md` | Harmony 不使用の注記、アーキテクチャ更新 |
| `DANO.Template/MyPlugin.cs` | DoorInteractEvent.Player 参照修正 |

---

# v0.4.1 Harmony 再有効化（ハイブリッド方式） [完了]

## コンテキスト

v0.4.0 で Harmony を全廃したが、他の MOD（Genehmigt 等）が PatchAll() + `[HarmonyPatch]` 属性方式で正常動作していることを発見。
v0.3.0 で失敗したのは手動 `harmony.Patch()` 方式のみで、属性方式は問題なかった。

## 判明した制約

- **PatchAll() + 属性方式** → 動作する
- **手動 harmony.Patch()** → 発火しない（v0.3.0 で確認済み）
- **ServerRpc メソッド** → FishNet IL 書き換えで Harmony 不可（ポーリング維持）
- **private メソッド**（Gun.Fire 等）→ Mono JIT 最適化で発火しない場合あり
- **ServerRpc(RunLocally=true)** → 状態変更は Update() 後に反映（同フレーム比較不可、フレーム間比較必要）

## 武器クラス階層の発見

全 14 サブクラスが `Weapon` を直接継承（`Gun` からの派生ではない）:
`Gun`, `Shotgun`, `Minigun`, `ChargeGun`, `BeamGun`, `LargeRaycastGun`, `DualLauncher`, `BumpGun`, `RepulsiveGun`, `Taser`, `MeleeWeapon`, `FlashLight`, `Propeller`, `WeaponHandSpawner`

→ `Gun.Update()` パッチでは Gun のみ検出、他のサブクラス（Shotgun 等）は漏れる
→ `Weapon.WeaponUpdate()` をパッチして全サブクラスを一括検出

## 変更内容

| ファイル | 変更内容 |
|----------|---------|
| `DANOLoader.cs` | Harmony PatchAll() 追加、診断ログ |
| `Patches/HarmonyPatches.cs` | 新規: ItemOnGrab/OnDrop, WeaponUpdate(射撃+リロード), DoorInteract パッチ |
| `ConnectionMonitor.cs` | アイテム/武器/ドアのポーリング無効化（Harmony に移行）、コメント更新 |
| `API/DanoWeapon.cs` | IsFirearm, WeaponType, ReserveAmmo, ChargedBullets 追加、Ammo を reloadWeapon 対応 |
| `DANO.Template/MyPlugin.cs` | WeaponFired/ItemDropped イベント、/guntest /eventtest コマンド追加 |
| `CLAUDE.md` | ハイブリッド方式、武器階層、Harmony 制約を文書化 |

---

# DANOUE Ultimate Edition — CustomItems / CustomRoles

## コンテキスト

DANOの公開APIを基盤として、EXILED の `CustomItem` / `CustomRole` 相当の上位レイヤーライブラリを追加する。
別プロジェクト（`DANOUE.CustomItems`, `DANOUE.CustomRoles`）として管理し、DANOプラグイン開発者が
依存ライブラリとして利用する。

## アーキテクチャ

| 項目 | 設計 |
|------|------|
| 登録方式 | `Register<T>()` / `RegisterAll(Assembly)` の両対応 |
| イベント購読 | 初回 `Register<T>()` 時にEventBusへ遅延サブスクライブ（lazy-init） |
| アイテム追跡 | `Dictionary<ItemBehaviour, CustomItem>`（Unity null チェック定期プルーニング） |
| ロール追跡 | `Dictionary<int, CustomRole>`（プレイヤーID） |
| 武器イベント制約 | `OnFiring/OnFired/OnReloading/OnReloaded` はFishNet制約によりローカルプレイヤーのみ発火 |
| HUD統合 | `PickupHint`（拾得時）/ `SelectHint`（手持ち時）/ ロール割当ヒント |
| 出力先 | `STRAFTAT\BepInEx\plugins\DANO\DANOUE.CustomItems.dll` / `DANOUE.CustomRoles.dll` |

## 新規ファイル

| ファイル | 内容 |
|----------|------|
| `DANOUE.CustomItems/DANOUE.CustomItems.csproj` | プロジェクト定義（ProjectReference: DANO.Core） |
| `DANOUE.CustomItems/CustomItem.cs` | `CustomItem` 抽象基底クラス |
| `DANOUE.CustomRoles/DANOUE.CustomRoles.csproj` | プロジェクト定義（ProjectReference: DANO.Core） |
| `DANOUE.CustomRoles/CustomRole.cs` | `CustomRole` 抽象基底クラス |

## CustomItem API 概要

```csharp
public abstract class CustomItem
{
    // 必須: Id, Name, BaseWeaponName
    // 任意: Description, PickupHint, SelectHint

    // 登録: CustomItem.Register<MyItem>() / RegisterAll(assembly)
    // 配布: ci.Give(player) / ci.Spawn(position)
    // クエリ: CustomItem.Check(item) / TryGet(item, out ci) / Get<T>()

    // フック (virtual):
    //   OnAcquired / OnReleased / OnPickingUp / OnPickedUp
    //   OnDropping / OnDropped / OnFiring* / OnFired* / OnReloading* / OnReloaded*
    //   OnOwnerDamaging / OnOwnerDying / OnRoundStarted
    //   * = ローカルプレイヤーのみ（FishNet制約）
}
```

## CustomRole API 概要

```csharp
public abstract class CustomRole
{
    // 必須: Id, RoleName
    // 任意: Description, TeamId, MaxHealth, SpawnItems, KeepRoleOnRoundReset, RoleColor

    // 登録: CustomRole.Register<MyRole>() / RegisterAll(assembly)
    // 割当: role.Assign(player) / role.Remove(player) / role.RemoveAll()
    // クエリ: CustomRole.Check(player) / TryGet(player, out role) / Get<T>() / role.GetPlayers()

    // フック (virtual):
    //   OnRoleAssigned / OnRoleRemoved / OnSpawned / OnDamaging
    //   OnDamaged / OnDying / OnRoundStarted / OnRoundEnded / OnTeamChanged
}
```
