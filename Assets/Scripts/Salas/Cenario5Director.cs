using System.Collections;
using System.Collections.Generic;
using Platformer.Mechanics;
using UltimaLinha.Cenario1;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace UltimaLinha.Salas
{
    /// <summary>
    /// Cenário 5 — o escritório abandonado e o corpo do chefe (clímax).
    /// 1. O Kai examina o corpo e acha, no bolso, a alavanca que falta e um recorte de jornal: foi ele quem matou o chefe.
    /// 2. Souta e Ren leem, entram em pânico, pegam uma arma na mesa do chefe, apontam para o Kai e o trancam no escritório.
    /// 3. O Kai vasculha os arquivos atrás da chave. Ela está no arquivo "DEMISSÕES". O bilhete na mesa do chefe diz onde
    ///    (depois de lido, o arquivo certo brilha). Aberto o arquivo, a chave aparece na gaveta e o Kai a pega com E.
    /// </summary>
    public class Cenario5Director : MonoBehaviour
    {
        public PlayerController player;
        public CameraFollow2D cameraFollow;
        public NpcTalk souta;
        public NpcTalk ren;

        [Header("Lugares")]
        public ObjectSpot body;
        public ObjectSpot bossDesk;
        public ObjectSpot[] cabinets = new ObjectSpot[0];
        [Tooltip("Id do arquivo onde está a chave.")]
        public string keyCabinetId = "arquivoC";
        [Tooltip("A chave na gaveta aberta (aparece quando o Kai abre o arquivo certo; E pega).")]
        public ObjectSpot keyPickup;
        [Tooltip("Brilho fraco no arquivo certo, depois que o Kai lê o memorando da mesa.")]
        public SpriteRenderer cabinetGlow;
        [Tooltip("Brilho em cima da chave, enquanto ela está na gaveta.")]
        public SpriteRenderer keyGlow;
        public ExitDoor door;

        [Header("Pontos do cenário (x em unidades)")]
        public float deskX = 17f;
        public float doorX = 32.5f;
        public float kaiBodyX = 22f;

        [Header("Física desta cena")]
        public Vector2 gravity = new Vector2(0f, -20f);

        [Header("Cenas")]
        public string nextScene = "Cenario6";
        public string menuScene = "MainMenu";

        [Header("Luz")]
        [Tooltip("Vermelho que cobre a tela durante a briga.")]
        public CanvasGroup redTint;
        public float tintAlpha = 0.28f;

        [Header("Som")]
        public AudioSource ambience;
        public float ambienceVolume = 0.5f;
        public AudioSource heartbeat;
        public AudioSource sfx;
        public AudioClip gunTakeClip;
        public AudioClip gunCockClip;
        public AudioClip doorSlamClip;
        public AudioClip doorLockClip;
        public AudioClip knockClip;
        public AudioClip keyClip;
        public AudioClip leverClip;

        [Header("Objetivos")]
        public string objectiveBody = "Examine o corpo.";
        public string objectiveKey = "Ache a chave da porta. Vasculhe o escritório.";
        public string objectiveTakeKey = "Pegue a chave na gaveta do arquivo.";
        public string objectiveDoor = "Abra a porta do escritório.";

        [Header("Falas")]
        public List<DialogueLine> intro = new List<DialogueLine>();
        public List<DialogueLine> doorBeforeBody = new List<DialogueLine>();
        public List<DialogueLine> confront1 = new List<DialogueLine>();
        public List<DialogueLine> confront2 = new List<DialogueLine>();
        public List<DialogueLine> confront3 = new List<DialogueLine>();
        public List<DialogueLine> afterLock = new List<DialogueLine>();
        public List<DialogueLine> renOutside = new List<DialogueLine>();
        public List<DialogueLine> cabinetHint = new List<DialogueLine>();
        public List<DialogueLine> doorNoKey = new List<DialogueLine>();
        public List<DialogueLine> keyFound = new List<DialogueLine>();
        public List<DialogueLine> doorUnlocked = new List<DialogueLine>();

        [Header("Fim do cenário (enquanto o Cenário 6 não existe)")]
        public string endTitle = "FIM DO CENÁRIO 5";
        public string endSubtitle = "A estação ainda espera pela alavanca.";

        Vector2 previousGravity;
        bool introDone, leaving, confronted, locked, hasKey, keyRevealed, memoRead, hintGiven, renSpoke, running, cutscene;
        int wrongSearches;

        void Awake()
        {
            previousGravity = Physics2D.gravity;
            Physics2D.gravity = gravity;
            Time.timeScale = 1f;
            Modal.Reset();
            if (redTint) redTint.alpha = 0f;
            if (keyPickup) { keyPickup.available = false; keyPickup.gameObject.SetActive(false); }
            if (cabinetGlow) cabinetGlow.enabled = false;
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

            if (body)
            {
                body.Intercept = OnBodyIntercept;
                body.Presented += OnBodyPresented;
            }
            if (bossDesk) { bossDesk.available = false; bossDesk.Presented += OnDeskRead; }
            foreach (var c in cabinets)
                if (c) { c.available = false; c.Presented += OnSearched; }
            if (keyPickup) keyPickup.Intercept = OnKeyPickup;
            if (door)
            {
                door.onLockedInteract = OnDoorLocked;
                door.onEnter = () => StartCoroutine(Leave(nextScene));
            }

            // jogo continuado: se a briga já aconteceu, o Kai volta trancado no escritório, com o que já tinha achado
            if (continuing)
            {
                if (GameSave.Has("confronto")) RestoreLocked();
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

            if (Hud.Instance) yield return Hud.Instance.FadeScreen(0f, continuing ? 1.2f : 2.2f);
            yield return new WaitForSeconds(continuing ? 0.2f : 0.7f);
            if (!continuing) yield return SayAndWait(intro);
            else if (Hud.Instance) Hud.Instance.Toast("Jogo carregado.");

            if (player) player.controlEnabled = true;
            if (Hud.Instance) Hud.Instance.SetObjective(CurrentObjective());
            introDone = true;
        }

        string CurrentObjective()
        {
            if (!confronted) return objectiveBody;
            if (hasKey) return objectiveDoor;
            if (keyRevealed) return objectiveTakeKey;
            return objectiveKey;
        }

        /// <summary>Volta ao momento depois da briga: Souta e Ren já saíram e a porta está trancada.</summary>
        void RestoreLocked()
        {
            confronted = true;
            locked = true;
            if (souta) souta.gameObject.SetActive(false);
            if (ren) ren.gameObject.SetActive(false);
            if (redTint) redTint.alpha = 0.1f;
            if (!GameFlags.Get(GameFlags.MissingLever)) GameFlags.Set(GameFlags.MissingLever);
            renSpoke = GameSave.Has("ren_bateu");
            memoRead = GameSave.Has("memorando");
            if (bossDesk) bossDesk.available = true;
            foreach (var c in cabinets)
            {
                if (!c) continue;
                c.available = true;
                if (GameSave.Has("arquivo_" + c.id) && c.openVisual) c.openVisual.SetActive(true);
                if (c.id == keyCabinetId && GameSave.Has("arquivo_" + c.id)) c.available = false;
            }
            if (!hasKey && GameSave.Has("arquivo_" + keyCabinetId)) ShowKey();
        }

        void Update()
        {
            if (cabinetGlow)
            {
                bool show = memoRead && locked && !keyRevealed && !hasKey;
                if (cabinetGlow.enabled != show) cabinetGlow.enabled = show;
                if (show) { var c = cabinetGlow.color; c.a = 0.2f + 0.14f * Mathf.Sin(Time.time * 2.4f); cabinetGlow.color = c; }
            }
            if (keyGlow && keyGlow.gameObject.activeInHierarchy)
            {
                var c = keyGlow.color; c.a = 0.3f + 0.25f * Mathf.Sin(Time.time * 4f); keyGlow.color = c;
            }

            if (!introDone || leaving || running) return;
            if (InputHelper.BackPressed() && !DialogueBox.IsBusy && !Modal.IsOpen)
                StartCoroutine(Leave(menuScene));
        }

        // ---------- O corpo ----------

        bool OnBodyIntercept(ObjectSpot spot)
        {
            if (spot.Uses == 0)
            {
                GameFlags.Set(GameFlags.MissingLever);
                GameFlags.Set(GameFlags.NewsRead);
                if (sfx && leverClip) sfx.PlayOneShot(leverClip, 0.8f);
                if (Hud.Instance) Hud.Instance.Toast("Alavanca encontrada no bolso do chefe.");
            }
            return false; // segue com o documento e as falas
        }

        void OnBodyPresented(ObjectSpot spot, bool firstTime)
        {
            if (firstTime && !confronted) StartCoroutine(Confrontation());
        }

        IEnumerator Confrontation()
        {
            confronted = true;
            running = true;
            cutscene = true;
            if (player) player.controlEnabled = false;
            if (Hud.Instance) Hud.Instance.SetObjective("");
            if (souta) { souta.enabled = false; souta.lookAt = null; }
            if (ren) { ren.enabled = false; ren.lookAt = null; }
            float kaiX = player ? player.transform.position.x : kaiBodyX;

            // eles se aproximam e leem o recorte
            StartCoroutine(Tint(tintAlpha, 3f));
            if (heartbeat)
            {
                heartbeat.loop = true;
                heartbeat.volume = 0f;
                heartbeat.Play();
                StartCoroutine(FadeAudio(heartbeat, 0.55f, 3f));
            }
            var a = StartCoroutine(Walk(souta, kaiX - 2.0f, 3.1f));
            yield return Walk(ren, kaiX - 3.4f, 2.8f);
            yield return a;
            if (ren && ren.rig) ren.rig.Tocar("panic", true);
            yield return SayAndWait(confront1);

            // o Souta vai até a mesa do chefe e pega a arma
            yield return Walk(souta, deskX, 3.0f);
            if (sfx && gunTakeClip) sfx.PlayOneShot(gunTakeClip);
            if (souta && souta.rig) souta.rig.Tocar("gun", true);
            yield return new WaitForSeconds(0.7f);
            if (sfx && gunCockClip) sfx.PlayOneShot(gunCockClip);
            if (cameraFollow) cameraFollow.Shake(0.05f, 0.4f);
            yield return new WaitForSeconds(0.8f);
            yield return Walk(souta, kaiX - 2.0f, 3.0f);
            FaceRight(souta);
            if (souta && souta.rig) souta.rig.Tocar("gun", true);
            yield return SayAndWait(confront2);

            // saem e trancam a porta
            yield return SayAndWait(confront3);
            var b = StartCoroutine(Walk(ren, doorX - 0.6f, 3.2f));
            yield return Walk(souta, doorX - 0.2f, 2.6f);
            yield return b;
            if (ren) ren.gameObject.SetActive(false);
            yield return new WaitForSeconds(0.2f);
            if (souta) souta.gameObject.SetActive(false);
            if (sfx && doorSlamClip) sfx.PlayOneShot(doorSlamClip);
            if (cameraFollow) cameraFollow.Shake(0.16f, 0.6f);
            yield return new WaitForSeconds(0.9f);
            if (sfx && doorLockClip) sfx.PlayOneShot(doorLockClip);
            yield return new WaitForSeconds(1.2f);

            if (heartbeat) StartCoroutine(FadeAudio(heartbeat, 0f, 3f));
            StartCoroutine(Tint(0.1f, 3f));
            yield return SayAndWait(afterLock);

            locked = true;
            running = false;
            cutscene = false;
            if (bossDesk) bossDesk.available = true;
            foreach (var c in cabinets)
                if (c) c.available = true;
            if (player) player.controlEnabled = true;
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveKey);
            GameSave.Mark("confronto");
        }

        // ---------- Procurar a chave ----------

        void OnDeskRead(ObjectSpot spot, bool firstTime)
        {
            if (!locked || memoRead) return;
            memoRead = true;               // o arquivo de DEMISSÕES passa a brilhar
            GameSave.Mark("memorando");
        }

        void OnSearched(ObjectSpot spot, bool firstTime)
        {
            if (!locked) return;
            if (firstTime) GameSave.Mark("arquivo_" + spot.id);
            if (spot.id == keyCabinetId)
            {
                if (!hasKey && !keyRevealed) StartCoroutine(RevealKey(spot));
                return;
            }
            if (!firstTime) return;
            wrongSearches++;
            if (!renSpoke)
            {
                renSpoke = true;
                GameSave.Mark("ren_bateu");
                StartCoroutine(RenKnocks());
            }
            else if (!hintGiven && wrongSearches >= 2 && !hasKey)
            {
                hintGiven = true;
                memoRead = true;           // a dica também acende o arquivo certo
                Say(cabinetHint);
            }
        }

        IEnumerator RenKnocks()
        {
            yield return new WaitForSeconds(0.4f);
            if (sfx && knockClip) sfx.PlayOneShot(knockClip, 0.8f);
            yield return new WaitForSeconds(0.9f);
            yield return SayAndWait(renOutside);
        }

        /// <summary>O arquivo certo foi aberto: a chave aparece na gaveta, brilhando, para o Kai pegar.</summary>
        IEnumerator RevealKey(ObjectSpot cabinet)
        {
            cabinet.available = false;     // o E passa para a chave
            ShowKey();
            if (sfx && keyClip) sfx.PlayOneShot(keyClip, 0.45f);
            yield return null;
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveTakeKey);
        }

        void ShowKey()
        {
            keyRevealed = true;
            if (keyPickup)
            {
                keyPickup.gameObject.SetActive(true);
                keyPickup.available = true;
            }
        }

        bool OnKeyPickup(ObjectSpot spot)
        {
            if (hasKey) return true;
            spot.available = false;
            spot.gameObject.SetActive(false);
            StartCoroutine(FoundKey());
            return true;
        }

        IEnumerator FoundKey()
        {
            hasKey = true;
            GameFlags.Set(GameFlags.Cenario5Key);
            GameSave.Mark("chave");
            if (sfx && keyClip) sfx.PlayOneShot(keyClip);
            if (Hud.Instance) Hud.Instance.Toast("Você pegou a chave.");
            yield return new WaitForSeconds(0.3f);
            yield return SayAndWait(keyFound);
            if (Hud.Instance) Hud.Instance.SetObjective(objectiveDoor);
        }

        // ---------- Porta ----------

        void OnDoorLocked()
        {
            if (!confronted)
            {
                Say(doorBeforeBody);
                return;
            }
            if (!hasKey)
            {
                door.FlashLocked();
                if (sfx && doorLockClip) sfx.PlayOneShot(doorLockClip, 0.5f);
                Say(doorNoKey);
                return;
            }
            StartCoroutine(UnlockDoor());
        }

        IEnumerator UnlockDoor()
        {
            door.Unlock();
            yield return new WaitForSeconds(0.2f);
            yield return SayAndWait(doorUnlocked);
            if (Hud.Instance) Hud.Instance.SetObjective("");
        }

        // ---------- Movimento dos NPCs ----------

        IEnumerator Walk(NpcTalk npc, float x, float speed)
        {
            if (!npc) yield break;
            var rig = npc.rig;
            while (Mathf.Abs(npc.transform.position.x - x) > 0.05f)
            {
                float dir = Mathf.Sign(x - npc.transform.position.x);
                if (rig)
                {
                    rig.facing = dir;
                    rig.externalSpeed = speed;
                }
                var p = npc.transform.position;
                p.x = Mathf.MoveTowards(p.x, x, speed * Time.deltaTime);
                npc.transform.position = p;
                yield return null;
            }
            if (rig) rig.externalSpeed = 0f;
        }

        static void FaceRight(NpcTalk npc)
        {
            if (npc && npc.rig) npc.rig.facing = 1f;
        }

        // ---------- Utilidades ----------

        IEnumerator Tint(float to, float duration)
        {
            if (!redTint) yield break;
            float from = redTint.alpha, t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                redTint.alpha = Mathf.Lerp(from, to, t / duration);
                yield return null;
            }
            redTint.alpha = to;
        }

        IEnumerator SayAndWait(List<DialogueLine> lines)
        {
            if (!DialogueBox.Instance || lines == null || lines.Count == 0) yield break;
            bool done = false;
            DialogueBox.Instance.Play(lines, () => done = true);
            while (!done) yield return null;
            if (cutscene && player) player.controlEnabled = false; // o diálogo devolve o controle: na cutscene ele fica travado
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
            if (Hud.Instance) yield return Hud.Instance.FadeScreen(1f, 1.4f);

            if (!Application.CanStreamedLevelBeLoaded(scene))
            {
                if (Hud.Instance) yield return Hud.Instance.ShowEnd(endTitle, endSubtitle);
                yield return new WaitForSeconds(3.5f);
                scene = menuScene;
            }
            if (Application.CanStreamedLevelBeLoaded(scene)) SceneManager.LoadScene(scene);
            else Debug.LogWarning($"[Cenário 5] A cena \"{scene}\" não está na lista de cenas do build.");
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
