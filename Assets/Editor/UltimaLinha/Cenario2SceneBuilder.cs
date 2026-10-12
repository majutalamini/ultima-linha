using System.Collections.Generic;
using System.IO;
using System.Linq;
using Platformer.Mechanics;
using TMPro;
using UltimaLinha.Cenario1;
using UltimaLinha.Cenario2;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UltimaLinha.EditorTools.Cenario1SceneBuilder;
using L = UltimaLinha.EditorTools.Cenario2Layout;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Monta a cena Assets/Scenes/Cenario2.unity: os últimos vagões do trem, agora andando.
    /// Quatro vagões ligados por três passagens; em cada passagem há uma porta em cada ponta que abre e
    /// bate num ritmo (puzzle de tempo, como pede o Notion) e um vão no chão entre os vagões.
    /// No último vagão o trem para na estação terminal e a porta do fim abre para a plataforma.
    ///
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Cenário 2.
    ///
    /// As medidas do fundo vêm de Cenario2Layout.cs (gerado por Ferramentas/Cenario2/cenario2.py).
    /// Reaproveita peças do Cenário 1 (personagens articulados, caixa de diálogo, HUD).
    /// </summary>
    [InitializeOnLoad]
    public static class Cenario2SceneBuilder
    {
        const int BuildVersion = 1; // aumente para forçar a cena a ser recriada automaticamente

        const string ScenePath = "Assets/Scenes/Cenario2.unity";
        const string Art2 = "Assets/Art/Cenario2";
        const string Audio2 = "Assets/Audio/Cenario2";
        const string PlayerPrefab = "Assets/Prefabs/Player.prefab";

        const float PPU = 100f;
        static float X(float px) => px / PPU;
        static float Y(float py) => (L.ImageHeight - py) / PPU;
        static float Floor => Y(L.FloorPy);
        static float DoorwayHeight => Y(L.DoorTopPy) - Y(L.FloorPy);
        const float LeftWallPx = 225f;
        static float RightWallPx => L.LevelWidth - 40f;

        // ordens de desenho
        const int DoorOrder = 30, WindowStreakOrder = -93;

        // ritmo de cada passagem: (aberta, aviso, batendo, fechada, abrindo, atraso da porta B, padrão de abertura)
        static readonly (float open, float warn, float close, float closed, float opening, float offsetB, float[] pattern)[] Rhythm =
        {
            (2.8f, 0.8f, 0.20f, 1.4f, 0.7f, 0.0f, null),                            // 1: as duas juntas, devagar
            (2.0f, 0.6f, 0.20f, 1.6f, 0.6f, 2.5f, null),                            // 2: alternadas (espera no engate)
            (1.9f, 0.5f, 0.16f, 1.0f, 0.5f, 0.0f, new[] { 1.9f, 1.2f, 2.5f, 1.0f }), // 3: rápidas e irregulares
        };

        static string VersionKey => "UltimaLinha.Cenario2.Version:" + Application.dataPath;
        static double firstTry = -1;

        static Cenario2SceneBuilder()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        static void TryAutoBuild()
        {
            if (EditorPrefs.GetInt(VersionKey, 0) >= BuildVersion) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (EditorApplication.isCompiling || EditorApplication.isUpdating)
            {
                if (firstTry < 0) firstTry = EditorApplication.timeSinceStartup;
                if (EditorApplication.timeSinceStartup - firstTry < 120) EditorApplication.delayCall += TryAutoBuild;
                else Debug.Log("[Última Linha] Não consegui criar o Cenário 2 sozinho. Use o menu  Última Linha > Abrir Cenário 2.");
                return;
            }
            if (Build(interactive: false))
            {
                EditorPrefs.SetInt(VersionKey, BuildVersion);
                Debug.Log("[Última Linha] Cenário 2 criado em " + ScenePath + ". Abra pelo menu Última Linha > Abrir Cenário 2.");
            }
        }

        [MenuItem("Última Linha/Recriar Cenário 2")]
        static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Recriar Cenário 2",
                    "A cena Cenario2 já existe. Recriar vai substituir qualquer mudança feita nela. Continuar?",
                    "Recriar", "Cancelar"))
                return;
            if (Build(interactive: true))
                EditorPrefs.SetInt(VersionKey, BuildVersion);
        }

        [MenuItem("Última Linha/Abrir Cenário 2")]
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
                Debug.Log("[Última Linha] Salve suas cenas e use o menu  Última Linha > Recriar Cenário 2.");
                return false;
            }

            ImportArt();   // peças do Cenário 1 (personagens, tecla E, halos...)
            ImportArt2();
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            if (!playerPrefab)
            {
                Debug.LogError("[Última Linha] Não achei " + PlayerPrefab + " (jogador do template).");
                return false;
            }
            if (!Spr2("cenario2_fundo"))
            {
                Debug.LogError("[Última Linha] Falta a arte do Cenário 2 em " + Art2 + ".");
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
                EditorUtility.DisplayDialog("Cenário 2 pronto", "Cena salva em " + ScenePath + ". Abrir agora?", "Abrir", "Depois"))
                OpenScene();
            return true;
        }

        /// <summary>Coloca o Cenário 2 logo depois do Cenário 1 na lista de cenas (a porta do Cenário 1 leva até ele).</summary>
        static void AddToBuildSettings()
        {
            var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
            int after = scenes.FindIndex(s => s.path.EndsWith("/Cenario1.unity"));
            if (after < 0) after = scenes.FindIndex(s => s.path.EndsWith("/MainMenu.unity"));
            scenes.Insert(after >= 0 ? after + 1 : scenes.Count, new EditorBuildSettingsScene(ScenePath, true));
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
            var rumble = Source(audioRoot, "TremAndando", Clip2("trem_andando"), loop: true);
            var station = Source(audioRoot, "Estacao", Clip2("estacao_ambiente"), loop: true);
            var sfx = Source(audioRoot, "Efeitos", null);
            var doorSfx = Source(audioRoot, "Portas", null);
            var voice = Source(audioRoot, "VozDialogo", null);
            voice.volume = 0.5f;

            // ---------- Cenário ----------
            var world = new GameObject("Cenario");
            var bg = SpriteObj("Fundo", world.transform, Spr2("cenario2_fundo"), Vector3.zero, -100);
            bg.transform.position = Vector3.zero;

            // halos das luminárias (piscam de vez em quando; sem estalo)
            var lights = new GameObject("Luminarias").transform;
            lights.SetParent(world.transform, false);
            for (int i = 0; i < L.Tubes.Length; i++)
            {
                var tube = L.Tubes[i];
                var go = new GameObject("Luminaria" + (i + 1));
                go.transform.SetParent(lights, false);
                go.transform.position = new Vector3(X(tube.x), Y(111f), 0);
                var cover = SpriteObj("Apagada", go.transform, Spr("pixel"), go.transform.position, -90);
                cover.color = new Color(0.03f, 0.03f, 0.035f, 0f);
                cover.transform.localScale = new Vector3(tube.y / 8f, 16f / 8f, 1f);
                var glow = SpriteObj("Halo", go.transform, Spr("brilho_tubo"), go.transform.position, 20);
                glow.color = new Color(0.86f, 0.93f, 0.9f, 0.15f);
                glow.transform.localScale = new Vector3(Mathf.Max(0.6f, tube.y * 1.7f / 256f), 1.8f, 1f);
                var fl = go.AddComponent<FluorescentLight>();
                fl.glow = glow;
                fl.cover = cover;
                fl.broken = i >= 4 && i <= 5; // terceiro vagão: luzes falhando
                fl.glowAlpha = fl.broken ? 0.11f : 0.15f;
                fl.crackle = null;
            }

            // ---------- Colisores ----------
            var solids = new GameObject("Colisores").transform;
            solids.SetParent(world.transform, false);
            // chão: inteiro, menos os vãos entre os vagões
            var holes = L.Holes.SelectMany(h => h).OrderBy(h => h.x).ToList();
            float from = LeftWallPx;
            int n = 0;
            foreach (var h in holes.Append(new Vector2(RightWallPx, RightWallPx)))
            {
                if (h.x - from > 4f)
                    Box($"Chao{++n}", solids, X((from + h.x) / 2f), Floor - 0.5f, X(h.x - from), 1f, oneWay: false);
                from = h.y;
            }
            Box("ParedeEsquerda", solids, X(LeftWallPx) - 0.5f, 6f, 1f, 14f, oneWay: false);
            Box("ParedeDireita", solids, X(RightWallPx) + 0.5f, 6f, 1f, 14f, oneWay: false);
            Box("Teto", solids, X(L.LevelWidth / 2f), L.LevelHeight / PPU + 0.5f, X(L.LevelWidth), 1f, oneWay: false);
            // parede acima de cada passagem (não dá para pular por cima da porta)
            var slabs = L.SlabA.Concat(L.SlabB).Append(L.EndSlabX).ToList();
            float top = L.LevelHeight / PPU + 1f;
            foreach (var sx in slabs)
            {
                float y0 = Y(L.DoorTopPy);
                Box("ParedeSobrePorta", solids, X(sx + L.Slab / 2f), (y0 + top) / 2f, X(L.Slab), top - y0, oneWay: false);
            }

            // ---------- Portas entre vagões ----------
            var doorsRoot = new GameObject("PortasEntreVagoes").transform;
            doorsRoot.SetParent(world.transform, false);
            var doors = new List<CarDoor>();
            for (int i = 0; i < Rhythm.Length; i++)
            {
                var r = Rhythm[i];
                for (int side = 0; side < 2; side++)
                {
                    float sx = side == 0 ? L.SlabA[i] : L.SlabB[i];
                    var d = BuildCarDoor(doorsRoot, $"Passagem{i + 1}_Porta{(side == 0 ? "A" : "B")}", sx, doorSfx);
                    d.openTime = r.open;
                    d.warnTime = r.warn;
                    d.closeTime = r.close;
                    d.closedTime = r.closed;
                    d.openingTime = r.opening;
                    d.openPattern = r.pattern ?? new float[0];
                    d.startOffset = side == 1 ? r.offsetB : 0f;
                    doors.Add(d);
                }
            }
            // porta do fim do último vagão: parada e fechada até o trem parar
            var exitDoor = BuildCarDoor(world.transform, "PortaDoFim", L.EndSlabX, doorSfx);
            exitDoor.cycling = false;
            exitDoor.startClosed = true;
            exitDoor.openingTime = 1.6f;

            // ---------- Luzes do túnel nas janelas ----------
            var tunnelGo = new GameObject("LuzesDoTunel");
            tunnelGo.transform.SetParent(world.transform, false);
            var tunnel = tunnelGo.AddComponent<TunnelLights>();
            var wins = new List<TunnelLights.Window>();
            for (int i = 0; i < L.Windows.Length; i++)
            {
                var w = L.Windows[i];
                float x0 = X(w.x), x1 = X(w.y), y0 = Y(w.w), y1 = Y(w.z);
                var mask = new GameObject("Janela" + (i + 1));
                mask.transform.SetParent(tunnelGo.transform, false);
                mask.transform.position = new Vector3((x0 + x1) / 2f, (y0 + y1) / 2f, 0f);
                mask.transform.localScale = new Vector3((x1 - x0) / 0.08f, (y1 - y0) / 0.08f, 1f);
                var sm = mask.AddComponent<SpriteMask>();
                sm.sprite = Spr("pixel");
                sm.isCustomRangeActive = true;
                sm.backSortingOrder = WindowStreakOrder - 2;
                sm.frontSortingOrder = WindowStreakOrder + 1;
                var streak = SpriteObj("Luz", tunnelGo.transform, Spr2("luz_tunel"), mask.transform.position, WindowStreakOrder);
                streak.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                streak.color = new Color(0.85f, 0.9f, 1f, 0f);
                wins.Add(new TunnelLights.Window { streak = streak, x0 = x0, x1 = x1, y0 = y0, y1 = y1 });
            }
            tunnel.windows = wins.ToArray();

            // ---------- Jogador (Kai) ----------
            float spawnY = Floor + 1.62f;
            var spawn = new GameObject("SpawnPoint");
            spawn.transform.position = new Vector3(3.6f, spawnY, 0);
            var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerGo.name = "Kai (Player)";
            playerGo.transform.position = spawn.transform.position;
            var player = playerGo.GetComponent<PlayerController>();
            player.maxSpeed = 3.4f;
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
            var kaiRig = BuildRig("kai", RigStyle.Kai, playerGo.transform, new Vector3(0f, -1.6f, 0f), 10, 1f);
            kaiRig.mover = player;
            kaiRig.flipSource = mainSr;
            kaiRig.strideLength = 2.3f;

            var prompt = SpriteObj("TeclaE", null, Spr("tecla_e"), Vector3.zero, 50);
            prompt.transform.localScale = Vector3.one * 0.75f;
            prompt.enabled = false;
            var interactor = playerGo.AddComponent<PlayerInteractor>();
            interactor.player = player;
            interactor.prompt = prompt;

            foreach (var d in doors) d.listener = playerGo.transform;
            exitDoor.listener = playerGo.transform;

            var gcGo = new GameObject("GameController");
            var gc = gcGo.AddComponent<GameController>();
            gc.model.player = player;
            gc.model.spawnPoint = spawn.transform;
            gc.model.virtualCamera = null;
            gc.model.jumpModifier = 1.5f;
            gc.model.jumpDeceleration = 0.5f;

            // ---------- Souta e Ren ----------
            var souta = BuildNpc("Souta", "souta", 9.2f, 3.55f, playerGo.transform);
            souta.firstTalk = Lines(
                ("souta", "Vai, Kai. Antes que ele volte a andar."));
            var ren = BuildNpc("Ren", "ren", 11.6f, 3.3f, playerGo.transform);
            ren.firstTalk = Lines(
                ("ren", "Você ouviu? Parecia alguém batendo... do outro lado daquela porta."),
                ("kai", "<i>Não tem ninguém atrás da gente. Tem?</i>"));

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
            follow.bounds = new Rect(0f, 0f, X(L.LevelWidth) - 0.6f, L.LevelHeight / PPU);

            // ---------- Interface ----------
            var ui = BuildUI(fonts.sans, fonts.serif, fonts.italic, voice);
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            // ---------- Diretor ----------
            var dir = new GameObject("Diretor_Cenario2").AddComponent<Cenario2Director>();
            dir.player = player;
            dir.cameraFollow = follow;
            dir.doors = doors.ToArray();
            dir.exitDoor = exitDoor;
            dir.tunnel = tunnel;
            dir.souta = souta;
            dir.ren = ren;
            dir.soutaEnd = new Vector3(X(L.CarX[3] + 820f), Floor, 0f);
            dir.renEnd = new Vector3(X(L.CarX[3] + 610f), Floor, 0f);
            dir.checkpoints = new[] { 3.6f, X(L.CarX[1] + 170f), X(L.CarX[2] + 170f), X(L.CarX[3] + 170f) };
            dir.spawnY = spawnY;
            dir.killY = Floor - 2.2f;
            dir.gang2HintX = X(L.SlabA[1] - 420f);
            dir.gang3HintX = X(L.SlabA[2] - 420f);
            dir.endTriggerX = X(L.CarX[3] + 300f);
            dir.exitX = X(L.EndSlabX + L.Slab + 110f);
            dir.rumble = rumble;
            dir.stationAmbience = station;
            dir.sfx = sfx;
            dir.crushClip = Clip2("esmagado");
            dir.fallClip = Clip2("queda");
            dir.lurchClip = Clip2("tranco");
            dir.brakeClip = Clip2("freio");
            dir.flash = ui.flash;

            dir.intro = Lines(
                ("kai", "<i>Mais um vagão. Mais escuro que o outro.</i>"),
                ("kai", "<i>...O trem voltou a andar?</i>"),
                ("souta", "Finalmente. Achei que a gente ia passar a noite parado naquele túnel."),
                ("ren", "Não é normal. Esse trem não devia estar andando. Não com a gente dentro."),
                ("souta", "Olha as portas entre os vagões. Com o tranco, elas abrem e batem sozinhas."),
                ("souta", "Tem um ritmo. A luz em cima pisca antes de bater. E cuidado com o vão no chão entre os vagões."),
                ("ren", "E se ela fechar com alguém no meio...?"));
            dir.goAhead = Lines(
                ("souta", "Então não fique no meio."),
                ("souta", "Eu vou na frente com o Ren. A gente te espera no último vagão."),
                ("kai", "<i>Sempre na frente. Sempre.</i>"));
            dir.alone = Lines(
                ("kai", "<i>Eles já passaram.</i>"),
                ("kai", "<i>Por um segundo eu pensei... e se a porta fechasse antes dele passar?</i>"),
                ("kai", "<i>...Que pensamento é esse, Kai.</i>"));
            dir.firstCrush = Lines(
                ("kai", "<i>Preciso contar. Abre... pisca... bate.</i>"),
                ("kai", "<i>Passo logo depois que ela abrir, nunca quando a luz estiver piscando.</i>"));
            dir.firstFall = Lines(
                ("kai", "<i>O vão entre os vagões... Tenho que pular da beirada, correndo.</i>"));
            dir.gang2Hint = Lines(
                ("kai", "<i>Essas duas portas não abrem juntas.</i>"),
                ("kai", "<i>Dá pra esperar em cima do engate, no meio do vão, até a outra abrir.</i>"));
            dir.gang3Hint = Lines(
                ("kai", "<i>Essa abre cada vez por um tempo diferente. E o vão é bem maior.</i>"),
                ("kai", "<i>Vou ter que correr antes de pular.</i>"));
            dir.braking = Lines(
                ("kai", "<i>Está freando...!</i>"));
            dir.ending = Lines(
                ("souta", "Demorou, Kai."),
                ("ren", "A gente passou logo antes de você... eu acho. Eu não lembro de ter passado."),
                ("souta", "Ele parou. Lá fora é uma estação."),
                ("kai", "<i>\"Terminal.\" A última parada.</i>"),
                ("souta", "A porta do fim abriu. Vamos sair daqui."));

            ui.dialogue.speakers = new List<Speaker>
            {
                new Speaker { id = "kai", displayName = "Kai", portrait = Portrait("kai"), nameColor = Hex("C9BFAE") },
                new Speaker { id = "souta", displayName = "Souta", portrait = Portrait("souta"), nameColor = Hex("A9B8C9") },
                new Speaker { id = "ren", displayName = "Ren", portrait = Portrait("ren"), nameColor = Hex("CBB98E") },
            };
        }

        // ---------- Peças ----------

        /// <summary>Porta entre vagões numa parede (sx = borda esquerda da parede, em px).</summary>
        static CarDoor BuildCarDoor(Transform parent, string name, float sx, AudioSource sfx)
        {
            float cx = X(sx + L.Slab / 2f);
            float h = DoorwayHeight;
            float w = X(L.Slab - 2f * L.Post);

            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(cx, Floor, 0f);
            var door = root.AddComponent<CarDoor>();
            door.doorwayHeight = h;
            door.halfWidth = w / 2f;

            // a folha só aparece dentro da passagem (sobe e some dentro da parede)
            var maskGo = new GameObject("Passagem");
            maskGo.transform.SetParent(root.transform, false);
            maskGo.transform.position = new Vector3(cx, Floor + h / 2f, 0f);
            maskGo.transform.localScale = new Vector3(w / 0.08f, h / 0.08f, 1f);
            var mask = maskGo.AddComponent<SpriteMask>();
            mask.sprite = Spr("pixel");
            mask.isCustomRangeActive = true;
            mask.backSortingOrder = DoorOrder - 1;
            mask.frontSortingOrder = DoorOrder;

            var panelRoot = new GameObject("Folha").transform;
            panelRoot.SetParent(root.transform, false);
            panelRoot.localPosition = new Vector3(0f, h, 0f);
            var panel = SpriteObj("Folha_Desenho", panelRoot, Spr2("porta_vagao"), root.transform.position, DoorOrder);
            panel.transform.localPosition = Vector3.zero;
            panel.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            door.panel = panelRoot;

            var block = new GameObject("Bloqueio");
            block.transform.SetParent(root.transform, false);
            var col = block.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, 0.05f);
            col.enabled = false;
            door.blocker = col;

            var lampPos = new Vector3(cx, Y(L.DoorTopPy - 34f), 0f);
            var lamp = SpriteObj("Aviso", root.transform, Spr("ponto"), lampPos, DoorOrder + 2);
            lamp.transform.localScale = new Vector3(2.4f, 1.2f, 1f);
            var glow = SpriteObj("Aviso_Halo", root.transform, Spr("brilho"), lampPos, DoorOrder + 3);
            glow.transform.localScale = Vector3.one * 0.45f;
            door.lamp = lamp;
            door.lampGlow = glow;

            door.sfx = sfx;
            door.slamClip = Clip2("porta_bate");
            door.warnClip = Clip2("porta_aviso");
            door.openClip = Clip2("porta_abre");
            return door;
        }

        static NpcTalk BuildNpc(string name, string id, float x, float height, Transform player)
        {
            var root = new GameObject(name);
            root.transform.position = new Vector3(x, Floor, 0);
            var npc = root.AddComponent<NpcTalk>();
            npc.reachX = 1.7f;
            npc.promptOffset = new Vector2(0f, height + 0.45f);
            npc.lookAt = player;
            var style = id == "souta" ? RigStyle.Souta : RigStyle.Ren;
            npc.rig = BuildRig(id, style, root.transform, Vector3.zero, 6, id == "souta" ? 1.02f : 0.97f);
            return npc;
        }

        // ---------- Interface ----------

        class UIRefs
        {
            public DialogueBox dialogue;
            public CanvasGroup flash;
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
            canvasGo.AddComponent<GraphicRaycaster>();
            var root = canvasGo.transform;

            // só a vinheta (sem granulado: nada de "chiado" durante o jogo)
            var vig = Img(Stretch("Vinheta", root), new Color(1f, 1f, 1f, 0.92f));
            vig.sprite = Spr("vinheta");

            // clarão vermelho quando a porta pega o Kai
            var flashRt = Stretch("Clarao", root);
            Img(flashRt, new Color(0.55f, 0.02f, 0.02f, 1f));
            var flash = flashRt.gameObject.AddComponent<CanvasGroup>();
            flash.alpha = 0f;
            flash.blocksRaycasts = false;

            var hudRt = Stretch("HUD", root);
            var hud = hudRt.gameObject.AddComponent<Hud>();
            var objRt = Stretch("Objetivo", hudRt);
            var objGroup = objRt.gameObject.AddComponent<CanvasGroup>();
            objGroup.alpha = 0f;
            var objLabel = Label("Rotulo", objRt, "OBJETIVO", sans, 16, Tagline, 64, 58, 600, 24);
            objLabel.characterSpacing = 22;
            Img(TopLeft("Linha", objRt, 64, 70, 46, 1), Rule);
            var objText = Label("Texto", objRt, "", italic, 32, Cream, 64, 82, 1200, 60);

            var controls = Label("Controles", hudRt, "A/D andar   ·   ESPAÇO pular   ·   E interagir   ·   ESC menu",
                sans, 15, new Color(Muted.r, Muted.g, Muted.b, 0.55f), 64, 1080 - 44, 1400, 24);
            controls.characterSpacing = 6;

            var toastRt = Stretch("Aviso", hudRt);
            var toastGroup = toastRt.gameObject.AddComponent<CanvasGroup>();
            toastGroup.alpha = 0f;
            var toast = Label("Texto", toastRt, "", sans, 26, Hex("C8B9A0"), 0, 690, 1920, 40);
            toast.alignment = TextAlignmentOptions.Center;
            toast.characterSpacing = 10;

            var dlg = BuildDialogue(root, sans, serif, voice);

            var endRt = Stretch("Fim", hudRt);
            var endGroup = endRt.gameObject.AddComponent<CanvasGroup>();
            endGroup.alpha = 0f;
            var endTitle = Label("Titulo", endRt, "", serif, 92, Cream, 0, 470, 1920, 120);
            endTitle.alignment = TextAlignmentOptions.Center;
            var endSub = Label("Subtitulo", endRt, "", sans, 20, Tagline, 0, 590, 1920, 40);
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

            return new UIRefs { dialogue = dlg, flash = flash };
        }

        // ---------- Assets ----------

        static void ImportArt2()
        {
            var pivots = new Dictionary<string, Vector2>
            {
                { "cenario2_fundo", new Vector2(0f, 0f) },
                { "porta_vagao", new Vector2(0.5f, 0f) },
            };
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { Art2 }).Select(AssetDatabase.GUIDToAssetPath))
            {
                string file = Path.GetFileNameWithoutExtension(path);
                var pivot = pivots.TryGetValue(file, out var p) ? p : new Vector2(0.5f, 0.5f);
                ConfigureSprite(path, pivot, PPU, file == "cenario2_fundo" ? 8192 : 2048);
            }
        }

        static Sprite Spr2(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Art2}/{name}.png");
            if (!s) Debug.LogWarning("[Última Linha] Sprite não encontrado: " + name);
            return s;
        }

        static AudioClip Clip2(string name)
        {
            var c = AssetDatabase.LoadAssetAtPath<AudioClip>($"{Audio2}/{name}.wav");
            if (!c) Debug.LogWarning("[Última Linha] Som não encontrado: " + name);
            return c;
        }
    }
}
