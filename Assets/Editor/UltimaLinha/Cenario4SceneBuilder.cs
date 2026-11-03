using System.Collections.Generic;
using System.IO;
using UltimaLinha.Cenario1;
using UltimaLinha.Salas;
using UnityEditor;
using UnityEngine;
using static UltimaLinha.EditorTools.Cenario1SceneBuilder;
using static UltimaLinha.EditorTools.SalasBuild;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Monta a cena Assets/Scenes/Cenario4.unity: a sala de controle, onde o grupo tenta reativar o trem.
    /// Puzzle (do Notion): deixar todas as alavancas levantadas — mas falta uma, que está em outro lugar — e achar a chave da porta,
    /// que está numa gaveta com cadeado de combinação estilo Resident Evil.
    ///
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Cenário 4.
    /// As medidas vêm de Ferramentas/Cenarios45/cenario4.py (mesmos números).
    /// </summary>
    [InitializeOnLoad]
    public static class Cenario4SceneBuilder
    {
        const int BuildVersion = 3; // aumente para forçar a cena a ser recriada automaticamente
        const string ScenePath = "Assets/Scenes/Cenario4.unity";
        const string Art4 = "Assets/Art/Cenario4";

        // medidas (px da imagem, y para baixo)
        const float W = 3400f, Floor = 850f, LWall = 120f, RWall = 3340f;
        const float CrateX0 = 1740f, CrateX1 = 1900f, CrateTop = 720f;
        const float CatX0 = 1900f, CatX1 = 2600f, CatY = 590f;
        const float GenX0 = 2650f, GenX1 = 2900f, GenTop = 640f;
        const float DoorX0 = 3120f, DoorX1 = 3300f, DoorTop = 300f;
        static readonly (float x, float y)[] Levers = { (1650f, 850f), (2050f, 590f), (2780f, 640f) };
        static readonly (float x, float w)[] Tubes = { (500f, 230f), (1300f, 230f), (2100f, 230f), (2900f, 230f) };
        static readonly (float x, float y)[] RedLamps = { (380f, 150f), (1500f, 150f), (2300f, 150f), (3200f, 150f) };

        static string VersionKey => "UltimaLinha.Cenario4.Version:" + Application.dataPath;
        static double firstTry = -1;

        static Cenario4SceneBuilder()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        static void TryAutoBuild()
        {
            if (EditorPrefs.GetInt(VersionKey, 0) >= BuildVersion) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (WaitForEditor(ref firstTry, TryAutoBuild, "Abrir Cenário 4")) return;
            if (Build(interactive: false))
            {
                EditorPrefs.SetInt(VersionKey, BuildVersion);
                Debug.Log("[Última Linha] Cenário 4 criado em " + ScenePath + ". Abra pelo menu Última Linha > Abrir Cenário 4.");
            }
        }

        [MenuItem("Última Linha/Recriar Cenário 4")]
        static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Recriar Cenário 4",
                    "A cena Cenario4 já existe. Recriar vai substituir qualquer mudança feita nela. Continuar?", "Recriar", "Cancelar"))
                return;
            if (Build(interactive: true))
                EditorPrefs.SetInt(VersionKey, BuildVersion);
        }

        [MenuItem("Última Linha/Abrir Cenário 4")]
        static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { BuildFromMenu(); return; }
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
        }

        static bool Build(bool interactive)
        {
            ImportFolder(Art4, new Dictionary<string, Vector2> { { "cenario4_fundo", Vector2.zero } }, "cenario4_fundo");
            return Run(interactive, ScenePath, "Cenário 4", new[] { "Cenario3", "Cenario2", "Cenario1", "MainMenu" },
                () => Load(Art4, "cenario4_fundo") != null, Populate);
        }

        // ------------------------------------------------------------------

        static void Populate(GameObject playerPrefab)
        {
            var fonts = LoadFonts();
            var core = BuildCore(playerPrefab, Load(Art4, "cenario4_fundo"), W, Floor, LWall, RWall, 300f, withLock: true, fonts);
            var player = core.player;
            var sfx = core.sfx;

            // ---------- Som ----------
            var rumble = Source(core.audioRoot, "AmbienteSala", Clip("ambiente_sala"), loop: true);
            var hum = Source(core.audioRoot, "Gerador", Clip("gerador_loop"), loop: true);
            var heart = Source(core.audioRoot, "Coracao", Clip("coracao"), loop: true);

            // ---------- Luzes ----------
            BuildTubes(core.world, Tubes, 111f, i => i == 2);
            var reds = new List<SpriteRenderer>();
            for (int i = 0; i < RedLamps.Length; i++)
            {
                var pos = new Vector3(X(RedLamps[i].x), Y(RedLamps[i].y + 6f), 0f);
                var glow = SpriteObj("LuzDeEmergencia" + (i + 1), core.world, Spr("brilho"), pos, 21);
                glow.color = new Color(1f, 0.1f, 0.06f, 0.5f);
                glow.transform.localScale = new Vector3(1.9f, 1.5f, 1f);
                reds.Add(glow);
            }

            // ---------- Colisores ----------
            Box("Caixote", core.solids, X((CrateX0 + CrateX1) / 2f), (Y(Floor) + Y(CrateTop)) / 2f, X(CrateX1 - CrateX0), Y(CrateTop) - Y(Floor), oneWay: false);
            Box("Passarela", core.solids, X((CatX0 + CatX1) / 2f), Y(CatY) - 0.1f, X(CatX1 - CatX0), 0.2f, oneWay: true);
            Box("Gerador", core.solids, X((GenX0 + GenX1) / 2f), (Y(Floor) + Y(GenTop)) / 2f, X(GenX1 - GenX0), Y(GenTop) - Y(Floor), oneWay: false);

            // ---------- Alavancas ----------
            var leverRoot = new GameObject("Alavancas").transform;
            leverRoot.SetParent(core.world, false);
            var levers = new List<PowerLever>();
            for (int i = 0; i < Levers.Length; i++)
                levers.Add(BuildPowerLever(leverRoot, i, Levers[i].x, Levers[i].y, sfx, Clip1("alavanca")));

            // ---------- Lugares de interação ----------
            var spots = new GameObject("Lugares").transform;
            spots.SetParent(core.world, false);

            var socket = BuildSpot(spots, "encaixe", 3000f, Floor, 3.2f, 1.3f, sfx, null);
            socket.first = Lines(
                ("kai", "<i>Quatro encaixes no painel. Eu só tenho três alavancas aqui.</i>"),
                ("kai", "<i>A que falta está em outro lugar. Se eu a achar, volto aqui.</i>"));
            socket.again = Lines(("kai", "<i>O quarto encaixe continua vazio. Preciso achar a alavanca.</i>"));

            var clock = BuildSpot(spots, "relogio", 1250f, Floor, 3.6f, 1.4f);
            clock.first = Lines(
                ("kai", "<i>O relógio parou. Os ponteiros estão em quatro e dezessete.</i>"),
                ("kai", "<i>Por que eu sinto que já vi essa hora antes?</i>"));
            clock.again = Lines(("kai", "<i>04:17. A hora em que tudo parou.</i>"));

            var note = BuildSpot(spots, "bilhete", 660f, Floor, 3.3f, 1.3f);
            note.docTitle = "REGISTRO DE TURNO";
            note.docBody =
                "Turno da noite. Supervisor: M. Takeda.\n\n" +
                "A gaveta da mesa fica trancada com <b>cadeado de combinação</b>.\n\n" +
                "A senha é a <b>hora em que o relógio da parede parou</b>, na noite em que faltou energia. Quatro números, sem os dois pontos.\n\n" +
                "Não anote a senha em lugar nenhum. Olhe o relógio.";
            note.docFooter = "Folha arrancada de um caderno, presa na parede com um alfinete vermelho.";
            note.first = Lines(("kai", "<i>O relógio da parede. A senha é a hora em que ele parou.</i>"));
            note.again = Lines(("kai", "<i>A hora em que o relógio parou, sem os dois pontos.</i>"));

            var drawer = BuildSpot(spots, "gaveta", 862f, Floor, 2.6f, 1.3f, sfx, null);
            drawer.first = Lines(("kai", "<i>Só papéis velhos. A chave já está comigo.</i>"));
            drawer.again = Lines(("kai", "<i>Já levei o que importava.</i>"));
            var open = SpriteObj("GavetaAberta", core.world, Load(Art4, "gaveta_aberta"), new Vector3(X(864f), Y(776f), 0f), 2);
            open.gameObject.SetActive(false);
            drawer.openVisual = open.gameObject;

            // ---------- Porta de serviço ----------
            var door = BuildExitDoor(core.world, "PortaDeServico", Art4, "porta4", DoorX0, DoorX1, DoorTop, Floor, sfx, Clip1("porta_destrava"));

            // ---------- Souta e Ren ----------
            var souta = BuildNpc("Souta", "souta", 470f, Floor, 3.55f, player.transform);
            souta.firstTalk = Lines(
                ("souta", "A gente precisa de energia. Sem as alavancas levantadas, essa sala é só um depósito."),
                ("souta", "Tem uma na passarela e outra em cima do gerador. Sobe pelo caixote."));
            souta.repeatTalk = Lines(("souta", "A senha daquela gaveta deve estar em algum lugar à vista. Olha o relógio, o bilhete... esse cara era metódico."));
            var ren = BuildNpc("Ren", "ren", 560f, Floor, 3.3f, player.transform);
            ren.firstTalk = Lines(
                ("ren", "Esse zumbido... parece que a sala inteira está respirando."),
                ("kai", "<i>Eu também ouço. Não é só o gerador.</i>"));
            ren.repeatTalk = Lines(("ren", "Eu fico aqui. Se algo se mexer, eu grito."));

            // ---------- Diretor ----------
            var dir = new GameObject("Diretor_Cenario4").AddComponent<Cenario4Director>();
            dir.player = player;
            dir.cameraFollow = core.follow;
            dir.souta = souta;
            dir.ren = ren;
            dir.levers = levers.ToArray();
            dir.emptySocket = socket;
            dir.clock = clock;
            dir.note = note;
            dir.drawer = drawer;
            dir.door = door;
            dir.lockUI = core.ui.lockUi;
            dir.code = "0417";
            dir.darkness = core.ui.darkness;
            dir.darkAlpha = 0.5f;
            dir.emergencyLights = reds.ToArray();
            dir.ambience = rumble;
            dir.hum = hum;
            dir.sfx = sfx;
            dir.powerOnClip = Clip("gerador_liga");
            dir.keyClip = Clip("chave_pega");
            dir.drawerClip = Clip("gaveta_abre");
            dir.emptyClip = Clip("vazio_encaixe");

            dir.intro = Lines(
                ("kai", "<i>A sala de controle. É daqui que o trem sai.</i>"),
                ("souta", "Painel de energia, gerador, tudo apagado. Se a gente religar isso, o trem volta a andar de verdade."),
                ("ren", "E se ele não parar mais? Eu não gosto desse zumbido."),
                ("souta", "Quatro encaixes de alavanca. Todas precisam ficar levantadas."),
                ("kai", "<i>Eu só vejo três alavancas: uma no chão, uma na passarela e uma em cima do gerador.</i>"),
                ("souta", "Então falta uma. A gente acha depois. Primeiro a energia, depois a porta."));
            dir.leverHalf = Lines(
                ("kai", "<i>Uma levantada. O painel ainda está morto. Faltam as outras duas.</i>"));
            dir.powerOn = Lines(
                ("souta", "Luz! O gerador pegou."),
                ("kai", "<i>Três acesas. O quarto encaixe continua escuro.</i>"),
                ("ren", "A porta de serviço ligou, mas a trava é por chave. Procura uma, Kai."),
                ("souta", "O supervisor guardava tudo naquela mesa. Olha a gaveta."));
            dir.redTint = core.ui.redTint;
            dir.heartbeat = heart;
            dir.clockX = X(1250f);
            dir.soutaClock = Lines(
                ("souta", "Quatro e dezessete."),
                ("souta", "É nessa hora, Kai. Às quatro e dezessete todos nós vamos morrer."),
                ("souta", "Você, eu, o Ren. Ninguém sai da última plataforma. O relógio parou para avisar a gente."),
                ("ren", "Souta...? Do que você está falando?"),
                ("kai", "<i>Os olhos dele estão vazios. Ele nem pisca.</i>"),
                ("souta", "Quatro e dezessete. Quatro e dezessete. Quatro e..."));
            dir.soutaForgets = Lines(
                ("souta", "...Hã? O que foi? Por que vocês estão me olhando assim?"),
                ("ren", "Você acabou de dizer que a gente vai morrer, Souta."),
                ("souta", "Eu? Eu não disse nada disso. Esse lugar está mexendo com a cabeça de vocês."),
                ("souta", "Enfim... o relógio parou às quatro e dezessete. Deve servir para alguma coisa."),
                ("kai", "<i>04:17. A senha da gaveta é a hora em que o relógio parou.</i>"));
            dir.drawerNoClue = Lines(
                ("kai", "<i>Um cadeado de quatro números. Eu não faço ideia da senha.</i>"),
                ("kai", "<i>Tem um bilhete preso na parede. Talvez explique.</i>"));
            dir.drawerNeedClock = Lines(
                ("kai", "<i>O bilhete diz que a senha é a hora em que o relógio parou.</i>"),
                ("kai", "<i>Primeiro eu preciso olhar o relógio.</i>"));
            dir.drawerOpened = Lines(
                ("kai", "<i>Uma chave. A etiqueta diz \"SAÍDA TÉCNICA\".</i>"),
                ("souta", "Quatro e dezessete. Quem diria que era só olhar a parede."));
            dir.doorNoPower = Lines(
                ("kai", "<i>A porta não responde. Sem energia, nada abre aqui.</i>"));
            dir.doorNoKey = Lines(
                ("kai", "<i>Tem energia, mas a trava é mecânica. Preciso da chave.</i>"));
            dir.doorUnlocked = Lines(
                ("souta", "Abriu. Vamos, Kai."),
                ("ren", "O que será que tem do outro lado?"),
                ("kai", "<i>Esse corredor... eu já andei por ele. Eu sei aonde ele leva.</i>"));

            SetSpeakers(core.ui.dialogue);
        }
    }
}
