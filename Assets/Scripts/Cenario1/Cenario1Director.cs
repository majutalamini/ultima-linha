using System.Collections;
using System.Collections.Generic;
using Platformer.Mechanics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Roteiro do Cenário 1: diário → o Kai acorda no vagão parado, encontra Souta e Ren,
    /// resolve as alavancas (1ª trava), acha as letras espalhadas e digita a palavra secreta
    /// na porta (2ª trava) e sai pela porta do fundo.
    /// Também ajusta a gravidade da cena (o template usa a da Unity, leve demais para esta escala)
    /// e devolve o valor original ao sair.
    /// </summary>
    public class Cenario1Director : MonoBehaviour
    {
        public PlayerController player;
        public LeverPuzzle puzzle;
        public ExitDoor door;
        public CameraFollow2D cameraFollow;
        public NpcTalk souta;
        public NpcTalk ren;
        [Tooltip("Segundo puzzle: as letras espalhadas (palavra secreta).")]
        public LetterPuzzle letterPuzzle;

        [Header("Cutscene de abertura (tela preta com texto)")]
        public IntroCutscene introCutscene;
        public bool playIntroCutscene = true;

        [Header("Física desta cena")]
        public Vector2 gravity = new Vector2(0f, -20f);

        [Header("Cenas")]
        public string nextScene = "Cenario2";
        public string menuScene = "MainMenu";

        [Header("Som ambiente")]
        public AudioSource ambience;
        public float ambienceVolume = 0.55f;

        [Header("Objetivos")]
        public string objectiveStart = "Descubra como sair do vagão.";
        public string objectiveLevers = "Puxe as três alavancas na ordem que o letreiro mostra.";
        public string objectiveLetters = "Encontre as letras escritas pelo vagão.";
        public string objectiveWord = "Digite a palavra na trava da porta do fundo.";
        public string objectiveExit = "A porta do fundo abriu.";

        [Header("Falas")]
        public List<DialogueLine> intro = new List<DialogueLine>();
        public List<DialogueLine> doorLocked = new List<DialogueLine>();
        public List<DialogueLine> firstFail = new List<DialogueLine>();
        [Tooltip("Depois das alavancas: as luzes apagam e voltam com as letras.")]
        public List<DialogueLine> lettersAppear = new List<DialogueLine>();
        [Tooltip("Com as alavancas feitas, ao tentar a porta.")]
        public List<DialogueLine> doorNeedsWord = new List<DialogueLine>();
        public List<DialogueLine> allLetters = new List<DialogueLine>();
        [Tooltip("Logo depois de acertar a palavra.")]
        public List<DialogueLine> wordSolved = new List<DialogueLine>();
        public List<DialogueLine> solved = new List<DialogueLine>();

        [Header("Fim do cenário (enquanto o Cenário 2 não existe)")]
        public string endTitle = "FIM DO CENÁRIO 1";
        public string endSubtitle = "O próximo vagão ainda está sendo construído.";

        Vector2 previousGravity;
        bool leaving;
        int fails;
        bool leversObjectiveShown;

        void Awake()
        {
            previousGravity = Physics2D.gravity;
            Physics2D.gravity = gravity;
            Time.timeScale = 1f;
        }

        void OnDestroy()
        {
            Physics2D.gravity = previousGravity;
        }

        IEnumerator Start()
        {
            if (player) player.controlEnabled = false;
            if (cameraFollow) cameraFollow.Snap();

            if (puzzle)
            {
                puzzle.Solved += OnSolved;
                puzzle.Failed += OnFailed;
                puzzle.Progressed += _ => ShowLeversObjective();
            }
            if (letterPuzzle)
            {
                letterPuzzle.Revealed += OnLettersRevealed;
                letterPuzzle.Collected += OnLetterCollected;
                letterPuzzle.AllCollected += OnAllLetters;
                letterPuzzle.Solved += OnWordSolved;
            }
            if (door)
            {
                door.onLockedInteract = () =>
                {
                    if (puzzle && puzzle.IsSolved && letterPuzzle)
                    {
                        // 2ª trava: painel das letras
                        if (letterPuzzle.Found == 0 && doorNeedsWord.Count > 0) Say(doorNeedsWord);
                        else letterPuzzle.OpenLock();
                        return;
                    }
                    ShowLeversObjective();
                    Say(doorLocked);
                };
                door.onEnter = () => StartCoroutine(Leave(nextScene));
            }

            // 1) cutscene: o diário do Kai. "Continuar" no menu pula direto para o vagão.
            bool continuing = GameSave.ContinueRequested;
            GameSave.ContinueRequested = false;
            if (playIntroCutscene && introCutscene && !continuing)
                yield return introCutscene.Play();

            // 2) o Kai acorda no vagão
            if (ambience)
            {
                ambience.volume = 0f;
                ambience.loop = true;
                ambience.Play();
                StartCoroutine(FadeAudio(ambience, ambienceVolume, 3f));
            }

            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 2.2f);
            yield return new WaitForSeconds(0.3f);

            bool done = false;
            if (DialogueBox.Instance) DialogueBox.Instance.Play(intro, () => done = true);
            else done = true;
            while (!done) yield return null;

            if (player) player.controlEnabled = true;
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveStart);
            GameSave.Save(SceneManager.GetActiveScene().name); // libera o "Continuar" no menu
        }

        void Update()
        {
            if (leaving || (introCutscene && introCutscene.IsPlaying) || LetterLockUI.IsOpen) return;
            if (InputHelper.BackPressed() && !DialogueBox.IsBusy)
                StartCoroutine(Leave(menuScene));

            // depois de falar com o Souta (que explica o letreiro), o objetivo fica mais claro
            if (!leversObjectiveShown && souta && souta.TimesTalked > 0 && !DialogueBox.IsBusy)
                ShowLeversObjective();
        }

        void ShowLeversObjective()
        {
            if (leversObjectiveShown || (puzzle && puzzle.IsSolved)) return;
            leversObjectiveShown = true;
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveLevers);
            if (puzzle) puzzle.HintSoon(1.2f);
        }

        void OnFailed()
        {
            fails++;
            if (Hud.Instance) Hud.Instance.Toast("A porta travou de novo.");
            if (cameraFollow) cameraFollow.Shake(0.06f, 0.5f);
            if (fails == 1) StartCoroutine(SayLater(firstFail, 1.6f));
        }

        /// <summary>Alavancas resolvidas: abre a 1ª trava. Se houver o puzzle das letras, ele começa.</summary>
        void OnSolved()
        {
            if (cameraFollow) cameraFollow.Shake(0.04f, 0.8f);
            if (letterPuzzle)
            {
                if (Hud.Instance) Hud.Instance.Toast("Uma das travas abriu.");
                letterPuzzle.Activate();
                return;
            }
            OpenDoor();
        }

        void OnLettersRevealed()
        {
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveLetters);
            StartCoroutine(SayLater(lettersAppear, 0.6f));
        }

        void OnLetterCollected(char c)
        {
            if (Hud.Instance) Hud.Instance.Toast($"Letra encontrada:  {char.ToUpperInvariant(c)}");
        }

        void OnAllLetters()
        {
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveWord);
            StartCoroutine(SayLater(allLetters, 1.0f));
        }

        void OnWordSolved()
        {
            StartCoroutine(WordSolvedRoutine());
        }

        IEnumerator WordSolvedRoutine()
        {
            yield return new WaitForSeconds(0.3f);
            bool done = false;
            if (DialogueBox.Instance && wordSolved.Count > 0) DialogueBox.Instance.Play(wordSolved, () => done = true);
            else done = true;
            while (!done) yield return null;
            OpenDoor();
        }

        void OpenDoor()
        {
            if (door) door.Unlock();
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveExit);
            if (cameraFollow) cameraFollow.Shake(0.04f, 0.8f);
            StartCoroutine(SayLater(solved, 2.2f));
        }

        IEnumerator SayLater(List<DialogueLine> lines, float delay)
        {
            yield return new WaitForSeconds(delay);
            while (DialogueBox.IsBusy) yield return null;
            Say(lines);
        }

        void Say(List<DialogueLine> lines)
        {
            if (DialogueBox.Instance && lines != null && lines.Count > 0) DialogueBox.Instance.Play(lines);
        }

        IEnumerator Leave(string scene)
        {
            if (leaving) yield break;
            leaving = true;
            if (player) player.controlEnabled = false;
            if (ambience) StartCoroutine(FadeAudio(ambience, 0f, 1.4f));
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 1.4f);

            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                // Cenário 2 ainda não existe: mostra uma cartela e volta ao menu.
                if (Hud.Instance) yield return Hud.Instance.ShowEnd(endTitle, endSubtitle);
                yield return new WaitForSeconds(3.5f);
                scene = menuScene;
            }
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
            else Debug.LogWarning($"[Cenário 1] A cena \"{scene}\" não está na lista de cenas do build.");
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
