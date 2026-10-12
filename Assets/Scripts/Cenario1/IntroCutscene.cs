using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UltimaLinha.Cenario1
{
    [Serializable]
    public class CutscenePage
    {
        [TextArea(2, 5)] public string text;
        [Tooltip("Cabeçalho do dia (ex.: a data): escrito um pouco maior.")]
        public bool isTitle;
    }

    /// <summary>
    /// Cutscene de abertura: o diário do Kai, aberto sobre o escuro. Cada trecho é escrito à mão,
    /// letra por letra, com som de caneta; quando a página enche, passa para a da direita e depois
    /// vira a folha. E / Espaço / Enter / clique: termina de escrever o trecho ou passa para o próximo.
    /// Segurar ESC pula tudo. No último trecho a letra fica mais lenta, a caneta escorrega
    /// (ele dorme), o diário some e o último trem chega no escuro.
    /// </summary>
    public class IntroCutscene : MonoBehaviour
    {
        [Header("UI")]
        public CanvasGroup group;
        [Tooltip("O diário inteiro (imagem + textos); usado para aparecer, sumir e virar a folha.")]
        public CanvasGroup diary;
        [Tooltip("Página da esquerda e da direita.")]
        public TMP_Text[] sides;
        public TMP_Text continueHint;
        public CanvasGroup skipGroup;
        public Image skipFill;
        [Tooltip("Risco da caneta escorregando no fim (Image do tipo Filled).")]
        public Image penSlip;

        [Header("Escrita")]
        public float charsPerSecond = 17f;
        [Tooltip("Quantas letras ficam 'secando' ao mesmo tempo na ponta da caneta.")]
        public float inkFade = 2.5f;
        public float punctuationPause = 0.32f;
        public float minTime = 0.8f;
        [Tooltip("Tamanho do cabeçalho (data), relativo ao texto.")]
        public int titleSizePercent = 122;
        [Tooltip("Espaço entre um trecho e outro, relativo ao texto.")]
        public int gapSizePercent = 45;
        public float skipHoldTime = 1.2f;

        [Header("Som")]
        public AudioSource drone;
        public float droneVolume = 0.45f;
        public AudioSource penLoop;
        public float penVolume = 0.55f;
        public AudioSource sfx;
        public AudioClip pageClip;
        public AudioClip trainClip;
        [Tooltip("Segundos de som do trem no escuro antes do corte para o vagão.")]
        public float trainSeconds = 6.5f;

        [Header("Texto")]
        public List<CutscenePage> pages = new List<CutscenePage>();

        public bool IsPlaying { get; private set; }
        /// <summary>Verdadeiro enquanto o diário está na tela (o resto da cena fica em silêncio).</summary>
        public static bool Active { get; private set; }

        bool skipped;
        float skipHeld;
        readonly string[] content = new string[2];

        void OnDestroy()
        {
            Active = false;
        }

        void Awake()
        {
            if (group)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
        }

        public IEnumerator Play()
        {
            if (pages == null || pages.Count == 0 || sides == null || sides.Length == 0) yield break;
            IsPlaying = true;
            Active = true;
            skipped = false;
            skipHeld = 0f;
            if (group) group.alpha = 1f;
            if (diary) diary.alpha = 0f;
            ClearPages();
            if (continueHint) continueHint.alpha = 0f;
            if (skipGroup) skipGroup.alpha = 0f;
            if (penSlip) penSlip.fillAmount = 0f;
            if (penLoop)
            {
                penLoop.loop = true;
                penLoop.volume = 0f;
                penLoop.Play();
            }
            if (drone)
            {
                drone.loop = true;
                drone.volume = 0f;
                drone.Play();
                StartCoroutine(FadeAudio(drone, droneVolume, 3f));
            }

            yield return Wait(0.6f);
            yield return FadeGroup(diary, 0f, 1f, 1.6f);
            yield return Wait(0.4f);

            int side = 0;
            for (int p = 0; p < pages.Count && !skipped; p++)
            {
                var page = pages[p];
                bool last = p == pages.Count - 1;
                string piece = page.isTitle ? $"<size={titleSizePercent}%>{page.text}</size>" : page.text;
                string sep = string.IsNullOrEmpty(content[side]) ? "" : $"\n<size={gapSizePercent}%> </size>\n";

                // não cabe mais nesta página? passa para a da direita, ou vira a folha
                if (!Fits(side, content[side] + sep + piece))
                {
                    side++;
                    sep = "";
                    if (side >= sides.Length)
                    {
                        yield return TurnPage();
                        side = 0;
                    }
                }

                string before = content[side] + sep;
                content[side] = before + piece;
                yield return Write(sides[side], before, content[side], last);
                if (skipped) break;

                if (last) break; // o último trecho segue sozinho: ele adormece
                yield return WaitForAdvance();
            }

            if (penLoop) penLoop.volume = 0f;
            if (continueHint) continueHint.alpha = 0f;

            if (!skipped)
            {
                yield return Wait(0.9f);
                yield return PenSlip();
                yield return Wait(1.3f);
            }

            // escuridão: o diário some, o drone some e o último trem chega
            if (drone) StartCoroutine(FadeAudio(drone, 0f, 2.5f));
            if (skipGroup) skipGroup.alpha = 0f;
            float fade = skipped ? 0.6f : 2.6f;
            skipped = false; // deixa o fade final acontecer
            yield return FadeGroup(diary, diary ? diary.alpha : 1f, 0f, fade);
            if (sfx && trainClip) sfx.PlayOneShot(trainClip);
            yield return new WaitForSecondsRealtime(trainSeconds);
            if (drone) drone.Stop();
            if (penLoop) penLoop.Stop();

            if (group) group.alpha = 0f; // o fade preto do HUD continua cobrindo a tela
            IsPlaying = false;
            Active = false;
        }

        // ---------- Escrita à mão ----------

        IEnumerator Write(TMP_Text text, string before, string full, bool sleepy)
        {
            text.text = before;
            text.ForceMeshUpdate();
            int start = text.textInfo.characterCount;
            text.text = full;
            text.ForceMeshUpdate();
            int total = text.textInfo.characterCount;
            ApplyInk(text, start, start);

            float progress = start;
            float pause = 0f;
            int lastChar = start - 1;
            bool canSkip = false;
            float elapsed = 0f;
            while (progress < total + inkFade && !skipped)
            {
                float dt = Time.unscaledDeltaTime;
                elapsed += dt;
                if (elapsed > 0.15f) canSkip = true;
                if (canSkip && InputHelper.AdvancePressed())
                {
                    progress = total + inkFade;
                    break;
                }

                if (pause > 0f)
                {
                    pause -= dt;
                    SetPen(false);
                }
                else
                {
                    float k = (progress - start) / Mathf.Max(1, total - start);
                    float speed = charsPerSecond * (sleepy ? Mathf.Lerp(1f, 0.38f, k) : 1f);
                    speed *= 0.85f + 0.3f * Mathf.PerlinNoise(Time.unscaledTime * 1.7f, 0.3f);
                    progress += dt * speed;
                    SetPen(progress < total);

                    int now = Mathf.FloorToInt(progress);
                    while (lastChar < now - 1 && lastChar < total - 1)
                    {
                        lastChar++;
                        char c = text.textInfo.characterInfo[lastChar].character;
                        if (c == '.' || c == ',' || c == '!' || c == '?' || c == '”' || c == '\n')
                            pause = punctuationPause * (c == ',' ? 0.6f : 1f) * (sleepy ? 1.8f : 1f);
                        else if (c == ' ')
                            pause = 0.04f;
                    }
                }

                ApplyInk(text, start, progress);
                UpdateSkip();
                yield return null;
            }
            SetPen(false);
            ApplyInk(text, total, total);
        }

        /// <summary>Letras já escritas: tinta cheia. Na ponta da caneta: a tinta vai aparecendo.</summary>
        void ApplyInk(TMP_Text text, int start, float progress)
        {
            var info = text.textInfo;
            for (int i = 0; i < info.characterCount; i++)
            {
                var ch = info.characterInfo[i];
                if (!ch.isVisible) continue;
                float a = i < start ? 1f : Mathf.Clamp01((progress - i) / inkFade);
                byte alpha = (byte)(255 * a);
                var colors = info.meshInfo[ch.materialReferenceIndex].colors32;
                int v = ch.vertexIndex;
                for (int j = 0; j < 4; j++) colors[v + j].a = alpha;
            }
            text.UpdateVertexData(TMP_VertexDataUpdateFlags.Colors32);
        }

        void SetPen(bool writing)
        {
            if (!penLoop) return;
            float target = writing ? penVolume : 0f;
            penLoop.volume = Mathf.MoveTowards(penLoop.volume, target, Time.unscaledDeltaTime * 6f);
        }

        bool Fits(int side, string candidate)
        {
            if (side >= sides.Length) return false;
            var t = sides[side];
            var size = t.rectTransform.rect.size;
            return t.GetPreferredValues(candidate, size.x, 0f).y <= size.y;
        }

        IEnumerator TurnPage()
        {
            if (sfx && pageClip) sfx.PlayOneShot(pageClip);
            yield return FadeGroup(diary, 1f, 0f, 0.55f);
            ClearPages();
            yield return Wait(0.25f);
            yield return FadeGroup(diary, 0f, 1f, 0.6f);
        }

        /// <summary>O Kai cochila: a caneta escorrega e risca a página a partir da última letra.</summary>
        IEnumerator PenSlip()
        {
            if (!penSlip) yield break;
            TMP_Text text = null;
            for (int i = sides.Length - 1; i >= 0; i--)
                if (!string.IsNullOrEmpty(content[i])) { text = sides[i]; break; }
            if (text)
            {
                text.ForceMeshUpdate();
                var info = text.textInfo;
                for (int i = info.characterCount - 1; i >= 0; i--)
                {
                    if (!info.characterInfo[i].isVisible) continue;
                    var rt = penSlip.rectTransform;
                    rt.SetParent(text.rectTransform, false);
                    rt.anchorMin = rt.anchorMax = text.rectTransform.pivot;
                    rt.anchoredPosition = info.characterInfo[i].bottomRight + new Vector3(4f, 6f, 0f);
                    break;
                }
            }
            if (penLoop) penLoop.volume = penVolume * 0.8f;
            float t = 0f;
            while (t < 0.7f)
            {
                t += Time.unscaledDeltaTime;
                penSlip.fillAmount = Mathf.SmoothStep(0f, 1f, t / 0.7f);
                yield return null;
            }
            if (penLoop) penLoop.volume = 0f;
        }

        IEnumerator WaitForAdvance()
        {
            float shown = 0f;
            bool hint = false;
            while (!skipped)
            {
                shown += Time.unscaledDeltaTime;
                if (!hint && shown >= minTime && continueHint)
                {
                    hint = true;
                    StartCoroutine(FadeText(continueHint, 0f, 0.6f, 0.5f));
                }
                if (shown >= minTime && InputHelper.AdvancePressed()) break;
                UpdateSkip();
                yield return null;
            }
            if (continueHint) continueHint.alpha = 0f;
        }

        void ClearPages()
        {
            for (int i = 0; i < content.Length; i++) content[i] = "";
            if (sides == null) return;
            foreach (var s in sides)
                if (s) s.text = "";
        }

        // ---------- Pular / esperar / fades ----------

        /// <summary>Segurar ESC (ou Start) por um instante pula a cutscene.</summary>
        void UpdateSkip()
        {
            var k = Keyboard.current;
            var g = Gamepad.current;
            bool holding = (k != null && k.escapeKey.isPressed) || (g != null && g.startButton.isPressed);
            skipHeld = holding ? skipHeld + Time.unscaledDeltaTime : Mathf.Max(0f, skipHeld - Time.unscaledDeltaTime * 2f);
            if (skipGroup) skipGroup.alpha = Mathf.MoveTowards(skipGroup.alpha, skipHeld > 0.05f ? 1f : 0f, Time.unscaledDeltaTime * 4f);
            if (skipFill) skipFill.fillAmount = Mathf.Clamp01(skipHeld / skipHoldTime);
            if (skipHeld >= skipHoldTime) skipped = true;
        }

        IEnumerator Wait(float seconds)
        {
            float t = 0f;
            while (t < seconds && !skipped)
            {
                t += Time.unscaledDeltaTime;
                UpdateSkip();
                yield return null;
            }
        }

        IEnumerator FadeGroup(CanvasGroup g, float from, float to, float duration)
        {
            if (!g) yield break;
            float t = 0f;
            while (t < duration && !skipped)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                UpdateSkip();
                yield return null;
            }
            g.alpha = to;
        }

        static IEnumerator FadeText(TMP_Text text, float from, float to, float duration)
        {
            if (!text) yield break;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                text.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            text.alpha = to;
        }

        static IEnumerator FadeAudio(AudioSource src, float to, float duration)
        {
            float from = src.volume, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                src.volume = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            src.volume = to;
        }
    }
}
