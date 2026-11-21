using System.Collections.Generic;
using System.IO;
using UltimaLinha.Cenario1;
using UltimaLinha.Cenario2;
using UltimaLinha.Salas;
using UnityEditor;
using UnityEngine;
using static UltimaLinha.EditorTools.Cenario1SceneBuilder;
using static UltimaLinha.EditorTools.SalasBuild;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Monta a cena Assets/Scenes/Cenario6.unity (o túnel: alavanca no painel e uma corrida contra a contagem regressiva até o metrô).
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Cenário 6.
    /// As medidas vêm de Ferramentas/Cenarios367/cenarios_finais.py (mesmos números).
    /// </summary>
    [InitializeOnLoad]
    public static class Cenario6SceneBuilder
    {
        const int BuildVersion = 10; // aumente para forçar a cena a ser recriada automaticamente
        const string ScenePath = "Assets/Scenes/Cenario6.unity";
        const string Art6 = "Assets/Art/Cenario6";

        const float W = 6200f, Floor = 850f, LWall = 120f, RWall = 6140f;
        // fundo: os painéis c6_inicio/meio/fim do usuário (Ferramentas/Cenarios4a7): painel de tração pintado no começo e o
        // trem parado, de porta aberta, no fim. Medidas tiradas deles (px "antigos": 100 por unidade).
        const float PePx = 565f;
        const float SocketX = 585f, Slab = 90f, Post = 10f, GateTop = 478f;
        const float DoorX0 = 5790f, DoorX1 = 5960f, DoorTop = 345f, DoorBase = 750f; // a porta aberta do trem pintado
        // sem anteparas por enquanto: o túnel é só uma corrida contra o tempo (antes: 1900, 3000, 4100, 5100)
        static readonly float[] Slabs = { };
        // obstáculos da corrida (px antigos). Buracos de 410 px (4,1 u): andando o pulo cobre ~3,3 u, correndo ~5,9 u.
        static readonly (float x0, float x1)[] Pits = { (1900f, 2310f), (3400f, 3810f), (4900f, 5310f) };
        const float CratesX = 1300f, BarrierX = 2850f, StepX = 4300f, TallX = 4410f, RubbleX = 5550f;
        // as luminárias pintadas na parede (centro do vidro, px antigos), que piscam em vermelho com o alarme
        static readonly float[] WallLamps = { 159f, 1055f, 1817f, 2478f, 3236f, 3993f, 4700f, 5391f };
        const float WallLampY = 372.4f;
        const float RaceSeconds = 15f; // correndo leva ~10 s; andando (~19 s) não dá
        static readonly (float x, float w)[] Tubes = { (600f, 380f), (1580f, 380f), (2760f, 380f), (3730f, 380f), (4910f, 380f), (5870f, 380f) };
        // ritmo de cada antepara: abre, avisa, fecha, fica fechada, reabre, deslocamento
        static readonly float[][] Rhythm =
        {
            new[] { 3.2f, 0.9f, 0.2f, 1.3f, 0.7f, 0f },
            new[] { 2.8f, 0.8f, 0.2f, 1.4f, 0.7f, 1.2f },
            new[] { 2.5f, 0.8f, 0.2f, 1.5f, 0.7f, 0.5f },
            new[] { 2.2f, 0.75f, 0.2f, 1.5f, 0.7f, 1.6f },
        };

        static string VersionKey => "UltimaLinha.Cenario6.Version:" + Application.dataPath;
        static double firstTry = -1;

        static Cenario6SceneBuilder()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        static void TryAutoBuild()
        {
            if (EditorPrefs.GetInt(VersionKey, 0) >= BuildVersion) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (WaitForEditor(ref firstTry, TryAutoBuild, "Abrir Cenário 6")) return;
            if (Build(interactive: false))
            {
                EditorPrefs.SetInt(VersionKey, BuildVersion);
                Debug.Log("[Última Linha] Cenário 6 criado em " + ScenePath + ". Abra pelo menu Última Linha > Abrir Cenário 6.");
            }
        }

        [MenuItem("Última Linha/Recriar Cenário 6")]
        static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Recriar Cenário 6",
                    "A cena Cenario6 já existe. Recriar vai substituir qualquer mudança feita nela. Continuar?", "Recriar", "Cancelar"))
                return;
            if (Build(interactive: true))
                EditorPrefs.SetInt(VersionKey, BuildVersion);
        }

        [MenuItem("Última Linha/Abrir Cenário 6")]
        static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { BuildFromMenu(); return; }
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
        }

        static bool Build(bool interactive)
        {
            ImportFolder(Art6, new Dictionary<string, Vector2> { { "cenario6_fundo", Vector2.zero } }, "cenario6_fundo");
            return Run(interactive, ScenePath, "Cenário 6", new[] { "Cenario5", "Cenario4", "Cenario3", "Cenario2", "Cenario1", "MainMenu" },
                () => Load(Art6, "porta6_folha_esq") != null, Populate);
        }

        // ------------------------------------------------------------------

        static AudioClip Clip2(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cenario2/" + name + ".wav");

        static void Populate(GameObject playerPrefab)
        {
            var fonts = LoadFonts();
            var core = BuildCore(playerPrefab, null, W, Floor, LWall, RWall, 330f, withLock: false, fonts);
            Paineis(core.world, Floor, PePx, "c6_inicio", "c6_meio", "c6_fim");
            var player = core.player;
            // sem corrida automática: para chegar a tempo o jogador tem que segurar SHIFT
            var sfx = core.sfx;

            var ambience = Source(core.audioRoot, "AmbienteTunel", Clip("ambiente_tunel"), loop: true);
            var alarm = Source(core.audioRoot, "Alarme", Clip("alarme_loop"), loop: true);

            BuildTubes(core.world, Tubes, 167f, i => i == 1 || i == 3);
            var alarmLights = BuildWallAlarm(core.world);
            BuildObstacles(core);

            // anteparas: parede acima do vão (sólida) + a folha que desce
            float gateH = (Floor - GateTop) / PPU;
            var gates = new List<CarDoor>();
            for (int i = 0; i < Slabs.Length; i++)
            {
                float sx = Slabs[i];
                Box("AntaparaTopo" + (i + 1), core.solids, X(sx + Slab / 2f), (Y(GateTop) + ImgH / PPU) / 2f, X(Slab), ImgH / PPU - Y(GateTop), oneWay: false);
                var g = BuildGate(core.world, "Antepara" + (i + 1), sx, sfx, gateH);
                var r = Rhythm[i];
                g.openTime = r[0]; g.warnTime = r[1]; g.closeTime = r[2]; g.closedTime = r[3]; g.openingTime = r[4]; g.startOffset = r[5];
                g.listener = player.transform;
                gates.Add(g);
            }

            // soquete do painel
            var spots = new GameObject("Lugares").transform;
            spots.SetParent(core.world, false);
            var socket = BuildSpot(spots, "soquete", SocketX, Floor, 3.6f, 1.6f);
            // a alavanca: a pega em T recortada do painel pintado (fundos.py, PEGA_C6: x 333-357, linhas 371-380) aparece
            // no alto do trilho quando o Kai encaixa a alavanca e desce até o pé dele
            var leverRoot = new GameObject("AlavancaEncaixada");
            leverRoot.transform.SetParent(spots, false);
            float Linha(float r) => Y(Floor) + (PePx - r) / PixelArtImporter.Ppu;
            var basePos = new Vector3(333f / PixelArtImporter.Ppu, Linha(380f), 0f);
            leverRoot.transform.position = basePos;
            ConfigureSprite(Paineis4a7 + "/alavanca_c6.png", Vector2.zero, PixelArtImporter.Ppu, 64, FilterMode.Point);
            var handle = SpriteObj("Cabo", leverRoot.transform, Load(Paineis4a7, "alavanca_c6"), basePos, -10);
            var door = BuildExitDoor(core.world, "PortaDoMetro", Art6, "porta6", DoorX0, DoorX1, DoorTop, DoorBase, sfx, Clip1("porta_destrava"));
            // a porta do trem pintado já está aberta, com luz lá dentro: só fica o lugar do E (sem folhas nem vão escuro)
            foreach (var n in new[] { "Escuro", "Mascara", "FolhaEsquerda", "FolhaDireita" })
            {
                var c = door.transform.Find(n);
                if (c) c.gameObject.SetActive(false);
            }
            door.minFeetDy = -1.2f;
            door.promptOffset = new Vector2(0f, (DoorBase - DoorTop) / PPU + 0.3f);

            var dir = new GameObject("Diretor_Cenario6").AddComponent<Cenario6Director>();
            dir.player = player;
            dir.cameraFollow = core.follow;
            dir.socket = socket;
            dir.leverInserted = leverRoot;
            dir.leverHandle = handle.transform;
            dir.leverDesce = 38f / PixelArtImporter.Ppu; // até o pé do trilho (linha 418)
            dir.leverClip = Clip("alavanca_pega");
            dir.door = door;
            dir.gates = gates.ToArray();
            // pontos de controle: o começo e um pouco antes de cada buraco (com espaço para pegar embalo)
            dir.checkpoints = new[] { X(330f), X(1650f), X(3150f), X(4590f) };
            dir.spawnY = Y(Floor) + 1.62f;
            dir.fallY = Y(Floor) - 0.6f;
            dir.countdownSeconds = RaceSeconds;
            dir.alarmLights = alarmLights;
            var train = BuildTrain(core.world);
            dir.train = train.transform;
            dir.trainLight = train.transform.Find("Farol").GetComponent<SpriteRenderer>();
            dir.trainY = Y(Floor) - 0.3f;
            dir.trainSpeed = 22f;
            dir.trainSource = Source(core.audioRoot, "TremQueVem", null);
            dir.hornClip = Clip("trem_buzina");
            dir.approachClip = Clip("trem_aproxima");
            dir.hitClip = Clip2("tranco");
            dir.timerText = core.ui.timer;
            dir.redTint = core.ui.redTint;
            dir.ambience = ambience;
            dir.alarm = alarm;
            dir.sfx = sfx;
            dir.activateClip = Clip("metro_ativa");
            dir.tickClip = Clip("contagem_tic");
            dir.crushClip = Clip2("esmagado");
            dir.timeoutClip = Clip2("tranco");

            dir.intro = Lines(
                ("kai", "<i>Corredor de serviço. O painel de tração, com o encaixe vazio.</i>"),
                ("kai", "<i>A alavanca que estava no bolso dele ainda está comigo. É isso que faltava para o trem andar.</i>"));
            dir.inserted = Lines(
                ("kai", "<i>O metrô está ligando. Dá para sentir no chão.</i>"),
                ("kai", "<i>E o painel marcou um tempo. Pouco tempo.</i>"),
                ("kai", "<i>Ele está lá no fim do túnel. Se eu não chegar antes do tempo acabar, outro trem vem por esses trilhos.</i>"),
                ("kai", "<i>Corre!</i>"));
            dir.firstCrush = Lines(("kai", "<i>As anteparas estão fechando no ritmo do alarme. Eu preciso passar na hora que a luz fica verde.</i>"));
            dir.timeout = Lines(("kai", "<i>...De novo. Eu preciso correr mais rápido.</i>"));
            dir.firstFall = Lines(("kai", "<i>Andando eu não alcanço o outro lado. Preciso correr e pular.</i>"));
            dir.doorBefore = Lines(("kai", "<i>O metrô ainda não está pronto. Preciso encaixar a alavanca no painel.</i>"));
            dir.boarding = Lines(
                ("kai", "<i>O metrô. O mesmo vagão do começo.</i>"),
                ("kai", "<i>Falta pouco.</i>"));

            SetSpeakers(core.ui.dialogue);
        }

        static void PixelSprite(string nome, Vector2 pivo, int max = 1024) =>
            ConfigureSprite($"{Paineis4a7}/{nome}.png", pivo, PixelArtImporter.Ppu, max, FilterMode.Point);

        /// <summary>Caixotes, cavalete, entulho e buracos no piso. O chão vira pedaços, com os buracos entre eles.</summary>
        static void BuildObstacles(Core core)
        {
            foreach (var n in new[] { "c6_caixa", "c6_cavalete", "c6_entulho" }) PixelSprite(n, new Vector2(0.5f, 0f));
            PixelSprite("c6_buraco", new Vector2(0.5f, 1f));
            var root = new GameObject("Obstaculos").transform;
            root.SetParent(core.world, false);
            var tom = new Color(0.8f, 0.78f, 0.8f, 1f); // o mesmo tom que assenta os personagens no escuro
            float u = 1f / PixelArtImporter.Ppu;
            float chao = Y(Floor);

            // o chão: um pedaço entre cada buraco
            var velho = core.solids.Find("Chao");
            if (velho) UnityEngine.Object.DestroyImmediate(velho.gameObject);
            float ini = 0f;
            int k = 0;
            foreach (var p in Pits)
            {
                Box("Chao" + (++k), core.solids, X((ini + p.x0) / 2f), chao - 0.5f, X(p.x0 - ini), 1f, oneWay: false);
                ini = p.x1;
                // o buraco desenhado: começa na base da parede (37 px acima da linha dos pés) e corta a plataforma
                var b = SpriteObj("Buraco" + k, root, Load(Paineis4a7, "c6_buraco"), new Vector3(X((p.x0 + p.x1) / 2f), chao + 37f * u, 0f), -60);
                b.transform.localScale = new Vector3((p.x1 - p.x0) / 100f / (238f * u), 1f, 1f);
            }
            Box("Chao" + (++k), core.solids, X((ini + W) / 2f), chao - 0.5f, X(W - ini), 1f, oneWay: false);

            SpriteRenderer Peca(string nome, string arte, float xPx, float baseY, int ordem = 3)
            {
                var sr = SpriteObj(nome, root, Load(Paineis4a7, arte), new Vector3(X(xPx), baseY, 0f), ordem);
                sr.color = tom;
                return sr;
            }
            float cx = 64f * u; // um caixote: 64 px

            // 1) dois caixotes lado a lado
            Peca("Caixote1a", "c6_caixa", CratesX - 32f / 0.58f, chao);
            Peca("Caixote1b", "c6_caixa", CratesX + 32f / 0.58f, chao);
            Box("Caixotes1", core.solids, X(CratesX), chao + cx / 2f, 2f * cx, cx, oneWay: false);
            // 2) cavalete de obra
            Peca("Cavalete", "c6_cavalete", BarrierX, chao);
            Box("Cavalete", core.solids, X(BarrierX), chao + 72f * u / 2f, 112f * u * 0.85f, 72f * u, oneWay: false);
            // 3) degrau e pilha alta (pula no caixote baixo para alcançar o alto)
            Peca("Degrau", "c6_caixa", StepX, chao);
            Box("Degrau", core.solids, X(StepX), chao + cx / 2f, cx, cx, oneWay: false);
            Peca("PilhaBaixo", "c6_caixa", TallX, chao);
            Peca("PilhaCima", "c6_caixa", TallX, chao + cx, 4);
            Box("Pilha", core.solids, X(TallX), chao + cx, cx, 2f * cx, oneWay: false);
            // 4) monte de entulho, perto do metrô
            Peca("Entulho", "c6_entulho", RubbleX, chao);
            Box("Entulho", core.solids, X(RubbleX), chao + 0.42f, 1.9f, 0.84f, oneWay: false);
        }

        /// <summary>As luminárias pintadas na parede ganham um vidro vermelho e uma mancha de luz vermelha na parede,
        /// que piscam com o alarme (LuzAlarme). A tela em si não fica vermelha.</summary>
        static LuzAlarme BuildWallAlarm(Transform world)
        {
            PixelSprite("c6_alarme_lampada", new Vector2(0.5f, 0.5f), 64);
            var root = new GameObject("AlarmeNaParede");
            root.transform.SetParent(world, false);
            var vidros = new List<SpriteRenderer>();
            var brilhos = new List<SpriteRenderer>();
            for (int i = 0; i < WallLamps.Length; i++)
            {
                var pos = new Vector3(X(WallLamps[i]), Y(WallLampY), 0f);
                var mancha = SpriteObj("LuzVermelha" + (i + 1), root.transform, Spr("brilho"), pos, -90);
                mancha.color = new Color(0.95f, 0.14f, 0.1f, 0f);
                mancha.transform.localScale = Vector3.one * 1.9f;
                brilhos.Add(mancha);
                var nucleo = SpriteObj("LuzVermelhaForte" + (i + 1), root.transform, Spr("brilho"), pos, -89);
                nucleo.color = new Color(1f, 0.25f, 0.16f, 0f);
                nucleo.transform.localScale = Vector3.one * 0.55f;
                brilhos.Add(nucleo);
                var vidro = SpriteObj("VidroVermelho" + (i + 1), root.transform, Load(Paineis4a7, "c6_alarme_lampada"), pos, -88);
                vidro.color = new Color(1f, 1f, 1f, 0f);
                vidros.Add(vidro);
            }
            var alarme = root.AddComponent<LuzAlarme>();
            alarme.vidros = vidros.ToArray();
            alarme.brilhos = brilhos.ToArray();
            alarme.periodo = 1.1f;
            alarme.brilhoMax = 0.32f; // só tinge a parede em volta da luminária
            return alarme;
        }

        /// <summary>O trem que atropela o Kai quando o tempo acaba: o vagão pintado no fim do túnel, recortado e virado
        /// (Ferramentas/Cenario6/obstaculos.py), com o farol aceso. Pivô na frente, embaixo; fica desligado.</summary>
        static GameObject BuildTrain(Transform world)
        {
            PixelSprite("c6_trem", new Vector2(1f, 0f), 1024);
            var root = new GameObject("TremQueAtropela");
            root.transform.SetParent(world, false);
            var body = SpriteObj("Vagao", root.transform, Load(Paineis4a7, "c6_trem"), Vector3.zero, 40);
            body.transform.localPosition = Vector3.zero;
            body.color = new Color(0.85f, 0.83f, 0.85f, 1f);
            var farol = SpriteObj("Farol", root.transform, Spr("brilho"), Vector3.zero, 41);
            farol.transform.localPosition = new Vector3(0.3f, 1.7f, 0f);
            farol.transform.localScale = Vector3.one * 4.5f;
            farol.color = new Color(1f, 0.95f, 0.8f, 0f);
            root.SetActive(false);
            return root;
        }

        /// <summary>Antepara que desce (a mesma peça das portas do Cenário 2), numa passagem de GateTop até o chão.</summary>
        static CarDoor BuildGate(Transform parent, string name, float sx, AudioSource sfx, float h)
        {
            float cx = X(sx + Slab / 2f);
            float w = X(Slab - 2f * Post);
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(cx, Y(Floor), 0f);
            var door = root.AddComponent<CarDoor>();
            door.doorwayHeight = h;
            door.halfWidth = w / 2f;

            var maskGo = new GameObject("Passagem");
            maskGo.transform.SetParent(root.transform, false);
            maskGo.transform.position = new Vector3(cx, Y(Floor) + h / 2f, 0f);
            maskGo.transform.localScale = new Vector3(w / 0.08f, h / 0.08f, 1f);
            var mask = maskGo.AddComponent<SpriteMask>();
            mask.sprite = Spr("pixel");
            mask.isCustomRangeActive = true;
            mask.backSortingOrder = 29;
            mask.frontSortingOrder = 30;

            var panelRoot = new GameObject("Folha").transform;
            panelRoot.SetParent(root.transform, false);
            panelRoot.localPosition = new Vector3(0f, h, 0f);
            var panel = SpriteObj("Folha_Desenho", panelRoot, Load("Assets/Art/Cenario2", "porta_vagao"), root.transform.position, 30);
            panel.transform.localPosition = Vector3.zero;
            panel.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            door.panel = panelRoot;

            var block = new GameObject("Bloqueio");
            block.transform.SetParent(root.transform, false);
            var col = block.AddComponent<BoxCollider2D>();
            col.size = new Vector2(w, 0.05f);
            col.enabled = false;
            door.blocker = col;

            var lampPos = new Vector3(cx, Y(GateTop - 34f), 0f);
            var lamp = SpriteObj("Aviso", root.transform, Spr("ponto"), lampPos, 32);
            lamp.transform.localScale = new Vector3(2.4f, 1.2f, 1f);
            var glow = SpriteObj("Aviso_Halo", root.transform, Spr("brilho"), lampPos, 33);
            glow.transform.localScale = Vector3.one * 0.45f;
            door.lamp = lamp;
            door.lampGlow = glow;
            door.sfx = sfx;
            door.slamClip = Clip2("porta_bate");
            door.warnClip = Clip2("porta_aviso");
            door.openClip = Clip2("porta_abre");
            return door;
        }
    }
}
