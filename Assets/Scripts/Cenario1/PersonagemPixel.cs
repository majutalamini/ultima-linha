using System;
using Platformer.Mechanics;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    [Serializable]
    public class ClipePixel
    {
        public string nome;
        public Sprite[] quadros;
        public float fps = 8f;
    }

    /// <summary>
    /// Personagem em pixel art de todos os cenários (Assets/Art/Personagens/PixelArt).
    ///  - jogador: andar guiado pela distância percorrida, pulo e queda lendo o KinematicObject;
    ///  - parado: respira, fala enquanto o diálogo é dele e, de tempos em tempos, faz o gesto próprio;
    ///  - cutscenes e interações: Tocar("nome") roda um clipe extra (alavanca, arma, queda...).
    /// Os desenhos olham para a direita; facing = -1 espelha. O tamanho do desenho não influencia a colisão.
    /// </summary>
    public class PersonagemPixel : MonoBehaviour
    {
        /// <summary>O personagem controlado pelo jogador na cena atual.</summary>
        public static PersonagemPixel Jogador { get; private set; }

        [Tooltip("Id de quem fala no diálogo (kai, souta, ren).")]
        public string speakerId;

        [Header("Desenho")]
        public SpriteRenderer view;
        public SpriteRenderer shadow;
        [Tooltip("Reflexo no chão encerado/molhado (opcional): repete o quadro do desenho, de cabeça para baixo.")]
        public SpriteRenderer reflexo;
        [Tooltip("Sombra de contato (opcional): pequena e de borda dura, logo abaixo das solas.")]
        public SpriteRenderer contato;
        public float alfaSombra = 0.55f, alfaReflexo = 0.2f;
        [Tooltip("Altura (mundo) do chão que reflete; fora dele o reflexo some.")]
        public float chaoReflexo;
        public Sprite[] idle, walk, run, jump, fall, talk, act;
        public ClipePixel[] extras;
        public float idleFps = 3f, talkFps = 5f;
        [Tooltip("Velocidade do gesto próprio (suspiro, relógio, olhar para trás).")]
        public float gestoFps = 3f;
        [Tooltip("Intervalo em segundos entre um gesto e outro (mínimo, máximo).")]
        public Vector2 pausaGesto = new Vector2(18f, 32f);

        [Header("Movimento")]
        [Tooltip("Corpo do jogador: a velocidade dele comanda a caminhada e o pulo.")]
        public KinematicObject mover;
        [Tooltip("SpriteRenderer original do jogador (escondido): diz para que lado ele olha.")]
        public SpriteRenderer flipSource;
        [Tooltip("Distância (unidades) de um ciclo completo de passos.")]
        public float strideLength = 2.3f;
        [Tooltip("Velocidade vertical da decolagem (unidades/s): o quadro do pulo/queda segue a velocidade vertical.")]
        public float jumpSpeed = 10.8f;
        [Tooltip("Usa a corrida no lugar da caminhada.")]
        public bool correndo;
        [Tooltip("Distância (unidades) de um ciclo de passos correndo.")]
        public float strideCorrida = 4.2f;

        /// <summary>1 = olhando para a direita, -1 = para a esquerda.</summary>
        [HideInInspector] public float facing = 1f;
        /// <summary>Velocidade de quem anda em cena sem corpo físico (cutscenes).</summary>
        [HideInInspector] public float externalSpeed;

        float cicloPasso, clipTime, air, actTimer, actPlayed = -1f;
        ClipePixel clipe;
        float clipeTempo;
        bool segurar;

        void Awake()
        {
            actTimer = UnityEngine.Random.Range(pausaGesto.x * 0.5f, pausaGesto.y * 0.5f);
            if (mover) Jogador = this;
        }

        /// <summary>Roda um clipe extra e devolve a duração. Com segurar, fica no último quadro até Soltar;
        /// andar não cancela, só interrompe a pose enquanto dura.</summary>
        public float Tocar(string nome, bool segurar = false)
        {
            clipe = Array.Find(extras ?? Array.Empty<ClipePixel>(), c => c.nome == nome);
            if (clipe == null)
            {
                Debug.LogWarning($"[Última Linha] {speakerId} sem o clipe {nome}");
                return 0f;
            }
            this.segurar = segurar;
            clipeTempo = 0f;
            return clipe.quadros.Length / clipe.fps;
        }

        public void Soltar() => clipe = null;

        void LateUpdate()
        {
            float dt = Time.deltaTime;

            float vx = externalSpeed, vy = 0f;
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
            bool walking = grounded && speed > 0.15f;
            air = Mathf.MoveTowards(air, grounded ? 0f : 1f, dt * (grounded ? 10f : 6f));
            bool noAr = !grounded && air > 0.3f;

            if (clipe != null && !segurar && (walking || noAr)) clipe = null;

            Sprite frame;
            if (noAr)
            {
                // subindo: do impulso ao ápice conforme a velocidade cai; descendo: do ápice até o pouso
                float t = vy > 0f ? 1f - Mathf.Clamp01(vy / jumpSpeed) : Mathf.Clamp01(-vy / (jumpSpeed * 0.9f));
                var arr = vy > 0f ? jump : fall;
                frame = Pick(arr, Mathf.RoundToInt(t * (Len(arr) - 1)));
                actPlayed = -1f;
            }
            else if (walking)
            {
                bool corre = (correndo || (mover is PlayerController pc && pc.Running)) && Len(run) > 0;
                var ciclo = corre ? run : walk;
                cicloPasso += dt * speed / Mathf.Max(0.5f, corre ? strideCorrida : strideLength);
                cicloPasso -= Mathf.Floor(cicloPasso);
                frame = Pick(ciclo, Mathf.FloorToInt(cicloPasso * Len(ciclo)));
                actPlayed = -1f;
            }
            else if (clipe != null)
            {
                clipeTempo += dt;
                int i = Mathf.FloorToInt(clipeTempo * clipe.fps);
                frame = Pick(clipe.quadros, i);
                if (i >= clipe.quadros.Length && !segurar) clipe = null;
            }
            else
            {
                frame = ParadoFrame(dt);
            }
            if (view && frame) view.sprite = frame;

            if (shadow)
            {
                var c = shadow.color;
                c.a = Mathf.Lerp(alfaSombra, 0.15f, air);
                shadow.color = c;
            }
            if (contato)
            {
                var c = contato.color;
                c.a = 1f - air;
                contato.color = c;
            }
            if (reflexo && view)
            {
                reflexo.sprite = view.sprite;
                var c = view.color;
                // só o chão do vagão reflete: em cima de banco ou bagageiro o reflexo some
                float noChao = Mathf.Clamp01(1f - Mathf.Abs(transform.position.y - chaoReflexo) / 0.3f);
                c.a *= alfaReflexo * (1f - air) * noChao;
                reflexo.color = c;
            }
        }

        Sprite ParadoFrame(float dt)
        {
            bool falando = !string.IsNullOrEmpty(speakerId) && DialogueBox.Speaking == speakerId;
            if (falando)
            {
                actPlayed = -1f;
                clipTime += dt;
                return Pick(talk, Mathf.FloorToInt(clipTime * talkFps) % Mathf.Max(1, Len(talk)));
            }

            // gesto de cada um, a cada poucos segundos
            if (actPlayed >= 0f)
            {
                actPlayed += dt;
                int i = Mathf.FloorToInt(actPlayed * gestoFps);
                if (i < Len(act)) return Pick(act, i);
                actPlayed = -1f;
                actTimer = UnityEngine.Random.Range(pausaGesto.x, pausaGesto.y);
            }
            else
            {
                actTimer -= dt;
                if (actTimer <= 0f && Len(act) > 0) actPlayed = 0f;
            }
            clipTime += dt;
            return Pick(idle, Mathf.FloorToInt(clipTime * idleFps) % Mathf.Max(1, Len(idle)));
        }

        static int Len(Sprite[] a) => a == null ? 0 : a.Length;

        static Sprite Pick(Sprite[] a, int i)
        {
            if (a == null || a.Length == 0) return null;
            return a[Mathf.Clamp(i, 0, a.Length - 1)];
        }
    }
}
