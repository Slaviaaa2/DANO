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

1. **通常 Harmony パッチの動作確認** — `Gun.Fire`, `ItemBehaviour.OnGrab/OnDrop`, `Weapon.OnReload`, `MeleeWeapon.HitServer`, `Door.OnInteract` は非 RPC メソッドなので Harmony で動くはず。ゲーム内で射撃・アイテム拾得・ドア操作して確認する
2. **上記が動かない場合** — ポーリング方式への拡張を検討
3. **不要な Harmony パッチの整理** — 動かないパッチ（PlayerHealth RpcLogic, LobbyChatUILogic.Start 等）を除去してコード簡潔化
4. **演習モード vs マルチプレイの差異** — 演習モード（`testMap=True`）ではラウンドフロー（StartGame, InvokeBeforeSpawn）がスキップされる。マルチプレイでは動く可能性あり
