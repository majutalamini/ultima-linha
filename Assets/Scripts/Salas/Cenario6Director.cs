using System.Collections;
using System.Collections.Generic;
using Platformer.Mechanics;
using TMPro;
using UltimaLinha.Cenario1;
using UltimaLinha.Cenario2;
using UnityEngine;
using UnityEngine.SceneManagement;
using static UltimaLinha.Salas.DirUtil;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Cenário 6 — a alavanca. O Kai encaixa a alavanca no painel, o metrô liga com um estrondo e uma contagem regressiva
    /// começa. Ele corre pelo túnel, pulando caixotes, cavalete, entulho e buracos no piso, até o metrô parado no fim.
    /// O tempo só dá correndo (SHIFT); andando não chega, e os buracos só se pulam correndo.
    /// Caiu num buraco: volta ao último ponto de controle (o tempo continua). Tempo esgotado: um trem vem pelo túnel e
    /// atropela o Kai; a corrida recomeça do início.
    /// </summary>
    public class Cenario6Director : MonoBehaviour
    {
        public PlayerController player;
        public CameraFollow2D cameraFollow;

        [Header("Cena")]
        public ObjectSpot socket;
        [Tooltip("A alavanca do chefe encaixada no painel (aparece quando o Kai a coloca).")]
        public GameObject leverInserted;
        public Transform leverHandle;
        [Tooltip("Maior que zero: o cabo desce por esse tanto (u) no trilho do painel, em vez de girar.")]
        public float leverDesce;
        public AudioClip leverClip;
        public ExitDoor door;
        public CarDoor[] gates = new CarDoor[0];
        public float[] checkpoints = new float[0];
        public float spawnY = 1.6f;
        public float countdownSeconds = 70f;
        public Vector2 gravity = new Vector2(0f, -20f);

        [Header("Quedas e atropelamento")]
        [Tooltip("Abaixo desse y (centro do Kai) ele caiu num buraco.")]
        public float fallY = -10f;
        [Tooltip("O trem que vem quando o tempo acaba (pivô na frente, embaixo). Fica desligado até lá.")]
        public Transform train;
        public SpriteRenderer trainLight;
        public float trainY = 0f;
        public float trainSpeed = 30f;
        public AudioSource trainSource;
        public AudioClip hornClip, approachClip, hitClip;
        [Tooltip("Luminárias da parede que piscam em vermelho com o alarme.")]
        public LuzAlarme alarmLights;

        [Header("Interface")]
        public TMP_Text timerText;
        public CanvasGroup redTint;

        [Header("Cenas")]
        public string nextScene = "Cenario7";
        public string menuScene = "MainMenu";

        [Header("Som")]
        public AudioSource ambience;
        public AudioSource alarm;
        public AudioSource sfx;
        public AudioClip activateClip, tickClip, crushClip, timeoutClip;

        public string objectiveSocket = "Encaixe a alavanca no painel.";
        public string objectiveRun = "Corra (SHIFT) até o metrô antes que o tempo acabe.";

        public List<DialogueLine> intro = new List<DialogueLine>();
        public List<DialogueLine> inserted = new List<DialogueLine>();
        public List<DialogueLine> firstCrush = new List<DialogueLine>();
        public List<DialogueLine> firstFall = new List<DialogueLine>();
        public List<DialogueLine> timeout = new List<DialogueLine>();
        public List<DialogueLine> doorBefore = new List<DialogueLine>();
        public List<DialogueLine> boarding = new List<DialogueLine>();

        public string endTitle = "FIM DO CENÁRIO 6";
        public string endSubtitle = "A plataforma final ainda está sendo construída.";

        Vector2 previousGravity;
        bool introDone, leaving, started, dying, finished;
        float remaining;
        int checkpoint, crushes, falls, lastTick = -1;

        void Awake()
        {
            previousGravity = Physics2D.gravity;
            Physics2D.gravity = gravity;
            Time.timeScale = 1f;
            Modal.Reset();
            foreach (var g in gates) if (g) g.enabled = false; // só se mexem depois que o metrô liga
            if (timerText) timerText.text = "";
            if (redTint) redTint.alpha = 0f;
            if (train) train.gameObject.SetActive(false);
            if (alarmLights) alarmLights.ligada = false;
        }

        void OnDestroy() { Physics2D.gravity = previousGravity; }

        IEnumerator Start()
        {
            bool continuing = GameSave.BeginScene(SceneManager.GetActiveScene().name);
            if (player) player.controlEnabled = false;
            if (cameraFollow) cameraFollow.Snap();
            if (socket) socket.Intercept = OnSocket;
            // a alavanca veio do bolso do chefe (Cenário 5): o Kai sempre chega aqui com ela
            if (!GameFlags.Get(GameFlags.MissingLever)) GameFlags.Set(GameFlags.MissingLever);
            if (leverInserted) leverInserted.SetActive(false);
            if (door)
            {
                door.onLockedInteract = () => { door.FlashLocked(); Say(doorBefore); };
                door.onEnter = () => StartCoroutine(Board());
            }
            if (ambience)
            {
                ambience.loop = true; ambience.volume = 0f; ambience.Play();
                StartCoroutine(FadeAudio(ambience, 0.5f, 3f));
            }
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 2.0f);
            yield return new WaitForSeconds(0.5f);
            if (!continuing) yield return SayAndWait(intro);
            else if (Hud.Instance) Hud.Instance.Toast("Jogo carregado.");
            if (player) player.controlEnabled = true;
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveSocket);
            GameSave.Save(SceneManager.GetActiveScene().name);
            introDone = true;
        }

        bool OnSocket(ObjectSpot spot)
        {
            if (started) return true;
            StartCoroutine(Activate());
            return true;
        }

        IEnumerator Activate()
        {
            started = true;
            if (player) player.controlEnabled = false;
            if (Hud.Instance) Hud.Instance.SetObjective("");
            // encaixa a alavanca e puxa para cima
            if (leverInserted) leverInserted.SetActive(true);
            if (sfx && leverClip) sfx.PlayOneShot(leverClip);
            if (Hud.Instance) Hud.Instance.Toast("Alavanca encaixada.");
            yield return new WaitForSeconds(0.5f);
            if (leverHandle)
            {
                float t = 0f;
                Vector3 p0 = leverHandle.localPosition;
                while (t < 0.45f)
                {
                    t += Time.deltaTime;
                    float k = Mathf.SmoothStep(0f, 1f, t / 0.45f);
                    if (leverDesce > 0f) leverHandle.localPosition = p0 + Vector3.down * (leverDesce * k);
                    else leverHandle.localRotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-38f, 38f, k));
                    yield return null;
                }
            }
            GameFlags.Set(GameFlags.MissingLever, false);
            yield return new WaitForSeconds(0.4f);
            if (sfx && activateClip) sfx.PlayOneShot(activateClip, 0.6f);
            if (cameraFollow) cameraFollow.Shake(0.09f, 3.2f);
            yield return new WaitForSeconds(1.2f);
            if (alarm) { alarm.loop = true; alarm.volume = 0.3f; alarm.Play(); }
            if (alarmLights) { alarmLights.som = alarm; alarmLights.ligada = true; } // as luminárias da parede começam a piscar em vermelho
            foreach (var g in gates) if (g) g.enabled = true;
            if (door) door.Unlock();
            yield return new WaitForSeconds(1.6f);
            yield return SayAndWait(inserted);
            remaining = countdownSeconds;
            if (player) player.controlEnabled = true;
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveRun);
        }

        void Update()
        {
            if (!introDone || leaving) return;
            if (InputHelper.BackPressed() && !DialogueBox.IsBusy && !DiarioKai.IsOpen && !Modal.IsOpen) { StartCoroutine(Leave(menuScene)); return; }
            if (!started || finished || dying) return;
            if (remaining > 0f && !DialogueBox.IsBusy && !Modal.IsOpen)
            {
                remaining -= Time.deltaTime;
                UpdateTimer();
                if (remaining <= 0f) { StartCoroutine(TimeUp()); return; }
            }
            if (!player) return;
            var b = player.Bounds;
            foreach (var g in gates)
                if (g && g.IsCrushing(b)) { StartCoroutine(Die()); return; }
            if (player.transform.position.y < fallY) { StartCoroutine(Fall()); return; }
            float x = player.transform.position.x;
            for (int i = checkpoints.Length - 1; i > checkpoint; i--)
                if (x >= checkpoints[i]) { checkpoint = i; break; }
        }

        void UpdateTimer()
        {
            int sec = Mathf.CeilToInt(Mathf.Max(0f, remaining));
            if (timerText)
            {
                timerText.text = $"{sec / 60:00}:{sec % 60:00}";
                bool low = remaining < 5f;
                timerText.color = low && Mathf.Repeat(Time.time * 2f, 1f) < 0.5f ? new Color(1f, 0.75f, 0.7f) : new Color(0.85f, 0.27f, 0.23f);
            }
            // o vermelho do alarme fica na parede (LuzAlarme), não na tela toda
            if (redTint) redTint.alpha = 0f;
            if (sec != lastTick)
            {
                lastTick = sec;
                if (remaining < 6f && sfx && tickClip) sfx.PlayOneShot(tickClip, 0.5f);
            }
        }

        IEnumerator Die()
        {
            if (dying) yield break;
            dying = true;
            player.controlEnabled = false;
            crushes++;
            if (sfx && crushClip) sfx.PlayOneShot(crushClip);
            if (cameraFollow) cameraFollow.Shake(0.18f, 0.5f);
            if (Hud.Instance) Hud.Instance.Toast("A antepara bateu em cima do Kai.");
            yield return new WaitForSeconds(0.15f);
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 0.25f);
            yield return Respawn(checkpoint);
            dying = false;
            if (crushes == 1) Say(firstCrush);
        }

        /// <summary>Caiu num buraco do piso: volta ao último ponto de controle. O tempo não volta.</summary>
        IEnumerator Fall()
        {
            if (dying) yield break;
            dying = true;
            player.controlEnabled = false;
            falls++;
            if (Hud.Instance) Hud.Instance.Toast("O Kai caiu no buraco.");
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 0.25f);
            yield return Respawn(checkpoint);
            dying = false;
            if (falls == 1) Say(firstFall);
        }

        /// <summary>O tempo acabou: um trem vem pelo túnel, o farol enche a tela e ele passa por cima do Kai.</summary>
        IEnumerator TimeUp()
        {
            dying = true;
            player.controlEnabled = false;
            if (timerText) timerText.text = "00:00";
            var cam = Camera.main;
            float kx = player.transform.position.x;
            float camL = cam ? cam.transform.position.x - cam.orthographicSize * cam.aspect : kx - 9f;
            var rig = player.GetComponentInChildren<PersonagemPixel>();

            if (trainSource)
            {
                trainSource.Stop();
                if (approachClip) trainSource.PlayOneShot(approachClip, 0.6f);
                if (hornClip) trainSource.PlayOneShot(hornClip, 0.55f);
            }
            else if (sfx && hornClip) sfx.PlayOneShot(hornClip);

            if (train)
            {
                // o farol aparece primeiro, na beira esquerda da tela, e cresce
                train.gameObject.SetActive(true);
                train.position = new Vector3(camL - 0.7f, trainY, 0f); // só o farol aparece na beira da tela
                float t = 0f;
                while (t < 1.1f)
                {
                    t += Time.deltaTime;
                    if (trainLight) { var c = trainLight.color; c.a = Mathf.Clamp01(t / 1.1f); trainLight.color = c; }
                    if (cameraFollow && t > 0.6f) cameraFollow.Shake(0.03f, 0.1f);
                    yield return null;
                }
                // e então o trem entra correndo
                bool bateu = false;
                float depois = 0f;
                while (depois < 0.22f)
                {
                    train.position += Vector3.right * trainSpeed * Time.deltaTime;
                    if (!bateu && train.position.x >= kx - 0.2f)
                    {
                        bateu = true;
                        if (sfx && hitClip) sfx.PlayOneShot(hitClip, 0.7f);
                        if (sfx && crushClip) sfx.PlayOneShot(crushClip, 0.5f);
                        if (cameraFollow) cameraFollow.Shake(0.3f, 0.6f);
                        if (rig && rig.view) rig.view.enabled = false;
                    }
                    if (bateu) depois += Time.deltaTime;
                    yield return null;
                }
            }
            else if (sfx && timeoutClip) sfx.PlayOneShot(timeoutClip);

            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 0.2f);
            if (Hud.Instance) Hud.Instance.Toast("O tempo acabou. O trem atropelou o Kai.");
            yield return new WaitForSeconds(0.9f);
            if (trainSource) trainSource.Stop();
            if (train) train.gameObject.SetActive(false);
            if (trainLight) { var c = trainLight.color; c.a = 0f; trainLight.color = c; }
            if (rig && rig.view) rig.view.enabled = true;
            checkpoint = 0;
            remaining = countdownSeconds;
            lastTick = -1;
            UpdateTimer();
            yield return Respawn(0);
            dying = false;
            Say(timeout);
        }

        IEnumerator Respawn(int index)
        {
            float cx = checkpoints.Length > 0 ? checkpoints[Mathf.Clamp(index, 0, checkpoints.Length - 1)] : player.transform.position.x;
            player.Teleport(new Vector3(cx, spawnY, 0f));
            yield return new WaitForFixedUpdate();
            yield return null;
            if (cameraFollow) cameraFollow.Snap();
            yield return new WaitForSeconds(0.35f);
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 0.6f);
            player.controlEnabled = true;
        }

        IEnumerator Board()
        {
            if (finished) yield break;
            finished = true;
            if (timerText) timerText.text = "";
            if (redTint) redTint.alpha = 0f;
            if (alarmLights) alarmLights.ligada = false;
            if (alarm) StartCoroutine(FadeAudio(alarm, 0f, 1f));
            if (player) player.controlEnabled = false;
            yield return new WaitForSeconds(0.5f);
            yield return SayAndWait(boarding);
            yield return Leave(nextScene);
        }

        IEnumerator Leave(string scene)
        {
            if (leaving) yield break;
            leaving = true;
            if (player) player.controlEnabled = false;
            if (ambience) StartCoroutine(FadeAudio(ambience, 0f, 1.2f));
            if (alarm) StartCoroutine(FadeAudio(alarm, 0f, 1.0f));
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 1.4f);
            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                if (Hud.Instance) yield return Hud.Instance.ShowEnd(endTitle, endSubtitle);
                yield return new WaitForSeconds(3.5f);
                scene = menuScene;
            }
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
            else Debug.LogWarning($"[Cenário 6] A cena \"{scene}\" não está na lista de cenas do build.");
        }
    }
}
