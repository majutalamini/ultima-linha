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

        [Header("O relógio (04:17)")]
        [Tooltip("Vermelho que cobre a tela enquanto o Souta fala do relógio.")]
        public CanvasGroup redTint;
        public AudioSource heartbeat;
        [Tooltip("Onde o Souta para, olhando o relógio (x em unidades).")]
        public float clockX = 12.5f;

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
        public string objectiveClock = "Olhe o relógio da parede: a senha é a hora em que ele parou.";
        public string objectiveCode = "Abra a gaveta: a senha é 04:17, sem os dois pontos.";

        [Header("Falas")]
        public List<DialogueLine> intro = new List<DialogueLine>();
        public List<DialogueLine> leverHalf = new List<DialogueLine>();
        public List<DialogueLine> powerOn = new List<DialogueLine>();
        public List<DialogueLine> drawerOpened = new List<DialogueLine>();
        [Tooltip("O Souta encara o relógio e diz que é a hora da morte de todos; depois esquece o que disse.")]
        public List<DialogueLine> soutaClock = new List<DialogueLine>();
        public List<DialogueLine> soutaForgets = new List<DialogueLine>();
        public List<DialogueLine> drawerNoClue = new List<DialogueLine>();
        public List<DialogueLine> drawerNeedClock = new List<DialogueLine>();
        public List<DialogueLine> doorNoPower = new List<DialogueLine>();
        public List<DialogueLine> doorNoKey = new List<DialogueLine>();
        public List<DialogueLine> doorUnlocked = new List<DialogueLine>();

        [Header("Fim do cenário (enquanto o Cenário 5 não existe)")]
        public string endTitle = "FIM DO CENÁRIO 4";
        public string endSubtitle = "O escritório ainda está sendo construído.";

        Vector2 previousGravity;
        bool introDone, leaving, powered, hasKey, drawerDone, halfSaid, clockSeen, noteRead, clockScene;
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
            bool continuing = GameSave.BeginScene(SceneManager.GetActiveScene().name);
            hasKey = continuing && GameSave.Has("chave");
            if (player) player.controlEnabled = false;

            foreach (var l in levers)
                if (l) l.Raised += OnLeverRaised;
            if (emptySocket) emptySocket.Presented += OnSocket;
            if (drawer) drawer.Intercept = OnDrawerInteract;
            if (clock) clock.Presented += OnClock;
            if (note) note.Presented += (s, first) =>
            {
                noteRead = true;
                GameSave.Mark("bilhete");
                if (!clockSeen && !hasKey && Hud.Instance) Hud.Instance.SetObjective(objectiveClock);
            };
            if (lockUI) lockUI.Submitted += OnCodeSubmitted;
            if (door)
            {
                door.onLockedInteract = OnDoorLocked;
                door.onEnter = () => StartCoroutine(Leave(nextScene));
            }

            // jogo continuado: volta tudo como estava quando foi salvo
            if (continuing)
            {
                noteRead = GameSave.Has("bilhete");
                clockSeen = GameSave.Has("relogio");
                drawerDone = hasKey;
                if (drawerDone && drawer && drawer.openVisual) drawer.openVisual.SetActive(true);
                for (int i = 0; i < levers.Length; i++)
                    if (levers[i] && GameSave.Has("alavanca" + i)) { levers[i].SetUpInstant(); raised++; }
                if (levers.Length > 0 && raised >= levers.Length) PowerOnInstant();
                halfSaid = raised > 0;
                AutoSave.RestorePlayer(player, cameraFollow);
            }
            if (cameraFollow) cameraFollow.Snap();

            if (ambience)
            {
                ambience.loop = true;
                ambience.volume = 0f;
                ambience.Play();
                StartCoroutine(FadeAudio(ambience, ambienceVolume, 3f));
            }
            if (!powered) StartCoroutine(DimLoop());

            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, continuing ? 1.2f : 2.0f);
            if (darkness && !powered) StartCoroutine(FadeGroup(darkness, darkAlpha, 1.2f));
            yield return new WaitForSeconds(continuing ? 0.2f : 0.6f);

            if (!continuing) yield return SayAndWait(intro);
            else if (Hud.Instance) Hud.Instance.Toast("Jogo carregado.");

            if (player) player.controlEnabled = true;
            if (Hud.Instance) Hud.Instance.SetObjective(CurrentObjective());
            introDone = true;
        }

        string CurrentObjective()
        {
            if (!powered) return objectivePower;
            if (hasKey) return objectiveDoor;
            if (clockSeen) return objectiveCode;
            if (noteRead) return objectiveClock;
            return objectiveKey;
        }

        /// <summary>Energia já ligada (jogo continuado): luz acesa, gerador zumbindo, porta âmbar.</summary>
        void PowerOnInstant()
        {
            powered = true;
            if (darkness) darkness.alpha = 0f;
            foreach (var e in emergencyLights)
                if (e) e.enabled = false;
            if (hum)
            {
                hum.loop = true;
                hum.volume = humVolume;
                hum.Play();
            }
            if (door) door.SetPartial();
        }

        void Update()
        {
            if (!introDone || leaving) return;
            if (InputHelper.BackPressed() && !DialogueBox.IsBusy && !DiarioKai.IsOpen && !Modal.IsOpen)
                StartCoroutine(Leave(menuScene));
        }

        // ---------- Alavancas e energia ----------

        void OnLeverRaised(PowerLever lever)
        {
            raised++;
            GameSave.Mark("alavanca" + System.Array.IndexOf(levers, lever));
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
            if (Hud.Instance) Hud.Instance.SetObjective(hasKey ? objectiveDoor : (clockSeen ? objectiveCode : objectiveKey));
        }

        void OnSocket(ObjectSpot spot, bool firstTime)
        {
            if (sfx && emptyClip) sfx.PlayOneShot(emptyClip, 0.6f);
        }

        // ---------- Gaveta ----------

        bool OnDrawerInteract(ObjectSpot spot)
        {
            if (drawerDone || !lockUI) return false; // já aberta: mostra a fala normal
            if (!clockSeen)
            {
                // sem o relógio não há senha: ele é a peça que faz a história andar
                Say(noteRead ? drawerNeedClock : drawerNoClue);
                return true;
            }
            lockUI.Open("0000");
            if (lockUI.status) lockUI.status.text = "A hora em que o relógio parou.";
            return true;
        }

        // ---------- O relógio ----------

        void OnClock(ObjectSpot spot, bool firstTime)
        {
            if (clockSeen || clockScene) return;
            StartCoroutine(ClockScene());
        }

        /// <summary>
        /// O Souta para na frente do relógio e, com a voz vazia, diz que 04:17 é a hora em que todos vão morrer.
        /// Logo depois volta ao normal e não lembra de nada. A hora vira a senha da gaveta.
        /// </summary>
        IEnumerator ClockScene()
        {
            clockScene = true;
            if (player) player.controlEnabled = false;
            if (Hud.Instance) Hud.Instance.SetObjective("");
            if (souta)
            {
                souta.enabled = false;
                yield return Walk(souta, clockX + 1.6f, 2.6f);
                if (souta.rig) souta.rig.facing = -1f;
            }
            yield return new WaitForSeconds(0.5f);
            if (heartbeat)
            {
                heartbeat.loop = true; heartbeat.volume = 0f; heartbeat.Play();
                StartCoroutine(FadeAudio(heartbeat, 0.5f, 1.5f));
            }
            if (redTint) StartCoroutine(FadeGroup(redTint, 0.22f, 1.5f));
            if (ambience) StartCoroutine(FadeAudio(ambience, ambienceVolume * 0.3f, 1.5f));
            if (cameraFollow) cameraFollow.Shake(0.03f, 1.2f);
            yield return SayAndWait(soutaClock);

            // ele "acorda"
            if (redTint) StartCoroutine(FadeGroup(redTint, 0f, 0.6f));
            if (heartbeat) StartCoroutine(FadeAudio(heartbeat, 0f, 1.0f));
            if (ambience) StartCoroutine(FadeAudio(ambience, ambienceVolume, 2f));
            if (souta && souta.rig) souta.rig.facing = player && player.transform.position.x < souta.transform.position.x ? -1f : 1f;
            yield return new WaitForSeconds(0.6f);
            yield return SayAndWait(soutaForgets);
            if (souta) souta.enabled = true;

            clockSeen = true;
            GameSave.Mark("relogio");
            if (Hud.Instance)
            {
                Hud.Instance.Toast("Senha da gaveta: 0417");
                Hud.Instance.SetObjective(hasKey ? (powered ? objectiveDoor : objectivePower) : objectiveCode);
            }
            if (player) player.controlEnabled = true;
        }

        IEnumerator Walk(NpcTalk npc, float x, float speed)
        {
            if (!npc) yield break;
            var rig = npc.rig;
            while (Mathf.Abs(npc.transform.position.x - x) > 0.05f)
            {
                float dir = Mathf.Sign(x - npc.transform.position.x);
                if (rig) { rig.facing = dir; rig.externalSpeed = speed; }
                var p = npc.transform.position;
                p.x = Mathf.MoveTowards(p.x, x, speed * Time.deltaTime);
                npc.transform.position = p;
                yield return null;
            }
            if (rig) rig.externalSpeed = 0f;
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
            GameSave.Mark("chave");
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
            if (player) player.controlEnabled = false;
            if (sfx && keyClip) sfx.PlayOneShot(keyClip);
            if (Hud.Instance) Hud.Instance.Toast("Você usou a chave.");
            yield return new WaitForSeconds(0.6f);
            door.Unlock();
            yield return new WaitForSeconds(0.2f);
            yield return SayAndWait(doorUnlocked);
            if (player) player.controlEnabled = true;
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
