using System.Collections.Generic;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Personagem parado no vagão (Souta, Ren). Olha para o Kai e conversa quando ele aperta E.
    /// Primeira conversa, conversas seguintes e conversa depois que a porta abre são listas separadas.
    /// </summary>
    public class NpcTalk : Interactable
    {
        public SpriteRenderer sprite;
        [Tooltip("Normalmente o Kai: o personagem vira o rosto para ele.")]
        public Transform lookAt;
        public LeverPuzzle puzzle;

        [Header("Falas")]
        public List<DialogueLine> firstTalk = new List<DialogueLine>();
        public List<DialogueLine> repeatTalk = new List<DialogueLine>();
        public List<DialogueLine> afterSolved = new List<DialogueLine>();

        [Header("Respiração")]
        public float breathAmount = 0.008f;
        public float breathSpeed = 1.6f;

        public int TimesTalked { get; private set; }

        float phase;

        void Awake()
        {
            phase = Random.value * 10f;
        }

        public override bool CanInteract => base.CanInteract && !DialogueBox.IsBusy;

        public override void Interact()
        {
            List<DialogueLine> lines;
            if (puzzle && puzzle.IsSolved && afterSolved.Count > 0) lines = afterSolved;
            else if (TimesTalked > 0 && repeatTalk.Count > 0) lines = repeatTalk;
            else lines = firstTalk;
            TimesTalked++;
            if (DialogueBox.Instance) DialogueBox.Instance.Play(lines);
        }

        void Update()
        {
            if (!sprite) return;
            if (lookAt)
            {
                float dx = lookAt.position.x - transform.position.x;
                if (Mathf.Abs(dx) > 0.2f) sprite.flipX = dx < 0f; // desenhos olham para a direita
            }
            float s = 1f + Mathf.Sin(Time.time * breathSpeed + phase) * breathAmount;
            sprite.transform.localScale = new Vector3(1f, s, 1f);
        }
    }
}
