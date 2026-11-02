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
    /// Escolha do jogador na tela inteira (final do Cenário 7): duas opções lado a lado.
    /// ← → (ou A/D, ↑ ↓) trocam, Enter/E/Espaço confirmam; também dá para clicar.
    /// </summary>
    public class ChoiceUI : MonoBehaviour
    {
        public static ChoiceUI Instance { get; private set; }

        public CanvasGroup group;
        public TMP_Text title;
        public TMP_Text[] labels = new TMP_Text[0];
        public TMP_Text[] details = new TMP_Text[0];
        public Image[] frames = new Image[0];
        public Button[] buttons = new Button[0];
        public AudioSource sfx;
        public AudioClip moveClip;
        public AudioClip confirmClip;

        public Color selectedFrame = new Color(0.75f, 0.12f, 0.1f, 0.95f);
        public Color idleFrame = new Color(0.47f, 0.43f, 0.36f, 0.55f);
        public Color selectedText = new Color(0.95f, 0.92f, 0.86f, 1f);
        public Color idleText = new Color(0.6f, 0.58f, 0.54f, 1f);

        public bool IsOpen { get; private set; }

        Action<int> onChosen;
        int selected;
        float openedAt;
        bool closing;

        void Awake()
        {
            Instance = this;
            if (group)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            for (int i = 0; i < buttons.Length; i++)
            {
                int k = i;
                if (buttons[i]) buttons[i].onClick.AddListener(() => { if (IsOpen && !closing) { Select(k); Confirm(); } });
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (IsOpen) Modal.Pop();
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
            if (title) title.text = heading ?? "";
            for (int i = 0; i < labels.Length; i++)
                if (labels[i]) labels[i].text = options != null && i < options.Length ? options[i] : "";
            for (int i = 0; i < details.Length; i++)
                if (details[i]) details[i].text = notes != null && i < notes.Length ? notes[i] : "";
            selected = 0;
            Refresh();
            StopAllCoroutines();
            StartCoroutine(Fade(1f, 0.6f));
            if (group)
            {
                group.blocksRaycasts = true;
                group.interactable = true;
            }
        }

        void Update()
        {
            if (!IsOpen || closing || Time.unscaledTime - openedAt < 0.6f) return;
            var k = Keyboard.current;
            var g = Gamepad.current;
            int dir = 0;
            if (k != null)
            {
                if (k.leftArrowKey.wasPressedThisFrame || k.aKey.wasPressedThisFrame || k.upArrowKey.wasPressedThisFrame || k.wKey.wasPressedThisFrame) dir = -1;
                if (k.rightArrowKey.wasPressedThisFrame || k.dKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame) dir = 1;
            }
            if (g != null)
            {
                if (g.dpad.left.wasPressedThisFrame || g.dpad.up.wasPressedThisFrame) dir = -1;
                if (g.dpad.right.wasPressedThisFrame || g.dpad.down.wasPressedThisFrame) dir = 1;
            }
            if (dir != 0) Select(selected + dir);

            bool confirm = (k != null && (k.enterKey.wasPressedThisFrame || k.numpadEnterKey.wasPressedThisFrame || k.spaceKey.wasPressedThisFrame || k.eKey.wasPressedThisFrame))
                           || (g != null && g.buttonSouth.wasPressedThisFrame);
            if (confirm) Confirm();
        }

        void Select(int index)
        {
            int n = Mathf.Max(1, labels.Length);
            int next = ((index % n) + n) % n;
            if (next != selected && sfx && moveClip) sfx.PlayOneShot(moveClip, 0.7f);
            selected = next;
            Refresh();
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
            Refresh();
            yield return new WaitForSecondsRealtime(0.5f);
            if (group)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            yield return Fade(0f, 0.5f);
            IsOpen = false;
            Modal.Pop();
            var cb = onChosen;
            onChosen = null;
            cb?.Invoke(selected);
        }

        void Refresh()
        {
            for (int i = 0; i < labels.Length; i++)
            {
                bool on = i == selected;
                if (labels[i]) labels[i].color = on ? selectedText : idleText;
                if (i < details.Length && details[i]) details[i].color = on ? new Color(selectedText.r, selectedText.g, selectedText.b, 0.75f) : new Color(idleText.r, idleText.g, idleText.b, 0.5f);
                if (i < frames.Length && frames[i]) frames[i].color = on ? selectedFrame : idleFrame;
            }
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
}
