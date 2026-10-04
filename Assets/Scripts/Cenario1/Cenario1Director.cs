using System.Collections;
using System.Collections.Generic;
using Platformer.Mechanics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Roteiro do Cenário 1: o Kai acorda no vagão parado, encontra Souta e Ren,
    /// resolve as alavancas e sai pela porta do fundo.
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
        public string objectiveExit = "A porta do fundo abriu.";

        [Header("Falas")]
        public List<DialogueLine> intro = new List<DialogueLine>();
        public List<DialogueLine> doorLocked = new List<DialogueLine>();
        public List<DialogueLine> firstFail = new List<DialogueLine>();
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
            if (door)
            {
                door.onLockedInteract = () =>
                {
                    ShowLeversObjective();
                    Say(doorLocked);
                };
                door.onEnter = () => StartCoroutine(Leave(nextScene));
            }

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
        }

        void Update()
        {
            if (leaving) return;
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

        void OnSolved()
        {
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
