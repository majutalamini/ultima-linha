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
    /// Monta a cena Assets/Scenes/Cenario6.unity (o túnel: alavanca no painel, contagem regressiva e anteparas que fecham).
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Cenário 6.
    /// As medidas vêm de Ferramentas/Cenarios367/cenarios_finais.py (mesmos números).
    /// </summary>
    [InitializeOnLoad]
    public static class Cenario6SceneBuilder
    {
        const int BuildVersion = 3; // aumente para forçar a cena a ser recriada automaticamente
        const string ScenePath = "Assets/Scenes/Cenario6.unity";
        const string Art6 = "Assets/Art/Cenario6";

        const float W = 6200f, Floor = 850f, LWall = 120f, RWall = 6140f;
        const float SocketX = 530f, Slab = 90f, Post = 10f, GateTop = 478f;
        const float DoorX0 = 5880f, DoorX1 = 6040f, DoorTop = 400f;
        static readonly float[] Slabs = { 1900f, 3000f, 4100f, 5100f };
        static readonly (float x, float w)[] Tubes = { (700f, 240f), (1450f, 240f), (2450f, 240f), (3550f, 240f), (4600f, 240f), (5400f, 240f) };
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
                () => Load(Art6, "cenario6_fundo") != null, Populate);
        }

        // ------------------------------------------------------------------

        static AudioClip Clip2(string name) => AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Audio/Cenario2/" + name + ".wav");

        static void Populate(GameObject playerPrefab)
        {
            var fonts = LoadFonts();
            var core = BuildCore(playerPrefab, Load(Art6, "cenario6_fundo"), W, Floor, LWall, RWall, 330f, withLock: false, fonts);
            var player = core.player;
            core.kai.correndo = true; // túnel: contagem regressiva
            core.kai.strideLength = 3.4f;
            var sfx = core.sfx;

            var ambience = Source(core.audioRoot, "AmbienteTunel", Clip("ambiente_tunel"), loop: true);
            var alarm = Source(core.audioRoot, "Alarme", Clip("alarme_loop"), loop: true);

            BuildTubes(core.world, Tubes, 111f, i => i == 1 || i == 3);

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
            // a alavanca do chefe, encaixada no soquete do painel (desenhado no fundo em x=530, y=700)
            var leverRoot = new GameObject("AlavancaEncaixada");
            leverRoot.transform.SetParent(spots, false);
            var basePos = new Vector3(X(SocketX), Y(722f), 0f);
            leverRoot.transform.position = basePos;
            SpriteObj("Base", leverRoot.transform, Spr("alavanca_base"), basePos, -10);
            var handle = SpriteObj("Cabo", leverRoot.transform, Spr("alavanca_cabo"), basePos + new Vector3(0f, 0.4f, 0f), -11);
            handle.transform.localRotation = Quaternion.Euler(0f, 0f, -38f);
            var door = BuildExitDoor(core.world, "PortaDoMetro", Art6, "porta6", DoorX0, DoorX1, DoorTop, Floor, sfx, Clip1("porta_destrava"));

            var dir = new GameObject("Diretor_Cenario6").AddComponent<Cenario6Director>();
            dir.player = player;
            dir.cameraFollow = core.follow;
            dir.socket = socket;
            dir.leverInserted = leverRoot;
            dir.leverHandle = handle.transform;
            dir.leverClip = Clip("alavanca_pega");
            dir.door = door;
            dir.gates = gates.ToArray();
            var cps = new List<float> { X(330f) };
            foreach (var sx in Slabs) cps.Add(X(sx + Slab + 60f));
            dir.checkpoints = cps.ToArray();
            dir.spawnY = Y(Floor) + 1.62f;
            dir.countdownSeconds = 70f;
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
