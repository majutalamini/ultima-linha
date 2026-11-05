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
    /// Esc sai sem perder nada.
    /// </summary>
    public class DestravaUI : MonoBehaviour
    {
        public static DestravaUI Instance { get; private set; }

        public CanvasGroup group;
        public RectTransform painel;
        public RectTransform ponteiro;
        public Image[] passos;
        public TMP_Text status;

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

        void Awake()
        {
            Instance = this;
            if (painel) painel0 = painel.anchoredPosition;
            if (group)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
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
            if (status) status.text = "E  quando o ponteiro estiver no verde   ·   ESC  sair";
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

            if (Time.unscaledTime - abriuEm < 0.25f) return; // o E que abriu não conta
            var k = Keyboard.current;
            if ((k != null && k.escapeKey.wasPressedThisFrame) || InputHelper.BackPressed()) { Close(); return; }
            if (InputHelper.InteractPressed()) StartCoroutine(Tranco(NaFaixa(angulo)));
        }

        public bool NaFaixa(float a) => a >= faixaDe && a <= faixaAte;

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
