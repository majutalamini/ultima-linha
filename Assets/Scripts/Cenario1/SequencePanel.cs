using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Os três pontos de luz no letreiro em cima das portas. Cada ponto representa uma alavanca,
    /// da esquerda para a direita. Eles piscam na ordem certa de tempos em tempos (a pista do puzzle).
    /// </summary>
    public class SequencePanel : MonoBehaviour
    {
        public SpriteRenderer[] dots;
        public SpriteRenderer[] glows;

        public void Set(int index, Color color, float glow)
        {
            if (dots != null && index < dots.Length && dots[index]) dots[index].color = color;
            if (glows != null && index < glows.Length && glows[index])
            {
                var g = color;
                g.a = glow;
                glows[index].color = g;
            }
        }
    }
}
