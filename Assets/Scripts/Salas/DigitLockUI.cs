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
