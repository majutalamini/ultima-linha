using System.Collections.Generic;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Qualquer coisa com que o Kai pode interagir apertando E (alavancas, personagens, porta).
    /// O ponto de referência é a posição deste objeto, que fica no chão/plataforma onde ele está.
    /// </summary>
    public abstract class Interactable : MonoBehaviour
    {
        public static readonly List<Interactable> Active = new List<Interactable>();

        [Header("Alcance")]
        [Tooltip("Distância horizontal máxima entre o Kai e este objeto.")]
        public float reachX = 1.3f;
        [Tooltip("Altura dos pés do Kai em relação a este objeto (mínimo). Impede interagir de outro andar.")]
        public float minFeetDy = -0.45f;
        [Tooltip("Altura dos pés do Kai em relação a este objeto (máximo).")]
        public float maxFeetDy = 0.6f;
        [Tooltip("Onde aparece o ícone da tecla E.")]
        public Vector2 promptOffset = new Vector2(0f, 1.6f);

        public virtual bool CanInteract => isActiveAndEnabled;

        public Vector3 PromptPosition => transform.position + (Vector3)promptOffset;

        public virtual bool InReach(Vector2 feet)
        {
            var p = transform.position;
            float dy = feet.y - p.y;
            return Mathf.Abs(feet.x - p.x) <= reachX && dy >= minFeetDy && dy <= maxFeetDy;
        }

        public abstract void Interact();

        protected virtual void OnEnable()
        {
            if (!Active.Contains(this)) Active.Add(this);
        }

        protected virtual void OnDisable()
        {
            Active.Remove(this);
        }

        protected virtual void OnDrawGizmosSelected()
        {
            var p = transform.position;
            Gizmos.color = new Color(0.4f, 1f, 0.6f, 0.8f);
            Gizmos.DrawWireCube(new Vector3(p.x, p.y + (minFeetDy + maxFeetDy) * 0.5f),
                new Vector3(reachX * 2f, maxFeetDy - minFeetDy));
        }
    }
}
