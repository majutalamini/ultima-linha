using System.Collections.Generic;
using System.IO;
using System.Linq;
using Platformer.Mechanics;
using TMPro;
using UltimaLinha.Cenario1;
using UltimaLinha.Cenario2;
using UltimaLinha.Salas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UltimaLinha.EditorTools.Cenario1SceneBuilder;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Monta a cena Assets/Scenes/Cenario2.unity: os últimos quatro vagões do trem, agora andando, no mesmo corte e na
    /// mesma escala dos Cenários 0 e 1 (vagao_antigo em pixel art, pontas com porta de dobradiça, túnel entre os vagões).
    /// Em cada vagão: algo para ler, um puzzle e uma conversa quando ele é resolvido (Ferramentas/Cenario2/PLANO.md).
    /// - Vagão 1: cadeado de rodas na porta do fundo (código no relógio e no celular).
    /// - Passagem 1: as duas portas batem juntas, vão curto.
    /// - Vagão 2 (curto): a porta da passagem 2 bate rápido demais; a alavanca de retenção, longe dela, a segura aberta.
    /// - Passagem 2: dois buracos com o engate no meio (espera a outra porta em cima dele).
    /// - Vagão 3: sem energia, a luz das portas não acende; três fusíveis espalhados, cada um junto de algo para ler.
    /// - Passagem 3: portas irregulares e vão grande.
    /// - Vagão 4 (curto): o trem freia na estação, que aparece pelas janelas; a porta do fim abre.
    ///
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Cenário 2.
    /// Objetos em Assets/Art/Cenario2/Objetos (Ferramentas/Cenario2/objetos_arte.py).
    /// </summary>
    [InitializeOnLoad]
    public static class Cenario2SceneBuilder
    {
        const int BuildVersion = 4; // aumente para forçar a cena a ser recriada automaticamente

        const string ScenePath = "Assets/Scenes/Cenario2.unity";
        const string Art2 = "Assets/Art/Cenario2";
        const string Obj2 = Art2 + "/Objetos/";
        const string VagaoCurto = Art2 + "/vagao_curto.png";
        const string Audio2 = "Assets/Audio/Cenario2";
        const string PlayerPrefab = "Assets/Prefabs/Player.prefab";

        // ---------- Medidas ----------
        // Tudo em unidades do mundo, na grade dos personagens (58 px por unidade): px do vagao_antigo / 58.
        static float A(float px) => px / PixelArtImporter.Ppu;
        static float AY(float pyTopo) => (540f - pyTopo) / PixelArtImporter.Ppu;
        /// <summary>Altura (px do fundo original do Cenário 1, como Cenario1SceneBuilder) em unidades do mundo.</summary>
        static float OY(float py) => (941f - py) / 100f * EscalaVagao;
        const float FundoX = 2075f / 4230f; // px do vagao_antigo por px do fundo original
        static float Chao => ChaoMundo;
        // vagão curto: sem o trecho entre o meio da 1ª e o da 2ª porta lateral (Ferramentas/Cenario2/objetos_arte.py)
        const float Emenda0 = 616f, Emenda1 = 1266f, Trecho = Emenda1 - Emenda0;

        class Vagao
        {
            public float x;    // origem do desenho (canto de baixo à esquerda)
            public bool curto;
            public float Ini => x + XIniVagao;
            public float Fim => x + XFimVagao - (curto ? A(Trecho) : 0f);
            /// <summary>px do vagao_antigo para x do mundo; null se o px caiu no trecho que o vagão curto não tem.</summary>
            public float? X(float px)
            {
                if (curto && px >= Emenda0 && px < Emenda1) return null;
                return x + A(curto && px >= Emenda1 ? px - Trecho : px);
            }
        }

        static readonly bool[] Curtos = { false, true, false, true };
        // largura entre as plataformas de cada passagem (correndo o Kai pula ~5 u; andando ~2,4)
        static readonly float[] Vaos = { 2.8f, 7.6f, 4.4f };
        const float Buraco2 = 1.9f; // passagem 2: buraco, engate, buraco (correndo, o pulo do 1º buraco cai no meio do engate)

        // ritmo das portas de cada passagem: (aberta, aviso, batendo, fechada, abrindo, atraso da porta da frente, padrão)
        static readonly (float open, float warn, float close, float closed, float opening, float offsetB, float[] pattern)[] Ritmo =
        {
            (3.0f, 0.8f, 0.25f, 1.4f, 0.7f, 0.0f, null),                            // 1: as duas juntas, devagar
            (2.2f, 0.6f, 0.20f, 1.6f, 0.6f, 2.5f, null),                            // 2: alternadas (a de trás é a da retenção)
            (1.9f, 0.5f, 0.16f, 1.0f, 0.5f, 0.0f, new[] { 1.9f, 1.2f, 2.5f, 1.0f }), // 3: rápidas e irregulares
        };
        // passagem 2, porta de trás: bate rápido demais até a alavanca de retenção segurar
        const float RapidaAberta = 0.35f, RapidaAviso = 0.25f, RapidaFechada = 1.8f, Retencao = 6.5f;

        // janelas do vagao_antigo (x0, x1, y0, y1 em px, y a partir do topo): por elas passam as luzes do túnel e a estação
        static readonly Vector4[] Janelas =
        {
            new Vector4(150, 431, 185, 322), new Vector4(537, 599, 199, 330), new Vector4(628, 690, 199, 330),
            new Vector4(797, 1079, 185, 322), new Vector4(1186, 1248, 199, 330), new Vector4(1276, 1338, 199, 330),
            new Vector4(1447, 1727, 185, 322), new Vector4(1836, 1899, 199, 330), new Vector4(1927, 1989, 199, 330),
        };
        const float BagageiroPx = 294f; // bagageiro de cima do 1º banco (px do vagao_antigo)

        const int ObjOrder = -20, WindowStreakOrder = -93;

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

            ImportArt();   // peças dos Cenários 0 e 1 (vagão, pontas, túnel, personagens, tecla E...)
            ImportArt2();
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefab);
            if (!playerPrefab)
            {
                Debug.LogError("[Última Linha] Não achei " + PlayerPrefab + " (jogador do template).");
                return false;
            }
            if (!AssetDatabase.LoadAssetAtPath<Sprite>(VagaoCurto) || !Obj2Spr("engate"))
            {
                Debug.LogError("[Última Linha] Falta a arte do Cenário 2: rode Ferramentas/Cenario2/objetos_arte.py.");
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
            var fonts = SalasBuild.LoadFonts();

            // ---------- Som ----------
            var audioRoot = new GameObject("Som");
            var rumble = Source(audioRoot, "TremAndando", Clip2("trem_andando"), loop: true);
            var station = Source(audioRoot, "Estacao", Clip2("estacao_ambiente"), loop: true);
            var sfx = Source(audioRoot, "Efeitos", null);
            var doorSfx = Source(audioRoot, "Portas", null);
            var voice = Source(audioRoot, "VozDialogo", null);
            voice.volume = 0.5f;

            // ---------- Os quatro vagões e as três passagens ----------
            var world = new GameObject("Cenario").transform;
            var solids = new GameObject("Colisores").transform;
            solids.SetParent(world, false);
            var vag = new Vagao[4];
            var vaos = new VaoRefs[3];
            float x = 0f;
            for (int i = 0; i < vag.Length; i++)
            {
                vag[i] = new Vagao { x = x, curto = Curtos[i] };
                BuildCarro(world, solids, vag[i], i);
                if (i == vag.Length - 1) break;
                vaos[i] = BuildVao(vag[i].Fim, doorSfx, Vaos[i], painel: false);
                vaos[i].tras.transform.root.SetParent(world, true);
                x = vaos[i].bordaFrente + U(AntW) - XIniVagao;
            }

            // passagem 2: o engate no meio do vão (dá para ficar em pé nele)
            float engX0 = vaos[1].bordaTras + Buraco2, engX1 = vaos[1].bordaFrente - Buraco2;
            var engate = SpriteObj("Engate", world, Obj2Spr("engate"), new Vector3((engX0 + engX1) / 2f, Chao, 0f), -60);
            Box("Engate", solids, (engX0 + engX1) / 2f, Chao - 0.5f, engX1 - engX0, 1f, oneWay: false);
            var buracos = new List<Vector2>
            {
                new Vector2(vaos[0].bordaTras, vaos[0].bordaFrente),
                new Vector2(vaos[1].bordaTras, engX0), new Vector2(engX1, vaos[1].bordaFrente),
                new Vector2(vaos[2].bordaTras, vaos[2].bordaFrente),
            };

            // as portas das pontas batem com o tranco (CarDoor gira a porta de dobradiça)
            var doors = new List<CarDoor>();
            for (int i = 0; i < vaos.Length; i++)
            {
                var r = Ritmo[i];
                foreach (var (pp, frente) in new[] { (vaos[i].tras, false), (vaos[i].frente, true) })
                {
                    var d = RitmoDaPorta(pp, frente, doorSfx);
                    d.openTime = r.open;
                    d.warnTime = r.warn;
                    d.closeTime = r.close;
                    d.closedTime = r.closed;
                    d.openingTime = r.opening;
                    d.openPattern = r.pattern ?? new float[0];
                    d.startOffset = frente ? r.offsetB : 0f;
                    doors.Add(d);
                }
            }
            // passagem 1: paradas e fechadas até o cadeado abrir
            doors[0].cycling = doors[1].cycling = false;
            doors[0].startClosed = doors[1].startClosed = true;
            // passagem 2, porta de trás: rápida demais (a alavanca de retenção segura)
            doors[2].openTime = RapidaAberta;
            doors[2].warnTime = RapidaAviso;
            doors[2].closedTime = RapidaFechada;
            // passagem 3: sem energia, a luz de aviso não acende
            doors[4].lampOff = doors[5].lampOff = true;

            // a porta por onde o Kai entrou (ponta esquerda do 1º vagão) e a do fim (ponta direita do 4º)
            var entrada = BuildPortaPonta(world, vag[0].Ini, true, sfx);
            SpriteObj("Tunel", world, Obj("tunel"), new Vector3(vag[0].Ini - U(AntW) - 8f, -150f / PixelArtImporter.Ppu, 0f), -130);
            var saida = BuildPortaPonta(world, vag[3].Fim, false, sfx);
            float plataformaFim = vag[3].Fim + U(AntW);
            SpriteObj("Tunel", world, Obj("tunel"), new Vector3(plataformaFim + 8f, -150f / PixelArtImporter.Ppu, 0f), -130);
            // a estação: aparece quando o trem para (atrás da plataforma da ponta e pelas janelas do último vagão)
            var estacaoFim = SpriteObj("Estacao", world, Obj2Spr("estacao"), new Vector3(plataformaFim + 8f, Chao - A(140f), 0f), -128);
            estacaoFim.color = new Color(1f, 1f, 1f, 0f);
            Box("PisoEstacao", solids, vag[3].Fim + 10f, Chao - 0.5f, 20f, 1f, oneWay: false);
            Box("ParedeEsquerda", solids, vag[0].Ini - U(AntW) - 0.5f, 6f, 1f, 14f, oneWay: false);
            Box("ParedeDireita", solids, plataformaFim + 12f, 6f, 1f, 14f, oneWay: false);

            // ---------- Luzes do túnel nas janelas e nos vãos ----------
            var tunnelGo = new GameObject("LuzesDoTunel");
            tunnelGo.transform.SetParent(world, false);
            var tunnel = tunnelGo.AddComponent<TunnelLights>();
            var wins = new List<TunnelLights.Window>();
            var janelasFim = new List<Rect>();
            for (int i = 0; i < vag.Length; i++)
                foreach (var j in Janelas)
                {
                    var x0 = vag[i].X(j.x);
                    var x1 = vag[i].X(j.y);
                    if (x0 == null || x1 == null) continue;
                    var rect = Rect.MinMaxRect(x0.Value, AY(j.w), x1.Value, AY(j.z));
                    wins.Add(Janela(tunnelGo.transform, "Janela", rect));
                    if (i == vag.Length - 1) janelasFim.Add(rect);
                }
            foreach (var v in vaos)
                wins.Add(Janela(tunnelGo.transform, "Vao", Rect.MinMaxRect(v.bordaTras, Chao + 0.4f, v.bordaFrente, 2f * CameraSize - 0.6f)));
            tunnel.windows = wins.ToArray();
            tunnel.speed = 22f;
            // a estação pelas janelas do último vagão (só dentro delas)
            var estacaoJanela = SpriteObj("EstacaoNasJanelas", world, Obj2Spr("estacao"),
                new Vector3((vag[3].Ini + vag[3].Fim) / 2f, AY(250f) - A(370f), 0f), WindowStreakOrder - 1); // a placa da estação na altura das janelas
            estacaoJanela.transform.localScale = new Vector3((vag[3].Fim - vag[3].Ini) / estacaoJanela.sprite.bounds.size.x, 1f, 1f);
            estacaoJanela.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            estacaoJanela.color = new Color(1f, 1f, 1f, 0f);

            // ---------- Jogador (Kai) ----------
            float spawnY = Chao + ColliderHeight / 2f + 0.02f;
            var spawn = new GameObject("SpawnPoint");
            spawn.transform.position = new Vector3(vag[0].Ini + 2.8f, spawnY, 0);
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
                box.size = new Vector2(0.9f, ColliderHeight);
                box.offset = Vector2.zero;
                PrefabUtility.RecordPrefabInstancePropertyModifications(box);
            }
            var mainSr = playerGo.GetComponent<SpriteRenderer>();
            if (mainSr)
            {
                mainSr.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(mainSr);
            }
            var kaiRig = BuildPersonagem("kai", playerGo.transform, new Vector3(0f, -ColliderHeight / 2f, 0f), 10);
            kaiRig.mover = player;
            kaiRig.flipSource = mainSr;
            kaiRig.jumpSpeed = player.jumpTakeOffSpeed * 1.5f; // jumpModifier do GameController
            NoVagao(kaiRig);

            var prompt = SpriteObj("TeclaE", null, Spr("tecla_e"), Vector3.zero, 50);
            prompt.transform.localScale = Vector3.one * 0.75f;
            prompt.enabled = false;
            var interactor = playerGo.AddComponent<PlayerInteractor>();
            interactor.player = player;
            interactor.prompt = prompt;

            foreach (var d in doors) d.listener = playerGo.transform;

            var gcGo = new GameObject("GameController");
            var gc = gcGo.AddComponent<GameController>();
            gc.model.player = player;
            gc.model.spawnPoint = spawn.transform;
            gc.model.virtualCamera = null;
            gc.model.jumpModifier = 1.5f;
            gc.model.jumpDeceleration = 0.5f;

            // ---------- Souta e Ren ----------
            var souta = BuildNpc("Souta", "souta", vag[0].Ini + 8.4f, PixelArtHeightSouta, playerGo.transform);
            var ren = BuildNpc("Ren", "ren", vag[0].Ini + 10.8f, PixelArtHeightRen, playerGo.transform);

            // ---------- Câmera ----------
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
            follow.bounds = Rect.MinMaxRect(vag[0].Ini - U(AntW), 0f, plataformaFim + 10f, 2f * CameraSize);

            // ---------- Interface ----------
            var ui = SalasBuild.BuildUi(fonts, voice, withLock: true);
            var titulo = ui.lockUi.transform.Find("Painel/Titulo")?.GetComponent<TMP_Text>();
            if (titulo) titulo.text = "CADEADO DO PASSADIÇO";
            SalasBuild.SetSpeakers(ui.dialogue);
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();

            // ---------- Diretor ----------
            var dir = new GameObject("Diretor_Cenario2").AddComponent<Cenario2Director>();
            dir.player = player;
            dir.cameraFollow = follow;
            dir.doors = doors.ToArray();
            dir.portaEntrada = entrada;
            dir.exitDoor = saida;
            dir.tunnel = tunnel;
            dir.souta = souta;
            dir.ren = ren;
            // onde os dois esperam em cada vagão (perto da entrada dele)
            dir.soutaSpots = new[] { souta.transform.position, P(vag[1].Ini + 5.2f), P(vag[2].Ini + 5.0f), P(vag[3].Ini + 12.0f) };
            dir.renSpots = new[] { ren.transform.position, P(vag[1].Ini + 3.4f), P(vag[2].Ini + 3.2f), P(vag[3].Ini + 10.0f) };
            dir.checkpoints = vag.Select(v => v.Ini + 1.6f).ToArray();
            dir.carEnds = vag.Select(v => v.Fim).ToArray();
            dir.buracos = buracos.ToArray();
            dir.spawnY = spawnY;
            dir.killY = Chao - 2.2f;
            dir.endTriggerX = vag[3].Ini + 6f;
            dir.exitX = plataformaFim + 0.6f;
            dir.rumble = rumble;
            dir.stationAmbience = station;
            dir.sfx = sfx;
            dir.crushClip = Clip2("esmagado");
            dir.fallClip = Clip2("queda");
            dir.lurchClip = Clip2("tranco");
            dir.brakeClip = Clip2("freio");
            dir.flash = ui.redTint;
            dir.darkness = ui.darkness;
            dir.lockUI = ui.lockUi;
            dir.estacao = new[] { estacaoFim, estacaoJanela };
            foreach (var r in janelasFim) Mascara(world, "MascaraEstacao", r, WindowStreakOrder - 2, WindowStreakOrder + 1);

            // ---------- Vagão 1: o horário ----------
            var v1 = world.Find("Vagao1");
            Peca(v1, "AvisoOperadora", "aviso_operadora", new Vector3(vag[0].X(463f).Value, AY(235f), 0f));
            Peca(v1, "Relogio", "relogio", new Vector3(vag[0].X(716f).Value, AY(165f), 0f));
            Peca(v1, "Celular", "celular", new Vector3(vag[0].X(905f).Value, Chao, 0f));
            var aviso = Spot(v1, "aviso", vag[0].X(463f).Value, Chao, 3.9f, 1.3f, Clip("papel"));
            aviso.docTitle = "AVISO DA OPERADORA";
            aviso.docBody = "Com o trem em movimento, as portas entre os vagões <b>abrem e fecham sozinhas</b> com o balanço.\n\n" +
                            "A luz em cima da porta <b>pisca antes de ela bater</b>.\n\nNão permaneça no passadiço. Não salte entre os vagões.";
            aviso.docFooter = "Placa de metal parafusada por cima de um cartaz antigo.";
            aviso.first = Lines(("kai", "<i>\"Não salte entre os vagões.\" Ótimo.</i>"));
            aviso.again = Lines(("kai", "<i>A luz pisca antes de bater. Passo logo depois que ela abrir.</i>"));
            var relogio = Spot(v1, "relogio", vag[0].X(716f).Value, Chao, 4.3f, 1.2f);
            relogio.first = Lines(
                ("kai", "<i>O relógio parou. Quatro e dezessete.</i>"),
                ("kai", "<i>O vidro está rachado bem em cima dos ponteiros. Como se alguém tivesse batido nele.</i>"));
            relogio.again = Lines(("kai", "<i>Quatro e dezessete. Parado.</i>"));
            var celular = Spot(v1, "celular", vag[0].X(905f).Value, Chao, 1.2f, 1.2f, Clip("papel"));
            celular.docTitle = "CELULAR CAÍDO";
            celular.docBody = "<b>11 chamadas perdidas</b>: Chefe\n\nÚltima mensagem, <b>04:17</b>:\n" +
                              "<i>\"Kai, preciso falar com você. Amanhã, 9h, sala 704. Não falte.\"</i>\n\nRascunho não enviado:\n<i>\"eu não vou\"</i>";
            celular.docFooter = "A tela está rachada. A bateria não acaba.";
            celular.first = Lines(
                ("kai", "<i>Esse celular é... igual ao meu.</i>"),
                ("kai", "<i>Sala 704. A reunião com o chefe. Está no meu diário.</i>"));
            celular.again = Lines(("kai", "<i>Onze chamadas. A última às quatro e dezessete.</i>"));
            var cadeadoX = vag[0].Fim + U(AntPilar) - 0.05f;
            var cadeadoSpr = Peca(v1, "Cadeado", "cadeado_rodas", new Vector3(cadeadoX, Chao + 2.0f, 0f), -40);
            var cadeado = Spot(v1, "cadeado", vag[0].Fim - 0.4f, Chao, 2.8f, 1.4f);
            dir.cadeado = cadeado;
            dir.cadeadoVisual = cadeadoSpr.gameObject;

            // ---------- Vagão 2: segurar a porta ----------
            var v2 = world.Find("Vagao2");
            Peca(v2, "MapaDaLinha", "mapa_linha", new Vector3(vag[1].X(290f).Value, AY(150f), 0f));
            var mapa = Spot(v2, "mapa", vag[1].X(290f).Value, Chao, 4.4f, 1.6f, Clip("papel"));
            mapa.docTitle = "MAPA DA LINHA";
            mapa.docBody = "<b>EMPRESA</b>  —  <b>CLÍNICA DR. MORI</b>  —  <b>APARTAMENTO 12</b>  —  <b>ESCOLA</b>  —  <b>HOSPITAL</b>  —  ...\n\n" +
                           "A linha vermelha sai da empresa e passa por todo lugar onde eu já estive.\n\n" +
                           "A última bolinha não tem nome. Embaixo dela, à mão: <b>ÚLTIMA LINHA</b>.";
            mapa.docFooter = "Alguém riscou as estações de trás, uma por uma.";
            mapa.first = Lines(
                ("kai", "<i>Essas estações... não existem. São lugares meus.</i>"),
                ("kai", "<i>E esse trem não para em nenhum deles.</i>"));
            mapa.again = Lines(("kai", "<i>Todas riscadas, menos a última.</i>"));
            var alavanca = BuildRetencao(v2, new Vector3(vag[1].X(150f).Value, Chao, 0f), sfx);
            dir.retencao = alavanca;
            dir.retencaoSegundos = Retencao;

            // ---------- Vagão 3: luzes ----------
            var v3 = world.Find("Vagao3");
            var rack = SpriteObj("Bagageiro", v3, Spr("bagageiro"), new Vector3(vag[2].X(BagageiroPx).Value, OY(RackPy), 0f), -20);
            rack.transform.localScale = Vector3.one * EscalaVagao;
            var bolsaX = vag[2].X(BagageiroPx).Value;
            Peca(v3, "Bolsa", "bolsa_cracha", new Vector3(bolsaX, OY(RackPy) + 0.04f, 0f));
            var presenteX = vag[2].X(1000f).Value;
            Peca(v3, "Presente", "presente", new Vector3(presenteX, Chao, 0f));
            var remedioX = vag[2].X(1620f).Value;
            Peca(v3, "Remedio", "remedio", new Vector3(remedioX, OY(CushionPy), 0f));
            var fusiveis = new[]
            {
                Fusivel(v3, "bolsa", bolsaX, OY(BackrestPy), new Vector3(bolsaX + 0.7f, OY(RackPy) + 0.12f, 0f), 2.8f),
                Fusivel(v3, "presente", presenteX, Chao, new Vector3(presenteX + 0.55f, Chao + 0.06f, 0f), 1.4f),
                Fusivel(v3, "remedio", remedioX, Chao, new Vector3(remedioX - 0.45f, OY(CushionPy) + 0.06f, 0f), 2.6f),
            };
            fusiveis[0].spot.docTitle = "BOLSA COM CRACHÁ";
            fusiveis[0].spot.docBody = "<b>KAI</b>\nOperações  ·  Mesa 12\nFuncionário nº <b>0417</b>\n\n" +
                                       "A foto do crachá foi riscada de caneta até rasgar o plástico.";
            fusiveis[0].spot.docFooter = "Dentro da bolsa, junto dos papéis: um fusível.";
            fusiveis[0].spot.first = Lines(
                ("kai", "<i>Meu crachá. Minha bolsa. No bagageiro de um vagão em que eu nunca entrei.</i>"),
                ("kai", "<i>0417... O número é o meu. Não o do Ren.</i>"));
            fusiveis[1].spot.docTitle = "CARTÃO DE ANIVERSÁRIO";
            fusiveis[1].spot.docBody = "<i>\"Feliz aniversário, Kai.\nMesmo dia que o meu, lembra? 20 de outubro.\n" +
                                       "Vamos comemorar depois do trabalho. Eu pago.\n— S.\"</i>";
            fusiveis[1].spot.docFooter = "O presente nunca foi aberto. Encostado nele, um fusível.";
            fusiveis[1].spot.first = Lines(
                ("kai", "<i>Hoje é vinte de outubro.</i>"),
                ("kai", "<i>Ele lembrou. Eu nem lembrei do meu.</i>"));
            fusiveis[2].spot.docTitle = "FRASCO DE REMÉDIO";
            fusiveis[2].spot.docBody = "<b>Paciente:</b> Kai\n<b>Médico:</b> Dr. Mori\n\nTomar 1 comprimido às <b>8h</b> e às <b>20h</b>.\n" +
                                       "<b>Não interromper sem orientação médica.</b>";
            fusiveis[2].spot.docFooter = "Vazio. A tampa rolou para baixo do banco, junto de um fusível.";
            fusiveis[2].spot.first = Lines(
                ("kai", "<i>Vazio.</i>"),
                ("kai", "<i>Faz três semanas. Eu estou bem. Eu estou bem.</i>"));
            foreach (var f in fusiveis) f.spot.again = Lines(("kai", "<i>Já peguei o fusível daqui.</i>"));
            dir.fusiveis = fusiveis.Select(f => f.spot).ToArray();
            dir.fusiveisVisual = fusiveis.Select(f => f.visual).ToArray();
            var caixaX = vag[2].X(1790f).Value;
            var caixaVazia = Peca(v3, "CaixaFusiveis", "caixa_fusiveis_vazia", new Vector3(caixaX, AY(300f), 0f));
            var caixaCheia = Peca(v3, "CaixaFusiveis_Cheia", "caixa_fusiveis_cheia", new Vector3(caixaX, AY(300f), 0f));
            var led = SpriteObj("CaixaFusiveis_Luz", caixaCheia.transform, Spr("brilho"), new Vector3(caixaX + 0.02f, AY(300f) + 0.33f, 0f), ObjOrder + 1);
            led.transform.localScale = Vector3.one * 0.25f;
            led.color = new Color(0.4f, 1f, 0.5f, 0.6f);
            caixaCheia.gameObject.SetActive(false);
            var caixa = Spot(v3, "caixa", caixaX, Chao, 3.6f, 1.3f);
            dir.caixa = caixa;
            dir.caixaVazia = caixaVazia.gameObject;
            dir.caixaCheia = caixaCheia.gameObject;
            dir.ligaClip = Clip("gerador_liga");
            dir.pegaClip = Clip("chave_pega");
            dir.cadeadoClip = Clip("cadeado_abre");
            dir.luzesVagao3 = v3.GetComponentsInChildren<FluorescentLight>();
            dir.faixaVagao3 = new Vector2(vag[2].Ini - 1f, vag[2].Fim + 1f);
            // escrito na parede quando a luz volta (como as letras do Cenário 1)
            var escrita = new GameObject("Escrita").AddComponent<TextMeshPro>();
            escrita.transform.SetParent(v3, false);
            escrita.transform.position = new Vector3(vag[2].X(938f).Value, AY(250f), 0f);
            escrita.font = EnsureHandwritingFont();
            escrita.text = "ELE NÃO ESTAVA NO TREM";
            escrita.fontSize = 7f;
            escrita.alignment = TextAlignmentOptions.Center;
            escrita.rectTransform.sizeDelta = new Vector2(9f, 2f);
            escrita.color = new Color(0.55f, 0.04f, 0.03f, 0f);
            escrita.GetComponent<MeshRenderer>().sortingOrder = ObjOrder + 2;
            dir.escrita = escrita;

            foreach (var spot in world.GetComponentsInChildren<ObjectSpot>()) spot.sfx = sfx;
            Falas(dir, souta, ren);
        }

        // ---------- Peças ----------

        static Vector3 P(float x) => new Vector3(x, Chao, 0f);

        /// <summary>Um vagão: fundo, chão, teto, bancos (dá para subir, como no Cenário 1) e luminárias. O 3º é o sem energia.</summary>
        static void BuildCarro(Transform world, Transform solids, Vagao v, int i)
        {
            var root = new GameObject("Vagao" + (i + 1)).transform;
            root.SetParent(world, false);
            var fundo = AssetDatabase.LoadAssetAtPath<Sprite>(v.curto ? VagaoCurto : FundoPixel);
            SpriteObj("Fundo", root, fundo, new Vector3(v.x, 0f, 0f), -100);
            float ini = v.Ini, fim = v.Fim;
            Box("Chao", solids, (ini + fim) / 2f, Chao - 0.5f, fim - ini + 0.4f, 1f, oneWay: false);
            Box("Teto", solids, (ini + fim) / 2f, OY(CeilingPy) + 0.5f, fim - ini, 1f, oneWay: false);
            foreach (var b in Benches)
            {
                var a0 = v.X(b.x0 * FundoX);
                var a1 = v.X(b.x1 * FundoX);
                if (a0 == null || a1 == null) continue;
                var e0 = v.X(b.b0 * FundoX).Value;
                var e1 = v.X(b.b1 * FundoX).Value;
                Box("Banco_Assento", solids, (a0.Value + a1.Value) / 2f, OY(CushionPy) - 0.1f, a1.Value - a0.Value, 0.2f, oneWay: true);
                Box("Banco_Encosto", solids, (e0 + e1) / 2f, OY(BackrestPy) - 0.1f, e1 - e0, 0.2f, oneWay: true);
            }
            foreach (var t in Tubes)
            {
                var cx = v.X(t.cx * FundoX);
                if (cx == null) continue;
                var go = new GameObject("Luminaria");
                go.transform.SetParent(root, false);
                go.transform.position = new Vector3(cx.Value, OY(TubePy), 0f);
                var cover = SpriteObj("Apagada", go.transform, Spr("pixel"), go.transform.position, -90);
                cover.color = new Color(0.03f, 0.03f, 0.035f, 0f);
                cover.transform.localScale = new Vector3(t.w / 8f, 2f, 1f) * EscalaVagao;
                var glow = SpriteObj("Halo", go.transform, Spr("brilho_tubo"), go.transform.position, 20);
                glow.color = new Color(0.86f, 0.93f, 0.9f, 0.16f);
                glow.transform.localScale = new Vector3(Mathf.Max(0.6f, t.w * 1.7f / 256f), 1.8f, 1f) * EscalaVagao;
                var fl = go.AddComponent<FluorescentLight>();
                fl.glow = glow;
                fl.cover = cover;
                fl.broken = i == 2 || t.broken;
                fl.glowAlpha = fl.broken ? 0.12f : 0.16f;
                fl.crackle = null; // sem estalo/chiado quando a luz falha
            }
        }

        /// <summary>O ritmo de uma porta de ponta: CarDoor no ponto onde a porta fechada bate (o colisor dela).</summary>
        static CarDoor RitmoDaPorta(PortaPonta pp, bool frente, AudioSource sfx)
        {
            var ponta = pp.transform.parent;
            float s = frente ? -1f : 1f, bate = ponta.position.x + s * (U(AntPilar) - 0.3f);
            var go = new GameObject("Ritmo");
            go.transform.SetParent(ponta, true);
            go.transform.position = new Vector3(bate, Chao, 0f);
            var d = go.AddComponent<CarDoor>();
            d.porta = pp;
            d.doorwayHeight = U(AntPortaBase - AntPortaTopo);
            d.halfWidth = 0.4f;
            d.lamp = pp.luz;
            d.lampGlow = pp.halo;
            d.sfx = sfx;
            d.slamClip = Clip2("porta_bate");
            d.warnClip = Clip2("porta_aviso");
            d.openClip = Clip2("porta_abre");
            pp.enabled = false; // quem move a porta agora é o ritmo
            return d;
        }

        static TunnelLights.Window Janela(Transform parent, string name, Rect r)
        {
            Mascara(parent, name, r, WindowStreakOrder - 2, WindowStreakOrder + 1);
            var streak = SpriteObj("Luz", parent, Spr2("luz_tunel"), r.center, WindowStreakOrder);
            streak.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            streak.color = new Color(0.85f, 0.9f, 1f, 0f);
            return new TunnelLights.Window { streak = streak, x0 = r.xMin, x1 = r.xMax, y0 = r.yMin, y1 = r.yMax };
        }

        static void Mascara(Transform parent, string name, Rect r, int back, int front)
        {
            var mask = new GameObject(name);
            mask.transform.SetParent(parent, false);
            mask.transform.position = r.center;
            mask.transform.localScale = new Vector3(r.width / 0.08f, r.height / 0.08f, 1f);
            var sm = mask.AddComponent<SpriteMask>();
            sm.sprite = Spr("pixel");
            sm.isCustomRangeActive = true;
            sm.backSortingOrder = back;
            sm.frontSortingOrder = front;
        }

        static SpriteRenderer Peca(Transform parent, string name, string sprite, Vector3 pos, int order = ObjOrder) =>
            SpriteObj(name, parent, Obj2Spr(sprite), pos, order);

        /// <summary>Lugar de interação (E) com os pés em y.</summary>
        static ObjectSpot Spot(Transform parent, string id, float x, float y, float promptHeight, float reach, AudioClip clip = null)
        {
            var go = new GameObject("Lugar_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var spot = go.AddComponent<ObjectSpot>();
            spot.id = id;
            spot.reachX = reach;
            spot.promptOffset = new Vector2(0f, promptHeight);
            spot.useClip = clip;
            spot.docOnlyOnce = true;
            return spot;
        }

        /// <summary>Uma coisa para ler com um fusível do lado (brilha até ser pego).</summary>
        static (ObjectSpot spot, GameObject visual) Fusivel(Transform parent, string id, float x, float standY, Vector3 fusivel, float promptHeight)
        {
            var spot = Spot(parent, id, x, standY, promptHeight, 1.3f, Clip("papel"));
            var f = Peca(parent, "Fusivel_" + id, "fusivel", fusivel, ObjOrder + 1);
            var glow = SpriteObj("Brilho", f.transform, Spr("brilho"), fusivel + new Vector3(0f, 0.05f, 0f), ObjOrder + 2);
            glow.transform.localScale = Vector3.one * 0.35f;
            glow.color = new Color(1f, 0.85f, 0.55f, 0.35f);
            return (spot, f.gameObject);
        }

        /// <summary>Alavanca de retenção da porta (a do Cenário 1, em pé no chão), com a lâmpada em cima.</summary>
        static PowerLever BuildRetencao(Transform parent, Vector3 p, AudioSource sfx)
        {
            var root = new GameObject("AlavancaRetencao");
            root.transform.SetParent(parent, false);
            root.transform.position = p;
            var lever = root.AddComponent<PowerLever>();
            lever.reachX = 1.1f;
            lever.promptOffset = new Vector2(0f, 2.15f);
            lever.sfx = sfx;
            lever.pullClip = SalasBuild.Clip1("alavanca");
            SpriteObj("Base", root.transform, Obj("alavanca_base"), p, -10);
            lever.handle = SpriteObj("Cabo", root.transform, Obj("alavanca_cabo"), p + new Vector3(0f, A(27f), 0f), -11).transform;
            var lampPos = p + new Vector3(0f, A(62f), 0f);
            SpriteObj("Suporte", root.transform, Obj("lampada_suporte"), lampPos + new Vector3(0f, A(9f), 0f), -12);
            lever.lamp = SpriteObj("Lampada", root.transform, Obj("lampada"), lampPos, -10);
            var glow = SpriteObj("Lampada_Halo", root.transform, Spr("brilho"), lampPos, -9);
            glow.transform.localScale = Vector3.one * 0.55f;
            glow.color = new Color(1f, 1f, 1f, 0f);
            lever.lampGlow = glow;
            return lever;
        }

        static NpcTalk BuildNpc(string name, string id, float x, float height, Transform player)
        {
            var root = new GameObject(name);
            root.transform.position = P(x);
            var npc = root.AddComponent<NpcTalk>();
            npc.reachX = 1.7f;
            npc.promptOffset = new Vector2(0f, height + 0.45f);
            npc.lookAt = player;
            npc.rig = BuildPersonagem(id, root.transform, Vector3.zero, 6);
            NoVagao(npc.rig);
            npc.balao = BalaoDica(root.transform, npc.promptOffset.y + 0.1f);
            return npc;
        }

        // ---------- Falas ----------

        static void Falas(Cenario2Director dir, NpcTalk souta, NpcTalk ren)
        {
            dir.intro = Lines(
                ("kai", "<i>Mais um vagão. Mais escuro que o outro.</i>"),
                ("kai", "<i>...O trem voltou a andar?</i>"),
                ("souta", "Finalmente. Achei que a gente ia passar a noite parado naquele túnel."),
                ("ren", "Não é normal. Esse trem não devia estar andando. Não com a gente dentro."),
                ("souta", "A porta do fundo tem um cadeado de quatro números. Alguém prendeu com corrente."),
                ("ren", "Quatro números... Pode ser qualquer coisa."),
                ("souta", "Nada aqui é qualquer coisa. Olha em volta, Kai."));
            dir.portaEntradaFala = Lines(("kai", "<i>Trancou atrás de mim. Não tem volta.</i>"));
            dir.saidaTrancada = Lines(("kai", "<i>Trancada. Com o trem andando, essa porta não abre.</i>"));
            dir.cadeadoPrimeiro = Lines(("kai", "<i>Um cadeado de rodas. Quatro números.</i>"));
            dir.codigoErrado = Lines(("kai", "<i>Não é esse. O número tem que estar em algum lugar deste vagão.</i>"));
            dir.cadeadoAberto = Lines(
                ("kai", "<i>Quatro... um... sete. Abriu.</i>"),
                ("ren", "Quatro e dezessete... é o número do meu crachá."),
                ("souta", "Coincidência. Esse trem é cheio delas."),
                ("kai", "<i>O relógio. O celular. O crachá dele. O mesmo número.</i>"),
                ("souta", "Olha as portas. Com o tranco, elas abrem e batem sozinhas. A luz em cima pisca antes de bater."),
                ("ren", "E se ela fechar com alguém no meio...?"));
            dir.goAhead = Lines(
                ("souta", "Então não fique no meio."),
                ("souta", "Eu vou na frente com o Ren. A gente te espera no próximo vagão."),
                ("kai", "<i>Sempre na frente. Sempre.</i>"));
            dir.alone = Lines(
                ("kai", "<i>Eles já passaram.</i>"),
                ("kai", "<i>Por um segundo eu pensei... e se a porta fechasse antes dele passar?</i>"),
                ("kai", "<i>...Que pensamento é esse, Kai.</i>"));
            dir.firstCrush = Lines(
                ("kai", "<i>Preciso contar. Abre... pisca... bate.</i>"),
                ("kai", "<i>Passo logo depois que ela abrir, nunca quando a luz estiver piscando.</i>"));
            dir.firstFall = Lines(("kai", "<i>O vão entre os vagões... Tenho que pular da beirada, correndo.</i>"));
            dir.vagao2 = Lines(
                ("kai", "<i>Como eles chegaram antes de mim? A porta mal abria.</i>"),
                ("souta", "Demorou. Essa próxima porta é pior: bate rápido demais. Ninguém passa por ela assim."),
                ("ren", "Tem uma alavanca aqui atrás... está escrito \"retenção\"."),
                ("souta", "Ela segura a porta aberta. Mas a porta está lá no fundo. Puxa e corre."),
                ("souta", "E no vão tem um engate. Se a outra porta estiver fechada, espera em cima dele."));
            dir.retencaoPuxada = "A porta do fundo vai ficar aberta por alguns segundos. Corra!";
            dir.vagao3 = Lines(
                ("ren", "Tá escuro demais... a luz das portas nem acende."),
                ("souta", "Sem energia. A caixa de fusíveis fica no fundo do vagão. Alguém tirou os fusíveis."),
                ("souta", "Sem a luz de aviso, você não vai saber quando a porta vai bater."),
                ("kai", "<i>Ele fala como se eu fosse o único que precisasse passar.</i>"),
                ("souta", "Viu? Tudo tem um mecanismo."),
                ("ren", "Você sempre fala isso. Mas quem puxou a alavanca foi o Kai."));
            dir.caixaFaltam = Lines(("kai", "<i>Três encaixes vazios. Preciso achar os fusíveis.</i>"));
            dir.todosFusiveis = Lines(("kai", "<i>Três fusíveis. Agora é a caixa, no fundo do vagão.</i>"));
            dir.luzVoltou = Lines(
                ("kai", "<i>A luz voltou...</i>"),
                ("ren", "Kai... aquilo na parede. Estava escrito antes?"),
                ("souta", "Não olha, Ren. Vamos."),
                ("kai", "<i>\"Ele não estava no trem.\" Ele quem?</i>"),
                ("souta", "A gente vai na frente. O último vagão é logo ali."));
            dir.braking = Lines(("kai", "<i>Está freando...!</i>"));
            dir.ending = Lines(
                ("souta", "Demorou, Kai."),
                ("ren", "A gente passou logo antes de você... eu acho. Eu não lembro de ter passado."),
                ("souta", "Ele parou. Lá fora é uma estação."),
                ("kai", "<i>\"Terminal.\" A última parada.</i>"),
                ("souta", "A porta do fim abriu. Vamos sair daqui."));

            // conversa de cada vagão (o diretor troca quando eles mudam de vagão) e as dicas para quem travou
            dir.conversaSouta = new[]
            {
                new FalasVagao { falas = Lines(("souta", "Quatro números. Num vagão parado, eu olharia para o que também parou.")) },
                new FalasVagao { falas = Lines(("souta", "A alavanca segura a porta por pouco tempo. Não anda: corre.")) },
                new FalasVagao { falas = Lines(("souta", "Três encaixes. Procura em tudo: no chão, nos bancos, lá em cima.")) },
                new FalasVagao { falas = Lines(("souta", "Ele está parando. Segura em alguma coisa.")) },
            };
            dir.conversaRen = new[]
            {
                new FalasVagao { falas = Lines(("ren", "Você ouviu? Parecia alguém batendo... do outro lado daquela porta."), ("kai", "<i>Não tem ninguém atrás da gente. Tem?</i>")) },
                new FalasVagao { falas = Lines(("ren", "Se a porta da frente estiver fechada... fica no engate. Não pula de volta.")) },
                new FalasVagao { falas = Lines(("ren", "Eu vi uma bolsa lá em cima, no bagageiro. Dá pra subir pelo encosto.")) },
                new FalasVagao { falas = Lines(("ren", "Eu não quero descer. E se lá fora for pior?")) },
            };
            dir.dicaSouta = new[]
            {
                new FalasVagao { falas = Lines(("souta", "Kai. Olha o relógio da parede. E o celular no chão."), ("souta", "Os dois marcam a mesma hora. Quatro números.")) },
                new FalasVagao { falas = Lines(("souta", "Puxa a alavanca e corre sem parar até a porta."), ("souta", "Se a da frente estiver fechada, espera no engate, no meio do vão.")) },
                new FalasVagao { falas = Lines(("souta", "Um está no bagageiro: sobe no assento, depois no encosto."), ("souta", "Os outros estão perto do chão. Debaixo do banco do meio e no último banco.")) },
                new FalasVagao { falas = Lines(("souta", "Espera a porta abrir e corre. Pula da beirada da plataforma.")) },
            };
            dir.dicaRen = new[]
            {
                new FalasVagao { falas = Lines(("ren", "O relógio parou às quatro e dezessete... será que é isso?")) },
                new FalasVagao { falas = Lines(("ren", "A porta só segura enquanto a luz dela está verde. Depois bate de novo.")) },
                new FalasVagao { falas = Lines(("ren", "Tinha um presente embaixo do banco... e um frasco laranja no último assento.")) },
                new FalasVagao { falas = Lines(("ren", "Correndo dá. Andando você cai.")) },
            };
            souta.firstTalk = dir.conversaSouta[0].falas;
            ren.firstTalk = dir.conversaRen[0].falas;
        }

        // ---------- Assets ----------

        static void ImportArt2()
        {
            var pivos = new Dictionary<string, Vector2>
            {
                { "celular", new Vector2(0.5f, 0f) }, { "presente", new Vector2(0.5f, 0f) }, { "remedio", new Vector2(0.5f, 0f) },
                { "bolsa_cracha", new Vector2(0.5f, 0f) }, { "engate", new Vector2(0.5f, 1f) }, { "estacao", new Vector2(0.5f, 0f) },
            };
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { Art2 }).Select(AssetDatabase.GUIDToAssetPath))
            {
                string file = Path.GetFileNameWithoutExtension(path);
                if (path.StartsWith(Obj2))
                    ConfigureSprite(path, pivos.TryGetValue(file, out var p) ? p : new Vector2(0.5f, 0.5f), PixelArtImporter.Ppu, 4096, FilterMode.Point);
                else if (path == VagaoCurto)
                    ConfigureSprite(path, Vector2.zero, PixelArtImporter.Ppu, 8192, FilterMode.Point);
                else
                    ConfigureSprite(path, new Vector2(0.5f, 0.5f), 100f, file == "cenario2_fundo" ? 8192 : 2048);
            }
        }

        static Sprite Obj2Spr(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(Obj2 + name + ".png");
            if (!s) Debug.LogWarning("[Última Linha] Objeto do Cenário 2 não encontrado: " + name + " (rode Ferramentas/Cenario2/objetos_arte.py)");
            return s;
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

        static AudioClip Clip(string name) => SalasBuild.Clip(name);
    }
}
