using System.Collections.Generic;
using DANO.API;
using DANO.Events;
using HeathenEngineering.SteamworksIntegration;
using TMPro;
using UnityEngine;
using Input = UnityEngine.Input;

namespace DANO
{
    /// <summary>
    /// 毎フレーム更新される中核モニタリングコンポーネント。
    /// Harmony PatchAll() + ポーリング/直接フックのハイブリッド方式でイベント検出を行う。
    ///
    /// Harmony パッチ（HarmonyPatches.cs）:
    /// - アイテム拾得/投棄: ItemBehaviour.OnGrab / OnDrop
    /// - 武器射撃/リロード: Weapon.WeaponUpdate (全サブクラス対応)
    /// - ドア開閉: Door.OnInteract (Cancel 可)
    ///
    /// ポーリング/直接フック（本クラス）:
    /// - F1 コンソール（チャット非依存コマンド入力）
    /// - チャット入力フィールドへの onSubmit 直接フック
    /// - チャット受信の evtChatMsgReceived 直接フック
    /// - プレイヤー接続/切断ポーリング
    /// - ヘルス/死亡ポーリング（ServerRpc のため Harmony 不可）
    /// - チーム変更ポーリング
    /// - スポーン検出ポーリング
    /// - ラウンド/ゲーム状態ポーリング
    /// - グレネード爆発ポーリング
    /// - DanoTimer 駆動
    /// </summary>
    internal class ConnectionMonitor : MonoBehaviour
    {
        internal static ConnectionMonitor Instance { get; private set; }

        // ─── チャット入力フィールド直接フック ───
        private TMP_InputField _chatInputField;
        private bool _chatHooked;

        // ─── チャット受信直接フック ───
        private bool _chatReceiveHooked;
        private LobbyManager _lobbyManager;

        // ─── F1 コンソール ───
        private bool _consoleOpen;
        private string _consoleInput = "";

        // ─── オブジェクトキャッシュ（0.5 秒毎に更新） ───
        private float _cacheTimer;
        private PlayerHealth[] _healthCache = System.Array.Empty<PlayerHealth>();
        private ItemBehaviour[] _itemCache = System.Array.Empty<ItemBehaviour>();
        private Weapon[] _weaponCache = System.Array.Empty<Weapon>();
        private Door[] _doorCache = System.Array.Empty<Door>();
        private PhysicsGrenade[] _grenadeCache = System.Array.Empty<PhysicsGrenade>();

        // ─── 接続モニタ ───
        private readonly Dictionary<int, string> _knownPlayers = new Dictionary<int, string>();

        // ─── ヘルスモニタ ───
        private readonly Dictionary<int, float> _lastHealth = new Dictionary<int, float>();
        private readonly Dictionary<int, bool> _lastKilled = new Dictionary<int, bool>();

        // ─── アイテムモニタ ───
        private readonly Dictionary<int, bool> _lastItemTaken = new Dictionary<int, bool>();
        private readonly Dictionary<int, API.Player> _lastItemHolder = new Dictionary<int, API.Player>();

        // ─── 武器モニタ ───
        private readonly Dictionary<int, int> _lastAmmo = new Dictionary<int, int>();
        private readonly Dictionary<int, bool> _lastReloading = new Dictionary<int, bool>();

        // ─── ドアモニタ ───
        private readonly Dictionary<int, bool> _lastDoorOpen = new Dictionary<int, bool>();

        // ─── チームモニタ ───
        private readonly Dictionary<int, int> _lastTeamId = new Dictionary<int, int>();

        // ─── スポーンモニタ ───
        private readonly HashSet<int> _lastAlivePlayers = new HashSet<int>();

        // ─── ラウンド/ゲームモニタ ───
        private int _lastTakeIndex = -1;
        private bool _lastRoundWasWon;
        private bool _lastGameStarted;
        private bool _lastBetweenRounds;
        private bool _lastInVictoryMenu;

        // ─── グレネードモニタ ───
        private readonly HashSet<int> _trackedGrenades = new HashSet<int>();
        private readonly HashSet<int> _explodedGrenades = new HashSet<int>();

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            DanoTimer.Tick();

            // チャット入力フィールドの onSubmit に直接フック
            if (!_chatHooked || _chatInputField == null)
            {
                _chatHooked = false;
                TryHookChatInput();
            }

            // チャット受信の evtChatMsgReceived に直接フック
            if (!_chatReceiveHooked)
                TryHookChatReceive();

            // F1 コンソール
            if (Input.GetKeyDown(KeyCode.F1))
                _consoleOpen = !_consoleOpen;

            // オブジェクトキャッシュ更新
            RefreshObjectCache();

            // 各モニタ（アイテム/武器/ドアは Harmony パッチに移行済み）
            TickHealthMonitor();
            TickConnectionMonitor();
            // TickItemMonitor();   — Harmony: ItemOnGrabPatch / ItemOnDropPatch
            // TickWeaponMonitor(); — Harmony: GunUpdatePatch
            // TickDoorMonitor();   — Harmony: DoorInteractPatch
            TickTeamMonitor();
            TickSpawnMonitor();
            TickRoundMonitor();
            TickGrenadeMonitor();
        }

        // ═══════════════════════════════════════════════
        //  オブジェクトキャッシュ
        // ═══════════════════════════════════════════════

        private void RefreshObjectCache()
        {
            _cacheTimer += Time.deltaTime;
            if (_cacheTimer < 0.5f && _healthCache.Length > 0) return;
            _cacheTimer = 0f;

            _healthCache = Object.FindObjectsOfType<PlayerHealth>();
            _itemCache = Object.FindObjectsOfType<ItemBehaviour>();
            _weaponCache = Object.FindObjectsOfType<Weapon>();
            _doorCache = Object.FindObjectsOfType<Door>();
            _grenadeCache = Object.FindObjectsOfType<PhysicsGrenade>();
        }

        // ═══════════════════════════════════════════════
        //  チャット入力フィールド直接フック
        // ═══════════════════════════════════════════════

        private void TryHookChatInput()
        {
            // ロビー: LobbyChatUILogic の inputField
            var lobbyChat = Object.FindObjectOfType<HeathenEngineering.DEMO.LobbyChatUILogic>();
            if (lobbyChat != null)
            {
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

            // ゲーム内: ChatInputField2
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

            if (text.StartsWith("/") && CommandManager.TryExecute(text))
            {
                if (_chatInputField != null)
                    _chatInputField.text = "";
                return;
            }

            var username = ClientInstance.Instance?.PlayerName ?? "";
            var ev = new ChatMessageSendingEvent(username, text);
            EventBus.Raise(ev);

            if (ev.Cancel && _chatInputField != null)
                _chatInputField.text = "";
            else if (ev.Message != text && _chatInputField != null)
                _chatInputField.text = ev.Message;
        }

        // ═══════════════════════════════════════════════
        //  チャット受信直接フック
        // ═══════════════════════════════════════════════

        private void TryHookChatReceive()
        {
            var lobbyChat = Object.FindObjectOfType<HeathenEngineering.DEMO.LobbyChatUILogic>();
            if (lobbyChat == null) return;

            var lm = HarmonyLib.Traverse.Create(lobbyChat).Field("lobbyManager").GetValue<LobbyManager>();
            if (lm == null) return;

            _lobbyManager = lm;
            _lobbyManager.evtChatMsgReceived.AddListener(OnChatReceived);
            _chatReceiveHooked = true;
            DANOLoader.Log.LogInfo("[DANO] チャット受信 evtChatMsgReceived にフック完了");
        }

        private void OnChatReceived(LobbyChatMsg message)
        {
            var senderName = message.sender.Name ?? "";
            var text = System.Text.Encoding.UTF8.GetString(message.data ?? System.Array.Empty<byte>());
            EventBus.Raise(new ChatMessageReceivedEvent(senderName, text));
        }

        // ═══════════════════════════════════════════════
        //  ヘルス/死亡ポーリング
        // ═══════════════════════════════════════════════

        private void TickHealthMonitor()
        {
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
                        if (damage < 500f)
                        {
                            var ev = new PlayerDamagedEvent(ph, damage, ph.killer);
                            EventBus.Raise(ev);

                            // Cancel → HP を回復して巻き戻す
                            if (ev.Cancel)
                            {
                                ph.health = prevHp;
                                currentHp = prevHp;
                            }
                        }
                    }
                }
                _lastHealth[id] = currentHp;

                // 死亡検出
                bool wasDead = _lastKilled.TryGetValue(id, out bool prevKilled) && prevKilled;
                bool isDead = currentKilled || currentHp <= 0f;

                if (isDead && !wasDead)
                {
                    EventBus.Raise(new PlayerDiedEvent(ph, ph.killer));
                }
                _lastKilled[id] = isDead;
            }
        }

        // ═══════════════════════════════════════════════
        //  接続/切断ポーリング
        // ═══════════════════════════════════════════════

        private void TickConnectionMonitor()
        {
            var current = ClientInstance.playerInstances;
            if (current == null) return;

            // 新規接続検出
            foreach (var kvp in current)
            {
                if (!_knownPlayers.ContainsKey(kvp.Key))
                {
                    var playerName = kvp.Value?.PlayerName ?? "";
                    var steamId = kvp.Value != null
                        ? (ulong)HarmonyLib.Traverse.Create(kvp.Value).Field("SteamID").GetValue<ulong>()
                        : 0UL;
                    _knownPlayers[kvp.Key] = playerName;
                    EventBus.Raise(new PlayerConnectedEvent(kvp.Key, playerName, steamId));
                }
            }

            // 切断検出
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

        // ═══════════════════════════════════════════════
        //  アイテム拾得/投棄ポーリング
        // ═══════════════════════════════════════════════

        private void TickItemMonitor()
        {
            foreach (var ib in _itemCache)
            {
                if (ib == null) continue;

                int id = ib.GetInstanceID();
                bool taken = ib.isTaken;

                if (_lastItemTaken.TryGetValue(id, out bool wasTaken))
                {
                    if (taken && !wasTaken)
                    {
                        EventBus.Raise(new ItemPickedUpEvent(ib));
                    }
                    else if (!taken && wasTaken)
                    {
                        _lastItemHolder.TryGetValue(id, out var lastHolder);
                        EventBus.Raise(new ItemDroppedEvent(ib, lastHolder));
                    }
                }

                _lastItemTaken[id] = taken;
                // Holder を記録（ドロップ後に参照できるように）
                if (taken)
                {
                    var item = API.Item.Get(ib);
                    var holder = item.Holder;
                    if (holder != null)
                        _lastItemHolder[id] = holder;
                }
            }
        }

        // ═══════════════════════════════════════════════
        //  武器射撃/リロードポーリング
        // ═══════════════════════════════════════════════

        private void TickWeaponMonitor()
        {
            foreach (var weapon in _weaponCache)
            {
                if (weapon == null) continue;

                int id = weapon.GetInstanceID();
                int currentAmmo = weapon.currentAmmo;
                bool isReloading = weapon.isReloading;

                // 射撃検出（弾数減少）
                if (_lastAmmo.TryGetValue(id, out int prevAmmo))
                {
                    if (currentAmmo < prevAmmo && !isReloading)
                    {
                        var ev = new WeaponFiredEvent(weapon);
                        EventBus.Raise(ev);

                        // Cancel → 弾を巻き戻す
                        if (ev.Cancel)
                        {
                            weapon.currentAmmo = prevAmmo;
                            currentAmmo = prevAmmo;
                        }
                    }
                }
                _lastAmmo[id] = currentAmmo;

                // リロード検出（isReloading が false → true）
                if (_lastReloading.TryGetValue(id, out bool wasReloading))
                {
                    if (isReloading && !wasReloading)
                    {
                        EventBus.Raise(new WeaponReloadEvent(weapon));
                    }
                }
                _lastReloading[id] = isReloading;
            }
        }

        // ═══════════════════════════════════════════════
        //  ドア開閉ポーリング
        // ═══════════════════════════════════════════════

        private void TickDoorMonitor()
        {
            foreach (var door in _doorCache)
            {
                if (door == null) continue;

                int id = door.GetInstanceID();
                bool isOpen = door.sync___get_value_isOpen();

                if (_lastDoorOpen.TryGetValue(id, out bool wasOpen))
                {
                    if (isOpen != wasOpen)
                    {
                        var ev = new DoorInteractEvent(door, wasOpen);
                        EventBus.Raise(ev);

                        // Cancel → ドアを元に戻す
                        if (ev.Cancel)
                        {
                            door.sync___set_value_isOpen(wasOpen, true);
                            isOpen = wasOpen;
                        }
                    }
                }
                _lastDoorOpen[id] = isOpen;
            }
        }

        // ═══════════════════════════════════════════════
        //  チーム変更ポーリング
        // ═══════════════════════════════════════════════

        private void TickTeamMonitor()
        {
            var sm = ScoreManager.Instance;
            if (sm == null) return;

            foreach (var kvp in sm.PlayerIdToTeamId)
            {
                int playerId = kvp.Key;
                int teamId = kvp.Value;

                if (_lastTeamId.TryGetValue(playerId, out int oldTeamId))
                {
                    if (teamId != oldTeamId)
                    {
                        EventBus.Raise(new TeamChangedEvent(playerId, oldTeamId, teamId));
                    }
                }
                _lastTeamId[playerId] = teamId;
            }
        }

        // ═══════════════════════════════════════════════
        //  スポーン検出ポーリング
        // ═══════════════════════════════════════════════

        private void TickSpawnMonitor()
        {
            var gm = GameManager.Instance;
            if (gm == null) return;

            var alive = gm.alivePlayers;
            if (alive == null) return;

            foreach (int playerId in alive)
            {
                if (!_lastAlivePlayers.Contains(playerId))
                {
                    EventBus.Raise(new PlayerSpawnedEvent(playerId));
                }
            }

            _lastAlivePlayers.Clear();
            foreach (int id in alive)
                _lastAlivePlayers.Add(id);
        }

        // ═══════════════════════════════════════════════
        //  ラウンド/ゲーム状態ポーリング
        // ═══════════════════════════════════════════════

        private void TickRoundMonitor()
        {
            // ScoreManager — ラウンド開始（TakeIndex 変化）
            var sm = ScoreManager.Instance;
            if (sm != null)
            {
                int takeIndex = sm.sync___get_value_TakeIndex();
                if (_lastTakeIndex >= 0 && takeIndex != _lastTakeIndex)
                {
                    EventBus.Raise(new RoundStartedEvent(takeIndex));
                }
                _lastTakeIndex = takeIndex;
            }

            // GameManager — ラウンド終了（roundWasWon 変化）
            var gm = GameManager.Instance;
            if (gm != null)
            {
                bool roundWon = gm.sync___get_value_roundWasWon();
                if (roundWon && !_lastRoundWasWon)
                {
                    // 勝利チームを算出
                    int winningTeamId = -1;
                    if (sm != null)
                    {
                        int highestScore = -1;
                        foreach (var kvp in sm.Points)
                        {
                            if (kvp.Value > highestScore)
                            {
                                highestScore = kvp.Value;
                                winningTeamId = kvp.Key;
                            }
                        }
                    }
                    EventBus.Raise(new RoundEndedEvent(winningTeamId, isDraw: false));
                }
                _lastRoundWasWon = roundWon;
            }

            // PauseManager — ゲーム開始、スポーンフェーズ、マッチ終了
            var pm = Object.FindObjectOfType<PauseManager>();
            if (pm != null)
            {
                // ゲーム開始
                bool gameStarted = pm.gameStarted;
                if (gameStarted && !_lastGameStarted)
                {
                    EventBus.Raise(new GameStartedEvent());
                }
                _lastGameStarted = gameStarted;

                // スポーンフェーズ
                bool betweenRounds = PauseManager.BetweenRounds;
                if (betweenRounds && !_lastBetweenRounds)
                {
                    EventBus.Raise(new SpawnPhaseStartedEvent());
                }
                _lastBetweenRounds = betweenRounds;

                // マッチ終了（ビクトリー画面遷移）
                bool inVictory = pm.inVictoryMenu;
                if (inVictory && !_lastInVictoryMenu)
                {
                    int winningTeamId = -1;
                    if (sm != null)
                    {
                        int highestScore = -1;
                        foreach (var kvp in sm.Points)
                        {
                            if (kvp.Value > highestScore)
                            {
                                highestScore = kvp.Value;
                                winningTeamId = kvp.Key;
                            }
                        }
                    }
                    EventBus.Raise(new MatchEndedEvent(winningTeamId));
                }
                _lastInVictoryMenu = inVictory;
            }
        }

        // ═══════════════════════════════════════════════
        //  グレネード爆発ポーリング
        // ═══════════════════════════════════════════════

        private void TickGrenadeMonitor()
        {
            var currentIds = new HashSet<int>();

            foreach (var grenade in _grenadeCache)
            {
                if (grenade == null) continue;

                int id = grenade.GetInstanceID();
                currentIds.Add(id);

                if (grenade.enabled)
                {
                    // アクティブなグレネードを追跡
                    _trackedGrenades.Add(id);
                    _explodedGrenades.Remove(id);
                }
                else if (_trackedGrenades.Contains(id) && !_explodedGrenades.Contains(id))
                {
                    // enabled が false になった = 爆発した（一度だけ発火）
                    _explodedGrenades.Add(id);
                    _trackedGrenades.Remove(id);
                    EventBus.Raise(new GrenadeExplodedEvent(
                        grenade.transform.position,
                        grenade.explosionRadius,
                        grenade.fragGrenade,
                        grenade.stunGrenade));
                }
            }

            // 破棄されたグレネードを除去
            _trackedGrenades.IntersectWith(currentIds);
            _explodedGrenades.IntersectWith(currentIds);
        }

        // ═══════════════════════════════════════════════
        //  シーン診断（/diag コマンドから呼ばれる）
        // ═══════════════════════════════════════════════

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

            var sm = ScoreManager.Instance;
            Check("ScoreManager", sm);

            // キャッシュ状態
            var inst = Instance;
            lines.Add($"  ObjectCache: HP={inst?._healthCache.Length ?? 0}, Items={inst?._itemCache.Length ?? 0}, Weapons={inst?._weaponCache.Length ?? 0}, Doors={inst?._doorCache.Length ?? 0}");

            // チャットフック状態
            var hookStatus = inst != null && inst._chatHooked ? "<color=#00FF00>hooked</color>" : "<color=#FF8800>not hooked</color>";
            var fieldStatus = inst?._chatInputField != null ? $" → {inst._chatInputField.gameObject.name}" : "";
            lines.Add($"  ChatHook: {hookStatus}{fieldStatus}");

            var recvStatus = inst != null && inst._chatReceiveHooked ? "<color=#00FF00>hooked</color>" : "<color=#FF8800>not hooked</color>";
            lines.Add($"  ChatReceiveHook: {recvStatus}");

            lines.Add("<color=#00FFFF>=== 診断完了 ===</color>");
            DANOLoader.Log.LogInfo("=== 診断完了 ===");

            ctx.Reply(string.Join("\n", lines));
        }

        // ═══════════════════════════════════════════════
        //  F1 コンソール (OnGUI)
        // ═══════════════════════════════════════════════

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
    }
}
