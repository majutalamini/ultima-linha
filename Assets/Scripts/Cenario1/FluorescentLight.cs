using System.Collections;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Luminária fluorescente do teto: halo suave que oscila e, de vez em quando, falha.
    /// As marcadas como "quebradas" falham muito mais (último trecho do vagão).
    /// </summary>
    public class FluorescentLight : MonoBehaviour
    {
        [Tooltip("Halo de luz em volta do tubo.")]
        public SpriteRenderer glow;
        [Tooltip("Retângulo escuro que cobre o tubo desenhado no fundo quando a luz apaga.")]
        public SpriteRenderer cover;
        public float glowAlpha = 0.16f;
        public bool broken;

        [Header("Som do estalo")]
        public AudioSource sfx;
        public AudioClip crackle;

        bool on = true;
        float seed;

        void Awake()
        {
            seed = Random.value * 100f;
            Set(true);
        }

        IEnumerator Start()
        {
            while (true)
            {
                yield return new WaitForSeconds(broken ? Random.Range(0.8f, 3.5f) : Random.Range(7f, 24f));
                int blinks = Random.Range(2, broken ? 7 : 4);
                for (int i = 0; i < blinks; i++)
                {
                    Set(false);
                    yield return new WaitForSeconds(Random.Range(0.03f, 0.12f));
                    Set(true);
                    yield return new WaitForSeconds(Random.Range(0.04f, 0.2f));
                }
                if (broken && Random.value < 0.35f)
                {
                    Set(false);
                    yield return new WaitForSeconds(Random.Range(0.6f, 2f));
                    Set(true);
                }
            }
        }

        void Update()
        {
            if (!on || !glow) return;
            var c = glow.color;
            c.a = glowAlpha * (0.9f + 0.1f * Mathf.PerlinNoise(Time.time * 6f, seed));
            glow.color = c;
        }

        void Set(bool value)
        {
            if (on && !value && sfx && crackle && Random.value < 0.5f) sfx.PlayOneShot(crackle, 0.35f);
            on = value;
            if (glow)
            {
                var c = glow.color;
                c.a = value ? glowAlpha : 0f;
                glow.color = c;
            }
            if (cover)
            {
                var c = cover.color;
                c.a = value ? 0f : 0.92f;
                cover.color = c;
            }
        }
    }
}
