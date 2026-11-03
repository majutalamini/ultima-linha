using System.Collections;
using System.Collections.Generic;
using Platformer.Mechanics;
using UltimaLinha.Cenario1;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UltimaLinha.Salas.DirUtil;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Cenário 3 — a estação.
    /// Puzzle: a porta da administração é ligada ao painel de desvios da estação. Quatro peças de trilho foram arrancadas
    /// do painel e estão presas atrás de papéis espalhados pela estação (uma delas no mezanino, subindo pelos caixotes).
    /// Com as peças no lugar, o Kai gira os trilhos até a corrente sair da PLATAFORMA e chegar à ADMINISTRAÇÃO.
    /// </summary>
    public class Cenario3Director : MonoBehaviour
    {
        public PlayerController player;
        public CameraFollow2D cameraFollow;
        public NpcTalk souta, ren;

        [Header("Puzzle")]
        [Tooltip("Os papéis com as peças do painel, na ordem das peças (0 a 3).")]
        public ObjectSpot[] documents = new ObjectSpot[0];
        public ExitDoor door;
        public SwitchPanelUI panel;
        [Tooltip("Brilho fraco em cima de cada papel (mesma ordem), some quando ele é lido.")]
        public SpriteRenderer[] docGlows = new SpriteRenderer[0];
        [Tooltip("Com só uma peça faltando, o Ren entrega a que ele achou.")]
        public List<DialogueLine> renGivesLast = new List<DialogueLine>();

        public Vector2 gravity = new Vector2(0f, -20f);
        public string nextScene = "Cenario4";
        public string menuScene = "MainMenu";

        [Header("Som")]
        public AudioSource ambience;
        public float ambienceVolume = 0.5f;
        public AudioSource sfx;
        public AudioClip noteClip;
        public AudioClip pieceClip;

        public string objectiveFind = "Ache as peças que faltam no painel de desvios. Estão espalhadas pela estação.";
        public string objectiveDoor = "Complete a linha no painel ao lado da porta.";

        public List<DialogueLine> intro = new List<DialogueLine>();
        public List<DialogueLine> allFound = new List<DialogueLine>();
        [Tooltip("Primeira vez no painel, sem nenhuma peça.")]
        public List<DialogueLine> doorClosed = new List<DialogueLine>();
        public List<DialogueLine> doorUnlocked = new List<DialogueLine>();

        public string endTitle = "FIM DO CENÁRIO 3";
        public string endSubtitle = "A sala de controle ainda está sendo construída.";

        Vector2 previousGravity;
        bool introDone, leaving, unlocked, renHelped, explained, busy;
        bool[] found;

        void Awake()
        {
            previousGravity = Physics2D.gravity;
            Physics2D.gravity = gravity;
            Time.timeScale = 1f;
            Modal.Reset();
            found = new bool[documents.Length];
        }

        void OnDestroy() { Physics2D.gravity = previousGravity; }

        IEnumerator Start()
        {
            bool continuing = GameSave.BeginScene(SceneManager.GetActiveScene().name);
            if (player) player.controlEnabled = false;

            foreach (var d in documents) if (d) d.Presented += OnDocument;
            if (panel) panel.Solved += OnSolved;
            if (door)
            {
                door.onLockedInteract = OnDoorLocked;
                door.onEnter = () => StartCoroutine(Leave(nextScene));
            }

            // jogo continuado: peças já achadas e, se for o caso, a linha já completa (porta aberta)
            if (continuing)
            {
                for (int i = 0; i < found.Length; i++) found[i] = GameSave.Has("peca" + i);
                renHelped = GameSave.Has("ren_ajudou");
                explained = GameSave.Has("painel_visto");
                if (GameSave.Has("linha"))
                {
                    unlocked = true;
                    if (panel) panel.MarkSolved();
                    if (door) door.Unlock();
                }
                AutoSave.RestorePlayer(player, cameraFollow);
            }
            if (cameraFollow) cameraFollow.Snap();

            if (ambience)
            {
                ambience.loop = true; ambience.volume = 0f; ambience.Play();
                StartCoroutine(FadeAudio(ambience, ambienceVolume, 3f));
            }
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, continuing ? 1.2f : 2.0f);
            yield return new WaitForSeconds(continuing ? 0.2f : 0.5f);
            if (!continuing) yield return SayAndWait(intro);
            else if (Hud.Instance) Hud.Instance.Toast("Jogo carregado.");
            if (player) player.controlEnabled = true;
            if (Hud.Instance) Hud.Instance.SetObjective(unlocked ? "" : Count() == found.Length ? objectiveDoor : objectiveFind);
            introDone = true;
        }

        void Update()
        {
            for (int i = 0; i < docGlows.Length; i++)
            {
                var g = docGlows[i];
                if (!g) continue;
                bool show = found == null || i >= found.Length || !found[i];
                if (g.enabled != show) g.enabled = show;
                if (show)
                {
                    var c = g.color;
                    c.a = 0.22f + 0.16f * Mathf.Sin(Time.time * 2.6f + i * 1.7f);
                    g.color = c;
                }
            }
            if (!introDone || leaving) return;
            if (InputHelper.BackPressed() && !DialogueBox.IsBusy && !DiarioKai.IsOpen && !Modal.IsOpen) StartCoroutine(Leave(menuScene));
        }

        int Count()
        {
            int n = 0;
            foreach (var f in found) if (f) n++;
            return n;
        }

        void OnDocument(ObjectSpot spot, bool firstTime)
        {
            int i = System.Array.IndexOf(documents, spot);
            if (i < 0 || found[i]) return;
            Found(i);
            if (Count() == found.Length)
            {
                if (Hud.Instance) Hud.Instance.SetObjective(objectiveDoor);
                Say(allFound);
            }
        }

        void Found(int i)
        {
            found[i] = true;
            GameSave.Mark("peca" + i);
            if (sfx && pieceClip) sfx.PlayOneShot(pieceClip, 0.8f);
            else if (sfx && noteClip) sfx.PlayOneShot(noteClip);
            if (Hud.Instance) Hud.Instance.Toast($"Peça do painel encontrada ({Count()}/{found.Length})");
        }

        void OnDoorLocked()
        {
            if (unlocked || busy || !panel) return;
            if (Count() == 0 && !explained)
            {
                StartCoroutine(FirstLook());
                return;
            }
            if (Count() == found.Length - 1 && !renHelped)
            {
                StartCoroutine(RenGivesLast());
                return;
            }
            panel.Open(found);
        }

        /// <summary>Primeira vez na porta, sem peças: o Kai entende o painel e o mostra para o jogador.</summary>
        IEnumerator FirstLook()
        {
            busy = true;
            explained = true;
            GameSave.Mark("painel_visto");
            yield return SayAndWait(doorClosed);
            busy = false;
            panel.Open(found);
        }

        /// <summary>Faltando só uma peça, o Ren aparece com ela: achou caída perto dos caixotes.</summary>
        IEnumerator RenGivesLast()
        {
            busy = true;
            renHelped = true;
            GameSave.Mark("ren_ajudou");
            if (player) player.controlEnabled = false;
            if (ren)
            {
                ren.enabled = false;
                yield return WalkTo(ren, door.transform.position.x - 1.6f, 3.0f);
                if (ren.rig) ren.rig.facing = 1f;
            }
            yield return SayAndWait(renGivesLast);
            int missing = System.Array.IndexOf(found, false);
            if (missing >= 0)
            {
                Found(missing);
                if (Hud.Instance) Hud.Instance.SetObjective(objectiveDoor);
            }
            if (ren) ren.enabled = true;
            if (player) player.controlEnabled = true;
            yield return new WaitForSeconds(0.3f);
            busy = false;
            panel.Open(found);
        }

        void OnSolved()
        {
            if (unlocked) return;
            unlocked = true;
            GameSave.Mark("linha");
            StartCoroutine(OpenDoor());
        }

        IEnumerator OpenDoor()
        {
            if (player) player.controlEnabled = false;
            door.Unlock();
            yield return new WaitForSeconds(0.6f);
            yield return SayAndWait(doorUnlocked);
            if (player) player.controlEnabled = true;
            if (Hud.Instance) Hud.Instance.SetObjective("");
        }

        IEnumerator Leave(string scene)
        {
            if (leaving) yield break;
            leaving = true;
            if (player) player.controlEnabled = false;
            if (ambience) StartCoroutine(FadeAudio(ambience, 0f, 1.2f));
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 1.4f);
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                if (Hud.Instance) yield return Hud.Instance.ShowEnd(endTitle, endSubtitle);
                yield return new WaitForSeconds(3.5f);
                scene = menuScene;
            }
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
            else Debug.LogWarning($"[Cenário 3] A cena \"{scene}\" não está na lista de cenas do build.");
        }
    }
}
