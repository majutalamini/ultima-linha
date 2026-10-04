using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Câmera que segue o Kai com suavidade, olha um pouco para onde ele anda
    /// e nunca mostra além das bordas do cenário.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraFollow2D : MonoBehaviour
    {
        public Transform target;
        public Vector2 offset = new Vector2(0f, 1.2f);
        public float smoothTime = 0.28f;
        public float lookAhead = 1.4f;
        [Tooltip("Retângulo do cenário em unidades do mundo (x, y, largura, altura).")]
        public Rect bounds = new Rect(0f, 0f, 42f, 11f);

        Camera cam;
        Vector3 basePos;
        Vector3 velocity;
        float look, lookDir = 1f, lastX;
        float shakeAmount, shakeTime;

        void Awake()
        {
            cam = GetComponent<Camera>();
            basePos = transform.position;
        }

        void Start()
        {
            Snap();
        }

        public void Snap()
        {
            if (!target) return;
            lastX = target.position.x;
            basePos = Clamp(Goal());
            velocity = Vector3.zero;
            transform.position = basePos;
        }

        public void Shake(float amount, float duration)
        {
            shakeAmount = amount;
            shakeTime = duration;
        }

        void LateUpdate()
        {
            if (!target) return;

            float dx = target.position.x - lastX;
            lastX = target.position.x;
            if (Mathf.Abs(dx) > 0.002f) lookDir = Mathf.Sign(dx);
            look = Mathf.MoveTowards(look, lookDir * lookAhead, Time.deltaTime * 1.6f);

            basePos = Vector3.SmoothDamp(basePos, Clamp(Goal()), ref velocity, smoothTime);

            var p = basePos;
            if (shakeTime > 0f)
            {
                shakeTime -= Time.deltaTime;
                float a = shakeAmount * Mathf.Clamp01(shakeTime * 3f);
                p.x += (Mathf.PerlinNoise(Time.time * 25f, 0.1f) - 0.5f) * 2f * a;
                p.y += (Mathf.PerlinNoise(0.7f, Time.time * 25f) - 0.5f) * 2f * a;
            }
            transform.position = p;
        }

        Vector3 Goal()
        {
            return new Vector3(target.position.x + offset.x + look, target.position.y + offset.y, transform.position.z);
        }

        Vector3 Clamp(Vector3 p)
        {
            if (!cam) cam = GetComponent<Camera>();
            float h = cam.orthographicSize, w = h * cam.aspect;
            float minX = bounds.xMin + w, maxX = bounds.xMax - w;
            float minY = bounds.yMin + h, maxY = bounds.yMax - h;
            p.x = minX > maxX ? bounds.center.x : Mathf.Clamp(p.x, minX, maxX);
            p.y = minY > maxY ? bounds.center.y : Mathf.Clamp(p.y, minY, maxY);
            return p;
        }
    }
}
