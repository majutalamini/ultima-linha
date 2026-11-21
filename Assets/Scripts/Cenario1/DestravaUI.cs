using System;
using System.Collections;
using TMPro;
using UltimaLinha.Salas;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Minijogo da alavanca emperrada: o ponteiro do mostrador vai e volta; E com ele na faixa verde é um tranco que
    /// solta um pouco. Três acertos seguidos soltam a alavanca; a cada acerto o ponteiro acelera; errar volta um passo.
    /// Esc sai sem perder nada. Embaixo do mostrador há um botão PUXAR para clicar (faz o mesmo que o E) que acende
    /// verde enquanto o ponteiro passa pela faixa certa.
    /// </summary>
    public class DestravaUI : MonoBehaviour
    {
        public static DestravaUI Instance { get; private set; }

        public CanvasGroup group;
        public RectTransform painel;
        public RectTransform ponteiro;
        public Image[] passos;
        public TMP_Text status;
        [Tooltip("Botão PUXAR. Vazio: é criado embaixo do texto quando a cena abre.")]
        public Button puxarBotao;

        [Header("Mostrador (graus, 0 = direita, anti-horário)")]
        public float de = 172f, ate = 6f;
        public float faixaDe = 12f, faixaAte = 60f;
        [Tooltip("Ângulo para onde o desenho do ponteiro aponta sem rotação.")]
        public float anguloDoDesenho = 52f;
        public float[] velocidades = { 120f, 165f, 215f };
        public int acertosPrecisos = 3;

        public Color passoApagado = new Color(0.3f, 0.27f, 0.24f, 1f);
        public Color passoAceso = new Color(0.55f, 1f, 0.6f, 1f);

        [Header("Som")]
        public AudioSource sfx;
        public AudioClip acertoClip, erroClip, soltaClip;

        public bool IsOpen { get; private set; }
        /// <summary>Errou o tranco (o diretor conta para dar dica).</summary>
        public event Action Errou;

        Action soltou;
        float angulo, sentido = -1f, abriuEm;
        int acertos;
        bool ocupado;
        Vector2 painel0;
        Image puxarFundo;
        Outline puxarBorda;
        TMP_Text puxarTexto;
        static readonly Color PuxarApagado = new Color(0.16f, 0.12f, 0.1f, 1f);
        static readonly Color PuxarVerde = new Color(0.13f, 0.34f, 0.16f, 1f);
        static readonly Color Creme = new Color(0.91f, 0.86f, 0.77f, 1f);

        void Awake()
        {
            Instance = this;
            if (painel) painel0 = painel.anchoredPosition;
            if (group)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
            if (!puxarBotao) puxarBotao = CriaPuxar();
            if (puxarBotao)
            {
                puxarFundo = puxarBotao.targetGraphic as Image;
                puxarBorda = puxarBotao.GetComponent<Outline>();
                puxarTexto = puxarBotao.GetComponentInChildren<TMP_Text>();
                puxarBotao.onClick.AddListener(Puxa);
            }
        }

        /// <summary>Clique no PUXAR: o mesmo tranco do E.</summary>
        void Puxa()
        {
            if (!IsOpen || ocupado || Time.unscaledTime - abriuEm < 0.25f) return;
            StartCoroutine(Tranco(NaFaixa(angulo)));
        }

        /// <summary>Botão PUXAR logo embaixo do texto de ajuda, centralizado com ele.</summary>
        Button CriaPuxar()
        {
            if (!status) return null;
            var sRt = status.rectTransform;
            var rt = new GameObject("Puxar", typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<RectTransform>();
            rt.gameObject.layer = gameObject.layer;
            rt.SetParent(sRt.parent, false);
            rt.anchorMin = sRt.anchorMin;
            rt.anchorMax = sRt.anchorMin; // tamanho fixo, mesmo se o texto estica na largura
            rt.pivot = new Vector2(0.5f, 1f);
            var r = sRt.rect;
            // embaixo do retângulo do texto, no meio dele
            Vector2 baseTexto = sRt.anchoredPosition + new Vector2(r.width * (0.5f - sRt.pivot.x), -r.height * sRt.pivot.y);
            if (sRt.anchorMin != sRt.anchorMax)
            {
                // texto esticado: calcula pelo centro da área dele
                var pai = (RectTransform)sRt.parent;
                rt.anchorMin = rt.anchorMax = new Vector2(0.5f, 0.5f);
                baseTexto = (Vector2)pai.InverseTransformPoint(sRt.TransformPoint(new Vector3(r.center.x, r.yMin, 0f)));
            }
            rt.anchoredPosition = baseTexto + new Vector2(0f, -14f);
            rt.sizeDelta = new Vector2(300f, 66f);

            var fundo = rt.GetComponent<Image>();
            fundo.color = PuxarApagado;
            var borda = rt.gameObject.AddComponent<Outline>();
            borda.effectColor = new Color(Creme.r, Creme.g, Creme.b, 0.8f);
            borda.effectDistance = new Vector2(2f, -2f);
            var botao = rt.GetComponent<Button>();
            botao.targetGraphic = fundo;
            botao.transition = Selectable.Transition.None; // a cor quem manda é o ponteiro (verde quando é a hora)
            botao.navigation = new Navigation { mode = Navigation.Mode.None };

            var texto = new GameObject("Texto", typeof(RectTransform)).AddComponent<TextMeshProUGUI>();
            texto.gameObject.layer = gameObject.layer;
            texto.rectTransform.SetParent(rt, false);
            texto.rectTransform.anchorMin = Vector2.zero;
            texto.rectTransform.anchorMax = Vector2.one;
            texto.rectTransform.offsetMin = texto.rectTransform.offsetMax = Vector2.zero;
            texto.font = status.font;
            texto.text = "PUXAR  <size=65%><alpha=#B0>(E)</size>";
            texto.fontSize = 32;
            texto.characterSpacing = 14;
            texto.color = Creme;
            texto.alignment = TextAlignmentOptions.Center;
            texto.raycastTarget = false;
            return botao;
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
            if (IsOpen) Modal.Pop();
        }

        public void Open(Action aoSoltar)
        {
            if (IsOpen) return;
            soltou = aoSoltar;
            IsOpen = true;
            abriuEm = Time.unscaledTime;
            acertos = 0;
            angulo = de;
            sentido = -1f;
            Passos();
            if (status) status.text = "PUXAR (ou E) quando o ponteiro estiver no verde   ·   ESC  sair";
            Modal.Push();
            Modal.SetControl(false);
            if (group)
            {
                group.alpha = 1f;
                group.blocksRaycasts = true;
            }
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            if (group)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
            Modal.Pop();
            if (!Modal.IsOpen && !DialogueBox.IsBusy) Modal.SetControl(true);
        }

        void Update()
        {
            if (!IsOpen || ocupado) return;
            // o ponteiro vai e volta entre os extremos da escala
            angulo += sentido * velocidades[Mathf.Min(acertos, velocidades.Length - 1)] * Time.unscaledDeltaTime;
            if (angulo <= ate) { angulo = ate; sentido = 1f; }
            if (angulo >= de) { angulo = de; sentido = -1f; }
            Ponteiro(angulo);
            BotaoNaHora(NaFaixa(angulo));

            if (Time.unscaledTime - abriuEm < 0.25f) return; // o E que abriu não conta
            var k = Keyboard.current;
            if ((k != null && k.escapeKey.wasPressedThisFrame) || InputHelper.BackPressed()) { Close(); return; }
            if (InputHelper.InteractPressed()) StartCoroutine(Tranco(NaFaixa(angulo)));
        }

        public bool NaFaixa(float a) => a >= faixaDe && a <= faixaAte;

        /// <summary>O botão PUXAR acende verde enquanto o ponteiro está na faixa certa.</summary>
        void BotaoNaHora(bool agora)
        {
            if (puxarFundo) puxarFundo.color = agora ? PuxarVerde : PuxarApagado;
            if (puxarBorda) puxarBorda.effectColor = agora ? new Color(0.55f, 1f, 0.6f, 1f) : new Color(Creme.r, Creme.g, Creme.b, 0.8f);
            if (puxarTexto) puxarTexto.color = agora ? new Color(0.85f, 1f, 0.86f, 1f) : Creme;
        }

        IEnumerator Tranco(bool acertou)
        {
            ocupado = true;
            if (acertou)
            {
                acertos++;
                Passos();
                if (acertos >= acertosPrecisos)
                {
                    if (sfx && soltaClip) sfx.PlayOneShot(soltaClip);
                    if (status) status.text = "Soltou.";
                    yield return Treme(0.35f, 14f);
                    Close();
                    soltou?.Invoke();
                    ocupado = false;
                    yield break;
                }
                if (sfx && acertoClip) sfx.PlayOneShot(acertoClip);
                yield return Treme(0.15f, 6f);
            }
            else
            {
                acertos = Mathf.Max(0, acertos - 1);
                Passos();
                if (sfx && erroClip) sfx.PlayOneShot(erroClip);
                Errou?.Invoke();
                yield return Treme(0.3f, 10f);
            }
            ocupado = false;
        }

        IEnumerator Treme(float tempo, float forca)
        {
            float t = 0f;
            while (t < tempo)
            {
                t += Time.unscaledDeltaTime;
                if (painel) painel.anchoredPosition = painel0 + UnityEngine.Random.insideUnitCircle * forca * (1f - t / tempo);
                yield return null;
            }
            if (painel) painel.anchoredPosition = painel0;
        }

        void Ponteiro(float a)
        {
            if (ponteiro) ponteiro.localRotation = Quaternion.Euler(0f, 0f, a - anguloDoDesenho);
        }

        void Passos()
        {
            if (passos == null) return;
            for (int i = 0; i < passos.Length; i++)
                if (passos[i]) passos[i].color = i < acertos ? passoAceso : passoApagado;
        }

#if UNITY_EDITOR
        // autoteste: a faixa verde fica dentro do curso do ponteiro
        void OnValidate()
        {
            Debug.Assert(faixaDe > ate && faixaAte < de && faixaDe < faixaAte, "[DestravaUI] faixa verde fora do curso do ponteiro");
        }
#endif
    }
}
