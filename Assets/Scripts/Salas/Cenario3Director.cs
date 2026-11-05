using System.Collections;
using System.Collections.Generic;
using Platformer.Mechanics;
using UltimaLinha.Cenario1;
using UltimaLinha.Cenario2;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UltimaLinha.Salas.DirUtil;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Cenário 3 — a estação (Ferramentas/Cenario3/PLANO.md). O Kai sai pela porta do fim do trem, na mesma plataforma que
    /// se via pela janela no fim do Cenário 2. Três partes, cada uma com algo para ler, um puzzle e uma conversa:
    /// 1. Plataforma: o portão de serviço tem um código de 4 números, o horário da última partida do quadro (2350).
    ///    O 04:17 escrito à mão embaixo dele é a armadilha.
    /// 2. Saguão: a moeda debaixo do banco da plataforma compra na máquina (troco: uma peça); no mezanino, subindo
    ///    pelos caixotes, a gaveta embaixo do monitor da câmera tem outra. Peças: bilhete, quadro, máquina, gaveta.
    /// 3. Administração: com as quatro peças, o painel de desvios (SwitchPanelUI) liga a porta.
    /// Souta e Ren vão uma parte na frente; 75 s sem avançar, ficam com o balão "..." e dão uma dica.
    /// </summary>
    public class Cenario3Director : MonoBehaviour
    {
        public PlayerController player;
        public CameraFollow2D cameraFollow;
        public NpcTalk souta, ren;
        [Tooltip("Onde Souta e Ren esperam em cada parte (plataforma, saguão, porta da administração).")]
        public Vector3[] soutaSpots = new Vector3[0], renSpots = new Vector3[0];
        [Tooltip("A porta do fim do trem, por onde o Kai saiu: fecha sozinha atrás dele.")]
        public PortaPonta portaTrem;

        [Header("Parte 1: o portão")]
        public ObjectSpot portao;
        public Transform portaoGrade;
        public Collider2D portaoBloqueio;
        public float portaoCorre = 2.3f;
        public DigitLockUI lockUI;
        public string codigo = "2350";
        public string armadilha = "0417";
        public AudioClip portaoClip;

        [Header("Parte 2: moeda, máquina e mezanino")]
        public ObjectSpot moeda;
        public GameObject moedaVisual;
        public ObjectSpot maquina;

        [Header("Parte 3: o painel")]
        [Tooltip("Os lugares com as peças do painel, na ordem das peças (0 a 3).")]
        public ObjectSpot[] documents = new ObjectSpot[0];
        public ExitDoor door;
        public SwitchPanelUI panel;
        public GameObject painelApagado, painelAceso;
        [Tooltip("Brilho fraco em cima de cada peça (mesma ordem), some quando ela é pega.")]
        public SpriteRenderer[] docGlows = new SpriteRenderer[0];
        [Tooltip("Com só uma peça faltando, o Ren entrega a que ele achou.")]
        public List<DialogueLine> renGivesLast = new List<DialogueLine>();

        public Vector2 gravity = new Vector2(0f, -20f);
        public string nextScene = "Cenario4";
        public string menuScene = "MainMenu";

        [Header("Som")]
        public AudioSource ambience;
        public float ambienceVolume = 0.5f;
        public AudioSource sfx;
        public AudioClip noteClip;
        public AudioClip pieceClip;
        public AudioClip coinClip;

        [Header("Dicas")]
        public float dicaSegundos = 75f;
        public string dicaToast = "Talvez o Souta ou o Ren saibam de alguma coisa.";

        public string objectiveGate = "Abra o portão de serviço no fim da plataforma.";
        public string objectiveFind = "Ache as peças que faltam no painel de desvios.";
        public string objectiveDoor = "Complete a linha no painel ao lado da porta da administração.";

        public List<DialogueLine> intro = new List<DialogueLine>();
        public List<DialogueLine> portaoPrimeiro = new List<DialogueLine>();
        public List<DialogueLine> codigoErrado = new List<DialogueLine>();
        public List<DialogueLine> codigoArmadilha = new List<DialogueLine>();
        public List<DialogueLine> portaoAberto = new List<DialogueLine>();
        public List<DialogueLine> semMoeda = new List<DialogueLine>();
        public List<DialogueLine> todasPecas = new List<DialogueLine>();
        [Tooltip("Primeira vez no painel, sem nenhuma peça.")]
        public List<DialogueLine> doorClosed = new List<DialogueLine>();
        public List<DialogueLine> doorUnlocked = new List<DialogueLine>();
        [Tooltip("O que cada um fala em cada parte, e a dica de quando o jogador trava.")]
        public FalasVagao[] conversaSouta = new FalasVagao[0], conversaRen = new FalasVagao[0];
        public FalasVagao[] dicaSouta = new FalasVagao[0], dicaRen = new FalasVagao[0];

        public string endTitle = "FIM DO CENÁRIO 3";
        public string endSubtitle = "A sala de controle ainda está sendo construída.";

        Vector2 previousGravity;
        bool introDone, leaving, unlocked, renHelped, explained, busy, gateOpen, temMoeda, falouPecas;
        bool[] found;
        int parteNpc;
        readonly Coroutine[] andando = new Coroutine[2];
        float semProgresso;

        void Awake()
        {
            previousGravity = Physics2D.gravity;
            Physics2D.gravity = gravity;
            Time.timeScale = 1f;
            Modal.Reset();
            found = new bool[documents.Length];
        }

        void OnDestroy() { Physics2D.gravity = previousGravity; }

        IEnumerator Start()
        {
            bool continuing = GameSave.BeginScene(SceneManager.GetActiveScene().name);
            if (player) player.controlEnabled = false;

            foreach (var d in documents) if (d) d.Presented += OnDocument;
            if (panel) panel.Solved += OnSolved;
            if (door)
            {
                door.onLockedInteract = OnDoorLocked;
                door.onEnter = () => StartCoroutine(Leave(nextScene));
            }
            if (portao) portao.Presented += (_, __) => StartCoroutine(AbrePortao());
            if (lockUI) lockUI.Submitted += t => StartCoroutine(CheckCode(t));
            if (moeda) moeda.Presented += (_, __) => PegaMoeda();
            if (maquina) maquina.Intercept = _ => { if (temMoeda) return false; Say(semMoeda); return true; };
            foreach (var npc in new[] { souta, ren })
                if (npc) npc.Talked += _ => { if (souta) souta.dica = false; if (ren) ren.dica = false; Progresso(); };
            if (painelAceso) painelAceso.SetActive(false);
            if (portaTrem) portaTrem.AbertaJa();

            // jogo continuado: portão, moeda, peças e, se for o caso, a linha já completa (porta aberta)
            if (continuing)
            {
                for (int i = 0; i < found.Length; i++) found[i] = GameSave.Has("peca" + i);
                renHelped = GameSave.Has("ren_ajudou");
                explained = GameSave.Has("painel_visto");
                falouPecas = GameSave.Has("pecas_conversa");
                if (GameSave.Has("portao")) AbrePortaoJa();
                if (GameSave.Has("moeda")) { temMoeda = true; EscondeMoeda(); }
                if (GameSave.Has("linha"))
                {
                    unlocked = true;
                    if (panel) panel.MarkSolved();
                    if (door) door.Unlock();
                    AcendePainel();
                }
                if (portaTrem) portaTrem.Close();
                AutoSave.RestorePlayer(player, cameraFollow);
            }
            MoveNpcs(!gateOpen ? 0 : Count() < found.Length ? 1 : 2);
            if (cameraFollow) cameraFollow.Snap();

            if (ambience)
            {
                ambience.loop = true; ambience.volume = 0f; ambience.Play();
                StartCoroutine(FadeAudio(ambience, ambienceVolume, 3f));
            }
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, continuing ? 1.2f : 1.6f);
            if (!continuing)
            {
                yield return new WaitForSeconds(0.4f);
                if (portaTrem) portaTrem.Close(); // a porta do fim do trem bate atrás do Kai
                yield return new WaitForSeconds(0.7f);
                yield return SayAndWait(intro);
                GameSave.Save(SceneManager.GetActiveScene().name); // libera o "Continuar" no menu
            }
            else if (Hud.Instance) Hud.Instance.Toast("Jogo carregado.");
            if (player) player.controlEnabled = true;
            Objetivo();
            introDone = true;
        }

        void Update()
        {
            for (int i = 0; i < docGlows.Length; i++)
            {
                var g = docGlows[i];
                if (!g) continue;
                bool show = i < documents.Length ? found == null || i >= found.Length || !found[i] : !temMoeda && !(moeda && moeda.Uses > 0);
                if (g.enabled != show) g.enabled = show;
                if (show)
                {
                    var c = g.color;
                    c.a = 0.22f + 0.16f * Mathf.Sin(Time.time * 2.6f + i * 1.7f);
                    g.color = c;
                }
            }
            if (!introDone || leaving) return;
            if (InputHelper.BackPressed() && !DialogueBox.IsBusy && !DiarioKai.IsOpen && !Modal.IsOpen) { StartCoroutine(Leave(menuScene)); return; }

            ChegaQuandoEscondidos();
            if (!unlocked && !DialogueBox.IsBusy && !Modal.IsOpen) semProgresso += Time.deltaTime;
            if (semProgresso > dicaSegundos) LigaDica();
            // todas as peças: a conversa espera o Kai estar livre (pode ter acabado de ler um papel)
            if (!falouPecas && Count() == found.Length && !busy && !DialogueBox.IsBusy && !Modal.IsOpen && player && player.IsGrounded)
                StartCoroutine(TodasPecas());
        }

        int Count()
        {
            int n = 0;
            foreach (var f in found) if (f) n++;
            return n;
        }

        void Objetivo()
        {
            if (!Hud.Instance) return;
            Hud.Instance.SetObjective(unlocked ? "" : !gateOpen ? objectiveGate : Count() < found.Length ? objectiveFind : objectiveDoor);
        }

        // ---------- Parte 1: o portão ----------

        IEnumerator AbrePortao()
        {
            if (gateOpen || !lockUI) yield break;
            if (portao.Uses == 1 && portaoPrimeiro.Count > 0) yield return SayAndWait(portaoPrimeiro);
            Progresso();
            lockUI.Open("0000");
        }

        IEnumerator CheckCode(string typed)
        {
            bool ok = typed == codigo;
            lockUI.ShowResult(ok);
            if (!ok)
            {
                yield return new WaitForSecondsRealtime(0.9f);
                var fala = typed == armadilha ? codigoArmadilha : codigoErrado;
                if (fala.Count > 0 && !DialogueBox.IsBusy)
                {
                    lockUI.Close();
                    Say(fala);
                }
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.8f);
            lockUI.Close();
            gateOpen = true;
            GameSave.Mark("portao");
            Progresso();
            // o teclado devolve o controle quando acaba de sumir: só depois disso prende o Kai (senão ele sai andando no
            // meio do portão correndo, abre um papel e o papel corta a conversa do portão)
            while (Modal.IsOpen) yield return null;
            yield return null;
            if (player) player.controlEnabled = false;
            if (sfx && portaoClip) sfx.PlayOneShot(portaoClip);
            if (portaoBloqueio) portaoBloqueio.enabled = false;
            if (portao) portao.available = false;
            if (portaoGrade)
            {
                Vector3 a = portaoGrade.localPosition, b = a + Vector3.left * portaoCorre;
                for (float t = 0f; t < 1.2f; t += Time.deltaTime)
                {
                    portaoGrade.localPosition = Vector3.Lerp(a, b, Mathf.SmoothStep(0f, 1f, t / 1.2f));
                    yield return null;
                }
                portaoGrade.localPosition = b;
            }
            yield return SayAndWait(portaoAberto);
            if (player) player.controlEnabled = true;
            Objetivo();
            AndaPara(1); // eles vão na frente, para o saguão
        }

        void AbrePortaoJa()
        {
            gateOpen = true;
            if (portaoBloqueio) portaoBloqueio.enabled = false;
            if (portao) portao.available = false;
            if (portaoGrade) portaoGrade.localPosition += Vector3.left * portaoCorre;
        }

        // ---------- Parte 2: moeda e peças ----------

        void PegaMoeda()
        {
            if (temMoeda) return;
            temMoeda = true;
            GameSave.Mark("moeda");
            EscondeMoeda();
            if (sfx && coinClip) sfx.PlayOneShot(coinClip, 0.8f);
            if (Hud.Instance) Hud.Instance.Toast("Uma moeda.");
            Progresso();
        }

        void EscondeMoeda()
        {
            if (moedaVisual) moedaVisual.SetActive(false);
            if (moeda) moeda.available = false;
        }

        void OnDocument(ObjectSpot spot, bool firstTime)
        {
            int i = System.Array.IndexOf(documents, spot);
            if (i < 0 || found[i]) return;
            Found(i);
            if (Count() == found.Length) Objetivo();
        }

        void Found(int i)
        {
            found[i] = true;
            GameSave.Mark("peca" + i);
            if (documents[i]) documents[i].available = documents[i] != maquina; // a máquina não tem mais nada
            if (sfx && pieceClip) sfx.PlayOneShot(pieceClip, 0.8f);
            else if (sfx && noteClip) sfx.PlayOneShot(noteClip);
            if (Hud.Instance) Hud.Instance.Toast($"Peça do painel encontrada ({Count()}/{found.Length})");
            Progresso();
        }

        IEnumerator TodasPecas()
        {
            falouPecas = true;
            GameSave.Mark("pecas_conversa");
            busy = true;
            yield return SayAndWait(todasPecas);
            busy = false;
            Objetivo();
            AndaPara(2);
        }

        // ---------- Parte 3: o painel ----------

        void OnDoorLocked()
        {
            if (unlocked || busy || !panel) return;
            if (Count() == 0 && !explained)
            {
                StartCoroutine(FirstLook());
                return;
            }
            if (Count() == found.Length - 1 && !renHelped)
            {
                StartCoroutine(RenGivesLast());
                return;
            }
            Progresso();
            panel.Open(found);
        }

        /// <summary>Primeira vez na porta, sem peças: o Kai entende o painel e o mostra para o jogador.</summary>
        IEnumerator FirstLook()
        {
            busy = true;
            explained = true;
            GameSave.Mark("painel_visto");
            yield return SayAndWait(doorClosed);
            busy = false;
            panel.Open(found);
        }

        /// <summary>Faltando só uma peça, o Ren aparece com ela: achou caída perto dos caixotes.</summary>
        IEnumerator RenGivesLast()
        {
            busy = true;
            renHelped = true;
            GameSave.Mark("ren_ajudou");
            if (player) player.controlEnabled = false;
            if (ren)
            {
                if (andando[1] != null) { StopCoroutine(andando[1]); andando[1] = null; }
                ren.enabled = false;
                yield return WalkTo(ren, door.transform.position.x - 2.2f, 3.0f);
                if (ren.rig) ren.rig.facing = 1f;
            }
            yield return SayAndWait(renGivesLast);
            int missing = System.Array.IndexOf(found, false);
            if (missing >= 0) Found(missing);
            if (ren) ren.enabled = true;
            if (player) player.controlEnabled = true;
            yield return new WaitForSeconds(0.3f);
            busy = false;
            falouPecas = true; // a conversa das peças é sobre a câmera da gaveta; com a ajuda do Ren ela fica para trás
            Objetivo();
            panel.Open(found);
        }

        void OnSolved()
        {
            if (unlocked) return;
            unlocked = true;
            GameSave.Mark("linha");
            StartCoroutine(OpenDoor());
        }

        void AcendePainel()
        {
            if (painelApagado) painelApagado.SetActive(false);
            if (painelAceso) painelAceso.SetActive(true);
        }

        IEnumerator OpenDoor()
        {
            if (player) player.controlEnabled = false;
            AcendePainel();
            door.Unlock();
            while (Modal.IsOpen) yield return null; // o painel devolve o controle quando acaba de sumir
            yield return null;
            if (player) player.controlEnabled = false;
            yield return new WaitForSeconds(0.6f);
            yield return SayAndWait(doorUnlocked);
            if (player) player.controlEnabled = true;
            Objetivo();
        }

        // ---------- Souta e Ren ----------

        void MoveNpcs(int parte)
        {
            parteNpc = parte;
            if (souta && parte < soutaSpots.Length) souta.transform.position = soutaSpots[parte];
            if (ren && parte < renSpots.Length) ren.transform.position = renSpots[parte];
            Falas(parte);
        }

        void Falas(int parte)
        {
            if (souta && parte < conversaSouta.Length) souta.firstTalk = souta.repeatTalk = conversaSouta[parte].falas;
            if (ren && parte < conversaRen.Length) ren.firstTalk = ren.repeatTalk = conversaRen[parte].falas;
        }

        /// <summary>Eles andam na frente até a próxima parte, sem prender o Kai (fora da câmera, chegam de uma vez).</summary>
        void AndaPara(int parte)
        {
            parteNpc = parte;
            Falas(parte);
            var npcs = new[] { souta, ren };
            var spots = new[] { soutaSpots, renSpots };
            for (int i = 0; i < 2; i++)
            {
                if (!npcs[i] || parte >= spots[i].Length) continue;
                if (andando[i] != null) StopCoroutine(andando[i]);
                andando[i] = StartCoroutine(Anda(npcs[i], spots[i][parte].x, i));
            }
        }

        IEnumerator Anda(NpcTalk npc, float x, int i)
        {
            npc.enabled = false;
            yield return WalkTo(npc, x, 3.2f);
            Chegou(npc, i);
        }

        void Chegou(NpcTalk npc, int i)
        {
            if (npc.rig) npc.rig.externalSpeed = 0f;
            npc.enabled = true;
            andando[i] = null;
        }

        /// <summary>Andando a 3,2 u/s (e parados enquanto um papel ou uma fala para o tempo) eles ficam para trás do Kai
        /// correndo: quando eles e o lugar de chegada estão fora da câmera, vão direto para lá.</summary>
        void ChegaQuandoEscondidos()
        {
            if (!cameraFollow) return;
            var cam = cameraFollow.GetComponent<Camera>();
            float meia = cam ? cam.orthographicSize * cam.aspect + 1.5f : 10f, cx = cameraFollow.transform.position.x;
            var npcs = new[] { souta, ren };
            var spots = new[] { soutaSpots, renSpots };
            for (int i = 0; i < 2; i++)
            {
                if (andando[i] == null || !npcs[i] || parteNpc >= spots[i].Length) continue;
                var alvo = spots[i][parteNpc];
                if (Mathf.Abs(npcs[i].transform.position.x - cx) > meia && Mathf.Abs(alvo.x - cx) > meia)
                {
                    StopCoroutine(andando[i]);
                    npcs[i].transform.position = alvo;
                    Chegou(npcs[i], i);
                }
            }
        }

        void LigaDica()
        {
            if (!souta || souta.dica || DialogueBox.IsBusy) return;
            int v = !gateOpen ? 0 : Count() < found.Length ? 1 : 2;
            if (v < dicaSouta.Length) souta.hintLevers = dicaSouta[v].falas;
            if (ren && v < dicaRen.Length) ren.hintLevers = dicaRen[v].falas;
            souta.dica = true;
            if (ren) ren.dica = true;
            if (Hud.Instance) Hud.Instance.Toast(dicaToast);
        }

        void Progresso() { semProgresso = 0f; }

        IEnumerator Leave(string scene)
        {
            if (leaving) yield break;
            leaving = true;
            if (player) player.controlEnabled = false;
            if (ambience) StartCoroutine(FadeAudio(ambience, 0f, 1.2f));
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 1.4f);
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                if (Hud.Instance) yield return Hud.Instance.ShowEnd(endTitle, endSubtitle);
                yield return new WaitForSeconds(3.5f);
                scene = menuScene;
            }
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
            else Debug.LogWarning($"[Cenário 3] A cena \"{scene}\" não está na lista de cenas do build.");
        }
    }
}
