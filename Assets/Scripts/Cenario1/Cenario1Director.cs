using System.Collections;
using System.Collections.Generic;
using Platformer.Mechanics;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaLinha.Cenario1
{
    /// <summary>
    /// Roteiro do Cenário 1: o Kai chega do vagão de trás (Abertura) e encontra Souta e Ren conversando,
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

        [Header("Entrada (vindo da Abertura, sem corte)")]
        [Tooltip("Porta da ponta esquerda: abre, o Kai entra, ela bate e tranca para sempre.")]
        public PortaPonta portaEntrada;
        [Tooltip("Na plataforma do engate: o mesmo lugar e o mesmo quadro em que a Abertura terminou.")]
        public Vector3 entradaInicio;
        [Tooltip("Até onde ele anda antes da porta bater atrás dele.")]
        public float entradaDentro;
        [Tooltip("Onde ele para, perto do Souta e do Ren.")]
        public float conversaX;
        [Tooltip("Câmera na entrada (igual à do fim da Abertura) e no vagão.")]
        public Rect boundsEntrada, boundsVagao;
        public List<DialogueLine> entradaTrancada = new List<DialogueLine>();

        [Header("Dicas para quem está travado")]
        public TrainLever emperrada;
        public int dicaErros = 2;
        public float dicaSegundos = 75f;
        public string dicaToast = "Talvez o Souta ou o Ren saibam de alguma coisa.";

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
        bool emJogo, dicaLigada;
        int erros;
        float semProgresso;

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
                puzzle.Progressed += _ => { ShowLeversObjective(); Progresso(); };
            }
            if (letterPuzzle)
            {
                letterPuzzle.Revealed += OnLettersRevealed;
                letterPuzzle.Collected += OnLetterCollected;
                letterPuzzle.AllCollected += OnAllLetters;
                letterPuzzle.Solved += OnWordSolved;
                if (letterPuzzle.lockUI)
                    letterPuzzle.lockUI.Submitted += w => { if (!string.Equals(w, letterPuzzle.secretWord, System.StringComparison.OrdinalIgnoreCase)) Erro(); };
            }
            if (emperrada) emperrada.Freed += Progresso;
            if (DestravaUI.Instance) DestravaUI.Instance.Errou += Erro;
            foreach (var npc in new[] { souta, ren })
                if (npc) npc.Talked += OnTalked;
            if (portaEntrada) portaEntrada.onLockedInteract = () => Say(entradaTrancada);
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
            bool continuing = GameSave.BeginScene(SceneManager.GetActiveScene().name);
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

            if (continuing)
            {
                if (cameraFollow) cameraFollow.bounds = boundsVagao;
                if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 2.2f);
            }
            else
                yield return Entrada();

            if (player) player.controlEnabled = true;
            emJogo = true;
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveStart);
            GameSave.Save(SceneManager.GetActiveScene().name); // libera o "Continuar" no menu
        }

        void Update()
        {
            if (leaving || (introCutscene && introCutscene.IsPlaying) || LetterLockUI.IsOpen) return;
            if (InputHelper.BackPressed() && !DialogueBox.IsBusy && !DiarioKai.IsOpen)
                StartCoroutine(Leave(menuScene));

            // depois de falar com o Souta (que explica o letreiro), o objetivo fica mais claro
            if (!leversObjectiveShown && souta && souta.TimesTalked > 0 && !DialogueBox.IsBusy)
                ShowLeversObjective();

            // travado (errando ou parado): os dois têm uma dica
            bool livre = player && player.controlEnabled && !DialogueBox.IsBusy && !UltimaLinha.Salas.Modal.IsOpen;
            if (!emJogo || dicaLigada || (door && door.IsOpen) || (letterPuzzle && letterPuzzle.IsSolved)) return;
            if (livre) semProgresso += Time.deltaTime;
            if (erros >= dicaErros || semProgresso >= dicaSegundos) LigaDica();
        }

        // ------------------------------------------------------------------ entrada

        /// <summary>Continua o quadro em que a Abertura terminou: a porta da ponta abre, o Kai entra, ela bate atrás dele,
        /// Souta e Ren se assustam com o barulho e o Kai vai até eles.</summary>
        IEnumerator Entrada()
        {
            if (player) player.Teleport(entradaInicio);
            if (cameraFollow)
            {
                cameraFollow.bounds = boundsEntrada;
                cameraFollow.Snap();
            }
            if (Hud.Instance && Hud.Instance.fader) Hud.Instance.fader.alpha = 0f; // sem corte: a Abertura acabou neste mesmo quadro

            yield return new WaitForSeconds(0.4f);
            if (portaEntrada)
            {
                portaEntrada.Open();
                while (!portaEntrada.IsOpen) yield return null;
            }
            if (cameraFollow) cameraFollow.bounds = boundsVagao; // a câmera solta e acompanha ele para dentro
            yield return UltimaLinha.Salas.DirUtil.WalkPlayerTo(player, entradaDentro);
            yield return new WaitForSeconds(0.5f);
            if (portaEntrada)
            {
                portaEntrada.Close();
                while (portaEntrada.IsOpen) yield return null;
            }
            if (cameraFollow) cameraFollow.Shake(0.08f, 0.4f);
            var corpo = player ? player.GetComponent<SpriteRenderer>() : null;
            if (corpo) corpo.flipX = true; // olha para trás, para a porta que bateu
            yield return new WaitForSeconds(0.9f);

            // Souta e Ren ouviram a porta; a câmera fica neles até notarem o Kai
            var alvo = cameraFollow ? cameraFollow.target : null;
            if (souta && ren)
            {
                souta.lookAt = ren.transform;
                ren.lookAt = souta.transform;
                if (cameraFollow) cameraFollow.target = souta.transform;
            }
            yield return SayAndWait(intro);
            if (souta) souta.lookAt = player ? player.transform : null;
            if (ren) ren.lookAt = player ? player.transform : null;
            if (cameraFollow && alvo) cameraFollow.target = alvo;

            yield return UltimaLinha.Salas.DirUtil.WalkPlayerTo(player, conversaX);
            yield return new WaitForSeconds(0.2f);
            if (souta)
            {
                souta.Interact(); // a primeira conversa com o Souta já começa
                while (DialogueBox.IsBusy) yield return null;
            }
        }

        // ------------------------------------------------------------------ dicas

        void Progresso()
        {
            semProgresso = 0f;
            erros = 0;
        }

        void Erro() => erros++;

        void LigaDica()
        {
            dicaLigada = true;
            if (souta) souta.dica = true;
            if (ren) ren.dica = true;
            if (Hud.Instance) Hud.Instance.Toast(dicaToast, 6f);
        }

        void OnTalked(NpcTalk npc)
        {
            if (!dicaLigada) return;
            dicaLigada = false;
            if (souta) souta.dica = false;
            if (ren) ren.dica = false;
            Progresso();
        }

        IEnumerator SayAndWait(List<DialogueLine> lines)
        {
            bool done = false;
            if (DialogueBox.Instance && lines != null && lines.Count > 0) DialogueBox.Instance.Play(lines, () => done = true);
            else done = true;
            while (!done) yield return null;
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
            Erro();
            if (Hud.Instance) Hud.Instance.Toast("A porta travou de novo.");
            if (cameraFollow) cameraFollow.Shake(0.06f, 0.5f);
            if (fails == 1) StartCoroutine(SayLater(firstFail, 1.6f));
        }

        /// <summary>Alavancas resolvidas: abre a 1ª trava. Se houver o puzzle das letras, ele começa.</summary>
        void OnSolved()
        {
            Progresso();
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
            Progresso();
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
