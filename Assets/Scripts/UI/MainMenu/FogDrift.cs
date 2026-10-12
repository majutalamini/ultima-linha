using UnityEngine;
using UnityEngine.UI;

namespace UltimaLinha.UI
{
    /// <summary>
    /// Névoa do menu: desliza a textura devagar na horizontal (a textura se repete sem emenda)
    /// e "respira" um pouco na transparência.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class FogDrift : MonoBehaviour
    {
        [Tooltip("Quanto da textura anda por segundo (negativo = para a esquerda).")]
        [SerializeField] float speed = 0.01f;
        [SerializeField, Range(0f, 1f)] float breathe = 0.15f;
        [SerializeField] float breathePeriod = 7f;

        RawImage image;
        float baseAlpha;
        float phase;

        void Awake()
        {
            image = GetComponent<RawImage>();
            baseAlpha = image.color.a;
            phase = Random.value * 10f;
        }

        void Update()
        {
            var r = image.uvRect;
            r.x = Mathf.Repeat(r.x + speed * Time.unscaledDeltaTime, 1f);
            image.uvRect = r;
            var c = image.color;
            c.a = baseAlpha * (1f - breathe * 0.5f + breathe * 0.5f * Mathf.Sin((Time.unscaledTime + phase) * Mathf.PI * 2f / breathePeriod));
            image.color = c;
        }
    }
}
