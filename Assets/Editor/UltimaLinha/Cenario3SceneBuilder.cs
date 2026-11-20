using System.Collections.Generic;
using System.IO;
using System.Linq;
using Platformer.Mechanics;
using TMPro;
using UltimaLinha.Cenario1;
using UltimaLinha.Cenario2;
using UltimaLinha.Salas;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using static UltimaLinha.EditorTools.Cenario1SceneBuilder;
using static UltimaLinha.EditorTools.SalasBuild;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Monta a cena Assets/Scenes/Cenario3.unity: a estação que se via pela porta do fim do Cenário 2, agora por dentro,
    /// na grade dos personagens (58 px por unidade, câmera 4,655). Plano em Ferramentas/Cenario3/PLANO.md.
    /// - À esquerda, a ponta do último vagão com a porta aberta (kit dos Cenários 0/1); o Kai sai por ela.
    /// Duas partes, um puzzle em cada:
    /// - Plataforma (a parede de estacao.png): bilhete; o portão de serviço com código, com o quadro de partidas logo ao lado.
    /// - Saguão: bilheteria (comunicado), a moeda no chão, a máquina (dá a única peça) e a porta da administração com o
    ///   painel de desvios preso ao batente; a porta abre quando a linha do painel fica completa.
    /// - Roda sozinho quando o projeto recompila, se a cena não existir ou estiver numa versão antiga.
    /// - Para refazer do zero: menu  Última Linha > Recriar Cenário 3.
    /// Peças em Assets/Art/Cenario3/Objetos (Ferramentas/Cenario3/objetos_arte.py; as medidas em px vêm de medidas.json).
    /// </summary>
    [InitializeOnLoad]
    public static class Cenario3SceneBuilder
    {
        const int BuildVersion = 9; // aumente para forçar a cena a ser recriada automaticamente
        const string ScenePath = "Assets/Scenes/Cenario3.unity";
        const string Obj3 = "Assets/Art/Cenario3/Objetos/";
        const string MapaC2 = "Assets/Art/Cenario2/Objetos/mapa_linha.png";

        static float A(float px) => px / PixelArtImporter.Ppu;
        static float Chao => ChaoMundo;
        // a parede da estação tem a linha dos pés 140 px acima da borda de baixo (como no fim do Cenário 2)
        static float ParedeY => Chao - A(140f);
        const float ParedeTrem = 6f; // onde termina o interior do último vagão (a ponta com a porta fica à direita)
        static float Plataforma0 => ParedeTrem + U(AntW); // fim da plataforma da ponta = começo da estação
        // no Cenário 2, a estação fica com o centro 8 u depois da plataforma da ponta: mesma posição aqui
        static float Parede0 => Plataforma0 + 8f - A(1255f) / 2f;

        // medidas.json (px na grade, do canto de baixo à esquerda de cada peça)
        const float PortaW = 155f, PortaChao = 0.4f;
        static readonly Vector4 PortaFolhas = new Vector4(77.2f, 125.2f, 131.9f, 247.8f), FolhaEsq = new Vector4(44.1f, 125.2f, 65.9f, 247.8f),
            FolhaDir = new Vector4(110.1f, 125.2f, 66.1f, 247.8f);
        static readonly Vector4 PortaoGrade = new Vector4(85.6f, 117.3f, 132.5f, 234.1f);
        const float PortaoChao = 0.2f, PortaoPoste = 170f; // o poste do portão fica em cima da emenda das paredes
        const float MaquinaW = 102f;
        static readonly Vector2 MaquinaBandeja = new Vector2(42.3f, 44.5f), BilheteriaPapel = new Vector2(64.8f, 149.9f);
        static readonly Vector2 PainelLampada = new Vector2(39.5f, 108.8f);
        const float PainelW = 79f;
        // estacao.png (px, de cima): banco, placa e moldura do cartaz
        const float PlacaX = 627f, PlacaY = 175f;

        internal const float SaguaoSobe = 35f; // linha dos pés na parede do saguão: 545 px de cima (o corredor do portão usa a mesma)
        const int ParedeOrder = -110, FundoOrder = -60, ObjOrder = -40;

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
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { Obj3.TrimEnd('/') }).Select(AssetDatabase.GUIDToAssetPath))
                ConfigureSprite(path, Vector2.zero, PixelArtImporter.Ppu, 4096, FilterMode.Point);
            ConfigureSprite(MapaC2, new Vector2(0.5f, 0.5f), PixelArtImporter.Ppu, 4096, FilterMode.Point);
            return Run(interactive, ScenePath, "Cenário 3", new[] { "Cenario2", "Cenario1", "MainMenu" },
                () => AssetDatabase.LoadAssetAtPath<Sprite>(Obj3 + "estacao_inicio.png") != null, Populate);
        }

        static Sprite O(string name)
        {
            var s = AssetDatabase.LoadAssetAtPath<Sprite>(Obj3 + name + ".png");
            if (!s) Debug.LogWarning("[Última Linha] Objeto do Cenário 3 não encontrado: " + name + " (rode Ferramentas/Cenario3/objetos_arte.py)");
            return s;
        }

        static float W(Sprite s) => s ? s.bounds.size.x : 0f;

        // ------------------------------------------------------------------

        static void Populate(GameObject playerPrefab)
        {
            var fonts = LoadFonts();

            // ---------- Som ----------
            var audioRoot = new GameObject("Som");
            var sfx = Source(audioRoot, "Efeitos", null);
            var voice = Source(audioRoot, "VozDialogo", null);
            voice.volume = 0.5f;
            var ambience = Source(audioRoot, "AmbienteEstacao", Clip("ambiente_estacao"), loop: true);

            var world = new GameObject("Cenario").transform;
            var solids = new GameObject("Colisores").transform;
            solids.SetParent(world, false);

            // ---------- Paredes: estacao.png (a plataforma inteira) e o saguão duas vezes ----------
            var paredes = new[] { "estacao_inicio", "parede_saguao", "parede_saguao" };
            var inicio = new float[paredes.Length + 1];
            inicio[0] = Parede0;
            for (int i = 0; i < paredes.Length; i++)
            {
                var spr = O(paredes[i]);
                // o saguão sobe 35 px: a base da parede fica atrás do Kai e aparece o chão molhado entre ele e a parede (como
                // na plataforma); com os pés na base da parede ele parecia pisar no vazio
                float y = paredes[i] == "parede_saguao" ? ParedeY + A(SaguaoSobe) : ParedeY;
                SpriteObj("Parede_" + paredes[i], world, spr, new Vector3(inicio[i], y, 0f), ParedeOrder);
                inicio[i + 1] = inicio[i] + W(spr);
            }
            float w0 = inicio[0], s0 = inicio[1]; // s0: emenda entre a plataforma e o saguão (o portão)
            float Ex(float px) => w0 + A(px);                      // px de estacao.png em x do mundo
            float Ey(float pyTopo) => ParedeY + A(540f - pyTopo);

            // ---------- O último vagão, com a porta do fim aberta ----------
            SpriteObj("Vagao", world, AssetDatabase.LoadAssetAtPath<Sprite>(FundoPixel), new Vector3(ParedeTrem - XFimVagao, 0f, 0f), -100);
            var portaTrem = BuildPortaPonta(world, ParedeTrem, false, sfx);
            portaTrem.AbertaJa();

            // ---------- Saguão ----------
            // um puzzle só: moeda no chão perto da bilheteria -> máquina (a peça) -> painel colado no batente da porta
            float bilheteriaX = s0 + 1.6f, moedaX = s0 + 7.0f, maquinaX = s0 + 9.0f;
            float porta0 = s0 + 13.5f, porta1 = porta0 + A(PortaW), painelX = porta0 - A(PainelW / 2f) - 0.05f;
            float fim = porta1 + 3f;

            // ---------- Colisores ----------
            Box("Chao", solids, (ParedeTrem - 12f + fim + 2f) / 2f, Chao - 0.5f, fim + 14f - ParedeTrem, 1f, oneWay: false);
            Box("Teto", solids, (ParedeTrem + fim) / 2f, 2f * CameraSize + 0.5f, fim - ParedeTrem + 20f, 1f, oneWay: false);
            Box("ParedeEsquerda", solids, ParedeTrem - 8.5f, 6f, 1f, 14f, oneWay: false);
            Box("ParedeDireita", solids, porta1 + 0.9f, 6f, 1f, 14f, oneWay: false);

            // ---------- Jogador (Kai): sai da porta do fim do trem ----------
            float spawnY = Chao + ColliderHeight / 2f + 0.02f;
            var spawn = new GameObject("SpawnPoint");
            spawn.transform.position = new Vector3(Plataforma0 + 0.9f, spawnY, 0f);
            var playerGo = (GameObject)PrefabUtility.InstantiatePrefab(playerPrefab);
            playerGo.name = "Kai (Player)";
            playerGo.transform.position = spawn.transform.position;
            var player = playerGo.GetComponent<PlayerController>();
            player.maxSpeed = 2.8f;
            player.runMultiplier = 2.06f; // corrida a ~5,8 u/s
            player.jumpTakeOffSpeed = 7.2f;
            player.gravityModifier = 1.25f;
            player.jumpAudio = null;
            PrefabUtility.RecordPrefabInstancePropertyModifications(player);
            var box = playerGo.GetComponent<BoxCollider2D>();
            if (box)
            {
                box.size = new Vector2(0.9f, ColliderHeight);
                box.offset = Vector2.zero;
                PrefabUtility.RecordPrefabInstancePropertyModifications(box);
            }
            var mainSr = playerGo.GetComponent<SpriteRenderer>();
            if (mainSr)
            {
                mainSr.enabled = false;
                PrefabUtility.RecordPrefabInstancePropertyModifications(mainSr);
            }
            var kaiRig = BuildPersonagem("kai", playerGo.transform, new Vector3(0f, -ColliderHeight / 2f, 0f), 10);
            kaiRig.mover = player;
            kaiRig.flipSource = mainSr;
            kaiRig.jumpSpeed = player.jumpTakeOffSpeed * 1.5f;
            NoVagao(kaiRig); // o chão da estação também está molhado: mesma sombra e reflexo

            var prompt = PromptE();
            var interactor = playerGo.AddComponent<PlayerInteractor>();
            interactor.player = player;
            interactor.prompt = prompt;

            var gc = new GameObject("GameController").AddComponent<GameController>();
            gc.model.player = player;
            gc.model.spawnPoint = spawn.transform;
            gc.model.virtualCamera = null;
            gc.model.jumpModifier = 1.5f;
            gc.model.jumpDeceleration = 0.5f;

            // ---------- Câmera ----------
            var camGo = new GameObject("Main Camera") { tag = "MainCamera" };
            var cam = camGo.AddComponent<Camera>();
            cam.orthographic = true;
            cam.orthographicSize = CameraSize;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = Color.black;
            camGo.transform.position = new Vector3(spawn.transform.position.x + 1f, CameraSize, -10f);
            camGo.AddComponent<AudioListener>();
            var follow = camGo.AddComponent<CameraFollow2D>();
            follow.target = playerGo.transform;
            // à esquerda, o mesmo pedaço do vagão que aparecia no último quadro do Cenário 2
            follow.bounds = Rect.MinMaxRect(ParedeTrem - 3.5f, 0f, fim, 2f * CameraSize);

            // ---------- Interface ----------
            var ui = BuildUi(fonts, voice, withLock: true);
            var titulo = ui.lockUi.transform.Find("Painel/Titulo")?.GetComponent<TMP_Text>();
            if (titulo) titulo.text = "PORTÃO DE SERVIÇO";
            SetSpeakers(ui.dialogue);
            var es = new GameObject("EventSystem");
            es.AddComponent<UnityEngine.EventSystems.EventSystem>();
            es.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>().AssignDefaultActions();

            var dir = new GameObject("Diretor_Cenario3").AddComponent<Cenario3Director>();
            dir.player = player;
            dir.cameraFollow = follow;
            dir.portaTrem = portaTrem;
            dir.ambience = ambience;
            dir.sfx = sfx;
            dir.noteClip = Clip("papel");
            dir.pieceClip = Clip("alavanca_pega");
            dir.coinClip = Clip("chave_pega");
            dir.portaoClip = Clip1("porta_destrava");
            dir.lockUI = ui.lockUi;
            var papel = Clip("papel");
            var glows = new List<SpriteRenderer>();
            SpriteRenderer Brilho(Transform pai, Vector3 p, float s)
            {
                var g = SpriteObj("Brilho", pai, Spr("brilho"), p, ObjOrder + 5);
                g.color = new Color(1f, 0.86f, 0.55f, 0.3f);
                g.transform.localScale = Vector3.one * s;
                return g;
            }

            // ---------- Plataforma: só o bilhete para ler; placa e cartaz são cenário ----------
            var plat = new GameObject("Plataforma").transform;
            plat.SetParent(world, false);
            var bilheteX = Plataforma0 + 2.4f;
            SpriteObj("Bilhete", plat, O("bilhete"), new Vector3(bilheteX - A(11f), Chao - 0.02f, 0f), ObjOrder);
            var bilhete = Spot(plat, "bilhete", bilheteX, Chao, 1.3f, 1.0f, sfx, papel);
            bilhete.docTitle = "BILHETE DE EMBARQUE";
            bilhete.docBody = "<b>ÚLTIMA LINHA</b>\nDestino: plataforma final\nValidade: uma viagem, sem volta.\n\n" +
                              "<i>No verso, à mão:</i>\nquem quiser sair vai ter que ligar a linha sozinho.";
            bilhete.docFooter = "Amassado, caído na porta do trem.";
            bilhete.first = Lines(("kai", "<i>\"Ligar a linha sozinho.\" Que linha?</i>"));
            bilhete.again = Lines(("kai", "<i>O bilhete. Sem volta.</i>"));

            // a placa escura de estacao.png: o nome quase apagado (sem E: só cenário)
            var placaTexto = new GameObject("PlacaTerminal").AddComponent<TextMeshPro>();
            placaTexto.transform.SetParent(plat, false);
            placaTexto.transform.position = new Vector3(Ex(PlacaX), Ey(PlacaY), 0f);
            placaTexto.font = fonts.sans;
            placaTexto.text = "TERMINAL";
            placaTexto.fontSize = 5.2f;
            placaTexto.characterSpacing = 18f;
            placaTexto.alignment = TextAlignmentOptions.Center;
            placaTexto.rectTransform.sizeDelta = new Vector2(6f, 1.2f);
            placaTexto.color = new Color(0.62f, 0.58f, 0.5f, 0.32f);
            placaTexto.GetComponent<MeshRenderer>().sortingOrder = FundoOrder;

            // sem o cartaz da clínica: a moldura vazia de estacao.png fica atrás do portão agora (ele cobria a grade)

            // ---------- O portão de serviço (o poste em cima da emenda das paredes), com o quadro logo ao lado ----------
            var portao0 = s0 - A(PortaoPoste);
            float portaoY = Chao - A(PortaoChao);
            SpriteObj("PortaoBatente", plat, O("portao_batente"), new Vector3(portao0, portaoY, 0f), ObjOrder + 1);
            var gradePos = new Vector3(portao0 + A(PortaoGrade.x - PortaoGrade.z / 2f), portaoY + A(PortaoGrade.y - PortaoGrade.w / 2f), 0f);
            // o corredor escuro atrás da grade: o portão é uma passagem, não grade pintada em cima dos azulejos
            SpriteObj("PortaoVao", plat, O("portao_vao"), gradePos + new Vector3(-A(2f), 0f, 0f), ObjOrder - 1);
            var grade = SpriteObj("PortaoGrade", plat, O("portao_grade"), gradePos, ObjOrder);
            float gradeX = portao0 + A(PortaoGrade.x);
            Box("PortaoBloqueio", solids, gradeX, Chao + 3f, 0.4f, 6f, oneWay: false);
            var bloqueio = solids.Find("PortaoBloqueio").GetComponent<BoxCollider2D>();

            // o quadro de partidas pendurado logo à esquerda do portão, à vista junto com o teclado
            var quadro = O("quadro_partidas");
            var quadroX = portao0 - W(quadro) / 2f - 0.4f;
            SpriteObj("QuadroPartidas", plat, quadro, new Vector3(quadroX - W(quadro) / 2f, Chao + A(238f), 0f), ObjOrder);
            var partidas = Spot(plat, "partidas", quadroX, Chao, 5.6f, 1.4f, sfx, papel);
            partidas.docTitle = "QUADRO DE PARTIDAS";
            partidas.docBody = "<b>PARTIDAS — LINHA ÚNICA</b>\n\n22:10   TERMINAL\n23:00   TERMINAL\n<b>23:50   TERMINAL   último trem</b>\n\n" +
                               "<i>Num papel colado embaixo, à mão, em vermelho, numa letra torta e apertada:</i>\n" +
                               "04:17 — ÚLTIMA LINHA. Não embarque.";
            partidas.docFooter = "O papel foi colado depois. O quadro é impresso.";
            partidas.first = Lines(("kai", "<i>Último trem: vinte e três e cinquenta.</i>"), ("kai", "<i>E embaixo, à mão... quatro e dezessete. De novo.</i>"));
            partidas.again = Lines(("kai", "<i>Último trem: vinte e três e cinquenta. O resto foi escrito à mão.</i>"));

            var portao = Spot(plat, "portao", gradeX - 0.9f, Chao, 4.8f, 1.3f, sfx, papel);
            portao.docTitle = "PORTÃO DE SERVIÇO";
            portao.docBody = "Fecha depois da <b>última partida</b>.\n\nCódigo: o horário da última partida, sem os dois pontos.";
            portao.docFooter = "Plaquinha parafusada em cima do teclado.";
            dir.portao = portao;
            dir.portaoGrade = grade.transform;
            dir.portaoBloqueio = bloqueio;
            dir.portaoCorre = A(PortaoGrade.z) + 0.1f;

            // ---------- Saguão: o comunicado, a moeda no chão e a máquina ----------
            var sag = new GameObject("Saguao").transform;
            sag.SetParent(world, false);
            SpriteObj("Bilheteria", sag, O("bilheteria"), new Vector3(bilheteriaX, Chao, 0f), ObjOrder);
            var comunicado = Spot(sag, "comunicado", bilheteriaX + A(BilheteriaPapel.x), Chao, 3.5f, 1.4f, sfx, papel);
            comunicado.docTitle = "COMUNICADO INTERNO";
            comunicado.docBody = "<b>Reestruturação do setor de operações.</b>\n\n" +
                                 "Os funcionários abaixo devem comparecer à <b>sala 704</b> (7º andar), às 9h, para tratar do seu desligamento:\n\n" +
                                 "A. Hayashi\nM. Ono\n<b>K.</b>\nT. Sato\n\n— A Direção";
            comunicado.docFooter = "Colado por dentro do vidro da bilheteria. Alguém circulou o \"K.\" de caneta.";
            comunicado.first = Lines(("kai", "<i>Sala 704. A mensagem do celular.</i>"), ("kai", "<i>O prédio tem sete andares. Eu contei.</i>"));
            comunicado.again = Lines(("kai", "<i>Desligamento.</i>"));

            var moedaSpr = SpriteObj("Moeda", sag, O("moeda"), new Vector3(moedaX - A(6f), Chao - 0.02f, 0f), ObjOrder);
            var gMoeda = Brilho(moedaSpr.transform, new Vector3(moedaX, Chao + 0.1f, 0f), 0.35f);
            var moeda = Spot(sag, "moeda", moedaX, Chao, 1.2f, 1.0f, sfx);
            moeda.first = Lines(("kai", "<i>Uma moeda, caída no chão.</i>"));
            dir.moeda = moeda;
            dir.moedaVisual = moedaSpr.gameObject;

            SpriteObj("MaquinaAutomatica", sag, O("maquina_automatica"), new Vector3(maquinaX, Chao, 0f), ObjOrder);
            var maquina = Spot(sag, "maquina", maquinaX + A(MaquinaW / 2f), Chao, 4.3f, 1.3f, sfx, papel);
            maquina.docTitle = "RECIBO DA MÁQUINA";
            maquina.docBody = "<b>MÁQUINA AUTOMÁTICA</b>\n1 água\nTotal: pago.\n\n<i>Impresso embaixo, em letras miúdas:</i>\ntroco: uma peça de trilho.";
            maquina.docFooter = "A peça caiu na bandeja, junto com a garrafa e o recibo.";
            maquina.first = Lines(("kai", "<i>A máquina engoliu a moeda... e cuspiu uma peça de trilho.</i>"), ("kai", "<i>Do tamanho dos trilhos do painel da porta.</i>"));
            maquina.again = Lines(("kai", "<i>A máquina não tem mais nada.</i>"));
            var gMaq = Brilho(sag, new Vector3(maquinaX + A(MaquinaBandeja.x), Chao + A(MaquinaBandeja.y), 0f), 0.5f);
            dir.maquina = maquina;

            // ---------- Administração: o painel de desvios preso ao batente e a porta ----------
            var adm = new GameObject("Administracao").transform;
            adm.SetParent(world, false);
            float painelY = Chao + A(95f);
            var painelApagado = SpriteObj("PainelDesvios", adm, O("painel_desvios"), new Vector3(painelX - A(PainelW / 2f), painelY, 0f), ObjOrder);
            var painelAceso = SpriteObj("PainelDesvios_Aceso", adm, O("painel_desvios_aceso"), painelApagado.transform.position, ObjOrder);
            var door = BuildPorta(adm, porta0, sfx, new Vector3(painelX - A(PainelW / 2f) + A(PainelLampada.x), painelY + A(PainelLampada.y), 0f));
            dir.door = door;
            dir.painelApagado = painelApagado.gameObject;
            dir.painelAceso = painelAceso.gameObject;

            foreach (var spot in world.GetComponentsInChildren<ObjectSpot>()) spot.docOnlyOnce = true; // o aviso do portão também: depois, o E vai direto para o teclado
            // só o que é puzzle brilha (a peça da máquina e a moeda); as leituras de história ficam sem brilho
            dir.documents = new[] { maquina };
            dir.docGlows = new[] { gMaq, gMoeda };
            dir.panel = BuildSwitchPanel(ui.dialogue.transform.root, fonts, voice);

            // ---------- Souta e Ren ----------
            // na plataforma, no primeiro quadro, entre o bilhete e a moeda (em cima da moeda eles roubavam o E dela)
            var souta = BuildNpc("Souta", "souta", Plataforma0 + 3.1f, PixelArtHeightSouta, playerGo.transform);
            var ren = BuildNpc("Ren", "ren", Plataforma0 + 4.3f, PixelArtHeightRen, playerGo.transform);
            dir.souta = souta;
            dir.ren = ren;
            // no saguão, na frente da bilheteria (longe da moeda); depois, à esquerda do painel
            dir.soutaSpots = new[] { souta.transform.position, P(s0 + 4.3f), P(porta0 - 3.0f) };
            dir.renSpots = new[] { ren.transform.position, P(s0 + 5.3f), P(porta0 - 4.2f) };

            Falas(dir, souta, ren);
        }

        static Vector3 P(float x) => new Vector3(x, Chao, 0f);

        static ObjectSpot Spot(Transform parent, string id, float x, float y, float promptHeight, float reach, AudioSource sfx, AudioClip clip = null)
        {
            var go = new GameObject("Lugar_" + id);
            go.transform.SetParent(parent, false);
            go.transform.position = new Vector3(x, y, 0f);
            var spot = go.AddComponent<ObjectSpot>();
            spot.id = id;
            spot.reachX = reach;
            spot.promptOffset = new Vector2(0f, promptHeight);
            spot.sfx = sfx;
            spot.useClip = clip;
            return spot;
        }

        static NpcTalk BuildNpc(string name, string id, float x, float height, Transform player)
        {
            var root = new GameObject(name);
            root.transform.position = P(x);
            var npc = root.AddComponent<NpcTalk>();
            npc.reachX = 1.7f;
            npc.promptOffset = new Vector2(0f, height + 0.45f);
            npc.lookAt = player;
            npc.rig = BuildPersonagem(id, root.transform, Vector3.zero, 6);
            NoVagao(npc.rig);
            npc.balao = BalaoDica(root.transform, npc.promptOffset.y + 0.1f);
            return npc;
        }

        /// <summary>A porta da administração: batente com o vão escuro e as duas folhas, que correm dentro dele.</summary>
        static ExitDoor BuildPorta(Transform parent, float x0, AudioSource sfx, Vector3 lampada)
        {
            float y0 = Chao - A(PortaChao);
            var root = new GameObject("PortaAdministracao");
            root.transform.SetParent(parent, false);
            root.transform.position = new Vector3(x0 + A(PortaFolhas.x), Chao, 0f);
            var door = root.AddComponent<ExitDoor>();
            door.reachX = 1.6f;
            door.promptOffset = new Vector2(0f, 4.6f);
            SpriteObj("Batente", root.transform, O("porta_adm"), new Vector3(x0, y0, 0f), FundoOrder - 2);
            // porta recuada: o batente projeta sombra na parede e no chão
            SpriteObj("Sombra", root.transform, O("porta_sombra"), new Vector3(x0, y0 - A(12f), 0f), FundoOrder - 3);
            var mask = new GameObject("Mascara");
            mask.transform.SetParent(root.transform, false);
            mask.transform.position = new Vector3(x0 + A(PortaFolhas.x), y0 + A(PortaFolhas.y), 0f);
            mask.transform.localScale = new Vector3(A(PortaFolhas.z) / 0.08f, A(PortaFolhas.w) / 0.08f, 1f);
            mask.AddComponent<SpriteMask>().sprite = Spr("pixel");
            SpriteRenderer Folha(string nome, Vector4 f)
            {
                var sr = SpriteObj(nome, root.transform, O(nome), new Vector3(x0 + A(f.x - f.z / 2f), y0 + A(f.y - f.w / 2f), 0f), FundoOrder - 1);
                sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
                return sr;
            }
            door.leftLeaf = Folha("porta_adm_folha_esq", FolhaEsq).transform;
            door.rightLeaf = Folha("porta_adm_folha_dir", FolhaDir).transform;
            door.slideDistance = A(FolhaEsq.z) + 0.05f;
            // a luz de trava é a lâmpada do painel de desvios, do lado
            var lamp = SpriteObj("SinalTrava_Luz", root.transform, Spr("ponto"), lampada, ObjOrder + 1);
            lamp.transform.localScale = new Vector3(1.1f, 1.1f, 1f);
            var glow = SpriteObj("SinalTrava_Halo", root.transform, Spr("brilho"), lampada, ObjOrder + 2);
            glow.transform.localScale = Vector3.one * 0.45f;
            door.signalLamp = lamp;
            door.signalGlow = glow;
            door.sfx = sfx;
            door.unlockClip = Clip1("porta_destrava");
            return door;
        }

        // ------------------------------------------------------------------ falas

        static void Falas(Cenario3Director dir, NpcTalk souta, NpcTalk ren)
        {
            dir.intro = Lines(
                ("kai", "<i>A estação que eu vi pela janela. Vazia, com as luzes ainda acesas.</i>"),
                ("ren", "A porta do trem... fechou sozinha."),
                ("souta", "Melhor. Ninguém volta para aquele trem."),
                ("souta", "Tem um portão no fim da plataforma. A saída deve ser por ali."),
                ("kai", "<i>Terminal. Mas o trem não devia ter parado aqui.</i>"));
            dir.portaoPrimeiro = Lines(("kai", "<i>O horário da última partida. O quadro de partidas está bem aqui do lado.</i>"));
            dir.codigoErrado = Lines(("kai", "<i>Não é esse. O último trem do quadro, sem os dois pontos.</i>"));
            dir.codigoArmadilha = Lines(
                ("kai", "<i>Não abriu.</i>"),
                ("kai", "<i>Quatro e dezessete não é partida nenhuma. Alguém escreveu por cima.</i>"));
            dir.portaoAberto = Lines(
                ("souta", "Vinte e três e cinquenta. O último trem de verdade."),
                ("ren", "Então o nosso... não estava na lista?"),
                ("souta", "Trem fora de horário. Acontece."),
                ("kai", "<i>A letra do 04:17 é torta, apertada. Igual à do meu diário.</i>"),
                ("souta", "Vamos. O saguão é por aqui. A porta da administração fica no fundo."));
            dir.semMoeda = Lines(("kai", "<i>Ela pede uma moeda. Eu não tenho nenhuma.</i>"));
            dir.todasPecas = Lines(
                ("souta", "Um pedaço de trilho? Deve ser do painel do lado da porta."),
                ("souta", "A gente te espera lá."));
            dir.renGivesLast = Lines(
                ("ren", "Kai, espera! Eu achei isso no chão, perto da máquina."),
                ("ren", "É um pedaço de trilho. É o que está faltando no painel?"),
                ("kai", "<i>É a última peça. Agora dá para completar a linha.</i>"));
            dir.doorClosed = Lines(
                ("kai", "<i>O painel do lado da porta mostra os trilhos da estação. A luz sai da plataforma e tem que chegar até a administração.</i>"),
                ("kai", "<i>Mas falta um trilho no meio. Sem ele a linha nunca fecha.</i>"));
            dir.doorUnlocked = Lines(
                ("kai", "<i>A linha acendeu inteira. A porta destravou.</i>"),
                ("souta", "Administração. Se tem um jeito de fazer o trem andar de novo, é lá dentro."),
                ("ren", "E se a gente só esperasse o próximo trem?"),
                ("souta", "Que próximo? O último foi às vinte e três e cinquenta."),
                ("kai", "<i>E o das quatro e dezessete.</i>"),
                ("ren", "Kai... a tela do painel. É a câmera da plataforma. Tem alguém lá, na beira."),
                ("souta", "Reflexo. Não tem ninguém aqui além de nós três."),
                ("kai", "<i>Na tela, uma pessoa só.</i>"));

            dir.conversaSouta = new[]
            {
                new FalasVagao { falas = Lines(("souta", "O portão tem um teclado. Tem um aviso do lado dele.")) },
                new FalasVagao { falas = Lines(("souta", "Tudo aqui tem um mecanismo, Kai. Até essa estação.")) },
                new FalasVagao { falas = Lines(("souta", "Depois dessa porta, nada de parar. Entendeu?")) },
            };
            dir.conversaRen = new[]
            {
                new FalasVagao { falas = Lines(("ren", "Eu não gosto de plataforma vazia. Parece que alguém vai empurrar a gente."), ("kai", "<i>...Por que ele disse isso?</i>")) },
                new FalasVagao { falas = Lines(("ren", "Tem um papel colado no vidro da bilheteria. Com o nome de alguém circulado.")) },
                new FalasVagao { falas = Lines(("ren", "Eu fico do seu lado. Mesmo que você não queira.")) },
            };
            dir.dicaSouta = new[]
            {
                new FalasVagao { falas = Lines(("souta", "O portão fecha depois da última partida. O quadro do lado dele diz qual foi.")) },
                new FalasVagao { falas = Lines(("souta", "A máquina pede moeda. Tem uma brilhando no chão, perto da bilheteria.")) },
                new FalasVagao { falas = Lines(("souta", "A luz entra pela plataforma e tem que sair embaixo, na administração. Gira só o que está no caminho.")) },
            };
            dir.dicaRen = new[]
            {
                new FalasVagao { falas = Lines(("ren", "Não o que está escrito à mão... o que está impresso.")) },
                new FalasVagao { falas = Lines(("ren", "Essas máquinas às vezes devolvem coisa estranha no troco...")) },
                new FalasVagao { falas = Lines(("ren", "Começa pela porta e volta até a entrada.")) },
            };
            souta.firstTalk = souta.repeatTalk = dir.conversaSouta[0].falas;
            ren.firstTalk = ren.repeatTalk = dir.conversaRen[0].falas;
        }

        // ------------------------------------------------------------------ painel de desvios

        /// <summary>
        /// A grade do painel: 5 x 3 peças. A corrente entra pela esquerda na linha do meio e tem que sair pela direita
        /// na linha de baixo. A peça 0 (a da máquina) fica no caminho. Solução: girar até
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
            // só cinco trilhos do caminho começam tortos (5 giros, no mínimo); o resto já está na posição da solução
            ui.startTurns = new[]
            {
                1, 0, 1, 0, 0,
                0, 0, 0, 0, 0,
                0, 0, 1, 1, 1,
            };
            // a única peça (a da máquina) é o trilho do meio da descida
            ui.pieceOf = new[]
            {
                -1, -1, -1, -1, -1,
                -1, -1,  0, -1, -1,
                -1, -1, -1, -1, -1,
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
