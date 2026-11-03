using System.Collections;
using System.Collections.Generic;
using Platformer.Mechanics;
using UltimaLinha.Cenario1;
using UnityEngine;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Corpo do chefe (Cenário 5). Quando o Kai anda por cima dele, cada passo gruda no sangue (som pegajoso).
    /// Ao passar pela cabeça, ela se desprende do corpo e rola pelo chão.
    /// </summary>
    public class BodyCrossing : MonoBehaviour
    {
        public PlayerController player;

        [Header("Onde está o corpo (x em unidades)")]
        public float x0 = 21.4f;
        public float x1 = 25.7f;

        [Header("Cabeça")]
        public Transform head;
        [Tooltip("X da cabeça: passando por aqui, ela se solta.")]
        public float headX = 24.97f;
        public float headRoll = 1.15f;

        [Header("Som")]
        public AudioSource sfx;
        public AudioClip[] squelch = new AudioClip[0];
        public AudioClip headClip;
        [Tooltip("Distância entre um passo pegajoso e outro (unidades).")]
        public float stepDistance = 0.55f;

        [Header("Falas (só na primeira vez)")]
        public List<DialogueLine> firstSticky = new List<DialogueLine>();
        public List<DialogueLine> headLoose = new List<DialogueLine>();

        public bool HeadLoose { get; private set; }

        float travelled, lastX;
        bool saidSticky;
        int lastClip = -1;

        void Start()
        {
            if (player) lastX = player.transform.position.x;
            travelled = stepDistance * 0.6f;
        }

        void Update()
        {
            if (!player) return;
            float x = player.transform.position.x;
            float dx = x - lastX;
            lastX = x;
            bool moving = Mathf.Abs(dx) > 0.0005f;
            bool onBody = x >= x0 && x <= x1 && player.IsGrounded;

            if (onBody && moving)
            {
                travelled += Mathf.Abs(dx);
                if (travelled >= stepDistance)
                {
                    travelled = 0f;
                    PlaySquelch(1f);
                    if (!saidSticky && firstSticky.Count > 0 && DialogueBox.Instance && !DialogueBox.IsBusy && !Modal.IsOpen)
                    {
                        saidSticky = true;
                        DialogueBox.Instance.Play(firstSticky);
                    }
                }
            }
            else if (!onBody)
            {
                travelled = stepDistance * 0.6f; // ao pisar no corpo, o primeiro passo já soa
            }

            if (!HeadLoose && head && onBody && moving && Mathf.Abs(x - headX) < 0.4f)
                StartCoroutine(Detach(Mathf.Sign(dx)));
        }

        void PlaySquelch(float volume)
        {
            if (!sfx || squelch == null || squelch.Length == 0) return;
            int i = Random.Range(0, squelch.Length);
            if (squelch.Length > 1 && i == lastClip) i = (i + 1) % squelch.Length;
            lastClip = i;
            if (squelch[i]) sfx.PlayOneShot(squelch[i], volume * Random.Range(0.75f, 1f));
        }

        /// <summary>A cabeça se solta e rola para o lado em que o Kai está andando.</summary>
        IEnumerator Detach(float dir)
        {
            HeadLoose = true;
            if (sfx && headClip) sfx.PlayOneShot(headClip);
            PlaySquelch(1f);
            Vector3 p0 = head.position;
            float z0 = head.eulerAngles.z;
            float t = 0f, dur = 0.95f;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float ease = 1f - (1f - k) * (1f - k) * (1f - k);
                float hop = Mathf.Sin(Mathf.Clamp01(k * 2.2f) * Mathf.PI) * 0.22f * (1f - k);
                head.position = p0 + new Vector3(dir * headRoll * ease, hop, 0f);
                head.rotation = Quaternion.Euler(0f, 0f, z0 - dir * 250f * ease);
                yield return null;
            }
            yield return new WaitForSeconds(0.35f);
            if (headLoose.Count > 0 && DialogueBox.Instance && !DialogueBox.IsBusy && !Modal.IsOpen)
                DialogueBox.Instance.Play(headLoose);
        }
    }
}
