using System;
using UnityEngine;

namespace UltimaLinha.Cenario2
{
    /// <summary>
    /// Porta entre vagões. Com o trem andando, ela abre e bate sozinha num ritmo:
    /// aberta → pisca (aviso) → bate (desce rápido) → fechada → sobe devagar → aberta...
    /// A folha desce do alto da passagem; enquanto está baixa, um colisor impede a passagem.
    /// Quem estiver embaixo quando ela bate é esmagado (o Cenario2Director confere com IsCrushing).
    /// </summary>
    public class CarDoor : MonoBehaviour
    {
        public enum State { Open, Warning, Closing, Closed, Opening }

        [Header("Peças")]
        [Tooltip("Folha da porta (pivô na ponta de baixo).")]
        public Transform panel;
        [Tooltip("Colisor sólido que acompanha a folha.")]
        public BoxCollider2D blocker;
        public SpriteRenderer lamp;
        public SpriteRenderer lampGlow;

        [Header("Medidas (unidades, relativas a este objeto, que fica no chão no meio da passagem)")]
        public float doorwayHeight = 3.72f;
        public float halfWidth = 0.35f;

        [Header("Ritmo (segundos)")]
        public float openTime = 2.8f;
        [Tooltip("Tempos de abertura em sequência (substituem openTime). Vazio = sempre openTime.")]
        public float[] openPattern = new float[0];
        public float warnTime = 0.8f;
        public float closeTime = 0.2f;
        public float closedTime = 1.4f;
        public float openingTime = 0.7f;
        [Tooltip("Adianta o ciclo (para portas que não abrem junto com as outras).")]
        public float startOffset = 0f;
        [Tooltip("Desligado = a porta fica parada (aberta ou fechada) até alguém chamar Open/Close.")]
        public bool cycling = true;
        public bool startClosed = false;

        [Header("Som")]
        public AudioSource sfx;
        public AudioClip slamClip;
        public AudioClip warnClip;
        public AudioClip openClip;
        [Tooltip("Normalmente o Kai: o som fica mais baixo quando ele está longe.")]
        public Transform listener;
        public float hearDistance = 14f;

        [Header("Cores da lâmpada")]
        public Color openColor = new Color(0.35f, 0.85f, 0.45f, 1f);
        public Color warnColor = new Color(1f, 0.2f, 0.12f, 1f);
        public Color closedColor = new Color(0.8f, 0.1f, 0.06f, 1f);
        public Color openingColor = new Color(1f, 0.6f, 0.15f, 1f);

        public State Current { get; private set; } = State.Open;
        /// <summary>0 = toda aberta (folha no alto), 1 = fechada (folha no chão).</summary>
        public float Closure { get; private set; }
        public event Action<CarDoor> Slammed;

        float timer;
        int patternIndex;
        bool held;

        public float BottomY => transform.position.y + (1f - Closure) * doorwayHeight;
        public float CenterX => transform.position.x;

        void Start()
        {
            if (startClosed) { Current = State.Closed; Closure = 1f; }
            if (!cycling) held = true;
            else
            {
                // avança o ciclo pelo startOffset sem tocar sons
                float off = Mathf.Max(0f, startOffset);
                while (off > 0f)
                {
                    float left = Duration(Current) - timer;
                    if (off < left) { timer += off; break; }
                    off -= left;
                    Next(silent: true);
                }
            }
            Apply();
        }

        void Update()
        {
            if (!held)
            {
                timer += Time.deltaTime;
                if (timer >= Duration(Current)) Next(silent: false);
            }
            else if (Current == State.Closing || Current == State.Opening)
            {
                timer += Time.deltaTime;
                if (timer >= Duration(Current))
                {
                    var finished = Current;
                    Current = finished == State.Closing ? State.Closed : State.Open;
                    timer = 0f;
                    if (finished == State.Closing) OnSlam(false);
                }
            }
            Apply();
        }

        float Duration(State s)
        {
            switch (s)
            {
                case State.Open:
                    return openPattern != null && openPattern.Length > 0 ? openPattern[patternIndex % openPattern.Length] : openTime;
                case State.Warning: return warnTime;
                case State.Closing: return closeTime;
                case State.Closed: return closedTime;
                default: return openingTime;
            }
        }

        void Next(bool silent)
        {
            timer = 0f;
            switch (Current)
            {
                case State.Open:
                    Current = State.Warning;
                    if (!silent) Play(warnClip, 0.7f);
                    break;
                case State.Warning:
                    Current = State.Closing;
                    break;
                case State.Closing:
                    Current = State.Closed;
                    if (!silent) OnSlam(false);
                    break;
                case State.Closed:
                    Current = State.Opening;
                    if (!silent) Play(openClip, 0.6f);
                    break;
                default:
                    Current = State.Open;
                    patternIndex++;
                    break;
            }
        }

        void OnSlam(bool silent)
        {
            if (!silent) Play(slamClip, 1f);
            Slammed?.Invoke(this);
        }

        void Apply()
        {
            float k = Mathf.Clamp01(timer / Mathf.Max(0.01f, Duration(Current)));
            switch (Current)
            {
                case State.Open:
                case State.Warning: Closure = 0f; break;
                case State.Closing: Closure = k * k; break; // acelera: bate
                case State.Closed: Closure = 1f; break;
                default: Closure = 1f - Mathf.SmoothStep(0f, 1f, k); break;
            }
            if (panel) panel.localPosition = new Vector3(0f, (1f - Closure) * doorwayHeight, 0f);

            if (blocker)
            {
                float bottom = (1f - Closure) * doorwayHeight;
                float h = doorwayHeight - bottom;
                blocker.enabled = h > 0.05f;
                blocker.size = new Vector2(halfWidth * 2f, Mathf.Max(0.05f, h));
                blocker.offset = new Vector2(0f, bottom + h * 0.5f);
            }

            if (lamp)
            {
                Color c; float glow;
                switch (Current)
                {
                    case State.Warning:
                        bool on = Mathf.Repeat(Time.time * 7f, 1f) < 0.55f;
                        c = on ? warnColor : warnColor * 0.25f; glow = on ? 0.75f : 0.05f; break;
                    case State.Closing:
                    case State.Closed: c = closedColor; glow = 0.45f; break;
                    case State.Opening: c = openingColor; glow = 0.35f; break;
                    default: c = openColor; glow = 0.22f; break;
                }
                c.a = 1f;
                lamp.color = c;
                if (lampGlow) { c.a = glow; lampGlow.color = c; }
            }
        }

        void Play(AudioClip clip, float volume)
        {
            if (!sfx || !clip) return;
            float v = volume;
            if (listener)
            {
                float d = Mathf.Abs(listener.position.x - transform.position.x);
                v *= Mathf.Clamp01(1f - d / hearDistance);
            }
            if (v > 0.02f) sfx.PlayOneShot(clip, v);
        }

        /// <summary>Esmagaria um corpo com essas bordas? (só quando a folha está descendo ou fechada).</summary>
        public bool IsCrushing(Bounds b)
        {
            if (Current != State.Closing && Current != State.Closed) return false;
            if (b.max.x <= CenterX - halfWidth + 0.04f || b.min.x >= CenterX + halfWidth - 0.04f) return false;
            return BottomY < b.max.y - 0.08f && BottomY > b.min.y - 0.5f;
        }

        /// <summary>Para o ciclo e deixa a porta aberta (o trem parou).</summary>
        public void StopOpen()
        {
            cycling = false;
            held = true;
            if (Current == State.Open || Current == State.Warning) { Current = State.Open; timer = 0f; return; }
            if (Current == State.Opening) return;
            Current = State.Opening;
            timer = 0f;
            Play(openClip, 0.6f);
        }

        /// <summary>Abre uma porta parada (a porta do fim do último vagão).</summary>
        public void Open()
        {
            held = true;
            cycling = false;
            if (Current == State.Open || Current == State.Opening) return;
            Current = State.Opening;
            timer = 0f;
            Play(openClip, 0.8f);
        }

        public bool IsOpen => Current == State.Open || Current == State.Warning;
    }
}
