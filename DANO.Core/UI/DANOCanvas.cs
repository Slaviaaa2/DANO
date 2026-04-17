using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using System.Linq;

namespace DANO.UI
{
    /// <summary>
    /// DANO が管理するオーバーレイ Canvas。
    /// ゲーム内の他 Canvas より手前に描画される。
    /// </summary>
    internal class DANOCanvas : MonoBehaviour
    {
        internal static DANOCanvas Instance { get; private set; } = null!;

        internal Canvas Canvas { get; private set; } = null!;
        internal RectTransform Root { get; private set; } = null!;

        // ヒント表示用の専用エリア（画面下部中央）
        internal RectTransform HintArea { get; private set; } = null!;
        // 自由配置要素のルート
        internal RectTransform ElementArea { get; private set; } = null!;

        internal static DANOCanvas GetOrCreate()
        {
            if (Instance != null) return Instance;

            var go = new GameObject("[DANO]Canvas");
            DontDestroyOnLoad(go);
            return go.AddComponent<DANOCanvas>();
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            BuildCanvas();
        }

        private void BuildCanvas()
        {
            Canvas = gameObject.AddComponent<Canvas>();
            Canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            Canvas.sortingOrder = 32767; // 最前面

            var scaler = gameObject.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            gameObject.AddComponent<GraphicRaycaster>();

            Root = GetComponent<RectTransform>();

            // ヒントエリア（画面下部中央）
            HintArea = NewRect("HintArea", Root);
            HintArea.anchorMin = new Vector2(0.1f, 0.04f);
            HintArea.anchorMax = new Vector2(0.9f, 0.22f);
            HintArea.offsetMin = HintArea.offsetMax = Vector2.zero;
            HintArea.gameObject.AddComponent<VerticalLayoutGroup>().childAlignment = TextAnchor.LowerCenter;

            // 自由配置エリア（全画面）
            ElementArea = NewRect("ElementArea", Root);
            ElementArea.anchorMin = Vector2.zero;
            ElementArea.anchorMax = Vector2.one;
            ElementArea.offsetMin = ElementArea.offsetMax = Vector2.zero;
        }

        private static RectTransform NewRect(string name, RectTransform parent)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<RectTransform>();
        }

        // 日本語対応 TMP フォントを返す。初回呼び出し時に解決してキャッシュする。
        private static TMP_FontAsset? _cachedFont;

        // よく使う日本語文字（ひらがな・カタカナ・記号・頻出漢字）
        private const string JapaneseTestChars =
            "あいうえおかきくけこさしすせそたちつてとなにぬねのはひふへほまみむめもやゆよらりるれろわをん" +
            "アイウエオカキクケコサシスセソタチツテトナニヌネノハヒフヘホマミムメモヤユヨラリルレロワヲン" +
            "ぁぃぅぇぉっゃゅょァィゥェォッャュョ" +
            "、。！？「」『』…ー〜・" +
            "日本語文字化け表示確認テスト有効無効成功失敗漢字";

        // Windows\Fonts 内の日本語フォント候補 (ファイル名, faceIndex)
        private static readonly (string file, int face)[] FontFiles =
        {
            ("YuGothR.ttc", 0),
            ("YuGothM.ttc", 0),
            ("meiryo.ttc",  0),
            ("msgothic.ttc", 0),
            ("msmincho.ttc", 0),
        };

        internal static TMP_FontAsset? ResolveJapaneseFont()
        {
            if (_cachedFont != null) return _cachedFont;

            // ── 手順 1: NotoSansSC の sourceFontFile から 4096×4096 アトラスで作り直す ──
            // 元の 1024×1024 アトラスはゲームの中国語テキストで埋まっているため
            // 同じフォントファイルの埋め込みデータを使って大きなアトラスで新規生成する。
            var notoSC = Resources.FindObjectsOfTypeAll<TMP_FontAsset>()
                                  .FirstOrDefault(f => f.name.StartsWith("NotoSansSC"));
            if (notoSC?.sourceFontFile != null)
            {
                var rebuilt = TMP_FontAsset.CreateFontAsset(
                    notoSC.sourceFontFile, 90, 9,
                    UnityEngine.TextCore.LowLevel.GlyphRenderMode.SDFAA,
                    4096, 4096,
                    AtlasPopulationMode.Dynamic,
                    true);

                if (rebuilt != null)
                {
                    rebuilt.name = "[DANO] NotoSansSC 4096";
                    Object.DontDestroyOnLoad(rebuilt);

                    rebuilt.TryAddCharacters(JapaneseTestChars, out string missing);
                    int supported = JapaneseTestChars.Length - missing.Length;
                    DANOLoader.Log.LogInfo(
                        $"[DANO][Font] 再生成 NotoSansSC TryAdd: {supported}/{JapaneseTestChars.Length} OK" +
                        (missing.Length > 0 ? $"  missing='{missing}'" : ""));

                    _cachedFont = rebuilt;
                    RegisterGlobalFallback(rebuilt);
                    DANOLoader.Log.LogInfo("[DANO][Font] NotoSansSC 4096 で日本語対応");
                    return _cachedFont;
                }
                DANOLoader.Log.LogWarning("[DANO][Font] NotoSansSC 再生成失敗。FontEngine へ");
            }
            else
            {
                DANOLoader.Log.LogWarning("[DANO][Font] NotoSansSC または sourceFontFile が null");
            }

            // ── 手順 2: FontEngine で Windows\Fonts から直接ロード ──
            // この TMP バージョンに CreateFontAsset(filePath, ...) は存在しないため
            // FontEngine.LoadFontFace(path) → CreateFontAsset_Internal をリフレクションで呼ぶ。
            var createInternal = typeof(TMP_FontAsset).GetMethod(
                "CreateFontAsset_Internal",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

            var fontsDir = System.Environment.GetFolderPath(System.Environment.SpecialFolder.Fonts);

            foreach (var (file, face) in FontFiles)
            {
                var path = System.IO.Path.Combine(fontsDir, file);
                if (!System.IO.File.Exists(path)) continue;

                var err = UnityEngine.TextCore.LowLevel.FontEngine.LoadFontFace(path, 90, face);
                if (err != UnityEngine.TextCore.LowLevel.FontEngineError.Success)
                {
                    DANOLoader.Log.LogWarning($"[DANO][Font] FontEngine.LoadFontFace 失敗: {file} ({err})");
                    continue;
                }

                DANOLoader.Log.LogInfo($"[DANO][Font] FontEngine.LoadFontFace 成功: {file}");

                // CreateFontAsset_Internal の引数シグネチャを確認してから呼ぶ
                if (createInternal != null)
                {
                    var paramTypes = createInternal.GetParameters();
                    DANOLoader.Log.LogInfo(
                        $"[DANO][Font] CreateFontAsset_Internal 引数数={paramTypes.Length}: " +
                        string.Join(", ", paramTypes.Select(p => p.ParameterType.Name)));

                    // 引数なし版なら直接呼べる
                    if (paramTypes.Length == 0)
                    {
                        var asset = createInternal.Invoke(null, null) as TMP_FontAsset;
                        if (asset != null)
                        {
                            asset.name = $"[DANO] {file}";
                            asset.atlasPopulationMode = AtlasPopulationMode.Dynamic;
                            Object.DontDestroyOnLoad(asset);
                            _cachedFont = asset;
                            DANOLoader.Log.LogInfo($"[DANO][Font] 生成成功: {file}");
                            return _cachedFont;
                        }
                    }
                }
                else
                {
                    DANOLoader.Log.LogWarning("[DANO][Font] CreateFontAsset_Internal が見つからない");
                }
                break; // 1ファイル試せば内部シグネチャは同じなのでループ中断
            }

            DANOLoader.Log.LogWarning("[DANO][Font] 日本語フォントの取得に失敗。デフォルト TMP フォントを使用");
            return null;
        }

        // TMP のグローバルフォールバックリストに登録する。
        // これにより DANO 管理外の TMP コンポーネント（ゲームの右上ログ等）でも
        // 日本語グリフが見つからない場合にこのフォントで補完される。
        private static void RegisterGlobalFallback(TMP_FontAsset font)
        {
            try
            {
                if (TMP_Settings.instance == null) return;

                var fallbacks = TMP_Settings.fallbackFontAssets;
                if (fallbacks == null || fallbacks.Contains(font)) return;

                fallbacks.Add(font);
                DANOLoader.Log.LogInfo("[DANO][Font] TMP グローバルフォールバックに登録");
            }
            catch (System.Exception ex)
            {
                DANOLoader.Log.LogWarning($"[DANO][Font] グローバルフォールバック登録失敗: {ex.Message}");
            }
        }
    }
}
