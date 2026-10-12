using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Platformer.Core;
using Platformer.Model;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Painel da trava de letras na porta do fundo. Mostra os espaços da palavra e as letras já
    /// encontradas. O jogador digita no teclado (ou clica nas letras / usa o controle).
    /// BACKSPACE apaga, ESC fecha. Quando todos os espaços estão preenchidos, a palavra é conferida.
    /// </summary>
    public class LetterLockUI : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        public CanvasGroup group;
        public RectTransform panel;
        public TMP_Text[] slots;
        public Image[] slotLines;
        public Button[] tiles;
        public TMP_Text[] tileLabels;
        public TMP_Text status;

        [Header("Cores")]
        public Color slotColor = new Color(0.91f, 0.88f, 0.83f, 1f);
        public Color okColor = new Color(0.55f, 1f, 0.6f, 1f);
        public Color wrongColor = new Color(1f, 0.25f, 0.2f, 1f);
        public Color tileColor = new Color(0.91f, 0.88f, 0.83f, 1f);
        public Color tileMissing = new Color(0.4f, 0.37f, 0.33f, 1f);
        public Color tileSelected = new Color(1f, 0.62f, 0.25f, 1f);

        [Header("Som")]
        public AudioSource sfx;
        public AudioClip typeClip;
        public AudioClip wrongClip;
        public AudioClip okClip;

        [NonSerialized] public LetterPuzzle puzzle;
        public event Action<string> Submitted;

        readonly StringBuilder typed = new StringBuilder();
        readonly List<char> available = new List<char>();
        int length = 5;
        int selected;
        bool busy;
        float openedAt;

        void Awake()
        {
            IsOpen = false;
            if (group)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            for (int i = 0; tiles != null && i < tiles.Length; i++)
            {
                int index = i;
                if (tiles[i]) tiles[i].onClick.AddListener(() => PickTile(index));
            }
        }

        void OnDestroy()
        {
            IsOpen = false;
            if (Keyboard.current != null) Keyboard.current.onTextInput -= OnTextInput;
        }

        public void SetAvailable(List<char> letters)
        {
            available.Clear();
            available.AddRange(letters);
            RefreshTiles();
        }

        public void Open(int wordLength)
        {
            if (IsOpen) return;
            length = Mathf.Clamp(wordLength, 1, slots != null ? slots.Length : wordLength);
            typed.Clear();
            busy = false;
            selected = 0;
            IsOpen = true;
            openedAt = Time.unscaledTime;
            SetControl(false);
            if (Keyboard.current != null) Keyboard.current.onTextInput += OnTextInput;
            if (status) status.text = available.Count < length
                ? $"Faltam letras ({available.Count}/{length}). Você pode tentar mesmo assim."
                : "Digite a palavra.";
            RefreshSlots(slotColor);
            RefreshTiles();
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
            if (Keyboard.current != null) Keyboard.current.onTextInput -= OnTextInput;
            if (group)
            {
                group.blocksRaycasts = false;
                group.interactable = false;
            }
            StartCoroutine(CloseRoutine());
        }

        IEnumerator CloseRoutine()
        {
            yield return Fade(0f, 0.2f);
            yield return null; // a tecla que fechou não vira pulo
            SetControl(true);
        }

        void Update()
        {
            if (!IsOpen || busy || Time.unscaledTime - openedAt < 0.2f) return;
            var k = Keyboard.current;
            var g = Gamepad.current;
            if ((k != null && k.escapeKey.wasPressedThisFrame) || (g != null && (g.buttonEast.wasPressedThisFrame || g.startButton.wasPressedThisFrame)))
            {
                Close();
                return;
            }
            if ((k != null && k.backspaceKey.wasPressedThisFrame) || (g != null && g.buttonWest.wasPressedThisFrame))
                Backspace();
            if (g != null && tiles != null && tiles.Length > 0)
            {
                if (g.dpad.left.wasPressedThisFrame) { selected = (selected + tiles.Length - 1) % tiles.Length; RefreshTiles(); }
                if (g.dpad.right.wasPressedThisFrame) { selected = (selected + 1) % tiles.Length; RefreshTiles(); }
                if (g.buttonSouth.wasPressedThisFrame) PickTile(selected);
            }
        }

        void OnTextInput(char c)
        {
            if (!IsOpen || busy || Time.unscaledTime - openedAt < 0.2f) return;
            char up = Normalize(c);
            if (up >= 'A' && up <= 'Z') Add(up);
        }

        void PickTile(int index)
        {
            if (!IsOpen || busy || index < 0 || index >= available.Count) return;
            Add(available[index]);
        }

        void Add(char c)
        {
            if (typed.Length >= length) return;
            typed.Append(c);
            if (sfx && typeClip) sfx.PlayOneShot(typeClip, 0.6f);
            RefreshSlots(slotColor);
            if (typed.Length == length)
            {
                busy = true;
                Submitted?.Invoke(typed.ToString());
            }
        }

        void Backspace()
        {
            if (typed.Length == 0) return;
            typed.Length--;
            RefreshSlots(slotColor);
        }

        /// <summary>Chamado pelo puzzle depois de conferir a palavra.</summary>
        public void ShowResult(bool ok)
        {
            StartCoroutine(ok ? OkRoutine() : WrongRoutine());
        }

        IEnumerator OkRoutine()
        {
            if (sfx && okClip) sfx.PlayOneShot(okClip);
            RefreshSlots(okColor);
            if (status) status.text = "DESTRAVADO";
            yield break;
        }

        IEnumerator WrongRoutine()
        {
            if (sfx && wrongClip) sfx.PlayOneShot(wrongClip);
            RefreshSlots(wrongColor);
            if (status) status.text = "Não é essa palavra.";
            Vector2 p0 = panel ? panel.anchoredPosition : Vector2.zero;
            float t = 0f;
            while (t < 0.45f)
            {
                t += Time.unscaledDeltaTime;
                if (panel) panel.anchoredPosition = p0 + new Vector2(Mathf.Sin(t * 70f) * 14f * (1f - t / 0.45f), 0f);
                yield return null;
            }
            if (panel) panel.anchoredPosition = p0;
            yield return new WaitForSecondsRealtime(0.35f);
            typed.Clear();
            RefreshSlots(slotColor);
            busy = false;
        }

        void RefreshSlots(Color color)
        {
            if (slots == null) return;
            for (int i = 0; i < slots.Length; i++)
            {
                bool used = i < length;
                if (slots[i])
                {
                    slots[i].gameObject.SetActive(used);
                    slots[i].text = i < typed.Length ? typed[i].ToString() : "";
                    slots[i].color = color;
                }
                if (slotLines != null && i < slotLines.Length && slotLines[i])
                {
                    slotLines[i].gameObject.SetActive(used);
                    var c = color;
                    c.a = 0.7f;
                    slotLines[i].color = c;
                }
            }
        }

        void RefreshTiles()
        {
            if (tiles == null) return;
            for (int i = 0; i < tiles.Length; i++)
            {
                bool has = i < available.Count;
                if (tiles[i]) tiles[i].interactable = has;
                if (tileLabels != null && i < tileLabels.Length && tileLabels[i])
                {
                    tileLabels[i].text = has ? available[i].ToString() : "?";
                    tileLabels[i].color = !has ? tileMissing : (Gamepad.current != null && i == selected ? tileSelected : tileColor);
                }
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

        static char Normalize(char c)
        {
            // tira acento (é → E, ó → O...) e deixa maiúscula
            string s = c.ToString().Normalize(NormalizationForm.FormD);
            foreach (char ch in s)
                if (CharUnicodeInfo.GetUnicodeCategory(ch) != UnicodeCategory.NonSpacingMark)
                    return char.ToUpperInvariant(ch);
            return char.ToUpperInvariant(c);
        }

        static void SetControl(bool enabled)
        {
            var player = Simulation.GetModel<PlatformerModel>().player;
            if (player) player.controlEnabled = enabled;
        }
    }
}
