using UnityEngine;
using UnityEngine.UI;

namespace UltimaLinha.UI
{
    /// <summary>
    /// Granulado de filme animado: sorteia um novo pedaço da textura de ruído várias vezes por segundo.
    /// Vai num RawImage que cobre a tela inteira, com uma textura de ruído em modo Repeat.
    /// </summary>
    [RequireComponent(typeof(RawImage))]
    public class FilmGrain : MonoBehaviour
    {
        [Tooltip("Tamanho de cada grão, em unidades de UI.")]
        [SerializeField] float grainSize = 3f;
        [Tooltip("Quantas vezes por segundo o granulado muda.")]
        [SerializeField] float framesPerSecond = 24f;

        RawImage image;
        RectTransform rect;
        float nextChange;

        void Awake()
        {
            image = GetComponent<RawImage>();
            rect = (RectTransform)transform;
        }

        void Update()
        {
            if (Time.unscaledTime < nextChange || !image.texture) return;
            nextChange = Time.unscaledTime + 1f / Mathf.Max(1f, framesPerSecond);

            var size = rect.rect.size;
            var tex = image.texture;
            image.uvRect = new Rect(Random.value, Random.value,
                size.x / (tex.width * grainSize), size.y / (tex.height * grainSize));
        }
    }
}
