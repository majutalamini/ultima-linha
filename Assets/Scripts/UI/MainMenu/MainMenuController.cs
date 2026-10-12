using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UltimaLinha.UI
{
    /// <summary>
    /// Menu inicial do "Última Linha": Começar, Continuar, Configurações e Sair.
    /// - Começar: jogo novo (apaga o salvamento e começa pelo diário).
    /// - Continuar: volta ao último cenário salvo, sem repetir a cutscene. Fica apagado se não houver jogo salvo.
    /// - Configurações: volume geral, música e tela cheia.
    /// Funciona com mouse, teclado (setas/WASD + Enter) e controle.
    /// A cena é montada pelo MainMenuSceneBuilder (menu "Última Linha" no Editor).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Cena do jogo")]
        [Tooltip("Nome da cena carregada ao clicar em Começar (precisa estar na lista de cenas do build).")]
        [SerializeField] string gameSceneName = "Cenario1";

        [Header("Botões")]
        [Tooltip("Botão selecionado ao abrir (teclado/controle) quando não há jogo salvo.")]
        [SerializeField] Selectable firstSelected;
        [SerializeField] Button continueButton;
        [SerializeField] Button settingsButton;
        [Tooltip("Grupo com os botões principais (fica inativo enquanto as configurações estão abertas).")]
        [SerializeField] CanvasGroup menuGroup;
        [SerializeField] SettingsPanel settings;

        [Header("Transição")]
        [SerializeField] CanvasGroup fader;
        [SerializeField] float fadeDuration = 0.8f;

        [Header("Música")]
        [Tooltip("Música de fundo do menu. Entra e sai junto com o fade da tela.")]
        [SerializeField] AudioSource music;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.6f;

        bool busy;
        float darkness = 1f; // 1 = tela preta (música muda), 0 = tela visível

        float MusicTarget => musicVolume * GameSettings.MusicVolume * (1f - darkness);

        void Awake()
        {
            // Se voltarmos ao menu a partir de um jogo pausado, garante que o tempo corre.
            Time.timeScale = 1f;
            GameSettings.Changed += ApplyMusicVolume;
            if (settings) settings.Closed += OnSettingsClosed;
        }

        void OnDestroy()
        {
            GameSettings.Changed -= ApplyMusicVolume;
            if (settings) settings.Closed -= OnSettingsClosed;
        }

        IEnumerator Start()
        {
            bool canContinue = GameSave.HasSave && Application.CanStreamedLevelBeLoaded(GameSave.SavedScene);
            if (continueButton) continueButton.interactable = canContinue;
            Select(canContinue && continueButton ? continueButton.gameObject : firstSelected ? firstSelected.gameObject : null);

            if (music)
            {
                music.loop = true;
                darkness = fader ? 1f : 0f;
                ApplyMusicVolume();
                music.Play();
            }

            if (fader)
            {
                fader.alpha = 1f;
                fader.blocksRaycasts = true;
                yield return Fade(1f, 0f, fadeDuration * 1.5f);
                fader.blocksRaycasts = false;
            }
        }

        void Update()
        {
            if (busy || (settings && settings.IsOpen)) return;

            // Se o mouse clicou no vazio e tirou a seleção, o teclado/controle traz ela de volta.
            var es = EventSystem.current;
            if (es && es.currentSelectedGameObject == null && NavigationPressed() && firstSelected)
                Select(continueButton && continueButton.interactable ? continueButton.gameObject : firstSelected.gameObject);
        }

        // ---------- Botões (ligados no Inspector) ----------

        /// <summary>Começar: jogo novo.</summary>
        public void Play()
        {
            if (busy) return;
            if (!CanLoad(gameSceneName)) return;
            GameSave.Clear();
            GameSave.ContinueRequested = false;
            StartCoroutine(LoadRoutine(gameSceneName));
        }

        /// <summary>Continuar: volta ao cenário salvo.</summary>
        public void Continue()
        {
            if (busy || !GameSave.HasSave) return;
            string scene = GameSave.SavedScene;
            if (!CanLoad(scene)) return;
            GameSave.ContinueRequested = true;
            StartCoroutine(LoadRoutine(scene));
        }

        public void OpenSettings()
        {
            if (busy || !settings) return;
            if (menuGroup) menuGroup.interactable = false;
            settings.Open();
        }

        public void Quit()
        {
            if (busy) return;
            StartCoroutine(QuitRoutine());
        }

        // ---------- Internos ----------

        void OnSettingsClosed()
        {
            if (menuGroup) menuGroup.interactable = true;
            Select(settingsButton ? settingsButton.gameObject : firstSelected ? firstSelected.gameObject : null);
        }

        static bool CanLoad(string scene)
        {
            if (Application.CanStreamedLevelBeLoaded(scene)) return true;
            Debug.LogError($"[Menu] A cena \"{scene}\" não está na lista de cenas do build. " +
                           "Adicione-a em File > Build Profiles (Scene List) ou troque o nome no MainMenuController.");
            return false;
        }

        IEnumerator LoadRoutine(string scene)
        {
            busy = true;
            if (fader) fader.blocksRaycasts = true;

            var op = SceneManager.LoadSceneAsync(scene);
            op.allowSceneActivation = false;

            yield return Fade(0f, 1f, fadeDuration);
            while (op.progress < 0.9f) yield return null;
            op.allowSceneActivation = true;
        }

        IEnumerator QuitRoutine()
        {
            busy = true;
            if (fader) fader.blocksRaycasts = true;
            yield return Fade(0f, 1f, fadeDuration);
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        IEnumerator Fade(float from, float to, float duration)
        {
            if (!fader) yield break;
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                fader.alpha = Mathf.Lerp(from, to, Mathf.SmoothStep(0f, 1f, t / duration));
                darkness = fader.alpha; // tela escura = música baixa
                ApplyMusicVolume();
                yield return null;
            }
            fader.alpha = to;
            darkness = to;
            ApplyMusicVolume();
        }

        void ApplyMusicVolume()
        {
            if (music) music.volume = MusicTarget;
        }

        static void Select(GameObject go)
        {
            var es = EventSystem.current;
            if (!es) return;
            es.SetSelectedGameObject(null); // força OnSelect mesmo que seja o mesmo objeto
            if (go) es.SetSelectedGameObject(go);
        }

        static bool NavigationPressed()
        {
            var k = Keyboard.current;
            var g = Gamepad.current;
            bool key = k != null && (k.upArrowKey.wasPressedThisFrame || k.downArrowKey.wasPressedThisFrame
                                     || k.wKey.wasPressedThisFrame || k.sKey.wasPressedThisFrame
                                     || k.tabKey.wasPressedThisFrame);
            bool pad = g != null && (g.dpad.up.wasPressedThisFrame || g.dpad.down.wasPressedThisFrame
                                     || g.leftStick.ReadValue().sqrMagnitude > 0.25f);
            return key || pad;
        }
    }
}
