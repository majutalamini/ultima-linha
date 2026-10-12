using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

namespace UltimaLinha.UI
{
    /// <summary>
    /// Painel "Configurações" do menu: volume geral, volume da música e tela cheia.
    /// Setas/controle: cima e baixo escolhem, esquerda e direita mudam o volume.
    /// ESC / Backspace / B do controle (ou "Voltar") fecham.
    /// </summary>
    public class SettingsPanel : MonoBehaviour
    {
        [SerializeField] CanvasGroup group;
        [SerializeField] Slider masterSlider;
        [SerializeField] TMP_Text masterValue;
        [SerializeField] Slider musicSlider;
        [SerializeField] TMP_Text musicValue;
        [SerializeField] Button fullscreenButton;
        [SerializeField] TMP_Text fullscreenValue;
        [SerializeField] Button backButton;
        [SerializeField] float fadeDuration = 0.2f;

        public bool IsOpen { get; private set; }
        public event Action Closed;

        float openedAt;

        void Awake()
        {
            SetVisible(0f, false);
            if (masterSlider) masterSlider.onValueChanged.AddListener(v => { GameSettings.MasterVolume = v; Refresh(); });
            if (musicSlider) musicSlider.onValueChanged.AddListener(v => { GameSettings.MusicVolume = v; Refresh(); });
            if (fullscreenButton) fullscreenButton.onClick.AddListener(() => { GameSettings.Fullscreen = !GameSettings.Fullscreen; Refresh(); });
            if (backButton) backButton.onClick.AddListener(Close);
        }

        public void Open()
        {
            if (IsOpen) return;
            IsOpen = true;
            openedAt = Time.unscaledTime;
            if (masterSlider) masterSlider.SetValueWithoutNotify(GameSettings.MasterVolume);
            if (musicSlider) musicSlider.SetValueWithoutNotify(GameSettings.MusicVolume);
            Refresh();
            StopAllCoroutines();
            StartCoroutine(Fade(1f));
            if (group)
            {
                group.interactable = true;
                group.blocksRaycasts = true;
            }
            var es = EventSystem.current;
            if (es)
            {
                es.SetSelectedGameObject(null);
                if (masterSlider) es.SetSelectedGameObject(masterSlider.gameObject);
            }
        }

        public void Close()
        {
            if (!IsOpen) return;
            IsOpen = false;
            GameSettings.Save();
            if (group)
            {
                group.interactable = false;
                group.blocksRaycasts = false;
            }
            StopAllCoroutines();
            StartCoroutine(Fade(0f));
            Closed?.Invoke();
        }

        void Update()
        {
            if (!IsOpen || Time.unscaledTime - openedAt < 0.15f) return;
            var k = Keyboard.current;
            var g = Gamepad.current;
            if ((k != null && (k.escapeKey.wasPressedThisFrame || k.backspaceKey.wasPressedThisFrame)) ||
                (g != null && g.buttonEast.wasPressedThisFrame))
                Close();
        }

        void Refresh()
        {
            if (masterValue) masterValue.text = Mathf.RoundToInt(GameSettings.MasterVolume * 100f) + "%";
            if (musicValue) musicValue.text = Mathf.RoundToInt(GameSettings.MusicVolume * 100f) + "%";
            if (fullscreenValue) fullscreenValue.text = GameSettings.Fullscreen ? "Sim" : "Não";
        }

        IEnumerator Fade(float to)
        {
            if (!group) yield break;
            float from = group.alpha, t = 0f;
            while (t < fadeDuration)
            {
                t += Time.unscaledDeltaTime;
                group.alpha = Mathf.Lerp(from, to, t / fadeDuration);
                yield return null;
            }
            group.alpha = to;
        }

        void SetVisible(float alpha, bool interactive)
        {
            if (!group) return;
            group.alpha = alpha;
            group.interactable = interactive;
            group.blocksRaycasts = interactive;
        }
    }
}
