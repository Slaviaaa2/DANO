using System.Collections.Generic;
using DANO.API;
using DANO.Events;
using TMPro;
using UnityEngine;
using Input = UnityEngine.Input;

namespace DANO
{
    /// <summary>
    /// 毎フレーム更新される自前コンポーネント。
    /// - F1 キーで DANO コンソール（チャット非依存コマンド入力）
    /// - チャット入力フィールドへの onSubmit フック（Harmony 不要）
    /// - プレイヤー切断ポーリング検出
    /// - DanoTimer 駆動
    /// </summary>
    internal class ConnectionMonitor : MonoBehaviour
    {
        internal static ConnectionMonitor Instance { get; private set; }

        private readonly Dictionary<int, string> _knownPlayers = new Dictionary<int, string>();

        // F1 コンソール
        private bool _consoleOpen;
        private string _consoleInput = "";

        // チャット入力フィールド直接フック
        private TMP_InputField _chatInputField;
        private bool _chatHooked;

        // ヘルス変化ポーリング（FishNet RPC パッチが効かないため）
        private readonly Dictionary<int, float> _lastHealth = new Dictionary<int, float>();
        private readonly Dictionary<int, bool> _lastKilled = new Dictionary<int, bool>();

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            DanoTimer.Tick();

            // F1 でコンソールトグル
            if (Input.GetKeyDown(KeyCode.F1))
            {
                _consoleOpen = !_consoleOpen;
            }

            // チャット入力フィールドの onSubmit に直接フック
            // フック済みでもフィールドが破棄されていたら再フック
            if (!_chatHooked || _chatInputField == null)
            {
                _chatHooked = false;
                TryHookChatInput();
            }

            TickHealthMonitor();
            TickConnectionMonitor();
        }

        // ─── チャット入力フィールド直接フック ───

        private void TryHookChatInput()
        {
            // ロビー: LobbyChatUILogic の inputField
            var lobbyChat = Object.FindObjectOfType<HeathenEngineering.DEMO.LobbyChatUILogic>();
            if (lobbyChat != null)
            {
                // inputField は private なので Traverse で取得
                var field = HarmonyLib.Traverse.Create(lobbyChat).Field("inputField").GetValue<TMP_InputField>();
                if (field != null && field != _chatInputField)
                {
                    _chatInputField = field;
                    _chatInputField.onSubmit.AddListener(OnChatSubmit);
                    _chatHooked = true;
                    DANOLoader.Log.LogInfo("[DANO] ロビーチャット inputField にフック完了");
                    return;
                }
            }

            // ゲーム内: ChatInputField2 (MatchChat / ChatBroadcast が使う)
            var chatObj = GameObject.Find("ChatInputField2");
            if (chatObj != null)
            {
                var field = chatObj.GetComponent<TMP_InputField>();
                if (field != null && field != _chatInputField)
                {
                    _chatInputField = field;
                    _chatInputField.onSubmit.AddListener(OnChatSubmit);
                    _chatHooked = true;
                    DANOLoader.Log.LogInfo("[DANO] ゲーム内チャット inputField にフック完了");
                }
            }
        }

        private void OnChatSubmit(string rawText)
        {
            var text = rawText?.Trim() ?? "";
            if (string.IsNullOrEmpty(text)) return;

            DANOLoader.Log.LogInfo($"[DANO ChatHook] 入力: \"{text}\"");

            // コマンドインターセプト
            if (text.StartsWith("/") && CommandManager.TryExecute(text))
            {
                // テキストをクリア（次フレームで反映）
                if (_chatInputField != null)
                    _chatInputField.text = "";
                return;
            }

            // チャット送信イベント
            var username = ClientInstance.Instance?.PlayerName ?? "";
            var ev = new ChatMessageSendingEvent(username, text);
            EventBus.Raise(ev);

            if (ev.Cancel && _chatInputField != null)
                _chatInputField.text = "";
            else if (ev.Message != text && _chatInputField != null)
                _chatInputField.text = ev.Message;
        }

        // ─── シーン診断（/diag コマンドから呼ばれる） ───

        internal static void RunDiagnostics(CommandContext ctx)
        {
            var lines = new List<string> { "<color=#00FFFF>=== DANO シーン診断 ===</color>" };

            void Check(string name, Object obj, string extra = "")
            {
                var status = obj != null ? $"<color=#00FF00>found</color>{extra}" : "<color=#FF4444>NOT FOUND</color>";
                lines.Add($"  {name}: {status}");
                DANOLoader.Log.LogInfo($"  {name}: {(obj != null ? $"found{extra}" : "NOT FOUND")}");
            }

            DANOLoader.Log.LogInfo("=== DANO シーン診断 ===");

            var matchChat = Object.FindObjectOfType<MatchChat>();
            Check("MatchChat", matchChat, matchChat != null ? $" (enabled={matchChat.enabled})" : "");

            var chatBroadcast = Object.FindObjectOfType<ChatBroadcast>();
            Check("ChatBroadcast", chatBroadcast, chatBroadcast != null ? $" (enabled={chatBroadcast.enabled})" : "");

            var lobbyChat = Object.FindObjectOfType<HeathenEngineering.DEMO.LobbyChatUILogic>();
            Check("LobbyChatUILogic", lobbyChat);

            var chatInput = GameObject.Find("ChatInputField2");
            Check("ChatInputField2", chatInput, chatInput != null ? $" (active={chatInput.activeSelf})" : "");

            var ci = ClientInstance.Instance;
            Check("ClientInstance", ci, ci != null ? $" ({ci.PlayerName})" : "");

            var ph = Object.FindObjectOfType<PlayerHealth>();
            Check("PlayerHealth", ph);

            var gm = Object.FindObjectOfType<GameManager>();
            Check("GameManager", gm);

            var pm = Object.FindObjectOfType<PauseManager>();
            Check("PauseManager", pm);

            // チャットフック状態
            var inst = Instance;
            var hookStatus = inst != null && inst._chatHooked ? "<color=#00FF00>hooked</color>" : "<color=#FF8800>not hooked</color>";
            var fieldStatus = inst?._chatInputField != null ? $" → {inst._chatInputField.gameObject.name}" : "";
            lines.Add($"  ChatHook: {hookStatus}{fieldStatus}");
            DANOLoader.Log.LogInfo($"  ChatHook: {(inst != null && inst._chatHooked ? "hooked" : "not hooked")}{fieldStatus}");

            lines.Add("<color=#00FFFF>=== 診断完了 ===</color>");
            DANOLoader.Log.LogInfo("=== 診断完了 ===");

            ctx.Reply(string.Join("\n", lines));
        }

        // ─── F1 コンソール (OnGUI) ───

        private void OnGUI()
        {
            if (!_consoleOpen) return;

            var boxRect = new Rect(10, 10, 500, 40);
            GUI.Box(boxRect, "");

            GUI.SetNextControlName("DANOConsole");
            _consoleInput = GUI.TextField(new Rect(15, 17, 420, 26), _consoleInput);
            GUI.FocusControl("DANOConsole");

            if (GUI.Button(new Rect(440, 15, 60, 28), "Run") ||
                (Event.current.isKey && Event.current.keyCode == KeyCode.Return && GUI.GetNameOfFocusedControl() == "DANOConsole"))
            {
                var text = _consoleInput.Trim();
                if (!string.IsNullOrEmpty(text))
                {
                    DANOLoader.Log.LogInfo($"[DANO Console] 入力: \"{text}\"");
                    var cmd = text.StartsWith("/") ? text : "/" + text;
                    if (CommandManager.TryExecute(cmd))
                    {
                        DANOLoader.Log.LogInfo("[DANO Console] コマンド実行成功");
                    }
                    else
                    {
                        DANOLoader.Log.LogInfo("[DANO Console] 未登録コマンド");
                        HudAPI.LocalMessage($"<color=#FF4444>未登録: {text}</color>");
                    }
                }
                _consoleInput = "";
            }

            if (Event.current.isKey && Event.current.keyCode == KeyCode.Escape)
                _consoleOpen = false;
        }

        // ─── ヘルス/死亡ポーリング検出 ───

        private PlayerHealth[] _healthCache = System.Array.Empty<PlayerHealth>();
        private float _healthCacheTimer;

        private void TickHealthMonitor()
        {
            // PlayerHealth の検索は重いので0.5秒ごとにキャッシュ更新
            _healthCacheTimer += Time.deltaTime;
            if (_healthCacheTimer > 0.5f || _healthCache.Length == 0)
            {
                _healthCacheTimer = 0f;
                _healthCache = Object.FindObjectsOfType<PlayerHealth>();
            }

            foreach (var ph in _healthCache)
            {
                if (ph == null) continue;

                int id = ph.GetInstanceID();
                float currentHp = ph.health;
                bool currentKilled = ph.isKilled;

                // ダメージ検出
                if (_lastHealth.TryGetValue(id, out float prevHp))
                {
                    if (currentHp < prevHp)
                    {
                        float damage = prevHp - currentHp;

                        // HP が大幅に下がった場合（-2000等）は死亡処理の一環なのでスキップ
                        if (damage < 500f)
                        {
                            DANOLoader.Log.LogInfo($"[HealthMonitor] ダメージ検出: {prevHp} → {currentHp} (damage={damage})");
                            var ev = new PlayerDamagedEvent(ph, damage, ph.killer);
                            EventBus.Raise(ev);
                        }
                    }
                }
                _lastHealth[id] = currentHp;

                // 死亡検出（isKilled フラグ or HP が 0 以下になった瞬間）
                bool wasDead = _lastKilled.TryGetValue(id, out bool prevKilled) && prevKilled;
                bool isDead = currentKilled || currentHp <= 0f;

                if (isDead && !wasDead)
                {
                    DANOLoader.Log.LogInfo("[HealthMonitor] 死亡検出");
                    var ev = new PlayerDiedEvent(ph, ph.killer);
                    EventBus.Raise(ev);
                }
                _lastKilled[id] = isDead;
            }
        }

        // ─── 切断検出 ───

        private void TickConnectionMonitor()
        {
            var current = ClientInstance.playerInstances;
            if (current == null) return;

            foreach (var kvp in current)
            {
                if (!_knownPlayers.ContainsKey(kvp.Key))
                    _knownPlayers[kvp.Key] = kvp.Value?.PlayerName ?? "";
            }

            if (_knownPlayers.Count > current.Count)
            {
                var disconnected = new List<int>();
                foreach (var kvp in _knownPlayers)
                {
                    if (!current.ContainsKey(kvp.Key))
                        disconnected.Add(kvp.Key);
                }

                foreach (int playerId in disconnected)
                {
                    var playerName = _knownPlayers[playerId];
                    _knownPlayers.Remove(playerId);
                    API.Player.Invalidate(playerId);
                    EventBus.Raise(new PlayerDisconnectedEvent(playerId, playerName));
                }
            }
        }
    }
}
