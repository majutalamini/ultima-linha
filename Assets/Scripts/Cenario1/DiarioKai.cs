using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace UltimaLinha.Cenario1
{
    [Serializable]
    public class PaginaDiario
    {
        public string titulo;
        [TextArea(4, 12)] public string texto;
    }

    /// <summary>
    /// Diário do Kai: Tab (Select no controle) abre e fecha a qualquer momento do jogo; A/D ou as setas viram a página.
    /// Pausa o jogo enquanto está aberto. Não abre por cima de diálogo, cutscene ou painel.
    /// </summary>
    public class DiarioKai : MonoBehaviour
    {
        public static bool IsOpen { get; private set; }

        public CanvasGroup group;
        public TMP_Text titulo;
        public TMP_Text texto;
        public TMP_Text numero;
        public List<PaginaDiario> paginas = new List<PaginaDiario>();
        public AudioSource sfx;
        public AudioClip folhaClip;

        int atual;
        float escalaAntes = 1f;

        void Awake()
        {
            IsOpen = false;
            if (group)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
        }

        void OnDestroy()
        {
            if (IsOpen) Time.timeScale = escalaAntes;
            IsOpen = false;
        }

        void Update()
        {
            if (InputHelper.DiaryPressed())
            {
                if (IsOpen) Fecha();
                else if (PodeAbrir()) Abre();
                return;
            }
            if (!IsOpen) return;
            var k = Keyboard.current;
            var g = Gamepad.current;
            if ((k != null && (k.dKey.wasPressedThisFrame || k.rightArrowKey.wasPressedThisFrame)) || (g != null && g.dpad.right.wasPressedThisFrame)) Vira(1);
            if ((k != null && (k.aKey.wasPressedThisFrame || k.leftArrowKey.wasPressedThisFrame)) || (g != null && g.dpad.left.wasPressedThisFrame)) Vira(-1);
        }

        static bool PodeAbrir() =>
            !DialogueBox.IsBusy && !LetterLockUI.IsOpen && !UltimaLinha.Salas.Modal.IsOpen && Time.timeScale > 0f && !IntroCutscene.Active;

        void Abre()
        {
            if (paginas.Count == 0 || !group) return;
            IsOpen = true;
            escalaAntes = Time.timeScale;
            Time.timeScale = 0f;
            group.alpha = 1f;
            group.blocksRaycasts = true;
            Mostra();
            if (sfx && folhaClip) sfx.PlayOneShot(folhaClip);
        }

        void Fecha()
        {
            IsOpen = false;
            Time.timeScale = escalaAntes;
            group.alpha = 0f;
            group.blocksRaycasts = false;
        }

        void Vira(int d)
        {
            int novo = Mathf.Clamp(atual + d, 0, paginas.Count - 1);
            if (novo == atual) return;
            atual = novo;
            Mostra();
            if (sfx && folhaClip) sfx.PlayOneShot(folhaClip, 0.6f);
        }

        void Mostra()
        {
            var p = paginas[atual];
            titulo.text = p.titulo;
            texto.text = p.texto;
            numero.text = $"{atual + 1} / {paginas.Count}";
        }
    }
}
