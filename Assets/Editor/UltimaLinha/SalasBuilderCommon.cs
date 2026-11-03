using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Platformer.Mechanics;
using TMPro;
using UltimaLinha.Cenario1;
using UltimaLinha.Salas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UltimaLinha.EditorTools.Cenario1SceneBuilder;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Peças que os Cenários 4 e 5 (salas fechadas de uma tela só, sem trem) têm em comum:
    /// o fluxo de criar/salvar a cena, o jogador, a câmera, a interface (diálogo, documento, cadeado) e os objetos de interação.
    /// </summary>
    internal static class SalasBuild
    {
        public const float PPU = 100f;
        public const float ImgH = 941f;
        public const string PlayerPrefab = "Assets/Prefabs/Player.prefab";
        public const string AudioFolder = "Assets/Audio/Salas";

        public static float X(float px) => px / PPU;
        public static float Y(float py) => (ImgH - py) / PPU;

        // ---------- Fluxo de criação ----------

        /// <summary>Cria a cena (sem mexer nas que estão abertas, se possível) e a coloca na lista de cenas depois de uma das anteriores.</summary>
        public static bool Run(bool interactive, string scenePath, string label, string[] after, Func<bool> ready, Action<GameObject> populate)
        {
            var loaded = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToList();
            bool hasUntitled = loaded.Any(s => string.IsNullOrEmpty(s.path));
            bool targetOpen = loaded.Any(s => s.path == scenePath);
            bool anyDirty = loaded.Any(s => s.isDirty);
            bool additive = !hasUntitled && !targetOpen;

            if (!additive && !interactive && anyDirty)
            {
                Debug.Log($"[Última Linha] Salve suas cenas e use o menu  Última Linha > Recriar {label}.");
                return false;
            }

            ImportArt();   // peças do Cenário 1 (personagens, tecla E, halos...)
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            if (!playerPrefab)
            {
                Debug.LogError("[Última Linha] Não achei " + PlayerPrefab + " (jogador do template).");
                return false;
            }
            if (!ready())
            {
                Debug.LogError($"[Última Linha] Falta a arte do {label}.");
                return false;
            }

            Scene previous = SceneManager.GetActiveScene();
            string reopenPath = (!targetOpen && !string.IsNullOrEmpty(previous.path)) ? previous.path : null;
            Scene scene;
            if (additive)
            {
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
                SceneManager.SetActiveScene(scene);
            }
            else
            {
                if (interactive && !EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return false;
                scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            }

            populate(playerPrefab);

            bool saved = EditorSceneManager.SaveScene(scene, scenePath);
            if (additive)
            {
                if (previous.IsValid()) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(scene, true);
            }
            else if (!interactive && reopenPath != null)
            {
                EditorSceneManager.OpenScene(reopenPath);
            }

            if (!saved)
            {
                Debug.LogError("[Última Linha] Não consegui salvar " + scenePath);
                return false;
            }

            AddToBuildSettings(scenePath, after);

            if (interactive && additive &&
                EditorUtility.DisplayDialog($"{label} pronto", "Cena salva em " + scenePath + ". Abrir agora?", "Abrir", "Depois"))
            {
                if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                    EditorSceneManager.OpenScene(scenePath);
            }
            return true;
        }

        /// <summary>Põe a cena logo depois da primeira das cenas "after" que existir na lista (ex.: Cenario3, Cenario2, Cenario1, MainMenu).</summary>
        public static void AddToBuildSettings(string scenePath, string[] after)
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != scenePath).ToList();
            int index = -1;
            foreach (var name in after)
            {
                index = scenes.FindIndex(s => s.path.EndsWith("/" + name + ".unity"));
                if (index >= 0) break;
            }
            scenes.Insert(index >= 0 ? index + 1 : scenes.Count, new EditorBuildSettingsScene(scenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        public static bool WaitForEditor(ref double firstTry, Action retry, string menuName)
        {
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (firstTry < 0) firstTry = EditorApplication.timeSinceStartup;
                if (EditorApplication.timeSinceStartup - firstTry < 120) EditorApplication.delayCall += () => retry();
                else Debug.Log($"[Última Linha] Não consegui criar o cenário sozinho. Use o menu  Última Linha > {menuName}.");
                return true;
            }
            return false;
        }

        // ---------- Assets ----------

        public static void ImportFolder(string artFolder, Dictionary<string, Vector2> pivots, string bigFile)
        {
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { artFolder }).Select(AssetDatabase.GUIDToAssetPath))
            {
                string file = Path.GetFileNameWithoutExtension(path);
                var pivot = pivots.TryGetValue(file, out var p) ? p : new Vector2(0.5f, 0.5f);
                ConfigureSprite(path, pivot, PPU, file == bigFile ? 8192 : 2048);
            }
        }

        public static Sprite Load(string folder, string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{folder}/{name}.png");
            if (!s) Debug.LogWarning("[Última Linha] Sprite não encontrado: " + name);
            return s;
        }

        public static AudioClip Clip(string name)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/{name}.wav");
            if (!c) Debug.LogWarning("[Última Linha] Som não encontrado: " + name);
            return c;
        }

        public static AudioClip Clip1(string name)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/Cenario1/{name}.wav");
            if (!c) Debug.LogWarning("[Última Linha] Som não encontrado: " + name);
            return c;
        }

        // ---------- Núcleo da cena ----------

        public class Core
        {
            public PlayerController player;
            public PersonagemPixel kai;
            public CameraFollow2D follow;
            public Transform solids;
            public Transform world;
            public Ui ui;
            public AudioSource voice, sfx;
            public GameObject audioRoot;
            public Vector3 spawn;
        }

        public class Ui
        {
            public DialogueBox dialogue;
            public DocumentView document;
            public DigitLockUI lockUi;
            public CanvasGroup darkness, redTint;
            public TMP_Text timer;
        }

        public struct Fonts
        {
            public TMP_FontAsset sans, serif, italic;
        }

        public static Fonts LoadFonts() => new Fonts
        {
            sans = LoadFont("Oswald-Regular"),
            serif = LoadFont("CormorantGaramond-Medium"),
            italic = LoadFont("CormorantGaramond-MediumItalic"),
        };

        /// <summary>Fundo, chão, paredes, jogador, câmera e interface. O cenário em si é montado por quem chama.</summary>
        public static Core BuildCore(GameObject playerPrefab, Sprite background, float levelW, float floorPy, float leftWallPx, float rightWallPx,
            float spawnPx, bool withLock, Fonts fonts)
        {
            var core = new Core();
            core.audioRoot = new GameObject("Som");
            core.sfx = Source(core.audioRoot, "Efeitos", null);
            core.voice = Source(core.audioRoot, "VozDialogo", null);
            core.voice.volume = 0.5f;

            core.world = new GameObject("Cenario").transform;
            var bg = SpriteObj("Fundo", core.world, background, Vector3.zero, -100);
            bg.transform.position = Vector3.zero;

            // colisores
            core.solids = new GameObject("Colisores").transform;
            core.solids.SetParent(core.world, false);
            float floorY = Y(floorPy);
            Box("Chao", core.solids, X(levelW) / 2f, floorY - 0.5f, X(levelW), 1f, oneWay: false);
            Box("ParedeEsquerda", core.solids, X(leftWallPx) - 0.5f, 6f, 1f, 14f, oneWay: false);
            Box("ParedeDireita", core.solids, X(rightWallPx) + 0.5f, 6f, 1f, 14f, oneWay: false);
            Box("Teto", core.solids, X(levelW) / 2f, ImgH / PPU + 0.5f, X(levelW), 1f, oneWay: false);

            // jogador (Kai)
            float spawnY = floorY + 1.62f;
            var spawn = new GameObject("SpawnPoint");
            spawn.transform.position = new Vector3(X(spawnPx), spawnY, 0);
            core.spawn = spawn.transform.position;
            var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerGo.name = "Kai (Player)";
            playerGo.transform.position = spawn.transform.position;
            var player = playerGo.GetComponent<PlayerController>();
            player.maxSpeed = 2.8f;
            player.runMultiplier = 2.06f; // corrida a ~5,8 u/s
            player.jumpTakeOffSpeed = 7.2f;
            player.gravityModifier = 1.25f;
            player.jumpAudio = null;
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            var box = playerGo.GetComponent<BoxCollider2D>();
            if (box)
            {
                box.size = new Vector2(0.9f, 3.2f);
                box.offset = Vector2.zero;
                PrefabUtility.RecordPrefabInstancePropertyModifications(box);
            }
            var mainSr = playerGo.GetComponent<SpriteRenderer>();
            if (mainSr)
            {
                mainSr.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(mainSr);
            }
            var kaiRig = BuildPersonagem("kai", playerGo.transform, new Vector3(0f, -1.6f, 0f), 10);
            kaiRig.mover = player;
            kaiRig.flipSource = mainSr;
            core.player = player;
            core.kai = kaiRig;

            var prompt = SpriteObj("TeclaE", null, Spr("tecla_e"), Vector3.zero, 50);
            prompt.transform.localScale = Vector3.one * 0.75f;
            prompt.enabled = false;
            var interactor = playerGo.AddComponent<PlayerInteractor>();
            interactor.player = player;
            interactor.prompt = prompt;

            var gcGo = new GameObject("GameController");
            var gc = gcGo.AddComponent<GameController>();
            gc.model.player = player;
            gc.model.spawnPoint = spawn.transform;
            gc.model.virtualCamera = null;
            gc.model.jumpModifier = 1.5f;
            gc.model.jumpDeceleration = 0.5f;

            // câmera
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.transform.position = new Vector3(spawn.transform.position.x + 5f, CameraSize, -10f);
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow2D>();
            follow.target = playerGo.transform;
            follow.bounds = new Rect(0f, 0f, X(levelW) - 0.01f, ImgH / PPU);
            core.follow = follow;

            core.ui = BuildUi(fonts, core.voice, withLock);
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();
            return core;
        }

        // ---------- Peças do cenário ----------

        /// <summary>Luminárias do teto: halo, capa escura (quando apaga) e o script de falhas (sem estalo).</summary>
        public static void BuildTubes(Transform parent, IEnumerable<(float x, float w)> tubes, float tubePy, Func<int, bool> broken)
        {
            var lights = new GameObject("Luminarias").transform;
            lights.SetParent(parent, false);
            int i = 0;
            foreach (var tube in tubes)
            {
                var go = new GameObject("Luminaria" + (i + 1));
                go.transform.SetParent(lights, false);
                go.transform.position = new Vector3(X(tube.x), Y(tubePy), 0);
                var cover = SpriteObj("Apagada", go.transform, Spr("pixel"), go.transform.position, -90);
                cover.color = new Color(0.03f, 0.03f, 0.035f, 0f);
                cover.transform.localScale = new Vector3(tube.w / 8f, 16f / 8f, 1f);
                var glow = SpriteObj("Halo", go.transform, Spr("brilho_tubo"), go.transform.position, 20);
                glow.color = new Color(0.86f, 0.93f, 0.9f, 0.15f);
                glow.transform.localScale = new Vector3(Mathf.Max(0.6f, tube.w * 1.7f / 256f), 1.8f, 1f);
                var fl = go.AddComponent<FluorescentLight>();
                fl.glow = glow;
                fl.cover = cover;
                fl.broken = broken != null && broken(i);
                fl.glowAlpha = fl.broken ? 0.11f : 0.15f;
                fl.crackle = null;
                i++;
            }
        }

        public static NpcTalk BuildNpc(string name, string id, float xPx, float floorPy, float height, Transform player)
        {
            var root = new GameObject(name);
            root.transform.position = new Vector3(X(xPx), Y(floorPy), 0);
            var npc = root.AddComponent<NpcTalk>();
            npc.reachX = 1.7f;
            npc.promptOffset = new Vector2(0f, (id == "souta" ? PixelArtHeightSouta : PixelArtHeightRen) + 0.45f);
            npc.lookAt = player;
            npc.rig = BuildPersonagem(id, root.transform, Vector3.zero, 6);
            return npc;
        }

        /// <summary>Porta de correr de duas folhas (a mesma do Cenário 1), com as imagens de uma pasta e prefixo próprios.</summary>
        public static ExitDoor BuildExitDoor(Transform parent, string name, string artFolder, string prefix, float x0Px, float x1Px, float topPy,
            float floorPy, AudioSource sfx, AudioClip unlock)
        {
            float cx = X((x0Px + x1Px) / 2f), cy = Y((topPy + floorPy) / 2f);
            float w = X(x1Px - x0Px), h = (floorPy - topPy) / PPU;
            float splitPx = (x0Px + x1Px) / 2f;

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(cx, Y(floorPy), 0);
            var door = root.AddComponent<ExitDoor>();
            door.reachX = 1.5f;
            door.promptOffset = new Vector2(0f, 4.0f);

            SpriteObj("Escuro", root.transform, Load(artFolder, prefix + "_interior"), new Vector3(cx, cy, 0), -60);
            var maskGo = new GameObject("Mascara");
            maskGo.transform.SetParent(root.transform, false);
            maskGo.transform.position = new Vector3(cx, cy, 0);
            maskGo.transform.localScale = new Vector3(w / 0.08f, h / 0.08f, 1f);
            maskGo.AddComponent<SpriteMask>().sprite = Spr("pixel");

            var left = SpriteObj("FolhaEsquerda", root.transform, Load(artFolder, prefix + "_folha_esq"),
                new Vector3(X((x0Px + splitPx) / 2f), cy, 0), -59);
            var right = SpriteObj("FolhaDireita", root.transform, Load(artFolder, prefix + "_folha_dir"),
                new Vector3(X((splitPx + x1Px) / 2f), cy, 0), -59);
            left.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            right.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            door.leftLeaf = left.transform;
            door.rightLeaf = right.transform;
            door.slideDistance = X(splitPx - x0Px) + 0.05f;

            var signalPos = new Vector3(cx, Y(topPy - 16f), 0);
            SpriteObj("SinalTrava", root.transform, Spr("sinal_porta"), signalPos, -45);
            var lamp = SpriteObj("SinalTrava_Luz", root.transform, Spr("ponto"), signalPos, -44);
            lamp.transform.localScale = new Vector3(2.6f, 1.2f, 1f);
            var glow = SpriteObj("SinalTrava_Halo", root.transform, Spr("brilho"), signalPos, -43);
            glow.transform.localScale = Vector3.one * 0.35f;
            door.signalLamp = lamp;
            door.signalGlow = glow;
            door.sfx = sfx;
            door.unlockClip = unlock;
            return door;
        }

        /// <summary>Lugar de interação (E). Fica no chão, em frente ao objeto desenhado no fundo.</summary>
        public static ObjectSpot BuildSpot(Transform parent, string id, float xPx, float floorPy, float promptHeight, float reach, AudioSource sfx = null, AudioClip clip = null)
        {
            var go = new GameObject("Lugar_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(X(xPx), Y(floorPy), 0);
            var spot = go.AddComponent<ObjectSpot>();
            spot.id = id;
            spot.reachX = reach;
            spot.promptOffset = new Vector2(0f, promptHeight);
            spot.sfx = sfx;
            spot.useClip = clip;
            return spot;
        }

        /// <summary>Alavanca do painel (começa abaixada). Reaproveita a arte do Cenário 1.</summary>
        public static PowerLever BuildPowerLever(Transform parent, int index, float xPx, float surfacePy, AudioSource sfx, AudioClip pull)
        {
            var root = new GameObject($"Alavanca{index + 1}");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(X(xPx), Y(surfacePy), 0);
            var lever = root.AddComponent<PowerLever>();
            lever.reachX = 1.0f;
            lever.minFeetDy = -0.35f;
            lever.maxFeetDy = 0.5f;
            lever.promptOffset = new Vector2(0f, 2.15f);
            lever.sfx = sfx;
            lever.pullClip = pull;

            var p = root.transform.position;
            SpriteObj("Base", root.transform, Spr("alavanca_base"), p, -10);
            var handle = SpriteObj("Cabo", root.transform, Spr("alavanca_cabo"), p + new Vector3(0f, 0.4f, 0f), -11);
            lever.handle = handle.transform;

            var lampPos = p + new Vector3(0f, 1.05f, 0f);
            SpriteObj("Soquete", root.transform, Spr("lampada_soquete"), lampPos + new Vector3(0f, 0.13f, 0f), -12);
            var lamp = SpriteObj("Lampada", root.transform, Spr("lampada"), lampPos, -10);
            var glow = SpriteObj("Lampada_Halo", root.transform, Spr("brilho"), lampPos, -9);
            glow.transform.localScale = Vector3.one * 0.55f;
            glow.color = new Color(1f, 1f, 1f, 0f);
            lever.lamp = lamp;
            lever.lampGlow = glow;
            return lever;
        }

        public static void SetSpeakers(DialogueBox dialogue)
        {
            dialogue.speakers = new List<Speaker>
            {
                new Speaker { id = "kai", displayName = "Kai", portrait = Portrait("kai"), nameColor = Hex("C9BFAE") },
                new Speaker { id = "souta", displayName = "Souta", portrait = Portrait("souta"), nameColor = Hex("A9B8C9") },
                new Speaker { id = "ren", displayName = "Ren", portrait = Portrait("ren"), nameColor = Hex("CBB98E") },
            };
        }

        // ---------- Interface ----------

        static Ui BuildUi(Fonts fonts, AudioSource voice, bool withLock)
        {
            var ui = new Ui();
            var canvasGo = new GameObject("Interface") { layer = UILayer };
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.transform;

            // escuridão da sala (some quando a energia volta) e vinheta (sem granulado: nada de "chiado")
            var darkRt = Stretch("Escuro", root);
            Img(darkRt, Color.black);
            ui.darkness = darkRt.gameObject.AddComponent<CanvasGroup>();
            ui.darkness.alpha = 0f;
            ui.darkness.blocksRaycasts = false;
            var vig = Img(Stretch("Vinheta", root), new Color(1f, 1f, 1f, 0.92f));
            vig.sprite = Spr("vinheta");

            // vermelho que cobre a tela durante a briga
            var tintRt = Stretch("TomVermelho", root);
            Img(tintRt, new Color(0.5f, 0.02f, 0.02f, 1f));
            ui.redTint = tintRt.gameObject.AddComponent<CanvasGroup>();
            ui.redTint.alpha = 0f;
            ui.redTint.blocksRaycasts = false;

            var hudRt = Stretch("HUD", root);
            var hud = hudRt.gameObject.AddComponent<Hud>();
            var objRt = Stretch("Objetivo", hudRt);
            var objGroup = objRt.gameObject.AddComponent<CanvasGroup>();
            objGroup.alpha = 0f;
            var objLabel = Label("Rotulo", objRt, "OBJETIVO", fonts.sans, 16, Tagline, 64, 58, 600, 24);
            objLabel.characterSpacing = 22;
            Img(TopLeft("Linha", objRt, 64, 70, 46, 1), Rule);
            var objText = Label("Texto", objRt, "", fonts.italic, 32, Cream, 64, 82, 1400, 60);

            var controls = Label("Controles", hudRt, "A/D andar   ·   SHIFT correr   ·   ESPAÇO pular   ·   E interagir   ·   TAB diário   ·   ESC menu",
                fonts.sans, 15, new Color(Muted.r, Muted.g, Muted.b, 0.55f), 64, 1080 - 44, 1400, 24);
            controls.characterSpacing = 6;

            ui.timer = Label("Contagem", hudRt, "", fonts.sans, 60, Hex("D9453A"), 1356, 44, 500, 80);
            ui.timer.alignment = TextAlignmentOptions.Right;
            ui.timer.characterSpacing = 8;

            var toastRt = Stretch("Aviso", hudRt);
            var toastGroup = toastRt.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            var toast = Label("Texto", toastRt, "", fonts.sans, 26, Hex("C8B9A0"), 0, 690, 1920, 40);
            toast.alignment = TextAlignmentOptions.Center;
            toast.characterSpacing = 10;

            ui.document = BuildDocument(root, fonts, voice);
            if (withLock) ui.lockUi = BuildDigitLock(root, fonts, voice);
            ui.dialogue = BuildDialogue(root, fonts.sans, fonts.serif, voice);
            BuildDiario(root, fonts.sans);

            var endRt = Stretch("Fim", hudRt);
            var endGroup = endRt.gameObject.AddComponent<CanvasGroup>();
            endGroup.alpha = 0f;
            var endTitle = Label("Titulo", endRt, "", fonts.serif, 92, Cream, 0, 470, 1920, 120);
            endTitle.alignment = TextAlignmentOptions.Center;
            var endSub = Label("Subtitulo", endRt, "", fonts.sans, 20, Tagline, 0, 590, 1920, 40);
            endSub.alignment = TextAlignmentOptions.Center;
            endSub.characterSpacing = 14;

            var faderRt = Stretch("Fade", root);
            Img(faderRt, Color.black);
            var fader = faderRt.gameObject.AddComponent<CanvasGroup>();
            fader.alpha = 0f;
            fader.blocksRaycasts = false;
            endRt.SetParent(root, false);
            endRt.SetAsLastSibling();

            hud.fader = fader;
            hud.objectiveGroup = objGroup;
            hud.objectiveText = objText;
            hud.toastGroup = toastGroup;
            hud.toastText = toast;
            hud.endGroup = endGroup;
            hud.endTitle = endTitle;
            hud.endSubtitle = endSub;
            return ui;
        }

        static DocumentView BuildDocument(Transform root, Fonts fonts, AudioSource voice)
        {
            var rt = Stretch("Documento", root);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var view = rt.gameObject.AddComponent<DocumentView>();
            Img(Stretch("Escurecer", rt), new Color(0f, 0f, 0f, 0.72f));

            var paper = new GameObject("Papel", typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            paper.SetParent(rt, false);
            paper.anchorMin = paper.anchorMax = paper.pivot = new Vector2(0.5f, 0.5f);
            paper.sizeDelta = new Vector2(820f, 880f);
            paper.localRotation = Quaternion.Euler(0f, 0f, -0.8f);
            Img(paper, new Color(0.04f, 0.035f, 0.03f, 1f));                            // sombra / borda
            var sheet = Stretch("Folha", paper);
            sheet.offsetMin = new Vector2(8f, 8f);
            sheet.offsetMax = new Vector2(-8f, -8f);
            Img(sheet, Hex("B9AF98"));
            var edge = Stretch("Mancha", sheet);
            Img(edge, new Color(0.35f, 0.28f, 0.18f, 0.12f));

            var ink = Hex("2A241C");
            var title = Label("Titulo", sheet, "", fonts.serif, 46, ink, 50, 44, 700, 120);
            title.alignment = TextAlignmentOptions.TopLeft;
            title.textWrappingMode = TextWrappingModes.Normal;
            title.characterSpacing = 4;
            Img(TopLeft("Traco", sheet, 50, 150, 700, 3), new Color(ink.r, ink.g, ink.b, 0.7f));
            var body = Label("Texto", sheet, "", fonts.serif, 35, ink, 50, 176, 700, 560);
            body.alignment = TextAlignmentOptions.TopLeft;
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Overflow;
            body.lineSpacing = 6;
            var footer = Label("Rodape", sheet, "", fonts.italic, 26, new Color(ink.r, ink.g, ink.b, 0.8f), 50, 744, 700, 60);
            footer.textWrappingMode = TextWrappingModes.Normal;
            var hint = Label("Fechar", sheet, "ESPAÇO  ›  fechar", fonts.sans, 18, new Color(ink.r, ink.g, ink.b, 0.6f), 0, 0, 200, 28);
            var hrt = hint.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(1, 0);
            hrt.pivot = new Vector2(1, 0);
            hrt.anchoredPosition = new Vector2(-28, 20);
            hint.alignment = TextAlignmentOptions.BottomRight;

            view.group = group;
            view.paper = paper;
            view.title = title;
            view.body = body;
            view.footer = footer;
            view.hint = hint;
            view.sfx = voice;
            view.openClip = Clip("papel");
            return view;
        }

        static DigitLockUI BuildDigitLock(Transform root, Fonts fonts, AudioSource voice)
        {
            var rt = Stretch("Cadeado", root);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var ui = rt.gameObject.AddComponent<DigitLockUI>();
            Img(Stretch("Escurecer", rt), new Color(0f, 0f, 0f, 0.68f));

            var panel = new GameObject("Painel", typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            panel.SetParent(rt, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1000f, 700f);
            Img(panel, new Color(0.035f, 0.033f, 0.035f, 0.96f));
            Img(TopLeft("Linha", panel, 0, 0, 1000, 2), Rule);

            var title = Label("Titulo", panel, "CADEADO DA GAVETA", fonts.sans, 20, Tagline, 0, 34, 1000, 30);
            title.alignment = TextAlignmentOptions.Top;
            title.characterSpacing = 26;
            var status = Label("Status", panel, "", fonts.italic, 28, Hex("C8B9A0"), 0, 76, 1000, 40);
            status.alignment = TextAlignmentOptions.Top;

            var digits = new TMP_Text[4];
            var above = new TMP_Text[4];
            var below = new TMP_Text[4];
            var frames = new Image[4];
            var ups = new Button[4];
            var downs = new Button[4];
            float wheelW = 150f, gap = 40f, x0 = (1000f - (4 * wheelW + 3 * gap)) / 2f;
            for (int i = 0; i < 4; i++)
            {
                float x = x0 + i * (wheelW + gap);
                // moldura da roda
                frames[i] = Img(TopLeft("Moldura" + (i + 1), panel, x - 4, 196, wheelW + 8, 330), new Color(0.47f, 0.43f, 0.36f, 0.7f));
                var inner = Img(TopLeft("Roda" + (i + 1), panel, x, 200, wheelW, 322), new Color(0.07f, 0.065f, 0.07f, 1f));
                above[i] = Label("Cima" + (i + 1), panel, "", fonts.serif, 66, Cream, x, 214, wheelW, 80);
                above[i].alignment = TextAlignmentOptions.Center;
                digits[i] = Label("Numero" + (i + 1), panel, "0", fonts.serif, 150, Cream, x, 296, wheelW, 150);
                digits[i].alignment = TextAlignmentOptions.Center;
                below[i] = Label("Baixo" + (i + 1), panel, "", fonts.serif, 66, Cream, x, 448, wheelW, 80);
                below[i].alignment = TextAlignmentOptions.Center;
                ups[i] = ArrowButton("Sobe" + (i + 1), panel, "^", fonts.sans, x + 35, 138);
                downs[i] = ArrowButton("Desce" + (i + 1), panel, "v", fonts.sans, x + 35, 540);
            }
            var help = Label("Ajuda", panel, "← →  ESCOLHE A RODA   ·   ↑ ↓  MUDA O NÚMERO   ·   ENTER CONFIRMA   ·   ESC FECHA",
                fonts.sans, 15, Muted, 0, 640, 1000, 24);
            help.alignment = TextAlignmentOptions.Top;
            help.characterSpacing = 8;

            ui.group = group;
            ui.panel = panel;
            ui.digits = digits;
            ui.above = above;
            ui.below = below;
            ui.frames = frames;
            ui.upButtons = ups;
            ui.downButtons = downs;
            ui.status = status;
            ui.sfx = voice;
            ui.tickClip = Clip("cadeado_clique");
            ui.wrongClip = Clip("cadeado_erro");
            ui.okClip = Clip("cadeado_abre");
            return ui;
        }

        static Button ArrowButton(string name, Transform parent, string glyph, TMP_FontAsset font, float x, float y)
        {
            var rt = TopLeft(name, parent, x, y, 80, 54);
            var img = Img(rt, new Color(0.11f, 0.105f, 0.11f, 1f));
            img.raycastTarget = true;
            var btn = rt.gameObject.AddComponent<Button>();
            btn.targetGraphic = img;
            var cb = btn.colors;
            cb.highlightedColor = new Color(0.25f, 0.2f, 0.18f, 1f);
            cb.pressedColor = new Color(0.4f, 0.12f, 0.1f, 1f);
            btn.colors = cb;
            btn.navigation = new Navigation { mode = Navigation.Mode.None };
            var t = Label("Seta", rt, glyph, font, 40, Cream, 0, 0, 80, 54);
            t.alignment = TextAlignmentOptions.Center;
            return btn;
        }
    }
}
