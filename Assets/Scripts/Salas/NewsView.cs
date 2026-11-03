using System;
using System.Collections;
using TMPro;
using UltimaLinha.Cenario1;
using UnityEngine;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Plantão do telejornal na tela inteira (finais do Cenário 7): faixa vermelha, manchete, texto e letreiro correndo embaixo.
    /// Depois de alguns segundos, E, Espaço, Enter ou clique continuam.
    /// </summary>
    public class NewsView : MonoBehaviour
    {
        public static NewsView Instance { get; private set; }

        public CanvasGroup group;
        public TMP_Text channel;
        public TMP_Text headline;
        public TMP_Text body;
        public TMP_Text ticker;
        public RectTransform tickerStrip;
        public TMP_Text hint;
        public TMP_Text clock;
        public CanvasGroup liveDot;
        public AudioSource sfx;
        public AudioClip stingClip;
        public float tickerSpeed = 140f;
        public float minSeconds = 3.5f;

        public bool IsOpen { get; private set; }

        Action onClose;
        float openedAt;
        bool closing;

        void Awake()
        {
            Instance = this;
            if (group)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
            if (hint) hint.alpha = 0f;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (IsOpen) Modal.Pop();
        }

        public void Show(string channelName, string title, string text, string tickerText, Action closed)
        {
            if (IsOpen) return;
            IsOpen = true;
            closing = false;
            onClose = closed;
            openedAt = Time.unscaledTime;
            Modal.Push();
            Modal.SetControl(false);
            if (channel) channel.text = channelName ?? "";
            if (headline) headline.text = title ?? "";
            if (body) body.text = text ?? "";
            if (ticker)
            {
                ticker.text = tickerText ?? "";
                var rt = ticker.rectTransform;
                rt.anchoredPosition = new Vector2(tickerStrip ? tickerStrip.rect.width : 1920f, rt.anchoredPosition.y);
            }
            if (hint) hint.alpha = 0f;
            if (sfx && stingClip) sfx.PlayOneShot(stingClip);
            StopAllCoroutines();
            StartCoroutine(Fade(1f, 0.5f));
            if (group) group.blocksRaycasts = true;
        }

        void Update()
        {
            if (!IsOpen) return;
            if (ticker)
            {
                var rt = ticker.rectTransform;
                var p = rt.anchoredPosition;
                p.x -= tickerSpeed * Time.unscaledDeltaTime;
                float width = ticker.preferredWidth;
                if (p.x < -width) p.x = tickerStrip ? tickerStrip.rect.width : 1920f;
                rt.anchoredPosition = p;
            }
            if (liveDot) liveDot.alpha = Mathf.Repeat(Time.unscaledTime, 1.2f) < 0.7f ? 1f : 0.25f;
            bool ready = Time.unscaledTime - openedAt >= minSeconds;
            if (hint) hint.alpha = Mathf.MoveTowards(hint.alpha, ready ? 0.75f : 0f, Time.unscaledDeltaTime * 2f);
            if (!closing && ready && InputHelper.AdvancePressed()) StartCoroutine(CloseRoutine());
        }

        IEnumerator CloseRoutine()
        {
            closing = true;
            if (group) group.blocksRaycasts = false;
            yield return Fade(0f, 0.6f);
            yield return null;
            IsOpen = false;
            Modal.Pop();
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
}
