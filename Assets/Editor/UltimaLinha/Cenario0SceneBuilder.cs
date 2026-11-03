using System.Collections.Generic;
using System.IO;
using System.Linq;
using Platformer.Mechanics;
using UltimaLinha.Cenario1;
using UltimaLinha.Salas;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem.UI;
using UnityEngine.SceneManagement;
using static UltimaLinha.EditorTools.Cenario1SceneBuilder;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Monta a Abertura (Cenario0.unity): o mesmo vagão do Cenário 1 em duas versões (limpo e cheio / decrépito e vazio),
    /// o painel que abre a porta da ponta e, depois dela, o vão entre os vagões que só se atravessa correndo, até a
    /// porta do vagão do Cenário 1 (montado igual lá: Cenario1SceneBuilder.BuildVao).
    /// Arte em Assets/Art/Cenario0 e nos passageiros, preparada por Ferramentas/Cenario0/abertura_arte.py.
    /// </summary>
    [InitializeOnLoad]
    public static class Cenario0SceneBuilder
    {
        const int BuildVersion = 15; // aumente para forçar a cena a ser recriada automaticamente
        const string ScenePath = "Assets/Scenes/Cenario0.unity";
        const string Art0 = "Assets/Art/Cenario0";
        const string Passageiros = "Assets/Art/Personagens/PixelArt/Passageiros";
        const string Prefab = "Assets/Prefabs/Player.prefab";
        const float PPU = 100f, ImgH = 941f;
        // os fundos da Abertura já vêm reduzidos (EscalaVagao) e na grade dos personagens (58 px por unidade)
        const float Grade = 58f;
        const float PeBancoPy = 775f; // chão logo na frente dos bancos: onde ficam os pés de quem está sentado
        const string PixelArtKai = "Assets/Art/Personagens/PixelArt/Kai";
        static float E => EscalaVagao;
        static readonly Color TomLimpo = new Color(0.92f, 0.92f, 0.92f), TomAntigo = new Color(0.62f, 0.6f, 0.64f);
        static float X(float px) => px / PPU;
        static float Y(float py) => (ImgH - py) / PPU;

        // passageiros do trem limpo: (arquivo, x px no vagão, olhando para a esquerda?). Sentados ficam nos bancos, em pé nas portas.
        static readonly (string arq, float x, bool esquerda)[] Gente =
        {
            ("sentado_1", 420f, false), ("sentado_2", 640f, true), ("sentado_6", 880f, false),
            ("sentado_3", 1960f, false), ("sentado_5", 2210f, true),
            ("sentado_4", 3130f, false), ("sentado_1", 3370f, true), ("sentado_2", 3590f, false),
            // pe_3 e pe_4 (braço levantado segurando alça) ficaram de fora: não há alça na altura da mão
            ("pe_1", 1100f, false), ("pe_10", 1250f, false), ("pe_2", 1400f, true),
            ("pe_7", 2430f, false), ("pe_9", 2590f, false), ("pe_8", 2740f, true),
            ("pe_5", 3810f, false), ("pe_6", 3990f, true),
        };
        const float KaiX = 1700f; // dorme no banco do meio

        static string VersionKey => "UltimaLinha.Cenario0.Version:" + Application.dataPath;
        static double firstTry = -1;

        static Cenario0SceneBuilder()
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
                return;
            }
            if (Build(interactive: false)) EditorPrefs.SetInt(VersionKey, BuildVersion);
        }

        [MenuItem("Última Linha/Recriar Abertura (Cenário 0)")]
        static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Recriar Abertura", "A cena Cenario0 já existe. Recriar vai substituir qualquer mudança feita nela. Continuar?",
                    "Recriar", "Cancelar"))
                return;
            if (Build(interactive: true)) EditorPrefs.SetInt(VersionKey, BuildVersion);
        }

        [MenuItem("Última Linha/Abrir Abertura (Cenário 0)")]
        static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { BuildFromMenu(); return; }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(ScenePath);
        }

        static bool Build(bool interactive)
        {
            var loaded = Enumerable.Range(0, SceneManager.sceneCount).Select(SceneManager.GetSceneAt).ToList();
            bool hasUntitled = loaded.Any(s => string.IsNullOrEmpty(s.path));
            bool targetOpen = loaded.Any(s => s.path == ScenePath);
            bool additive = !hasUntitled && !targetOpen;
            if (!additive && !interactive && loaded.Any(s => s.isDirty)) return false;

            ImportArt();
            foreach (var p in AssetDatabase.FindAssets("t:Texture2D", new[] { Art0 }).Select(AssetDatabase.GUIDToAssetPath))
                ConfigureSprite(p, Vector2.zero, Grade, 8192, FilterMode.Point);
            var playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
            if (!playerPrefab) return false;

            Scene previous = SceneManager.GetActiveScene();
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
            if (saved)
            {
                // logo depois do menu: é a primeira cena do jogo
                var scenes = EditorBuildSettings.scenes.Where(s => s.path != ScenePath).ToList();
                int menu = scenes.FindIndex(s => s.path.EndsWith("/MainMenu.unity"));
                scenes.Insert(menu >= 0 ? menu + 1 : 0, new EditorBuildSettingsScene(ScenePath, true));
                EditorBuildSettings.scenes = scenes.ToArray();
            }
            return saved;
        }

        static Sprite Arte(string nome)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Art0}/{nome}.png");
            if (!s) Debug.LogWarning("[Última Linha] Arte da abertura não encontrada: " + nome);
            return s;
        }

        static AudioClip Som(string pasta, string nome) => AssetDatabase.LoadAssetAtPath<AudioClip>($"Assets/Audio/{pasta}/{nome}.wav");

        static void Populate(GameObject playerPrefab)
        {
            var sans = LoadFont("Oswald-Regular");
            var serif = LoadFont("CormorantGaramond-Medium");
            var italic = LoadFont("CormorantGaramond-MediumItalic");

            // ---------- Som ----------
            var audioRoot = new GameObject("Som");
            var ambience = Source(audioRoot, "TremAndando", Som("Cenario2", "trem_andando"), loop: true);
            var sfx = Source(audioRoot, "Efeitos", null);
            var voice = Source(audioRoot, "VozDialogo", null);
            voice.volume = 0.5f;
            var introSfx = Source(audioRoot, "CutsceneEfeitos", null);
            var penLoop = Source(audioRoot, "CutsceneCaneta", Som("Cenario1", "caneta_escrevendo"), loop: true);

            // ---------- Os dois trens ----------
            var world = new GameObject("Cenario").transform;
            var limpo = SpriteObj("TremLimpo", world, Arte("vagao_limpo"), Vector3.zero, -99);
            limpo.transform.localScale = Vector3.one / E; // as imagens já vêm reduzidas; a raiz do cenário reduz o resto
            var antigo = new GameObject("TremAntigo");
            antigo.transform.SetParent(world, false);
            SpriteObj("Fundo", antigo.transform, Arte("vagao_antigo"), Vector3.zero, -100).transform.localScale = Vector3.one / E;
            foreach (var t in Tubes)
            {
                var go = new GameObject("Luminaria");
                go.transform.SetParent(antigo.transform, false);
                go.transform.position = new Vector3(X(t.cx), Y(TubePy), 0);
                var cover = SpriteObj("Apagada", go.transform, Spr("pixel"), go.transform.position, -90);
                cover.color = new Color(0.03f, 0.03f, 0.035f, 0f);
                cover.transform.localScale = new Vector3(t.w / 8f, 2f, 1f);
                var glow = SpriteObj("Halo", go.transform, Spr("brilho_tubo"), go.transform.position, 20);
                glow.color = new Color(0.86f, 0.93f, 0.9f, 0.16f);
                glow.transform.localScale = new Vector3(Mathf.Max(0.6f, t.w * 1.7f / 256f), 1.8f, 1f);
                var fl = go.AddComponent<FluorescentLight>();
                fl.glow = glow;
                fl.cover = cover;
                fl.broken = t.broken;
                fl.glowAlpha = t.broken ? 0.12f : 0.16f;
                fl.crackle = null;
            }

            // passageiros sem rosto: só existem no trem limpo. Ficam fora da raiz reduzida (personagem não encolhe).
            var gente = new GameObject("Passageiros");
            float pesSentado = (Y(PeBancoPy) - Y(FloorPy)) * E; // quanto os pés de quem senta ficam acima da base do quadro
            foreach (var g in Gente)
            {
                bool sentado = g.arq.StartsWith("sentado");
                var sr = SpriteObj("Passageiro_" + g.arq, gente.transform, AssetDatabase.LoadAssetAtPath<Sprite>($"{Passageiros}/{g.arq}.png"),
                    new Vector3(X(g.x), Y(FloorPy), 0f) * E, sentado ? 3 : 6);
                sr.flipX = g.esquerda;
                sr.color = TomLimpo;
                // as mesmas sombras dos personagens, nos pés: quem está sentado apoia os pés na frente do banco
                SombrasVagao(sr.transform, sr.sortingOrder - 1, sentado ? pesSentado : 0f);
                var reflexo = Reflexo(sr.transform, sr.sprite, sr.sortingOrder - 3, sentado ? pesSentado : 0f);
                reflexo.flipX = g.esquerda;
                reflexo.color = new Color(TomLimpo.r, TomLimpo.g, TomLimpo.b, AlfaReflexo);
                if (!sentado) continue;
                // e a sombra que ele faz no encosto e no assento
                var noBanco = SpriteObj("SombraNoBanco", sr.transform, AssetDatabase.LoadAssetAtPath<Sprite>($"{Passageiros}/sombra_{g.arq}.png"),
                    sr.transform.position, sr.sortingOrder - 1);
                noBanco.flipX = g.esquerda;
            }

            // ---------- Colisores do vagão ----------
            var solids = new GameObject("Colisores").transform;
            solids.SetParent(world, false);
            float floorY = Y(FloorPy);
            Box("Chao", solids, X(LevelWidth / 2), floorY - 0.5f, X(LevelWidth), 1f, oneWay: false);
            Box("ParedeEsquerda", solids, X(LeftWallPx) - 0.5f, 6f, 1f, 14f, oneWay: false);
            // a parede direita é a porta da ponta (BuildVao)
            Box("Teto", solids, X(LevelWidth / 2), Y(CeilingPy) + 0.5f, X(LevelWidth), 1f, oneWay: false);
            for (int i = 0; i < Benches.Length; i++)
            {
                var b = Benches[i];
                Platform($"Banco{i + 1}_Assento", solids, b.x0, b.x1, CushionPy);
                Platform($"Banco{i + 1}_Encosto", solids, b.b0, b.b1, BackrestPy);
            }

            world.localScale = Vector3.one * E; // tudo do vagão já foi montado em px do fundo

            // ---------- A ponta do vagão, o vão e a porta do vagão da frente (o do Cenário 1) ----------
            var vao = BuildVao(XFimVagao, sfx);
            vao.frente.enabled = false; // daqui só se chega a ela pulando; ela abre no Cenário 1
            SpriteObj("VagaoDaFrente", vao.tras.transform.root, AssetDatabase.LoadAssetAtPath<Sprite>(FundoPixel), new Vector3(Deslocamento, 0f, 0f), -100);
            float floorW = ChaoMundo;

            // painel de emergência na parede da ponta, ao lado da porta
            var painelGo = new GameObject("PainelEmergencia");
            painelGo.transform.position = vao.painel; // a arte vem do BuildVao, igual no Cenário 1
            var painel = painelGo.AddComponent<ObjectSpot>();
            painel.id = "painel";
            painel.reachX = 1.1f;
            painel.promptOffset = new Vector2(0f, 3.4f);
            painel.sfx = sfx;
            painel.useClip = Som("Cenario1", "porta_destrava");
            painel.first = Lines(
                ("kai", "<i>Destravamento de emergência.</i>"),
                ("kai", "<i>...Funcionou. A porta da ponta abriu.</i>"));
            painel.again = Lines(("kai", "<i>A porta já está aberta.</i>"));

            // ---------- Kai ----------
            var spawn = new GameObject("SpawnPoint");
            spawn.transform.position = new Vector3(X(KaiX) * E, floorY * E + ColliderHeight / 2f + 0.02f, 0);
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
            var kai = BuildPersonagem("kai", playerGo.transform, new Vector3(0f, -ColliderHeight / 2f, 0f), 10);
            kai.mover = player;
            kai.flipSource = mainSr;
            kai.jumpSpeed = player.jumpTakeOffSpeed * 1.5f;
            NoVagao(kai);
            // sombra no encosto e no assento enquanto ele dorme sentado (o diretor liga e desliga)
            var sombraSentado = SpriteObj("SombraNoBanco", kai.transform, AssetDatabase.LoadAssetAtPath<Sprite>(PixelArtKai + "/sombra_acorda_00.png"),
                kai.transform.position, 9);
            sombraSentado.gameObject.SetActive(false);
            var prompt = SpriteObj("TeclaE", null, Spr("tecla_e"), Vector3.zero, 50);
            prompt.transform.localScale = Vector3.one * 0.75f;
            prompt.enabled = false;
            var interactor = playerGo.AddComponent<PlayerInteractor>();
            interactor.player = player;
            interactor.prompt = prompt;

            var gc = new GameObject("GameController").AddComponent<GameController>();
            gc.model.player = player;
            gc.model.spawnPoint = spawn.transform;
            gc.model.virtualCamera = null;
            gc.model.jumpModifier = 1.5f;
            gc.model.jumpDeceleration = 0.5f;

            // ---------- Câmera ----------
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.transform.position = new Vector3(spawn.transform.position.x, CameraSize, -10f);
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow2D>();
            follow.target = playerGo.transform;

            // ---------- Interface ----------
            var ui = BuildUI(sans, serif, italic, voice);
            var canvas = ui.dialogue.GetComponentInParent<Canvas>().transform;
            var vermelhoRt = Stretch("Vermelho", canvas);
            Img(vermelhoRt, new Color(0.55f, 0.02f, 0.02f, 1f));
            var vermelho = vermelhoRt.gameObject.AddComponent<CanvasGroup>();
            vermelho.alpha = 0f;
            vermelho.blocksRaycasts = false;
            var fade = canvas.Find("Fade");
            if (fade) vermelhoRt.SetSiblingIndex(fade.GetSiblingIndex()); // por baixo do preto: a piscada forte some no escuro
            var es = new GameObject("EventSystem");
            es.AddComponent<EventSystem>();
            es.AddComponent<InputSystemUIInputModule>().AssignDefaultActions();

            ui.cutscene.drone = null;
            ui.cutscene.sfx = introSfx;
            ui.cutscene.trainClip = null;
            ui.cutscene.pageClip = null;
            ui.cutscene.trainSeconds = 1.6f;
            ui.cutscene.penLoop = penLoop;
            ui.cutscene.pages = PaginasAbertura();
            ui.dialogue.speakers = Falantes();

            // ---------- Diretor ----------
            var dir = new GameObject("Diretor_Cenario0").AddComponent<Cenario0Director>();
            dir.player = player;
            dir.kai = kai;
            dir.tomLimpo = TomLimpo;
            dir.tomAntigo = TomAntigo;
            dir.pesSentado = pesSentado;
            dir.sombraSentado = sombraSentado.gameObject;
            dir.cameraFollow = follow;
            dir.door = vao.tras;
            dir.painel = painel;
            dir.introCutscene = ui.cutscene;
            dir.tremLimpo = limpo;
            dir.tremAntigo = antigo;
            dir.passageiros = gente;
            dir.vermelho = vermelho;
            // o vagão, o vão e o comecinho do vagão da frente; no fim a câmera fica presa igual ao começo do Cenário 1
            dir.boundsVagao = Rect.MinMaxRect(0f, 0f, XIniVagao + Deslocamento + FolgaCameraEngate, LevelHeight / PPU * E);
            dir.inicioVao = vao.pilarTras + 0.3f; // passou do pilar da ponta: está na plataforma
            dir.vaoInicio = new Vector3(vao.pilarTras + 0.5f, floorW + ColliderHeight / 2f + 0.02f, 0f);
            dir.quedaY = vao.quedaY;
            dir.chegadaX = vao.bordaFrente + 0.35f;
            dir.descansoX = vao.bordaFrente + DescansoNoEngate;
            dir.bordaVao = vao.bordaTras;
            dir.ambience = ambience;
            dir.sfx = sfx;
            dir.piscaClip = Som("Cenario1", "apagao");
            dir.freioClip = Som("Cenario2", "freio");
            dir.acorda = Lines(
                ("kai", "<i>...</i>"),
                ("kai", "<i>Peguei no sono no trem de novo.</i>"),
                ("kai", "<i>Cadê todo mundo? Agora mesmo o vagão estava cheio.</i>"),
                ("kai", "<i>E o trem... parou. Parece que o vagão envelheceu vinte anos.</i>"));
            dir.portaTrancada = Lines(
                ("kai", "<i>Trancada. Tem um painel de emergência aqui do lado.</i>"));
            dir.antesDoPulo = Lines(
                ("kai", "<i>A passarela entre os vagões... não está aqui.</i>"),
                ("kai", "<i>Tem luz no próximo vagão. E vozes.</i>"),
                ("kai", "<i>Se eu correr, acho que alcanço.</i>"));
            dir.primeiraQueda = Lines(
                ("kai", "<i>Andando não dá. Preciso pegar impulso.</i>"));
        }
    }
}
