using System;
using System.Collections;
using System.Collections.Generic;
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

        [Header("Emperrada")]
        [Tooltip("Emperrada: E abre o mostrador (DestravaUI); solta, vira uma alavanca normal.")]
        public bool stuck;
        public GameObject stuckVisual;
        public GameObject normalVisual;
        public List<DialogueLine> freedLines = new List<DialogueLine>();

        public bool IsDown { get; private set; }
        /// <summary>Acabou de soltar (o diretor apaga a dica).</summary>
        public event Action Freed;

        bool moving;

        public override bool CanInteract =>
            base.CanInteract && !IsDown && !moving && puzzle && !puzzle.IsBusy && !puzzle.IsSolved && !DialogueBox.IsBusy;

        void Start()
        {
            if (handle) handle.localRotation = Quaternion.Euler(0f, 0f, upAngle);
        }

        void Update()
        {
            // emperrada: o cabo dá uns trancos sozinho, para chamar atenção
            if (stuck && stuckVisual)
                stuckVisual.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.PerlinNoise(Time.time * 3f, 0f) > 0.8f ? Mathf.Sin(Time.time * 60f) * 1.5f : 0f);
        }

        public override void Interact()
        {
            if (!CanInteract) return;
            if (stuck)
            {
                if (DestravaUI.Instance) DestravaUI.Instance.Open(Free);
                return;
            }
            StartCoroutine(PullRoutine());
        }

        void Free()
        {
            stuck = false;
            if (stuckVisual) stuckVisual.SetActive(false);
            if (normalVisual) normalVisual.SetActive(true);
            if (handle) handle.localRotation = Quaternion.Euler(0f, 0f, upAngle);
            Freed?.Invoke();
            if (DialogueBox.Instance && freedLines.Count > 0) DialogueBox.Instance.Play(freedLines);
        }

        IEnumerator PullRoutine()
        {
            moving = true;
            if (PersonagemPixel.Jogador) PersonagemPixel.Jogador.Tocar("lever");
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
