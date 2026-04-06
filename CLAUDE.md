# DANO — STRAFTAT MOD フレームワーク

EXILED (SCP:SL) に触発された、STRAFTAT 用の BepInEx 5 ベース MOD フレームワーク。
プラグイン開発者は `Plugin<TConfig>` を継承し `[DANOPlugin]` 属性を付けるだけで MOD が作れる。

## プロジェクト構成

```
DANO.sln
├── DANO.Core/          ← フレームワーク本体（BepInEx プラグイン）
│   ├── DANOLoader.cs       エントリーポイント（BepInPlugin）
│   ├── Plugin/             プラグインシステム（Plugin<T>, PluginLoader, IPlugin, PluginConfig, DANOPluginAttribute）
│   ├── Events/             イベントバス（EventBus, PlayerEvents, GameEvents）
│   ├── Patches/            Harmony パッチ（PlayerHealth, Gun, GameManager）
│   ├── API/                公開 API（HudAPI, PlayerAPI）
│   └── UI/                 UI システム（DANOCanvas, HintController, Elements/）
└── DANO.Template/      ← プラグイン開発者向けテンプレート
    └── MyPlugin.cs         サンプルプラグイン
```

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
- **ComputerysModdingUtilities** — `[assembly: StraftatMod(isVanillaCompatible: true)]` でバニラ matchmaking 互換

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

## プラグイン初期化フロー

```
1. BepInEx Chainloader → DANOLoader.Awake()
   ├── EventBus.Initialize()
   ├── DANOCanvas / HintController 作成
   ├── Harmony パッチ適用（個別 try-catch）
   ├── PluginLoader.ScanAndPrepare() — DANO/mods/ の DLL をスキャン、インスタンス化
   └── DANOSentinel GameObject 作成（HideFlags + DontDestroyOnLoad）

2. DANOSentinel.Update() — SteamLobby.Instance をポーリング
   └── 検出 → PluginLoader.TryEnableAll()
       └── 各プラグインの InternalEnable() → OnEnabled()
```

## 公開 API（プラグイン開発者向け）

- `EventBus.Subscribe<T>() / Unsubscribe<T>() / Raise<T>()`
- `HudAPI.ShowHint() / Broadcast() / LocalMessage() / CreateTextLabel() / CreateProgressBar() / CreatePanel()`
- `PlayerAPI.All / Local / Get(id) / GetHealth() / GetController() / IsAlive() / GetTeamId()`
- イベント: `PlayerDamagedEvent`(Cancel可), `PlayerDiedEvent`, `WeaponFiredEvent`(Cancel可), `RoundStartedEvent`, `RoundEndedEvent`

## 今後の課題

- MatchEndedEvent の実装
- Harmony パッチが効かないライフサイクルメソッドの代替手段の確立
- プラグインのホットリロード
- より多くのゲームイベント（チャット、アイテムピックアップ等）
- DANO 専用 NuGet パッケージ化でプラグイン開発をさらに簡素化
