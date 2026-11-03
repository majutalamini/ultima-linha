using System;
using System.Collections;
using System.Collections.Generic;
using Platformer.Core;
using Platformer.Model;
using TMPro;
using UltimaLinha.Cenario1;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UltimaLinha.Salas
{
    /// <summary>Conta quantas janelas (documento, cadeado) estão abertas: enquanto houver, o Kai não interage com nada.</summary>
    public static class Modal
    {
        public static int Count { get; private set; }
        public static bool IsOpen => Count > 0;

        public static void Push() => Count++;
        public static void Pop() => Count = Mathf.Max(0, Count - 1);
        public static void Reset() => Count = 0;

        /// <summary>Liga ou desliga o controle do Kai (só se houver jogador).</summary>
        public static void SetControl(bool enabled)
        {
            var model = Simulation.GetModel<PlatformerModel>();
            if (model != null && model.player) model.player.controlEnabled = enabled;
        }
    }

    /// <summary>
    /// Itens e fatos que passam de um cenário para o outro (guardados no PlayerPrefs):
    /// a alavanca que falta (achada no Cenário 5) e a chave de cada sala.
    /// </summary>
    public static class GameFlags
    {
        const string Prefix = "UltimaLinha.Flag.";

        public const string MissingLever = "alavanca_faltando";
        public const string Cenario4Key = "chave_cenario4";
        public const string Cenario5Key = "chave_cenario5";
        public const string NewsRead = "jornal_lido";

        public static bool Get(string flag) => PlayerPrefs.GetInt(Prefix + flag, 0) == 1;

        public static void Set(string flag, bool value = true)
        {
            PlayerPrefs.SetInt(Prefix + flag, value ? 1 : 0);
            PlayerPrefs.Save();
        }

        public static void ClearAll()
        {
            foreach (var f in new[] { MissingLever, Cenario4Key, Cenario5Key, NewsRead })
                PlayerPrefs.DeleteKey(Prefix + f);
            PlayerPrefs.Save();
        }
    }

    /// <summary>
    /// Lugar onde o Kai olha ou vasculha apertando E: gaveta, arquivo, bilhete, relógio, o corpo...
    /// Mostra um documento (se tiver) e depois as falas, e avisa o diretor da cena (Presented).
    /// A primeira vez usa "first"; as seguintes usam "again" (se vazio, repete "first").
    /// </summary>
    public class ObjectSpot : Interactable
    {
        public string id;
        [Tooltip("Desligado: o Kai ainda não pode usar (o diretor liga na hora certa).")]
        public bool available = true;
        public List<DialogueLine> first = new List<DialogueLine>();
        public List<DialogueLine> again = new List<DialogueLine>();
        [Header("Documento (opcional)")]
        public string docTitle;
        [TextArea(3, 12)] public string docBody;
        public string docFooter;
        [Tooltip("Mostra o documento só na primeira vez.")]
        public bool docOnlyOnce;
        [Header("Visual")]
        [Tooltip("Aparece depois da primeira vez (gaveta aberta).")]
        public GameObject openVisual;
        public AudioSource sfx;
        public AudioClip useClip;

        /// <summary>Chamado quando o documento e as falas acabaram. O parâmetro diz se foi a primeira vez.</summary>
        [NonSerialized] public Action<ObjectSpot, bool> Presented;
        /// <summary>Chamado assim que o Kai aperta E (antes de mostrar qualquer coisa).</summary>
        [NonSerialized] public Func<ObjectSpot, bool> Intercept;

        public int Uses { get; private set; }
        public bool Busy { get; private set; }

        public override bool CanInteract => base.CanInteract && available && !Busy && !DialogueBox.IsBusy && !Modal.IsOpen;

        public override void Interact()
        {
            if (!CanInteract) return;
            if (PersonagemPixel.Jogador) PersonagemPixel.Jogador.Tocar("search");
            if (Intercept != null && Intercept(this)) return;
            StartCoroutine(Present());
        }

        public IEnumerator Present()
        {
            Busy = true;
            bool firstTime = Uses == 0;
            Uses++;
            if (sfx && useClip) sfx.PlayOneShot(useClip);
            if (firstTime && openVisual) openVisual.SetActive(true);

            if (!string.IsNullOrEmpty(docBody) && (firstTime || !docOnlyOnce) && DocumentView.Instance)
            {
                bool closed = false;
                DocumentView.Instance.Show(docTitle, docBody, docFooter, () => closed = true);
                while (!closed) yield return null;
            }

            var lines = !firstTime && again.Count > 0 ? again : first;
            if (lines.Count > 0 && DialogueBox.Instance)
            {
                bool done = false;
                DialogueBox.Instance.Play(lines, () => done = true);
                while (!done) yield return null;
            }
            Busy = false;
            Presented?.Invoke(this, firstTime);
        }
    }

    /// <summary>Papel lido na tela inteira: bilhete, recorte de jornal. E, Espaço, Enter, Esc ou clique fecham.</summary>
    public class DocumentView : MonoBehaviour
    {
        public static DocumentView Instance { get; private set; }

        public CanvasGroup group;
        public RectTransform paper;
        public TMP_Text title;
        public TMP_Text body;
        public TMP_Text footer;
        public TMP_Text hint;
        public AudioSource sfx;
        public AudioClip openClip;

        Action onClose;
        float openedAt;
        bool open;

        public bool IsOpen => open;

        void Awake()
        {
            Instance = this;
            if (group)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (open) Modal.Pop();
        }

        public void Show(string heading, string text, string foot, Action closed)
        {
            if (open) return;
            open = true;
            onClose = closed;
            openedAt = Time.unscaledTime;
            Modal.Push();
            Modal.SetControl(false);
            if (title) title.text = heading ?? "";
            if (body) body.text = text ?? "";
            if (footer) footer.text = foot ?? "";
            if (sfx && openClip) sfx.PlayOneShot(openClip);
            StopAllCoroutines();
            StartCoroutine(Fade(1f, 0.25f));
            if (group) group.blocksRaycasts = true;
        }

        void Update()
        {
            if (!open || Time.unscaledTime - openedAt < 0.35f) return;
            if (InputHelper.AdvancePressed() || InputHelper.BackPressed()) Close();
        }

        void Close()
        {
            if (!open) return;
            open = false;
            if (group) group.blocksRaycasts = false;
            StopAllCoroutines();
            StartCoroutine(CloseRoutine());
        }

        IEnumerator CloseRoutine()
        {
            yield return Fade(0f, 0.2f);
            yield return null; // a tecla que fechou não vira pulo
            Modal.Pop();
            if (!Modal.IsOpen && !DialogueBox.IsBusy) Modal.SetControl(true);
            var cb = onClose;
            onClose = null;
            cb?.Invoke();
        }

        IEnumerator Fade(float to, float duration)
        {
            if (!group) yield break;
            float from = group.alpha, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            group.alpha = to;
        }
    }

    /// <summary>
    /// Alavanca do painel de energia. Começa abaixada; E levanta e ela fica levantada.
    /// </summary>
    public class PowerLever : Interactable
    {
        public Transform handle;
        public float upAngle = 38f;
        public float downAngle = -38f;
        public SpriteRenderer lamp;
        public SpriteRenderer lampGlow;
        public AudioSource sfx;
        public AudioClip pullClip;
        public Color offColor = new Color(0.55f, 0.08f, 0.06f, 1f);
        public Color onColor = new Color(0.45f, 1f, 0.55f, 1f);

        public bool IsUp { get; private set; }
        [NonSerialized] public Action<PowerLever> Raised;

        bool moving;

        public override bool CanInteract => base.CanInteract && !IsUp && !moving && !DialogueBox.IsBusy && !Modal.IsOpen;

        void Start()
        {
            if (handle) handle.localRotation = Quaternion.Euler(0f, 0f, downAngle);
            SetLamp(offColor, 0.35f);
        }

        void Update()
        {
            if (!IsUp && lamp) SetLamp(offColor, 0.2f + 0.15f * Mathf.Sin(Time.time * 2.4f + transform.position.x));
        }

        public override void Interact()
        {
            if (!CanInteract) return;
            StartCoroutine(Pull());
        }

        IEnumerator Pull()
        {
            moving = true;
            if (PersonagemPixel.Jogador) PersonagemPixel.Jogador.Tocar("lever");
            if (sfx && pullClip) sfx.PlayOneShot(pullClip);
            float t = 0f, d = 0.45f;
            while (t < d)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / d);
                if (handle) handle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(downAngle, upAngle, k));
                yield return null;
            }
            if (handle) handle.localRotation = Quaternion.Euler(0f, 0f, upAngle);
            IsUp = true;
            moving = false;
            SetLamp(onColor, 0.6f);
            Raised?.Invoke(this);
        }

        void SetLamp(Color c, float glow)
        {
            c.a = 1f;
            if (lamp) lamp.color = c;
            if (lampGlow)
            {
                c.a = glow;
                lampGlow.color = c;
            }
        }
    }

    /// <summary>
    /// Cadeado de combinação estilo Resident Evil: quatro rodas de número. Esquerda/direita escolhem a roda,
    /// cima/baixo mudam o número, Enter confere. Esc fecha. Também dá para clicar nas setas.
    /// </summary>
    public class DigitLockUI : MonoBehaviour
    {
        public static DigitLockUI Instance { get; private set; }

        public CanvasGroup group;
        public RectTransform panel;
        public TMP_Text[] digits;      // número do meio de cada roda
        public TMP_Text[] above;       // número de cima (apagado)
        public TMP_Text[] below;       // número de baixo (apagado)
        public UnityEngine.UI.Image[] frames; // moldura de cada roda (destaca a escolhida)
        public UnityEngine.UI.Button[] upButtons;
        public UnityEngine.UI.Button[] downButtons;
        public TMP_Text status;

        public Color normal = new Color(0.91f, 0.88f, 0.83f, 1f);
        public Color faded = new Color(0.55f, 0.52f, 0.48f, 0.55f);
        public Color selectedFrame = new Color(1f, 0.62f, 0.25f, 0.95f);
        public Color idleFrame = new Color(0.47f, 0.43f, 0.36f, 0.7f);
        public Color okColor = new Color(0.55f, 1f, 0.6f, 1f);
        public Color wrongColor = new Color(1f, 0.25f, 0.2f, 1f);

        public AudioSource sfx;
        public AudioClip tickClip;
        public AudioClip wrongClip;
        public AudioClip okClip;

        public event Action<string> Submitted;
        public bool IsOpen { get; private set; }

        int[] values = new int[4];
        int selected;
        bool busy;
        float openedAt;

        void Awake()
        {
            Instance = this;
            if (group)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            for (int i = 0; i < 4; i++)
            {
                int w = i;
                if (upButtons != null && i < upButtons.Length && upButtons[i]) upButtons[i].onClick.AddListener(() => Step(w, 1));
                if (downButtons != null && i < downButtons.Length && downButtons[i]) downButtons[i].onClick.AddListener(() => Step(w, -1));
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (IsOpen) Modal.Pop();
        }

        public void Open(string startAt = "0000")
        {
            if (IsOpen) return;
            for (int i = 0; i < 4; i++) values[i] = i < startAt.Length ? Mathf.Clamp(startAt[i] - '0', 0, 9) : 0;
            selected = 0;
            busy = false;
            IsOpen = true;
            openedAt = Time.unscaledTime;
            Modal.Push();
            Modal.SetControl(false);
            if (status) status.text = "Gire os números e confirme.";
            Refresh(normal);
            StopAllCoroutines();
            StartCoroutine(Fade(1f, 0.2f));
            if (group)
            {
                group.blocksRaycasts = true;
                group.interactable = true;
            }
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (group)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            StopAllCoroutines();
            StartCoroutine(CloseRoutine());
        }

        IEnumerator CloseRoutine()
        {
            yield return Fade(0f, 0.2f);
            yield return null;
            Modal.Pop();
            if (!Modal.IsOpen && !DialogueBox.IsBusy) Modal.SetControl(true);
        }

        void Update()
        {
            if (!IsOpen || busy || Time.unscaledTime - openedAt < 0.2f) return;
            var k = Keyboard.current;
            var g = Gamepad.current;
            if (k != null)
            {
                if (k.escapeKey.wasPressedThisFrame) { Close(); return; }
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) Select(selected - 1);
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) Select(selected + 1);
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) Step(selected, 1);
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) Step(selected, -1);
                if (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame) Submit();
                for (int d = 0; d <= 9; d++) // também dá para digitar o número direto
                {
                    // no enum das teclas o 0 vem depois do 9
                    var top = k[d == 0 ? Key.Digit0 : (Key)((int)Key.Digit1 + d - 1)];
                    var pad = k[d == 0 ? Key.Numpad0 : (Key)((int)Key.Numpad1 + d - 1)];
                    if (top.wasPressedThisFrame || pad.wasPressedThisFrame)
                    {
                        Set(selected, d);
                        Select(selected + 1 > 3 ? 3 : selected + 1);
                    }
                }
            }
            if (g != null)
            {
                if (g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame) { Close(); return; }
                if (g.dpad.left.wasPressedThisFrame) Select(selected - 1);
                if (g.dpad.right.wasPressedThisFrame) Select(selected + 1);
                if (g.dpad.up.wasPressedThisFrame) Step(selected, 1);
                if (g.dpad.down.wasPressedThisFrame) Step(selected, -1);
                if (g.buttonSouth.wasPressedThisFrame) Submit();
            }
        }

        void Select(int index)
        {
            selected = Mathf.Clamp(index, 0, 3);
            Refresh(normal);
        }

        void Set(int wheel, int value)
        {
            values[wheel] = ((value % 10) + 10) % 10;
            if (sfx && tickClip) sfx.PlayOneShot(tickClip, 0.8f);
            Refresh(normal);
        }

        void Step(int wheel, int dir)
        {
            if (!IsOpen || busy) return;
            selected = wheel;
            Set(wheel, values[wheel] + dir);
        }

        void Submit()
        {
            if (busy) return;
            busy = true;
            Submitted?.Invoke(Code);
        }

        public string Code => $"{values[0]}{values[1]}{values[2]}{values[3]}";

        /// <summary>Chamado pelo diretor depois de conferir o código.</summary>
        public void ShowResult(bool ok)
        {
            StartCoroutine(ok ? OkRoutine() : WrongRoutine());
        }

        IEnumerator OkRoutine()
        {
            if (sfx && okClip) sfx.PlayOneShot(okClip);
            Refresh(okColor);
            if (status) status.text = "ABERTO";
            yield break;
        }

        IEnumerator WrongRoutine()
        {
            if (sfx && wrongClip) sfx.PlayOneShot(wrongClip);
            Refresh(wrongColor);
            if (status) status.text = "Não abriu.";
            Vector2 p0 = panel ? panel.anchoredPosition : Vector2.zero;
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                if (panel) panel.anchoredPosition = p0 + new Vector2(Mathf.Sin(t * 70f) * 14f * (1f - t / 0.45f), 0f);
                yield return null;
            }
            if (panel) panel.anchoredPosition = p0;
            yield return new WaitForSecondsRealtime(0.25f);
            Refresh(normal);
            if (status) status.text = "Gire os números e confirme.";
            busy = false;
        }

        void Refresh(Color main)
        {
            for (int i = 0; i < 4; i++)
            {
                int v = values[i];
                if (digits != null && i < digits.Length && digits[i]) { digits[i].text = v.ToString(); digits[i].color = main; }
                if (above != null && i < above.Length && above[i]) { above[i].text = ((v + 1) % 10).ToString(); above[i].color = faded; }
                if (below != null && i < below.Length && below[i]) { below[i].text = ((v + 9) % 10).ToString(); below[i].color = faded; }
                if (frames != null && i < frames.Length && frames[i])
                    frames[i].color = main != normal ? main : (i == selected ? selectedFrame : idleFrame);
            }
        }

        IEnumerator Fade(float to, float duration)
        {
            if (!group) yield break;
            float from = group.alpha, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            group.alpha = to;
        }
    }
}
