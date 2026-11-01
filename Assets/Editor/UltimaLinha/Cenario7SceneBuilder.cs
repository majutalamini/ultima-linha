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
    /// Monta a cena Assets/Scenes/Cenario7.unity (a plataforma final: o empurrão, o tiro e os recortes de jornal).
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Cenário 7.
    /// As medidas vêm de Ferramentas/Cenarios367/cenarios_finais.py (mesmos números).
    /// </summary>
    [InitializeOnLoad]
    public static class Cenario7SceneBuilder
    {
        const int BuildVersion = 1; // aumente para forçar a cena a ser recriada automaticamente
        const string ScenePath = "Assets/Scenes/Cenario7.unity";
        const string Art7 = "Assets/Art/Cenario7";

        const float W = 3600f, Floor = 850f, LWall = 120f, RWall = 2660f;
        const float EdgeX = 2700f, SoutaX = 2540f, RenX = 2250f, TunnelX = 3300f;
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

            var ambience = Source(core.audioRoot, "AmbientePlataforma", Clip("ambiente_plataforma"), loop: true);
            var rumble = Source(core.audioRoot, "Trem", Clip("trem_aproxima"), loop: false);

            BuildTubes(core.world, Tubes, 111f, i => i == 1);

            var spots = new GameObject("Lugares").transform;
            spots.SetParent(core.world, false);

            // Souta: de costas para o Kai, olhando os trilhos. O lugar de interação começa o final.
            var soutaSpot = BuildSpot(spots, "souta", SoutaX, Floor, 3.55f, 1.7f);
            var tmp = BuildNpc("Souta", "souta", SoutaX, Floor, 3.55f, null);
            var rig = tmp.rig;
            rig.facing = 1f;
            Object.DestroyImmediate(tmp);
            rig.transform.parent.SetParent(core.world, true);
            soutaSpot.transform.SetParent(rig.transform.parent, true);
            var ren = BuildNpc("Ren", "ren", RenX, Floor, 3.3f, player.transform);
            ren.firstTalk = Lines(
                ("ren", "Kai... o Souta está tão estranho. Ele não fala desde que a gente desceu."),
                ("ren", "Ele está parado ali, olhando os trilhos."));
            ren.repeatTalk = Lines(("ren", "Fala com ele, Kai. Por favor."));

            // pistola caída e pistola na mão do Kai
            var gunFloor = SpriteObj("PistolaChao", core.world, Load("Assets/Art/Cenario5", "pistola"), new Vector3(X(SoutaX), Y(Floor) + 0.12f, 0f), 5);
            gunFloor.transform.localScale = Vector3.one * 0.55f;
            gunFloor.gameObject.SetActive(false);
            var kaiGun = SpriteObj("PistolaKai", player.transform, Load("Assets/Art/Cenario5", "pistola"), Vector3.zero, 12);
            kaiGun.transform.localPosition = new Vector3(0.62f, -0.2f, 0f);
            kaiGun.transform.localScale = Vector3.one * 0.55f;
            kaiGun.gameObject.SetActive(false);

            // farol do trem (sai do túnel à direita)
            var head = SpriteObj("FarolDoTrem", core.world, Spr("brilho"), new Vector3(X(TunnelX), Y(560f), 0f), 60);
            head.color = new Color(1f, 0.96f, 0.85f, 0f);
            head.transform.localScale = Vector3.one * 0.3f;

            var dir = new GameObject("Diretor_Cenario7").AddComponent<Cenario7Director>();
            dir.player = player;
            dir.cameraFollow = core.follow;
            dir.ren = ren;
            dir.soutaSpot = soutaSpot;
            dir.soutaRig = rig;
            dir.gunOnFloor = gunFloor.gameObject;
            dir.kaiGun = kaiGun.gameObject;
            dir.headlight = head;
            dir.edgeX = X(EdgeX);
            dir.railY = Y(895f) + 0.3f;
            dir.floorY = Y(Floor);
            dir.darkness = core.ui.darkness;
            dir.flash = core.ui.redTint;
            dir.ambience = ambience;
            dir.sfx = sfx;
            dir.trainRumble = rumble;
            dir.pushClip = Clip("empurrao");
            dir.fallClip = Clip("queda_trilho");
            dir.hornClip = Clip("trem_buzina");
            dir.shotClip = Clip("tiro");
            dir.paperClip = Clip("papel_virar");

            dir.intro = Lines(
                ("kai", "<i>A última plataforma. O nome está escrito na parede: ÚLTIMA LINHA.</i>"),
                ("kai", "<i>O Souta está na beira, olhando os trilhos. O Ren está logo atrás dele.</i>"),
                ("kai", "<i>Eu sei o que eu preciso fazer.</i>"));
            dir.beforePush = Lines(
                ("kai", "<i>Foi você que me trouxe até aqui. Foi você que me acusou o tempo todo.</i>"),
                ("souta", "Kai? O que foi?"),
                ("kai", "Chega."));
            dir.renPanic = Lines(
                ("ren", "Souta! SOUTA!"),
                ("ren", "Kai, o que você fez?! Ele está nos trilhos! O trem está vindo!"),
                ("ren", "Você enlouqueceu... fica longe de mim!"));
            dir.pointGun = Lines(
                ("kai", "Você também, Ren."),
                ("ren", "Por favor... abaixa isso. Eu sou seu amigo."),
                ("kai", "<i>Amigo.</i>"));
            dir.newsTitles = new[] { "JORNAL DA MANHÃ", "RELATÓRIO CLÍNICO", "JORNAL DA NOITE" };
            dir.newsBodies = new[]
            {
                "<b>PACIENTE FOGE DE CENTRO DE REABILITAÇÃO</b>\n\nUm paciente de 27 anos, identificado como <b>Kai</b>, deixou o centro de reabilitação psiquiátrica durante a madrugada. " +
                "Ele cumpria tratamento depois da morte do diretor da empresa onde trabalhava, crime pelo qual foi considerado incapaz de responder.\n\n" +
                "O diagnóstico é de <b>esquizofrenia</b>. A família pede ajuda para localizá-lo.",
                "<b>CENTRO DE REABILITAÇÃO — PRONTUÁRIO</b>\n\nO paciente relata dois amigos que o acompanham: <b>Souta</b>, que decide e acusa, e <b>Ren</b>, que teme e foge.\n\n" +
                "Não existe registro de nenhuma pessoa com esses nomes. Nunca houve visitas. São duas <b>personalidades</b> criadas pelo próprio paciente para suportar o que ele fez.\n\n" +
                "Ele chama o corredor da clínica de \"o vagão\".",
                "<b>HOMEM É ENCONTRADO SOZINHO NA ESTAÇÃO</b>\n\nSegurança da estação localizou, na última plataforma da linha, o paciente que havia fugido. Segundo as câmeras, ele falava e gesticulava sozinho, como se conversasse com duas pessoas.\n\n" +
                "Nenhuma outra pessoa foi encontrada nos trilhos ou na plataforma.",
            };
            dir.newsFooters = new[] { "Recorte guardado no bolso do casaco.", "Papel dobrado em quatro, com o nome do paciente.", "A última notícia." };
            dir.afterNews = Lines(
                ("kai", "Eu nunca saí daquele lugar."),
                ("kai", "<i>Eles nunca estiveram aqui. Só eu.</i>"));

            SetSpeakers(core.ui.dialogue);
        }
    }
}
