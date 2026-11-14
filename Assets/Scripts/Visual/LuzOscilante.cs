using UnityEngine;
using UnityEngine.Rendering.Universal;

namespace UltimaLinha.Visual
{
    /// <summary>
    /// Luz 2D de verdade (Light2D) com vida própria. Fluorescente: treme por ruído (nunca repete) e falha de vez em quando
    /// (apaga e volta, em intervalos aleatórios). Emergência: pulso lento. Pode seguir a opacidade de um halo já animado por
    /// outro script (o FluorescentLight das luminárias, a luz vermelha do diretor): apagado ou desligado, a luz some junto.
    /// </summary>
    [RequireComponent(typeof(Light2D))]
    public class LuzOscilante : MonoBehaviour
    {
        public enum Modo { Fluorescente, Emergencia }

        public Modo modo = Modo.Fluorescente;
        [Range(0f, 4f)] public float intensidade = 1f;
        public Color cor = new Color(0.78f, 0.88f, 0.92f);
        [Tooltip("Fluorescente: velocidade do tremor. Emergência: pulsos por segundo.")]
        public float velocidade = 1.5f;
        [Range(0f, 1f), Tooltip("Quanto o tremor tira da intensidade.")]
        public float oscilacao = 0.12f;
        [Tooltip("Falhas por minuto, em média (0 = nunca). Só no modo fluorescente.")]
        public float falhasPorMinuto = 1.5f;
        public Vector2 duracaoFalha = new Vector2(0.06f, 0.5f);
        [Tooltip("Opcional: a luz segue a opacidade (e o liga/desliga) deste sprite.")]
        public SpriteRenderer segue;
        [Tooltip("Opcional: estalo quando falha.")]
        public AudioSource som;
        public AudioClip estalo;

        Light2D luz;
        float semente, falhaAte, proxima = -1f, alfaSegue = 1f;

        void Awake()
        {
            luz = GetComponent<Light2D>();
            semente = Random.value * 100f;
            if (segue) alfaSegue = Mathf.Max(0.01f, segue.color.a);
        }

        // intervalo de Poisson: as falhas não caem num ritmo que o jogador perceba
        float Proxima() => falhasPorMinuto <= 0f ? float.MaxValue : -Mathf.Log(1f - Random.value * 0.999f) * 60f / falhasPorMinuto;

        void Update()
        {
            float t = Time.time, k;
            if (modo == Modo.Emergencia)
            {
                float s = 0.5f + 0.5f * Mathf.Sin(t * velocidade * Mathf.PI * 2f + semente);
                k = 0.3f + 0.7f * s * s;
            }
            else
            {
                if (proxima < 0f) proxima = t + Proxima();
                k = 1f - oscilacao * Mathf.PerlinNoise(semente, t * velocidade);
                if (t >= proxima)
                {
                    falhaAte = t + Random.Range(duracaoFalha.x, duracaoFalha.y);
                    proxima = falhaAte + Proxima();
                    if (som && estalo) som.PlayOneShot(estalo, 0.5f);
                }
                if (t < falhaAte) k *= Mathf.PerlinNoise(semente + 7f, t * 28f) < 0.55f ? 0.04f : 0.6f;
            }
            if (segue) k *= segue.enabled && segue.gameObject.activeInHierarchy ? Mathf.Clamp01(segue.color.a / alfaSegue) : 0f;
            luz.intensity = intensidade * k;
            luz.color = cor;
        }
    }
}
