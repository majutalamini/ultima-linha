using System.Collections;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Alavanca do puzzle. Fica em cima de uma plataforma (banco, bagageiro); o Kai precisa estar
    /// em pé na mesma plataforma para puxar. A lâmpada em cima dela pisca durante a pista de ordem.
    /// </summary>
    public class TrainLever : Interactable
    {
        public LeverPuzzle puzzle;
        [Tooltip("Cabo da alavanca (gira em volta do próprio pivô, na base).")]
        public Transform handle;
        public float upAngle = 38f;
        public float downAngle = -38f;

        [Header("Lâmpada")]
        public SpriteRenderer lamp;
        public SpriteRenderer lampGlow;

        [Header("Som")]
        public AudioSource sfx;
        public AudioClip pullClip;

        public bool IsDown { get; private set; }

        bool moving;

        public override bool CanInteract =>
            base.CanInteract && !IsDown && !moving && puzzle && !puzzle.IsBusy && !puzzle.IsSolved;

        void Start()
        {
            if (handle) handle.localRotation = Quaternion.Euler(0f, 0f, upAngle);
        }

        public override void Interact()
        {
            if (!CanInteract) return;
            StartCoroutine(PullRoutine());
        }

        IEnumerator PullRoutine()
        {
            moving = true;
            if (sfx && pullClip) sfx.PlayOneShot(pullClip);
            yield return Rotate(upAngle, downAngle, 0.22f);
            IsDown = true;
            moving = false;
            if (puzzle) puzzle.OnLeverPulled(this);
        }

        /// <summary>Volta a alavanca para cima (quando a ordem estava errada).</summary>
        public void ResetLever()
        {
            if (!IsDown) return;
            StartCoroutine(ResetRoutine());
        }

        IEnumerator ResetRoutine()
        {
            moving = true;
            yield return Rotate(downAngle, upAngle, 0.35f);
            IsDown = false;
            moving = false;
        }

        IEnumerator Rotate(float from, float to, float duration)
        {
            if (!handle) yield break;
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / duration);
                handle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(from, to, k));
                yield return null;
            }
            handle.localRotation = Quaternion.Euler(0f, 0f, to);
        }

        public void SetLamp(Color color, float glow)
        {
            if (lamp) lamp.color = color;
            if (lampGlow)
            {
                var g = color;
                g.a = glow;
                lampGlow.color = g;
            }
        }
    }
}
