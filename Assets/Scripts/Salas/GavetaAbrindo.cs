using System.Collections;
using UnityEngine;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Gaveta de arquivo abrindo: quando o objeto é ligado, a frente desliza para baixo (vem na direção
    /// da câmera) e o miolo com as pastas cresce a partir do topo da gaveta, por trás dela.
    /// As duas peças usam a mesma arte dos arquivos pintados, então aberta e fechada têm o mesmo visual.
    /// </summary>
    public class GavetaAbrindo : MonoBehaviour
    {
        [Tooltip("Frente da gaveta (pivô no topo). Desce 'saida' unidades ao abrir.")]
        public Transform frente;
        [Tooltip("Vão e pastas (pivô no topo). A altura vai de 0 a 1 ao abrir.")]
        public Transform dentro;
        [Tooltip("Quanto a frente desce, em unidades.")]
        public float saida = 10f / 58f;
        public float duracao = 0.3f;
        [Tooltip("Tamanho do pixel da arte, para a frente andar de pixel em pixel.")]
        public float pixel = 1f / 58f;

        Vector3 frenteFechada;
        bool pronto;

        void Prepara()
        {
            if (pronto || !frente) return;
            frenteFechada = frente.localPosition;
            pronto = true;
        }

        void OnEnable()
        {
            Prepara();
            if (!pronto) return;
            StopAllCoroutines();
            StartCoroutine(Abre());
        }

        void Aplica(float k)
        {
            float d = saida * k;
            if (pixel > 0f) d = Mathf.Round(d / pixel) * pixel;
            frente.localPosition = frenteFechada + Vector3.down * d;
            if (dentro)
            {
                var s = dentro.localScale;
                s.y = saida > 0f ? d / saida : 1f;
                dentro.localScale = s;
            }
        }

        IEnumerator Abre()
        {
            Aplica(0f);
            float t = 0f;
            while (t < duracao)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / duracao);
                Aplica(1f - (1f - k) * (1f - k) * (1f - k)); // sai rápido e freia no fim
                yield return null;
            }
            Aplica(1f);
        }
    }
}
