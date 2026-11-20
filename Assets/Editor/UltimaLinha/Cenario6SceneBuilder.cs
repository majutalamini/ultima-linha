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
        const int BuildVersion = 7; // aumente para forçar a cena a ser recriada automaticamente
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
            core.kai.correndo = true; // túnel: contagem regressiva
            var sfx = core.sfx;

            var ambience = Source(core.audioRoot, "AmbienteTunel", Clip("ambiente_tunel"), loop: true);
            var alarm = Source(core.audioRoot, "Alarme", Clip("alarme_loop"), loop: true);

            BuildTubes(core.world, Tubes, 167f, i => i == 1 || i == 3);

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
            var cps = new List<float> { X(330f) };
            foreach (var sx in Slabs) cps.Add(X(sx + Slab + 60f));
            dir.checkpoints = cps.ToArray();
            dir.spawnY = Y(Floor) + 1.62f;
            dir.countdownSeconds = 25f; // ~55 u correndo a 5,8 u/s levam uns 10 s: sobra folga, mas aperta
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
                ("kai", "<i>Ele está lá no começo da linha. Eu preciso chegar antes que ele parta. Corre!</i>"));
            dir.firstCrush = Lines(("kai", "<i>As anteparas estão fechando no ritmo do alarme. Eu preciso passar na hora que a luz fica verde.</i>"));
            dir.timeout = Lines(("kai", "<i>O trem esperou. Eu ainda tenho outra chance.</i>"));
            dir.doorBefore = Lines(("kai", "<i>O metrô ainda não está pronto. Preciso encaixar a alavanca no painel.</i>"));
            dir.boarding = Lines(
                ("kai", "<i>O metrô. O mesmo vagão do começo.</i>"),
                ("kai", "<i>Falta pouco.</i>"));

            SetSpeakers(core.ui.dialogue);
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
