using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UltimaLinha.Cenario1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Reconstrói os 7 cenários com os personagens em pixel art e os roda em Play, sem som: cada cena fica alguns
    /// segundos rodando, os personagens passam pelos clipes e sai uma foto de cada em Ferramentas/PixelArt/previa.
    /// Por linha de comando: -executeMethod UltimaLinha.EditorTools.PixelArtTeste.SoRodar (com janela, sem -batchmode;
    /// a Unity fecha sozinha no fim e o resultado fica no log).
    /// </summary>
    [InitializeOnLoad]
    public static class PixelArtTeste
    {
        const string Previa = "Ferramentas/PixelArt/previa";
        const string Fila = "UltimaLinha.TesteFila";
        const string Fase = "UltimaLinha.TesteFase";
        const string Erros = "UltimaLinha.TesteErros";
        const float SegundosPorCena = 4f;

        static double inicio = -1;
        static readonly HashSet<string> FotosAbertura = new HashSet<string>();
        // o teste abre cenários que salvam o progresso: guarda o "Continuar" do jogador e devolve no fim
        static readonly string[] ChavesSave = { "UltimaLinha.Save.Cena", "UltimaLinha.Save.Progresso", "UltimaLinha.Save.Posicao", "UltimaLinha.Save.Quando" };
        const string Backup = "UltimaLinha.TesteBackup.";
        static int etapaPulo;
        static double marcaPulo;
        static int quedasAntes;
        static float alcance, velocidade;
        static bool fotoVao;
        static bool fotografada;

        static PixelArtTeste()
        {
            Application.logMessageReceived += (msg, stack, tipo) =>
            {
                if (SessionState.GetString(Fila, "") == "") return;
                if (tipo == LogType.Exception || tipo == LogType.Error)
                    SessionState.SetInt(Erros, SessionState.GetInt(Erros, 0) + 1);
            };
            EditorApplication.update += Passo;
        }

        [MenuItem("Última Linha/Testes/Reconstruir os 7 cenários")]
        public static void Reconstruir()
        {
            for (int i = 0; i <= 7; i++)
            {
                // direto no Build: os menus pedem confirmação, e em batch a caixa de diálogo cancela
                var tipo = typeof(PixelArtTeste).Assembly.GetType($"UltimaLinha.EditorTools.Cenario{i}SceneBuilder");
                var build = tipo.GetMethod("Build", BindingFlags.NonPublic | BindingFlags.Static);
                if (!(bool)build.Invoke(null, new object[] { false }))
                {
                    Debug.LogError($"[PixelArt] Cenário {i} não foi reconstruído");
                    continue;
                }
                // marca a versão como a reconstrução automática faria, para ela não repetir o trabalho
                var chave = (string)tipo.GetProperty("VersionKey", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
                EditorPrefs.SetInt(chave, (int)tipo.GetField("BuildVersion", BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue());
            }
            AssetDatabase.SaveAssets();
        }

        public static void ReconstruirERodar()
        {
            Reconstruir();
            SoRodar();
        }

        [MenuItem("Última Linha/Testes/Rodar os 7 cenários sem som")]
        public static void SoRodar()
        {
            Application.runInBackground = true; // sem foco a Unity desacelera o Play e o teste não anda
            EditorUtility.audioMasterMute = true; // o teste não pode tocar o som dos cenários no computador de ninguém
            SessionState.SetInt(Erros, 0);
            SessionState.SetString(Fase, "abrir");
            FotosAbertura.Clear();
            etapaPulo = 0;
            foreach (var k in ChavesSave)
                SessionState.SetString(Backup + k, PlayerPrefs.HasKey(k) ? "1" + PlayerPrefs.GetString(k) : "0");
            PlayerPrefs.SetInt(Cenario0Director.ChaveTeste, 1); // o diário espera tecla; o resto da abertura roda sozinho
            SessionState.SetString(Fila, "2,1,0,3,4,5,6,7"); // o primeiro Play recarrega os scripts e corta corrotinas: nem a Abertura nem a entrada do Cenário 1 podem ser a primeira
        }

        static void Passo()
        {
            var fila = SessionState.GetString(Fila, "");
            if (fila == "") return;
            var partes = fila.Split(',');
            var fase = SessionState.GetString(Fase, "abrir");

            if (fase == "abrir" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                EditorSceneManager.OpenScene($"Assets/Scenes/Cenario{partes[0]}.unity", OpenSceneMode.Single);
                inicio = -1;
                fotografada = false;
                etapaC1 = 0;
                FotosC1.Clear();
                SessionState.SetString(Fase, "jogando");
                EditorApplication.isPlaying = true;
            }
            else if (fase == "jogando" && EditorApplication.isPlaying)
            {
                if (inicio < 0) inicio = EditorApplication.timeSinceStartup;
                double t = EditorApplication.timeSinceStartup - inicio;
                if (partes[0] == "1")
                {
                    Cenario1(t);
                    if (t < 60.0 && etapaC1 < 99) return;
                }
                if (!fotografada && t > SegundosPorCena * 0.5 && partes[0] != "0")
                {
                    fotografada = true;
                    Exercita(partes[0]);
                }
                // a abertura é longa: fotos no trem cheio, nas piscadas e já no tutorial
                if (partes[0] == "0")
                {
                    foreach (var (seg, nome) in new[] { (4.0, "a"), (13.5, "b"), (27.0, "c") })
                        if (t > seg && !FotosAbertura.Contains(nome))
                        {
                            FotosAbertura.Add(nome);
                            if (Camera.main) Foto(Camera.main, $"jogo_cenario0_{nome}.png");
                            Estado(nome);
                        }
                    if (t > 27.5) Pulo(t);
                    if (t < 46.0) return;
                }
                if (t > SegundosPorCena)
                {
                    SessionState.SetString(Fase, "saindo");
                    EditorApplication.isPlaying = false;
                }
            }
            else if (fase == "saindo" && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                var resto = new List<string>(partes);
                resto.RemoveAt(0);
                if (resto.Count > 0)
                {
                    SessionState.SetString(Fila, string.Join(",", resto));
                    SessionState.SetString(Fase, "abrir");
                    return;
                }
                SessionState.SetString(Fila, "");
                PlayerPrefs.DeleteKey(Cenario0Director.ChaveTeste);
                foreach (var k in ChavesSave)
                {
                    var v = SessionState.GetString(Backup + k, "0");
                    if (v.StartsWith("1")) PlayerPrefs.SetString(k, v.Substring(1));
                    else PlayerPrefs.DeleteKey(k);
                }
                PlayerPrefs.Save();
                EditorUtility.audioMasterMute = false;
                int erros = SessionState.GetInt(Erros, 0);
                Debug.Log($"[PixelArt] TESTE CONCLUIDO, erros em execução: {erros}");
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-executeMethod") >= 0) EditorApplication.Exit(erros == 0 ? 0 : 1);
            }
        }

        /// <summary>
        /// Pulo no vão com o teclado simulado: primeiro andando (tem que cair), depois correndo (tem que passar).
        /// </summary>
        static void Pulo(double t)
        {
            var d = UnityEngine.Object.FindFirstObjectByType<Cenario0Director>();
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!d)
            {
                if (etapaPulo > 0 && etapaPulo < 9) { Debug.Log("[PixelArt] Pulo correndo: passou, carregou o Cenário 1"); etapaPulo = 9; }
                if (etapaPulo == 9 && Camera.main && UnityEngine.Object.FindFirstObjectByType<Cenario1Director>())
                {
                    etapaPulo = 10;
                    Foto(Camera.main, "jogo_cenario1_comeco.png"); // tem que ser o mesmo quadro de jogo_cenario0_chegada.png
                    Debug.Log($"[PixelArt] Cenário 1 entrou sem corte: câmera x={Camera.main.transform.position.x:0.00}, fade={(Hud.Instance && Hud.Instance.fader ? Hud.Instance.fader.alpha : -1f):0.00}");
                }
                InputSystem.QueueStateEvent(kb, new KeyboardState());
                return;
            }
            var p = d.player;
            bool saindo = (bool)typeof(Cenario0Director).GetField("leaving", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(d);
            if (saindo && etapaPulo < 9 && Mathf.Abs(p.velocity.x) < 0.01f && Mathf.Abs(p.transform.position.x - d.descansoX) < 0.3f && Camera.main)
            {
                etapaPulo = 9;
                Foto(Camera.main, "jogo_cenario0_chegada.png");
                Debug.Log($"[PixelArt] Abertura chegou do outro lado: kai x={p.transform.position.x:0.00} (descanso {d.descansoX:0.00}), câmera x={Camera.main.transform.position.x:0.00}");
            }
            var campo = typeof(Cenario0Director).GetField("quedas", BindingFlags.NonPublic | BindingFlags.Instance);
            int quedas = (int)campo.GetValue(d);
            float borda = d.bordaVao - 0.5f;
            switch (etapaPulo)
            {
                case 0:
                    typeof(Cenario0Director).GetField("noVao", BindingFlags.NonPublic | BindingFlags.Instance).SetValue(d, true);
                    if (d.door) d.door.AbertaJa(); // como se tivesse usado o painel: a última foto tem que bater com o Cenário 1
                    p.Teleport(d.vaoInicio);
                    if (d.cameraFollow) d.cameraFollow.Snap();
                    quedasAntes = quedas;
                    etapaPulo = 1;
                    break;
                case 1: case 3:
                    if (etapaPulo == 1 && !fotoVao && Camera.main)
                    {
                        // a câmera vai até a emenda entre o fim do vagão e o vão só para a foto
                        fotoVao = true;
                        var c = Camera.main.transform.position;
                        Camera.main.transform.position = new Vector3(d.inicioVao + 1f, c.y, c.z);
                        Foto(Camera.main, "jogo_cenario0_vao.png");
                        Camera.main.transform.position = c;
                    }
                    if (DialogueBox.IsBusy) { Aperta(kb, (marcaPulo = t) > 0 && Time.frameCount % 20 == 0 ? Key.Space : Key.None); return; }
                    p.controlEnabled = true;
                    if (p.transform.position.x < borda) { Aperta(kb, Key.D, etapaPulo == 3 ? Key.LeftShift : Key.None); return; }
                    marcaPulo = t;
                    etapaPulo++;
                    break;
                case 2: case 4:
                    bool correndo = etapaPulo == 4;
                    if (t - marcaPulo < 0.35) Aperta(kb, Key.D, Key.Space, correndo ? Key.LeftShift : Key.None);
                    else Aperta(kb, Key.D, correndo ? Key.LeftShift : Key.None);
                    alcance = Mathf.Max(alcance, p.transform.position.x);
                    velocidade = Mathf.Max(velocidade, Mathf.Abs(p.velocity.x));
                    if (quedas > quedasAntes)
                    {
                        Debug.Log($"[PixelArt] Pulo {(correndo ? "correndo" : "andando")}: caiu no vão (chegou a x={alcance:0.0}, " +
                                  $"borda {d.bordaVao:0.0}, vel {velocidade:0.0}, corrida x{p.runMultiplier:0.00})");
                        alcance = velocidade = 0f;
                        quedasAntes = quedas;
                        etapaPulo = correndo ? 9 : 3;
                    }
                    else if (saindo) Aperta(kb);
                    else if (t - marcaPulo > 2.5)
                    {
                        Debug.Log($"[PixelArt] Pulo {(correndo ? "correndo" : "andando")}: não caiu, x={p.transform.position.x:0.0} (chegada {d.chegadaX:0.0})");
                        etapaPulo = correndo ? 9 : 3;
                    }
                    break;
                default:
                    Aperta(kb);
                    break;
            }
        }

        static int etapaC1;
        static double marcaC1;
        static readonly HashSet<string> FotosC1 = new HashSet<string>();

        /// <summary>Cenário 1: entra pela porta da ponta, a porta bate, conversa; solta a alavanca emperrada no mostrador;
        /// dois erros ligam a dica e falar com o Souta desliga.</summary>
        static void Cenario1(double t)
        {
            var d = UnityEngine.Object.FindFirstObjectByType<Cenario1Director>();
            var kb = Keyboard.current;
            if (!d || kb == null) return;
            var p = d.player;
            foreach (var (seg, nome) in new[] { (0.3, "a"), (1.6, "b"), (3.2, "c") })
                if (t > seg && !FotosC1.Contains(nome) && Camera.main)
                {
                    FotosC1.Add(nome);
                    Foto(Camera.main, $"jogo_cenario1_entrada_{nome}.png");
                    Debug.Log($"[PixelArt] Entrada {nome}: kai x={p.transform.position.x:0.00} porta aberta={(d.portaEntrada && d.portaEntrada.IsOpen)}");
                }
            var destrava = DestravaUI.Instance;
            switch (etapaC1)
            {
                case 0: // a cinemática roda sozinha; só avança as falas
                    if (DialogueBox.IsBusy) { Aperta(kb, Time.frameCount % 20 == 0 ? Key.Space : Key.None); return; }
                    Aperta(kb);
                    if (p.controlEnabled && t > 3)
                    {
                        Debug.Log($"[PixelArt] Entrada terminou em {t:0.0}s: kai x={p.transform.position.x:0.00}, porta de entrada fechada={(d.portaEntrada && !d.portaEntrada.IsOpen)}, Souta falou {d.souta.TimesTalked}x");
                        if (Camera.main) Foto(Camera.main, "jogo_cenario1_conversa.png");
                        etapaC1 = 1;
                    }
                    break;
                case 1: // vai até a emperrada e aperta E
                    p.Teleport(new Vector3(d.emperrada.transform.position.x, d.emperrada.transform.position.y + 1.62f, 0f));
                    d.emperrada.Interact();
                    Debug.Log($"[PixelArt] Emperrada: mostrador aberto={(destrava && destrava.IsOpen)}");
                    if (Camera.main) Foto(Camera.main, "jogo_cenario1_emperrada.png");
                    marcaC1 = t;
                    etapaC1 = 2;
                    break;
                case 2: // um erro de propósito, depois E sempre que o ponteiro está no verde
                    if (!destrava || !destrava.IsOpen)
                    {
                        Debug.Log($"[PixelArt] Emperrada soltou={!d.emperrada.stuck} em {t - marcaC1:0.0}s");
                        etapaC1 = 3;
                        break;
                    }
                    float ang = (float)typeof(DestravaUI).GetField("angulo", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(destrava);
                    bool ocupado = (bool)typeof(DestravaUI).GetField("ocupado", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(destrava);
                    bool errou = (int)typeof(Cenario1Director).GetField("erros", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(d) > 0;
                    bool aperta = !ocupado && t - marcaC1 > 0.5 && Time.frameCount % 2 == 0 && (errou ? destrava.NaFaixa(ang) && ang > destrava.faixaDe + 4 : ang > 120f);
                    Aperta(kb, aperta ? Key.E : Key.None);
                    if (t - marcaC1 > 20) { Debug.LogError("[PixelArt] Emperrada: não soltou em 20s"); etapaC1 = 3; }
                    break;
                case 3: // dois erros ligam a dica
                    if (DialogueBox.IsBusy) { Aperta(kb, Time.frameCount % 20 == 0 ? Key.Space : Key.None); return; }
                    Aperta(kb);
                    var erro = typeof(Cenario1Director).GetMethod("Erro", BindingFlags.NonPublic | BindingFlags.Instance);
                    erro.Invoke(d, null);
                    erro.Invoke(d, null);
                    marcaC1 = t;
                    etapaC1 = 4;
                    break;
                case 4:
                    if (DialogueBox.IsBusy) { Aperta(kb, Time.frameCount % 20 == 0 ? Key.Space : Key.None); return; }
                    if (t - marcaC1 < 0.3) return;
                    Debug.Log($"[PixelArt] Dica ligada: souta={d.souta.dica} ren={d.ren.dica}");
                    d.souta.Interact();
                    Debug.Log($"[PixelArt] Falou com o Souta: dica desligada={!d.souta.dica && !d.ren.dica}");
                    etapaC1 = 5;
                    break;
                case 5:
                    if (DialogueBox.IsBusy) { Aperta(kb, Time.frameCount % 20 == 0 ? Key.Space : Key.None); return; }
                    Aperta(kb);
                    Debug.Log($"[PixelArt] Cenário 1 testado em {t:0.0}s");
                    etapaC1 = 99;
                    break;
            }
        }

        static void Aperta(Keyboard kb, params Key[] teclas)
        {
            var estado = new KeyboardState();
            foreach (var k in teclas) if (k != Key.None) estado.Press(k);
            InputSystem.QueueStateEvent(kb, estado);
        }

        /// <summary>A foto da câmera não pega a interface: o estado da abertura vai para o log.</summary>
        static void Estado(string momento)
        {
            var d = UnityEngine.Object.FindFirstObjectByType<Cenario0Director>();
            if (!d) return;
            var hud = Hud.Instance;
            Debug.Log($"[PixelArt] Abertura {momento}: tempo={Time.time:0.0}s quadros={Time.frameCount} escala={Time.timeScale} tremLimpo={(d.tremLimpo && d.tremLimpo.gameObject.activeSelf)} passageiros={(d.passageiros && d.passageiros.activeSelf)} " +
                      $"vermelho={(d.vermelho ? d.vermelho.alpha : -1f):0.00} controle={(d.player && d.player.controlEnabled)} " +
                      $"dialogo={DialogueBox.IsBusy} objetivo=\"{(hud && hud.objectiveText ? hud.objectiveText.text : "")}\" kai={(d.kai && d.kai.view && d.kai.view.sprite ? d.kai.view.sprite.name : "-")}");
        }

        /// <summary>Foto do jogo rodando e confere se os clipes extras existem e tocam.</summary>
        static void Exercita(string cena)
        {
            var ps = UnityEngine.Object.FindObjectsByType<PersonagemPixel>(FindObjectsSortMode.None);
            var txt = new List<string>();
            foreach (var p in ps)
            {
                float total = 0f;
                foreach (var c in p.extras ?? Array.Empty<ClipePixel>()) total += p.Tocar(c.nome);
                txt.Add($"{p.speakerId}:{(p.view && p.view.sprite ? p.view.sprite.name : "SEM SPRITE")}:{total:0.0}s");
            }
            Debug.Log($"[PixelArt] Cenário {cena} em Play: {string.Join(" ", txt)}");
            var cam = Camera.main;
            if (cam) Foto(cam, $"jogo_cenario{cena}.png");
        }

        static void Foto(Camera cam, string arquivo)
        {
            var rt = new RenderTexture(1920, 1080, 24);
            cam.targetTexture = rt;
            cam.Render();
            RenderTexture.active = rt;
            var t = new Texture2D(1920, 1080, TextureFormat.RGB24, false);
            t.ReadPixels(new Rect(0, 0, 1920, 1080), 0, 0);
            t.Apply();
            RenderTexture.active = null;
            cam.targetTexture = null;
            UnityEngine.Object.DestroyImmediate(rt);
            File.WriteAllBytes(Path.Combine(Path.GetFullPath(Previa), arquivo), t.EncodeToPNG());
            UnityEngine.Object.DestroyImmediate(t);
        }
    }
}
