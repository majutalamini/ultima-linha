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
        [Tooltip("Personagem articulado (o normal). Se estiver vazio, usa o sprite simples abaixo.")]
        public CharacterRig2D rig;
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

        [Header("Respiração (só no sprite simples)")]
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
            if (door && door.IsOpen && afterDoor.Count > 0) lines = afterDoor;
            else if (puzzle && puzzle.IsSolved && afterSolved.Count > 0) lines = afterSolved;
            else if (TimesTalked > 0 && repeatTalk.Count > 0) lines = repeatTalk;
            else lines = firstTalk;
            TimesTalked++;
            if (DialogueBox.Instance) DialogueBox.Instance.Play(lines);
        }

        void Update()
        {
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
