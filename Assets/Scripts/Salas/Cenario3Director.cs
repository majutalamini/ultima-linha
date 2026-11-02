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
    /// Puzzle: quatro documentos espalhados pela estação trazem, cada um, um número. Com os quatro (na ordem das folhas 1 a 4)
    /// o Kai abre a porta da administração. O terceiro documento está no mezanino, que se alcança subindo pelos caixotes.
    /// </summary>
    public class Cenario3Director : MonoBehaviour
    {
        public PlayerController player;
        public CameraFollow2D cameraFollow;
        public NpcTalk souta, ren;

        [Header("Puzzle")]
        [Tooltip("Os documentos, na ordem das folhas (1 a 4).")]
        public ObjectSpot[] documents = new ObjectSpot[0];
        public int[] digits = { 5, 2, 9, 1 };
        public ExitDoor door;
        public DigitLockUI lockUI;
        [Tooltip("Brilho fraco em cima de cada documento (mesma ordem), some quando ele é lido.")]
        public SpriteRenderer[] docGlows = new SpriteRenderer[0];
        [Tooltip("Com só um número faltando, o Ren entrega a folha que ele achou.")]
        public List<DialogueLine> renGivesLast = new List<DialogueLine>();

        public Vector2 gravity = new Vector2(0f, -20f);
        public string nextScene = "Cenario4";
        public string menuScene = "MainMenu";

        [Header("Som")]
        public AudioSource ambience;
        public float ambienceVolume = 0.5f;
        public AudioSource sfx;
        public AudioClip noteClip;

        public string objectiveFind = "Ache os documentos da estação. Cada um tem um número.";
        public string objectiveDoor = "Digite o código na porta da administração.";

        public List<DialogueLine> intro = new List<DialogueLine>();
        public List<DialogueLine> allFound = new List<DialogueLine>();
        public List<DialogueLine> doorClosed = new List<DialogueLine>();
        public List<DialogueLine> doorUnlocked = new List<DialogueLine>();

        public string endTitle = "FIM DO CENÁRIO 3";
        public string endSubtitle = "A sala de controle ainda está sendo construída.";

        Vector2 previousGravity;
        bool introDone, leaving, unlocked, renHelped;
        bool[] found;

        void Awake()
        {
            previousGravity = Physics2D.gravity;
            Physics2D.gravity = gravity;
            Time.timeScale = 1f;
            Modal.Reset();
            found = new bool[digits.Length];
        }

        void OnDestroy() { Physics2D.gravity = previousGravity; }

        IEnumerator Start()
        {
            GameSave.ContinueRequested = false;
            if (player) player.controlEnabled = false;
            if (cameraFollow) cameraFollow.Snap();

            foreach (var d in documents) if (d) d.Presented += OnDocument;
            if (lockUI) lockUI.Submitted += OnCode;
            if (door)
            {
                door.onLockedInteract = OnDoorLocked;
                door.onEnter = () => StartCoroutine(Leave(nextScene));
            }
            if (ambience)
            {
                ambience.loop = true; ambience.volume = 0f; ambience.Play();
                StartCoroutine(FadeAudio(ambience, ambienceVolume, 3f));
            }
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 2.0f);
            yield return new WaitForSeconds(0.5f);
            yield return SayAndWait(intro);
            if (player) player.controlEnabled = true;
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveFind);
            GameSave.Save(SceneManager.GetActiveScene().name);
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
            if (InputHelper.BackPressed() && !DialogueBox.IsBusy && !Modal.IsOpen) StartCoroutine(Leave(menuScene));
        }

        int Count()
        {
            int n = 0;
            foreach (var f in found) if (f) n++;
            return n;
        }

        string Typed()
        {
            var s = "";
            for (int i = 0; i < digits.Length; i++) s += found[i] ? digits[i].ToString() : "0";
            return s;
        }

        string Notes()
        {
            var s = "Anotados:";
            for (int i = 0; i < digits.Length; i++) s += "  " + (found[i] ? digits[i].ToString() : "_");
            return s;
        }

        void OnDocument(ObjectSpot spot, bool firstTime)
        {
            int i = System.Array.IndexOf(documents, spot);
            if (i < 0 || found[i]) return;
            found[i] = true;
            if (sfx && noteClip) sfx.PlayOneShot(noteClip);
            if (Hud.Instance) Hud.Instance.Toast($"Número anotado ({Count()}/{digits.Length})");
            if (Count() == digits.Length)
            {
                if (Hud.Instance) Hud.Instance.SetObjective(objectiveDoor);
                Say(allFound);
            }
        }

        void OnDoorLocked()
        {
            if (unlocked || !lockUI) return;
            if (Count() == 0)
            {
                door.FlashLocked();
                Say(doorClosed);
                return;
            }
            if (Count() == digits.Length - 1 && !renHelped)
            {
                StartCoroutine(RenGivesLast());
                return;
            }
            OpenLock();
        }

        /// <summary>Faltando só uma folha, o Ren aparece com ela: ele achou caída pela estação.</summary>
        IEnumerator RenGivesLast()
        {
            renHelped = true;
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
                found[missing] = true;
                if (sfx && noteClip) sfx.PlayOneShot(noteClip);
                if (Hud.Instance)
                {
                    Hud.Instance.Toast($"Número anotado ({Count()}/{digits.Length})");
                    Hud.Instance.SetObjective(objectiveDoor);
                }
            }
            if (ren) ren.enabled = true;
            if (player) player.controlEnabled = true;
            yield return new WaitForSeconds(0.3f);
            OpenLock();
        }

        void OpenLock()
        {
            if (unlocked || !lockUI) return;
            lockUI.Open(Typed());
            if (lockUI.status) lockUI.status.text = Notes();
        }

        void OnCode(string typed)
        {
            StartCoroutine(CheckCode(typed));
        }

        IEnumerator CheckCode(string typed)
        {
            string code = "";
            foreach (var d in digits) code += d;
            bool ok = typed == code;
            lockUI.ShowResult(ok);
            if (!ok) yield break;
            yield return new WaitForSecondsRealtime(0.9f);
            lockUI.Close();
            unlocked = true;
            yield return new WaitForSecondsRealtime(0.4f);
            door.Unlock();
            yield return new WaitForSeconds(0.2f);
            yield return SayAndWait(doorUnlocked);
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
