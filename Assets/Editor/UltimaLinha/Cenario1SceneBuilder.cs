using System.Collections.Generic;
using System.IO;
using System.Linq;
using Platformer.Mechanics;
using TMPro;
using UltimaLinha.Cenario1;
using UltimaLinha.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Monta a cena Assets/Scenes/Cenario1.unity: o vagão do metrô (fundo feito a partir da imagem do Notion),
    /// Souta e Ren, o puzzle das três alavancas e a porta do fundo.
    ///
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Cenário 1.
    ///
    /// Medidas: o fundo tem 100 px por unidade. As posições abaixo estão em pixels da imagem do vagão
    /// (x a partir da esquerda do fundo; y a partir do topo da imagem original, sem a faixa de teto extra).
    /// </summary>
    [InitializeOnLoad]
    public static class Cenario1SceneBuilder
    {
        const int BuildVersion = 1; // aumente para forçar a cena a ser recriada automaticamente

        const string ScenePath = "Assets/Scenes/Cenario1.unity";
        const string Art = "Assets/Art/Cenario1";
        const string CharArt = Art + "/Personagens";
        const string FinalCharArt = "Assets/Art/Personagens"; // arte definitiva, se existir
        const string AudioFolder = "Assets/Audio/Cenario1";
        const string FontFolder = "Assets/UI/MainMenu/Fonts";
        const string PlayerPrefab = "Assets/Prefabs/Player.prefab";
        const string GrainTexture = "Assets/UI/MainMenu/menu_granulado.png";
        const int UILayer = 5;

        // ---------- Escala e medidas do fundo ----------
        const float PPU = 100f;
        const float ImageHeight = 941f;               // imagem original
        const float LevelWidth = 4230f, LevelHeight = 1101f; // fundo final (com teto extra)
        static float X(float px) => px / PPU;
        static float Y(float py) => (ImageHeight - py) / PPU;

        const float FloorPy = 870f;     // onde os pés pisam no chão
        const float CushionPy = 648f;   // assento dos bancos
        const float BackrestPy = 515f;  // topo do encosto
        const float RackPy = 262f;      // bagageiro (onde se pisa)
        const float LeftWallPx = 225f, RightWallPx = 4128f;

        // bancos: (início, fim) do assento e do encosto, em px
        static readonly (float x0, float x1, float b0, float b1)[] Benches =
        {
            (215, 1020, 205, 1045),
            (1500, 2345, 1460, 2370),
            (2825, 3670, 2785, 3695),
        };
        static readonly float[] RackCenters = { 600, 3370 };
        const float RackWidthPx = 340f;

        // alavancas, da esquerda para a direita: (x px, y py da plataforma)
        static readonly (float x, float py, string where)[] Levers =
        {
            (600, RackPy, "bagageiro do 1º banco"),
            (2160, BackrestPy, "encosto do 2º banco"),
            (3370, RackPy, "bagageiro do 3º banco"),
        };
        static readonly int[] LeverOrder = { 1, 0, 2 }; // meio, esquerda, direita

        // letreiros em cima das portas (centro x px), y do letreiro
        static readonly float[] PanelCenters = { 1256, 2581, 3906 };
        const float PanelPy = 171f, PanelHalfWidthPx = 77f;

        // porta de saída (3º trecho)
        const float DoorX0 = 3730, DoorSplit = 3904, DoorX1 = 4080, DoorTop = 212, DoorBottom = 746;

        // luminárias do teto: (centro px, largura px, falhando?)
        static readonly (float cx, float w, bool broken)[] Tubes =
        {
            (558, 225, false), (1177, 197, false), (1623, 50, false), (1883, 225, false),
            (2502, 197, false), (2948, 50, true), (3208, 225, true), (3824, 190, true),
        };
        const float TubePy = 111f;

        static readonly Color Cream = Hex("E8E1D4");
        static readonly Color Muted = Hex("969A8A");
        static readonly Color Tagline = Hex("78736A");
        static readonly Color Rule = Hex("796F5D");
        static readonly Color CharTint = new Color(0.84f, 0.82f, 0.8f, 1f);

        static string VersionKey => "UltimaLinha.Cenario1.Version:" + Application.dataPath;
        static double firstTry = -1;

        static Cenario1SceneBuilder()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        static void TryAutoBuild()
        {
            if (EditorPrefs.GetInt(VersionKey, 0) >= BuildVersion) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                // espera a Unity terminar de importar/compilar (até ~2 minutos), sem desistir em silêncio
                if (firstTry < 0) firstTry = EditorApplication.timeSinceStartup;
                if (EditorApplication.timeSinceStartup - firstTry < 120) EditorApplication.delayCall += TryAutoBuild;
                else Debug.Log("[Última Linha] Não consegui criar o Cenário 1 sozinho. Use o menu  Última Linha > Abrir Cenário 1.");
                return;
            }
            if (Build(interactive: false))
            {
                EditorPrefs.SetInt(VersionKey, BuildVersion);
                Debug.Log("[Última Linha] Cenário 1 criado em " + ScenePath + ". Abra pelo menu Última Linha > Abrir Cenário 1.");
            }
        }

        [MenuItem("Última Linha/Recriar Cenário 1")]
        static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Recriar Cenário 1",
                    "A cena Cenario1 já existe. Recriar vai substituir qualquer mudança feita nela. Continuar?",
                    "Recriar", "Cancelar"))
                return;
            if (Build(interactive: true))
                EditorPrefs.SetInt(VersionKey, BuildVersion);
        }

        [MenuItem("Última Linha/Abrir Cenário 1")]
        static void OpenScene()
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
                Debug.Log("[Última Linha] Salve suas cenas e use o menu  Última Linha > Recriar Cenário 1.");
                return false;
            }

            ImportArt();
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            if (!playerPrefab)
            {
                Debug.LogError("[Última Linha] Não achei " + PlayerPrefab + " (jogador do template).");
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

            Populate(playerPrefab);

            bool saved = EditorSceneManager.SaveScene(scene, ScenePath);
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
                Debug.LogError("[Última Linha] Não consegui salvar " + ScenePath);
                return false;
            }

            AddToBuildSettings();

            if (interactive && additive &&
                EditorUtility.DisplayDialog("Cenário 1 pronto", "Cena salva em " + ScenePath + ". Abrir agora?", "Abrir", "Depois"))
                OpenScene();
            return true;
        }

        static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            int menu = scenes.FindIndex(s => s.path.EndsWith("/MainMenu.unity"));
            scenes.Insert(menu >= 0 ? menu + 1 : 0, new EditorBuildSettingsScene(ScenePath, true));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        // ------------------------------------------------------------------

        static void Populate(GameObject playerPrefab)
        {
            var fonts = (
                sans: LoadFont("Oswald-Regular"),
                serif: LoadFont("CormorantGaramond-Medium"),
                italic: LoadFont("CormorantGaramond-MediumItalic"));

            // ---------- Som ----------
            var audioRoot = new GameObject("Som");
            var ambience = Source(audioRoot, "Ambiente", Clip("ambiente_vagao"), loop: true);
            var sfx = Source(audioRoot, "Efeitos", null);
            var beep = Source(audioRoot, "BipeDaPista", null);
            beep.volume = 0.7f;
            var voice = Source(audioRoot, "VozDialogo", null);
            voice.volume = 0.5f;
            var lightsSfx = Source(audioRoot, "EstaloLuzes", null);

            // ---------- Cenário ----------
            var world = new GameObject("Cenario");
            var bg = SpriteObj("Fundo", world.transform, Spr("cenario1_fundo"), Vector3.zero, -100);
            bg.transform.position = Vector3.zero; // pivô no canto inferior esquerdo; y = 0 é a base da imagem

            var lights = new GameObject("Luminarias").transform;
            lights.SetParent(world.transform, false);
            foreach (var t in Tubes)
            {
                var go = new GameObject("Luminaria");
                go.transform.SetParent(lights, false);
                go.transform.position = new Vector3(X(t.cx), Y(TubePy), 0);
                var cover = SpriteObj("Apagada", go.transform, Spr("pixel"), go.transform.position, -90);
                cover.color = new Color(0.03f, 0.03f, 0.035f, 0f);
                cover.transform.localScale = new Vector3(t.w / 8f, 16f / 8f, 1f);
                var glow = SpriteObj("Halo", go.transform, Spr("brilho_tubo"), go.transform.position, 20);
                glow.color = new Color(0.86f, 0.93f, 0.9f, 0.16f);
                glow.transform.localScale = new Vector3(Mathf.Max(0.6f, t.w * 1.7f / 256f), 1.8f, 1f);
                var fl = go.AddComponent<FluorescentLight>();
                fl.glow = glow;
                fl.cover = cover;
                fl.broken = t.broken;
                fl.glowAlpha = t.broken ? 0.12f : 0.16f;
                fl.sfx = lightsSfx;
                fl.crackle = Clip("luz_falha");
            }

            // ---------- Colisores ----------
            var solids = new GameObject("Colisores").transform;
            solids.SetParent(world.transform, false);
            Box("Chao", solids, X(LevelWidth / 2), Y(FloorPy) - 0.5f, X(LevelWidth), 1f, oneWay: false);
            Box("ParedeEsquerda", solids, X(LeftWallPx) - 0.5f, 6f, 1f, 14f, oneWay: false);
            Box("ParedeDireita", solids, X(RightWallPx) + 0.5f, 6f, 1f, 14f, oneWay: false);
            Box("Teto", solids, X(LevelWidth / 2), LevelHeight / PPU + 0.5f, X(LevelWidth), 1f, oneWay: false);
            for (int i = 0; i < Benches.Length; i++)
            {
                var b = Benches[i];
                Platform($"Banco{i + 1}_Assento", solids, b.x0, b.x1, CushionPy);
                Platform($"Banco{i + 1}_Encosto", solids, b.b0, b.b1, BackrestPy);
            }
            var racks = new GameObject("Bagageiros").transform;
            racks.SetParent(world.transform, false);
            for (int i = 0; i < RackCenters.Length; i++)
            {
                float cx = RackCenters[i];
                SpriteObj($"Bagageiro{i + 1}", racks, Spr("bagageiro"), new Vector3(X(cx), Y(RackPy), 0), -20);
                Platform($"Bagageiro{i + 1}_Plataforma", solids, cx - RackWidthPx / 2 + 6, cx + RackWidthPx / 2 - 6, RackPy);
            }

            // ---------- Porta de saída ----------
            var door = BuildDoor(world.transform, sfx);

            // ---------- Puzzle ----------
            var puzzleGo = new GameObject("PuzzleAlavancas");
            var puzzle = puzzleGo.AddComponent<LeverPuzzle>();
            var levers = new TrainLever[Levers.Length];
            for (int i = 0; i < Levers.Length; i++)
                levers[i] = BuildLever(world.transform, i, Levers[i].x, Levers[i].py, puzzle, sfx);
            var panels = PanelCenters.Select((cx, i) => BuildPanel(world.transform, i, cx)).ToArray();
            puzzle.levers = levers;
            puzzle.order = LeverOrder;
            puzzle.panels = panels;
            puzzle.door = door;
            puzzle.sfx = sfx;
            puzzle.beepSource = beep;
            puzzle.beepClip = Clip("beep_sequencia");
            puzzle.stepClip = Clip("acerto");
            puzzle.wrongClip = Clip("erro_porta_trava");
            puzzle.solvedClip = Clip("acerto");

            // ---------- Jogador (Kai) ----------
            var spawn = new GameObject("SpawnPoint");
            spawn.transform.position = new Vector3(3.6f, Y(FloorPy) + 1.62f, 0);
            var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerGo.name = "Kai (Player)";
            playerGo.transform.position = spawn.transform.position;
            var player = playerGo.GetComponent<PlayerController>();
            player.maxSpeed = 3.8f;
            player.jumpTakeOffSpeed = 7.2f;
            player.gravityModifier = 1.25f;
            player.jumpAudio = null; // o pulo "cartoon" do template não combina com o clima
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
                mainSr.enabled = false; // o boneco do template fica escondido; o Kai é desenhado por cima
                PrefabUtility.RecordPrefabInstancePropertyModifications(mainSr);
            }
            // o desenho fica num "pai" que acerta o tamanho; o CharacterVisual anima só o filho
            var kaiSprite = CharacterSprite("kai");
            var kaiHolder = new GameObject("Kai_Escala").transform;
            kaiHolder.SetParent(playerGo.transform, false);
            kaiHolder.localPosition = new Vector3(0f, -1.6f, 0f); // pés na base do colisor
            kaiHolder.localScale = Vector3.one * CharScale(kaiSprite, 3.4f);
            var kaiVis = SpriteObj("Kai_Desenho", kaiHolder, kaiSprite, kaiHolder.position, 10);
            kaiVis.color = CharTint;
            var cv = kaiVis.gameObject.AddComponent<CharacterVisual>();
            cv.body = player;
            cv.source = mainSr;

            var prompt = SpriteObj("TeclaE", null, Spr("tecla_e"), Vector3.zero, 50);
            prompt.transform.localScale = Vector3.one * 0.75f;
            prompt.enabled = false;
            var interactor = playerGo.AddComponent<PlayerInteractor>();
            interactor.player = player;
            interactor.prompt = prompt;

            // ---------- Game controller do template ----------
            var gcGo = new GameObject("GameController");
            var gc = gcGo.AddComponent<GameController>();
            gc.model.player = player;
            gc.model.spawnPoint = spawn.transform;
            gc.model.virtualCamera = null;
            gc.model.jumpModifier = 1.5f;
            gc.model.jumpDeceleration = 0.5f;

            // ---------- Souta e Ren ----------
            var souta = BuildNpc("Souta", "souta", 12.35f, 3.5f, playerGo.transform, puzzle);
            souta.firstTalk = Lines(
                ("souta", "Kai. Finalmente acordou."),
                ("kai", "Souta? O que você tá fazendo aqui?"),
                ("souta", "Voltando pra casa, igual a você. O trem parou faz uns vinte minutos."),
                ("souta", "As portas laterais não abrem. A do fundo do vagão tem uma trava ligada a três alavancas."),
                ("souta", "Repara no letreiro em cima das portas. Ele acende numa ordem e depois apaga. Não é defeito."),
                ("kai", "<i>Claro que ele já descobriu tudo. Como sempre.</i>"));
            souta.repeatTalk = Lines(
                ("souta", "Três luzes no letreiro: esquerda, meio, direita. Cada uma é uma alavanca."),
                ("souta", "Puxe na mesma ordem em que elas acendem. Não é difícil, Kai."));
            souta.afterSolved = Lines(
                ("souta", "Viu? Quando você presta atenção, você consegue."),
                ("kai", "<i>...Por que isso soa igual a uma bronca do chefe?</i>"));

            var ren = BuildNpc("Ren", "ren", 16.85f, 3.25f, playerGo.transform, puzzle);
            ren.firstTalk = Lines(
                ("ren", "Ah! D-desculpa. Achei que fosse... outra coisa."),
                ("kai", "Quem é você?"),
                ("ren", "Ren. Eu trabalho com vocês dois... no mesmo andar. Você não lembra de mim?"),
                ("kai", "...Não."),
                ("ren", "Tudo bem. Ninguém lembra."),
                ("ren", "Tem uma alavanca em cima do encosto daquele banco. E outras lá no alto, nos bagageiros. Eu não tive coragem de mexer."));
            ren.repeatTalk = Lines(
                ("ren", "Se errar a ordem, a porta trava de novo. Eu ouvi esse barulho... duas vezes. Antes de você acordar."),
                ("ren", "Dá pra subir nos bancos. E do encosto dá pra alcançar os bagageiros."));
            ren.afterSolved = Lines(
                ("ren", "Abriu...? Você vai na frente, né?"));

            // ---------- Câmera ----------
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = 4.7f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.transform.position = new Vector3(spawn.transform.position.x + 5f, 4.7f, -10f);
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow2D>();
            follow.target = playerGo.transform;
            follow.bounds = new Rect(0f, 0f, X(LevelWidth) - 0.6f, LevelHeight / PPU);

            // ---------- Interface ----------
            var ui = BuildUI(fonts.sans, fonts.serif, fonts.italic, voice);

            // ---------- Diretor ----------
            var dirGo = new GameObject("Diretor_Cenario1");
            var dir = dirGo.AddComponent<Cenario1Director>();
            dir.player = player;
            dir.puzzle = puzzle;
            dir.door = door;
            dir.cameraFollow = follow;
            dir.souta = souta;
            dir.ren = ren;
            dir.ambience = ambience;
            dir.intro = Lines(
                ("kai", "<i>...</i>"),
                ("kai", "<i>Peguei no sono no trem de novo.</i>"),
                ("kai", "<i>Parado no meio do túnel. E essas luzes...</i>"),
                ("kai", "<i>\"Próxima parada: terminal.\" Que horas são?</i>"));
            dir.doorLocked = Lines(
                ("kai", "<i>Trancada. A luz vermelha em cima dela não apaga.</i>"));
            dir.firstFail = Lines(
                ("kai", "<i>Ordem errada. As alavancas voltaram sozinhas.</i>"),
                ("kai", "<i>Preciso olhar o letreiro de novo.</i>"));
            dir.solved = Lines(
                ("kai", "<i>Abriu.</i>"),
                ("kai", "<i>O próximo vagão está escuro...</i>"));

            ui.dialogue.speakers = new List<Speaker>
            {
                new Speaker { id = "kai", displayName = "Kai", portrait = Portrait("kai"), nameColor = Hex("C9BFAE") },
                new Speaker { id = "souta", displayName = "Souta", portrait = Portrait("souta"), nameColor = Hex("A9B8C9") },
                new Speaker { id = "ren", displayName = "Ren", portrait = Portrait("ren"), nameColor = Hex("CBB98E") },
            };
        }

        // ---------- Peças do cenário ----------

        static ExitDoor BuildDoor(Transform parent, AudioSource sfx)
        {
            float cx = X((DoorX0 + DoorX1) / 2f), cy = Y((DoorTop + DoorBottom) / 2f);
            float w = X(DoorX1 - DoorX0), h = (DoorBottom - DoorTop) / PPU;

            var root = new GameObject("PortaDoFundo");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(cx, Y(FloorPy), 0);
            var door = root.AddComponent<ExitDoor>();
            door.reachX = 1.5f;
            door.promptOffset = new Vector2(0f, 4.0f);

            SpriteObj("Escuro", root.transform, Spr("porta_interior"), new Vector3(cx, cy, 0), -60);

            var maskGo = new GameObject("Mascara");
            maskGo.transform.SetParent(root.transform, false);
            maskGo.transform.position = new Vector3(cx, cy, 0);
            maskGo.transform.localScale = new Vector3(w / 0.08f, h / 0.08f, 1f); // sprite "pixel" = 8 px = 0.08 u
            maskGo.AddComponent<SpriteMask>().sprite = Spr("pixel");

            var left = SpriteObj("FolhaEsquerda", root.transform, Spr("porta_folha_esq"),
                new Vector3(X((DoorX0 + DoorSplit) / 2f), cy, 0), -59);
            var right = SpriteObj("FolhaDireita", root.transform, Spr("porta_folha_dir"),
                new Vector3(X((DoorSplit + DoorX1) / 2f), cy, 0), -59);
            left.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            right.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            door.leftLeaf = left.transform;
            door.rightLeaf = right.transform;
            door.slideDistance = X(DoorSplit - DoorX0) + 0.05f;

            var signalPos = new Vector3(X(DoorSplit), Y(DoorTop - 10), 0);
            SpriteObj("SinalTrava", root.transform, Spr("sinal_porta"), signalPos, -45);
            var lamp = SpriteObj("SinalTrava_Luz", root.transform, Spr("ponto"), signalPos, -44);
            lamp.transform.localScale = new Vector3(2.6f, 1.2f, 1f);
            var glow = SpriteObj("SinalTrava_Halo", root.transform, Spr("brilho"), signalPos, -43);
            glow.transform.localScale = Vector3.one * 0.35f;
            door.signalLamp = lamp;
            door.signalGlow = glow;
            door.sfx = sfx;
            door.unlockClip = Clip("porta_destrava");
            return door;
        }

        static TrainLever BuildLever(Transform parent, int index, float xPx, float surfacePy, LeverPuzzle puzzle, AudioSource sfx)
        {
            var root = new GameObject($"Alavanca{index + 1}");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(X(xPx), Y(surfacePy), 0);
            var lever = root.AddComponent<TrainLever>();
            lever.reachX = 1.0f;
            lever.minFeetDy = -0.35f;
            lever.maxFeetDy = 0.5f;
            lever.promptOffset = new Vector2(0f, 2.15f);
            lever.puzzle = puzzle;
            lever.sfx = sfx;
            lever.pullClip = Clip("alavanca");

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

        static SequencePanel BuildPanel(Transform parent, int index, float cxPx)
        {
            var root = new GameObject($"Letreiro{index + 1}");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(X(cxPx), Y(PanelPy), 0);
            var panel = root.AddComponent<SequencePanel>();
            panel.dots = new SpriteRenderer[3];
            panel.glows = new SpriteRenderer[3];
            for (int i = 0; i < 3; i++)
            {
                var pos = root.transform.position + new Vector3((i - 1) * X(PanelHalfWidthPx), 0f, 0f);
                var dot = SpriteObj($"Luz{i + 1}", root.transform, Spr("ponto"), pos, -40);
                dot.transform.localScale = Vector3.one * 1.3f;
                var glow = SpriteObj($"Luz{i + 1}_Halo", root.transform, Spr("brilho"), pos, -39);
                glow.transform.localScale = Vector3.one * 0.22f;
                glow.color = new Color(1f, 1f, 1f, 0f);
                panel.dots[i] = dot;
                panel.glows[i] = glow;
            }
            return panel;
        }

        static NpcTalk BuildNpc(string name, string id, float x, float height, Transform player, LeverPuzzle puzzle)
        {
            var root = new GameObject(name);
            root.transform.position = new Vector3(x, Y(FloorPy), 0);
            var npc = root.AddComponent<NpcTalk>();
            npc.reachX = 1.7f;
            npc.promptOffset = new Vector2(0f, height + 0.45f);
            npc.lookAt = player;
            npc.puzzle = puzzle;

            var sprite = CharacterSprite(id);
            float scale = CharScale(sprite, height);
            var holder = new GameObject(name + "_Escala").transform;
            holder.SetParent(root.transform, false);
            holder.localScale = Vector3.one * scale;
            var sr = SpriteObj(name + "_Desenho", holder, sprite, root.transform.position, 6);
            sr.color = CharTint;
            npc.sprite = sr;
            return npc;
        }

        static void Platform(string name, Transform parent, float x0Px, float x1Px, float py)
        {
            Box(name, parent, X((x0Px + x1Px) / 2f), Y(py) - 0.1f, X(x1Px - x0Px), 0.2f, oneWay: true);
        }

        static void Box(string name, Transform parent, float cx, float cy, float w, float h, bool oneWay)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(cx, cy, 0);
            var c = go.AddComponent<BoxCollider2D>();
            c.size = new Vector2(w, h);
            if (oneWay) go.AddComponent<OneWayPlatform>();
        }

        // ---------- Interface ----------

        class UIRefs
        {
            public DialogueBox dialogue;
        }

        static UIRefs BuildUI(TMP_FontAsset sans, TMP_FontAsset serif, TMP_FontAsset italic, AudioSource voice)
        {
            var canvasGo = new GameObject("Interface") { layer = UILayer };
            var canvas = canvasGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            var scaler = canvasGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            var root = canvasGo.transform;

            // atmosfera: vinheta e granulado (o mesmo do menu)
            var vig = Img(Stretch("Vinheta", root), new Color(1f, 1f, 1f, 0.92f));
            vig.sprite = Spr("vinheta");
            var grainTex = AssetDatabase.LoadAssetAtPath<Texture2D>(GrainTexture);
            if (grainTex)
            {
                var grainRt = Stretch("Granulado", root);
                var grain = grainRt.gameObject.AddComponent<RawImage>();
                grain.texture = grainTex;
                grain.color = new Color(1f, 1f, 1f, 0.2f);
                grain.raycastTarget = false;
                var fg = grainRt.gameObject.AddComponent<FilmGrain>();
                var so = new SerializedObject(fg);
                so.FindProperty("grainSize").floatValue = 1.5f;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // objetivo
            var hudRt = Stretch("HUD", root);
            var hud = hudRt.gameObject.AddComponent<Hud>();
            var objRt = Stretch("Objetivo", hudRt);
            var objGroup = objRt.gameObject.AddComponent<CanvasGroup>();
            objGroup.alpha = 0f;
            var objLabel = Label("Rotulo", objRt, "OBJETIVO", sans, 16, Tagline, 64, 58, 600, 24);
            objLabel.characterSpacing = 22;
            Img(TopLeft("Linha", objRt, 64, 70, 46, 1), Rule);
            var objText = Label("Texto", objRt, "", italic, 32, Cream, 64, 82, 1000, 60);

            // dicas de controle
            var controls = Label("Controles", hudRt, "A/D andar   ·   ESPAÇO pular   ·   S descer   ·   E interagir   ·   ESC menu",
                sans, 15, new Color(Muted.r, Muted.g, Muted.b, 0.55f), 64, 1080 - 44, 1400, 24);
            controls.characterSpacing = 6;

            // aviso curto
            var toastRt = Stretch("Aviso", hudRt);
            var toastGroup = toastRt.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            var toast = Label("Texto", toastRt, "", sans, 26, Hex("C8B9A0"), 0, 690, 1920, 40);
            toast.alignment = TextAlignmentOptions.Center;
            toast.characterSpacing = 10;

            // diálogo
            var dlg = BuildDialogue(root, sans, serif, voice);

            // fim do cenário
            var endRt = Stretch("Fim", hudRt);
            var endGroup = endRt.gameObject.AddComponent<CanvasGroup>();
            endGroup.alpha = 0f;
            var endTitle = Label("Titulo", endRt, "", serif, 92, Cream, 0, 470, 1920, 120);
            endTitle.alignment = TextAlignmentOptions.Center;
            var endSub = Label("Subtitulo", endRt, "", sans, 20, Tagline, 0, 590, 1920, 40);
            endSub.alignment = TextAlignmentOptions.Center;
            endSub.characterSpacing = 14;

            // fade (fica por cima de tudo, exceto a cartela de fim)
            var faderRt = Stretch("Fade", root);
            Img(faderRt, Color.black);
            var fader = faderRt.gameObject.AddComponent<CanvasGroup>();
            fader.alpha = 0f; // invisível no Editor; o Hud começa preto ao dar Play
            fader.blocksRaycasts = false;
            endRt.SetParent(root, false); // a cartela aparece por cima do preto
            endRt.SetAsLastSibling();

            hud.fader = fader;
            hud.objectiveGroup = objGroup;
            hud.objectiveText = objText;
            hud.toastGroup = toastGroup;
            hud.toastText = toast;
            hud.endGroup = endGroup;
            hud.endTitle = endTitle;
            hud.endSubtitle = endSub;

            return new UIRefs { dialogue = dlg };
        }

        static DialogueBox BuildDialogue(Transform root, TMP_FontAsset sans, TMP_FontAsset serif, AudioSource voice)
        {
            var rt = Stretch("Dialogo", root);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            var box = rt.gameObject.AddComponent<DialogueBox>();

            // painel na parte de baixo
            var panel = new GameObject("Painel", typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            panel.SetParent(rt, false);
            panel.anchorMin = new Vector2(0, 0);
            panel.anchorMax = new Vector2(1, 0);
            panel.pivot = new Vector2(0.5f, 0);
            panel.offsetMin = new Vector2(150, 48);
            panel.offsetMax = new Vector2(-150, 48 + 250);
            Img(panel, new Color(0.02f, 0.02f, 0.022f, 0.88f));
            var line = new GameObject("Linha", typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            line.SetParent(panel, false);
            line.anchorMin = new Vector2(0, 1);
            line.anchorMax = new Vector2(1, 1);
            line.pivot = new Vector2(0.5f, 1);
            line.offsetMin = new Vector2(0, -2);
            line.offsetMax = Vector2.zero;
            Img(line, Rule);

            // retrato (sobe um pouco acima do painel, estilo visual novel)
            var frame = new GameObject("Retrato", typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            frame.SetParent(panel, false);
            frame.anchorMin = frame.anchorMax = new Vector2(0, 0);
            frame.pivot = new Vector2(0, 0);
            frame.anchoredPosition = new Vector2(18, 0);
            frame.sizeDelta = new Vector2(250, 320);
            var portrait = Img(frame, Color.white);
            portrait.preserveAspect = true;

            var name = Label("Nome", panel, "", sans, 26, Cream, 300, 26, 900, 36);
            name.characterSpacing = 16;
            var body = Label("Fala", panel, "", serif, 38, Cream, 300, 72, 1300, 160);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.overflowMode = TextOverflowModes.Overflow;
            body.alignment = TextAlignmentOptions.TopLeft;
            var hint = Label("Continuar", panel, "E  ›", sans, 20, Muted, 0, 0, 200, 30);
            var hrt = hint.rectTransform;
            hrt.anchorMin = hrt.anchorMax = new Vector2(1, 0);
            hrt.pivot = new Vector2(1, 0);
            hrt.anchoredPosition = new Vector2(-30, 22);
            hint.alignment = TextAlignmentOptions.BottomRight;

            box.group = group;
            box.portrait = portrait;
            box.portraitFrame = frame.gameObject;
            box.nameText = name;
            box.bodyText = body;
            box.continueHint = hint;
            box.voice = voice;
            box.blip = Clip("dialogo_blip");
            return box;
        }

        // ---------- Utilitários de UI ----------

        /// <summary>Texto com o canto superior esquerdo em (x, y) de uma tela 1920×1080 (y para baixo).</summary>
        static TextMeshProUGUI Label(string name, Transform parent, string text, TMP_FontAsset font, float size, Color color,
            float x, float y, float w, float h)
        {
            var rt = TopLeft(name, parent, x, y, w, h);
            var t = rt.gameObject.AddComponent<TextMeshProUGUI>();
            if (font) t.font = font;
            t.text = text;
            t.fontSize = size;
            t.color = color;
            t.alignment = TextAlignmentOptions.TopLeft;
            t.textWrappingMode = TextWrappingModes.NoWrap;
            t.raycastTarget = false;
            return t;
        }

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

        // ---------- Utilitários de cena ----------

        static SpriteRenderer SpriteObj(string name, Transform parent, Sprite sprite, Vector3 worldPos, int order)
        {
            var go = new GameObject(name);
            if (parent) go.transform.SetParent(parent, false);
            go.transform.position = worldPos;
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = sprite;
            sr.sortingOrder = order;
            return sr;
        }

        static AudioSource Source(GameObject parent, string name, AudioClip clip, bool loop = false)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent.transform, false);
            var s = go.AddComponent<AudioSource>();
            s.clip = clip;
            s.loop = loop;
            s.playOnAwake = false;
            s.spatialBlend = 0f;
            return s;
        }

        static List<DialogueLine> Lines(params (string speaker, string text)[] lines)
        {
            return lines.Select(l => new DialogueLine(l.speaker, l.text)).ToList();
        }

        // ---------- Assets ----------

        /// <summary>Configura a importação de todas as imagens do cenário (sprite, 100 px/unidade, sem compressão).</summary>
        static void ImportArt()
        {
            var pivots = new Dictionary<string, Vector2>
            {
                { "cenario1_fundo", new Vector2(0f, 0f) },
                { "bagageiro", new Vector2(0.5f, (58f - 6f) / 58f) },
                { "alavanca_base", new Vector2(0.5f, 0f) },
                { "alavanca_cabo", new Vector2(0.5f, 0f) },
                { "kai_cena", new Vector2(0.5f, 0f) },
                { "souta_cena", new Vector2(0.5f, 0f) },
                { "ren_cena", new Vector2(0.5f, 0f) },
            };
            var paths = AssetDatabase.FindAssets("t:Texture2D", new[] { Art }).Select(AssetDatabase.GUIDToAssetPath).ToList();
            foreach (var path in paths)
            {
                string file = Path.GetFileNameWithoutExtension(path);
                var pivot = pivots.TryGetValue(file, out var p) ? p : new Vector2(0.5f, 0.5f);
                ConfigureSprite(path, pivot, PPU, file == "cenario1_fundo" ? 8192 : 2048);
            }
            // arte definitiva dos personagens (opcional): pivô nos pés
            if (AssetDatabase.IsValidFolder(FinalCharArt))
            {
                foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { FinalCharArt }).Select(AssetDatabase.GUIDToAssetPath))
                {
                    bool body = !Path.GetFileName(path).Contains("retrato");
                    ConfigureSprite(path, body ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f), PPU, 2048);
                }
            }
        }

        static void ConfigureSprite(string path, Vector2 pivot, float ppu, int maxSize)
        {
            var imp = AssetImporter.GetAtPath(path) as TextureImporter;
            if (imp == null) return;
            var s = new TextureImporterSettings();
            imp.ReadTextureSettings(s);
            bool changed = imp.textureType != TextureImporterType.Sprite
                           || imp.spriteImportMode != SpriteImportMode.Single
                           || !Mathf.Approximately(imp.spritePixelsPerUnit, ppu)
                           || s.spriteAlignment != (int)SpriteAlignment.Custom
                           || s.spritePivot != pivot
                           || imp.maxTextureSize != maxSize
                           || imp.textureCompression != TextureImporterCompression.Uncompressed
                           || imp.mipmapEnabled
                           || imp.filterMode != FilterMode.Bilinear;
            if (!changed) return;

            imp.textureType = TextureImporterType.Sprite;
            imp.spriteImportMode = SpriteImportMode.Single;
            imp.ReadTextureSettings(s);
            s.spriteAlignment = (int)SpriteAlignment.Custom;
            s.spritePivot = pivot;
            s.spritePixelsPerUnit = ppu;
            s.spriteMeshType = SpriteMeshType.FullRect;
            imp.SetTextureSettings(s);
            imp.alphaIsTransparency = true;
            imp.mipmapEnabled = false;
            imp.wrapMode = TextureWrapMode.Clamp;
            imp.filterMode = FilterMode.Bilinear;
            imp.maxTextureSize = maxSize;
            imp.textureCompression = TextureImporterCompression.Uncompressed;
            imp.SaveAndReimport();
        }

        static Sprite Spr(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Art}/{name}.png");
            if (!s) Debug.LogWarning("[Última Linha] Sprite não encontrado: " + name);
            return s;
        }

        /// <summary>Desenho do personagem no cenário: usa a arte definitiva se existir, senão a provisória.</summary>
        static Sprite CharacterSprite(string id)
        {
            foreach (var path in new[]
                     {
                         $"{FinalCharArt}/{id}_cena.png",
                         $"{FinalCharArt}/{id}_frente_transparente.png",
                         $"{CharArt}/{id}_cena.png",
                     })
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>(path);
                if (s) return s;
            }
            Debug.LogWarning("[Última Linha] Sem desenho para " + id);
            return null;
        }

        static Sprite Portrait(string id)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{FinalCharArt}/{id}_retrato_dialogo.png");
            if (!s) s = AssetDatabase.LoadAssetAtPath<Sprite>($"{CharArt}/{id}_retrato_provisorio.png");
            return s;
        }

        /// <summary>Escala para o personagem ficar com a altura desejada (em unidades), qualquer que seja a arte.</summary>
        static float CharScale(Sprite s, float height)
        {
            if (!s || s.bounds.size.y <= 0.01f) return 1f;
            float scale = height / s.bounds.size.y;
            return Mathf.Abs(scale - 1f) < 0.02f ? 1f : scale;
        }

        static AudioClip Clip(string name)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"{AudioFolder}/{name}.wav");
            if (!c) Debug.LogWarning("[Última Linha] Som não encontrado: " + name);
            return c;
        }

        static TMP_FontAsset LoadFont(string file)
        {
            var f = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>($"{FontFolder}/{file} SDF.asset");
            return f ? f : TMP_Settings.defaultFontAsset;
        }

        static Color Hex(string hex) => ColorUtility.TryParseHtmlString("#" + hex, out var c) ? c : Color.magenta;
    }
}
