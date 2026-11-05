using System;
using System.Collections;
using System.Collections.Generic;
using Platformer.Mechanics;
using TMPro;
using UltimaLinha.Cenario1;
using UltimaLinha.Salas;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaLinha.Cenario2
{
    /// <summary>Falas de um vagão (a Unity não guarda lista de listas).</summary>
    [Serializable]
    public class FalasVagao
    {
        public List<DialogueLine> falas = new List<DialogueLine>();
    }

    /// <summary>
    /// Roteiro do Cenário 2: o trem volta a andar. Quatro vagões, três passagens; em cada vagão um puzzle, coisas para
    /// ler e uma conversa quando ele é resolvido. Souta e Ren vão sempre um vagão na frente.
    /// 1. Cadeado de rodas na porta do fundo (código 0417, no relógio e no celular). Abre: as portas da passagem 1 começam a bater.
    /// 2. A porta de trás da passagem 2 bate rápido demais: a alavanca de retenção, no começo do vagão, a segura aberta.
    /// 3. O vagão 3 está sem energia (a luz das portas não acende): três fusíveis, a caixa no fundo; a luz volta com uma frase na parede.
    /// 4. No último vagão o trem freia na estação e a porta do fim abre.
    /// Se a porta bater no Kai ou ele cair no vão, volta ao começo do vagão em que estava. Travou (3 mortes na mesma
    /// passagem ou 75 s sem avançar): Souta e Ren ficam com o balão "..." e dão uma dica.
    /// </summary>
    public class Cenario2Director : MonoBehaviour
    {
        public PlayerController player;
        public CameraFollow2D cameraFollow;
        [Tooltip("Portas das passagens, duas por passagem (a de trás e a da frente).")]
        public CarDoor[] doors = new CarDoor[0];
        [Tooltip("Por onde o Kai entrou (fica trancada).")]
        public PortaPonta portaEntrada;
        [Tooltip("Porta do fim do último vagão (abre quando o trem para).")]
        public PortaPonta exitDoor;
        public TunnelLights tunnel;
        [Tooltip("A estação: atrás da porta do fim e pelas janelas do último vagão (aparece quando o trem para).")]
        public SpriteRenderer[] estacao = new SpriteRenderer[0];

        [Header("Souta e Ren")]
        public NpcTalk souta;
        public NpcTalk ren;
        [Tooltip("Onde cada um espera em cada vagão.")]
        public Vector3[] soutaSpots = new Vector3[0];
        public Vector3[] renSpots = new Vector3[0];

        [Header("Pontos do cenário (x em unidades)")]
        [Tooltip("Começo de cada vagão: onde o Kai volta se morrer.")]
        public float[] checkpoints = new float[0];
        [Tooltip("Fim (parede da ponta direita) de cada vagão.")]
        public float[] carEnds = new float[0];
        [Tooltip("Os buracos do chão entre os vagões (x0, x1).")]
        public Vector2[] buracos = new Vector2[0];
        public float spawnY = 2.2f;
        [Tooltip("Abaixo disso o Kai caiu no vão.")]
        public float killY = -1.6f;
        [Tooltip("Chegou ao último vagão: o trem freia.")]
        public float endTriggerX = 120f;
        [Tooltip("Passou da porta do fim: sai do cenário.")]
        public float exitX = 140f;

        [Header("Puzzle 1: cadeado")]
        public ObjectSpot cadeado;
        public GameObject cadeadoVisual;
        public DigitLockUI lockUI;
        public string codigo = "0417";
        public AudioClip cadeadoClip;

        [Header("Puzzle 2: retenção")]
        public PowerLever retencao;
        public float retencaoSegundos = 6.5f;
        public string retencaoPuxada = "";

        [Header("Puzzle 3: fusíveis")]
        public ObjectSpot[] fusiveis = new ObjectSpot[0];
        public GameObject[] fusiveisVisual = new GameObject[0];
        public ObjectSpot caixa;
        public GameObject caixaVazia, caixaCheia;
        public FluorescentLight[] luzesVagao3 = new FluorescentLight[0];
        [Tooltip("x de começo e fim do vagão 3 (fica escuro até a luz voltar).")]
        public Vector2 faixaVagao3;
        public CanvasGroup darkness;
        public float escuroVagao3 = 0.5f;
        public TMP_Text escrita;
        public AudioClip ligaClip;
        public AudioClip pegaClip;

        [Header("Física desta cena")]
        public Vector2 gravity = new Vector2(0f, -20f);

        [Header("Cenas")]
        public string nextScene = "Cenario3";
        [Tooltip("Enquanto o Cenário 3 não existe, a porta leva a esta cena.")]
        public string fallbackScene = "Cenario4";
        public string menuScene = "MainMenu";

        [Header("Som")]
        public AudioSource rumble;
        public float rumbleVolume = 0.6f;
        public AudioSource stationAmbience;
        public float stationVolume = 0.5f;
        public AudioSource sfx;
        public AudioClip crushClip;
        public AudioClip fallClip;
        public AudioClip lurchClip;
        public AudioClip brakeClip;
        [Tooltip("Intervalo entre os trancos do trem (segundos).")]
        public Vector2 lurchInterval = new Vector2(7f, 14f);

        [Header("Tela")]
        [Tooltip("Clarão vermelho quando a porta pega o Kai.")]
        public CanvasGroup flash;

        [Header("Dicas")]
        public int dicaMortes = 3;
        public float dicaSegundos = 75f;
        public string dicaToast = "Talvez o Souta ou o Ren saibam de alguma coisa.";

        [Header("Objetivos")]
        public string objectiveLock = "Abra o cadeado da porta do fundo do vagão.";
        public string objectiveCross = "Passe para o próximo vagão quando a porta abrir.";
        public string objectiveLever = "Puxe a alavanca de retenção e corra até a porta.";
        public string objectiveFuses = "Ache os três fusíveis do vagão.";
        public string objectiveBox = "Encaixe os fusíveis na caixa do fundo do vagão.";
        public string objectiveLast = "Passe para o último vagão.";
        public string objectiveExit = "Saia do trem pela porta do fim do vagão.";

        [Header("Falas")]
        public List<DialogueLine> intro = new List<DialogueLine>();
        public List<DialogueLine> portaEntradaFala = new List<DialogueLine>();
        public List<DialogueLine> saidaTrancada = new List<DialogueLine>();
        public List<DialogueLine> cadeadoPrimeiro = new List<DialogueLine>();
        public List<DialogueLine> codigoErrado = new List<DialogueLine>();
        public List<DialogueLine> cadeadoAberto = new List<DialogueLine>();
        [Tooltip("Souta decide ir na frente com o Ren.")]
        public List<DialogueLine> goAhead = new List<DialogueLine>();
        [Tooltip("O Kai sozinho, depois que eles foram.")]
        public List<DialogueLine> alone = new List<DialogueLine>();
        public List<DialogueLine> firstCrush = new List<DialogueLine>();
        public List<DialogueLine> firstFall = new List<DialogueLine>();
        public List<DialogueLine> vagao2 = new List<DialogueLine>();
        public List<DialogueLine> vagao3 = new List<DialogueLine>();
        public List<DialogueLine> caixaFaltam = new List<DialogueLine>();
        public List<DialogueLine> todosFusiveis = new List<DialogueLine>();
        public List<DialogueLine> luzVoltou = new List<DialogueLine>();
        public List<DialogueLine> braking = new List<DialogueLine>();
        public List<DialogueLine> ending = new List<DialogueLine>();
        [Tooltip("O que cada um fala em cada vagão, e a dica de quando o jogador trava.")]
        public FalasVagao[] conversaSouta = new FalasVagao[0], conversaRen = new FalasVagao[0];
        public FalasVagao[] dicaSouta = new FalasVagao[0], dicaRen = new FalasVagao[0];

        [Header("Fim do cenário (enquanto o Cenário 3 não existe)")]
        public string endTitle = "FIM DO CENÁRIO 2";
        public string endSubtitle = "A estação ainda está sendo construída.";

        Vector2 previousGravity;
        bool introDone, leaving, dying, endingStarted, exitOpen;
        bool lockOpen, cadeadoVisto, luzOk, falouV2, falouV3, retencaoUsada;
        int checkpoint, crushes, falls, fuses, mortesAqui;
        int npcVagao;           // vagão onde Souta e Ren estão
        int npcIr = -1;         // vagão para onde eles vão assim que ninguém estiver olhando
        float semProgresso;

        void Awake()
        {
            previousGravity = Physics2D.gravity;
            Physics2D.gravity = gravity;
            Time.timeScale = 1f;
            if (flash) flash.alpha = 0f;
            Modal.Reset();
        }

        void OnDestroy()
        {
            Physics2D.gravity = previousGravity;
        }

        IEnumerator Start()
        {
            GameSave.BeginScene(SceneManager.GetActiveScene().name);
            if (player) player.controlEnabled = false;
            if (cameraFollow) cameraFollow.Snap();
            foreach (var d in doors)
                if (d) d.Slammed += OnDoorSlam;
            if (tunnel) tunnel.Moving = true;
            if (portaEntrada) portaEntrada.onLockedInteract = () => Say(portaEntradaFala);
            if (exitDoor) exitDoor.onLockedInteract = () => Say(saidaTrancada);
            if (cadeado) cadeado.Intercept = _ => { StartCoroutine(AbreCadeado()); return true; };
            if (lockUI) lockUI.Submitted += OnCodeSubmitted;
            if (retencao) retencao.Raised += OnRetencao;
            foreach (var f in fusiveis)
                if (f) f.Presented += OnFusivel;
            if (caixa) caixa.Intercept = _ => { StartCoroutine(Caixa()); return true; };
            foreach (var npc in new[] { souta, ren })
                if (npc) npc.Talked += OnTalked;
            SetLuzVagao3(false);
            if (escrita) escrita.alpha = 0f;
            foreach (var e in estacao)
                if (e) e.color = new Color(1f, 1f, 1f, 0f);
            if (rumble)
            {
                rumble.loop = true;
                rumble.volume = rumbleVolume * 0.35f; // o trem ainda está quase parado
                rumble.Play();
            }

            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 1.8f);
            yield return new WaitForSeconds(0.5f);

            // o tranco: o trem volta a andar
            Lurch(0.13f, 1.0f, 1f);
            if (rumble) StartCoroutine(FadeAudio(rumble, rumbleVolume, 2.5f));
            yield return new WaitForSeconds(1.1f);

            yield return SayAndWait(intro);

            if (player) player.controlEnabled = true;
            Objetivo(objectiveLock);
            GameSave.Save(SceneManager.GetActiveScene().name); // libera o "Continuar" no menu
            introDone = true;
            StartCoroutine(LurchLoop());
        }

        void Update()
        {
            if (!introDone || leaving || !player) return;

            if (darkness)
            {
                float px = player.transform.position.x;
                float alvo = !luzOk && px > faixaVagao3.x && px < faixaVagao3.y ? escuroVagao3 : 0f;
                darkness.alpha = Mathf.MoveTowards(darkness.alpha, alvo, Time.deltaTime * 0.8f);
            }
            if (dying) return;

            if (InputHelper.BackPressed() && !DialogueBox.IsBusy && !DiarioKai.IsOpen && !Modal.IsOpen)
            {
                StartCoroutine(Leave(menuScene));
                return;
            }

            var b = player.Bounds;
            foreach (var d in doors)
            {
                if (d && d.IsCrushing(b))
                {
                    StartCoroutine(Die(crushed: true));
                    return;
                }
            }
            if (player.transform.position.y < killY)
            {
                StartCoroutine(Die(crushed: false));
                return;
            }

            float x = player.transform.position.x;
            for (int i = checkpoints.Length - 1; i > checkpoint; i--)
            {
                if (x > checkpoints[i] + 0.4f && player.IsGrounded)
                {
                    checkpoint = i;
                    mortesAqui = 0;
                    Progresso();
                    break;
                }
            }
            // passou da porta de trás da passagem 2: eles vão para o vagão 3
            if (npcVagao == 1 && carEnds.Length > 1 && x > carEnds[1] + 1.5f) npcIr = 2;
            MoveNpcsQuandoEscondidos(x);

            semProgresso += Time.deltaTime;
            if (semProgresso > dicaSegundos || mortesAqui >= dicaMortes) LigaDica();

            if (DialogueBox.IsBusy || Modal.IsOpen) return;
            if (!falouV2 && checkpoint >= 1 && player.IsGrounded) StartCoroutine(Vagao2());
            else if (!falouV3 && checkpoint >= 2 && player.IsGrounded) StartCoroutine(Vagao3());

            if (!endingStarted && x > endTriggerX && player.IsGrounded) StartCoroutine(EndSequence());
            if (exitOpen && x > exitX) StartCoroutine(Leave(nextScene));
        }

        // ---------- Vagão 1: o cadeado ----------

        IEnumerator AbreCadeado()
        {
            if (lockOpen || !lockUI) yield break;
            if (!cadeadoVisto && cadeadoPrimeiro.Count > 0) yield return SayAndWait(cadeadoPrimeiro);
            cadeadoVisto = true;
            lockUI.Open("0000");
        }

        void OnCodeSubmitted(string typed) => StartCoroutine(CheckCode(typed));

        IEnumerator CheckCode(string typed)
        {
            bool ok = typed == codigo;
            lockUI.ShowResult(ok);
            if (!ok)
            {
                yield return new WaitForSecondsRealtime(0.9f);
                if (codigoErrado.Count > 0 && !DialogueBox.IsBusy)
                {
                    lockUI.Close();
                    Say(codigoErrado);
                }
                yield break;
            }
            yield return new WaitForSecondsRealtime(0.8f);
            lockUI.Close();
            lockOpen = true;
            Progresso();
            if (sfx && cadeadoClip) sfx.PlayOneShot(cadeadoClip);
            if (cadeadoVisual) cadeadoVisual.SetActive(false);
            if (cadeado) cadeado.available = false;
            yield return new WaitForSeconds(0.4f);
            doors[0].StartCycling();
            doors[1].StartCycling();
            yield return SayAndWait(cadeadoAberto);
            yield return SayAndWait(goAhead);

            // Souta e Ren vão na frente
            player.controlEnabled = false;
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 0.6f);
            MoveNpcs(1);
            yield return new WaitForSeconds(0.5f);
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 0.9f);
            yield return SayAndWait(alone);
            player.controlEnabled = true;
            Objetivo(objectiveCross);
        }

        // ---------- Vagão 2: retenção ----------

        IEnumerator Vagao2()
        {
            falouV2 = true;
            yield return SayAndWait(vagao2);
            Objetivo(objectiveLever);
            Progresso();
        }

        void OnRetencao(PowerLever lever)
        {
            Progresso();
            doors[2].HoldOpen(retencaoSegundos);
            if (!retencaoUsada && Hud.Instance && !string.IsNullOrEmpty(retencaoPuxada)) Hud.Instance.Toast(retencaoPuxada);
            retencaoUsada = true;
            StartCoroutine(AbaixaDepois(lever, retencaoSegundos + 1.5f));
        }

        IEnumerator AbaixaDepois(PowerLever lever, float s)
        {
            yield return new WaitForSeconds(s);
            lever.Lower();
        }

        // ---------- Vagão 3: fusíveis ----------

        IEnumerator Vagao3()
        {
            falouV3 = true;
            if (npcVagao < 2) MoveNpcs(2);
            yield return SayAndWait(vagao3);
            Objetivo(objectiveFuses);
            Progresso();
        }

        void OnFusivel(ObjectSpot spot, bool primeira)
        {
            if (!primeira) return;
            int i = Array.IndexOf(fusiveis, spot);
            if (i >= 0 && i < fusiveisVisual.Length && fusiveisVisual[i]) fusiveisVisual[i].SetActive(false);
            fuses++;
            Progresso();
            if (sfx && pegaClip) sfx.PlayOneShot(pegaClip);
            if (Hud.Instance) Hud.Instance.Toast($"Fusível ({fuses}/{fusiveis.Length})");
            if (fuses >= fusiveis.Length)
            {
                Say(todosFusiveis);
                Objetivo(objectiveBox);
            }
        }

        IEnumerator Caixa()
        {
            if (luzOk) yield break;
            if (fuses < fusiveis.Length)
            {
                Say(caixaFaltam);
                yield break;
            }
            luzOk = true;
            Progresso();
            player.controlEnabled = false;
            if (caixa) caixa.available = false;
            if (caixaVazia) caixaVazia.SetActive(false);
            if (caixaCheia) caixaCheia.SetActive(true);
            if (sfx && ligaClip) sfx.PlayOneShot(ligaClip);
            yield return new WaitForSeconds(0.6f);
            SetLuzVagao3(true);
            doors[4].lampOff = doors[5].lampOff = false;
            yield return new WaitForSeconds(0.8f);
            if (escrita)
            {
                float t = 0f;
                while (t < 1.6f)
                {
                    t += Time.deltaTime;
                    escrita.alpha = Mathf.Clamp01(t / 1.6f) * 0.9f;
                    yield return null;
                }
            }
            yield return SayAndWait(luzVoltou);
            player.controlEnabled = true;
            npcIr = 3;
            Objetivo(objectiveLast);
        }

        void SetLuzVagao3(bool ligada)
        {
            foreach (var l in luzesVagao3)
            {
                if (!l) continue;
                l.broken = !ligada;
                l.enabled = ligada;
                if (l.glow) l.glow.color = new Color(l.glow.color.r, l.glow.color.g, l.glow.color.b, ligada ? l.glowAlpha : 0f);
                if (l.cover) l.cover.color = new Color(l.cover.color.r, l.cover.color.g, l.cover.color.b, ligada ? 0f : 0.92f);
            }
        }

        // ---------- Souta e Ren ----------

        void MoveNpcs(int vagao)
        {
            npcVagao = vagao;
            npcIr = -1;
            if (souta && vagao < soutaSpots.Length) souta.transform.position = soutaSpots[vagao];
            if (ren && vagao < renSpots.Length) ren.transform.position = renSpots[vagao];
            if (souta && vagao < conversaSouta.Length) souta.firstTalk = souta.repeatTalk = conversaSouta[vagao].falas;
            if (ren && vagao < conversaRen.Length) ren.firstTalk = ren.repeatTalk = conversaRen[vagao].falas;
        }

        /// <summary>Eles mudam de vagão sem ninguém ver: longe do Kai onde estão e onde vão aparecer.</summary>
        void MoveNpcsQuandoEscondidos(float x)
        {
            if (npcIr < 0 || npcIr >= soutaSpots.Length || !cameraFollow) return;
            var cam = cameraFollow.GetComponent<Camera>();
            float meia = cam ? cam.orthographicSize * cam.aspect + 1.5f : 10f;
            float cx = cameraFollow.transform.position.x;
            bool longe(Vector3 p) => Mathf.Abs(p.x - cx) > meia;
            if (longe(soutaSpots[npcIr]) && longe(renSpots[npcIr]) && (!souta || longe(souta.transform.position)) && (!ren || longe(ren.transform.position)))
                MoveNpcs(npcIr);
        }

        void LigaDica()
        {
            if (!souta || souta.dica || DialogueBox.IsBusy) return;
            int v = Mathf.Clamp(Etapa(), 0, 3);
            if (v < dicaSouta.Length) souta.hintLevers = dicaSouta[v].falas;
            if (ren && v < dicaRen.Length) ren.hintLevers = dicaRen[v].falas;
            souta.dica = true;
            if (ren) ren.dica = true;
            if (Hud.Instance) Hud.Instance.Toast(dicaToast);
        }

        /// <summary>0 cadeado, 1 retenção, 2 fusíveis, 3 só atravessar.</summary>
        int Etapa()
        {
            if (!lockOpen) return 0;
            if (checkpoint == 1 && !retencaoUsada) return 1;
            if (checkpoint == 2 && !luzOk) return 2;
            return 3;
        }

        void OnTalked(NpcTalk npc)
        {
            if (souta) souta.dica = false;
            if (ren) ren.dica = false;
            Progresso();
        }

        void Progresso()
        {
            semProgresso = 0f;
        }

        // ---------- Morte e volta ----------

        IEnumerator Die(bool crushed)
        {
            if (dying) yield break;
            dying = true;
            mortesAqui++;
            player.controlEnabled = false;
            if (crushed)
            {
                crushes++;
                if (sfx && crushClip) sfx.PlayOneShot(crushClip);
                if (cameraFollow) cameraFollow.Shake(0.18f, 0.5f);
                StartCoroutine(Flash());
                if (Hud.Instance) Hud.Instance.Toast("A porta bateu em cima do Kai.");
                yield return new WaitForSeconds(0.15f);
                if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 0.25f);
            }
            else
            {
                falls++;
                if (sfx && fallClip) sfx.PlayOneShot(fallClip);
                if (Hud.Instance) Hud.Instance.Toast("O Kai caiu no vão entre os vagões.");
                if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 0.45f);
            }

            float cx = checkpoints.Length > 0 ? checkpoints[Mathf.Clamp(checkpoint, 0, checkpoints.Length - 1)] : player.transform.position.x;
            player.Teleport(new Vector3(cx, spawnY, 0f));
            yield return new WaitForFixedUpdate();
            yield return null;
            if (cameraFollow) cameraFollow.Snap();
            yield return new WaitForSeconds(0.35f);
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 0.6f);
            player.controlEnabled = true;
            dying = false;

            if (crushed && crushes == 1) Say(firstCrush);
            else if (!crushed && falls == 1) Say(firstFall);
        }

        IEnumerator Flash()
        {
            if (!flash) yield break;
            flash.alpha = 0.55f;
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                flash.alpha = Mathf.Lerp(0.55f, 0f, t / 0.5f);
                yield return null;
            }
            flash.alpha = 0f;
        }

        // ---------- Trem ----------

        void OnDoorSlam(CarDoor d)
        {
            if (!player || !cameraFollow) return;
            float dx = Mathf.Abs(player.transform.position.x - d.CenterX);
            if (dx < 7f) cameraFollow.Shake(0.05f * (1f - dx / 7f) + 0.01f, 0.3f);
        }

        IEnumerator LurchLoop()
        {
            while (!endingStarted)
            {
                yield return new WaitForSeconds(UnityEngine.Random.Range(lurchInterval.x, lurchInterval.y));
                if (endingStarted) yield break;
                if (!dying) Lurch(0.07f, 0.7f, 0.55f);
            }
        }

        void Lurch(float amount, float duration, float volume)
        {
            if (cameraFollow) cameraFollow.Shake(amount, duration);
            if (sfx && lurchClip) sfx.PlayOneShot(lurchClip, volume);
        }

        IEnumerator EndSequence()
        {
            endingStarted = true;
            while (DialogueBox.IsBusy || dying || Modal.IsOpen) yield return null;
            player.controlEnabled = false;
            MoveNpcs(3);

            // o trem freia na estação
            if (sfx && brakeClip) sfx.PlayOneShot(brakeClip, 0.9f);
            if (cameraFollow) cameraFollow.Shake(0.12f, 5.5f); // o freio de emergência dura uns 6 segundos até parar
            if (tunnel) tunnel.Moving = false;
            foreach (var d in doors)
                if (d) d.StopOpen();
            if (rumble) StartCoroutine(FadeAudio(rumble, 0f, 5f));
            yield return new WaitForSeconds(0.7f);
            yield return SayAndWait(braking);
            StartCoroutine(MostraEstacao(3f));
            yield return new WaitForSeconds(1.2f);
            if (stationAmbience)
            {
                stationAmbience.loop = true;
                stationAmbience.volume = 0f;
                stationAmbience.Play();
                StartCoroutine(FadeAudio(stationAmbience, stationVolume, 2f));
            }

            yield return SayAndWait(ending);
            if (exitDoor) exitDoor.Open();
            Objetivo(objectiveExit);
            player.controlEnabled = true;
            yield return new WaitForSeconds(0.8f);
            exitOpen = true;
        }

        IEnumerator MostraEstacao(float duracao)
        {
            float t = 0f;
            while (t < duracao)
            {
                t += Time.deltaTime;
                foreach (var e in estacao)
                    if (e) e.color = new Color(1f, 1f, 1f, Mathf.Clamp01(t / duracao) * 0.85f);
                yield return null;
            }
        }

        // ---------- Utilidades ----------

        void Objetivo(string texto)
        {
            if (Hud.Instance) Hud.Instance.SetObjective(texto);
        }

        IEnumerator SayAndWait(List<DialogueLine> lines)
        {
            if (!DialogueBox.Instance || lines == null || lines.Count == 0) yield break;
            bool done = false;
            DialogueBox.Instance.Play(lines, () => done = true);
            while (!done) yield return null;
        }

        void Say(List<DialogueLine> lines)
        {
            if (DialogueBox.Instance && lines != null && lines.Count > 0) DialogueBox.Instance.Play(lines);
        }

        IEnumerator Leave(string scene)
        {
            if (leaving) yield break;
            leaving = true;
            if (player) player.controlEnabled = false;
            if (rumble) StartCoroutine(FadeAudio(rumble, 0f, 1.2f));
            if (stationAmbience) StartCoroutine(FadeAudio(stationAmbience, 0f, 1.2f));
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 1.4f);

            if (!Application.CanStreamedLevelBeLoaded(scene) && scene == nextScene && Application.CanStreamedLevelBeLoaded(fallbackScene))
                scene = fallbackScene; // o Cenário 3 ainda não existe: segue direto para a sala de controle

            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                if (Hud.Instance) yield return Hud.Instance.ShowEnd(endTitle, endSubtitle);
                yield return new WaitForSeconds(3.5f);
                scene = menuScene;
            }
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
            else Debug.LogWarning($"[Cenário 2] A cena \"{scene}\" não está na lista de cenas do build.");
        }

        static IEnumerator FadeAudio(AudioSource src, float to, float duration)
        {
            float from = src.volume, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            src.volume = to;
        }
    }
}
