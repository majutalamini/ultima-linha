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
    /// Visual: título "ÚLTIMA / LINHA" à esquerda, trem passando ao fundo atrás de postes,
    /// granulado de filme, vinheta, botões JOGAR e SAIR.
    ///
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Menu Inicial.
    ///
    /// Todas as posições abaixo estão em pixels de uma tela 1920×1080, medidos a partir do canto superior esquerdo.
    /// </summary>
    [InitializeOnLoad]
    public static class MainMenuSceneBuilder
    {
        const int BuildVersion = 5; // aumente para forçar a cena a ser recriada automaticamente

        const string ScenePath = "Assets/Scenes/MainMenu.unity";
        const string ArtFolder = "Assets/UI/MainMenu";
        const string TrainFolder = ArtFolder + "/Trem";
        const string FontFolder = ArtFolder + "/Fonts";
        const string MusicPath = "Assets/Audio/Menu/musica_menu.ogg";
        const int UILayer = 5;

        // Trem: PNG de 1992x480 (medidas abaixo em "unidades de desenho" de 6 px cada)
        const float PixelSize = 6f;
        const float CarWidth = 332 * PixelSize, CarHeight = 80 * PixelSize, CarGap = 6 * PixelSize;
        const float TrainTop = 414f;

        // Cores medidas na imagem de referência
        static readonly Color Bg = Hex("070807");
        static readonly Color Haze = Hex("3A3C35");
        static readonly Color Shade = Hex("040505");
        static readonly Color Pole = Hex("101011");
        static readonly Color BarDark = Hex("111311");
        static readonly Color BarLight = Hex("242622");
        static readonly Color Cream = Hex("E8E1D4");
        static readonly Color Taupe = Hex("A69882");
        static readonly Color Tagline = Hex("78736A");
        static readonly Color Rule = Hex("796F5D");
        static readonly Color Number = Hex("6C684E");
        static readonly Color ButtonIdle = Hex("969A8A");

        const string AllText = "ÚLTIMALINHAUMA JORNADA SEM VOLTAJOGARSIR0123456789 ";

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
                Glow = EnsureRadialSprite("menu_glow", vignette: false),
                Vignette = EnsureRadialSprite("menu_vignette", vignette: true),
                LeftShade = EnsureLeftShadeSprite(),
                Grain = EnsureGrainTexture(),
                CarA = LoadTrainSprite(TrainFolder + "/train_car_a.png"),
                CarB = LoadTrainSprite(TrainFolder + "/train_car_b.png"),
                Serif = EnsureFontAsset("CormorantGaramond-Medium"),
                SerifItalic = EnsureFontAsset("CormorantGaramond-MediumItalic"),
                Sans = EnsureFontAsset("Oswald-Regular"),
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
            public Sprite Glow, Vignette, LeftShade, CarA, CarB;
            public Texture2D Grain;
            public TMP_FontAsset Serif, SerifItalic, Sans;
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

            // ---------- Cenário ----------
            Img(Stretch("Fundo", root), Bg);

            var haze = Img(TopLeft("Nevoa", root, 1440 - 1500, 560 - 750, 3000, 1500), WithAlpha(Haze, 1f));
            haze.sprite = art.Glow;

            // Trem (loop contínuo de vagões A B A B)
            var trainRt = TopLeft("Trem", root, -60, TrainTop, 4 * (CarWidth + CarGap), CarHeight);
            var body = Stretch("Vagoes", trainRt);
            for (int i = 0; i < 4; i++)
            {
                float x = i * (CarWidth + CarGap);
                if (i > 0) // sanfona entre os vagões
                    Img(TopLeft("Engate" + i, body, x - CarGap, 10 * PixelSize, CarGap, 53 * PixelSize), Hex("090B0C"));
                var car = Img(TopLeft("Vagao" + i, body, x, 0, CarWidth, CarHeight), WithAlpha(Color.white, 0.93f));
                car.sprite = i % 2 == 0 ? art.CarA : art.CarB;
            }
            var pass = trainRt.gameObject.AddComponent<TrainPass>();
            var pso = new SerializedObject(pass);
            pso.FindProperty("train").objectReferenceValue = trainRt;
            pso.FindProperty("body").objectReferenceValue = body;
            pso.FindProperty("loopLength").floatValue = 2 * (CarWidth + CarGap);
            pso.ApplyModifiedPropertiesWithoutUndo();

            // Chão escuro abaixo da plataforma
            var ground = Stretch("Chao", root);
            ground.anchorMax = new Vector2(1, 0);
            ground.offsetMax = new Vector2(0, 1080 - 969);
            Img(ground, WithAlpha(Hex("050606"), 0.7f));

            // Estrutura da rede aérea: barras horizontais e dois postes, na frente do trem
            HBar("BarraFraca", root, 226, 802, 227, 24, WithAlpha(BarDark, 0.35f));
            HBar("BarraSuperior", root, 802, -1, 227, 7, BarDark);
            HBar("FioClaro", root, 950, 1554, 235, 4, BarLight);
            HBar("BarraInferior", root, 802, -1, 240, 12, Hex("0E0E0E"));
            Img(AnchoredX("Poste1", root, 927f / 1920f, 108, 22, 975 - 108), Pole);
            Img(AnchoredX("Poste2", root, 1686f / 1920f, 127, 21, 975 - 127), Pole);

            // Sombra à esquerda (o trem some no escuro atrás do título)
            var shade = Img(TopLeft("SombraEsquerda", root, 0, 0, 1100, 1080), WithAlpha(Shade, 0.88f));
            shade.sprite = art.LeftShade;
            var shadeRt = shade.rectTransform;
            shadeRt.anchorMin = new Vector2(0, 0);
            shadeRt.anchorMax = new Vector2(0, 1);
            shadeRt.sizeDelta = new Vector2(1100, 0);
            shadeRt.anchoredPosition = Vector2.zero;

            Img(Stretch("Vinheta", root), WithAlpha(Color.black, 0.95f)).sprite = art.Vignette;

            // ---------- Título ----------
            var main = Stretch("PainelPrincipal", root);

            var tag = Label("Subtitulo", main, "UMA JORNADA SEM VOLTA", art.Sans, 17, Tagline, 160, 315);
            tag.characterSpacing = 27;
            Fringe(tag);

            var t1 = Label("Titulo_ULTIMA", main, "ÚLTIMA", art.Serif, 174, Cream, 160, 452);
            t1.characterSpacing = -7;
            var t2 = Label("Titulo_LINHA", main, "LINHA", art.SerifItalic, 174, Taupe, 222, 565);
            t2.characterSpacing = -3;

            Img(TopLeft("Linha1", main, 159, 616, 79, 2), Rule);
            Img(TopLeft("Linha2", main, 288, 616, 513, 2), Rule);
            var num = Label("Numero", main, "13", art.Sans, 13, Number, 255, 621);
            Fringe(num);

            // ---------- Botões ----------
            var play = MenuButton(main, "JOGAR", art.Sans, 790);
            var quit = MenuButton(main, "SAIR", art.Sans, 890);

            // ---------- Granulado de filme por cima de tudo ----------
            var grainRt = Stretch("Granulado", root);
            var grain = grainRt.gameObject.AddComponent<RawImage>();
            grain.texture = art.Grain;
            grain.color = WithAlpha(Color.white, 0.25f);
            grain.raycastTarget = false;
            SetFloats(grainRt.gameObject.AddComponent<FilmGrain>(), ("grainSize", 1.5f));

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
            so.FindProperty("firstSelected").objectReferenceValue = play;
            so.FindProperty("fader").objectReferenceValue = fader;
            so.FindProperty("music").objectReferenceValue = music;
            so.ApplyModifiedPropertiesWithoutUndo();

            UnityEventTools.AddPersistentListener(play.onClick, ctrl.Play);
            UnityEventTools.AddPersistentListener(quit.onClick, ctrl.Quit);
        }

        // ---------- Peças de UI ----------

        /// <summary>Botão só de texto. baseline = altura (a partir do topo) da base das letras.</summary>
        static Button MenuButton(Transform parent, string label, TMP_FontAsset font, float baseline)
        {
            const float h = 84f;
            var rt = TopLeft("Botao_" + label, parent, 176, baseline - 15 - h * 0.5f, 480, h);
            var hit = Img(rt, new Color(0, 0, 0, 0));
            hit.raycastTarget = true; // área clicável invisível

            var btn = rt.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.targetGraphic = hit;
            btn.navigation = new Navigation { mode = Navigation.Mode.Vertical, wrapAround = true };

            // texto com a linha de base 15 unidades abaixo do centro do botão
            var text = Label("Texto", rt, label, font, 44, ButtonIdle, 12, h * 0.5f + 15);
            text.characterSpacing = 20;
            Fringe(text);

            var fx = rt.gameObject.AddComponent<MenuButtonFx>();
            var so = new SerializedObject(fx);
            so.FindProperty("label").objectReferenceValue = text;
            so.FindProperty("normalColor").colorValue = ButtonIdle;
            so.FindProperty("highlightColor").colorValue = Cream;
            so.FindProperty("slideDistance").floatValue = 16f;
            so.ApplyModifiedPropertiesWithoutUndo();
            return btn;
        }

        /// <summary>Texto ancorado no canto superior esquerdo do pai, com a linha de base em (x, baseline).</summary>
        static TextMeshProUGUI Label(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color,
            float x, float baseline)
        {
            var rt = new GameObject(name, typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
            rt.pivot = new Vector2(0, 0.5f);
            rt.anchoredPosition = new Vector2(x, -baseline);
            rt.sizeDelta = new Vector2(1200, 10);

            var tmp = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font) tmp.font = font;
            tmp.text = text;
            tmp.fontSize = size;
            tmp.color = color;
            tmp.alignment = TextAlignmentOptions.BaselineLeft;
            tmp.raycastTarget = false;
            tmp.overflowMode = TextOverflowModes.Overflow;
            return tmp;
        }

        /// <summary>Aberração cromática leve: cópias vermelha e azul deslocadas 1 unidade para os lados.</summary>
        static void Fringe(TextMeshProUGUI source)
        {
            foreach (var (name, dx, color) in new[]
                     {
                         ("FranjaAzul", -1f, new Color(0.35f, 0.7f, 1f, 0.22f)),
                         ("FranjaVermelha", 1f, new Color(1f, 0.27f, 0.23f, 0.22f)),
                     })
            {
                var rt = new GameObject(name, typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
                rt.SetParent(source.transform, false);
                rt.anchorMin = Vector2.zero;
                rt.anchorMax = Vector2.one;
                rt.offsetMin = new Vector2(dx, 0);
                rt.offsetMax = new Vector2(dx, 0);
                var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
                t.font = source.font;
                t.text = source.text;
                t.fontSize = source.fontSize;
                t.characterSpacing = source.characterSpacing;
                t.alignment = source.alignment;
                t.overflowMode = source.overflowMode;
                t.color = color;
                t.raycastTarget = false;
            }
        }

        static void HBar(string name, Transform parent, float xFrom, float xTo, float top, float height, Color color)
        {
            var rt = new GameObject(name, typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.pivot = new Vector2(0, 1);
            if (xTo < 0) // vai até a borda direita da tela
            {
                rt.anchorMin = new Vector2(0, 1);
                rt.anchorMax = new Vector2(1, 1);
                rt.offsetMin = new Vector2(xFrom, -top - height);
                rt.offsetMax = new Vector2(0, -top);
            }
            else
            {
                rt.anchorMin = rt.anchorMax = new Vector2(0, 1);
                rt.anchoredPosition = new Vector2(xFrom, -top);
                rt.sizeDelta = new Vector2(xTo - xFrom, height);
            }
            Img(rt, color);
        }

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

        /// <summary>Posição horizontal proporcional à largura da tela (para os postes).</summary>
        static RectTransform AnchoredX(string name, Transform parent, float xFraction, float y, float w, float h)
        {
            var rt = TopLeft(name, parent, 0, y, w, h);
            rt.anchorMin = rt.anchorMax = new Vector2(xFraction, 1);
            rt.anchoredPosition = new Vector2(0, -y);
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

        /// <summary>Importa o PNG do trem como sprite liso (filtro bilinear, sem compressão).</summary>
        static Sprite LoadTrainSprite(string path)
        {
            var importer = AssetImporter.GetAtPath(path) as TextureImporter;
            if (importer == null)
            {
                Debug.LogWarning("[Última Linha] Não achei " + path);
                return null;
            }
            if (importer.textureType != TextureImporterType.Sprite || importer.filterMode != FilterMode.Bilinear ||
                importer.textureCompression != TextureImporterCompression.Uncompressed)
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

        static Sprite EnsureLeftShadeSprite()
        {
            return EnsureGeneratedSprite("menu_sombra_esquerda", 256, 4, (x, y) =>
            {
                float u = x / 255f;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(0.23f, 1f, u));
                return new Color(1, 1, 1, 1f - t);
            });
        }

        static Texture2D EnsureGrainTexture()
        {
            string path = $"{ArtFolder}/menu_granulado.png";
            if (!File.Exists(path))
            {
                var rnd = new System.Random(1234);
                WritePng(path, 256, 256, (x, y) =>
                {
                    float v = (float)(rnd.NextDouble() * 2.0 - 1.0); // -1..1
                    v = Mathf.Sign(v) * Mathf.Pow(Mathf.Abs(v), 1.5f);
                    return v >= 0 ? new Color(1, 1, 1, v * 0.16f) : new Color(0, 0, 0, -v * 0.16f);
                });
                var imp = (TextureImporter)AssetImporter.GetAtPath(path);
                imp.textureType = TextureImporterType.Default;
                imp.alphaIsTransparency = true;
                imp.mipmapEnabled = false;
                imp.wrapMode = TextureWrapMode.Repeat;
                imp.textureCompression = TextureImporterCompression.Uncompressed;
                imp.SaveAndReimport();
            }
            // grão suave (bilinear), sem aparência de pixel
            var importer = (TextureImporter)AssetImporter.GetAtPath(path);
            if (importer && importer.filterMode != FilterMode.Bilinear)
            {
                importer.filterMode = FilterMode.Bilinear;
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
