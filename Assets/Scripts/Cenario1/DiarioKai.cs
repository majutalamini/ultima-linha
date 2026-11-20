using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UltimaLinha.Cenario1
{
    [Serializable]
    public class PaginaDiario
    {
        public string titulo;
        [TextArea(4, 12)] public string texto;
    }

    /// <summary>
    /// Diário do Kai: Tab (Select no controle) abre e fecha a qualquer momento do jogo; Esc também fecha; A/D ou as setas viram a página.
    /// Pausa o jogo enquanto está aberto. Não abre por cima de diálogo, cutscene ou painel.
    /// O botão FECHAR fica no canto de cima à direita (criado quando a cena abre, se a cena não tiver um).
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
        [Tooltip("Botão FECHAR. Vazio: é criado no canto de cima à direita quando a cena abre.")]
        public Button fecharBotao;

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
            var dica = transform.Find("Dica")?.GetComponent<TMP_Text>();
            if (dica)
            {
                // a dica de baixo era apagada demais: mais clara e maior, com o ESC junto
                dica.text = "A / D   virar a página        ·        TAB  ou  ESC   fechar";
                dica.color = new Color(0.91f, 0.86f, 0.78f, 0.95f);
                dica.fontSize = Mathf.Max(dica.fontSize, 22f);
            }
            if (!fecharBotao) fecharBotao = CriaFechar(dica ? dica.font : null);
            if (fecharBotao) fecharBotao.onClick.AddListener(() => { if (IsOpen) Fecha(); });
        }

        /// <summary>Botão FECHAR bem visível, no canto de cima à direita da tela (fora do caderno).</summary>
        Button CriaFechar(TMP_FontAsset fonte)
        {
            var creme = new Color(0.91f, 0.86f, 0.77f, 1f);
            var rt = new GameObject("Fechar", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
            rt.gameObject.layer = gameObject.layer;
            rt.SetParent(transform, false);
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 1f);
            rt.anchoredPosition = new Vector2(-36f, -26f);
            rt.sizeDelta = new Vector2(250f, 62f);
            var fundo = rt.GetComponent<Image>();
            fundo.color = new Color(0.3f, 0.07f, 0.06f, 1f);
            var borda = rt.gameObject.AddComponent<Outline>();
            borda.effectColor = new Color(creme.r, creme.g, creme.b, 0.85f);
            borda.effectDistance = new Vector2(2f, -2f);
            var botao = rt.GetComponent<Button>();
            botao.targetGraphic = fundo;
            var cores = botao.colors;
            cores.normalColor = new Color(0.85f, 0.85f, 0.85f, 1f);
            cores.highlightedColor = Color.white;
            cores.pressedColor = new Color(0.6f, 0.5f, 0.45f, 1f);
            cores.fadeDuration = 0.05f;
            botao.colors = cores;
            botao.navigation = new Navigation { mode = Navigation.Mode.None };

            var texto = new GameObject("Texto", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            texto.gameObject.layer = gameObject.layer;
            texto.rectTransform.SetParent(rt, false);
            texto.rectTransform.anchorMin = Vector2.zero;
            texto.rectTransform.anchorMax = Vector2.one;
            texto.rectTransform.offsetMin = texto.rectTransform.offsetMax = Vector2.zero;
            if (fonte) texto.font = fonte;
            texto.text = "FECHAR  <size=65%><alpha=#B0>TAB / ESC</size>";
            texto.fontSize = 28;
            texto.characterSpacing = 10;
            texto.color = creme;
            texto.alignment = TextAlignmentOptions.Center;
            texto.raycastTarget = false;
            return botao;
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
            if (InputHelper.BackPressed())
            {
                InputHelper.ConsumeBack(); // o mesmo Esc não leva ao menu
                Fecha();
                return;
            }
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
