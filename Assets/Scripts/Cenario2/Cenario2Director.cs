using System.Collections;
using System.Collections.Generic;
using Platformer.Mechanics;
using UltimaLinha.Cenario1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaLinha.Cenario2
{
    /// <summary>
    /// Roteiro do Cenário 2: o trem volta a andar. Souta e Ren vão na frente e o Kai precisa atravessar
    /// os últimos vagões passando pelas portas que abrem e batem com o tranco do trem, pulando os vãos
    /// entre os vagões. Se a porta bater nele ou ele cair no vão, volta ao começo do vagão em que estava.
    /// No último vagão o trem freia na estação terminal e a porta do fim abre para a plataforma.
    /// </summary>
    public class Cenario2Director : MonoBehaviour
    {
        public PlayerController player;
        public CameraFollow2D cameraFollow;
        [Tooltip("Portas entre vagões (as que batem).")]
        public CarDoor[] doors = new CarDoor[0];
        [Tooltip("Porta do fim do último vagão (abre quando o trem para).")]
        public CarDoor exitDoor;
        public TunnelLights tunnel;

        [Header("Souta e Ren")]
        public NpcTalk souta;
        public NpcTalk ren;
        [Tooltip("Onde eles esperam, no último vagão.")]
        public Vector3 soutaEnd;
        public Vector3 renEnd;

        [Header("Pontos do cenário (x em unidades)")]
        [Tooltip("Começo de cada vagão: onde o Kai volta se morrer.")]
        public float[] checkpoints = new float[0];
        public float spawnY = 2.33f;
        [Tooltip("Abaixo disso o Kai caiu no vão.")]
        public float killY = -1.5f;
        public float gang2HintX = 30f;
        public float gang3HintX = 50f;
        [Tooltip("Chegou ao último vagão: o trem freia.")]
        public float endTriggerX = 64f;
        [Tooltip("Passou da porta do fim: sai do cenário.")]
        public float exitX = 75f;

        [Header("Física desta cena")]
        public Vector2 gravity = new Vector2(0f, -20f);

        [Header("Cenas")]
        public string nextScene = "Cenario3";
        public string menuScene = "MainMenu";

        [Header("Som")]
        public AudioSource rumble;
        public float rumbleVolume = 0.6f;
        public AudioSource stationAmbience;
        public float stationVolume = 0.5f;
        public AudioSource sfx;
        public AudioClip crushClip;
        public AudioClip fallClip;
        public AudioClip lurchClip;
        public AudioClip brakeClip;
        [Tooltip("Intervalo entre os trancos do trem (segundos).")]
        public Vector2 lurchInterval = new Vector2(7f, 14f);

        [Header("Tela")]
        [Tooltip("Clarão vermelho quando a porta pega o Kai.")]
        public CanvasGroup flash;

        [Header("Objetivos")]
        public string objectiveCross = "Chegue ao último vagão. Passe pelas portas quando abrirem.";
        public string objectiveExit = "Saia do trem pela porta do fim do vagão.";

        [Header("Falas")]
        public List<DialogueLine> intro = new List<DialogueLine>();
        [Tooltip("Souta decide ir na frente com o Ren.")]
        public List<DialogueLine> goAhead = new List<DialogueLine>();
        [Tooltip("O Kai sozinho, depois que eles foram.")]
        public List<DialogueLine> alone = new List<DialogueLine>();
        public List<DialogueLine> firstCrush = new List<DialogueLine>();
        public List<DialogueLine> firstFall = new List<DialogueLine>();
        public List<DialogueLine> gang2Hint = new List<DialogueLine>();
        public List<DialogueLine> gang3Hint = new List<DialogueLine>();
        public List<DialogueLine> braking = new List<DialogueLine>();
        public List<DialogueLine> ending = new List<DialogueLine>();

        [Header("Fim do cenário (enquanto o Cenário 3 não existe)")]
        public string endTitle = "FIM DO CENÁRIO 2";
        public string endSubtitle = "A estação ainda está sendo construída.";

        Vector2 previousGravity;
        bool introDone, leaving, dying, endingStarted, exitOpen;
        bool hint2, hint3;
        int checkpoint, crushes, falls;

        void Awake()
        {
            previousGravity = Physics2D.gravity;
            Physics2D.gravity = gravity;
            Time.timeScale = 1f;
            if (flash) flash.alpha = 0f;
        }

        void OnDestroy()
        {
            Physics2D.gravity = previousGravity;
        }

        IEnumerator Start()
        {
            GameSave.ContinueRequested = false;
            if (player) player.controlEnabled = false;
            if (cameraFollow) cameraFollow.Snap();
            foreach (var d in doors)
                if (d) d.Slammed += OnDoorSlam;
            if (tunnel) tunnel.Moving = true;
            if (rumble)
            {
                rumble.loop = true;
                rumble.volume = rumbleVolume * 0.35f; // o trem ainda está quase parado
                rumble.Play();
            }

            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 1.8f);
            yield return new WaitForSeconds(0.5f);

            // o tranco: o trem volta a andar
            Lurch(0.13f, 1.0f, 1f);
            if (rumble) StartCoroutine(FadeAudio(rumble, rumbleVolume, 2.5f));
            yield return new WaitForSeconds(1.1f);

            yield return SayAndWait(intro);
            yield return SayAndWait(goAhead);

            // Souta e Ren vão na frente
            if (player) player.controlEnabled = false;
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 0.6f);
            if (souta) souta.transform.position = soutaEnd;
            if (ren) ren.transform.position = renEnd;
            yield return new WaitForSeconds(0.5f);
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 0.9f);

            yield return SayAndWait(alone);

            if (player) player.controlEnabled = true;
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveCross);
            GameSave.Save(SceneManager.GetActiveScene().name); // libera o "Continuar" no menu
            introDone = true;
            StartCoroutine(LurchLoop());
        }

        void Update()
        {
            if (!introDone || leaving || dying || !player) return;

            if (InputHelper.BackPressed() && !DialogueBox.IsBusy)
            {
                StartCoroutine(Leave(menuScene));
                return;
            }

            var b = player.Bounds;
            foreach (var d in doors)
            {
                if (d && d.IsCrushing(b))
                {
                    StartCoroutine(Die(crushed: true));
                    return;
                }
            }
            if (player.transform.position.y < killY)
            {
                StartCoroutine(Die(crushed: false));
                return;
            }

            float x = player.transform.position.x;
            for (int i = checkpoints.Length - 1; i > checkpoint; i--)
            {
                if (x > checkpoints[i] + 0.4f && player.IsGrounded)
                {
                    checkpoint = i;
                    break;
                }
            }

            if (DialogueBox.IsBusy) return;
            if (!hint2 && x > gang2HintX && player.IsGrounded) { hint2 = true; Say(gang2Hint); }
            else if (!hint3 && x > gang3HintX && player.IsGrounded) { hint3 = true; Say(gang3Hint); }

            if (!endingStarted && x > endTriggerX && player.IsGrounded) StartCoroutine(EndSequence());
            if (exitOpen && x > exitX) StartCoroutine(Leave(nextScene));
        }

        // ---------- Morte e volta ----------

        IEnumerator Die(bool crushed)
        {
            if (dying) yield break;
            dying = true;
            player.controlEnabled = false;
            if (crushed)
            {
                crushes++;
                if (sfx && crushClip) sfx.PlayOneShot(crushClip);
                if (cameraFollow) cameraFollow.Shake(0.18f, 0.5f);
                StartCoroutine(Flash());
                if (Hud.Instance) Hud.Instance.Toast("A porta bateu em cima do Kai.");
                yield return new WaitForSeconds(0.15f);
                if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 0.25f);
            }
            else
            {
                falls++;
                if (sfx && fallClip) sfx.PlayOneShot(fallClip);
                if (Hud.Instance) Hud.Instance.Toast("O Kai caiu no vão entre os vagões.");
                if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 0.45f);
            }

            float cx = checkpoints.Length > 0 ? checkpoints[Mathf.Clamp(checkpoint, 0, checkpoints.Length - 1)] : player.transform.position.x;
            player.Teleport(new Vector3(cx, spawnY, 0f));
            yield return new WaitForFixedUpdate();
            yield return null;
            if (cameraFollow) cameraFollow.Snap();
            yield return new WaitForSeconds(0.35f);
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 0.6f);
            player.controlEnabled = true;
            dying = false;

            if (crushed && crushes == 1) Say(firstCrush);
            else if (!crushed && falls == 1) Say(firstFall);
        }

        IEnumerator Flash()
        {
            if (!flash) yield break;
            flash.alpha = 0.55f;
            float t = 0f;
            while (t < 0.5f)
            {
                t += Time.unscaledDeltaTime;
                flash.alpha = Mathf.Lerp(0.55f, 0f, t / 0.5f);
                yield return null;
            }
            flash.alpha = 0f;
        }

        // ---------- Trem ----------

        void OnDoorSlam(CarDoor d)
        {
            if (!player || !cameraFollow) return;
            float dx = Mathf.Abs(player.transform.position.x - d.CenterX);
            if (dx < 7f) cameraFollow.Shake(0.05f * (1f - dx / 7f) + 0.01f, 0.3f);
        }

        IEnumerator LurchLoop()
        {
            while (!endingStarted)
            {
                yield return new WaitForSeconds(Random.Range(lurchInterval.x, lurchInterval.y));
                if (endingStarted) yield break;
                if (!dying) Lurch(0.07f, 0.7f, 0.55f);
            }
        }

        void Lurch(float amount, float duration, float volume)
        {
            if (cameraFollow) cameraFollow.Shake(amount, duration);
            if (sfx && lurchClip) sfx.PlayOneShot(lurchClip, volume);
        }

        IEnumerator EndSequence()
        {
            endingStarted = true;
            while (DialogueBox.IsBusy || dying) yield return null;
            player.controlEnabled = false;

            // o trem freia na estação
            if (sfx && brakeClip) sfx.PlayOneShot(brakeClip, 0.9f);
            if (cameraFollow) cameraFollow.Shake(0.12f, 2.8f);
            if (tunnel) tunnel.Moving = false;
            foreach (var d in doors)
                if (d) d.StopOpen();
            if (rumble) StartCoroutine(FadeAudio(rumble, 0f, 3f));
            yield return new WaitForSeconds(0.7f);
            yield return SayAndWait(braking);
            yield return new WaitForSeconds(1.2f);
            if (stationAmbience)
            {
                stationAmbience.loop = true;
                stationAmbience.volume = 0f;
                stationAmbience.Play();
                StartCoroutine(FadeAudio(stationAmbience, stationVolume, 2f));
            }

            yield return SayAndWait(ending);
            if (exitDoor) exitDoor.Open();
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveExit);
            player.controlEnabled = true;
            yield return new WaitForSeconds(0.8f);
            exitOpen = true;
        }

        // ---------- Utilidades ----------

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
            if (rumble) StartCoroutine(FadeAudio(rumble, 0f, 1.2f));
            if (stationAmbience) StartCoroutine(FadeAudio(stationAmbience, 0f, 1.2f));
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 1.4f);

            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                // Cenário 3 ainda não existe: mostra uma cartela e volta ao menu.
                if (Hud.Instance) yield return Hud.Instance.ShowEnd(endTitle, endSubtitle);
                yield return new WaitForSeconds(3.5f);
                scene = menuScene;
            }
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
            else Debug.LogWarning($"[Cenário 2] A cena \"{scene}\" não está na lista de cenas do build.");
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
