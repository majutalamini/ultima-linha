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
        const int BuildVersion = 9; // aumente para forçar a cena a ser recriada automaticamente
        const string ScenePath = "Assets/Scenes/Cenario4.unity";
        // fundo: os painéis c4_painel_a/b do usuário (Ferramentas/Cenarios4a7), com os móveis pintados. As medidas abaixo são
        // em px "antigos" (100 por unidade, y para baixo numa tela de 941): X()/Y() dão unidades; foram tiradas da arte.
        const string Art4 = "Assets/Art/Cenario4/Pixel"; // gaveta aberta e caixa da luz de emergência (c4_objetos.py)
        static readonly string[] Moveis = { "gaveta_aberta", "luz_emergencia" };
        const float PePx = 599f; // linha dos pés nos painéis (o C4 vem a 85 %: Ferramentas/Cenarios4a7/fundos.py, ESCALA)

        // medidas (px da imagem, y para baixo)
        const float W = 3400f, Floor = 850f, LWall = 120f, RWall = 3270f;
        const float CrateX0 = 1469f, CrateX1 = 1758f, CrateTop = 660f;   // o caixote pintado (1,9 u acima dos pés)
        const float CatX0 = 1890f, CatX1 = 2395f, CatY = 598f;            // a passarela pintada
        const float GenX0 = 2420f, GenX1 = 2726f, GenTop = 578f;          // o gerador pintado
        const float DoorX0 = 2956f, DoorX1 = 3160f, DoorTop = 412f, DoorBase = 813f; // o vão vazio pintado em c4_painel_b
        static readonly (float x, float y)[] Levers = { (1359f, 850f), (2081f, 598f), (2573f, 578f) };
        // as luminárias pintadas nos painéis
        static readonly (float x, float w)[] Tubes = { (527f, 376f), (1452f, 376f), (2344f, 376f) };
        const float TubePy = 263f;
        static readonly (float x, float y)[] RedLamps = { (978f, 230f), (1870f, 230f), (2650f, 230f) };

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
            var pivos = new Dictionary<string, Vector2>();
            foreach (var m in Moveis) pivos[m] = Vector2.zero; // canto de baixo à esquerda: posição = pé do móvel
            ImportFolder(Art4, pivos, "", PixelArtImporter.Ppu, FilterMode.Point);
            EfeitosBuild.ImportaEfeitos();
            return Run(interactive, ScenePath, "Cenário 4", new[] { "Cenario3", "Cenario2", "Cenario1", "MainMenu" },
                () => Load(Art4, "relogio") != null, Populate);
        }

        // ------------------------------------------------------------------

        static void Populate(GameObject playerPrefab)
        {
            var fonts = LoadFonts();
            var core = BuildCore(playerPrefab, null, W, Floor, LWall, RWall, 300f, withLock: true, fonts);
            Moveis4(core.world);
            var player = core.player;
            var sfx = core.sfx;

            // ---------- Som ----------
            var rumble = Source(core.audioRoot, "AmbienteSala", Clip("ambiente_sala"), loop: true);
            var hum = Source(core.audioRoot, "Gerador", Clip("gerador_loop"), loop: true);
            var heart = Source(core.audioRoot, "Coracao", Clip("coracao"), loop: true);

            // ---------- Luzes ----------
            BuildTubes(core.world, Tubes, TubePy, i => i == 2);
            var reds = new List<SpriteRenderer>();
            for (int i = 0; i < RedLamps.Length; i++)
            {
                SpriteObj("LuzDeEmergencia_Caixa" + (i + 1), core.world, Load(Art4, "luz_emergencia"), new Vector3(X(RedLamps[i].x) - A(8f), Y(RedLamps[i].y) - A(12f), 0f), -40);
                var pos = new Vector3(X(RedLamps[i].x), Y(RedLamps[i].y + 6f), 0f);
                var glow = SpriteObj("LuzDeEmergencia" + (i + 1), core.world, Spr("brilho"), pos, 21);
                glow.color = new Color(1f, 0.1f, 0.06f, 0.35f);
                glow.transform.localScale = new Vector3(0.6f, 0.45f, 1f); // a luz no ambiente é a Light2D; o halo é só o brilho da lente
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

            var socket = BuildSpot(spots, "encaixe", 2828f, Floor, 3.2f, 1.3f, sfx, null);
            socket.first = Lines(
                ("kai", "<i>Quatro encaixes no painel. Eu só tenho três alavancas aqui.</i>"),
                ("kai", "<i>A que falta está em outro lugar. Se eu a achar, volto aqui.</i>"));
            socket.again = Lines(("kai", "<i>O quarto encaixe continua vazio. Preciso achar a alavanca.</i>"));

            var clock = BuildSpot(spots, "relogio", 1240f, Floor, 3.6f, 1.4f);
            clock.first = Lines(
                ("kai", "<i>O relógio parou. Os ponteiros estão em quatro e dezessete.</i>"),
                ("kai", "<i>Por que eu sinto que já vi essa hora antes?</i>"));
            clock.again = Lines(("kai", "<i>04:17. A hora em que tudo parou.</i>"));

            var note = BuildSpot(spots, "bilhete", 641f, Floor, 3.3f, 1.3f);
            note.docTitle = "REGISTRO DE TURNO";
            note.docBody =
                "Turno da noite. Supervisor: M. Takeda.\n\n" +
                "A gaveta da mesa fica trancada com <b>cadeado de combinação</b>.\n\n" +
                "A senha é a <b>hora em que o relógio da parede parou</b>, na noite em que faltou energia. Quatro números, sem os dois pontos.\n\n" +
                "Não anote a senha em lugar nenhum. Olhe o relógio.";
            note.docFooter = "Folha arrancada de um caderno, presa na parede com um alfinete vermelho.";
            note.first = Lines(("kai", "<i>O relógio da parede. A senha é a hora em que ele parou.</i>"));
            note.again = Lines(("kai", "<i>A hora em que o relógio parou, sem os dois pontos.</i>"));

            var drawer = BuildSpot(spots, "gaveta", 764f, Floor, 2.6f, 1.3f, sfx, null);
            drawer.first = Lines(("kai", "<i>Só papéis velhos. A chave já está comigo.</i>"));
            drawer.again = Lines(("kai", "<i>Já levei o que importava.</i>"));
            var open = SpriteObj("GavetaAberta", core.world, Load(Art4, "gaveta_aberta"), new Vector3(X(688f), Y(Floor) + 1.62f, 0f), -38);
            open.gameObject.SetActive(false);
            drawer.openVisual = open.gameObject;

            // ---------- Porta de serviço ----------
            var door = PortaNoVao(core.world, "PortaDeServico", "porta_c4", DoorX0, DoorX1, DoorTop, DoorBase, sfx);

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
            dir.darkAlpha = 0.15f; // era 0.5: somado à luz geral baixa, não dava para ver a caixa nem os puzzles
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
            Luzes(core, reds);
        }

        static float A(float px) => px / PixelArtImporter.Ppu;

        /// <summary>A sala: os dois painéis do usuário, com armários, mesa, relógio (repintado em 04:17), caixote, passarela,
        /// gerador, painel de energia e o batente vazio da porta.</summary>
        static void Moveis4(Transform world) => Paineis(world, Floor, PePx, "c4_painel_a", "c4_painel_b");

        /// <summary>Luz 2D de verdade e efeitos de ambiente (piloto: Ferramentas/Assets/GUIA_ARTE.md).</summary>
        static void Luzes(Core core, List<SpriteRenderer> reds)
        {
            var luzes = new GameObject("Luzes2D").transform;
            luzes.SetParent(core.world, false);
            EfeitosBuild.Global(luzes, 0.9f, new Color(0.78f, 0.8f, 0.84f)); // era 0.62: escuro demais para jogar
            // uma luz suave que anda com o Kai: o que está perto dele (a caixa, a passarela, as alavancas) sempre aparece
            Renderizador2D.Luz("Luz2D_Kai", core.player.transform, core.player.transform.position + Vector3.up * 1.2f,
                UnityEngine.Rendering.Universal.Light2D.LightType.Point, new Color(1f, 0.93f, 0.82f), 0.6f, 5f);
            // as luminárias: uma luz fria embaixo de cada halo, que falha junto com ele
            int i = 0;
            foreach (var t in Tubes)
            {
                var halo = core.world.Find($"Luminarias/Luminaria{++i}/Halo")?.GetComponent<SpriteRenderer>();
                EfeitosBuild.Fluorescente(luzes, new Vector3(X(t.x), Y(111f) - 0.4f, 0f), halo);
            }
            // vermelho de emergência: segue o halo que o diretor pulsa e desliga quando a energia volta
            for (int r = 0; r < reds.Count; r++)
                EfeitosBuild.DeEmergencia(luzes, reds[r].transform.position + Vector3.down * 0.3f, reds[r]);
            // a luz quente do mostrador do gerador e a lâmpada da mesa
            Renderizador2D.Luz("Luz2D_Mesa", luzes, new Vector3(X(935f), Y(604f), 0f), UnityEngine.Rendering.Universal.Light2D.LightType.Point,
                new Color(1f, 0.8f, 0.55f), 0.6f, 2.6f);

            var fx = new GameObject("Efeitos").transform;
            fx.SetParent(core.world, false);
            float chao = Y(Floor);
            EfeitosBuild.Emissor(fx, "Poeira_A", new Vector3(X(765f), chao + 3.0f, 0f), UltimaLinha.Visual.EmissorAmbiente.Tipo.Poeira, new Vector2(12f, 5f), new Color(0.78f, 0.76f, 0.7f), 0.45f);
            EfeitosBuild.Emissor(fx, "Poeira_B", new Vector3(X(2040f), chao + 3.0f, 0f), UltimaLinha.Visual.EmissorAmbiente.Tipo.Poeira, new Vector2(12f, 5f), new Color(0.78f, 0.76f, 0.7f), 0.45f);
            var neblina = EfeitosBuild.Emissor(fx, "Neblina", new Vector3(X(1445f), chao + 0.25f, 0f), UltimaLinha.Visual.EmissorAmbiente.Tipo.Neblina, new Vector2(30f, 0.4f), new Color(0.55f, 0.58f, 0.6f), 0.06f, 1f, 15);
            neblina.textura = Spr("brilho"); // mancha suave: em quadradinho a neblina virava blocos cinza no chão
            neblina.Aplica();
            // sem vapor no gerador: a fumaça em quadradinhos ficou feia
            EfeitosBuild.Goteira(fx, new Vector3(X(1000f), Y(220f), 0f), chao, new Vector2(1.8f, 4f));
            EfeitosBuild.Goteira(fx, new Vector3(X(2098f), Y(220f), 0f), chao, new Vector2(2.5f, 6f));
            EfeitosBuild.Faiscas(fx, new Vector3(X(2883f), chao + 2.8f, 0f)); // os fios soltos do quarto encaixe, no painel pintado
            // uma silhueta passa pela parede do relógio quando o Kai chega perto (uma vez)
            var sombra = EfeitosBuild.Sombra(fx, new Vector3(X(1359f), chao, 0f), core.player.transform, -5f);
            sombra.distanciaAtivacao = 2.5f;

            int lit = EfeitosBuild.LitNaCena();
            Debug.Log($"[Cenário 4] Luz 2D: {lit} sprites Lit, {Tubes.Length} luminárias, {reds.Count} luzes de emergência");
        }
    }
}
