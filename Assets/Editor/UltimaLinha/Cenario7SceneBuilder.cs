using System.Collections.Generic;
using System.IO;
using UltimaLinha.Cenario1;
using UltimaLinha.Cenario2;
using TMPro;
using UltimaLinha.Salas;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UltimaLinha.EditorTools.Cenario1SceneBuilder;
using static UltimaLinha.EditorTools.SalasBuild;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Monta a cena Assets/Scenes/Cenario7.unity (a plataforma final: a escolha do jogador, os dois finais e o plantão do telejornal).
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Cenário 7.
    /// As medidas vêm de Ferramentas/Cenarios367/cenarios_finais.py (mesmos números).
    /// </summary>
    [InitializeOnLoad]
    public static class Cenario7SceneBuilder
    {
        const int BuildVersion = 8; // aumente para forçar a cena a ser recriada automaticamente
        const string ScenePath = "Assets/Scenes/Cenario7.unity";
        const string Art7 = "Assets/Art/Cenario7";

        // fundo: o painel c7_painel_b do usuário (Ferramentas/Cenarios4a7): a plataforma termina na beirada amarela (11,55 u),
        // depois vêm os trilhos e a boca do túnel. Tudo foi trazido para perto do começo do painel (px "antigos": 100 por u).
        const float PePx = 540f;
        // a parede da direita fica antes do Ren: o Kai não passa dele nem chega na beira (só nas cenas do final)
        const float W = 2150f, Floor = 850f, LWall = 60f, RWall = 880f;
        const float EdgeX = 1155f, RenX = 925f, SoutaX = 705f, TunnelX = 1630f;
        const float PitPy = 991f; // os trilhos, logo abaixo da beirada
        static readonly (float x, float w)[] Tubes = { (590f, 400f), (1590f, 400f) }; // as pintadas

        static string VersionKey => "UltimaLinha.Cenario7.Version:" + Application.dataPath;
        static double firstTry = -1;

        static Cenario7SceneBuilder()
        {
            EditorApplication.delayCall += TryAutoBuild;
        }

        static void TryAutoBuild()
        {
            if (EditorPrefs.GetInt(VersionKey, 0) >= BuildVersion) return;
            if (EditorApplication.isPlayingOrWillChangePlaymode) return;
            if (WaitForEditor(ref firstTry, TryAutoBuild, "Abrir Cenário 7")) return;
            if (Build(interactive: false))
            {
                EditorPrefs.SetInt(VersionKey, BuildVersion);
                Debug.Log("[Última Linha] Cenário 7 criado em " + ScenePath + ". Abra pelo menu Última Linha > Abrir Cenário 7.");
            }
        }

        [MenuItem("Última Linha/Recriar Cenário 7")]
        static void BuildFromMenu()
        {
            if (File.Exists(ScenePath) &&
                !EditorUtility.DisplayDialog("Recriar Cenário 7",
                    "A cena Cenario7 já existe. Recriar vai substituir qualquer mudança feita nela. Continuar?", "Recriar", "Cancelar"))
                return;
            if (Build(interactive: true))
                EditorPrefs.SetInt(VersionKey, BuildVersion);
        }

        [MenuItem("Última Linha/Abrir Cenário 7")]
        static void OpenScene()
        {
            if (!File.Exists(ScenePath)) { BuildFromMenu(); return; }
            if (UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo())
                UnityEditor.SceneManagement.EditorSceneManager.OpenScene(ScenePath);
        }

        static bool Build(bool interactive)
        {
            ImportFolder(Art7, new Dictionary<string, Vector2> { { "cenario7_fundo", Vector2.zero } }, "cenario7_fundo");
            return Run(interactive, ScenePath, "Cenário 7", new[] { "Cenario6", "Cenario5", "Cenario4", "Cenario3", "Cenario2", "Cenario1", "MainMenu" },
                () => File.Exists(Paineis4a7 + "/c7_painel_b.png"), Populate);
        }

        // ------------------------------------------------------------------

        static void Populate(GameObject playerPrefab)
        {
            var fonts = LoadFonts();
            var core = BuildCore(playerPrefab, null, W, Floor, LWall, RWall, 150f, withLock: false, fonts);
            Paineis(core.world, Floor, PePx, "c7_painel_b");
            var player = core.player;
            var sfx = core.sfx;

            float extra = 1f; // na queda a câmera desce até os trilhos
            core.follow.bounds = new Rect(0f, 0f, X(W) - 0.01f, ImgH / PPU);

            var ambience = Source(core.audioRoot, "AmbientePlataforma", Clip("ambiente_plataforma"), loop: true);
            var rumble = Source(core.audioRoot, "Trem", Clip("trem_aproxima"), loop: false);
            var siren = Source(core.audioRoot, "Sirene", null, loop: false);

            BuildTubes(core.world, Tubes, 203f, i => i == 1);

            var spots = new GameObject("Lugares").transform;
            spots.SetParent(core.world, false);

            // Ren: na beira da plataforma, de costas para o Kai, olhando os trilhos. Falar com ele começa o final.
            var tmp = BuildNpc("Ren", "ren", RenX, Floor, 3.3f, null);
            var renRig = tmp.rig;
            renRig.facing = 1f;
            renRig.olhaNaConversa = false; // de costas para o Kai, olhando os trilhos
            Object.DestroyImmediate(tmp);
            renRig.transform.parent.SetParent(core.world, true);
            var renSpot = BuildSpot(spots, "ren", RenX, Floor, 3.4f, 1.7f);
            renSpot.transform.SetParent(renRig.transform.parent, true);

            // Souta: mais atrás, ainda com a arma do chefe na mão
            var souta = BuildNpc("Souta", "souta", SoutaX, Floor, 3.55f, player.transform);
            souta.firstTalk = Lines(
                ("souta", "Não chega perto, Kai. Eu ainda estou com a arma dele."),
                ("souta", "O Ren quis vir até aqui. Diz que precisa ver os trilhos. Vai lá, fala com ele."));
            souta.repeatTalk = Lines(("souta", "Quatro e dezessete, Kai. Você lembra o que isso quer dizer?"));
            var pistol = Load("Assets/Art/Cenario5", "pistola");

            // pistola caída; nas mãos ela já faz parte dos quadros dos personagens
            var gunFloor = SpriteObj("PistolaChao", core.world, pistol, new Vector3(X(SoutaX), Y(Floor) + 0.1f, 0f), 5);
            gunFloor.transform.localScale = Vector3.one * 0.5f;
            gunFloor.gameObject.SetActive(false);
            var kaiRig = core.kai;
            // clarão na ponta da arma do quadro de mira
            var muzzle = SpriteObj("Clarao", kaiRig.transform, Spr("brilho"), Vector3.zero, 15);
            muzzle.transform.localPosition = new Vector3(1.40f, 3.10f, 0f);
            muzzle.transform.localScale = Vector3.one * 0.5f;
            muzzle.color = new Color(1f, 0.9f, 0.6f, 0f);

            // a vala é funda: o fundo dos trilhos some no escuro (quem cai desaparece; só se ouve a batida)
            ConfigureSprite($"{Paineis4a7}/c7_vala_escura.png", new Vector2(0f, 1f), PixelArtImporter.Ppu, 1024, FilterMode.Point);
            var vala = SpriteObj("ValaEscura", core.world, Load(Paineis4a7, "c7_vala_escura"),
                new Vector3(668f / PixelArtImporter.Ppu, Y(Floor) + (PePx - 552f) / PixelArtImporter.Ppu, 0f), 30);

            // a frente do trem que sai do túnel (final em que o Kai se joga nos trilhos)
            ConfigureSprite($"{Paineis4a7}/c7_trem_frente.png", new Vector2(0.5f, 0f), PixelArtImporter.Ppu, 256, FilterMode.Point);
            var trainFront = SpriteObj("TremDeFrente", core.world, Load(Paineis4a7, "c7_trem_frente"), new Vector3(X(TunnelX), Y(600f), 0f), 50);
            trainFront.color = new Color(0.8f, 0.79f, 0.78f, 1f);
            var lights = new List<SpriteRenderer>();
            foreach (var lx in new[] { -56f, 55f })
            {
                var l = SpriteObj("FarolFrente", trainFront.transform, Spr("brilho"), Vector3.zero, 51);
                l.transform.localPosition = new Vector3(lx / PixelArtImporter.Ppu, 52f / PixelArtImporter.Ppu, 0f);
                l.transform.localScale = Vector3.one * 0.45f;
                l.color = new Color(1f, 0.96f, 0.85f, 0.9f);
                lights.Add(l);
            }
            trainFront.gameObject.SetActive(false);

            // sangue do Souta: poça que cresce embaixo dele e respingos no chão atrás
            foreach (var n in new[] { "c7_sangue_poca", "c7_sangue_respingo" })
                ConfigureSprite($"{Paineis4a7}/{n}.png", new Vector2(0.5f, 0.5f), PixelArtImporter.Ppu, 256, FilterMode.Point);
            var pool = SpriteObj("SangueDoSouta", core.world, Load(Paineis4a7, "c7_sangue_poca"), new Vector3(X(SoutaX), Y(Floor) + 0.05f, 0f), 4);
            pool.gameObject.SetActive(false);
            var spatter = SpriteObj("RespingosDoSouta", core.world, Load(Paineis4a7, "c7_sangue_respingo"), new Vector3(X(SoutaX), Y(Floor) + 0.1f, 0f), 4);
            spatter.gameObject.SetActive(false);

            // farol do trem (sai do túnel à direita)
            var head = SpriteObj("FarolDoTrem", core.world, Spr("brilho"), new Vector3(X(TunnelX), Y(609f), 0f), 60);
            head.color = new Color(1f, 0.96f, 0.85f, 0f);
            head.transform.localScale = Vector3.one * 0.3f;

            // interface do final: a escolha e o plantão do telejornal
            var canvas = core.ui.dialogue.transform.root;
            var choice = BuildChoice(canvas, fonts, core.voice);
            choice.anchor = player.transform; // as opções flutuam ao lado do Kai
            var news = BuildNews(canvas, fonts, core.voice);
            var fade = canvas.Find("Fade");
            var fim = canvas.Find("Fim");
            if (fade) choice.transform.SetSiblingIndex(fade.GetSiblingIndex());          // acima do diálogo, abaixo do preto
            if (fim) news.transform.SetSiblingIndex(fim.GetSiblingIndex());              // acima do preto, abaixo do "FIM"

            var dir = new GameObject("Diretor_Cenario7").AddComponent<Cenario7Director>();
            dir.player = player;
            dir.kaiRig = kaiRig;
            dir.cameraFollow = core.follow;
            dir.souta = souta;
            dir.renSpot = renSpot;
            dir.renRig = renRig;
            dir.gunOnFloor = gunFloor.gameObject;
            dir.muzzleFlash = muzzle;
            dir.headlight = head;
            dir.trainFront = trainFront.transform;
            dir.trainFrontY = Y(600f);
            dir.trainFrontEndY = Y(Floor) - 3.2f;
            dir.bloodPool = pool;
            dir.bloodSpatter = spatter;
            dir.trenchDark = vala;
            dir.edgeX = X(EdgeX);
            dir.floorY = Y(Floor);
            dir.pitY = Y(PitPy);
            dir.fallBounds = new Rect(0f, -extra, X(W) - 0.01f, ImgH / PPU + extra);
            dir.darkness = core.ui.darkness;
            dir.flash = core.ui.redTint;
            dir.choice = choice;
            dir.news = news;
            dir.ambience = ambience;
            dir.sfx = sfx;
            dir.trainRumble = rumble;
            dir.siren = siren;
            dir.pushClip = Clip("empurrao");
            dir.fallClip = Clip("queda_trilho");
            dir.hornClip = Clip("trem_buzina");
            dir.shotClip = Clip("tiro");
            dir.bodyClip = Clip("corpo_cai");
            dir.gunDropClip = Clip("cadeado_clique");
            dir.cockClip = Clip("arma_engatilha");
            dir.ambulanceClip = Clip("sirene_ambulancia");
            dir.policeClip = Clip("sirene_policia");
            dir.brakeClip = Clip("trem_freando");
            dir.pitImpactClip = Clip("c7_queda_vala");
            dir.crushClip = Clip("c7_esmagamento");
            dir.struggleClip = Clip("c7_luta");

            dir.intro = Lines(
                ("kai", "<i>A última plataforma. O nome está escrito na parede: ÚLTIMA LINHA.</i>"),
                ("kai", "<i>O relógio daqui também parou em quatro e dezessete.</i>"),
                ("kai", "<i>O Ren está na beira, olhando os trilhos. O Souta, logo atrás, com a arma do chefe.</i>"));
            dir.confront = Lines(
                ("ren", "Kai... você veio."),
                ("ren", "Lá embaixo é fundo. Se alguém cair, não levanta mais."),
                ("souta", "Fica onde está, Kai. A gente sabe o que você fez com o chefe."),
                ("souta", "São quatro e dezessete. Eu avisei. É a hora em que tudo acaba."),
                ("ren", "Eu só quero que isso termine. Do jeito que for."),
                ("kai", "<i>Eles nunca vão me deixar em paz. Eles estão sempre aqui. Sempre.</i>"),
                ("kai", "<i>Alguém tem que parar.</i>"));
            dir.choiceTitle = "";
            dir.choiceOptions = new[] { "Acabar com eles", "Acabar comigo" };
            dir.choiceNotes = new[] { "Ninguém mais vai me acusar.", "Isso termina comigo." };

            dir.othersStart = Lines(
                ("kai", "Chega."),
                ("ren", "Kai...? O que você vai...?"));
            dir.soutaPanic = Lines(
                ("souta", "REN!"),
                ("souta", "Você empurrou ele! Eu ouvi... eu ouvi ele bater lá embaixo!"),
                ("souta", "Fica longe de mim! Para, Kai! Eu atiro!"));
            dir.fightLines = Lines(("kai", "<i>Ele não vai atirar. Não em mim.</i>"));
            dir.fightWon = Lines(("souta", "Me solta! Kai, me solta!"));
            dir.pointGun = Lines(
                ("souta", "Kai, abaixa isso. Sou eu. Sou eu, Kai."),
                ("kai", "Eu sei."));
            dir.afterShot = Lines(
                ("kai", "<i>Silêncio. Finalmente, silêncio.</i>"),
                ("kai", "<i>...Por que ainda tem alguém gritando?</i>"));

            dir.selfStart = Lines(
                ("kai", "Não. Vocês não têm culpa de nada."),
                ("kai", "Quem tem que parar sou eu."),
                ("ren", "Kai? Kai, sai da beira!"));
            dir.selfLast = Lines(
                ("souta", "O trem! Kai, o trem está vindo!"),
                ("ren", "KAI!"),
                ("kai", "<i>Desculpa.</i>"));

            dir.policeChannel = "EDIÇÃO EXTRA  ·  QUARTA-FEIRA";
            dir.policeHeadline = "FUNCIONÁRIO É PRESO NO TRABALHO POR TENTATIVA DE MASSACRE";
            dir.policeBody =
                "Kai, 27 anos, funcionário da companhia de trens, foi preso na madrugada desta quarta-feira, por volta das 4h17, " +
                "na estação onde trabalhava. Segundo a polícia, ele empurrou um colega para a vala dos trilhos e, depois de lutar " +
                "com outro pela arma do antigo diretor da empresa, encontrado morto na semana passada, atirou nele.\n\n" +
                "Testemunhas contam que, antes do ataque, ele conversava sozinho com duas pessoas que chamava de Souta e Ren. " +
                "Os dois funcionários feridos foram levados ao hospital.";
            dir.policeTicker =
                "<b>ESTAÇÃO FECHADA</b>\nA última plataforma da linha segue interditada. A polícia investiga a ligação do caso " +
                "com a morte do diretor da empresa.";
            dir.policePhoto = Load(Art7, "jornal_foto_eles");
            dir.policeCaption = "A última plataforma da linha, onde o funcionário foi detido na madrugada.";
            dir.ambulanceChannel = "EDIÇÃO EXTRA  ·  QUARTA-FEIRA";
            dir.ambulanceHeadline = "TRABALHADOR TIRA A PRÓPRIA VIDA NO LOCAL DE TRABALHO";
            dir.ambulanceBody =
                "Um funcionário de 27 anos, identificado como Kai, morreu na madrugada desta quarta-feira, por volta das 4h17, " +
                "na estação de trem onde trabalhava. Segundo a família, ele tinha esquizofrenia e havia parado o tratamento.\n\n" +
                "As câmeras mostram que ele passou a noite andando sozinho pela estação, falando com duas pessoas: Souta e Ren. " +
                "Não existe nenhum registro de alguém com esses nomes. Para os médicos, eles nunca estiveram lá: eram parte da doença.";
            dir.ambulanceTicker =
                "<b>PRECISA CONVERSAR?</b>\nSe você ou alguém próximo está passando por um momento difícil, o CVV atende " +
                "24 horas, de graça, pelo 188. Esquizofrenia tem tratamento.";
            dir.ambulancePhoto = Load(Art7, "jornal_foto_mim");
            dir.ambulanceCaption = "A linha ficou fechada por três horas na madrugada.";

            SetSpeakers(core.ui.dialogue);
        }

        // ------------------------------------------------------------------ interface do final

        /// <summary>
        /// Escolha no estilo de Detroit: Become Human. Sem tela escura: um tom azul leve por cima da cena e, ao lado do Kai,
        /// uma lista curta com o texto em maiúsculas. A opção escolhida ganha um traço azul embaixo.
        /// </summary>
        static ChoiceUI BuildChoice(Transform root, Fonts fonts, AudioSource voice)
        {
            var rt = Stretch("Escolha", root);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            group.interactable = false;
            var ui = rt.gameObject.AddComponent<ChoiceUI>();

            var tintRt = Stretch("TomAzul", rt);
            Img(tintRt, new Color(0.03f, 0.1f, 0.2f, 0.32f));
            var tint = tintRt.gameObject.AddComponent<CanvasGroup>();
            tint.alpha = 0f;
            tint.blocksRaycasts = false;

            const int count = 2;
            const float rowH = 64f, rowGap = 12f, width = 520f;
            var list = new GameObject("Opcoes", typeof(RectTransform)) { layer = UILayer }.GetComponent<RectTransform>();
            list.SetParent(rt, false);
            list.anchorMin = list.anchorMax = new Vector2(0.5f, 0.5f);
            list.pivot = new Vector2(0f, 0.5f);
            list.sizeDelta = new Vector2(width, count * rowH + (count - 1) * rowGap);

            var title = Label("Titulo", list, "", fonts.sans, 16, new Color(0.6f, 0.8f, 1f, 0.7f), 0, -34, width, 24);
            title.characterSpacing = 18;

            var rows = new RectTransform[count];
            var icons = new Image[count];
            var labels = new TMP_Text[count];
            var bars = new Image[count];
            var lines = new Image[count];
            var buttons = new Button[count];
            for (int i = 0; i < count; i++)
            {
                var row = TopLeft("Opcao" + (i + 1), list, 0, i * (rowH + rowGap), width, rowH);
                row.gameObject.AddComponent<CanvasGroup>();
                rows[i] = row;

                bars[i] = Img(TopLeft("Faixa", row, 0, 4, width - 60, rowH - 8), new Color(0.02f, 0.04f, 0.07f, 0.32f));
                bars[i].raycastTarget = true;

                // sem os ícones do controle de PlayStation: só o texto, com o traço embaixo da escolhida
                var label = Label("Texto", row, "", fonts.sans, 30, new Color(0.82f, 0.88f, 0.94f, 0.62f), 24, 11, width - 90, 40);
                label.characterSpacing = 12;
                labels[i] = label;

                var line = Img(TopLeft("Traco", row, 74, rowH - 13, 300, 2), new Color(0.38f, 0.75f, 1f, 1f));
                line.rectTransform.localScale = new Vector3(0f, 1f, 1f);
                lines[i] = line;

                var btn = row.gameObject.AddComponent<Button>();
                btn.targetGraphic = bars[i];
                btn.transition = Selectable.Transition.None;
                btn.navigation = new Navigation { mode = Navigation.Mode.None };
                buttons[i] = btn;
            }
            var help = Label("Ajuda", list, "↑ ↓  ESCOLHER    ·    E  CONFIRMAR", fonts.sans, 14, new Color(0.75f, 0.85f, 0.95f, 1f),
                0, count * (rowH + rowGap) + 6, width, 22);
            help.characterSpacing = 10;
            help.alpha = 0f;

            ui.group = group;
            ui.tint = tint;
            ui.list = list;
            ui.title = title;
            ui.rows = rows;
            ui.icons = icons;
            ui.labels = labels;
            ui.bars = bars;
            ui.underlines = lines;
            ui.buttons = buttons;
            ui.hint = help;
            ui.sfx = voice;
            ui.appearClip = Clip("coracao");
            ui.moveClip = Clip("contagem_tic");
            ui.confirmClip = Clip("cadeado_clique");
            return ui;
        }

        /// <summary>A notícia dos finais como uma página de jornal em cima de uma mesa escura: nome do jornal, data,
        /// manchete, foto em meio-tom com legenda, o texto em duas colunas e um quadro no pé da página.</summary>
        static NewsView BuildNews(Transform root, Fonts fonts, AudioSource voice)
        {
            var rt = Stretch("Jornal", root);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var view = rt.gameObject.AddComponent<NewsView>();
            Img(Stretch("Mesa", rt), new Color(0.035f, 0.03f, 0.028f, 1f));
            var tinta = new Color(0.1f, 0.09f, 0.08f, 1f);
            var tintaClara = new Color(0.28f, 0.26f, 0.23f, 1f);

            // a página, um pouco torta, com sombra
            var sombra = TopLeft("Sombra", rt, 344, 46, 1260, 1020);
            Img(sombra, new Color(0f, 0f, 0f, 0.55f));
            sombra.localRotation = Quaternion.Euler(0f, 0f, -1.2f);
            var pagina = TopLeft("Pagina", rt, 330, 30, 1260, 1020);
            pagina.localRotation = Quaternion.Euler(0f, 0f, -1.2f);
            var papel = Img(pagina, Color.white);
            papel.sprite = Load(Art7, "jornal_papel");

            TextMeshProUGUI Texto(string nome, string texto, TMP_FontAsset fonte, float tamanho, Color cor, float x, float y, float w, float h)
            {
                var t = Label(nome, pagina, texto, fonte, 30, cor, x, y, w, h);
                t.fontSize = tamanho;
                return t;
            }
            void Linha(string nome, float x, float y, float w, float h) => Img(TopLeft(nome, pagina, x, y, w, h), tinta);

            // cabeçalho
            var nomeJornal = Texto("NomeDoJornal", "JORNAL DA MANHÃ", fonts.serif, 84, tinta, 40, 20, 1180, 100);
            nomeJornal.alignment = TextAlignmentOptions.Top;
            nomeJornal.fontStyle = FontStyles.Bold;
            nomeJornal.characterSpacing = 6;
            Linha("Fio1", 40, 120, 1180, 2);
            Linha("Fio2", 40, 126, 1180, 5);
            var data = Texto("Data", "", fonts.sans, 20, tinta, 40, 140, 800, 28);
            data.characterSpacing = 8;
            var numero = Texto("Numero", "ANO XLII  ·  Nº 4.217  ·  R$ 4,50", fonts.sans, 20, tinta, 720, 140, 500, 28);
            numero.alignment = TextAlignmentOptions.TopRight;
            numero.characterSpacing = 6;
            Linha("Fio3", 40, 174, 1180, 2);

            // manchete
            var manchete = Texto("Manchete", "", fonts.serif, 54, tinta, 40, 190, 1180, 150);
            manchete.fontStyle = FontStyles.Bold;
            manchete.textWrappingMode = TextWrappingModes.Normal;
            manchete.lineSpacing = -12;

            // foto e legenda
            var foto = Img(TopLeft("Foto", pagina, 40, 352, 600, 394), Color.white);
            foto.preserveAspect = true;
            Img(TopLeft("MolduraFoto", pagina, 40, 746, 600, 2), tinta);
            var legenda = Texto("Legenda", "", fonts.serif, 20, tintaClara, 40, 754, 600, 56);
            legenda.fontStyle = FontStyles.Italic;
            legenda.textWrappingMode = TextWrappingModes.Normal;

            // o texto em duas colunas
            var col1 = Texto("Coluna1", "", fonts.serif, 22, tinta, 670, 352, 265, 600);
            var col2 = Texto("Coluna2", "", fonts.serif, 22, tinta, 955, 352, 265, 600);
            foreach (var c in new[] { col1, col2 })
            {
                c.textWrappingMode = TextWrappingModes.Normal;
                c.alignment = TextAlignmentOptions.TopJustified;
                c.lineSpacing = 4;
            }
            Linha("FioColunas", 945, 352, 1, 600);

            // quadro no pé da página
            var quadro = TopLeft("Quadro", pagina, 40, 826, 600, 150);
            Img(quadro, tinta);
            Img(TopLeft("QuadroMiolo", quadro, 3, 3, 594, 144), new Color(0.9f, 0.87f, 0.79f, 1f));
            var quadroTexto = Label("QuadroTexto", quadro, "", fonts.sans, 30, tinta, 18, 14, 564, 124);
            quadroTexto.fontSize = 21;
            quadroTexto.textWrappingMode = TextWrappingModes.Normal;
            quadroTexto.lineSpacing = 6;
            Linha("FioRodape", 40, 990, 1180, 2);

            var hint = Label("Continuar", rt, "ESPAÇO  ›  continuar", fonts.sans, 18, Muted, 1605, 1010, 300, 28);
            hint.alignment = TextAlignmentOptions.TopRight;

            view.group = group;
            view.channel = data;
            view.headline = manchete;
            view.body = col1;
            view.bodyColumn2 = col2;
            view.photo = foto;
            view.caption = legenda;
            view.ticker = quadroTexto;
            view.tickerStrip = null;
            view.hint = hint;
            view.sfx = voice;
            view.stingClip = Clip("papel_virar");
            return view;
        }
    }
}
