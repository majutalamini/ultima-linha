using Platformer.Mechanics;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Desenho do Kai por cima do jogador do template. Enquanto não há animação de andar,
    /// dá vida ao desenho parado: balança ao andar, estica no pulo e vira para o lado certo.
    /// </summary>
    [RequireComponent(typeof(SpriteRenderer))]
    public class CharacterVisual : MonoBehaviour
    {
        public KinematicObject body;
        [Tooltip("SpriteRenderer original do jogador (escondido): copia o lado para onde ele olha.")]
        public SpriteRenderer source;
        public float bobHeight = 0.06f;
        public float bobSpeed = 10f;
        public float lean = 2.5f;

        SpriteRenderer sprite;
        Vector3 basePos;
        float phase;

        void Awake()
        {
            sprite = GetComponent<SpriteRenderer>();
            basePos = transform.localPosition;
        }

        void LateUpdate()
        {
            if (!body) return;
            if (source) sprite.flipX = source.flipX;

            float vx = body.velocity.x;
            bool walking = body.IsGrounded && Mathf.Abs(vx) > 0.1f;
            if (walking) phase += Time.deltaTime * bobSpeed;
            else phase = Mathf.MoveTowards(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, Time.deltaTime * 8f);

            float bob = Mathf.Abs(Mathf.Sin(phase)) * bobHeight;
            float sy = 1f, sx = 1f;
            if (!body.IsGrounded)
            {
                sy = 1f + Mathf.Clamp(body.velocity.y * 0.008f, -0.04f, 0.05f);
                sx = 2f - sy;
            }
            transform.localPosition = basePos + Vector3.up * bob;
            transform.localScale = new Vector3(sx, sy, 1f);
            transform.localRotation = Quaternion.Euler(0f, 0f, walking ? -Mathf.Sign(vx) * lean : 0f);
        }
    }
}
