# DANO

**STRAFTAT** 用の MOD フレームワーク。BepInEx 5 ベース。

プラグイン開発者が最小限のコードで MOD を作れる環境を提供します。

## 特徴

- **シンプルなプラグインシステム** — `Plugin<TConfig>` を継承して `[DANOPlugin]` 属性を付けるだけ
- **JSON 自動設定** — `PluginConfig` を継承したクラスのプロパティが自動で保存/読み込み
- **イベントシステム** — `EventBus.Subscribe<T>()` でキャンセル可能なゲームイベントを購読
- **UI API** — ヒント表示、テキストラベル、プログレスバー、パネルなどの HUD 要素
- **プレイヤー API** — プレイヤー情報への簡単なアクセス
- **専用 matchmaking pool** — DANO 導入者同士でマッチング

## クイックスタート

### 前提条件

- [BepInEx 5.4.x](https://github.com/BepInEx/BepInEx) が STRAFTAT にインストール済み
- DANO.Core.dll が `STRAFTAT/BepInEx/plugins/DANO/` に配置済み

### プラグインの作成

```csharp
using DANO.API;
using DANO.Events;
using DANO.Plugin;

[DANOPlugin("my-plugin", "1.0.0", "YourName", "プラグインの説明")]
public class MyPlugin : Plugin<MyPlugin.MyConfig>
{
    public override void OnEnabled()
    {
        Log.LogInfo("Hello from MyPlugin!");
        EventBus.Subscribe<PlayerDiedEvent>(OnPlayerDied);
        HudAPI.ShowHint("<color=#00FFFF>MyPlugin loaded!</color>", duration: 4f);
    }

    public override void OnDisabled()
    {
        EventBus.Unsubscribe<PlayerDiedEvent>(OnPlayerDied);
    }

    private void OnPlayerDied(PlayerDiedEvent ev)
    {
        if (Config.AnnounceDeaths)
            HudAPI.LocalMessage($"Player {ev.PlayerId} died.");
    }

    public class MyConfig : PluginConfig
    {
        public bool AnnounceDeaths { get; set; } = true;
    }
}
```

### ビルドと配置

プラグイン DLL を `STRAFTAT/DANO/mods/` に配置すると自動的にロードされます。
設定ファイルは `STRAFTAT/DANO/configs/<plugin-id>.json` に自動生成されます。

## API リファレンス

### イベント

| イベント | 説明 | Cancel 可 |
|----------|------|-----------|
| `PlayerDamagedEvent` | ダメージ発生時（Damage 値を変更可） | Yes |
| `PlayerDiedEvent` | プレイヤー死亡時 | No |
| `WeaponFiredEvent` | 武器発射時 | Yes |
| `RoundStartedEvent` | ラウンド開始時 | No |
| `RoundEndedEvent` | ラウンド終了時 | No |

### HudAPI

```csharp
HudAPI.ShowHint(text, duration, color, fontSize)   // 一時的なヒント表示
HudAPI.Broadcast(text)                              // 全員に見えるメッセージ
HudAPI.LocalMessage(text)                           // 自分だけに見えるメッセージ
HudAPI.CreateTextLabel(anchor, text, fontSize, color)
HudAPI.CreateProgressBar(anchor, width, height, fillColor, bgColor)
HudAPI.CreatePanel(anchor, width, height, color)
```

### PlayerAPI

```csharp
PlayerAPI.All           // 全プレイヤー
PlayerAPI.Local         // ローカルプレイヤー
PlayerAPI.Get(id)       // ID でプレイヤー取得
PlayerAPI.IsAlive(id)   // 生存判定
PlayerAPI.GetTeamId(id) // チーム ID
```

## プロジェクト構成

```
DANO.Core/      フレームワーク本体 → BepInEx/plugins/DANO/
DANO.Template/  プラグイン開発テンプレート → DANO/mods/
```

## ビルド

```bash
dotnet build DANO.Core/DANO.Core.csproj -c Debug
dotnet build DANO.Template/DANO.Template.csproj -c Debug
```

> **Note:** ビルドには参照 DLL が必要です。環境変数でパスを設定できます：
> - `STRAFTAT_DIR` — ゲームルート
> - `STRAFTAT_LIBS` — 参照 DLL 置き場

## ライセンス

MIT
