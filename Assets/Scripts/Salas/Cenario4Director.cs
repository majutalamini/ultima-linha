using System.Collections;
using System.Collections.Generic;
using Platformer.Mechanics;
using UltimaLinha.Cenario1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Cenário 4 — a sala de controle, para reativar o trem.
    /// Puzzle: três alavancas (no chão, na passarela e em cima do gerador) precisam ficar levantadas. O painel tem
    /// quatro encaixes: a quarta alavanca não está aqui (está com o chefe, no Cenário 5).
    /// A porta só abre com energia (as 3 alavancas) e com a chave, que está numa gaveta com cadeado de 4 números.
    /// A senha é a hora em que o relógio da parede parou (04:17), dita num bilhete preso na parede.
    /// </summary>
    public class Cenario4Director : MonoBehaviour
    {
        public PlayerController player;
        public CameraFollow2D cameraFollow;
        public NpcTalk souta;
        public NpcTalk ren;

        [Header("Puzzle")]
        public PowerLever[] levers = new PowerLever[0];
        public ObjectSpot emptySocket;
        public ObjectSpot clock;
        public ObjectSpot note;
        public ObjectSpot drawer;
        public ObjectSpot generator;
        public ExitDoor door;
        public DigitLockUI lockUI;
        public string code = "0417";

        [Header("Luz da sala")]
        [Tooltip("Preto por cima da sala: some quando a energia volta.")]
        public CanvasGroup darkness;
        public float darkAlpha = 0.5f;
        public SpriteRenderer[] emergencyLights = new SpriteRenderer[0];

        [Header("Física desta cena")]
        public Vector2 gravity = new Vector2(0f, -20f);

        [Header("Cenas")]
        public string nextScene = "Cenario5";
        public string menuScene = "MainMenu";

        [Header("Som")]
        public AudioSource ambience;
        public float ambienceVolume = 0.5f;
        public AudioSource hum;
        public float humVolume = 0.45f;
        public AudioSource sfx;
        public AudioClip powerOnClip;
        public AudioClip keyClip;
        public AudioClip drawerClip;
        public AudioClip emptyClip;

        [Header("Objetivos")]
        public string objectivePower = "Reative a energia: deixe as alavancas levantadas.";
        public string objectiveKey = "Ache a chave da porta. A gaveta da mesa tem um cadeado.";
        public string objectiveDoor = "Abra a porta da sala.";

        [Header("Falas")]
        public List<DialogueLine> intro = new List<DialogueLine>();
        public List<DialogueLine> leverHalf = new List<DialogueLine>();
        public List<DialogueLine> powerOn = new List<DialogueLine>();
        public List<DialogueLine> drawerOpened = new List<DialogueLine>();
        public List<DialogueLine> doorNoPower = new List<DialogueLine>();
        public List<DialogueLine> doorNoKey = new List<DialogueLine>();
        public List<DialogueLine> doorUnlocked = new List<DialogueLine>();

        [Header("Fim do cenário (enquanto o Cenário 5 não existe)")]
        public string endTitle = "FIM DO CENÁRIO 4";
        public string endSubtitle = "O escritório ainda está sendo construído.";

        Vector2 previousGravity;
        bool introDone, leaving, powered, hasKey, drawerDone, halfSaid;
        int raised;

        void Awake()
        {
            previousGravity = Physics2D.gravity;
            Physics2D.gravity = gravity;
            Time.timeScale = 1f;
            Modal.Reset();
            if (darkness) darkness.alpha = 1f;
        }

        void OnDestroy()
        {
            Physics2D.gravity = previousGravity;
        }

        IEnumerator Start()
        {
            GameSave.ContinueRequested = false;
            hasKey = GameFlags.Get(GameFlags.Cenario4Key);
            if (player) player.controlEnabled = false;
            if (cameraFollow) cameraFollow.Snap();

            foreach (var l in levers)
                if (l) l.Raised += OnLeverRaised;
            if (emptySocket) emptySocket.Presented += OnSocket;
            if (drawer) drawer.Intercept = OnDrawerInteract;
            if (lockUI) lockUI.Submitted += OnCodeSubmitted;
            if (door)
            {
                door.onLockedInteract = OnDoorLocked;
                door.onEnter = () => StartCoroutine(Leave(nextScene));
            }

            if (ambience)
            {
                ambience.loop = true;
                ambience.volume = 0f;
                ambience.Play();
                StartCoroutine(FadeAudio(ambience, ambienceVolume, 3f));
            }
            StartCoroutine(DimLoop());

            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 2.0f);
            if (darkness) StartCoroutine(FadeGroup(darkness, darkAlpha, 1.2f));
            yield return new WaitForSeconds(0.6f);

            yield return SayAndWait(intro);

            if (player) player.controlEnabled = true;
            if (Hud.Instance) Hud.Instance.SetObjective(objectivePower);
            GameSave.Save(SceneManager.GetActiveScene().name);
            introDone = true;
        }

        void Update()
        {
            if (!introDone || leaving) return;
            if (InputHelper.BackPressed() && !DialogueBox.IsBusy && !Modal.IsOpen)
                StartCoroutine(Leave(menuScene));
        }

        // ---------- Alavancas e energia ----------

        void OnLeverRaised(PowerLever lever)
        {
            raised++;
            int total = levers.Length;
            if (raised < total)
            {
                if (Hud.Instance) Hud.Instance.Toast($"Alavanca levantada ({raised}/{total})");
                if (!halfSaid && raised == 1)
                {
                    halfSaid = true;
                    Say(leverHalf);
                }
            }
            else
            {
                StartCoroutine(PowerOn());
            }
        }

        IEnumerator PowerOn()
        {
            powered = true;
            if (player) player.controlEnabled = false;
            yield return new WaitForSeconds(0.5f);
            if (sfx && powerOnClip) sfx.PlayOneShot(powerOnClip);
            if (cameraFollow) cameraFollow.Shake(0.07f, 2.4f);
            if (hum)
            {
                hum.loop = true;
                hum.volume = 0f;
                hum.Play();
                StartCoroutine(FadeAudio(hum, humVolume, 3f));
            }
            // as luzes voltam piscando
            if (darkness)
            {
                float[] steps = { 0.2f, 0.5f, 0.12f, 0.5f, 0.0f, 0.32f, 0f };
                foreach (var a in steps)
                {
                    darkness.alpha = Mathf.Min(darkAlpha, a == 0f ? 0f : a);
                    yield return new WaitForSeconds(0.12f);
                }
                darkness.alpha = 0f;
            }
            foreach (var e in emergencyLights)
                if (e) e.enabled = false;
            if (door) door.SetPartial();
            if (Hud.Instance) Hud.Instance.Toast("A energia voltou.");
            yield return new WaitForSeconds(1.0f);
            yield return SayAndWait(powerOn);
            if (player) player.controlEnabled = true;
            if (Hud.Instance) Hud.Instance.SetObjective(hasKey ? objectiveDoor : objectiveKey);
        }

        void OnSocket(ObjectSpot spot, bool firstTime)
        {
            if (sfx && emptyClip) sfx.PlayOneShot(emptyClip, 0.6f);
        }

        // ---------- Gaveta ----------

        bool OnDrawerInteract(ObjectSpot spot)
        {
            if (drawerDone || !lockUI) return false; // já aberta: mostra a fala normal
            lockUI.Open("0000");
            return true;
        }

        void OnCodeSubmitted(string typed)
        {
            StartCoroutine(CheckCode(typed));
        }

        IEnumerator CheckCode(string typed)
        {
            bool ok = typed == code;
            lockUI.ShowResult(ok);
            if (!ok) yield break;

            yield return new WaitForSecondsRealtime(0.9f);
            lockUI.Close();
            yield return new WaitForSecondsRealtime(0.5f);
            drawerDone = true;
            if (drawer && drawer.openVisual) drawer.openVisual.SetActive(true);
            if (sfx && drawerClip) sfx.PlayOneShot(drawerClip);
            yield return new WaitForSecondsRealtime(0.7f);
            hasKey = true;
            GameFlags.Set(GameFlags.Cenario4Key);
            if (sfx && keyClip) sfx.PlayOneShot(keyClip);
            if (Hud.Instance) Hud.Instance.Toast("Chave encontrada.");
            yield return SayAndWait(drawerOpened);
            if (Hud.Instance) Hud.Instance.SetObjective(powered ? objectiveDoor : objectivePower);
        }

        // ---------- Porta ----------

        void OnDoorLocked()
        {
            if (!powered)
            {
                door.FlashLocked();
                Say(doorNoPower);
            }
            else if (!hasKey)
            {
                door.FlashLocked();
                Say(doorNoKey);
            }
            else
            {
                StartCoroutine(UnlockDoor());
            }
        }

        IEnumerator UnlockDoor()
        {
            door.Unlock();
            yield return new WaitForSeconds(0.2f);
            yield return SayAndWait(doorUnlocked);
            if (Hud.Instance) Hud.Instance.SetObjective("");
        }

        // ---------- Utilidades ----------

        IEnumerator DimLoop()
        {
            // luz de emergência vermelha pulsando enquanto não tem energia
            float seed = Random.value * 10f;
            while (!powered)
            {
                float a = 0.45f + 0.35f * Mathf.Sin(Time.time * 1.6f + seed);
                foreach (var e in emergencyLights)
                    if (e) { var c = e.color; c.a = a; e.color = c; }
                yield return null;
            }
        }

        IEnumerator SayAndWait(List<DialogueLine> lines)
        {
            if (!DialogueBox.Instance || lines == null || lines.Count == 0) yield break;
            bool done = false;
            DialogueBox.Instance.Play(lines, () => done = true);
            while (!done) yield return null;
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
            if (ambience) StartCoroutine(FadeAudio(ambience, 0f, 1.2f));
            if (hum) StartCoroutine(FadeAudio(hum, 0f, 1.2f));
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 1.4f);

            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                if (Hud.Instance) yield return Hud.Instance.ShowEnd(endTitle, endSubtitle);
                yield return new WaitForSeconds(3.5f);
                scene = menuScene;
            }
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
            else Debug.LogWarning($"[Cenário 4] A cena \"{scene}\" não está na lista de cenas do build.");
        }

        static IEnumerator FadeGroup(CanvasGroup g, float to, float duration)
        {
            float from = g.alpha, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                g.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            g.alpha = to;
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
