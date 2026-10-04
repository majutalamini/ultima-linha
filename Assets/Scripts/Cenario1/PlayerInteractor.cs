using Platformer.Mechanics;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Fica no Kai: acha o objeto interativo mais próximo ao alcance, mostra a tecla E em cima dele
    /// e chama Interact() quando o jogador aperta E.
    /// </summary>
    public class PlayerInteractor : MonoBehaviour
    {
        public PlayerController player;
        [Tooltip("Sprite da tecla E que aparece sobre o objeto.")]
        public SpriteRenderer prompt;

        public Interactable Current { get; private set; }

        float promptAlpha;

        void Update()
        {
            Current = null;
            if (player && player.controlEnabled && !DialogueBox.IsBusy)
            {
                var b = player.Bounds;
                var feet = new Vector2(b.center.x, b.min.y);
                float best = float.MaxValue;
                foreach (var it in Interactable.Active)
                {
                    if (!it || !it.CanInteract || !it.InReach(feet)) continue;
                    float dx = Mathf.Abs(feet.x - it.transform.position.x);
                    if (dx < best)
                    {
                        best = dx;
                        Current = it;
                    }
                }
            }

            if (prompt)
            {
                promptAlpha = Mathf.MoveTowards(promptAlpha, Current ? 1f : 0f, Time.deltaTime * 6f);
                var c = prompt.color;
                c.a = promptAlpha;
                prompt.color = c;
                prompt.enabled = promptAlpha > 0.01f;
                if (Current)
                    prompt.transform.position = Current.PromptPosition + Vector3.up * (Mathf.Sin(Time.time * 4f) * 0.05f);
            }

            if (Current && InputHelper.InteractPressed())
                Current.Interact();
        }
    }
}
