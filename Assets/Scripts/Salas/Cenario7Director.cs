using System.Collections;
using System.Collections.Generic;
using Platformer.Mechanics;
using UltimaLinha.Cenario1;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using static UltimaLinha.Salas.DirUtil;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Cenário 7 — final. O Ren está na beira da plataforma; o Souta, atrás, ainda com a arma do chefe.
    /// O jogador escolhe o fim:
    ///  • Acabar com eles: o Kai empurra o Ren (a queda até o fundo da vala aparece inteira), pega a arma que o Souta deixa cair,
    ///    estica os braços e atira no Souta. Sirene de polícia e o plantão: o Kai foi preso no trabalho por tentativa de massacre.
    ///  • Acabar comigo: o Kai vai até a beira enquanto o trem chega; corte para o preto. Sirene de ambulância e o plantão:
    ///    um trabalhador tirou a própria vida no trabalho; ele tinha esquizofrenia e o Souta e o Ren nunca existiram.
    /// </summary>
    public class Cenario7Director : MonoBehaviour
    {
        public PlayerController player;
        public PersonagemPixel kaiRig;
        public CameraFollow2D cameraFollow;

        [Header("Personagens")]
        public NpcTalk souta;
        [Tooltip("O Ren, de costas, olhando os trilhos. Falar com ele começa o final.")]
        public ObjectSpot renSpot;
        public PersonagemPixel renRig;

        [Header("Arma")]
        public GameObject gunOnFloor;
        public SpriteRenderer muzzleFlash;

        [Header("Trem")]
        public SpriteRenderer headlight;

        [Header("Medidas (unidades)")]
        public float edgeX = 27f;
        public float floorY = 0.91f;
        [Tooltip("Altura do fundo da vala, onde o corpo do Ren bate.")]
        public float pitY = -4.69f;
        public Vector2 gravity = new Vector2(0f, -20f);
        [Tooltip("Limites da câmera durante a queda (mostra a vala inteira).")]
        public Rect fallBounds = new Rect(0f, -5.59f, 36f, 15f);

        [Header("Interface")]
        public CanvasGroup darkness;
        public CanvasGroup flash;
        public ChoiceUI choice;
        public NewsView news;

        public string menuScene = "MainMenu";

        [Header("Som")]
        public AudioSource ambience;
        public AudioSource sfx;
        public AudioSource trainRumble;
        public AudioSource siren;
        public AudioClip pushClip, fallClip, hornClip, shotClip, bodyClip, gunDropClip, cockClip;
        public AudioClip ambulanceClip, policeClip;
        [Tooltip("Freio de emergência do trem (no final em que o Kai fica nos trilhos).")]
        public AudioClip brakeClip;

        [Header("Falas")]
        public List<DialogueLine> intro = new List<DialogueLine>();
        public List<DialogueLine> confront = new List<DialogueLine>();
        public List<DialogueLine> othersStart = new List<DialogueLine>();
        public List<DialogueLine> soutaPanic = new List<DialogueLine>();
        public List<DialogueLine> pointGun = new List<DialogueLine>();
        public List<DialogueLine> afterShot = new List<DialogueLine>();
        public List<DialogueLine> selfStart = new List<DialogueLine>();
        public List<DialogueLine> selfLast = new List<DialogueLine>();

        [Header("Escolha")]
        public string choiceTitle = "O QUE O KAI FAZ?";
        public string[] choiceOptions = { "Acabar com eles", "Acabar comigo" };
        public string[] choiceNotes = { "Ninguém mais vai me acusar.", "Isso termina comigo." };

        [Header("Plantão — acabar com eles (polícia)")]
        public string policeChannel = "JORNAL DA NOITE  ·  PLANTÃO";
        public string policeHeadline = "";
        [TextArea(3, 12)] public string policeBody = "";
        public string policeTicker = "";

        [Header("Plantão — acabar comigo (ambulância)")]
        public string ambulanceChannel = "JORNAL DA NOITE  ·  PLANTÃO";
        public string ambulanceHeadline = "";
        [TextArea(3, 12)] public string ambulanceBody = "";
        public string ambulanceTicker = "";

        public string endTitle = "ÚLTIMA LINHA";
        public string endSubtitleOthers = "FIM";
        public string endSubtitleSelf = "FIM";

        Vector2 previousGravity;
        bool introDone, finale;

        void Awake()
        {
            previousGravity = Physics2D.gravity;
            Physics2D.gravity = gravity;
            Time.timeScale = 1f;
            Modal.Reset();
            if (flash) flash.alpha = 0f;
            if (headlight) headlight.color = new Color(1f, 0.96f, 0.85f, 0f);
            if (gunOnFloor) gunOnFloor.SetActive(false);
            if (souta && souta.rig) souta.rig.Tocar("arma", true); // ele chega com a arma do chefe
            if (muzzleFlash) muzzleFlash.color = new Color(1f, 0.9f, 0.6f, 0f);
        }

        void OnDestroy() { Physics2D.gravity = previousGravity; }

        IEnumerator Start()
        {
            bool continuing = GameSave.BeginScene(SceneManager.GetActiveScene().name);
            if (player) player.controlEnabled = false;
            if (cameraFollow) cameraFollow.Snap();
            if (renSpot) renSpot.Intercept = s => { if (!finale && introDone) { s.available = false; StartCoroutine(Finale()); } return true; };
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
            if (Hud.Instance) Hud.Instance.SetObjective("O Ren está na beira da plataforma, olhando os trilhos.");
            GameSave.Save(SceneManager.GetActiveScene().name);
            introDone = true;
        }

        void Update()
        {
            if (!introDone || finale) return;
            if (InputHelper.BackPressed() && !DialogueBox.IsBusy && !Modal.IsOpen) StartCoroutine(Quit());
        }

        IEnumerator Quit()
        {
            finale = true;
            if (player) player.controlEnabled = false;
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 1f);
            SceneManager.LoadScene(menuScene);
        }

        // ------------------------------------------------------------------ o final

        IEnumerator Finale()
        {
            finale = true;
            if (player) player.controlEnabled = false;
            if (Hud.Instance) Hud.Instance.SetObjective("");
            if (souta) { souta.enabled = false; if (souta.rig) souta.rig.facing = 1f; }
            if (renRig) renRig.facing = -1f;   // o Ren se vira para o Kai
            FaceKai(1f);
            yield return SayAndWait(confront);
            if (player) player.controlEnabled = false;

            // a escolha
            int picked = 0;
            bool chosen = false;
            if (choice)
            {
                choice.Show(choiceTitle, choiceOptions, choiceNotes, i => { picked = i; chosen = true; });
                while (!chosen) yield return null;
            }
            if (player) player.controlEnabled = false;

            if (picked == 0) yield return EndingOthers();
            else yield return EndingSelf();
        }

        // ------------------------------------------------------------------ acabar com eles

        IEnumerator EndingOthers()
        {
            yield return SayAndWait(othersStart);
            if (player) player.controlEnabled = false;

            // o empurrão: o Ren cai de costas na vala, e a câmera acompanha até ele bater no chão
            FaceKai(1f);
            yield return Aim(0.85f, 0.15f);                      // os braços empurram
            if (sfx && pushClip) sfx.PlayOneShot(pushClip);
            StartCoroutine(Aim(0f, 0.6f));
            yield return FallIntoPit(renRig);
            yield return new WaitForSeconds(1.4f);
            yield return CameraBack();

            // o Souta entra em pânico e deixa a arma cair
            if (souta && souta.rig) souta.rig.facing = 1f;
            yield return SayAndWait(soutaPanic);
            if (player) player.controlEnabled = false;
            if (souta)
            {
                float kaiX = player ? player.transform.position.x : edgeX - 2f;
                yield return WalkTo(souta, kaiX - 1.5f, 3.2f);
                if (souta.rig) souta.rig.Soltar();
                if (gunOnFloor)
                {
                    gunOnFloor.transform.position = new Vector3(kaiX - 0.8f, floorY + 0.1f, 0f);
                    gunOnFloor.transform.rotation = Quaternion.Euler(0f, 0f, 170f);
                    gunOnFloor.SetActive(true);
                }
                if (sfx && gunDropClip) sfx.PlayOneShot(gunDropClip);
                yield return new WaitForSeconds(0.3f);
                yield return WalkTo(souta, kaiX - 3.4f, 2.4f);   // recua, sem tirar os olhos do Kai
                if (souta.rig) souta.rig.facing = 1f;
            }

            // o Kai pega a arma e estica os braços
            FaceKai(-1f);
            yield return new WaitForSeconds(0.4f);
            if (gunOnFloor) gunOnFloor.SetActive(false);
            if (sfx && cockClip) sfx.PlayOneShot(cockClip);
            yield return Aim(1f, 0.45f);
            yield return SayAndWait(pointGun);
            if (player) player.controlEnabled = false;
            yield return new WaitForSeconds(0.5f);

            // o tiro acerta o Souta
            if (sfx && shotClip) sfx.PlayOneShot(shotClip);
            if (cameraFollow) cameraFollow.Shake(0.14f, 0.35f);
            StartCoroutine(MuzzleFlash());
            if (flash)
            {
                var img = flash.GetComponent<Image>();
                if (img) img.color = new Color(1f, 0.95f, 0.85f, 1f);
                flash.alpha = 0.55f;
                StartCoroutine(FadeGroup(flash, 0f, 0.35f));
            }
            yield return Collapse(souta ? souta.rig : null);
            if (sfx && bodyClip) sfx.PlayOneShot(bodyClip);
            yield return new WaitForSeconds(1.2f);
            if (flash)
            {
                var img = flash.GetComponent<Image>();
                if (img) img.color = new Color(0.5f, 0.02f, 0.02f, 1f);
                StartCoroutine(FadeGroup(flash, 0.22f, 1.5f));
            }
            yield return SayAndWait(afterShot);
            if (player) player.controlEnabled = false;
            yield return Aim(0f, 0.8f);

            // preto, sirene de polícia e o plantão
            if (ambience) StartCoroutine(FadeAudio(ambience, 0.15f, 2f));
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 2.0f);
            yield return Siren(policeClip);
            yield return ShowNews(policeChannel, policeHeadline, policeBody, policeTicker);
            yield return TheEnd(endSubtitleOthers);
        }

        // ------------------------------------------------------------------ acabar comigo

        IEnumerator EndingSelf()
        {
            yield return SayAndWait(selfStart);
            if (player) player.controlEnabled = false;
            FaceKai(1f);

            // o trem vem; o Souta e o Ren gritam; o Kai olha para os trilhos
            if (trainRumble) { trainRumble.loop = false; trainRumble.volume = 0.9f; trainRumble.Play(); }
            // o maquinista vê o Kai e puxa o freio de emergência: o chiado cresce e só para depois do corte
            float brakeEnd = Time.time;
            if (sfx && brakeClip) { sfx.PlayOneShot(brakeClip, 1f); brakeEnd = Time.time + brakeClip.length; }
            StartCoroutine(TrainComes(5.5f));
            if (souta && souta.rig) souta.rig.facing = 1f;
            yield return SayAndWait(selfLast);
            if (player) player.controlEnabled = false;
            yield return new WaitForSeconds(1.2f);

            // corte seco para o preto (nada é mostrado)
            if (Hud.Instance && Hud.Instance.fader) Hud.Instance.fader.alpha = 1f;
            if (darkness) darkness.alpha = 1f;
            if (headlight) headlight.color = new Color(1f, 0.96f, 0.85f, 0f);
            if (ambience) ambience.volume = 0f;
            yield return new WaitForSeconds(1.0f);
            if (trainRumble) StartCoroutine(FadeAudio(trainRumble, 0f, 1.5f));
            yield return new WaitForSeconds(Mathf.Max(2.2f, brakeEnd - Time.time + 0.8f)); // o freio termina no escuro

            yield return Siren(ambulanceClip);
            yield return ShowNews(ambulanceChannel, ambulanceHeadline, ambulanceBody, ambulanceTicker);
            yield return TheEnd(endSubtitleSelf);
        }

        // ------------------------------------------------------------------ peças

        void FaceKai(float dir)
        {
            var sr = player ? player.GetComponent<SpriteRenderer>() : null;
            if (sr) sr.flipX = dir < 0f;
            if (kaiRig) kaiRig.facing = dir;
        }

        /// <summary>Braços do Kai: 0 solta, menos que 1 empurra, 1 pega a arma do chão e aponta.</summary>
        IEnumerator Aim(float to, float duration)
        {
            if (!kaiRig) yield break;
            if (to <= 0f) kaiRig.Soltar();
            else kaiRig.Tocar(to >= 1f ? "aponta" : "empurra", true);
            yield return new WaitForSeconds(duration);
        }

        IEnumerator MuzzleFlash()
        {
            if (!muzzleFlash) yield break;
            muzzleFlash.color = new Color(1f, 0.9f, 0.6f, 1f);
            muzzleFlash.transform.localScale = Vector3.one * 0.5f;
            yield return new WaitForSeconds(0.06f);
            float t = 0f;
            while (t < 0.15f)
            {
                t += Time.deltaTime;
                muzzleFlash.color = new Color(1f, 0.9f, 0.6f, 1f - t / 0.15f);
                yield return null;
            }
            muzzleFlash.color = new Color(1f, 0.9f, 0.6f, 0f);
        }

        /// <summary>O corpo desliza da beira da plataforma, cai girando até o fundo da vala e bate no chão.</summary>
        IEnumerator FallIntoPit(PersonagemPixel rig)
        {
            if (!rig) yield break;
            rig.Tocar("panic", true);
            var tr = rig.transform;
            Transform oldTarget = cameraFollow ? cameraFollow.target : null;
            Rect oldBounds = cameraFollow ? cameraFollow.bounds : new Rect();
            if (cameraFollow)
            {
                cameraFollow.bounds = fallBounds;
                cameraFollow.target = tr;
            }
            restoreTarget = oldTarget;
            restoreBounds = oldBounds;

            float x0 = tr.position.x, y0 = tr.position.y;
            float t = 0f;
            // para fora da plataforma
            while (t < 0.4f)
            {
                t += Time.deltaTime;
                tr.position = new Vector3(Mathf.Lerp(x0, edgeX + 0.35f, t / 0.4f), y0, 0f);
                tr.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(0f, -25f, t / 0.4f));
                yield return null;
            }
            // a queda
            float x = edgeX + 0.35f, y = y0, vy = 0f, rot = -25f;
            float landX = edgeX + 1.6f;
            while (y > pitY + 0.01f)
            {
                vy += gravity.y * 0.9f * Time.deltaTime;
                y = Mathf.Max(pitY, y + vy * Time.deltaTime);
                x = Mathf.MoveTowards(x, landX, 1.4f * Time.deltaTime);
                rot = Mathf.MoveTowards(rot, -100f, 150f * Time.deltaTime);
                tr.position = new Vector3(x, y, 0f);
                tr.rotation = Quaternion.Euler(0f, 0f, rot);
                yield return null;
            }
            // o impacto: bate, quica um pouco e fica
            if (sfx && fallClip) sfx.PlayOneShot(fallClip);
            if (sfx && bodyClip) sfx.PlayOneShot(bodyClip, 0.8f);
            if (cameraFollow) cameraFollow.Shake(0.22f, 0.6f);
            t = 0f;
            while (t < 0.35f)
            {
                t += Time.deltaTime;
                float k = t / 0.35f;
                float hop = Mathf.Sin(k * Mathf.PI) * 0.18f;
                tr.position = new Vector3(x + 0.15f * k, pitY + hop, 0f);
                tr.rotation = Quaternion.Euler(0f, 0f, Mathf.Lerp(-100f, -90f, k));
                yield return null;
            }
            tr.position = new Vector3(x + 0.15f, pitY, 0f);
            tr.rotation = Quaternion.Euler(0f, 0f, -90f);
        }

        Transform restoreTarget;
        Rect restoreBounds;

        IEnumerator CameraBack()
        {
            if (!cameraFollow) yield break;
            cameraFollow.target = restoreTarget ? restoreTarget : (player ? player.transform : null);
            yield return new WaitForSeconds(1.2f);
            cameraFollow.bounds = restoreBounds;
        }

        /// <summary>Baleado, o personagem cai no chão da plataforma.</summary>
        IEnumerator Collapse(PersonagemPixel rig)
        {
            if (!rig) yield break;
            yield return new WaitForSeconds(rig.Tocar("fallen", true));
        }

        IEnumerator TrainComes(float dur)
        {
            float t = 0f;
            bool horned = false;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                if (headlight)
                {
                    headlight.color = new Color(1f, 0.96f, 0.85f, Mathf.Clamp01(k * 1.4f) * 0.85f);
                    headlight.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 2.4f, k * k);
                }
                if (cameraFollow && Time.frameCount % 20 == 0) cameraFollow.Shake(0.015f + 0.05f * k, 0.3f);
                if (!horned && k > 0.5f)
                {
                    horned = true;
                    if (sfx && hornClip) sfx.PlayOneShot(hornClip);
                }
                yield return null;
            }
        }

        IEnumerator Siren(AudioClip clip)
        {
            if (!siren || !clip) yield break;
            siren.clip = clip;
            siren.loop = false;
            siren.volume = 0.9f;
            siren.Play();
            yield return new WaitForSeconds(Mathf.Min(6.5f, clip.length * 0.6f));
        }

        IEnumerator ShowNews(string channel, string headline, string body, string ticker)
        {
            if (!news) yield break;
            bool closed = false;
            news.Show(channel, headline, body, ticker, () => closed = true);
            while (!closed) yield return null;
            if (siren && siren.isPlaying) StartCoroutine(FadeAudio(siren, 0f, 2f));
        }

        IEnumerator TheEnd(string subtitle)
        {
            yield return new WaitForSeconds(0.8f);
            if (Hud.Instance) yield return Hud.Instance.ShowEnd(endTitle, subtitle);
            yield return new WaitForSeconds(5f);
            GameSave.Clear();
            GameFlags.ClearAll();
            if (Application.CanStreamedLevelBeLoaded(menuScene)) SceneManager.LoadScene(menuScene);
        }
    }
}
