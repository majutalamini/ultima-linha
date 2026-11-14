using UnityEngine;

namespace UltimaLinha.Visual
{
    /// <summary>
    /// Sacode um objeto (porta, fios, cano) de vez em quando: um tremor curto, por ruído, e volta ao lugar. Com som
    /// opcional (rangido). Para fios, use amplitude pequena e intervalo curto.
    /// </summary>
    public class Tremor : MonoBehaviour
    {
        [Tooltip("Deslocamento máximo (u). Um pixel da grade = 0,017.")]
        public Vector2 amplitude = new Vector2(0.035f, 0.017f);
        public float velocidade = 30f;
        public Vector2 intervalo = new Vector2(4f, 10f);
        public float duracao = 0.5f;
        public float distanciaAtivacao = 12f;
        public AudioSource som;
        public AudioClip rangido;
        [Tooltip("Arredonda para a grade de pixels (sem borrar a pixel art).")]
        public bool naGrade = true;

        Vector3 origem;
        float ate, proximo, semente;
        Camera cam;

        void Awake()
        {
            origem = transform.localPosition;
            semente = Random.value * 50f;
            proximo = Time.time + Random.Range(intervalo.x, intervalo.y);
        }

        void Update()
        {
            float t = Time.time;
            if (t >= proximo)
            {
                if (!cam) cam = Camera.main;
                bool perto = distanciaAtivacao <= 0f || !cam || Mathf.Abs(cam.transform.position.x - transform.position.x) < distanciaAtivacao;
                if (perto)
                {
                    ate = t + duracao;
                    if (som && rangido) som.PlayOneShot(rangido, 0.6f);
                }
                proximo = t + duracao + Random.Range(intervalo.x, intervalo.y);
            }
            if (t >= ate)
            {
                transform.localPosition = origem;
                return;
            }
            float k = (ate - t) / duracao; // some aos poucos
            var d = new Vector3((Mathf.PerlinNoise(semente, t * velocidade) - 0.5f) * 2f * amplitude.x,
                                (Mathf.PerlinNoise(semente + 3f, t * velocidade) - 0.5f) * 2f * amplitude.y, 0f) * k;
            if (naGrade) d = new Vector3(Mathf.Round(d.x * 58f), Mathf.Round(d.y * 58f), 0f) / 58f;
            transform.localPosition = origem + d;
        }
    }
}
