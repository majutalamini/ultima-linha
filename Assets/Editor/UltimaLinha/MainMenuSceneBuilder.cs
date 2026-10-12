using System.IO;
using System.Linq;
using TMPro;
using UltimaLinha.UI;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.TextCore.LowLevel;
using UnityEngine.UI;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Monta a cena Assets/Scenes/MainMenu.unity com o menu inicial e a coloca como primeira cena do build.
    /// Visual (desde a versão 6): o vagão do Cenário 1 visto de frente, banhado de luz vermelha; a janela do
    /// meio virou um letreiro vermelho com o título "Última Linha"; névoa vermelha no chão; itens do menu
    /// centralizados (Começar, Continuar, Configurações, Sair) com uma faixa clara no item escolhido.
    /// A arte é gerada por Ferramentas/Menu/menu.py (Assets/UI/MainMenu/Vermelho).
    ///
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Menu Inicial.
    ///
    /// Posições em pixels de uma tela 1920×1080.
    /// </summary>
    [InitializeOnLoad]
    public static class MainMenuSceneBuilder
    {
        const int BuildVersion = 6; // aumente para forçar a cena a ser recriada automaticamente

        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string ArtFolder = "Assets/UI/MainMenu";
        const string RedFolder = ArtFolder + "/Vermelho";
        const string FontFolder = ArtFolder + "/Fonts";
        const string MusicPath = "Assets/Audio/Menu/musica_menu.ogg";
        const int UILayer = 5;

        static readonly Color Bg = Hex("140304");
        static readonly Color ItemIdle = Hex("EADBD6");
        static readonly Color ItemOn = Hex("FFFFFF");
        static readonly Color Highlight = Hex("F6E2DE");
        static readonly Color PanelBg = new Color(0.07f, 0.01f, 0.012f, 0.94f);
        static readonly Color PanelLine = Hex("B4231E");
        static readonly Color Muted = Hex("B99A95");

        const string AllText = "ÚltimaLinhaComeçarContinuarConfiguraçõesSairVolumegeralMúsicaTelacheiaSimNãoVoltar0123456789% ";

        static string VersionKey => "UltimaLinha.MainMenu.Version:" + Application.dataPath;
        static int retries;

        static MainMenuSceneBuilder()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        static void TryAutoBuild()
        {
            if (EditorPrefs.GetInt(VersionKey, 0) >= BuildVersion) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (retries++ < 300) EditorApplication.delayCall += TryAutoBuild;
                return;
            }

            if (Build(interactive: false))
            {
                EditorPrefs.SetInt(VersionKey, BuildVersion);
                Debug.Log("[Última Linha] Menu inicial atualizado em " + ScenePath +
                          ". Abra essa cena e aperte Play para testar.");
            }
        }

        [MenuItem("Última Linha/Recriar Menu Inicial")]
        static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Recriar menu inicial",
                    "A cena MainMenu já existe. Recriar vai substituir qualquer mudança feita nela. Continuar?",
                    "Recriar", "Cancelar"))
                return;

            if (Build(interactive: true))
                EditorPrefs.SetInt(VersionKey, BuildVersion);
        }

        [MenuItem("Última Linha/Abrir Menu Inicial")]
        static void OpenMenuScene()
        {
            if (!File.Exists(ScenePath)) { BuildFromMenu(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                EditorSceneManager.OpenScene(ScenePath);
        }

        // ------------------------------------------------------------------

        static bool Build(bool interactive)
        {
            var loaded = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToList();
            bool hasUntitled = loaded.Any(s => string.IsNullOrEmpty(s.path));
            bool targetOpen = loaded.Any(s => s.path == ScenePath);
            bool anyDirty = loaded.Any(s => s.isDirty);
            bool additive = !hasUntitled && !targetOpen;

            if (!additive && !interactive && anyDirty)
            {
                Debug.Log("[Última Linha] Salve suas cenas e use o menu  Última Linha > Recriar Menu Inicial  para atualizar o menu.");
                return false;
            }

            var art = new Art
            {
                Background = LoadSprite(RedFolder + "/menu_fundo_vermelho.png"),
                Title = LoadSprite(RedFolder + "/menu_titulo.png"),
                SignGlow = LoadSprite(RedFolder + "/menu_brilho_letreiro.png"),
                Fog = LoadRepeatTexture(RedFolder + "/menu_nevoa.png"),
                Vignette = EnsureRadialSprite("menu_vignette", vignette: true),
                Serif = EnsureFontAsset("IMFellEnglish-Regular"),
                Music = LoadMusic(MusicPath),
            };

            Scene previous = SceneManager.GetActiveScene();
            string reopenPath = (!targetOpen && !string.IsNullOrEmpty(previous.path)) ? previous.path : null;
            Scene scene;
            if (additive)
            {
                // Cria a cena "por fora", sem fechar nem mexer na cena que você está editando.
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
            }
            else
            {
                if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            Populate(art);

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);

            if (additive)
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            else if (!interactive && reopenPath != null)
            {
                EditorSceneManager.OpenScene(reopenPath); // devolve a cena em que você estava
            }

            if (!saved)
            {
                Debug.LogError("[Última Linha] Não consegui salvar " + ScenePath);
                return false;
            }

            AddToBuildSettings();

            if (interactive && additive &&
                EditorUtility.DisplayDialog("Menu inicial pronto", "Cena salva em " + ScenePath + ". Abrir agora?", "Abrir", "Depois"))
                OpenMenuScene();

            return true;
        }

        static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            scenes.Insert(0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        class Art
        {
            public Sprite Background, Title, SignGlow, Vignette;
            public Texture2D Fog;
            public TMP_FontAsset Serif;
            public AudioClip Music;
        }

        // ------------------------------------------------------------------

        static void Populate(Art art)
        {
            // Câmera
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Bg;
            camGo.transform.position = new Vector3(0, 0, -10);
            camGo.AddComponent<AudioListener>();

            // EventSystem (o projeto usa o Input System novo)
            var esGo = new GameObject("EventSystem");
            esGo.AddComponent<EventSystem>();
            esGo.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            // Canvas
            var canvasGo = new GameObject("MenuCanvas") { layer = UILayer };
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.transform;

            Img(Stretch("FundoCor", root), Bg);

            // ---------- Cenário (cobre a tela inteira em qualquer proporção, sem distorcer) ----------
            var scene = Stretch("Vagao", root);
            var fit = scene.gameObject.AddComponent<AspectRatioFitter>();
            fit.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
            fit.aspectRatio = 16f / 9f;

            var bg = Img(Stretch("Fundo", scene), Color.white);
            bg.sprite = art.Background;

            var glow = Img(Stretch("BrilhoLetreiro", scene), Color.white);
            glow.sprite = art.SignGlow;
            SetFloats(glow.gameObject.AddComponent<PulseAlpha>(), ("minAlpha", 0.6f), ("maxAlpha", 1f), ("period", 5.5f));

            var title = Img(Stretch("Titulo", scene), Color.white);
            title.sprite = art.Title;
            var flicker = title.gameObject.AddComponent<LightFlicker>();
            var fso = new SerializedObject(flicker);
            fso.FindProperty("intervalRange").vector2Value = new Vector2(7f, 16f);
            fso.FindProperty("dimAlpha").floatValue = 0.55f;
            fso.ApplyModifiedPropertiesWithoutUndo();

            // névoa vermelha no chão: duas camadas deslizando em velocidades diferentes
            Fog("NevoaFundo", scene, art.Fog, 0.48f, 0.55f, 0.75f, 0.006f);
            Fog("NevoaFrente", scene, art.Fog, 0.36f, 0.8f, 1.15f, -0.011f);

            Img(Stretch("Vinheta", root), WithAlpha(Color.black, 0.6f)).sprite = art.Vignette;

            // ---------- Menu ----------
            var menuRt = Stretch("Menu", root);
            var menuGroup = menuRt.gameObject.AddComponent<CanvasGroup>();
            var start = MenuButton(menuRt, "Começar", art.Serif, 310);
            var cont = MenuButton(menuRt, "Continuar", art.Serif, 244);
            var config = MenuButton(menuRt, "Configurações", art.Serif, 178);
            var quit = MenuButton(menuRt, "Sair", art.Serif, 112);
            Chain(start, cont, config, quit);

            // ---------- Configurações ----------
            var settings = BuildSettings(root, art.Serif);

            // ---------- Fade ----------
            var faderRt = Stretch("Fade", root);
            Img(faderRt, Color.black).raycastTarget = true;
            var fader = faderRt.gameObject.AddComponent<CanvasGroup>();
            fader.alpha = 0f;          // invisível no Editor; o controlador começa preto e clareia no Play
            fader.blocksRaycasts = false;

            // ---------- Controlador ----------
            var ctrlGo = new GameObject("MenuController");
            var ctrl = ctrlGo.AddComponent<MainMenuController>();

            // Música de fundo (toca em loop; o controlador faz o fade de entrada e saída)
            var music = ctrlGo.AddComponent<AudioSource>();
            music.clip = art.Music;
            music.loop = true;
            music.playOnAwake = false;
            music.spatialBlend = 0f;
            music.volume = 0f;

            var so = new SerializedObject(ctrl);
            so.FindProperty("gameSceneName").stringValue = "Cenario1"; // primeira cena do jogo
            so.FindProperty("firstSelected").objectReferenceValue = start;
            so.FindProperty("continueButton").objectReferenceValue = cont;
            so.FindProperty("settingsButton").objectReferenceValue = config;
            so.FindProperty("menuGroup").objectReferenceValue = menuGroup;
            so.FindProperty("settings").objectReferenceValue = settings;
            so.FindProperty("fader").objectReferenceValue = fader;
            so.FindProperty("music").objectReferenceValue = music;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(start.onClick, ctrl.Play);
            UnityEventTools.AddPersistentListener(cont.onClick, ctrl.Continue);
            UnityEventTools.AddPersistentListener(config.onClick, ctrl.OpenSettings);
            UnityEventTools.AddPersistentListener(quit.onClick, ctrl.Quit);
        }

        // ---------- Peças de UI ----------

        static void Fog(string name, Transform parent, Texture2D tex, float heightFraction, float alpha, float uvWidth, float speed)
        {
            var rt = new GameObject(name, typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = new Vector2(0, 0);
            rt.anchorMax = new Vector2(1, heightFraction);
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            var raw = rt.gameObject.AddComponent<RawImage>();
            raw.texture = tex;
            raw.color = WithAlpha(Color.white, alpha);
            raw.uvRect = new Rect(Random.value, 0f, uvWidth, 1f);
            raw.raycastTarget = false;
            SetFloats(rt.gameObject.AddComponent<FogDrift>(), ("speed", speed));
        }

        /// <summary>Item do menu: texto centralizado com faixa clara atrás quando escolhido.
        /// centerFromBottom = altura do centro do item, medida de baixo da tela.</summary>
        static Button MenuButton(Transform parent, string label, TMP_FontAsset font, float centerFromBottom)
        {
            var rt = new GameObject("Botao_" + label, typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0f);
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = new Vector2(0, centerFromBottom);
            rt.sizeDelta = new Vector2(420, 60);
            var hit = Img(rt, new Color(0, 0, 0, 0));
            hit.raycastTarget = true; // área clicável invisível

            var bar = Img(Stretch("Destaque", rt), WithAlpha(Highlight, 0f));

            var btn = rt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;

            var text = Centered("Texto", rt, label, font, 44, ItemIdle);

            var fx = rt.gameObject.AddComponent<MenuButtonFx>();
            var so = new SerializedObject(fx);
            so.FindProperty("label").objectReferenceValue = text;
            so.FindProperty("marker").objectReferenceValue = bar;
            so.FindProperty("normalColor").colorValue = ItemIdle;
            so.FindProperty("highlightColor").colorValue = ItemOn;
            so.FindProperty("markerAlpha").floatValue = 0.34f;
            so.FindProperty("slideDistance").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return btn;
        }

        /// <summary>Navegação explícita de cima para baixo (com volta), pulando itens desligados.</summary>
        static void Chain(params Selectable[] items)
        {
            for (int i = 0; i < items.Length; i++)
            {
                var nav = new Navigation
                {
                    mode = Navigation.Mode.Vertical,
                    wrapAround = true,
                };
                items[i].navigation = nav;
            }
        }

        static SettingsPanel BuildSettings(Transform root, TMP_FontAsset font)
        {
            var rt = Stretch("Configuracoes", root);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.interactable = false;
            group.blocksRaycasts = false;
            Img(Stretch("Escurecer", rt), WithAlpha(Color.black, 0.65f)).raycastTarget = true;

            var box = new GameObject("Painel", typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            box.SetParent(rt, false);
            box.anchorMin = box.anchorMax = new Vector2(0.5f, 0.5f);
            box.sizeDelta = new Vector2(820, 560);
            Img(box, PanelBg);
            var line = TopLeftIn("Linha", box, 0, 0, 820, 3);
            Img(line, PanelLine);
            var line2 = TopLeftIn("LinhaBaixo", box, 0, 557, 820, 3);
            Img(line2, WithAlpha(PanelLine, 0.6f));

            var head = Centered("Titulo", box, "Configurações", font, 58, ItemOn);
            var hrt = head.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(0.5f, 1f);
            hrt.sizeDelta = new Vector2(800, 80);
            hrt.anchoredPosition = new Vector2(0, -70);

            var (master, masterValue) = SliderRow(box, "Volume geral", font, 180);
            var (music, musicValue) = SliderRow(box, "Música", font, 270);
            var (full, fullValue) = ButtonRow(box, "Tela cheia", font, 360);

            var back = MenuButton(box, "Voltar", font, 0);
            var brt = (RectTransform)back.transform;
            brt.anchorMin = brt.anchorMax = new Vector2(0.5f, 0f);
            brt.anchoredPosition = new Vector2(0, 70);

            // navegação: cima/baixo entre as linhas, esquerda/direita mexe no volume
            Explicit(master, back, music);
            Explicit(music, master, full);
            Explicit(full, music, back);
            Explicit(back, full, master);

            var panel = rt.gameObject.AddComponent<SettingsPanel>();
            var so = new SerializedObject(panel);
            so.FindProperty("group").objectReferenceValue = group;
            so.FindProperty("masterSlider").objectReferenceValue = master;
            so.FindProperty("masterValue").objectReferenceValue = masterValue;
            so.FindProperty("musicSlider").objectReferenceValue = music;
            so.FindProperty("musicValue").objectReferenceValue = musicValue;
            so.FindProperty("fullscreenButton").objectReferenceValue = full;
            so.FindProperty("fullscreenValue").objectReferenceValue = fullValue;
            so.FindProperty("backButton").objectReferenceValue = back;
            so.ApplyModifiedPropertiesWithoutUndo();
            return panel;
        }

        static void Explicit(Selectable s, Selectable up, Selectable down)
        {
            var nav = new Navigation { mode = Navigation.Mode.Explicit, selectOnUp = up, selectOnDown = down };
            s.navigation = nav;
        }

        /// <summary>Linha do painel: faixa de destaque + rótulo à esquerda (o controle vai à direita).</summary>
        static (RectTransform row, TextMeshProUGUI label, Image bar) Row(RectTransform box, string label, TMP_FontAsset font, float top)
        {
            var row = TopLeftIn("Linha_" + label, box, 50, top, 720, 64);
            var bar = Img(Stretch("Destaque", row), WithAlpha(Highlight, 0f));
            var text = Centered("Rotulo", row, label, font, 38, ItemIdle);
            text.alignment = TextAlignmentOptions.MidlineLeft;
            var trt = text.rectTransform;
            trt.anchorMin = new Vector2(0, 0);
            trt.anchorMax = new Vector2(0.5f, 1);
            trt.offsetMin = new Vector2(24, 0);
            trt.offsetMax = Vector2.zero;
            return (row, text, bar);
        }

        static void AddFx(GameObject go, TextMeshProUGUI label, Image bar)
        {
            var fx = go.AddComponent<MenuButtonFx>();
            var so = new SerializedObject(fx);
            so.FindProperty("label").objectReferenceValue = label;
            so.FindProperty("marker").objectReferenceValue = bar;
            so.FindProperty("normalColor").colorValue = ItemIdle;
            so.FindProperty("highlightColor").colorValue = ItemOn;
            so.FindProperty("markerAlpha").floatValue = 0.22f;
            so.FindProperty("slideDistance").floatValue = 0f;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static (Slider, TextMeshProUGUI) SliderRow(RectTransform box, string label, TMP_FontAsset font, float top)
        {
            var (row, text, bar) = Row(box, label, font, top);

            var srt = new GameObject("Slider", typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            srt.SetParent(row, false);
            srt.anchorMin = srt.anchorMax = new Vector2(1, 0.5f);
            srt.pivot = new Vector2(1, 0.5f);
            srt.anchoredPosition = new Vector2(-120, 0);
            srt.sizeDelta = new Vector2(260, 26);
            var track = Img(Stretch("Trilho", srt), WithAlpha(Hex("3A0A0A"), 1f));
            track.raycastTarget = true;
            var fillArea = Stretch("AreaPreenchida", srt);
            var fill = Img(Stretch("Preenchido", fillArea), PanelLine);
            var handleArea = Stretch("AreaPegador", srt);
            handleArea.offsetMin = new Vector2(8, 0);
            handleArea.offsetMax = new Vector2(-8, 0);
            var handle = new GameObject("Pegador", typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            handle.SetParent(handleArea, false);
            handle.sizeDelta = new Vector2(16, 34);
            var himg = Img(handle, ItemOn);
            himg.raycastTarget = true;

            var slider = srt.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.fillRect = (RectTransform)fill.transform;
            slider.handleRect = handle;
            slider.targetGraphic = himg;
            slider.direction = Slider.Direction.LeftToRight;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            AddFx(srt.gameObject, text, bar); // escolhido = a linha inteira acende

            var value = Centered("Valor", row, "100%", font, 32, Muted);
            var vrt = value.rectTransform;
            vrt.anchorMin = vrt.anchorMax = new Vector2(1, 0.5f);
            vrt.pivot = new Vector2(1, 0.5f);
            vrt.sizeDelta = new Vector2(100, 50);
            vrt.anchoredPosition = new Vector2(-12, 0);
            value.alignment = TextAlignmentOptions.MidlineRight;
            return (slider, value);
        }

        static (Button, TextMeshProUGUI) ButtonRow(RectTransform box, string label, TMP_FontAsset font, float top)
        {
            var (row, text, bar) = Row(box, label, font, top);
            var hit = Img(row, new Color(0, 0, 0, 0));
            hit.raycastTarget = true;
            var btn = row.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            AddFx(row.gameObject, text, bar);

            var value = Centered("Valor", row, "Não", font, 36, ItemOn);
            var vrt = value.rectTransform;
            vrt.anchorMin = vrt.anchorMax = new Vector2(1, 0.5f);
            vrt.pivot = new Vector2(1, 0.5f);
            vrt.sizeDelta = new Vector2(240, 50);
            vrt.anchoredPosition = new Vector2(-12, 0);
            value.alignment = TextAlignmentOptions.MidlineRight;
            return (btn, value);
        }

        /// <summary>Texto que ocupa o retângulo do pai inteiro, centralizado.</summary>
        static TextMeshProUGUI Centered(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color)
        {
            var rt = Stretch(name, parent);
            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.Center;
            tmp.raycastTarget = false;
            tmp.textWrappingMode = TextWrappingModes.NoWrap;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        static RectTransform TopLeftIn(string name, Transform parent, float x, float y, float w, float h) => TopLeft(name, parent, x, y, w, h);

        // ---------- Utilitários de layout ----------

        static RectTransform TopLeft(string name, Transform parent, float x, float y, float w, float h)
        {
            var rt = new GameObject(name, typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 1);
            rt.anchoredPosition = new Vector2(x, -y);
            rt.sizeDelta = new Vector2(w, h);
            return rt;
        }

        static RectTransform Stretch(string name, Transform parent)
        {
            var rt = new GameObject(name, typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = rt.offsetMax = Vector2.zero;
            return rt;
        }

        static Image Img(RectTransform rt, Color color)
        {
            var img = rt.gameObject.AddComponent<Image>();
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        static void SetFloats(Object target, params (string name, float value)[] values)
        {
            var so = new SerializedObject(target);
            foreach (var (n, v) in values) so.FindProperty(n).floatValue = v;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        // ---------- Assets ----------

        /// <summary>Cria (uma vez) o TMP Font Asset a partir do .ttf em Assets/UI/MainMenu/Fonts.</summary>
        static TMP_FontAsset EnsureFontAsset(string fileName)
        {
            string assetPath = $"{FontFolder}/{fileName} SDF.asset";
            var asset = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(assetPath);
            if (asset) return asset;

            var font = AssetDatabase.LoadAssetAtPath<Font>($"{FontFolder}/{fileName}.ttf");
            if (!font)
            {
                Debug.LogWarning($"[Última Linha] Fonte {fileName}.ttf não encontrada; usando a fonte padrão do TextMeshPro.");
                return TMP_Settings.defaultFontAsset;
            }

            asset = TMP_FontAsset.CreateFontAsset(font, 90, 9, GlyphRenderMode.SDFAA, 1024, 1024, AtlasPopulationMode.Dynamic);
            if (!asset) return TMP_Settings.defaultFontAsset;

            asset.name = fileName + " SDF";
            AssetDatabase.CreateAsset(asset, assetPath);
            asset.atlasTextures[0].name = asset.name + " Atlas";
            AssetDatabase.AddObjectToAsset(asset.atlasTextures[0], asset);
            asset.material.name = asset.name + " Material";
            AssetDatabase.AddObjectToAsset(asset.material, asset);
            asset.TryAddCharacters(AllText, out string _);
            EditorUtility.SetDirty(asset.atlasTextures[0]);
            EditorUtility.SetDirty(asset);
            AssetDatabase.SaveAssets();
            return asset;
        }

        /// <summary>
        /// Carrega a música do menu. Como o arquivo é longo (1 hora), ela é tocada em streaming
        /// direto do disco, em vez de ser carregada inteira na memória.
        /// </summary>
        static AudioClip LoadMusic(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as AudioImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Última Linha] Música do menu não encontrada em " + path);
                return null;
            }
            var settings = importer.defaultSampleSettings;
            if (settings.loadType != AudioClipLoadType.Streaming)
            {
                settings.loadType = AudioClipLoadType.Streaming;
                settings.compressionFormat = AudioCompressionFormat.Vorbis;
                settings.quality = 0.7f;
                importer.defaultSampleSettings = settings;
                importer.forceToMono = false;
                importer.loadInBackground = true;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<AudioClip>(path);
        }

        /// <summary>Importa um PNG do menu como sprite liso (filtro bilinear, sem compressão, até 2048 px).</summary>
        static Sprite LoadSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Última Linha] Não achei " + path);
                return null;
            }
            if (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Bilinear ||
                importer.textureCompression != TextureImporterCompression.Uncompressed || importer.maxTextureSize < 2048)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        /// <summary>Textura que se repete na horizontal (névoa deslizando).</summary>
        static Texture2D LoadRepeatTexture(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Última Linha] Não achei " + path);
                return null;
            }
            if (importer.textureType != TextureImporterType.Default || importer.wrapModeU != TextureWrapMode.Repeat ||
                importer.wrapModeV != TextureWrapMode.Clamp || importer.maxTextureSize < 2048)
            {
                importer.textureType = TextureImporterType.Default;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapModeU = TextureWrapMode.Repeat;
                importer.wrapModeV = TextureWrapMode.Clamp;
                importer.filterMode = FilterMode.Bilinear;
                importer.maxTextureSize = 2048;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        /// <summary>Gera (uma vez) uma textura radial suave: brilho (centro claro) ou vinheta (bordas escuras).</summary>
        static Sprite EnsureRadialSprite(string name, bool vignette)
        {
            return EnsureGeneratedSprite(name, 512, 512, (x, y) =>
            {
                float d = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), new Vector2(256, 256)) / 256f;
                float a = vignette
                    ? Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.35f, 1.35f, d))
                    : Mathf.Pow(Mathf.Clamp01(1f - d), 2.2f);
                return new Color(1, 1, 1, a);
            });
        }

        static Sprite EnsureGeneratedSprite(string name, int w, int h, System.Func<int, int, Color> pixel)
        {
            string path = $"{ArtFolder}/{name}.png";
            if (!File.Exists(path))
            {
                WritePng(path, w, h, pixel);
                var importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.wrapMode = TextureWrapMode.Clamp;
                importer.SaveAndReimport();
            }
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        static void WritePng(string path, int w, int h, System.Func<int, int, Color> pixel)
        {
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            var tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
            var px = new Color[w * h];
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                    px[y * w + x] = pixel(x, y);
            tex.SetPixels(px);
            tex.Apply();
            File.WriteAllBytes(path, tex.EncodeToPNG());
            Object.DestroyImmediate(tex);
            AssetDatabase.ImportAsset(path);
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;
        static Color WithAlpha(Color c, float a) { c.a = a; return c; }
    }
}
