using UnityEngine;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Alarme do túnel (Cenário 6): as luminárias pintadas na parede piscam em vermelho e jogam luz vermelha na parede
    /// em volta delas. Só a parede fica vermelha; a tela continua com as cores normais.
    /// </summary>
    public class LuzAlarme : MonoBehaviour
    {
        [Tooltip("O vidro vermelho por cima de cada luminária pintada.")]
        public SpriteRenderer[] vidros = new SpriteRenderer[0];
        [Tooltip("A mancha de luz vermelha na parede, em volta de cada luminária.")]
        public SpriteRenderer[] brilhos = new SpriteRenderer[0];
        [Tooltip("Segundos de um ciclo aceso + apagado.")]
        public float periodo = 1.1f;
        [Range(0f, 1f)] public float brilhoMax = 0.5f;
        public bool ligada;

        float nivel;

        void OnEnable() => Aplica(0f);

        void Update()
        {
            float alvo = 0f;
            if (ligada)
            {
                // acende rápido, segura e apaga: metade do ciclo acesa, metade apagada
                float t = Mathf.Repeat(Time.time / Mathf.Max(0.1f, periodo), 1f);
                alvo = t < 0.55f ? Mathf.Sin(Mathf.Clamp01(t / 0.55f) * Mathf.PI) : 0f;
                alvo = Mathf.Clamp01(alvo * 1.4f);
            }
            nivel = Mathf.MoveTowards(nivel, alvo, Time.deltaTime * 8f);
            Aplica(nivel);
        }

        void Aplica(float a)
        {
            foreach (var v in vidros)
                if (v) { var c = v.color; c.a = a; v.color = c; }
            foreach (var b in brilhos)
                if (b) { var c = b.color; c.a = a * brilhoMax; b.color = c; }
        }
    }
}
