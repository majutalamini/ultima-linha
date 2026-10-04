using System.Collections;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace UltimaLinha.UI
{
    /// <summary>
    /// Menu inicial do "Última Linha": só Jogar e Sair.
    /// Funciona com mouse, teclado (setas/WASD + Enter) e controle.
    /// A cena é montada automaticamente pelo MainMenuSceneBuilder (menu "Última Linha" no Editor).
    /// </summary>
    public class MainMenuController : MonoBehaviour
    {
        [Header("Cena do jogo")]
        [Tooltip("Nome da cena carregada ao clicar em Jogar (precisa estar na lista de cenas do build).")]
        [SerializeField] string gameSceneName = "Cenario1";

        [Header("Botão selecionado ao abrir (teclado/controle)")]
        [SerializeField] Selectable firstSelected;

        [Header("Transição")]
        [SerializeField] CanvasGroup fader;
        [SerializeField] float fadeDuration = 0.8f;

        [Header("Música")]
        [Tooltip("Música de fundo do menu. Entra e sai junto com o fade da tela.")]
        [SerializeField] AudioSource music;
        [SerializeField, Range(0f, 1f)] float musicVolume = 0.6f;

        bool busy;

        void Awake()
        {
            // Se voltarmos ao menu a partir de um jogo pausado, garante que o tempo corre.
            Time.timeScale = 1f;
        }

        IEnumerator Start()
        {
            Select(firstSelected ? firstSelected.gameObject : null);

            if (music)
            {
                music.loop = true;
                music.volume = fader ? 0f : musicVolume;
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
            if (busy) return;

            // Se o mouse clicou no vazio e tirou a seleção, o teclado/controle traz ela de volta.
            var es = EventSystem.current;
            if (es && es.currentSelectedGameObject == null && NavigationPressed() && firstSelected)
                Select(firstSelected.gameObject);
        }

        // ---------- Botões (ligados no Inspector) ----------

        public void Play()
        {
            if (busy) return;
            if (!Application.CanStreamedLevelBeLoaded(gameSceneName))
            {
                Debug.LogError($"[Menu] A cena \"{gameSceneName}\" não está na lista de cenas do build. " +
                               "Adicione-a em File > Build Profiles (Scene List) ou troque o nome no MainMenuController.");
                return;
            }
            StartCoroutine(PlayRoutine());
        }

        public void Quit()
        {
            if (busy) return;
            StartCoroutine(QuitRoutine());
        }

        // ---------- Internos ----------

        IEnumerator PlayRoutine()
        {
            busy = true;
            if (fader) fader.blocksRaycasts = true;

            var op = SceneManager.LoadSceneAsync(gameSceneName);
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
                if (music) music.volume = musicVolume * (1f - fader.alpha); // tela escura = música baixa
                yield return null;
            }
            fader.alpha = to;
            if (music) music.volume = musicVolume * (1f - to);
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
