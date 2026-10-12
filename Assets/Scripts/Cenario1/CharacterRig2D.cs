using Platformer.Mechanics;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    public enum RigStyle { Kai, Souta, Ren }

    /// <summary>
    /// Personagem articulado de perfil (cabeça, tronco, braços e pernas separados) animado por código:
    /// caminhada, pulo, respiração e o jeito de cada um.
    ///  - Kai: curvado, cabeça baixa, passos pesados, suspira de vez em quando.
    ///  - Souta: postura reta, queixo erguido, de vez em quando olha o relógio.
    ///  - Ren: encolhido, mãos juntas na frente, tremendo, olha para trás assustado.
    /// No jogador, lê a velocidade do KinematicObject; nos NPCs, só respira e vira para o Kai.
    /// Os ângulos seguem a mesma conta da prévia (Ferramentas/Cenario1/rig_anim.py).
    /// </summary>
    public class CharacterRig2D : MonoBehaviour
    {
        public RigStyle style;
        [Tooltip("Id de quem fala no diálogo (kai, souta, ren): mexe a cabeça enquanto a fala dele aparece.")]
        public string speakerId;

        [Header("Ossos")]
        public Transform body;
        public Transform torso, head;
        public Transform thighF, shinF, footF, thighB, shinB, footB;
        public Transform upperF, foreF, upperB, foreB;
        public SpriteRenderer shadow;

        [Header("Movimento (jogador)")]
        [Tooltip("Corpo do jogador: a velocidade dele comanda a caminhada e o pulo.")]
        public KinematicObject mover;
        [Tooltip("SpriteRenderer original do jogador (escondido): diz para que lado ele olha.")]
        public SpriteRenderer flipSource;
        [Tooltip("Distância (unidades) de um ciclo completo de passos.")]
        public float strideLength = 2.5f;

        [Header("Altura do quadril (unidades)")]
        public float hipHeight = 1.64f;

        /// <summary>1 = olhando para a direita, -1 = para a esquerda (NPCs).</summary>
        [HideInInspector] public float facing = 1f;

        float phase, walk, air, t, seed;
        float sighT = 5f, watchT = 7f, glanceT = 4f;
        float sigh, watch, glance;

        struct Style
        {
            public float lean, head, stride, knee, arm, bob, elbow, shoulder;
            public bool clasp;
        }

        Style S
        {
            get
            {
                switch (style)
                {
                    case RigStyle.Souta: return new Style { lean = -1, head = -3, stride = 26, knee = 30, arm = 22, bob = 2.2f, elbow = 14, shoulder = 0 };
                    case RigStyle.Ren: return new Style { lean = 12, head = 16, stride = 16, knee = 30, arm = 6, bob = 2.0f, elbow = 62, shoulder = 6, clasp = true };
                    default: return new Style { lean = 7, head = 10, stride = 22, knee = 34, arm = 16, bob = 3.0f, elbow = 10, shoulder = 3 };
                }
            }
        }

        void Awake()
        {
            seed = Random.value * 50f;
            sighT = Random.Range(4f, 9f);
            watchT = Random.Range(5f, 10f);
            glanceT = Random.Range(3f, 7f);
        }

        void LateUpdate()
        {
            float dt = Time.deltaTime;
            t += dt;
            var s = S;

            // entrada: velocidade do jogador ou NPC parado
            float vx = 0f, vy = 0f;
            bool grounded = true;
            if (mover)
            {
                vx = mover.velocity.x;
                vy = mover.velocity.y;
                grounded = mover.IsGrounded;
            }
            if (flipSource) facing = flipSource.flipX ? -1f : 1f;
            transform.localScale = new Vector3(Mathf.Abs(transform.localScale.x) * Mathf.Sign(facing), transform.localScale.y, 1f);

            float speed = Mathf.Abs(vx);
            walk = Mathf.MoveTowards(walk, grounded && speed > 0.15f ? 1f : 0f, dt * 5f);
            air = Mathf.MoveTowards(air, grounded ? 0f : 1f, dt * (grounded ? 10f : 6f));
            if (speed > 0.05f) phase += dt * speed / Mathf.Max(0.5f, strideLength) * Mathf.PI * 2f;
            else if (walk < 0.05f) phase = Mathf.MoveTowards(phase, Mathf.Round(phase / Mathf.PI) * Mathf.PI, dt * 4f);

            float breath = Mathf.Sin(t * 2f + seed);
            float idle = 1f - walk;

            // jeitos de cada um (só parado)
            UpdateMannerisms(dt, idle);

            // tronco e cabeça
            float bob = s.bob * (0.5f + 0.5f * Mathf.Cos(2f * phase)) * walk - 1.2f * walk;
            float tremble = style == RigStyle.Ren ? (Mathf.PerlinNoise(t * 9f, seed) - 0.5f) * 1.6f : 0f;
            float torsoA = -s.lean - 2f * walk + breath * 0.6f * idle - sigh * 3f + tremble - 4f * air;
            float headA = -s.head + 1.5f * Mathf.Sin(2f * phase) * walk + breath * 0.8f * idle
                          - sigh * 4f + tremble * 1.5f + glance * 30f + watch * -14f + Talking() * Mathf.Sin(t * 11f) * 2.5f;
            if (body) body.localPosition = new Vector3(0f, hipHeight + (bob + breath * 0.4f * idle + sigh * 1.5f) / 100f, 0f);
            Rot(torso, torsoA);
            Rot(head, headA);

            // pernas e braços
            Leg(thighF, shinF, footF, phase, s, torsoA);
            Leg(thighB, shinB, footB, phase + Mathf.PI, s, torsoA);
            Arm(upperF, foreF, phase, s, breath, idle, true);
            Arm(upperB, foreB, phase + Mathf.PI, s, breath, idle, false);

            if (shadow)
            {
                var c = shadow.color;
                c.a = Mathf.Lerp(0.55f, 0.15f, air);
                shadow.color = c;
            }
        }

        void Leg(Transform thigh, Transform shin, Transform foot, float ph, Style s, float torsoA)
        {
            float a = Mathf.Sin(ph), c = Mathf.Cos(ph);
            float th = s.stride * a * walk;
            float knee = s.knee * Mathf.Pow(Mathf.Max(0f, c), 1.4f) * walk + 4f;
            float ft = -(th - knee) * 0.75f + (a > 0f ? 8f * Mathf.Max(0f, -c) * walk : 0f);
            bool front = thigh == thighF;
            if (air > 0f)
            {
                th = Mathf.Lerp(th, front ? 38f : -6f, air);
                knee = Mathf.Lerp(knee, front ? 55f : 30f, air);
                ft = Mathf.Lerp(ft, front ? 10f : 0f, air);
            }
            Rot(thigh, th);
            Rot(shin, -knee);
            Rot(foot, ft);
        }

        void Arm(Transform upper, Transform fore, float ph, Style s, float breath, float idle, bool front)
        {
            float a = Mathf.Sin(ph);
            float swing = -s.arm * a * walk;
            float u, f;
            if (s.clasp)
            {
                u = 2f + swing * 0.3f + breath * 1.0f;
                f = s.elbow;
            }
            else
            {
                u = swing + s.shoulder + breath * 0.8f * idle;
                f = s.elbow + 12f * Mathf.Max(0f, -a) * walk;
            }
            if (front && watch > 0f) // Souta olhando o relógio
            {
                u = Mathf.Lerp(u, 32f, watch);
                f = Mathf.Lerp(f, 105f, watch);
            }
            if (air > 0f)
            {
                u = Mathf.Lerp(u, front ? -30f : 40f, air);
                f = Mathf.Lerp(f, 25f, air);
            }
            Rot(upper, u);
            Rot(fore, f);
        }

        void UpdateMannerisms(float dt, float idle)
        {
            switch (style)
            {
                case RigStyle.Kai:
                    sighT -= dt;
                    if (sighT < 0f) sighT = Random.Range(6f, 11f);
                    sigh = idle * Bump(sighT, 6f, 1.6f);
                    break;
                case RigStyle.Souta:
                    watchT -= dt;
                    if (watchT < 0f) watchT = Random.Range(8f, 13f);
                    watch = idle * Bump(watchT, 8f, 2.2f);
                    break;
                case RigStyle.Ren:
                    glanceT -= dt;
                    if (glanceT < 0f) glanceT = Random.Range(4f, 8f);
                    glance = idle * Bump(glanceT, 4f, 0.9f);
                    break;
            }
        }

        /// <summary>Sobe e desce suave no começo de cada ciclo do contador (0 → 1 → 0 em "duration" segundos).</summary>
        static float Bump(float timer, float cycleStart, float duration)
        {
            float into = cycleStart - timer;
            if (into < 0f || into > duration) return 0f;
            return Mathf.Sin(into / duration * Mathf.PI);
        }

        float Talking()
        {
            return !string.IsNullOrEmpty(speakerId) && DialogueBox.Speaking == speakerId ? 1f : 0f;
        }

        static void Rot(Transform tr, float deg)
        {
            if (tr) tr.localRotation = Quaternion.Euler(0f, 0f, deg);
        }
    }
}
