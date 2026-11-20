using System.Collections;
using System.Collections.Generic;
using Platformer.Mechanics;
using TMPro;
using UltimaLinha.Salas;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Abertura (Cenário 0): diário → o Kai dorme no trem cheio e limpo → o vagão pisca em vermelho, alternando com o
    /// trem decrépito, até a piscada forte → ele acorda sozinho → tutorial (andar, pular, correr, diário, interagir)
    /// → o painel abre a porta da ponta → pulo com corrida até o outro vagão → Cenário 1, sem corte (o Cenário 1 começa
    /// no mesmo quadro em que este termina).
    /// </summary>
    public class Cenario0Director : MonoBehaviour
    {
        /// <summary>Só para testes automáticos: começa direto no trem, sem o diário (que espera o jogador).
        /// Fica em PlayerPrefs porque entrar em Play pode recarregar os scripts.</summary>
        public const string ChaveTeste = "UltimaLinha.Teste.PularDiario";

        public PlayerController player;
        public PersonagemPixel kai;
        public CameraFollow2D cameraFollow;
        public PortaPonta door;
        public ObjectSpot painel;

        [Header("Cutscene de abertura (diário)")]
        public IntroCutscene introCutscene;
        public bool playIntroCutscene = true;

        [Header("Os dois trens")]
        public SpriteRenderer tremLimpo;
        public GameObject tremAntigo;
        public GameObject passageiros;
        public CanvasGroup vermelho;
        [Tooltip("Cor do Kai em cada trem: ele recebe a luz do vagão em que está.")]
        public Color tomLimpo = Color.white, tomAntigo = Color.white;
        [Tooltip("Quanto a sombra do Kai sobe enquanto ele está sentado (os pés ficam na frente do banco).")]
        public float pesSentado;
        [Tooltip("Sombra do Kai no encosto e no assento enquanto ele está sentado.")]
        public GameObject sombraSentado;

        [Header("Entre os vagões")]
        [Tooltip("Onde ele volta depois de cair no vão.")]
        public Vector3 vaoInicio;
        [Tooltip("Limites da câmera: o vagão e o vão, lado a lado.")]
        public Rect boundsVagao;
        [Tooltip("Daqui para a direita é o vão (passou do pilar da ponta).")]
        public float inicioVao;
        [Tooltip("Abaixo disto o Kai caiu no vão.")]
        public float quedaY;
        [Tooltip("Pisando além disto, chegou ao outro vagão.")]
        public float chegadaX;
        [Tooltip("Onde ele para depois do pulo: o Cenário 1 começa com ele aqui.")]
        public float descansoX;
        [Tooltip("Onde termina a plataforma antes do vão.")]
        public float bordaVao;

        [Header("Física desta cena")]
        public Vector2 gravity = new Vector2(0f, -20f);

        [Header("Som")]
        public AudioSource ambience;
        public AudioSource sfx;
        public AudioClip piscaClip;
        public AudioClip freioClip;

        [Header("Cenas")]
        public string nextScene = "Cenario1";
        public string menuScene = "MainMenu";

        [Header("Falas")]
        public List<DialogueLine> acorda = new List<DialogueLine>();
        public List<DialogueLine> portaTrancada = new List<DialogueLine>();
        public List<DialogueLine> antesDoPulo = new List<DialogueLine>();
        public List<DialogueLine> primeiraQueda = new List<DialogueLine>();

        Vector2 previousGravity;
        bool leaving, noVao, voltando, dormindo, sentado;
        int quedas;

        // botão "Pular abertura" (diário + trem limpo + piscadas): vai direto para o Kai acordado no trem
        bool aberturaAcabou, pularPedido;
        Coroutine fadeEscuro;
        GameObject botaoPular;
        CanvasGroup grupoPular;
        float escSegurado, pularAte;
        const float PularDelay = 1f, PularSegundos = 4.5f, PularMouse = 3f;

        void Awake()
        {
            previousGravity = Physics2D.gravity;
            Physics2D.gravity = gravity;
            Time.timeScale = 1f;
        }

        void OnDestroy() => Physics2D.gravity = previousGravity;

        IEnumerator Start()
        {
            if (player) player.controlEnabled = false;
            if (vermelho) vermelho.alpha = 0f;
            if (cameraFollow)
            {
                cameraFollow.bounds = boundsVagao;
                cameraFollow.Snap();
            }
            if (door) door.onLockedInteract = () => Say(portaTrancada);
            if (painel) painel.Presented = (_, primeira) => { if (primeira && door) door.Open(); };

            bool continuing = GameSave.BeginScene(SceneManager.GetActiveScene().name);
            if (!continuing)
            {
                CriaBotaoPular();
                var abertura = StartCoroutine(Abertura());
                while (!aberturaAcabou && !pularPedido)
                {
                    if (introCutscene && introCutscene.WasSkipped) pularPedido = true; // segurou ESC no diário
                    yield return null;
                }
                if (!aberturaAcabou)
                {
                    StopCoroutine(abertura);
                    yield return PulaAbertura();
                }
                TiraBotaoPular();
            }
            else
            {
                Decrepito(true);
                if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 1.6f);
            }

            yield return SayAndWait(acorda);
            if (player) player.controlEnabled = true;
            GameSave.Save(SceneManager.GetActiveScene().name);
            yield return Tutorial();
        }

        void Update()
        {
            if (leaving) return;
            if (botaoPular)
            {
                // o botão fica na tela uns segundos e some; mexer o mouse traz de volta
                var mouse = UnityEngine.InputSystem.Mouse.current;
                if (mouse != null && mouse.delta.ReadValue().sqrMagnitude > 4f && Time.unscaledTime > PularDelay)
                    pularAte = Mathf.Max(pularAte, Time.unscaledTime + PularMouse);
                if (grupoPular)
                {
                    bool mostra = Time.unscaledTime < pularAte;
                    grupoPular.alpha = Mathf.MoveTowards(grupoPular.alpha, mostra ? 1f : 0f, Time.unscaledDeltaTime * (mostra ? 2f : 1.2f));
                    grupoPular.blocksRaycasts = grupoPular.interactable = grupoPular.alpha > 0.15f;
                }
                // abertura na tela: segurar ESC pula tudo (no diário quem conta é a própria cutscene)
                var k = UnityEngine.InputSystem.Keyboard.current;
                var g = UnityEngine.InputSystem.Gamepad.current;
                bool segura = (k != null && k.escapeKey.isPressed) || (g != null && g.startButton.isPressed);
                escSegurado = segura ? escSegurado + Time.unscaledDeltaTime : 0f;
                if (escSegurado >= 1.2f) pularPedido = true;
                return;
            }
            if (InputHelper.BackPressed() && !DialogueBox.IsBusy && !DiarioKai.IsOpen && !Modal.IsOpen && !IntroCutscene.Active)
                StartCoroutine(Leave(menuScene));
            if (!noVao && player && player.transform.position.x > inicioVao) StartCoroutine(EntraNoVao());
            if (!noVao || voltando || !player) return;
            if (player.transform.position.y < quedaY) StartCoroutine(Caiu());
            else if (player.transform.position.x > chegadaX && player.IsGrounded) StartCoroutine(Chegou());
        }

        // ------------------------------------------------------------------ o trem normal e as piscadas

        void Decrepito(bool sim)
        {
            if (tremLimpo) tremLimpo.gameObject.SetActive(!sim);
            if (tremAntigo) tremAntigo.SetActive(sim);
            if (passageiros) passageiros.SetActive(!sim);
            if (kai && kai.view) kai.view.color = sim ? tomAntigo : tomLimpo;
            // dormindo, ele também troca de roupa: limpo no trem limpo, encardido no de verdade
            if (dormindo && kai) kai.Tocar(sim ? "dorme" : "dormeLimpo", true);
        }

        void Sentado(bool sim)
        {
            if (sentado == sim) return;
            sentado = sim;
            if (kai && kai.shadow) kai.shadow.transform.localPosition += Vector3.up * (sim ? pesSentado : -pesSentado);
            if (kai && kai.contato) kai.contato.transform.localPosition += Vector3.up * (sim ? pesSentado : -pesSentado);
            if (kai && kai.reflexo) kai.reflexo.transform.localPosition += Vector3.up * (sim ? 2f * pesSentado : -2f * pesSentado);
            if (sombraSentado) sombraSentado.SetActive(sim);
        }

        IEnumerator TremNormal()
        {
            dormindo = true;
            Decrepito(false);
            Sentado(true);
            if (ambience)
            {
                ambience.loop = true;
                ambience.volume = 0.5f;
                ambience.Play();
            }
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 2.4f);
            yield return Balanca(3.0f);

            // cada piscada mostra o outro trem por mais tempo, e mais rápido
            float[] espera = { 1.4f, 1.1f, 0.9f, 0.7f, 0.55f, 0.4f, 0.3f, 0.22f, 0.16f };
            float[] dura = { 0.05f, 0.07f, 0.09f, 0.12f, 0.15f, 0.2f, 0.25f, 0.3f, 0.36f };
            for (int i = 0; i < espera.Length; i++)
            {
                yield return Balanca(espera[i]);
                if (vermelho) vermelho.alpha = 0.25f + 0.04f * i;
                Decrepito(true);
                if (sfx && piscaClip) sfx.PlayOneShot(piscaClip, 0.25f + 0.05f * i);
                yield return new WaitForSeconds(dura[i]);
                if (vermelho) vermelho.alpha = 0f;
                Decrepito(false);
            }

            // a piscada forte: ele "atravessa"
            if (vermelho) vermelho.alpha = 1f;
            Decrepito(true);
            if (ambience) ambience.Stop();
            if (sfx && freioClip) sfx.PlayOneShot(freioClip, 0.8f);
            if (cameraFollow) cameraFollow.Shake(0.25f, 0.6f);
            yield return new WaitForSeconds(0.5f);
            if (Hud.Instance) fadeEscuro = StartCoroutine(Hud.Instance.FadeScreen(1f, 1.2f));
            yield return Fade(vermelho, 0f, 1.6f);
            yield return new WaitForSeconds(1.8f); // silêncio no escuro
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 2.6f);
            yield return new WaitForSeconds(0.8f);
            // acorda sentado, olha em volta e levanta
            dormindo = false;
            if (kai)
            {
                float acordar = kai.Tocar("acorda", true);
                yield return new WaitForSeconds(acordar * 6f / 9f); // 6 dos 9 quadros sentado; depois ele levanta
                Sentado(false);
                yield return new WaitForSeconds(acordar * 3f / 9f);
                kai.Soltar();
            }
            yield return new WaitForSeconds(0.3f);
        }

        IEnumerator Balanca(float segundos)
        {
            float t = 0f;
            while (t < segundos)
            {
                if (cameraFollow) cameraFollow.Shake(0.02f, 0.25f);
                yield return new WaitForSeconds(0.25f);
                t += 0.25f;
            }
        }

        // ------------------------------------------------------------------ pular a abertura

        IEnumerator Abertura()
        {
            if (playIntroCutscene && introCutscene && PlayerPrefs.GetInt(ChaveTeste, 0) == 0) yield return introCutscene.Play();
            if (!pularPedido) yield return TremNormal();
            aberturaAcabou = true;
        }

        /// <summary>Corta o que estiver tocando da abertura e deixa o Kai acordado no trem de verdade, como no "Continuar".</summary>
        IEnumerator PulaAbertura()
        {
            TiraBotaoPular();
            if (introCutscene) introCutscene.Abort();
            if (fadeEscuro != null) StopCoroutine(fadeEscuro);
            if (Hud.Instance && Hud.Instance.fader) Hud.Instance.fader.alpha = 1f; // troca o trem por baixo do preto
            if (vermelho) vermelho.alpha = 0f;
            if (ambience) ambience.Stop();
            if (sfx) sfx.Stop();
            dormindo = false;
            Sentado(false);
            if (kai) kai.Soltar();
            Decrepito(true);
            if (cameraFollow) cameraFollow.Snap();
            yield return new WaitForSeconds(0.3f);
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 1.2f);
        }

        /// <summary>Botão no canto de baixo à direita, por cima de tudo (até do preto da abertura).</summary>
        void CriaBotaoPular()
        {
            if (botaoPular) return;
            if (!EventSystem.current)
            {
                var es = new GameObject("EventSystem", typeof(EventSystem), typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                es.transform.SetParent(transform, false);
            }
            botaoPular = new GameObject("PularAbertura", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler),
                typeof(GraphicRaycaster), typeof(CanvasGroup));
            botaoPular.layer = 5;
            var canvas = botaoPular.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 900;
            var scaler = botaoPular.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;
            var grupo = botaoPular.GetComponent<CanvasGroup>();
            grupo.alpha = 0f;
            grupoPular = grupo;
            pularAte = Time.unscaledTime + PularDelay + PularSegundos;

            var fonte = Hud.Instance && Hud.Instance.objectiveText ? Hud.Instance.objectiveText.font : TMP_Settings.defaultFontAsset;
            var cor = new Color(0.91f, 0.86f, 0.77f, 1f);

            var rt = new GameObject("Botao", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
            rt.gameObject.layer = 5;
            rt.SetParent(botaoPular.transform, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-40f, -36f);
            rt.sizeDelta = new Vector2(330f, 60f);
            var fundo = rt.GetComponent<Image>();
            fundo.color = new Color(0.07f, 0.06f, 0.055f, 0.85f);
            var borda = rt.gameObject.AddComponent<Outline>();
            borda.effectColor = new Color(cor.r, cor.g, cor.b, 0.45f);
            borda.effectDistance = new Vector2(2f, -2f);
            var botao = rt.GetComponent<Button>();
            botao.targetGraphic = fundo;
            var cores = botao.colors;
            cores.normalColor = new Color(0.8f, 0.8f, 0.8f, 1f);
            cores.highlightedColor = Color.white;
            cores.pressedColor = new Color(0.6f, 0.5f, 0.45f, 1f);
            botao.colors = cores;
            botao.navigation = new Navigation { mode = Navigation.Mode.None };
            botao.onClick.AddListener(() => pularPedido = true);

            var texto = new GameObject("Texto", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            texto.gameObject.layer = 5;
            texto.rectTransform.SetParent(rt, false);
            texto.rectTransform.anchorMin = Vector2.zero;
            texto.rectTransform.anchorMax = Vector2.one;
            texto.rectTransform.offsetMin = texto.rectTransform.offsetMax = Vector2.zero;
            if (fonte) texto.font = fonte;
            texto.text = "PULAR ABERTURA  ››";
            texto.fontSize = 24;
            texto.characterSpacing = 10;
            texto.color = cor;
            texto.alignment = TextAlignmentOptions.Center;
            texto.raycastTarget = false;

            var dica = new GameObject("Dica", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            dica.gameObject.layer = 5;
            dica.rectTransform.SetParent(botaoPular.transform, false);
            dica.rectTransform.anchorMin = dica.rectTransform.anchorMax = dica.rectTransform.pivot = new Vector2(1f, 1f);
            dica.rectTransform.anchoredPosition = new Vector2(-40f, -100f);
            dica.rectTransform.sizeDelta = new Vector2(330f, 26f);
            if (fonte) dica.font = fonte;
            dica.text = "ou segure ESC";
            dica.fontSize = 16;
            dica.characterSpacing = 6;
            dica.color = new Color(cor.r, cor.g, cor.b, 0.5f);
            dica.alignment = TextAlignmentOptions.Center;
            dica.raycastTarget = false;

        }

        void TiraBotaoPular()
        {
            if (botaoPular) Destroy(botaoPular);
            botaoPular = null;
            grupoPular = null;
        }

        // ------------------------------------------------------------------ tutorial

        IEnumerator Tutorial()
        {
            var hud = Hud.Instance;
            if (hud) hud.SetObjective("Descubra para onde todo mundo foi.");

            float x0 = player.transform.position.x;
            if (hud) hud.Toast("A  /  D   para andar", 7f);
            while (Mathf.Abs(player.transform.position.x - x0) < 2f) yield return null;

            if (hud) hud.Toast("ESPAÇO   para pular   ·   suba num banco", 9f);
            float chao = player.transform.position.y;
            while (!(player.IsGrounded && player.transform.position.y > chao + 1.4f)) yield return null;
            if (hud) hud.Toast("Segure  S  para descer do banco", 5f);
            yield return new WaitForSeconds(2.5f);

            if (hud) hud.Toast("Segure  SHIFT   para correr", 8f);
            float correndo = 0f;
            while (correndo < 0.6f)
            {
                correndo = player.Running ? correndo + Time.deltaTime : 0f;
                yield return null;
            }

            if (hud) hud.Toast("TAB   abre o diário do Kai", 6f);
            yield return new WaitForSeconds(3f);

            if (hud)
            {
                hud.SetObjective("Abra a porta da ponta do vagão.");
                hud.Toast("E   para interagir", 6f);
            }
            while (door && !door.IsOpen) yield return null;
            if (hud) hud.SetObjective("Atravesse para o próximo vagão.");
        }

        // ------------------------------------------------------------------ entre os vagões

        IEnumerator EntraNoVao()
        {
            noVao = true;
            player.controlEnabled = false;
            yield return SayAndWait(antesDoPulo);
            player.controlEnabled = true;
            if (Hud.Instance)
            {
                Hud.Instance.SetObjective("Pule para o outro vagão.");
                Hud.Instance.Toast("Segure SHIFT para correr e pule no fim da plataforma", 8f);
            }
        }

        IEnumerator Caiu()
        {
            voltando = true;
            player.controlEnabled = false;
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 0.5f);
            player.Teleport(vaoInicio);
            if (cameraFollow) cameraFollow.Snap();
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 0.6f);
            quedas++;
            if (quedas == 1) yield return SayAndWait(primeiraQueda);
            player.controlEnabled = true;
            voltando = false;
        }

        /// <summary>Do outro lado: ele para na plataforma, a câmera assenta e o Cenário 1 entra sem corte.</summary>
        IEnumerator Chegou()
        {
            if (leaving) yield break;
            leaving = true;
            yield return DirUtil.WalkPlayerTo(player, descansoX);
            yield return new WaitForSeconds(1.2f); // a câmera termina de assentar (SmoothDamp): o Cenário 1 começa neste quadro
            GameSave.Save(nextScene);
            if (Application.CanStreamedLevelBeLoaded(nextScene)) SceneManager.LoadScene(nextScene);
            else Debug.LogWarning($"[Cenário 0] A cena \"{nextScene}\" não está na lista de cenas do build.");
        }

        // ------------------------------------------------------------------ utilidades

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
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 1.2f);
            if (scene == nextScene) GameSave.Save(nextScene); // o "Continuar" já começa no vagão do Souta e do Ren
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
            else Debug.LogWarning($"[Cenário 0] A cena \"{scene}\" não está na lista de cenas do build.");
        }

        static IEnumerator Fade(CanvasGroup g, float to, float duration)
        {
            if (!g) yield break;
            float from = g.alpha, t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                g.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            g.alpha = to;
        }
    }
}
