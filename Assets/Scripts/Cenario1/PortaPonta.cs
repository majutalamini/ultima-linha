using System;
using System.Collections;
using UnityEngine;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Porta da ponta do vagão (para o vão entre os vagões), na linha do pilar da ponta. É de dobradiça e abre para fora:
    /// fechada, aparece de perfil (uma lâmina); aberta, gira e fica de frente, no fundo, atrás da plataforma. A animação é
    /// a troca de quadros (todos do mesmo tamanho, com a dobradiça no pivô). Fechada, um colisor no pilar segura o Kai.
    /// Não se atravessa com E: aberta, passa andando. Trancada, E chama onLockedInteract.
    /// </summary>
    public class PortaPonta : Interactable
    {
        public SpriteRenderer folha;
        [Tooltip("Do primeiro (fechada, de perfil) ao último (aberta, de frente).")]
        public Sprite[] quadros;
        public Collider2D bloqueio;
        public float tempo = 0.7f;

        [Header("Luz de trava")]
        public SpriteRenderer luz;
        public SpriteRenderer halo;
        public Color travada = new Color(0.85f, 0.12f, 0.08f, 1f);
        public Color livre = new Color(0.45f, 1f, 0.55f, 1f);

        [Header("Som")]
        public AudioSource sfx;
        public AudioClip abreClip;
        public AudioClip fechaClip;

        [NonSerialized] public Action onLockedInteract;

        public bool IsOpen { get; private set; }
        bool movendo;

        // desligada (porta só de cenário), fica com a cor com que foi montada
        void Start()
        {
            if (!IsOpen) Luz(travada, 0.4f);
        }

        void Update()
        {
            if (!IsOpen && !movendo) Luz(travada, 0.3f + 0.15f * Mathf.Sin(Time.time * 2.2f));
        }

        public override bool CanInteract => base.CanInteract && !IsOpen && !movendo && !DialogueBox.IsBusy;

        public override void Interact() => onLockedInteract?.Invoke();

        public void Open()
        {
            if (IsOpen || movendo) return;
            StartCoroutine(Gira(true));
        }

        public void Close()
        {
            if (!IsOpen || movendo) return;
            StartCoroutine(Gira(false));
        }

        /// <summary>Aberta na hora, sem som (também no editor, ao montar a cena).</summary>
        public void AbertaJa()
        {
            IsOpen = true;
            Quadro(1f);
            if (bloqueio) bloqueio.enabled = false;
            Luz(livre, 0.6f);
        }

        IEnumerator Gira(bool abrir)
        {
            movendo = true;
            if (abrir)
            {
                Luz(livre, 0.6f);
                if (sfx && abreClip) sfx.PlayOneShot(abreClip);
                yield return new WaitForSeconds(0.3f);
            }
            float t = 0f;
            while (t < tempo)
            {
                t += Time.deltaTime;
                float k = abrir ? Mathf.SmoothStep(0f, 1f, t / tempo) : 1f - (t / tempo) * (t / tempo); // fecha acelerando: bate
                Quadro(k);
                yield return null;
            }
            Quadro(abrir ? 1f : 0f);
            IsOpen = abrir;
            if (bloqueio) bloqueio.enabled = !abrir;
            if (!abrir)
            {
                if (sfx && fechaClip) sfx.PlayOneShot(fechaClip);
                Luz(travada, 1f);
            }
            movendo = false;
        }

        /// <summary>0 = fechada (de perfil), 1 = aberta (de frente); o montador da cena a deixa fechada.</summary>
        public void Quadro(float aberta)
        {
            if (folha && quadros != null && quadros.Length > 0)
                folha.sprite = quadros[Mathf.Clamp(Mathf.RoundToInt(aberta * (quadros.Length - 1)), 0, quadros.Length - 1)];
        }

        void Luz(Color c, float brilho)
        {
            c.a = 1f;
            if (luz) luz.color = c;
            if (!halo) return;
            c.a = brilho;
            halo.color = c;
        }
    }
}
