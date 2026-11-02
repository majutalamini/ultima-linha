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
    /// Cenário 7 — final. O Kai encontra o Souta perto dos trilhos e o empurra: ele cai, desacordado, onde o trem vai passar.
    /// O Ren se assusta; o Kai aponta para o amigo e atira. Recortes de jornal explicam: o Kai tinha esquizofrenia, estava num
    /// centro de reabilitação e o Souta e o Ren eram personalidades dele, não pessoas. Nada é mostrado de forma explícita.
    /// </summary>
    public class Cenario7Director : MonoBehaviour
    {
        public PlayerController player;
        public CameraFollow2D cameraFollow;
        public NpcTalk ren;
        public ObjectSpot soutaSpot;
        public CharacterRig2D soutaRig;
        public GameObject gunOnFloor;
        public GameObject kaiGun;
        public SpriteRenderer headlight;

        [Header("Medidas (unidades)")]
        public float edgeX = 27f;
        public float railY = 0.43f;
        public float floorY = 0.91f;
        public Vector2 gravity = new Vector2(0f, -20f);

        [Header("Interface")]
        public CanvasGroup darkness;
        public CanvasGroup flash;

        public string menuScene = "MainMenu";

        [Header("Som")]
        public AudioSource ambience;
        public AudioSource sfx;
        public AudioSource trainRumble;
        public AudioClip pushClip, fallClip, hornClip, shotClip, paperClip;

        public List<DialogueLine> intro = new List<DialogueLine>();
        public List<DialogueLine> beforePush = new List<DialogueLine>();
        public List<DialogueLine> renPanic = new List<DialogueLine>();
        public List<DialogueLine> pointGun = new List<DialogueLine>();
        public List<DialogueLine> afterNews = new List<DialogueLine>();

        public string[] newsTitles = new string[0];
        [TextArea(3, 12)] public string[] newsBodies = new string[0];
        public string[] newsFooters = new string[0];

        public string endTitle = "ÚLTIMA LINHA";
        public string endSubtitle = "FIM";

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
        }

        void OnDestroy() { Physics2D.gravity = previousGravity; }

        IEnumerator Start()
        {
            GameSave.ContinueRequested = false;
            if (player) player.controlEnabled = false;
            if (cameraFollow) cameraFollow.Snap();
            if (soutaSpot) soutaSpot.Intercept = s => { if (!finale && introDone) { s.available = false; StartCoroutine(Finale()); } return true; };
            if (ambience)
            {
                ambience.loop = true; ambience.volume = 0f; ambience.Play();
                StartCoroutine(FadeAudio(ambience, 0.5f, 3f));
            }
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, 2.0f);
            yield return new WaitForSeconds(0.5f);
            yield return SayAndWait(intro);
            if (player) player.controlEnabled = true;
            if (Hud.Instance) Hud.Instance.SetObjective("O Souta está parado perto dos trilhos.");
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

        void FaceKai(float dir)
        {
            var sr = player ? player.GetComponent<SpriteRenderer>() : null;
            if (sr) sr.flipX = dir < 0f;
            if (kaiGun)
            {
                kaiGun.transform.localPosition = new Vector3(0.62f * dir, -0.2f, 0f);
                kaiGun.transform.localScale = new Vector3(0.55f * dir, 0.55f, 1f);
            }
        }

        IEnumerator Finale()
        {
            finale = true;
            if (player) player.controlEnabled = false;
            if (Hud.Instance) Hud.Instance.SetObjective("");
            if (ren) ren.lookAt = null;
            FaceKai(1f);
            yield return SayAndWait(beforePush);

            // o empurrão
            if (sfx && pushClip) sfx.PlayOneShot(pushClip);
            if (gunOnFloor)
            {
                gunOnFloor.transform.position = new Vector3(soutaRig.transform.position.x + 0.15f, floorY + 0.12f, 0f);
                gunOnFloor.SetActive(true);
            }
            yield return Fall();
            if (cameraFollow) cameraFollow.Shake(0.1f, 0.5f);
            yield return new WaitForSeconds(1.0f);

            // o trem vem; o Ren entra em pânico
            if (trainRumble)
            {
                trainRumble.loop = false; trainRumble.volume = 0.9f; trainRumble.Play();
            }
            StartCoroutine(TrainComes());
            if (ren && ren.rig) ren.rig.facing = 1f;
            yield return SayAndWait(renPanic);
            if (ren) yield return WalkTo(ren, ren.transform.position.x - 1.2f, 2.6f);
            if (ren && ren.rig) ren.rig.facing = 1f;

            // o Kai pega a arma e aponta para o amigo
            if (gunOnFloor) gunOnFloor.SetActive(false);
            if (kaiGun) kaiGun.SetActive(true);
            FaceKai(-1f);
            yield return new WaitForSeconds(0.6f);
            yield return SayAndWait(pointGun);

            // o tiro: clarão branco e corte para o preto (nada é mostrado)
            if (sfx && shotClip) sfx.PlayOneShot(shotClip);
            if (flash)
            {
                var img = flash.GetComponent<Image>();
                if (img) img.color = Color.white;
                flash.alpha = 1f;
            }
            if (ambience) ambience.volume = 0f;
            if (trainRumble) trainRumble.Stop();
            if (headlight) headlight.color = new Color(1f, 0.96f, 0.85f, 0f);
            if (darkness) darkness.alpha = 1f;
            yield return new WaitForSeconds(0.12f);
            yield return FadeGroup(flash, 0f, 1.4f);
            yield return new WaitForSeconds(2.2f);

            // os recortes
            for (int i = 0; i < newsBodies.Length; i++)
            {
                if (sfx && paperClip) sfx.PlayOneShot(paperClip);
                bool closed = false;
                if (DocumentView.Instance)
                {
                    DocumentView.Instance.Show(newsTitles[i], newsBodies[i], newsFooters[i], () => closed = true);
                    while (!closed) yield return null;
                }
                yield return new WaitForSeconds(0.5f);
            }
            yield return SayAndWait(afterNews);
            yield return new WaitForSeconds(0.8f);
            if (Hud.Instance) yield return Hud.Instance.ShowEnd(endTitle, endSubtitle);
            yield return new WaitForSeconds(4.5f);
            GameSave.Clear();
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 1.5f);
            if (Application.CanStreamedLevelBeLoaded(menuScene)) SceneManager.LoadScene(menuScene);
        }

        IEnumerator Fall()
        {
            var tr = soutaRig.transform;
            float x0 = tr.position.x, y0 = tr.position.y;
            float endX = x0 + (edgeX - x0) + 1.3f;
            soutaRig.facing = 1f;
            float t = 0f, vy = 0f, y = y0;
            // desliza para fora da plataforma
            while (t < 0.45f)
            {
                t += Time.deltaTime;
                soutaRig.externalSpeed = 0f;
                tr.position = new Vector3(Mathf.Lerp(x0, edgeX + 0.2f, t / 0.45f), y0, 0f);
                yield return null;
            }
            // cai nos trilhos
            float x = edgeX + 0.2f, rot = 0f;
            while (y > railY + 0.02f)
            {
                vy += gravity.y * Time.deltaTime;
                y = Mathf.Max(railY, y + vy * Time.deltaTime);
                x = Mathf.MoveTowards(x, endX, 1.6f * Time.deltaTime);
                rot = Mathf.MoveTowards(rot, -90f, 260f * Time.deltaTime);
                tr.position = new Vector3(x, y, 0f);
                tr.rotation = Quaternion.Euler(0f, 0f, rot);
                yield return null;
            }
            tr.rotation = Quaternion.Euler(0f, 0f, -90f);
            tr.position = new Vector3(x, railY, 0f);
            if (sfx && fallClip) sfx.PlayOneShot(fallClip);
        }

        IEnumerator TrainComes()
        {
            float t = 0f, dur = 6f;
            bool horned = false;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = t / dur;
                if (headlight)
                {
                    headlight.color = new Color(1f, 0.96f, 0.85f, Mathf.Clamp01(k * 1.4f) * 0.85f);
                    headlight.transform.localScale = Vector3.one * Mathf.Lerp(0.3f, 2.2f, k * k);
                }
                if (cameraFollow && Time.frameCount % 20 == 0) cameraFollow.Shake(0.015f + 0.05f * k, 0.3f);
                if (!horned && k > 0.55f)
                {
                    horned = true;
                    if (sfx && hornClip) sfx.PlayOneShot(hornClip);
                }
                yield return null;
            }
        }
    }
}
