using UnityEngine;

namespace UltimaLinha.Visual
{
    /// <summary>
    /// Aparição pontual: quando o Kai chega perto, uma silhueta escura atravessa um trecho da parede e some. Toca poucas
    /// vezes (vezes) e com espera entre uma e outra, para continuar estranho e não virar susto barato.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class SombraPassageira : MonoBehaviour
    {
        public Transform alvo;
        [Tooltip("Dispara com o alvo a menos disso (u, no eixo x).")]
        public float distanciaAtivacao = 4f;
        [Tooltip("Para onde ela anda a partir do ponto de partida (u).")]
        public float percurso = 5f;
        public float velocidade = 3.5f;
        [Range(0f, 1f)] public float opacidade = 0.55f;
        public Color cor = new Color(0.02f, 0.02f, 0.025f);
        public int vezes = 1;
        public float espera = 40f;

        SpriteRenderer sr;
        Vector3 inicio;
        float andou = -1f, liberada;
        int feitas;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            inicio = transform.position;
            sr.enabled = false;
        }

        void Update()
        {
            if (andou < 0f)
            {
                if (!alvo || feitas >= vezes || Time.time < liberada) return;
                if (Mathf.Abs(alvo.position.x - inicio.x) > distanciaAtivacao) return;
                andou = 0f;
                sr.enabled = true;
            }
            andou += velocidade * Time.deltaTime;
            float k = andou / Mathf.Abs(percurso);
            transform.position = inicio + Vector3.right * Mathf.Sign(percurso) * andou;
            var c = cor;
            c.a = opacidade * Mathf.Sin(Mathf.Clamp01(k) * Mathf.PI); // aparece e some no meio do caminho
            sr.color = c;
            if (k < 1f) return;
            andou = -1f;
            feitas++;
            liberada = Time.time + espera;
            sr.enabled = false;
            transform.position = inicio;
        }
    }
}
