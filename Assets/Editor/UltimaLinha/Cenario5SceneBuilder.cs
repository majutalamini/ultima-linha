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
        const int BuildVersion = 8; // aumente para forçar a cena a ser recriada automaticamente
        const string ScenePath = "Assets/Scenes/Cenario5.unity";
        const string Art5 = "Assets/Art/Cenario5";

        // fundo: os painéis c5_painel_a/b do usuário (Ferramentas/Cenarios4a7), com mesas, arquivos e a mesa do chefe
        // pintados; as medidas abaixo foram tiradas deles (px "antigos": 100 por unidade). Corpo, placas, gavetas e chave
        // continuam a arte antiga por cima.
        const float PePx = 585f;
        const float W = 3900f, Floor = 850f, LWall = 120f, RWall = 3800f;
        const float DoorX0 = 3519f, DoorX1 = 3750f, DoorTop = 321f, DoorBase = 790f; // o vão vazio pintado em c5_painel_b
        const float BossDeskX = 1840f, BodyX = 2505f;
        // corpo e cabeça (sprites separados, mesmas medidas de Ferramentas/Cenarios45/cenario5.py: BODY_BOX e HEAD_BOX)
        // o corpo: um passageiro do C0 deitado (Ferramentas/Cenario5/corpo.py, 178 x 55 px; a cabeça, 40 px, à direita)
        // o chefe pendurado: a corda sai de baixo do cano do teto (RopeY) bem em cima de BodyX; a poça e a cadeira no chão
        const float RopeY = 94.8f, PoolDx = 7f, ChairDx = -102f, BloodX0 = 2440f, BloodX1 = 2580f;
        const float PlateY = 383f, KeyY = 620f, DrawerTopY = 600f, DrawerDx = 3.5f; // plaquinhas e topo da 3ª gaveta dos arquivos pintados
        static readonly (string id, string label, float x0, float x1)[] Cabinets =
        {
            ("arquivoA", "ADMISSÕES", 1320f, 1490f),
            ("arquivoB", "AVALIAÇÕES", 2870f, 3030f),
            ("arquivoC", "DEMISSÕES", 3060f, 3220f),
            ("arquivoD", "FINANCEIRO", 3250f, 3410f),
        };
        // placas legíveis em cima dos arquivos (Ferramentas/Cenarios45/extras5.py), na mesma ordem
        static readonly string[] Plates = { "placa_admissoes", "placa_avaliacoes", "placa_demissoes", "placa_financeiro" };
        static readonly (float x, float w)[] Tubes = { (610f, 390f), (1550f, 390f), (2780f, 390f), (3720f, 390f) }; // as pintadas

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
                () => Load(Art5, "corpo_chefe") != null, Populate);
        }

        // ------------------------------------------------------------------

        static void Populate(GameObject playerPrefab)
        {
            var fonts = LoadFonts();
            var core = BuildCore(playerPrefab, null, W, Floor, LWall, RWall, 330f, withLock: false, fonts);
            Paineis(core.world, Floor, PePx, "c5_painel_a", "c5_painel_b");
            var player = core.player;
            var sfx = core.sfx;

            // ---------- Som ----------
            var ambience = Source(core.audioRoot, "AmbienteEscritorio", Clip("ambiente_escritorio"), loop: true);
            var heartbeat = Source(core.audioRoot, "Coracao", Clip("coracao"), loop: true);

            // ---------- Luzes ----------
            BuildTubes(core.world, Tubes, 126f, i => i == 1 || i == 3);

            // ---------- Porta do escritório ----------
            var door = PortaNoVao(core.world, "PortaDoEscritorio", "porta_c5", DoorX0, DoorX1, DoorTop, DoorBase, sfx);

            // ---------- Lugares de interação ----------
            var spots = new GameObject("Lugares").transform;
            spots.SetParent(core.world, false);

            var body = BuildSpot(spots, "corpo", BodyX, Floor, 3.2f, 1.6f);
            // o chefe pendurado pelo pescoço no cano do teto, a cadeira chutada embaixo e o sangue no chão:
            // montaram para parecer suicídio (Ferramentas/Cenario5/enforcado.py desenha os três)
            ConfigureSprite($"{Paineis4a7}/enforcado.png", new Vector2(0.5f, 1f), PixelArtImporter.Ppu, 512, FilterMode.Point);
            ConfigureSprite($"{Paineis4a7}/cadeira_caida.png", new Vector2(0.5f, 0f), PixelArtImporter.Ppu, 256, FilterMode.Point);
            ConfigureSprite($"{Paineis4a7}/sangue_chefe.png", new Vector2(0.5f, 0.5f), PixelArtImporter.Ppu, 256, FilterMode.Point);
            var tomCena = new Color(0.82f, 0.79f, 0.82f, 1f); // o mesmo tom que assenta os personagens no escuro
            SpriteObj("SangueDoChefe", body.transform, Load(Paineis4a7, "sangue_chefe"), new Vector3(X(BodyX + PoolDx), Y(Floor) + 0.03f, 0f), 2);
            var chair = SpriteObj("CadeiraCaida", body.transform, Load(Paineis4a7, "cadeira_caida"), new Vector3(X(BodyX + ChairDx), Y(Floor) + 0.02f, 0f), 3);
            chair.color = tomCena;
            var hanged = SpriteObj("ChefeEnforcado", body.transform, Load(Paineis4a7, "enforcado"), new Vector3(X(BodyX), Y(RopeY), 0f), 4);
            hanged.color = tomCena;
            var crossing = body.gameObject.AddComponent<BodyCrossing>();
            crossing.player = player;
            crossing.x0 = X(BloodX0);
            crossing.x1 = X(BloodX1);
            crossing.head = null; // a cabeça fica presa ao corpo
            crossing.sfx = sfx;
            crossing.squelch = new[] { Clip("sangue_passo_1"), Clip("sangue_passo_2"), Clip("sangue_passo_3") };
            crossing.firstSticky = Lines(("kai", "<i>O chão gruda nos sapatos. É sangue. Ainda está mole.</i>"));
            body.docTitle = "JORNAL DA MANHÃ";
            body.docBody =
                "<b>EXECUTIVO É ENCONTRADO ENFORCADO NO PRÓPRIO ESCRITÓRIO</b>\n\n" +
                "O diretor da empresa foi encontrado pendurado no teto da sala na noite de terça-feira. A cena parecia suicídio, " +
                "mas a perícia achou marcas de estrangulamento por baixo da corda: <b>ele já estava morto quando foi pendurado</b>.\n\n" +
                "A polícia procura um funcionário do setor de operações, identificado como <b>Kai</b>, visto discutindo com a vítima " +
                "poucas horas antes. Segundo colegas, Kai havia sido avisado de que seria demitido naquela semana.";
            body.docFooter = "Recorte dobrado ao meio, achado no bolso do paletó.";
            body.first = Lines(
                ("kai", "<i>A corda, a cadeira caída... era para parecer que ele mesmo fez isso.</i>"),
                ("kai", "<i>Mas no pescoço, por baixo da corda, tem marcas roxas. De dedos.</i>"),
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
                "Ninguém do setor deve saber até o último dia.";
            desk.docFooter = "Assinado: o Diretor.";
            desk.first = Lines(("kai", "<i>Meu nome. Meu desligamento. Ele ia me demitir sem avisar ninguém.</i>"));
            desk.again = Lines(("kai", "<i>Ele ia me demitir sem avisar ninguém.</i>"));

            // gaveta aberta: frente recortada do próprio painel (mesmo visual da fechada) + vão com as pastas
            foreach (var n in new[] { "gaveta_frente", "gaveta_dentro" })
                ConfigureSprite($"{Paineis4a7}/{n}.png", new Vector2(0.5f, 1f), PixelArtImporter.Ppu, 256, FilterMode.Point);
            var cabSpots = new List<ObjectSpot>();
            for (int ci = 0; ci < Cabinets.Length; ci++)
            {
                var cab = Cabinets[ci];
                SpriteObj("Placa_" + cab.id, core.world, Load(Art5, Plates[ci]), new Vector3(X((cab.x0 + cab.x1) / 2f), Y(PlateY), 0f), 1);
                // a gaveta de demissões abre sem som (a chave aparece em silêncio)
                var abre = cab.id == "arquivoC" ? null : Clip("gaveta_abre");
                var spot = BuildSpot(spots, cab.id, (cab.x0 + cab.x1) / 2f, Floor, 3.5f, 1.2f, core.sfx, abre);
                spot.again = Lines(("kai", "<i>Já olhei aqui.</i>"));
                var open = new GameObject("GavetaAberta_" + cab.id);
                open.transform.SetParent(core.world, false);
                open.transform.position = new Vector3(X((cab.x0 + cab.x1) / 2f + DrawerDx), Y(DrawerTopY), 0f);
                var dentro = SpriteObj("Pastas", open.transform, Load(Paineis4a7, "gaveta_dentro"), open.transform.position, 2);
                var frente = SpriteObj("Frente", open.transform, Load(Paineis4a7, "gaveta_frente"), open.transform.position, 3);
                var anim = open.AddComponent<GavetaAbrindo>();
                anim.frente = frente.transform;
                anim.dentro = dentro.transform;
                anim.saida = 10f / PixelArtImporter.Ppu;
                anim.pixel = 1f / PixelArtImporter.Ppu;
                open.SetActive(false);
                spot.openVisual = open;
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
                ("kai", "<i>E atrás dela, presa com fita... uma chave. Ficou em cima das pastas.</i>"));
            cabSpots[3].first = Lines(
                ("kai", "<i>Planilhas, recibos. Uma pasta com o meu salário riscado de vermelho.</i>"),
                ("kai", "<i>Nenhuma chave.</i>"));

            // sem brilho nem destaque no arquivo da chave: o jogador descobre sozinho
            float keyCabX = (Cabinets[2].x0 + Cabinets[2].x1) / 2f;

            // a chave na gaveta aberta: aparece quando o Kai abre o arquivo de demissões; E pega
            var keySpot = BuildSpot(spots, "chave", keyCabX, Floor, 2.75f, 1.1f);
            var keyGlow = SpriteObj("Brilho", keySpot.transform, Spr("brilho"), new Vector3(X(keyCabX), Y(KeyY), 0f), 3);
            keyGlow.color = new Color(1f, 0.85f, 0.5f, 0.45f);
            keyGlow.transform.localScale = Vector3.one * 0.45f;
            SpriteObj("Chave", keySpot.transform, Load(Art5, "chave"), new Vector3(X(keyCabX), Y(KeyY), 0f), 4);

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

            // ---------- Diretor ----------
            var dir = new GameObject("Diretor_Cenario5").AddComponent<Cenario5Director>();
            dir.player = player;
            dir.cameraFollow = core.follow;
            dir.souta = souta;
            dir.ren = ren;
            dir.body = body;
            dir.bossDesk = desk;
            dir.cabinets = cabSpots.ToArray();
            dir.keyCabinetId = "arquivoC";
            dir.keyPickup = keySpot;
            dir.cabinetGlow = null;
            dir.keyGlow = keyGlow;
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
                ("souta", "Kai... tem um homem pendurado ali. No cano do teto."),
                ("ren", "Ele se... enforcou? E aquilo no chão é... sangue?"),
                ("kai", "<i>Eu conheço esse escritório. Eu conheço essa mesa.</i>"),
                ("souta", "Vê se ele tem algo no bolso. Um documento, qualquer coisa que diga quem é."),
                ("kai", "<i>...Por que sou eu que tenho que olhar?</i>"));
            dir.doorBeforeBody = Lines(("kai", "<i>Antes de sair, eu preciso saber quem é ele.</i>"));
            dir.confront1 = Lines(
                ("ren", "Kai... isso é o seu nome? Aqui, no jornal."),
                ("souta", "Me dá isso. ...\"Procura-se funcionário Kai.\""),
                ("souta", "Você matou ele."),
                ("ren", "E pendurou ele ali... pra parecer que foi ele mesmo?"),
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
                ("kai", "<i>Eu preciso sair daqui. Tem que existir outra chave em algum lugar.</i>"));
            dir.renOutside = Lines(
                ("ren", "<i>(do outro lado da porta)</i> Kai... a gente vai chamar alguém. Fica quieto, por favor."),
                ("kai", "<i>Ren... eu juro que eu não sabia.</i>"));
            dir.cabinetLocked = Lines(
                ("kai", "<i>Emperrada. O trinco dessa gaveta é preso aos outros arquivos...</i>"),
                ("kai", "<i>Preciso abrir os outros primeiro.</i>"));
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
