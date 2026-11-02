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
}
