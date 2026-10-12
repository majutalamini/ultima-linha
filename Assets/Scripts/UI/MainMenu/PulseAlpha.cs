using UnityEngine;
using UnityEngine.UI;

namespace UltimaLinha.UI
{
    /// <summary>
    /// Pulsação lenta de transparência (usada na "última linha" sob o título e no brilho de fundo).
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class PulseAlpha : MonoBehaviour
    {
        [SerializeField, Range(0f, 1f)] float minAlpha = 0.35f;
        [SerializeField, Range(0f, 1f)] float maxAlpha = 1f;
        [SerializeField] float period = 3.2f;

        Graphic graphic;

        void Awake() => graphic = GetComponent<Graphic>();

        void Update()
        {
            float s = 0.5f + 0.5f * Mathf.Sin(Time.unscaledTime * Mathf.PI * 2f / Mathf.Max(0.01f, period));
            var c = graphic.color;
            c.a = Mathf.Lerp(minAlpha, maxAlpha, s);
            graphic.color = c;
        }
    }
}
