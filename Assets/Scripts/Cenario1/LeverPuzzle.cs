using System;
using System.Collections;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Puzzle do Cenário 1: três alavancas em plataformas altas do vagão precisam ser puxadas na ordem certa.
    ///
    /// Pista: de tempos em tempos, as lâmpadas das alavancas e os pontos do letreiro em cima das portas
    /// piscam na ordem certa e depois apagam ("resetam").
    /// Errou a ordem: tudo pisca em vermelho, a porta trava de novo, as alavancas voltam e a pista se repete.
    /// Acertou as três: a porta do fundo destrava e abre.
    /// </summary>
    public class LeverPuzzle : MonoBehaviour
    {
        [Tooltip("As alavancas, da esquerda para a direita (é assim que aparecem no letreiro).")]
        public TrainLever[] levers;
        [Tooltip("Ordem certa, em índices da lista acima (0 = mais à esquerda).")]
        public int[] order = { 1, 0, 2 };
        public SequencePanel[] panels;
        public ExitDoor door;

        [Header("Som")]
        public AudioSource sfx;
        [Tooltip("Fonte separada para o bipe da pista (o tom sobe a cada luz).")]
        public AudioSource beepSource;
        public AudioClip beepClip;
        public AudioClip stepClip;
        public AudioClip wrongClip;
        public AudioClip solvedClip;

        [Header("Ritmo da pista")]
        public float firstHintDelay = 3f;
        public float replayDelay = 6f;
        public float blinkOn = 0.5f;
        public float blinkGap = 0.28f;

        [Header("Cores")]
        public Color lampOff = new Color(0.22f, 0.07f, 0.05f, 1f);
        public Color lampBlink = new Color(1f, 0.86f, 0.6f, 1f);
        public Color lampHeld = new Color(1f, 0.6f, 0.18f, 1f);
        public Color lampWrong = new Color(1f, 0.12f, 0.08f, 1f);
        public Color lampSolved = new Color(0.5f, 1f, 0.6f, 1f);

        public bool IsSolved { get; private set; }
        public bool IsBusy { get; private set; }
        public int Step => step;

        public event Action Solved;
        public event Action Failed;
        public event Action<int> Progressed;

        int step;
        bool[] held;
        float nextHint;
        Coroutine hint;

        void Start()
        {
            held = new bool[levers.Length];
            ShowIdle();
            nextHint = Time.time + firstHintDelay;
        }

        void Update()
        {
            if (IsSolved || IsBusy || hint != null) return;
            if (DialogueBox.IsBusy)
            {
                nextHint = Mathf.Max(nextHint, Time.time + 1.2f);
                return;
            }
            if (Time.time >= nextHint) hint = StartCoroutine(PlayHint());
        }

        /// <summary>Faz a pista tocar logo (por exemplo, depois de um diálogo que fala dela).</summary>
        public void HintSoon(float delay = 0.8f)
        {
            if (hint == null) nextHint = Time.time + delay;
        }

        IEnumerator PlayHint()
        {
            for (int k = 0; k < order.Length; k++)
            {
                int i = order[k];
                Light(i, lampBlink, 1f);
                if (beepSource && beepClip)
                {
                    beepSource.pitch = 1f + 0.14f * k;
                    beepSource.PlayOneShot(beepClip);
                }
                yield return new WaitForSeconds(blinkOn);
                ShowIdle(i);
                yield return new WaitForSeconds(blinkGap);
            }
            hint = null;
            nextHint = Time.time + replayDelay;
        }

        void StopHint()
        {
            if (hint != null) StopCoroutine(hint);
            hint = null;
        }

        public void OnLeverPulled(TrainLever lever)
        {
            int i = Array.IndexOf(levers, lever);
            if (i < 0 || IsSolved || IsBusy) return;

            if (order[step] == i)
            {
                held[i] = true;
                step++;
                Light(i, lampHeld, 0.6f);
                if (sfx && stepClip) sfx.PlayOneShot(stepClip, 0.8f);
                Progressed?.Invoke(step);
                if (step >= order.Length) StartCoroutine(SolveRoutine());
            }
            else
            {
                StartCoroutine(FailRoutine());
            }
        }

        IEnumerator FailRoutine()
        {
            IsBusy = true;
            StopHint();
            if (sfx && wrongClip) sfx.PlayOneShot(wrongClip);
            if (door) door.FlashLocked();
            Failed?.Invoke();

            for (int n = 0; n < 3; n++)
            {
                LightAll(lampWrong, 0.9f);
                yield return new WaitForSeconds(0.18f);
                LightAll(lampOff, 0f);
                yield return new WaitForSeconds(0.14f);
            }
            yield return new WaitForSeconds(0.3f);

            foreach (var l in levers) l.ResetLever();
            for (int i = 0; i < held.Length; i++) held[i] = false;
            step = 0;
            yield return new WaitForSeconds(0.6f);

            ShowIdle();
            IsBusy = false;
            nextHint = Time.time + 0.9f; // repete a pista logo depois do erro
        }

        IEnumerator SolveRoutine()
        {
            IsSolved = true;
            StopHint();
            yield return new WaitForSeconds(0.45f);
            if (sfx && solvedClip) sfx.PlayOneShot(solvedClip);
            LightAll(lampSolved, 0.7f);
            if (door) door.Unlock();
            Solved?.Invoke();
        }

        void ShowIdle()
        {
            for (int i = 0; i < levers.Length; i++) ShowIdle(i);
        }

        void ShowIdle(int i)
        {
            if (IsSolved) Light(i, lampSolved, 0.7f);
            else if (held != null && held[i]) Light(i, lampHeld, 0.6f);
            else Light(i, lampOff, 0f);
        }

        void LightAll(Color c, float glow)
        {
            for (int i = 0; i < levers.Length; i++) Light(i, c, glow);
        }

        void Light(int i, Color c, float glow)
        {
            if (levers[i]) levers[i].SetLamp(c, glow);
            if (panels == null) return;
            foreach (var p in panels)
                if (p) p.Set(i, c, glow * 0.8f);
        }
    }
}
