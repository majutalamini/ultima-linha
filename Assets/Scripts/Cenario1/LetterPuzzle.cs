using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using TMPro;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Segundo puzzle do Cenário 1 (depois das alavancas): as luzes apagam e, quando voltam,
    /// há letras escritas pelo vagão. O Kai junta todas e digita a palavra secreta na trava da porta.
    /// </summary>
    public class LetterPuzzle : MonoBehaviour
    {
        [Tooltip("Palavra que abre a porta.")]
        public string secretWord = "MORTE";
        public LetterPickup[] letters;
        public LetterLockUI lockUI;
        public ExitDoor door;

        [Header("HUD: letras encontradas")]
        public CanvasGroup hudGroup;
        public TMP_Text hudText;

        [Header("Apagão antes das letras aparecerem")]
        public FluorescentLight[] lights;
        public AudioSource sfx;
        public AudioClip blackoutClip;
        public AudioClip pickupClip;

        public bool IsActive { get; private set; }
        public bool IsSolved { get; private set; }
        public int Found => found.Count;
        public bool AllFound => letters != null && found.Count >= letters.Length;

        public event Action Revealed;
        public event Action<char> Collected;
        public event Action AllCollected;
        public event Action Solved;

        readonly List<char> found = new List<char>();

        void Start()
        {
            if (hudGroup) hudGroup.alpha = 0f;
            if (lockUI)
            {
                lockUI.puzzle = this;
                lockUI.Submitted += OnSubmitted;
            }
        }

        /// <summary>Chamado quando as alavancas são resolvidas.</summary>
        public void Activate()
        {
            if (IsActive) return;
            IsActive = true;
            StartCoroutine(Blackout());
        }

        IEnumerator Blackout()
        {
            yield return new WaitForSeconds(1.2f);
            if (sfx && blackoutClip) sfx.PlayOneShot(blackoutClip);
            // as luzes morrem...
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0.92f, 0.25f);
            yield return new WaitForSeconds(0.35f);
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0.4f, 0.08f);
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0.97f, 0.08f);
            yield return new WaitForSeconds(1.1f);
            // ...e quando voltam, as letras estão lá
            for (int i = 0; letters != null && i < letters.Length; i++)
                if (letters[i]) letters[i].Reveal(0.1f + 0.15f * i);
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 1.6f);
            UpdateHud();
            if (hudGroup) StartCoroutine(FadeGroup(hudGroup, 1f, 0.8f));
            Revealed?.Invoke();
        }

        public void OnCollected(LetterPickup pickup)
        {
            found.Add(char.ToUpperInvariant(pickup.letter));
            if (sfx && pickupClip) sfx.PlayOneShot(pickupClip, 0.8f);
            UpdateHud();
            if (lockUI) lockUI.SetAvailable(found);
            Collected?.Invoke(pickup.letter);
            if (AllFound) AllCollected?.Invoke();
        }

        public void OpenLock()
        {
            if (!lockUI || IsSolved) return;
            lockUI.SetAvailable(found);
            lockUI.Open(secretWord.Length);
        }

        void OnSubmitted(string word)
        {
            if (string.Equals(word, secretWord, StringComparison.OrdinalIgnoreCase))
            {
                IsSolved = true;
                lockUI.ShowResult(true);
                StartCoroutine(SolvedRoutine());
            }
            else
            {
                lockUI.ShowResult(false);
            }
        }

        IEnumerator SolvedRoutine()
        {
            yield return new WaitForSeconds(1.1f);
            lockUI.Close();
            if (hudGroup) StartCoroutine(FadeGroup(hudGroup, 0f, 0.8f));
            Solved?.Invoke();
        }

        void UpdateHud()
        {
            if (!hudText || letters == null) return;
            var sb = new StringBuilder();
            for (int i = 0; i < letters.Length; i++)
            {
                if (i > 0) sb.Append("   ");
                sb.Append(i < found.Count ? found[i].ToString() : "<color=#5a5148>_</color>");
            }
            hudText.text = sb.ToString();
        }

        static IEnumerator FadeGroup(CanvasGroup g, float to, float duration)
        {
            float from = g.alpha, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            g.alpha = to;
        }
    }
}
