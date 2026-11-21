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
    ///  • Acabar com eles: o Kai empurra o Ren, que some no escuro da vala (só se ouve a batida lá embaixo). O Souta aponta
    ///    a arma; os dois brigam por ela (apertar E) até o Kai tomá-la. O tiro: o Souta se joga para trás e fica estendido,
    ///    com muito sangue. Sirene de polícia e o jornal: o Kai foi preso no trabalho por tentativa de massacre.
    ///  • Acabar comigo: o Kai anda até a beira, pula nos trilhos e o trem sai do túnel e passa por cima (som de esmagamento,
    ///    corte para o preto). Sirene de ambulância e o jornal: um trabalhador tirou a própria vida no trabalho; ele tinha
    ///    esquizofrenia e o Souta e o Ren nunca existiram.
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
        [Tooltip("A frente do trem vista de frente (pivô embaixo), que sai do túnel e cresce até tomar a tela.")]
        public Transform trainFront;
        public float trainFrontY = 2.5f;
        public float trainFrontEndY = -2f;

        [Header("Vala e sangue")]
        [Tooltip("O escuro do fundo da vala (quem cai some atrás dele).")]
        public SpriteRenderer trenchDark;
        public SpriteRenderer bloodPool;
        public SpriteRenderer bloodSpatter;

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
        [Tooltip("O corpo batendo no fundo da vala (ouvido, não visto).")]
        public AudioClip pitImpactClip;
        [Tooltip("O trem passando por cima do Kai.")]
        public AudioClip crushClip;
        [Tooltip("A briga pela arma.")]
        public AudioClip struggleClip;

        [Header("Falas")]
        public List<DialogueLine> intro = new List<DialogueLine>();
        public List<DialogueLine> confront = new List<DialogueLine>();
        public List<DialogueLine> othersStart = new List<DialogueLine>();
        public List<DialogueLine> soutaPanic = new List<DialogueLine>();
        public List<DialogueLine> fightLines = new List<DialogueLine>();
        public List<DialogueLine> fightWon = new List<DialogueLine>();
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
        public Sprite policePhoto;
        public string policeCaption = "";

        [Header("Plantão — acabar comigo (ambulância)")]
        public string ambulanceChannel = "JORNAL DA NOITE  ·  PLANTÃO";
        public string ambulanceHeadline = "";
        [TextArea(3, 12)] public string ambulanceBody = "";
        public string ambulanceTicker = "";
        public Sprite ambulancePhoto;
        public string ambulanceCaption = "";

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
            if (trainFront) trainFront.gameObject.SetActive(false);
            if (bloodPool) bloodPool.gameObject.SetActive(false);
            if (bloodSpatter) bloodSpatter.gameObject.SetActive(false);
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
            if (InputHelper.BackPressed() && !DialogueBox.IsBusy && !DiarioKai.IsOpen && !Modal.IsOpen) StartCoroutine(Quit());
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

            // o empurrão: o Ren cai de costas e some no escuro da vala; a câmera fica na plataforma
            FaceKai(1f);
            yield return Aim(0.85f, 0.15f);                      // os braços empurram
            if (sfx && pushClip) sfx.PlayOneShot(pushClip, 0.8f);
            StartCoroutine(Aim(0f, 0.6f));
            yield return FallIntoPit(renRig);
            yield return new WaitForSeconds(1.0f);

            // o Souta aponta a arma para o Kai
            if (souta && souta.rig) { souta.rig.facing = 1f; souta.rig.Tocar("gun", true); }
            yield return SayAndWait(soutaPanic);
            if (player) player.controlEnabled = false;

            // a briga pela arma
            yield return FightForGun();
            yield return SayAndWait(fightWon);
            if (player) player.controlEnabled = false;

            // o Kai empurra o Souta para longe e aponta
            float kaiX = player ? player.transform.position.x : edgeX - 2f;
            if (souta)
            {
                if (souta.rig) souta.rig.Soltar();
                yield return WalkTo(souta, kaiX - 2.6f, 4.5f);
                if (souta.rig) souta.rig.facing = 1f;
            }
            FaceKai(-1f);
            if (sfx && cockClip) sfx.PlayOneShot(cockClip);
            yield return Aim(1f, 0.45f);
            yield return SayAndWait(pointGun);
            if (player) player.controlEnabled = false;
            yield return new WaitForSeconds(0.5f);

            // o tiro: o Souta é jogado para trás e fica estendido no chão, sangrando
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
            yield return SoutaDies();
            yield return new WaitForSeconds(1.4f);
            if (flash)
            {
                var img = flash.GetComponent<Image>();
                if (img) img.color = new Color(0.5f, 0.02f, 0.02f, 1f);
                StartCoroutine(FadeGroup(flash, 0.22f, 1.5f));
            }
            yield return SayAndWait(afterShot);
            if (player) player.controlEnabled = false;
            yield return Aim(0f, 0.8f);

            // preto, sirene de polícia e o jornal
            if (ambience) StartCoroutine(FadeAudio(ambience, 0.15f, 2f));
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 2.0f);
            yield return Siren(policeClip);
            yield return ShowNews(policeChannel, policeHeadline, policeBody, policeTicker, policePhoto, policeCaption);
            yield return TheEnd(endSubtitleOthers);
        }

        // ------------------------------------------------------------------ acabar comigo

        IEnumerator EndingSelf()
        {
            yield return SayAndWait(selfStart);
            if (player) player.controlEnabled = false;

            // o Kai anda até a beira (passa da parede: daqui em diante é cena)
            ScriptKai(true);
            yield return WalkKai(edgeX - 0.3f, 1.6f);
            FaceKai(1f);

            // o trem vem do túnel; o Souta e o Ren gritam
            if (trainRumble) { trainRumble.loop = false; trainRumble.volume = 0.8f; trainRumble.Play(); }
            float brakeEnd = Time.time;
            StartCoroutine(TrainComes(4.5f));
            if (souta && souta.rig) souta.rig.facing = 1f;
            yield return SayAndWait(selfLast);
            if (player) player.controlEnabled = false;
            yield return new WaitForSeconds(0.4f);
            // a câmera fica parada olhando a beira e o túnel (não acompanha a queda)
            if (cameraFollow)
            {
                var olho = new GameObject("OlharDaCamera").transform;
                olho.position = new Vector3((player ? player.transform.position.x : edgeX) + 1.5f, player ? player.transform.position.y : floorY, 0f);
                cameraFollow.target = olho;
            }

            // o pulo: o Kai se joga nos trilhos e some no escuro da vala
            yield return KaiJumps();
            // o maquinista vê e puxa o freio, mas o trem sai do túnel e passa por cima
            if (sfx && brakeClip) { sfx.PlayOneShot(brakeClip, 0.8f); brakeEnd = Time.time + brakeClip.length; }
            yield return TrainRunsOver();

            // preto: o som do trem passando por cima continua no escuro
            if (Hud.Instance && Hud.Instance.fader) Hud.Instance.fader.alpha = 1f;
            if (darkness) darkness.alpha = 1f;
            if (headlight) headlight.color = new Color(1f, 0.96f, 0.85f, 0f);
            if (ambience) ambience.volume = 0f;
            yield return new WaitForSeconds(1.6f);
            if (trainRumble) StartCoroutine(FadeAudio(trainRumble, 0f, 1.5f));
            yield return new WaitForSeconds(Mathf.Max(2.6f, brakeEnd - Time.time + 0.6f));

            yield return Siren(ambulanceClip);
            yield return ShowNews(ambulanceChannel, ambulanceHeadline, ambulanceBody, ambulanceTicker, ambulancePhoto, ambulanceCaption);
            yield return TheEnd(endSubtitleSelf);
        }

        // ------------------------------------------------------------------ a briga pela arma

        /// <summary>O Kai avança no Souta e os dois brigam pela arma. Apertar E (ou Espaço) ganha força; no meio da briga
        /// a arma dispara para o alto. Se o jogador não apertar, o Kai vence sozinho, mais devagar.</summary>
        IEnumerator FightForGun()
        {
            if (!souta || !player) yield break;
            var srig = souta.rig;
            float sx = souta.transform.position.x;
            // o Kai corre para cima dele
            ScriptKai(true);
            FaceKai(-1f);
            if (kaiRig) kaiRig.Tocar("empurra", true);
            yield return WalkKai(sx + 0.95f, 5f, keepPose: true);
            if (sfx && bodyClip) sfx.PlayOneShot(bodyClip, 0.5f);
            if (cameraFollow) cameraFollow.Shake(0.08f, 0.25f);
            Say(fightLines);

            var ui = MashBar("APERTE  E  PARA TOMAR A ARMA");
            float progress = 0f, t = 0f;
            bool fired = false;
            float kaiBase = player.transform.position.x, soutaBase = souta.transform.position.x;
            float nextStruggle = 0f;
            while (progress < 1f)
            {
                t += Time.deltaTime;
                if (!DialogueBox.IsBusy && InputHelper.AdvancePressed()) progress += 0.085f;
                progress += (t > 6f ? 0.18f : 0.015f) * Time.deltaTime;   // sozinho ele também vence, devagar
                progress = Mathf.Clamp01(progress - 0.06f * Time.deltaTime);
                // os dois se empurram para lá e para cá
                float sway = Mathf.Sin(t * 13f) * 0.07f + Mathf.Sin(t * 5.3f) * 0.12f;
                player.transform.position = new Vector3(kaiBase + sway, player.transform.position.y, 0f);
                souta.transform.position = new Vector3(soutaBase + sway, souta.transform.position.y, 0f);
                if (srig) srig.facing = 1f;
                if (kaiRig) kaiRig.facing = -1f;
                if (Time.time >= nextStruggle && sfx && struggleClip) { sfx.PlayOneShot(struggleClip, 0.7f); nextStruggle = Time.time + struggleClip.length * 0.9f; }
                if (Time.frameCount % 15 == 0 && cameraFollow) cameraFollow.Shake(0.02f + 0.03f * progress, 0.2f);
                if (!fired && progress > 0.45f)
                {
                    // a arma dispara no meio da briga
                    fired = true;
                    if (sfx && shotClip) sfx.PlayOneShot(shotClip, 0.9f);
                    if (cameraFollow) cameraFollow.Shake(0.16f, 0.4f);
                    if (flash)
                    {
                        var img = flash.GetComponent<Image>();
                        if (img) img.color = new Color(1f, 0.95f, 0.85f, 1f);
                        flash.alpha = 0.45f;
                        StartCoroutine(FadeGroup(flash, 0f, 0.3f));
                    }
                }
                if (ui.fill) ui.fill.fillAmount = progress;
                yield return null;
            }
            if (ui.root) Destroy(ui.root);
            // o Kai arranca a arma e o Souta é jogado para trás
            if (sfx && gunDropClip) sfx.PlayOneShot(gunDropClip);
            if (sfx && bodyClip) sfx.PlayOneShot(bodyClip, 0.6f);
            if (cameraFollow) cameraFollow.Shake(0.12f, 0.3f);
            if (srig) srig.Soltar();
            if (kaiRig) kaiRig.Tocar("aponta", true);
            float k = 0f;
            while (k < 0.25f)
            {
                k += Time.deltaTime;
                souta.transform.position = new Vector3(soutaBase - 0.9f * (k / 0.25f), souta.transform.position.y, 0f);
                yield return null;
            }
            ScriptKai(false);
        }

        struct Barra { public GameObject root; public Image fill; }

        /// <summary>Barra "aperte E" da briga, criada na hora por cima de tudo.</summary>
        Barra MashBar(string texto)
        {
            var go = new GameObject("BarraDaBriga", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 500;
            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            var fundo = Caixa("Fundo", go.transform, new Vector2(0f, -250f), new Vector2(620f, 22f), new Color(0.05f, 0.05f, 0.06f, 0.85f));
            var cheio = Caixa("Cheio", fundo, Vector2.zero, new Vector2(612f, 14f), new Color(0.75f, 0.12f, 0.1f, 1f));
            cheio.anchorMin = cheio.anchorMax = new Vector2(0.5f, 0.5f);
            cheio.pivot = new Vector2(0.5f, 0.5f);
            var img = cheio.GetComponent<Image>();
            img.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 4, 4), new Vector2(0.5f, 0.5f));
            img.type = Image.Type.Filled;
            img.fillMethod = Image.FillMethod.Horizontal;
            img.fillAmount = 0f;

            var txtGo = new GameObject("Texto", typeof(RectTransform));
            var tr = txtGo.GetComponent<RectTransform>();
            tr.SetParent(go.transform, false);
            tr.anchorMin = tr.anchorMax = new Vector2(0.5f, 1f);
            tr.pivot = new Vector2(0.5f, 1f);
            tr.anchoredPosition = new Vector2(0f, -190f);
            tr.sizeDelta = new Vector2(900f, 50f);
            var label = txtGo.AddComponent<TMPro.TextMeshProUGUI>();
            var modelo = FindAnyObjectByType<TMPro.TMP_Text>();
            if (modelo) label.font = modelo.font;
            label.text = texto;
            label.fontSize = 30;
            label.characterSpacing = 10;
            label.alignment = TMPro.TextAlignmentOptions.Center;
            label.color = new Color(0.95f, 0.92f, 0.86f, 1f);
            return new Barra { root = go, fill = img };
        }

        static RectTransform Caixa(string n, Transform pai, Vector2 pos, Vector2 tam, Color cor)
        {
            var r = new GameObject(n, typeof(RectTransform), typeof(Image)).GetComponent<RectTransform>();
            r.SetParent(pai, false);
            r.anchorMin = r.anchorMax = new Vector2(0.5f, 1f);   // no alto da tela, longe da caixa de diálogo
            r.pivot = new Vector2(0.5f, 1f);
            r.anchoredPosition = pos;
            r.sizeDelta = tam;
            r.GetComponent<Image>().color = cor;
            return r;
        }

        // ------------------------------------------------------------------ morte do Souta

        /// <summary>Baleado, o Souta é jogado para trás, se estatela no chão e fica estendido de costas.
        /// O sangue espirra no chão atrás dele e a poça cresce, grande.</summary>
        IEnumerator SoutaDies()
        {
            var rig = souta ? souta.rig : null;
            if (!rig) yield break;
            var tr = souta.transform;
            float x0 = tr.position.x;
            rig.olhaNaConversa = false;   // morto, não vira mais para quem fala
            if (bloodSpatter)
            {
                bloodSpatter.transform.position = new Vector3(x0 - 1.0f, floorY + 0.06f, 0f);
                bloodSpatter.gameObject.SetActive(true);
            }
            // cambaleia e se joga para trás (para longe do Kai)
            rig.facing = -1f;
            float dur = rig.Tocar("cai", true);
            float t = 0f;
            while (t < dur)
            {
                t += Time.deltaTime;
                tr.position = new Vector3(x0 - 0.7f * Mathf.SmoothStep(0f, 1f, t / dur), tr.position.y, 0f);
                yield return null;
            }
            // estendido de costas: cabeça para longe do Kai
            rig.facing = 1f;
            rig.Tocar("deitado", true);
            float bodyX = tr.position.x;
            if (sfx && bodyClip) sfx.PlayOneShot(bodyClip);
            if (cameraFollow) cameraFollow.Shake(0.12f, 0.3f);
            // um quiquezinho do corpo batendo no chão
            float y0 = tr.position.y;
            t = 0f;
            while (t < 0.22f)
            {
                t += Time.deltaTime;
                tr.position = new Vector3(bodyX, y0 + Mathf.Sin(t / 0.22f * Mathf.PI) * 0.12f, 0f);
                yield return null;
            }
            tr.position = new Vector3(bodyX, y0, 0f);
            // a poça cresce embaixo do tronco
            if (bloodPool)
            {
                bloodPool.transform.position = new Vector3(bodyX - 0.5f, floorY + 0.04f, 0f);
                bloodPool.gameObject.SetActive(true);
                StartCoroutine(GrowPool(bloodPool.transform, 3.2f, 1.3f));
            }
        }

        IEnumerator GrowPool(Transform pool, float duration, float finalScale)
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / duration), 2f);
                pool.localScale = new Vector3(Mathf.Lerp(0.15f, finalScale, k), Mathf.Lerp(0.3f, 1.25f, k), 1f);
                yield return null;
            }
        }

        // ------------------------------------------------------------------ o Kai nos trilhos

        /// <summary>Liga/desliga o Kai "de cena": sem física nem controle, o desenho obedece ao diretor.</summary>
        void ScriptKai(bool on)
        {
            if (!player) return;
            player.controlEnabled = false;
            player.enabled = !on;
            // o corpo físico (com interpolação) reescreveria a posição a cada quadro: desligado, o diretor move o Kai
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb)
            {
                if (!on) rb.position = player.transform.position;
                rb.simulated = !on;
            }
            if (kaiRig)
            {
                kaiRig.mover = on ? null : player;
                kaiRig.flipSource = on ? null : player.GetComponent<SpriteRenderer>();
                kaiRig.externalSpeed = 0f;
            }
        }

        IEnumerator WalkKai(float x, float speed, bool keepPose = false)
        {
            if (!player) yield break;
            var tr = player.transform;
            while (Mathf.Abs(tr.position.x - x) > 0.03f)
            {
                float dir = Mathf.Sign(x - tr.position.x);
                if (kaiRig) { kaiRig.facing = dir; kaiRig.externalSpeed = keepPose ? 0f : speed; }
                tr.position = new Vector3(Mathf.MoveTowards(tr.position.x, x, speed * Time.deltaTime), tr.position.y, 0f);
                yield return null;
            }
            if (kaiRig) kaiRig.externalSpeed = 0f;
        }

        /// <summary>O Kai pula da beira e cai no escuro da vala.</summary>
        IEnumerator KaiJumps()
        {
            if (!player) yield break;
            var tr = player.transform;
            float x0 = tr.position.x, y0 = tr.position.y;
            if (kaiRig) kaiRig.Tocar("salta", true);
            yield return new WaitForSeconds(0.22f);           // o impulso
            float t = 0f, vx = 2.6f, vy = 5.5f, x = x0, y = y0;
            bool caindo = false;
            while (y > y0 - 9f)
            {
                t += Time.deltaTime;
                vy += gravity.y * Time.deltaTime;
                x += vx * Time.deltaTime;
                y += vy * Time.deltaTime;
                tr.position = new Vector3(x, y, 0f);
                if (!caindo && vy < 0f && kaiRig) { caindo = true; kaiRig.Tocar("despenca", true); }
                yield return null;
            }
            if (kaiRig && kaiRig.view) kaiRig.view.enabled = false;
        }

        /// <summary>O trem sai da boca do túnel e cresce até tomar a tela: passa por cima do lugar onde o Kai caiu.</summary>
        IEnumerator TrainRunsOver()
        {
            if (!trainFront) yield break;
            trainFront.gameObject.SetActive(true);
            float dur = 1.25f, t = 0f;
            bool crushed = false;
            Vector3 p0 = trainFront.position;
            while (t < dur)
            {
                t += Time.deltaTime;
                float k = Mathf.Clamp01(t / dur);
                float e = k * k * k;                          // vem de longe e acelera em cima da câmera
                trainFront.localScale = Vector3.one * Mathf.Lerp(0.3f, 4.6f, e);
                trainFront.position = new Vector3(p0.x, Mathf.Lerp(trainFrontY, trainFrontEndY, e), 0f);
                if (cameraFollow && Time.frameCount % 6 == 0) cameraFollow.Shake(0.03f + 0.25f * e, 0.25f);
                if (!crushed && k > 0.78f)
                {
                    crushed = true;
                    if (sfx && crushClip) sfx.PlayOneShot(crushClip, 1f);
                    if (cameraFollow) cameraFollow.Shake(0.5f, 0.8f);
                    if (flash)
                    {
                        var img = flash.GetComponent<Image>();
                        if (img) img.color = new Color(0.55f, 0.02f, 0.02f, 1f);
                        flash.alpha = 0.6f;
                    }
                }
                yield return null;
            }
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

        /// <summary>O Ren escorrega da beira e cai de costas na vala, que é funda: ele some no escuro lá embaixo e,
        /// um instante depois, só se ouve o corpo batendo no fundo.</summary>
        IEnumerator FallIntoPit(PersonagemPixel rig)
        {
            if (!rig) yield break;
            rig.Tocar("panic", true);
            var tr = rig.transform;
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
            // a queda, para dentro do escuro
            float x = edgeX + 0.35f, y = y0, vy = 0f, rot = -25f;
            while (y > y0 - 9f)
            {
                vy += gravity.y * 0.9f * Time.deltaTime;
                y += vy * Time.deltaTime;
                x = Mathf.MoveTowards(x, edgeX + 1.4f, 1.4f * Time.deltaTime);
                rot = Mathf.MoveTowards(rot, -100f, 150f * Time.deltaTime);
                tr.position = new Vector3(x, y, 0f);
                tr.rotation = Quaternion.Euler(0f, 0f, rot);
                yield return null;
            }
            rig.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.35f);          // é fundo: a batida demora a vir
            if (sfx && pitImpactClip) sfx.PlayOneShot(pitImpactClip, 1f);
            else if (sfx && fallClip) sfx.PlayOneShot(fallClip);
            if (cameraFollow) cameraFollow.Shake(0.06f, 0.4f);
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

        IEnumerator ShowNews(string channel, string headline, string body, string ticker, Sprite photo, string caption)
        {
            if (!news) yield break;
            bool closed = false;
            news.Show(channel, headline, body, ticker, photo, caption, () => closed = true);
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
