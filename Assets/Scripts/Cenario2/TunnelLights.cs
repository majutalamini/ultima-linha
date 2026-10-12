using UnityEngine;

namespace UltimaLinha.Cenario2
{
    /// <summary>
    /// Luzes do túnel passando pelas janelas enquanto o trem anda: cada janela tem uma máscara e
    /// um reflexo que corre da direita para a esquerda de tempos em tempos.
    /// Quando o trem para (Moving = false), as luzes somem aos poucos.
    /// </summary>
    public class TunnelLights : MonoBehaviour
    {
        [System.Serializable]
        public class Window
        {
            public SpriteRenderer streak;
            public float x0, x1, y0, y1;
            [System.NonSerialized] public float x, wait, y;
        }

        public Window[] windows = new Window[0];
        [Tooltip("Velocidade das luzes (unidades por segundo).")]
        public float speed = 16f;
        public Vector2 gap = new Vector2(0.4f, 1.8f);
        public float alpha = 0.55f;

        public bool Moving { get; set; } = true;
        float fade = 1f;

        void Start()
        {
            foreach (var w in windows)
            {
                w.wait = Random.Range(0f, gap.y);
                w.x = w.x1 + 3f;
                w.y = (w.y0 + w.y1) * 0.5f;
                Place(w);
            }
        }

        void Update()
        {
            fade = Mathf.MoveTowards(fade, Moving ? 1f : 0f, Time.deltaTime * 0.6f);
            foreach (var w in windows)
            {
                if (!w.streak) continue;
                if (w.wait > 0f)
                {
                    w.wait -= Time.deltaTime;
                    if (w.wait <= 0f)
                    {
                        w.x = w.x1 + 2.2f;
                        w.y = Random.Range(w.y0 + 0.3f, w.y1 - 0.3f);
                    }
                }
                else
                {
                    w.x -= speed * Mathf.Max(0.15f, fade) * Time.deltaTime;
                    if (w.x < w.x0 - 2.2f) w.wait = Random.Range(gap.x, gap.y);
                }
                Place(w);
            }
        }

        void Place(Window w)
        {
            if (!w.streak) return;
            w.streak.transform.position = new Vector3(w.x, w.y, 0f);
            var c = w.streak.color;
            c.a = w.wait > 0f ? 0f : alpha * fade;
            w.streak.color = c;
        }
    }
}
