using System.Collections.Generic;
using System.IO;
using UltimaLinha.Cenario1;
using UltimaLinha.Cenario2;
using UltimaLinha.Salas;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UltimaLinha.EditorTools.Cenario1SceneBuilder;
using static UltimaLinha.EditorTools.SalasBuild;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Monta a cena Assets/Scenes/Cenario3.unity (a estação: peças do painel de desvios escondidas atrás de papéis,
    /// um mezanino e a porta da administração, que abre quando a linha do painel fica completa).
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Cenário 3.
    /// As medidas vêm de Ferramentas/Cenarios367/cenarios_finais.py (mesmos números).
    /// </summary>
    [InitializeOnLoad]
    public static class Cenario3SceneBuilder
    {
        const int BuildVersion = 4; // aumente para forçar a cena a ser recriada automaticamente
        const string ScenePath = "Assets/Scenes/Cenario3.unity";
        const string Art3 = "Assets/Art/Cenario3";

        const float W = 4800f, Floor = 850f, LWall = 120f, RWall = 4700f;
        const float DoorX0 = 4450f, DoorX1 = 4630f, DoorTop = 300f;
        const float DeckX0 = 3000f, DeckX1 = 3700f, DeckY = 600f;
        static readonly (float x0, float x1, float top)[] Steps = { (2780f, 2900f, 740f), (2890f, 3010f, 670f) };
        static readonly (float x, float w)[] Tubes = { (500f, 240f), (1300f, 240f), (2100f, 240f), (2900f, 240f), (3700f, 240f), (4400f, 240f) };

        static string VersionKey => "UltimaLinha.Cenario3.Version:" + Application.dataPath;
        static double firstTry = -1;

        static Cenario3SceneBuilder()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        static void TryAutoBuild()
        {
            if (EditorPrefs.GetInt(VersionKey, 0) >= BuildVersion) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (WaitForEditor(ref firstTry, TryAutoBuild, "Abrir Cenário 3")) return;
            if (Build(interactive: false))
            {
                EditorPrefs.SetInt(VersionKey, BuildVersion);
                Debug.Log("[Última Linha] Cenário 3 criado em " + ScenePath + ". Abra pelo menu Última Linha > Abrir Cenário 3.");
            }
        }

        [MenuItem("Última Linha/Recriar Cenário 3")]
        static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Recriar Cenário 3",
                    "A cena Cenario3 já existe. Recriar vai substituir qualquer mudança feita nela. Continuar?", "Recriar", "Cancelar"))
                return;
            if (Build(interactive: true))
                EditorPrefs.SetInt(VersionKey, BuildVersion);
        }

        [MenuItem("Última Linha/Abrir Cenário 3")]
        static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { BuildFromMenu(); return; }
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
        }

        static bool Build(bool interactive)
        {
            ImportFolder(Art3, new Dictionary<string, Vector2> { { "cenario3_fundo", Vector2.zero } }, "cenario3_fundo");
            return Run(interactive, ScenePath, "Cenário 3", new[] { "Cenario2", "Cenario1", "MainMenu" },
                () => Load(Art3, "cenario3_fundo") != null, Populate);
        }

        // ------------------------------------------------------------------

        static void Populate(GameObject playerPrefab)
        {
            var fonts = LoadFonts();
            var core = BuildCore(playerPrefab, Load(Art3, "cenario3_fundo"), W, Floor, LWall, RWall, 330f, withLock: false, fonts);
            var player = core.player;
            var sfx = core.sfx;

            var ambience = Source(core.audioRoot, "AmbienteEstacao", Clip("ambiente_estacao"), loop: true);

            BuildTubes(core.world, Tubes, 111f, i => i == 2 || i == 4);

            foreach (var s in Steps)
                Box("Degrau", core.solids, X((s.x0 + s.x1) / 2f), (Y(Floor) + Y(s.top)) / 2f, X(s.x1 - s.x0), Y(s.top) - Y(Floor), oneWay: false);
            Box("Mezanino", core.solids, X((DeckX0 + DeckX1) / 2f), Y(DeckY) - 0.1f, X(DeckX1 - DeckX0), 0.2f, oneWay: true);

            var door = BuildExitDoor(core.world, "PortaAdministracao", Art3, "porta3", DoorX0, DoorX1, DoorTop, Floor, sfx, Clip1("porta_destrava"));

            var spots = new GameObject("Lugares").transform;
            spots.SetParent(core.world, false);
            var papel = Clip("papel");

            var d1 = BuildSpot(spots, "horarios", 1010f, Floor, 3.2f, 1.6f, sfx, papel);
            d1.docTitle = "QUADRO DE HORÁRIOS";
            d1.docBody =
                "<b>ESTAÇÃO — LINHA ÚNICA</b>\n\nPartidas, todas as noites:\n22:10   ·   23:00   ·   23:50\n\n" +
                "Depois do último trem, a estação é fechada e a administração tranca a sala de controle.\n\n" +
                "<i>Escrito a lápis, na margem:</i>\nTirei as peças do painel de desvios. Sem a linha completa, a porta da administração não abre.";
            d1.docFooter = "Atrás da folha, presa com fita: uma peça de trilho.";
            d1.first = Lines(("kai", "<i>Uma peça de trilho, do tamanho da mão. Arrancada de algum painel.</i>"),
                ("kai", "<i>\"Sem a linha completa, a porta não abre.\" Então tem mais peças por aí.</i>"));
            d1.again = Lines(("kai", "<i>O quadro de horários. Já peguei a peça.</i>"));

            var d2 = BuildSpot(spots, "bilhete", 1900f, Floor, 1.3f, 1.4f, sfx, papel);
            d2.docTitle = "BILHETE DE EMBARQUE";
            d2.docBody =
                "<b>ÚLTIMA LINHA</b>\nDestino: plataforma final\nValidade: uma viagem, sem volta.\n\n" +
                "<i>No verso, com a mesma letra:</i>\nquem quiser sair vai ter que ligar a linha sozinho.";
            d2.docFooter = "Embaixo do bilhete, no chão: outra peça de trilho.";
            d2.first = Lines(("kai", "<i>Um bilhete caído e, embaixo dele, outra peça do painel.</i>"));
            d2.again = Lines(("kai", "<i>O bilhete. Sem volta.</i>"));

            var d3 = BuildSpot(spots, "guiche", 3350f, DeckY, 2.9f, 1.8f, sfx, papel);
            d3.docTitle = "ESCALA DOS FUNCIONÁRIOS";
            d3.docBody =
                "Turno da noite:  ninguém escalado.\nTurno da madrugada:  ninguém escalado.\n\n" +
                "A estação está funcionando sozinha há muito tempo. Quem estiver lendo isto: não espere por ninguém.";
            d3.docFooter = "Na gaveta do guichê, junto da escala: uma peça de trilho.";
            d3.first = Lines(("kai", "<i>No guichê dos funcionários, lá no alto, mais uma peça.</i>"), ("kai", "<i>\"Ninguém escalado.\" Então quem mantém essa estação ligada?</i>"));
            d3.again = Lines(("kai", "<i>Ninguém escalado. Ninguém.</i>"));

            var d4 = BuildSpot(spots, "recibo", 4100f, Floor, 3.6f, 1.6f, sfx, papel);
            d4.docTitle = "RECIBO DA MÁQUINA";
            d4.docBody =
                "<b>MÁQUINA AUTOMÁTICA</b>\n1 água · 1 pacote de biscoito\nTotal: pago.\n\n" +
                "<i>Impresso embaixo, em letras miúdas:</i>\ntroco: uma peça de trilho.";
            d4.docFooter = "A peça caiu na bandeja da máquina, junto com o recibo.";
            d4.first = Lines(("kai", "<i>A máquina cuspiu o recibo... e uma peça de trilho.</i>"));
            d4.again = Lines(("kai", "<i>A máquina não tem mais nada.</i>"));

            var souta = BuildNpc("Souta", "souta", 450f, Floor, 3.55f, player.transform);
            souta.firstTalk = Lines(
                ("souta", "Do lado da porta tem um painel com um desenho dos trilhos. Está faltando pedaço."),
                ("souta", "Se a porta depende dele, as peças devem estar pela estação. Procura em tudo."));
            souta.repeatTalk = Lines(("souta", "Quatro buracos no painel. Ainda falta alguma peça?"));
            var ren = BuildNpc("Ren", "ren", 540f, Floor, 3.3f, player.transform);
            ren.firstTalk = Lines(
                ("ren", "Eu vi uma pilha de caixotes lá no fundo. Dá para subir até o mezanino."),
                ("ren", "Os funcionários ficavam lá em cima."));
            ren.repeatTalk = Lines(("ren", "Se faltar uma peça, olha lá em cima, subindo pelos caixotes."));

            var dir = new GameObject("Diretor_Cenario3").AddComponent<Cenario3Director>();
            dir.player = player;
            dir.cameraFollow = core.follow;
            dir.souta = souta;
            dir.ren = ren;
            dir.documents = new[] { d1, d2, d3, d4 };
            // brilho fraco em cima de cada folha (some quando o Kai lê): ajuda a achar o bilhete no chão e a folha do mezanino
            var glowSpots = new (float x, float y, float s)[] { (1010f, 470f, 1.1f), (1905f, 840f, 0.55f), (3330f, 545f, 0.8f), (4110f, 700f, 0.8f) };
            var glows = new List<SpriteRenderer>();
            foreach (var g in glowSpots)
            {
                var sr = SpriteObj("BrilhoDocumento", spots, Spr("brilho"), new Vector3(X(g.x), Y(g.y), 0f), -50);
                sr.color = new Color(1f, 0.86f, 0.55f, 0.3f);
                sr.transform.localScale = Vector3.one * g.s;
                glows.Add(sr);
            }
            dir.docGlows = glows.ToArray();
            dir.door = door;
            dir.panel = BuildSwitchPanel(core.ui.dialogue.transform.root, fonts, core.voice);
            dir.ambience = ambience;
            dir.sfx = sfx;
            dir.noteClip = Clip("papel");
            dir.pieceClip = Clip("alavanca_pega");
            dir.intro = Lines(
                ("kai", "<i>Uma estação. Vazia, com as luzes ainda acesas.</i>"),
                ("souta", "Acabou o trilho do vagão. A saída é por aquela porta, no fundo."),
                ("ren", "Está trancada. Do lado tem um painel com um desenho dos trilhos."),
                ("kai", "<i>Um painel de desvios. E está faltando pedaço.</i>"));
            dir.renGivesLast = Lines(
                ("ren", "Kai, espera! Eu achei isso no chão, perto dos caixotes."),
                ("ren", "É um pedaço de trilho. É o que está faltando no painel?"),
                ("kai", "<i>É a última peça. Agora dá para completar a linha.</i>"));
            dir.doorClosed = Lines(
                ("kai", "<i>O painel mostra os trilhos da estação. A luz sai da plataforma e tem que chegar até a administração.</i>"),
                ("kai", "<i>Mas estão faltando peças. Sem elas a linha nunca fecha.</i>"));
            dir.allFound = Lines(("kai", "<i>Quatro peças. Agora é encaixar no painel e girar os trilhos até a luz chegar na porta.</i>"));
            dir.doorUnlocked = Lines(
                ("kai", "<i>A linha acendeu inteira. A porta destravou.</i>"),
                ("souta", "Vai. A gente vem logo atrás."));

            SetSpeakers(core.ui.dialogue);
        }
        // ------------------------------------------------------------------ painel de desvios

        /// <summary>
        /// A grade do painel: 5 x 3 peças. A corrente entra pela esquerda na linha do meio e tem que sair pela direita
        /// na linha de baixo. As peças 0 a 3 (as que o Kai acha) ficam no caminho. Solução: girar até
        /// todas ficarem na posição de "masks" (ver a simulação em Ferramentas/Cenarios367/painel_desvios.py).
        /// </summary>
        static SwitchPanelUI BuildSwitchPanel(Transform root, Fonts fonts, AudioSource voice)
        {
            var rt = Stretch("PainelDesvios", root);
            var fade = root.Find("Fade");
            if (fade) rt.SetSiblingIndex(fade.GetSiblingIndex()); // acima do HUD e do diálogo, abaixo do preto
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var ui = rt.gameObject.AddComponent<SwitchPanelUI>();
            Img(Stretch("Escurecer", rt), new Color(0f, 0f, 0f, 0.7f));

            var panel = new GameObject("Painel", typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            panel.SetParent(rt, false);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0.5f, 0.5f);
            panel.sizeDelta = new Vector2(1100f, 760f);
            Img(panel, new Color(0.035f, 0.033f, 0.035f, 0.97f));
            Img(TopLeft("Linha", panel, 0, 0, 1100, 2), Rule);

            var title = Label("Titulo", panel, "PAINEL DE DESVIOS  ·  ESTAÇÃO", fonts.sans, 20, Tagline, 0, 34, 1100, 30);
            title.alignment = TextAlignmentOptions.Top;
            title.characterSpacing = 26;
            var status = Label("Status", panel, "", fonts.italic, 28, Hex("C8B9A0"), 0, 78, 1100, 40);
            status.alignment = TextAlignmentOptions.Top;

            const float cell = 132f, gap = 10f, gx = 200f, gy = 170f;
            var grid = TopLeft("Grade", panel, gx, gy, 5 * cell + 4 * gap, 3 * cell + 2 * gap);

            // entrada (linha 1, à esquerda) e saída (linha 2, à direita)
            float entryY = gy + 1 * (cell + gap) + cell / 2f, exitY = gy + 2 * (cell + gap) + cell / 2f;
            var entry = TopLeft("Entrada", panel, 40, entryY - 13, gx - 40, 26);
            Img(entry, new Color(1f, 0.68f, 0.28f, 1f));
            var entryLabel = Label("RotuloEntrada", panel, "PLATAFORMA", fonts.sans, 18, Muted, 30, entryY - 52, 200, 26);
            entryLabel.characterSpacing = 12;
            float gridRight = gx + 5 * cell + 4 * gap;
            var exit = TopLeft("Saida", panel, gridRight, exitY - 13, 1040 - gridRight, 26);
            Img(exit, new Color(0.34f, 0.32f, 0.29f, 1f));
            var lamp = Img(TopLeft("Lampada", panel, 1030, exitY - 24, 48, 48), new Color(0.75f, 0.12f, 0.08f, 1f));
            var exitLabel = Label("RotuloSaida", panel, "ADMINISTRAÇÃO", fonts.sans, 18, Muted, 870, exitY + 34, 220, 26);
            exitLabel.alignment = TextAlignmentOptions.TopRight;
            exitLabel.characterSpacing = 12;

            var help = Label("Ajuda", panel, "SETAS MOVEM   ·   E GIRA   ·   Q GIRA AO CONTRÁRIO   ·   CLIQUE GIRA   ·   ESC FECHA",
                fonts.sans, 15, Muted, 0, 700, 1100, 24);
            help.alignment = TextAlignmentOptions.Top;
            help.characterSpacing = 8;

            const int N = SwitchPanelUI.N, E = SwitchPanelUI.E, S = SwitchPanelUI.S, W = SwitchPanelUI.W;
            ui.columns = 5;
            ui.rows = 3;
            ui.masks = new[]
            {
                S | E, W | E,     W | S, W | S,     N | S,
                W | N, N | E | S, N | S, N | E | W, W | N,
                N | E, W | E,     N | E, W | E,     W | E,
            };
            ui.startTurns = new[]
            {
                1, 1, 2, 3, 0,
                2, 0, 1, 1, 1,
                2, 1, 3, 1, 1,
            };
            ui.pieceOf = new[]
            {
                -1, 1, -1, -1, -1,
                 0, -1, 2, -1, -1,
                -1, -1, -1, 3, -1,
            };
            ui.entryRow = 1;
            ui.exitRow = 2;
            ui.group = group;
            ui.grid = grid;
            ui.entryMarker = entry;
            ui.exitMarker = exit;
            ui.exitLamp = lamp;
            ui.status = status;
            ui.font = fonts.sans;
            ui.cellSize = cell;
            ui.gap = gap;
            ui.sfx = voice;
            ui.turnClip = Clip("cadeado_clique");
            ui.placeClip = Clip("vazio_encaixe");
            ui.solvedClip = Clip("cadeado_abre");
            return ui;
        }
    }
}
