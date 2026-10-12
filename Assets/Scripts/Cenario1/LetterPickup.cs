using System.Collections;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Uma letra escrita (em vermelho, como tinta ou sangue) em algum ponto do vagão.
    /// Só aparece depois das alavancas. O Kai precisa chegar perto (na mesma plataforma) e apertar E.
    /// </summary>
    public class LetterPickup : Interactable
    {
        public char letter = 'M';
        public LetterPuzzle puzzle;
        [Tooltip("A letra desenhada na parede/banco.")]
        public SpriteRenderer mark;
        [Tooltip("Brilho fraco em volta, para dar para achar no escuro.")]
        public SpriteRenderer glow;
        public float glowAlpha = 0.28f;

        public bool Collected { get; private set; }
        public bool Revealed { get; private set; }

        float seed;

        void Awake()
        {
            seed = Random.value * 10f;
            SetAlpha(0f, 0f);
        }

        public override bool CanInteract => base.CanInteract && Revealed && !Collected && !DialogueBox.IsBusy && !LetterLockUI.IsOpen;

        public void Reveal(float delay)
        {
            if (Revealed) return;
            StartCoroutine(RevealRoutine(delay));
        }

        IEnumerator RevealRoutine(float delay)
        {
            yield return new WaitForSeconds(delay);
            Revealed = true;
            float t = 0f;
            while (t < 1.2f)
            {
                t += Time.deltaTime;
                float k = Mathf.SmoothStep(0f, 1f, t / 1.2f);
                SetAlpha(k, k * glowAlpha);
                yield return null;
            }
        }

        void Update()
        {
            if (!Revealed || Collected || !glow) return;
            var c = glow.color;
            c.a = glowAlpha * (0.65f + 0.35f * Mathf.Sin(Time.time * 2.4f + seed));
            glow.color = c;
        }

        public override void Interact()
        {
            if (!CanInteract) return;
            Collected = true;
            StartCoroutine(CollectRoutine());
            if (puzzle) puzzle.OnCollected(this);
        }

        IEnumerator CollectRoutine()
        {
            // a letra "some" do lugar: fica mais forte, cresce um pouco e desaparece
            Vector3 s0 = mark ? mark.transform.localScale : Vector3.one;
            float t = 0f;
            while (t < 0.6f)
            {
                t += Time.deltaTime;
                float k = t / 0.6f;
                if (mark) mark.transform.localScale = s0 * (1f + 0.25f * k);
                SetAlpha(1f - k, glowAlpha * (1f + k) * (1f - k));
                yield return null;
            }
            SetAlpha(0f, 0f);
        }

        void SetAlpha(float markA, float glowA)
        {
            if (mark)
            {
                var c = mark.color;
                c.a = markA;
                mark.color = c;
            }
            if (glow)
            {
                var c = glow.color;
                c.a = glowA;
                glow.color = c;
            }
        }
    }
}
