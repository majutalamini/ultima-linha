using System.Collections;
using TMPro;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Textos na tela: objetivo atual (canto superior esquerdo), avisos curtos, fade e cartela de fim.
    /// </summary>
    public class Hud : MonoBehaviour
    {
        public static Hud Instance { get; private set; }

        public CanvasGroup fader;
        public CanvasGroup objectiveGroup;
        public TMP_Text objectiveText;
        public CanvasGroup toastGroup;
        public TMP_Text toastText;
        public CanvasGroup endGroup;
        public TMP_Text endTitle;
        public TMP_Text endSubtitle;

        Coroutine objectiveRoutine, toastRoutine;

        void Awake()
        {
            Instance = this;
            if (fader)
            {
                fader.alpha = 1f; // começa preto; o diretor da cena clareia
                fader.blocksRaycasts = false;
            }
            if (objectiveGroup) objectiveGroup.alpha = 0f;
            if (toastGroup) toastGroup.alpha = 0f;
            if (endGroup) endGroup.alpha = 0f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void SetObjective(string text)
        {
            if (objectiveRoutine != null) StopCoroutine(objectiveRoutine);
            objectiveRoutine = StartCoroutine(ObjectiveRoutine(text));
        }

        IEnumerator ObjectiveRoutine(string text)
        {
            if (objectiveGroup.alpha > 0f) yield return FadeGroup(objectiveGroup, 0f, 0.35f);
            objectiveText.text = text;
            if (!string.IsNullOrEmpty(text)) yield return FadeGroup(objectiveGroup, 1f, 0.8f);
            objectiveRoutine = null;
        }

        public void Toast(string text, float duration = 2.6f)
        {
            if (toastRoutine != null) StopCoroutine(toastRoutine);
            toastRoutine = StartCoroutine(ToastRoutine(text, duration));
        }

        IEnumerator ToastRoutine(string text, float duration)
        {
            toastText.text = text;
            yield return FadeGroup(toastGroup, 1f, 0.25f);
            yield return new WaitForSeconds(duration);
            yield return FadeGroup(toastGroup, 0f, 0.6f);
            toastRoutine = null;
        }

        public IEnumerator FadeScreen(float to, float duration)
        {
            yield return FadeGroup(fader, to, duration);
        }

        public IEnumerator ShowEnd(string title, string subtitle)
        {
            endTitle.text = title;
            endSubtitle.text = subtitle;
            yield return FadeGroup(endGroup, 1f, 1.2f);
        }

        static IEnumerator FadeGroup(CanvasGroup g, float to, float duration)
        {
            if (!g) yield break;
            float from = g.alpha, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                yield return null;
            }
            g.alpha = to;
        }
    }
}
