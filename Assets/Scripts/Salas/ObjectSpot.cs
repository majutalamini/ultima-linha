using System;
using System.Collections;
using System.Collections.Generic;
using Platformer.Core;
using Platformer.Model;
using TMPro;
using UltimaLinha.Cenario1;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Lugar onde o Kai olha ou vasculha apertando E: gaveta, arquivo, bilhete, relógio, o corpo...
    /// Mostra um documento (se tiver) e depois as falas, e avisa o diretor da cena (Presented).
    /// A primeira vez usa "first"; as seguintes usam "again" (se vazio, repete "first").
    /// </summary>
    public class ObjectSpot : Interactable
    {
        public string id;
        [Tooltip("Desligado: o Kai ainda não pode usar (o diretor liga na hora certa).")]
        public bool available = true;
        public List<DialogueLine> first = new List<DialogueLine>();
        public List<DialogueLine> again = new List<DialogueLine>();
        [Header("Documento (opcional)")]
        public string docTitle;
        [TextArea(3, 12)] public string docBody;
        public string docFooter;
        [Tooltip("Mostra o documento só na primeira vez.")]
        public bool docOnlyOnce;
        [Header("Visual")]
        [Tooltip("Aparece depois da primeira vez (gaveta aberta).")]
        public GameObject openVisual;
        public AudioSource sfx;
        public AudioClip useClip;

        /// <summary>Chamado quando o documento e as falas acabaram. O parâmetro diz se foi a primeira vez.</summary>
        [NonSerialized] public Action<ObjectSpot, bool> Presented;
        /// <summary>Chamado assim que o Kai aperta E (antes de mostrar qualquer coisa).</summary>
        [NonSerialized] public Func<ObjectSpot, bool> Intercept;

        public int Uses { get; private set; }
        public bool Busy { get; private set; }

        public override bool CanInteract => base.CanInteract && available && !Busy && !DialogueBox.IsBusy && !Modal.IsOpen;

        public override void Interact()
        {
            if (!CanInteract) return;
            if (PersonagemPixel.Jogador) PersonagemPixel.Jogador.Tocar("search");
            if (Intercept != null && Intercept(this)) return;
            StartCoroutine(Present());
        }

        public IEnumerator Present()
        {
            Busy = true;
            bool firstTime = Uses == 0;
            Uses++;
            if (sfx && useClip) sfx.PlayOneShot(useClip);
            if (firstTime && openVisual) openVisual.SetActive(true);

            if (!string.IsNullOrEmpty(docBody) && (firstTime || !docOnlyOnce) && DocumentView.Instance)
            {
                bool closed = false;
                DocumentView.Instance.Show(docTitle, docBody, docFooter, () => closed = true);
                while (!closed) yield return null;
            }

            var lines = !firstTime && again.Count > 0 ? again : first;
            if (lines.Count > 0 && DialogueBox.Instance)
            {
                bool done = false;
                DialogueBox.Instance.Play(lines, () => done = true);
                while (!done) yield return null;
            }
            Busy = false;
            Presented?.Invoke(this, firstTime);
        }
    }
}
