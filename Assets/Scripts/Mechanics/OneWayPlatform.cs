using UnityEngine;

namespace Platformer.Mechanics
{
    /// <summary>
    /// Marca um colisor como plataforma de mão única: o jogador atravessa por baixo e pelos lados,
    /// e só fica em pé quando cai em cima. Segurar "para baixo" faz descer dela.
    /// A regra fica em KinematicObject.PassesThrough.
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class OneWayPlatform : MonoBehaviour
    {
        void OnDrawGizmos()
        {
            var c = GetComponent<Collider2D>();
            if (!c) return;
            var b = c.bounds;
            Gizmos.color = new Color(1f, 0.75f, 0.2f, 0.9f);
            Gizmos.DrawLine(new Vector3(b.min.x, b.max.y), new Vector3(b.max.x, b.max.y));
        }
    }
}
