using System;
using System.Collections;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Porta do fundo do vagão. Começa travada (luz vermelha). Quando o puzzle é resolvido,
    /// destrava (luz verde) e as folhas deslizam para dentro da parede.
    /// Com a porta aberta, E atravessa para o próximo cenário.
    /// </summary>
    public class ExitDoor : Interactable
    {
        public Transform leftLeaf;
        public Transform rightLeaf;
        public float slideDistance = 1.75f;
        public float openTime = 1.5f;

        [Header("Luz de trava")]
        public SpriteRenderer signalLamp;
        public SpriteRenderer signalGlow;
        public Color lockedColor = new Color(0.85f, 0.12f, 0.08f, 1f);
        public Color openColor = new Color(0.45f, 1f, 0.55f, 1f);

        [Header("Som")]
        public AudioSource sfx;
        public AudioClip unlockClip;

        /// <summary>Chamado quando o jogador aperta E com a porta travada / aberta.</summary>
        [NonSerialized] public Action onLockedInteract;
        [NonSerialized] public Action onEnter;

        public bool IsOpen { get; private set; }

        bool opening;
        Vector3 left0, right0;
        Coroutine flash;

        void Awake()
        {
            if (leftLeaf) left0 = leftLeaf.localPosition;
            if (rightLeaf) right0 = rightLeaf.localPosition;
            SetSignal(lockedColor, 0.45f);
        }

        void Update()
        {
            // a luz vermelha pulsa devagar enquanto está travada
            if (!IsOpen && !opening && flash == null)
                SetSignal(lockedColor, 0.3f + 0.15f * Mathf.Sin(Time.time * 2.2f));
        }

        public override bool CanInteract => base.CanInteract && !opening && !DialogueBox.IsBusy;

        public override void Interact()
        {
            if (IsOpen) onEnter?.Invoke();
            else onLockedInteract?.Invoke();
        }

        public void Unlock()
        {
            if (IsOpen || opening) return;
            StartCoroutine(OpenRoutine());
        }

        IEnumerator OpenRoutine()
        {
            opening = true;
            if (flash != null) StopCoroutine(flash);
            flash = null;
            SetSignal(openColor, 0.6f);
            if (sfx && unlockClip) sfx.PlayOneShot(unlockClip);
            yield return new WaitForSeconds(0.4f);

            float t = 0f;
            while (t < openTime)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / openTime);
                if (leftLeaf) leftLeaf.localPosition = left0 + Vector3.left * (slideDistance * k);
                if (rightLeaf) rightLeaf.localPosition = right0 + Vector3.right * (slideDistance * k);
                yield return null;
            }
            IsOpen = true;
            opening = false;
        }

        /// <summary>Pisca a luz vermelha (quando a ordem das alavancas está errada).</summary>
        public void FlashLocked()
        {
            if (IsOpen || opening) return;
            if (flash != null) StopCoroutine(flash);
            flash = StartCoroutine(FlashRoutine());
        }

        IEnumerator FlashRoutine()
        {
            for (int i = 0; i < 4; i++)
            {
                SetSignal(lockedColor * 1.4f, 1f);
                yield return new WaitForSeconds(0.15f);
                SetSignal(lockedColor * 0.4f, 0.05f);
                yield return new WaitForSeconds(0.12f);
            }
            flash = null;
        }

        void SetSignal(Color c, float glow)
        {
            c.a = 1f;
            if (signalLamp) signalLamp.color = c;
            if (signalGlow)
            {
                c.a = glow;
                signalGlow.color = c;
            }
        }
    }
}
