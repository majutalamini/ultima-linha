using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace UltimaLinha.UI
{    /// <summary>
    /// Piscar irregular, como uma lâmpada fluorescente de escritório falhando. Usado no título.
    /// </summary>
    [RequireComponent(typeof(Graphic))]
    public class LightFlicker : MonoBehaviour
    {
        [SerializeField] Vector2 intervalRange = new Vector2(4f, 10f);
        [SerializeField, Range(0f, 1f)] float dimAlpha = 0.45f;

        Graphic graphic;
        float baseAlpha;

        void Awake()
        {
            graphic = GetComponent<Graphic>();
            baseAlpha = graphic.color.a;
        }

        void OnEnable() => StartCoroutine(Loop());

        void OnDisable() => SetAlpha(baseAlpha);

        IEnumerator Loop()
        {
            while (true)
            {
                yield return new WaitForSecondsRealtime(Random.Range(intervalRange.x, intervalRange.y));
                int blinks = Random.Range(1, 4);
                for (int i = 0; i < blinks; i++)
                {
                    SetAlpha(baseAlpha * Random.Range(dimAlpha, dimAlpha + 0.2f));
                    yield return new WaitForSecondsRealtime(Random.Range(0.03f, 0.09f));
                    SetAlpha(baseAlpha);
                    yield return new WaitForSecondsRealtime(Random.Range(0.04f, 0.12f));
                }
            }
        }

        void SetAlpha(float a)
        {
            if (!graphic) return;
            var c = graphic.color;
            c.a = a;
            graphic.color = c;
        }
    }
}
