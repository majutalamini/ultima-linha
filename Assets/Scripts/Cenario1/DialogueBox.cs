using System;
using System.Collections;
using System.Collections.Generic;
using Platformer.Core;
using Platformer.Model;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace UltimaLinha.Cenario1
{
    [Serializable]
    public class DialogueLine
    {
        [Tooltip("Id de quem fala: kai, souta ou ren.")]
        public string speaker;
        [TextArea(2, 4)] public string text;

        public DialogueLine() { }

        public DialogueLine(string speaker, string text)
        {
            this.speaker = speaker;
            this.text = text;
        }
    }

    [Serializable]
    public class Speaker
    {
        public string id;
        public string displayName;
        public Sprite portrait;
        public Color nameColor = Color.white;
    }

    /// <summary>
    /// Caixa de diálogo estilo visual novel: retrato, nome e texto aparecendo letra a letra.
    /// Enquanto está aberta, o Kai não se move. E / Espaço / Enter / clique avançam.
    /// </summary>
    public class DialogueBox : MonoBehaviour
    {
        public static DialogueBox Instance { get; private set; }
        public static bool IsBusy => Instance != null && Instance.open;

        [Header("UI")]
        public CanvasGroup group;
        public Image portrait;
        public GameObject portraitFrame;
        public TMP_Text nameText;
        public TMP_Text bodyText;
        public TMP_Text continueHint;

        [Header("Som das letras")]
        public AudioSource voice;
        public AudioClip blip;

        [Header("Ritmo")]
        public float charsPerSecond = 40f;
        public float fadeTime = 0.18f;

        [Header("Quem fala")]
        public List<Speaker> speakers = new List<Speaker>();

        bool open;
        Coroutine routine;

        void Awake()
        {
            Instance = this;
            if (group)
            {
                group.alpha = 0f;
                group.blocksRaycasts = false;
            }
        }

        void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void Play(IList<DialogueLine> lines, Action onDone = null)
        {
            if (lines == null || lines.Count == 0)
            {
                onDone?.Invoke();
                return;
            }
            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Run(lines, onDone));
        }

        IEnumerator Run(IList<DialogueLine> lines, Action onDone)
        {
            open = true;
            SetPlayerControl(false);
            yield return Fade(1f);

            foreach (var line in lines)
            {
                ShowSpeaker(line.speaker);
                bodyText.text = line.text;
                bodyText.maxVisibleCharacters = 0;
                bodyText.ForceMeshUpdate();
                int total = bodyText.textInfo.characterCount;
                if (continueHint) continueHint.enabled = false;

                yield return null; // não reaproveita a mesma tecla que abriu a conversa

                float shown = 0f;
                int lastBlip = 0;
                while (shown < total)
                {
                    if (InputHelper.AdvancePressed()) break;
                    shown += Time.deltaTime * charsPerSecond;
                    int visible = Mathf.Min(total, Mathf.FloorToInt(shown));
                    if (visible != bodyText.maxVisibleCharacters)
                    {
                        bodyText.maxVisibleCharacters = visible;
                        if (voice && blip && visible - lastBlip >= 2)
                        {
                            voice.pitch = UnityEngine.Random.Range(0.92f, 1.08f);
                            voice.PlayOneShot(blip);
                            lastBlip = visible;
                        }
                    }
                    yield return null;
                }

                bodyText.maxVisibleCharacters = total;
                if (continueHint) continueHint.enabled = true;
                yield return null;
                while (!InputHelper.AdvancePressed()) yield return null;
            }

            yield return Fade(0f);
            open = false;
            routine = null;
            yield return null; // a tecla que fechou o diálogo não vira pulo
            SetPlayerControl(true);
            onDone?.Invoke();
        }

        void ShowSpeaker(string id)
        {
            var s = speakers.Find(x => x.id == id);
            if (nameText)
            {
                nameText.text = s != null ? s.displayName.ToUpperInvariant() : "";
                if (s != null) nameText.color = s.nameColor;
            }
            bool hasPortrait = s != null && s.portrait;
            if (portraitFrame) portraitFrame.SetActive(hasPortrait);
            if (portrait && hasPortrait) portrait.sprite = s.portrait;
        }

        IEnumerator Fade(float to)
        {
            if (!group) yield break;
            float from = group.alpha;
            float t = 0f;
            while (t < fadeTime)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / fadeTime);
                yield return null;
            }
            group.alpha = to;
        }

        static void SetPlayerControl(bool enabled)
        {
            var player = Simulation.GetModel<PlatformerModel>().player;
            if (player) player.controlEnabled = enabled;
        }
    }
}
