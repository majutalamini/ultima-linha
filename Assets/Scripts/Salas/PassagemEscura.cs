using System.Collections.Generic;
using UltimaLinha.Cenario1;
using UnityEngine;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Uma passagem escura (o vão do portão do Cenário 3): quem anda por dentro dela escurece, como se entrasse no
    /// corredor, e volta à cor normal ao sair. Junto com o batente desenhado na frente, dá para ver o personagem
    /// atravessando a porta, e não passando por fora dela. Só vale com "ativo" ligado (o portão aberto).
    /// </summary>
    public class PassagemEscura : MonoBehaviour
    {
        [Tooltip("Começo e fim da passagem, em x do mundo.")]
        public float x0, x1;
        [Tooltip("Largura da transição nas bordas (o personagem vai escurecendo aos poucos).")]
        public float borda = 0.45f;
        [Tooltip("Quanto da cor sobra lá dentro (0 = preto).")]
        [Range(0f, 1f)] public float escuro = 0.38f;
        public bool ativo;

        readonly List<PersonagemPixel> personagens = new List<PersonagemPixel>();
        readonly Dictionary<PersonagemPixel, Color> corBase = new Dictionary<PersonagemPixel, Color>();
        readonly HashSet<PersonagemPixel> tingidos = new HashSet<PersonagemPixel>();

        void Start()
        {
            personagens.AddRange(FindObjectsByType<PersonagemPixel>(FindObjectsInactive.Include, FindObjectsSortMode.None));
            foreach (var p in personagens)
                if (p && p.view) corBase[p] = p.view.color;
        }

        void LateUpdate()
        {
            foreach (var p in personagens)
            {
                if (!p || !p.view || !corBase.TryGetValue(p, out var cor)) continue;
                float x = p.transform.position.x;
                float f = 0f;
                if (ativo && x > x0 - borda && x < x1 + borda)
                    f = Mathf.Clamp01(Mathf.Min(x - (x0 - borda), (x1 + borda) - x) / (2f * borda));
                if (f <= 0f)
                {
                    if (tingidos.Remove(p)) p.view.color = cor; // saiu: volta a cor de antes uma vez só
                    continue;
                }
                tingidos.Add(p);
                var dentro = new Color(cor.r * escuro, cor.g * escuro, cor.b * escuro, cor.a);
                p.view.color = Color.Lerp(cor, dentro, f);
            }
        }
    }
}
