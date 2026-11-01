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
    /// Monta a cena Assets/Scenes/Cenario5.unity: o escritório abandonado, com o corpo do chefe que o Kai matou (clímax).
    /// Do Notion: a alavanca que falta está no bolso do chefe, junto de um jornal que diz que o Kai o matou. Souta e Ren
    /// descobrem, ficam desesperados, pegam uma arma, apontam para o Kai e o trancam no escritório. A chave está numa das gavetas.
    ///
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Cenário 5.
    /// As medidas vêm de Ferramentas/Cenarios45/cenario5.py (mesmos números).
    /// </summary>
    [InitializeOnLoad]
    public static class Cenario5SceneBuilder
    {
        const int BuildVersion = 1; // aumente para forçar a cena a ser recriada automaticamente
        const string ScenePath = "Assets/Scenes/Cenario5.unity";
        const string Art5 = "Assets/Art/Cenario5";

        const float W = 3600f, Floor = 850f, LWall = 120f, RWall = 3540f;
        const float DoorX0 = 3250f, DoorX1 = 3430f, DoorTop = 300f;
        const float BossDeskX = 1790f, BodyX = 2330f;
        static readonly (string id, string label, float x0, float x1)[] Cabinets =
        {
            ("arquivoA", "ADMISSÕES", 1280f, 1410f),
            ("arquivoB", "AVALIAÇÕES", 2640f, 2770f),
            ("arquivoC", "DEMISSÕES", 2810f, 2940f),
            ("arquivoD", "FINANCEIRO", 2980f, 3110f),
        };
        static readonly (float x, float w)[] Tubes = { (560f, 240f), (1300f, 240f), (2050f, 240f), (2800f, 240f), (3280f, 240f) };

        static string VersionKey => "UltimaLinha.Cenario5.Version:" + Application.dataPath;
        static double firstTry = -1;

        static Cenario5SceneBuilder()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        static void TryAutoBuild()
        {
            if (EditorPrefs.GetInt(VersionKey, 0) >= BuildVersion) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (WaitForEditor(ref firstTry, TryAutoBuild, "Abrir Cenário 5")) return;
            if (Build(interactive: false))
            {
                EditorPrefs.SetInt(VersionKey, BuildVersion);
                Debug.Log("[Última Linha] Cenário 5 criado em " + ScenePath + ". Abra pelo menu Última Linha > Abrir Cenário 5.");
            }
        }

        [MenuItem("Última Linha/Recriar Cenário 5")]
        static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Recriar Cenário 5",
                    "A cena Cenario5 já existe. Recriar vai substituir qualquer mudança feita nela. Continuar?", "Recriar", "Cancelar"))
                return;
            if (Build(interactive: true))
                EditorPrefs.SetInt(VersionKey, BuildVersion);
        }

        [MenuItem("Última Linha/Abrir Cenário 5")]
        static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { BuildFromMenu(); return; }
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
        }

        static bool Build(bool interactive)
        {
            ImportFolder(Art5, new Dictionary<string, Vector2> { { "cenario5_fundo", Vector2.zero } }, "cenario5_fundo");
            return Run(interactive, ScenePath, "Cenário 5", new[] { "Cenario4", "Cenario3", "Cenario2", "Cenario1", "MainMenu" },
                () => Load(Art5, "cenario5_fundo") != null, Populate);
        }

        // ------------------------------------------------------------------

        static void Populate(GameObject playerPrefab)
        {
            var fonts = LoadFonts();
            var core = BuildCore(playerPrefab, Load(Art5, "cenario5_fundo"), W, Floor, LWall, RWall, 330f, withLock: false, fonts);
            var player = core.player;
            var sfx = core.sfx;

            // ---------- Som ----------
            var ambience = Source(core.audioRoot, "AmbienteEscritorio", Clip("ambiente_escritorio"), loop: true);
            var heartbeat = Source(core.audioRoot, "Coracao", Clip("coracao"), loop: true);

            // ---------- Luzes ----------
            BuildTubes(core.world, Tubes, 111f, i => i == 1 || i == 3);

            // ---------- Porta do escritório ----------
            var door = BuildExitDoor(core.world, "PortaDoEscritorio", Art5, "porta5", DoorX0, DoorX1, DoorTop, Floor, sfx, Clip1("porta_destrava"));

            // ---------- Lugares de interação ----------
            var spots = new GameObject("Lugares").transform;
            spots.SetParent(core.world, false);

            var body = BuildSpot(spots, "corpo", BodyX, Floor, 1.6f, 1.6f);
            body.docTitle = "JORNAL DA MANHÃ";
            body.docBody =
                "<b>EXECUTIVO É ENCONTRADO MORTO NO PRÓPRIO ESCRITÓRIO</b>\n\n" +
                "O diretor da empresa foi encontrado sem vida na noite de terça-feira. A polícia procura um funcionário do setor de operações, " +
                "identificado como <b>Kai</b>, visto discutindo com a vítima poucas horas antes.\n\n" +
                "Segundo colegas, Kai havia sido avisado de que seria demitido naquela semana. Ele não foi mais visto desde então.";
            body.docFooter = "Recorte dobrado ao meio, achado no bolso do paletó.";
            body.first = Lines(
                ("kai", "<i>No bolso dele, uma alavanca de ferro. A que faltava no painel lá embaixo.</i>"),
                ("kai", "<i>E esse recorte...</i>"),
                ("kai", "<i>Não. Eu lembro da discussão. Eu lembro da mesa. Eu lembro das minhas mãos.</i>"),
                ("kai", "<i>Foi o que eu fiz.</i>"));
            body.again = Lines(("kai", "<i>Eu não quero mais olhar para ele.</i>"));

            var desk = BuildSpot(spots, "mesaChefe", BossDeskX, Floor, 2.9f, 1.6f, core.sfx, Clip("gaveta_abre"));
            desk.docTitle = "MEMORANDO INTERNO";
            desk.docBody =
                "Para: Setor de Operações\nDe: Direção\n\n" +
                "O funcionário K. será desligado ao fim do mês. <b>Não avisar antes.</b>\n\n" +
                "Os papéis ficam no arquivo de <b>DEMISSÕES</b>, junto da chave reserva da sala. Ninguém procura lá.";
            desk.docFooter = "Assinado: o Diretor.";
            desk.first = Lines(
                ("kai", "<i>Meu nome. Meu desligamento. Ele ia me demitir sem avisar ninguém.</i>"),
                ("kai", "<i>\"A chave reserva fica junto dos papéis de demissão.\"</i>"));
            desk.again = Lines(("kai", "<i>Arquivo de demissões. É lá que está a chave.</i>"));

            var cabSpots = new List<ObjectSpot>();
            foreach (var cab in Cabinets)
            {
                var spot = BuildSpot(spots, cab.id, (cab.x0 + cab.x1) / 2f, Floor, 3.5f, 1.2f, core.sfx, Clip("gaveta_abre"));
                spot.again = Lines(("kai", "<i>Já olhei aqui.</i>"));
                var open = SpriteObj("GavetaAberta_" + cab.id, core.world, Load(Art5, "arquivo_gaveta"),
                    new Vector3(X((cab.x0 + cab.x1) / 2f), Y(690f), 0f), 2);
                open.gameObject.SetActive(false);
                spot.openVisual = open.gameObject;
                cabSpots.Add(spot);
            }
            cabSpots[0].first = Lines(
                ("kai", "<i>Fichas de admissão. A minha está aqui: \"contratado há seis anos\".</i>"),
                ("kai", "<i>Nenhuma chave.</i>"));
            cabSpots[1].first = Lines(
                ("kai", "<i>Avaliações de desempenho. \"Kai: insuficiente.\" \"Kai: insuficiente.\" Dezenas delas.</i>"),
                ("kai", "<i>Nenhuma chave.</i>"));
            cabSpots[2].first = Lines(
                ("kai", "<i>Pastas de demissões. A minha está por cima: \"Kai — desligamento imediato\".</i>"),
                ("kai", "<i>E atrás dela, presa com fita... uma chave.</i>"));
            cabSpots[3].first = Lines(
                ("kai", "<i>Planilhas, recibos. Uma pasta com o meu salário riscado de vermelho.</i>"),
                ("kai", "<i>Nenhuma chave.</i>"));

            // ---------- Souta e Ren ----------
            var souta = BuildNpc("Souta", "souta", 450f, Floor, 3.55f, player.transform);
            souta.firstTalk = Lines(
                ("souta", "Vê se ele tem algo no bolso. Um documento, qualquer coisa que diga quem é."));
            souta.repeatTalk = Lines(("souta", "Anda logo, Kai."));
            var ren = BuildNpc("Ren", "ren", 540f, Floor, 3.3f, player.transform);
            ren.firstTalk = Lines(
                ("ren", "Ele está... ele não está respirando. Tem sangue por todo lado."),
                ("kai", "<i>Eu conheço esse escritório.</i>"));
            ren.repeatTalk = Lines(("ren", "Eu não quero ficar perto dele."));

            // pistola na mão do Souta (aparece quando ele pega a arma)
            var gun = SpriteObj("Pistola", souta.rig.transform, Load(Art5, "pistola"), Vector3.zero, 12);
            gun.transform.localPosition = new Vector3(0.58f, 1.22f, 0f);
            gun.transform.localScale = Vector3.one * 0.55f;
            gun.gameObject.SetActive(false);

            // ---------- Diretor ----------
            var dir = new GameObject("Diretor_Cenario5").AddComponent<Cenario5Director>();
            dir.player = player;
            dir.cameraFollow = core.follow;
            dir.souta = souta;
            dir.ren = ren;
            dir.gunProp = gun.gameObject;
            dir.body = body;
            dir.bossDesk = desk;
            dir.cabinets = cabSpots.ToArray();
            dir.keyCabinetId = "arquivoC";
            dir.door = door;
            dir.deskX = X(BossDeskX);
            dir.doorX = X((DoorX0 + DoorX1) / 2f);
            dir.kaiBodyX = X(BodyX);
            dir.redTint = core.ui.redTint;
            dir.ambience = ambience;
            dir.heartbeat = heartbeat;
            dir.sfx = sfx;
            dir.gunTakeClip = Clip("arma_pega");
            dir.gunCockClip = Clip("arma_engatilha");
            dir.doorSlamClip = Clip("porta_batida");
            dir.doorLockClip = Clip("porta_tranca");
            dir.knockClip = Clip("batida_na_porta");
            dir.keyClip = Clip("chave_pega");
            dir.leverClip = Clip("alavanca_pega");

            dir.intro = Lines(
                ("kai", "<i>Essa porta de vidro fosco. Esse corredor.</i>"),
                ("souta", "Kai... tem um homem no chão, atrás da mesa."),
                ("ren", "Ele não está se mexendo. E aquilo no chão é... sangue?"),
                ("kai", "<i>Eu conheço esse escritório. Eu conheço essa mesa.</i>"),
                ("souta", "Vê se ele tem algo no bolso. Um documento, qualquer coisa que diga quem é."),
                ("kai", "<i>...Por que sou eu que tenho que olhar?</i>"));
            dir.doorBeforeBody = Lines(("kai", "<i>Antes de sair, eu preciso saber quem é ele.</i>"));
            dir.confront1 = Lines(
                ("ren", "Kai... isso é o seu nome? Aqui, no jornal."),
                ("souta", "Me dá isso. ...\"Procura-se funcionário Kai.\""),
                ("souta", "Você matou ele."),
                ("kai", "Eu... eu não lembro. Souta, eu não..."),
                ("ren", "A gente confiou em você! Desde o vagão, desde o começo!"),
                ("souta", "Você vivia dizendo que ele pegava no seu pé. Mas matar...?"));
            dir.confront2 = Lines(
                ("souta", "Ele guardava uma arma na gaveta da mesa. Eu sabia."),
                ("ren", "Souta, não! A gente não precisa disso!"),
                ("souta", "Fica onde está, Kai. Mãos onde eu possa ver."),
                ("kai", "Souta, abaixa isso. Sou eu."),
                ("souta", "Foi o que o chefe deve ter dito."));
            dir.confront3 = Lines(
                ("ren", "Eu só quero ir embora... Eu tenho medo de você, Kai."),
                ("souta", "A gente vai buscar ajuda. Você fica aqui."),
                ("kai", "Esperem! Não me deixem aqui!"));
            dir.afterLock = Lines(
                ("kai", "<i>Trancaram. Pelo lado de fora.</i>"),
                ("kai", "<i>Eles têm razão em ter medo. Eu também tenho.</i>"),
                ("kai", "<i>A chave tem que estar em algum lugar. O chefe guardava tudo nesses arquivos e nessa mesa.</i>"));
            dir.renOutside = Lines(
                ("ren", "<i>(do outro lado da porta)</i> Kai... a gente vai chamar alguém. Fica quieto, por favor."),
                ("kai", "<i>Ren... eu juro que eu não sabia.</i>"));
            dir.cabinetHint = Lines(
                ("kai", "<i>Nenhuma chave nesses arquivos. Só falta o de demissões.</i>"),
                ("kai", "<i>Se ele escondesse algo, seria junto da minha própria demissão.</i>"));
            dir.doorNoKey = Lines(("kai", "<i>Trancada por fora. Preciso da chave.</i>"));
            dir.keyFound = Lines(
                ("kai", "<i>Achei. A chave reserva, escondida junto da minha própria demissão.</i>"),
                ("kai", "<i>Agora preciso sair. Com a alavanca no bolso... o trem ainda espera.</i>"));
            dir.doorUnlocked = Lines(
                ("kai", "<i>Destrancada. Do outro lado, eles estão me esperando.</i>"),
                ("kai", "<i>Eu preciso colocar a alavanca no painel antes que me achem.</i>"));

            SetSpeakers(core.ui.dialogue);
        }
    }
}
