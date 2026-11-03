using System;
using System.Collections;
using TMPro;
using UltimaLinha.Cenario1;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Escolha do jogador no estilo de Detroit: Become Human (final do Cenário 7).
    /// As opções aparecem flutuando ao lado do Kai, sem tela escura: um ícone de botão (△ ○ □ ✕) e um texto curto
    /// em maiúsculas. O tempo fica lento e a tela ganha um tom azul enquanto o jogador decide.
    /// ↑ ↓ (W/S) trocam, E/Enter/Espaço confirmam, 1 e 2 escolhem direto; no controle, △ e ○ escolhem direto.
    /// Também dá para clicar.
    /// </summary>
    public class ChoiceUI : MonoBehaviour
    {
        public static ChoiceUI Instance { get; private set; }

        public CanvasGroup group;
        [Tooltip("Tom azul por cima da cena enquanto a escolha está aberta.")]
        public CanvasGroup tint;
        [Tooltip("A lista de opções (segue o Kai na tela).")]
        public RectTransform list;
        public TMP_Text title;
        public RectTransform[] rows = new RectTransform[0];
        public Image[] icons = new Image[0];
        public TMP_Text[] labels = new TMP_Text[0];
        public Image[] bars = new Image[0];
        public Image[] underlines = new Image[0];
        public Button[] buttons = new Button[0];
        public TMP_Text hint;

        [Header("Onde aparece")]
        [Tooltip("A lista fica ao lado deste objeto (o Kai).")]
        public Transform anchor;
        public Vector2 worldOffset = new Vector2(1.1f, 2.3f);

        [Header("Ritmo")]
        [Tooltip("Velocidade do tempo enquanto o jogador decide.")]
        public float slowMotion = 0.3f;

        [Header("Cores")]
        public Color accent = new Color(0.38f, 0.75f, 1f, 1f);
        public Color textIdle = new Color(0.82f, 0.88f, 0.94f, 0.62f);
        public Color textSelected = new Color(1f, 1f, 1f, 1f);
        public Color barIdle = new Color(0.02f, 0.04f, 0.07f, 0.32f);
        public Color barSelected = new Color(0.06f, 0.16f, 0.26f, 0.6f);

        [Header("Som")]
        public AudioSource sfx;
        public AudioClip appearClip;
        public AudioClip moveClip;
        public AudioClip confirmClip;

        public bool IsOpen { get; private set; }

        Action<int> onChosen;
        int selected, count;
        float openedAt, previousTimeScale = 1f;
        bool closing;
        RectTransform rootRect;

        void Awake()
        {
            Instance = this;
            rootRect = transform as RectTransform;
            if (group)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            if (tint) tint.alpha = 0f;
            for (int i = 0; i < buttons.Length; i++)
            {
                int k = i;
                if (buttons[i]) buttons[i].onClick.AddListener(() => { if (IsOpen && !closing && k < count) { Select(k); Confirm(); } });
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (IsOpen)
            {
                Modal.Pop();
                Time.timeScale = previousTimeScale;
            }
        }

        public void Show(string heading, string[] options, string[] notes, Action<int> chosen)
        {
            if (IsOpen) return;
            IsOpen = true;
            closing = false;
            onChosen = chosen;
            openedAt = Time.unscaledTime;
            Modal.Push();
            Modal.SetControl(false);
            count = Mathf.Min(options != null ? options.Length : 0, rows.Length);
            if (title)
            {
                title.text = string.IsNullOrEmpty(heading) ? "" : heading.ToUpperInvariant();
                title.gameObject.SetActive(!string.IsNullOrEmpty(heading));
            }
            for (int i = 0; i < rows.Length; i++)
            {
                if (rows[i]) rows[i].gameObject.SetActive(i < count);
                if (i < count && i < labels.Length && labels[i]) labels[i].text = options[i].ToUpperInvariant();
            }
            selected = 0;
            Follow();
            Refresh(0f);
            previousTimeScale = Time.timeScale > 0f ? Time.timeScale : 1f;
            StopAllCoroutines();
            StartCoroutine(Appear());
            if (sfx && appearClip) sfx.PlayOneShot(appearClip, 0.8f);
            if (group)
            {
                group.blocksRaycasts = true;
                group.interactable = true;
            }
        }

        IEnumerator Appear()
        {
            // o tempo desacelera e as opções entram uma de cada vez, deslizando um pouco
            float t = 0f, dur = 0.55f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / dur);
                Time.timeScale = Mathf.Lerp(previousTimeScale, slowMotion, k);
                if (group) group.alpha = Mathf.SmoothStep(0f, 1f, k);
                if (tint) tint.alpha = Mathf.SmoothStep(0f, 1f, k);
                for (int i = 0; i < count; i++)
                {
                    float ki = Mathf.Clamp01((t - i * 0.09f) / (dur - 0.15f));
                    if (rows[i]) rows[i].localScale = Vector3.one;
                    SetRowOffset(i, Mathf.Lerp(-28f, 0f, Mathf.SmoothStep(0f, 1f, ki)), ki);
                }
                yield return null;
            }
            Time.timeScale = slowMotion;
            for (int i = 0; i < count; i++) SetRowOffset(i, 0f, 1f);
        }

        void SetRowOffset(int i, float x, float alpha)
        {
            if (i >= rows.Length || !rows[i]) return;
            var p = rows[i].anchoredPosition;
            p.x = x;
            rows[i].anchoredPosition = p;
            var cg = rows[i].GetComponent<CanvasGroup>();
            if (cg) cg.alpha = alpha;
        }

        void LateUpdate()
        {
            if (!IsOpen) return;
            Follow();
            Refresh(Time.unscaledTime - openedAt);
        }

        /// <summary>Põe a lista ao lado do Kai, sem sair da tela.</summary>
        void Follow()
        {
            if (!list || !anchor || !rootRect) return;
            var cam = Camera.main;
            if (!cam) return;
            Vector3 screen = cam.WorldToScreenPoint(anchor.position + (Vector3)worldOffset);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(rootRect, screen, null, out var local)) return;
            var r = rootRect.rect;
            local.x = Mathf.Clamp(local.x, r.xMin + 40f, r.xMax - list.rect.width - 40f);
            local.y = Mathf.Clamp(local.y, r.yMin + list.rect.height * 0.5f + 40f, r.yMax - list.rect.height * 0.5f - 40f);
            list.anchoredPosition = local;
        }

        void Update()
        {
            if (!IsOpen || closing || Time.unscaledTime - openedAt < 0.5f) return;
            var k = Keyboard.current;
            var g = Gamepad.current;
            int dir = 0;
            if (k != null)
            {
                if (k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame) dir = -1;
                if (k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame) dir = 1;
                if (k.digit1Key.wasPressedThisFrame || k.numpad1Key.wasPressedThisFrame) { Pick(0); return; }
                if (k.digit2Key.wasPressedThisFrame || k.numpad2Key.wasPressedThisFrame) { Pick(1); return; }
                if (k.digit3Key.wasPressedThisFrame || k.numpad3Key.wasPressedThisFrame) { Pick(2); return; }
                if (k.digit4Key.wasPressedThisFrame || k.numpad4Key.wasPressedThisFrame) { Pick(3); return; }
            }
            if (g != null)
            {
                if (g.dpad.up.wasPressedThisFrame || g.dpad.left.wasPressedThisFrame) dir = -1;
                if (g.dpad.down.wasPressedThisFrame || g.dpad.right.wasPressedThisFrame) dir = 1;
                // os ícones são os do controle: △ = primeira opção, ○ = segunda, □ = terceira, ✕ = quarta
                if (g.buttonNorth.wasPressedThisFrame) { Pick(0); return; }
                if (g.buttonEast.wasPressedThisFrame) { Pick(1); return; }
                if (g.buttonWest.wasPressedThisFrame) { Pick(2); return; }
            }
            if (dir != 0) Select(selected + dir);

            bool confirm = (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame))
                           || (g != null && g.buttonSouth.wasPressedThisFrame);
            if (confirm) Confirm();
        }

        void Pick(int index)
        {
            if (index < 0 || index >= count) return;
            Select(index);
            Confirm();
        }

        void Select(int index)
        {
            int n = Mathf.Max(1, count);
            int next = ((index % n) + n) % n;
            if (next != selected && sfx && moveClip) sfx.PlayOneShot(moveClip, 0.6f);
            selected = next;
        }

        void Confirm()
        {
            if (closing) return;
            closing = true;
            if (sfx && confirmClip) sfx.PlayOneShot(confirmClip);
            StartCoroutine(CloseRoutine());
        }

        IEnumerator CloseRoutine()
        {
            if (group)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            // a opção escolhida acende e cresce um pouco; as outras somem
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.45f);
                for (int i = 0; i < count; i++)
                {
                    if (!rows[i]) continue;
                    var cg = rows[i].GetComponent<CanvasGroup>();
                    if (i == selected)
                    {
                        rows[i].localScale = Vector3.one * Mathf.Lerp(1f, 1.08f, Mathf.Sin(k * Mathf.PI * 0.5f));
                        if (i < bars.Length && bars[i]) bars[i].color = Color.Lerp(new Color(1f, 1f, 1f, 0.85f), barSelected, k);
                    }
                    else if (cg) cg.alpha = 1f - k;
                }
                yield return null;
            }
            yield return new WaitForSecondsRealtime(0.25f);
            t = 0f;
            float fromScale = Time.timeScale;
            while (t < 0.4f)
            {
                t += Time.unscaledDeltaTime;
                float k = Mathf.Clamp01(t / 0.4f);
                if (group) group.alpha = 1f - k;
                if (tint) tint.alpha = 1f - k;
                Time.timeScale = Mathf.Lerp(fromScale, previousTimeScale, k);
                yield return null;
            }
            if (group) group.alpha = 0f;
            if (tint) tint.alpha = 0f;
            Time.timeScale = previousTimeScale;
            IsOpen = false;
            Modal.Pop();
            var cb = onChosen;
            onChosen = null;
            cb?.Invoke(selected);
        }

        void Refresh(float time)
        {
            for (int i = 0; i < count; i++)
            {
                bool on = i == selected;
                if (i < labels.Length && labels[i]) labels[i].color = on ? textSelected : textIdle;
                if (i < icons.Length && icons[i])
                {
                    var c = on ? accent : textIdle;
                    if (on) c = Color.Lerp(accent, Color.white, 0.25f + 0.25f * Mathf.Sin(time * 5f));
                    icons[i].color = c;
                }
                if (!closing && i < bars.Length && bars[i]) bars[i].color = on ? barSelected : barIdle;
                if (i < underlines.Length && underlines[i])
                {
                    var rt = underlines[i].rectTransform;
                    var s = rt.localScale;
                    s.x = Mathf.MoveTowards(s.x, on ? 1f : 0f, Time.unscaledDeltaTime * 6f);
                    rt.localScale = s;
                    underlines[i].color = accent;
                }
            }
            if (hint) hint.alpha = Mathf.Clamp01((time - 0.8f) * 2f) * 0.6f;
        }
    }
}
