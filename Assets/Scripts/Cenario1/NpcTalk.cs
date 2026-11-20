using System;
using System.Collections.Generic;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Personagem parado no vagão (Souta, Ren). Vira para o Kai e conversa quando ele aperta E.
    /// As falas mudam com o andamento: primeira conversa, conversas seguintes,
    /// depois das alavancas (fase das letras) e depois que a porta abre.
    /// </summary>
    public class NpcTalk : Interactable
    {
        [Tooltip("Personagem em pixel art (o normal). Se estiver vazio, usa o sprite simples abaixo.")]
        public PersonagemPixel rig;
        public SpriteRenderer sprite;
        [Tooltip("Normalmente o Kai: o personagem vira o rosto para ele.")]
        public Transform lookAt;
        public LeverPuzzle puzzle;
        public ExitDoor door;

        [Header("Falas")]
        public List<DialogueLine> firstTalk = new List<DialogueLine>();
        public List<DialogueLine> repeatTalk = new List<DialogueLine>();
        [Tooltip("Depois das alavancas, enquanto procuram as letras.")]
        public List<DialogueLine> afterSolved = new List<DialogueLine>();
        [Tooltip("Depois que a porta abre.")]
        public List<DialogueLine> afterDoor = new List<DialogueLine>();
        [Tooltip("Dica direta para quem está travado nas alavancas (o diretor liga 'dica').")]
        public List<DialogueLine> hintLevers = new List<DialogueLine>();
        [Tooltip("Dica direta para quem está travado nas letras.")]
        public List<DialogueLine> hintLetters = new List<DialogueLine>();

        [Header("Balão de dica")]
        [Tooltip("Os pontinhos em cima da cabeça, piscando enquanto há dica.")]
        public SpriteRenderer[] balao;

        /// <summary>Ligado pelo diretor quando o jogador está travado: a próxima conversa é a dica.</summary>
        [NonSerialized] public bool dica;
        public event Action<NpcTalk> Talked;

        [Header("Respiração (só no sprite simples)")]
        public float breathAmount = 0.008f;
        public float breathSpeed = 1.6f;

        public int TimesTalked { get; private set; }

        float phase;

        void Awake()
        {
            phase = UnityEngine.Random.value * 10f;
        }

        public override bool CanInteract => base.CanInteract && !DialogueBox.IsBusy;

        public override void Interact()
        {
            List<DialogueLine> lines;
            var hint = puzzle && puzzle.IsSolved ? hintLetters : hintLevers;
            if (dica && hint.Count > 0) lines = hint;
            else if (door && door.IsOpen && afterDoor.Count > 0) lines = afterDoor;
            else if (puzzle && puzzle.IsSolved && afterSolved.Count > 0) lines = afterSolved;
            else if (TimesTalked > 0 && repeatTalk.Count > 0) lines = repeatTalk;
            else lines = firstTalk;
            TimesTalked++;
            if (DialogueBox.Instance) DialogueBox.Instance.Play(lines);
            Talked?.Invoke(this);
        }

        void Update()
        {
            if (balao != null)
                for (int i = 0; i < balao.Length; i++)
                    if (balao[i])
                    {
                        var c = balao[i].color;
                        c.a = dica ? 0.35f + 0.65f * Mathf.Clamp01(Mathf.Sin(Time.time * 4f - i * 0.9f)) : 0f; // os três pontos em onda
                        balao[i].color = c;
                    }
            float dx = lookAt ? lookAt.position.x - transform.position.x : 0f;
            if (rig)
            {
                if (Mathf.Abs(dx) > 0.2f) rig.facing = dx < 0f ? -1f : 1f; // desenhos olham para a direita
                return;
            }
            if (!sprite) return;
            if (Mathf.Abs(dx) > 0.2f) sprite.flipX = dx < 0f;
            float s = 1f + Mathf.Sin(Time.time * breathSpeed + phase) * breathAmount;
            sprite.transform.localScale = new Vector3(1f, s, 1f);
        }
    }
}
