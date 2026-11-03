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
    /// Monta a cena Assets/Scenes/Cenario3.unity (a estação: documentos espalhados, um mezanino e a porta da administração).
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Cenário 3.
    /// As medidas vêm de Ferramentas/Cenarios367/cenarios_finais.py (mesmos números).
    /// </summary>
    [InitializeOnLoad]
    public static class Cenario3SceneBuilder
    {
        const int BuildVersion = 2; // aumente para forçar a cena a ser recriada automaticamente
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
            var core = BuildCore(playerPrefab, Load(Art3, "cenario3_fundo"), W, Floor, LWall, RWall, 330f, withLock: true, fonts);
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
                "<i>Escrito a lápis, na margem:</i>\nCódigo da administração, parte <b>1</b> de 4:  <b>5</b>";
            d1.docFooter = "Folha 1 de 4.";
            d1.first = Lines(("kai", "<i>Uma folha presa no quadro. \"Parte 1 de 4: 5\".</i>"), ("kai", "<i>Então o código tem quatro números. Eu preciso achar as outras folhas.</i>"));
            d1.again = Lines(("kai", "<i>Parte 1: 5.</i>"));

            var d2 = BuildSpot(spots, "bilhete", 1900f, Floor, 1.3f, 1.4f, sfx, papel);
            d2.docTitle = "BILHETE DE EMBARQUE";
            d2.docBody =
                "<b>ÚLTIMA LINHA</b>\nDestino: plataforma final\nValidade: uma viagem, sem volta.\n\n" +
                "<i>No verso, com a mesma letra:</i>\nparte <b>2</b> de 4:  <b>2</b>";
            d2.docFooter = "Folha 2 de 4.";
            d2.first = Lines(("kai", "<i>Um bilhete caído no chão, com a letra do quadro. \"Parte 2: 2\".</i>"));
            d2.again = Lines(("kai", "<i>Parte 2: 2.</i>"));

            var d3 = BuildSpot(spots, "guiche", 3350f, DeckY, 2.9f, 1.8f, sfx, papel);
            d3.docTitle = "ESCALA DOS FUNCIONÁRIOS";
            d3.docBody =
                "Turno da noite:  ninguém escalado.\nTurno da madrugada:  ninguém escalado.\n\n" +
                "A estação está funcionando sozinha há muito tempo. Quem estiver lendo isto: não espere por ninguém.\n\n" +
                "<i>Anotado embaixo:</i>\nparte <b>3</b> de 4:  <b>9</b>";
            d3.docFooter = "Folha 3 de 4.";
            d3.first = Lines(("kai", "<i>No guichê dos funcionários, no alto. \"Parte 3: 9\".</i>"), ("kai", "<i>\"Ninguém escalado.\" Então quem mantém essa estação ligada?</i>"));
            d3.again = Lines(("kai", "<i>Parte 3: 9.</i>"));

            var d4 = BuildSpot(spots, "recibo", 4100f, Floor, 3.6f, 1.6f, sfx, papel);
            d4.docTitle = "RECIBO DA MÁQUINA";
            d4.docBody =
                "<b>MÁQUINA AUTOMÁTICA</b>\n1 água · 1 pacote de biscoito\nTotal: pago.\n\n" +
                "<i>Impresso embaixo, em letras miúdas:</i>\nparte <b>4</b> de 4:  <b>1</b>";
            d4.docFooter = "Folha 4 de 4.";
            d4.first = Lines(("kai", "<i>O recibo saiu da máquina sozinho. \"Parte 4: 1\".</i>"), ("kai", "<i>Quatro folhas. Agora a porta.</i>"));
            d4.again = Lines(("kai", "<i>Parte 4: 1.</i>"));

            var souta = BuildNpc("Souta", "souta", 450f, Floor, 3.55f, player.transform);
            souta.firstTalk = Lines(
                ("souta", "A porta da administração tem um teclado de quatro números."),
                ("souta", "Deve ter papéis espalhados com os números. Procura pela estação inteira."));
            souta.repeatTalk = Lines(("souta", "Quatro folhas, quatro números. Ainda falta alguma?"));
            var ren = BuildNpc("Ren", "ren", 540f, Floor, 3.3f, player.transform);
            ren.firstTalk = Lines(
                ("ren", "Eu vi uma pilha de caixotes lá no fundo. Dá para subir até o mezanino."),
                ("ren", "Os funcionários ficavam lá em cima."));
            ren.repeatTalk = Lines(("ren", "Se faltar uma folha, olha lá em cima, nos caixotes."));

            var dir = new GameObject("Diretor_Cenario3").AddComponent<Cenario3Director>();
            dir.player = player;
            dir.cameraFollow = core.follow;
            dir.souta = souta;
            dir.ren = ren;
            dir.documents = new[] { d1, d2, d3, d4 };
            dir.digits = new[] { 5, 2, 9, 1 };
            dir.door = door;
            dir.lockUI = core.ui.lockUi;
            dir.ambience = ambience;
            dir.sfx = sfx;
            dir.noteClip = Clip("papel");
            dir.intro = Lines(
                ("kai", "<i>Uma estação. Vazia, com as luzes ainda acesas.</i>"),
                ("souta", "Acabou o trilho do vagão. A saída é por aquela porta, no fundo."),
                ("ren", "Está trancada. Tem um teclado do lado."),
                ("kai", "<i>Quatro números. Em algum lugar tem que estar escrito.</i>"));
            dir.doorClosed = Lines(("kai", "<i>Um teclado de quatro números. Eu ainda não tenho nenhuma pista.</i>"));
            dir.allFound = Lines(("kai", "<i>Quatro folhas, na ordem: 5, 2, 9, 1. Agora é só digitar na porta.</i>"));
            dir.doorUnlocked = Lines(
                ("kai", "<i>Abriu.</i>"),
                ("souta", "Vai. A gente vem logo atrás."));

            SetSpeakers(core.ui.dialogue);
        }
    }
}
