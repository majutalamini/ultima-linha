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
        const int BuildVersion = 4; // aumente para forçar a cena a ser recriada automaticamente
        const string ScenePath = "Assets/Scenes/Cenario7.unity";
        const string Art7 = "Assets/Art/Cenario7";

        const float W = 3600f, Floor = 850f, LWall = 120f, RWall = 2660f;
        const float EdgeX = 2700f, RenX = 2540f, SoutaX = 2250f, TunnelX = 3300f;
        // o fundo é mais alto (1500 px): embaixo da plataforma fica a vala funda, para ver a queda do Ren
        const float ImgH7 = 1500f, PitPy = 1410f;
        static readonly (float x, float w)[] Tubes = { (500f, 240f), (1300f, 240f), (2100f, 240f), (2900f, 240f) };

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
                () => Load(Art7, "cenario7_fundo") != null, Populate);
        }

        // ------------------------------------------------------------------

        static void Populate(GameObject playerPrefab)
        {
            var fonts = LoadFonts();
            var core = BuildCore(playerPrefab, Load(Art7, "cenario7_fundo"), W, Floor, LWall, RWall, 330f, withLock: false, fonts);
            var player = core.player;
            var sfx = core.sfx;

            // o fundo desce além da tela normal: a parte de cima continua no mesmo lugar, a vala fica abaixo do zero
            float extra = (ImgH7 - ImgH) / PPU;
            var bg = core.world.Find("Fundo");
            if (bg) bg.position = new Vector3(0f, -extra, 0f);
            core.follow.bounds = new Rect(0f, 0f, X(W) - 0.01f, ImgH / PPU);

            var ambience = Source(core.audioRoot, "AmbientePlataforma", Clip("ambiente_plataforma"), loop: true);
            var rumble = Source(core.audioRoot, "Trem", Clip("trem_aproxima"), loop: false);
            var siren = Source(core.audioRoot, "Sirene", null, loop: false);

            BuildTubes(core.world, Tubes, 111f, i => i == 1);

            var spots = new GameObject("Lugares").transform;
            spots.SetParent(core.world, false);

            // Ren: na beira da plataforma, de costas para o Kai, olhando os trilhos. Falar com ele começa o final.
            var tmp = BuildNpc("Ren", "ren", RenX, Floor, 3.3f, null);
            var renRig = tmp.rig;
            renRig.facing = 1f;
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

            // farol do trem (sai do túnel à direita)
            var head = SpriteObj("FarolDoTrem", core.world, Spr("brilho"), new Vector3(X(TunnelX), Y(560f), 0f), 60);
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
            dir.edgeX = X(EdgeX);
            dir.floorY = Y(Floor);
            dir.pitY = Y(PitPy);
            dir.fallBounds = new Rect(0f, -extra, X(W) - 0.01f, ImgH7 / PPU);
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
                ("souta", "Você empurrou ele! Ele bateu lá embaixo, ele não está se mexendo!"),
                ("souta", "Fica longe de mim! Fica longe!"));
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

            dir.policeChannel = "JORNAL DA NOITE  ·  PLANTÃO";
            dir.policeHeadline = "FUNCIONÁRIO É PRESO NO TRABALHO POR TENTATIVA DE MASSACRE";
            dir.policeBody =
                "Kai, 27 anos, funcionário da companhia de trens, foi preso na madrugada desta quarta-feira, por volta das 4h17, " +
                "na estação onde trabalhava. Segundo a polícia, ele empurrou um colega para a vala dos trilhos e atirou em outro " +
                "com a arma do antigo diretor da empresa, encontrado morto na semana passada.\n\n" +
                "Testemunhas contam que, antes do ataque, ele conversava sozinho com duas pessoas que chamava de Souta e Ren. " +
                "Os dois funcionários feridos foram levados ao hospital.";
            dir.policeTicker =
                "URGENTE  ·  Suspeito foi detido na última plataforma da linha  ·  Estação permanece fechada  ·  " +
                "Polícia investiga ligação com a morte do diretor da empresa  ·  ";
            dir.ambulanceChannel = "JORNAL DA NOITE  ·  PLANTÃO";
            dir.ambulanceHeadline = "TRABALHADOR TIRA A PRÓPRIA VIDA NO LOCAL DE TRABALHO";
            dir.ambulanceBody =
                "Um funcionário de 27 anos, identificado como Kai, morreu na madrugada desta quarta-feira, por volta das 4h17, " +
                "na estação de trem onde trabalhava. Segundo a família, ele tinha esquizofrenia e havia parado o tratamento.\n\n" +
                "As câmeras mostram que ele passou a noite andando sozinho pela estação, falando com duas pessoas: Souta e Ren. " +
                "Não existe nenhum registro de alguém com esses nomes. Para os médicos, eles nunca estiveram lá: eram parte da doença.";
            dir.ambulanceTicker =
                "SAÚDE MENTAL  ·  Se você ou alguém próximo precisa conversar, ligue 188 (CVV), 24 horas, de graça  ·  " +
                "Esquizofrenia tem tratamento  ·  ";

            SetSpeakers(core.ui.dialogue);
        }

        // ------------------------------------------------------------------ interface do final

        /// <summary>
        /// Escolha no estilo de Detroit: Become Human. Sem tela escura: um tom azul leve por cima da cena e, ao lado do Kai,
        /// uma lista curta com um ícone de botão (△ ○) e o texto em maiúsculas. A opção escolhida ganha um traço azul embaixo.
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

            string[] iconNames = { "icone_triangulo", "icone_circulo", "icone_quadrado", "icone_xis" };
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

                var icon = Img(TopLeft("Icone", row, 10, 9, 46, 46), Color.white);
                icon.sprite = Load(Art7, iconNames[i]);
                icon.preserveAspect = true;
                icons[i] = icon;

                var label = Label("Texto", row, "", fonts.sans, 30, new Color(0.82f, 0.88f, 0.94f, 0.62f), 74, 11, width - 140, 40);
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

        static NewsView BuildNews(Transform root, Fonts fonts, AudioSource voice)
        {
            var rt = Stretch("Plantao", root);
            var group = rt.gameObject.AddComponent<CanvasGroup>();
            group.alpha = 0f;
            group.blocksRaycasts = false;
            var view = rt.gameObject.AddComponent<NewsView>();
            Img(Stretch("Fundo", rt), new Color(0.025f, 0.03f, 0.045f, 1f));
            var red = Hex("C0281E");

            // barra de cima: canal, "ao vivo" e a hora
            Img(TopLeft("BarraTopo", rt, 0, 0, 1920, 90), new Color(0.06f, 0.07f, 0.1f, 1f));
            var channel = Label("Canal", rt, "", fonts.sans, 24, Cream, 120, 30, 1100, 34);
            channel.characterSpacing = 14;
            var dotRt = TopLeft("AoVivo", rt, 1470, 34, 22, 22);
            Img(dotRt, red);
            var dot = dotRt.gameObject.AddComponent<CanvasGroup>();
            var live = Label("AoVivoTexto", rt, "AO VIVO", fonts.sans, 22, Cream, 1504, 30, 200, 34);
            live.characterSpacing = 12;
            var clock = Label("Hora", rt, "04:17", fonts.sans, 26, Cream, 1660, 28, 160, 34);
            clock.alignment = TextAlignmentOptions.TopRight;

            // faixa "PLANTÃO", manchete e texto
            Img(TopLeft("Faixa", rt, 120, 190, 330, 66), red);
            var tag = Label("FaixaTexto", rt, "PLANTÃO", fonts.sans, 36, Color.white, 120, 198, 330, 50);
            tag.alignment = TextAlignmentOptions.Top;
            tag.characterSpacing = 20;
            var headline = Label("Manchete", rt, "", fonts.serif, 66, Cream, 120, 290, 1680, 250);
            headline.textWrappingMode = TextWrappingModes.Normal;
            headline.lineSpacing = -10;
            Img(TopLeft("Traco", rt, 120, 548, 1680, 2), Rule);
            var body = Label("Texto", rt, "", fonts.serif, 32, Hex("C9C2B5"), 120, 580, 1450, 330);
            body.textWrappingMode = TextWrappingModes.Normal;
            body.lineSpacing = 8;

            // letreiro correndo embaixo
            Img(TopLeft("LetreiroEtiqueta", rt, 0, 980, 220, 64), red);
            var lbl = Label("LetreiroEtiquetaTexto", rt, "URGENTE", fonts.sans, 26, Color.white, 0, 994, 220, 40);
            lbl.alignment = TextAlignmentOptions.Top;
            lbl.characterSpacing = 10;
            var strip = TopLeft("Letreiro", rt, 220, 980, 1700, 64);
            Img(strip, new Color(0.9f, 0.88f, 0.84f, 1f));
            strip.gameObject.AddComponent<RectMask2D>();
            var ticker = Label("LetreiroTexto", strip, "", fonts.sans, 28, Hex("1A1A1A"), 0, 14, 6000, 40);
            ticker.characterSpacing = 4;

            var hint = Label("Continuar", rt, "E  ›  continuar", fonts.sans, 18, Muted, 1500, 930, 300, 28);
            hint.alignment = TextAlignmentOptions.TopRight;

            view.group = group;
            view.channel = channel;
            view.headline = headline;
            view.body = body;
            view.ticker = ticker;
            view.tickerStrip = strip;
            view.hint = hint;
            view.clock = clock;
            view.liveDot = dot;
            view.sfx = voice;
            view.stingClip = Clip("plantao");
            return view;
        }
    }
}
