using UnityEngine;

namespace UltimaLinha.Visual
{
    /// <summary>
    /// Toca uma sequência de quadros (faísca, respingo, chiado de monitor) num SpriteRenderer, de vez em quando ou em
    /// laço. Fora dos intervalos o sprite fica escondido (ou no quadro de repouso, se houver). Só toca com a câmera perto.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class EfeitoQuadros : MonoBehaviour
    {
        public Sprite[] quadros = new Sprite[0];
        [Tooltip("Quadros por segundo.")]
        public float fps = 14f;
        [Tooltip("Em laço = toca sem parar; senão espera um intervalo sorteado entre x e y (s) entre uma vez e outra.")]
        public bool emLaco;
        public Vector2 intervalo = new Vector2(2f, 6f);
        [Tooltip("Opcional: mostrado entre uma vez e outra.")]
        public Sprite repouso;
        public Color cor = Color.white;
        [Range(0f, 1f)] public float opacidade = 1f;
        public float distanciaAtivacao = 14f;

        SpriteRenderer sr;
        float inicio = -1f, proximo;
        Camera cam;

        void Awake()
        {
            sr = GetComponent<SpriteRenderer>();
            proximo = Time.time + (emLaco ? 0f : Random.Range(intervalo.x, intervalo.y));
            Repousa();
        }

        void Repousa()
        {
            sr.sprite = repouso;
            sr.enabled = repouso;
        }

        void Update()
        {
            if (quadros.Length == 0) return;
            if (!cam) cam = Camera.main;
            bool perto = distanciaAtivacao <= 0f || !cam || Mathf.Abs(cam.transform.position.x - transform.position.x) < distanciaAtivacao;
            var c = cor;
            c.a = opacidade;
            sr.color = c;
            if (inicio < 0f)
            {
                if (!perto || Time.time < proximo) return;
                inicio = Time.time;
                sr.enabled = true;
            }
            int i = Mathf.FloorToInt((Time.time - inicio) * fps);
            if (i < quadros.Length) { sr.sprite = quadros[i]; return; }
            if (emLaco) { inicio = Time.time; return; }
            inicio = -1f;
            proximo = Time.time + Random.Range(intervalo.x, intervalo.y);
            Repousa();
        }
    }
}
