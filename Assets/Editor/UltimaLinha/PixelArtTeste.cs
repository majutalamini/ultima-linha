using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        const string SoC2 = "UltimaLinha.TesteSoC2";
        const string SoC3 = "UltimaLinha.TesteSoC3";
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

        /// <summary>Só o Cenário 2, jogado por um piloto automático do começo até a porta do fim (duas vezes: o primeiro
        /// Play pode cortar a corrotina da entrada). Por linha de comando: -executeMethod UltimaLinha.EditorTools.PixelArtTeste.SoCenario2</summary>
        [MenuItem("Última Linha/Testes/Jogar o Cenário 2 sozinho (piloto automático, sem som)")]
        public static void SoCenario2()
        {
            SoRodar();
            SessionState.SetBool(SoC2, true);
            SessionState.SetString(Fila, "2,2");
        }

        /// <summary>Recria a cena do Cenário 2 e roda o piloto (linha de comando, depois de mudar o montador).</summary>
        public static void ReconstruirC2ERodar()
        {
            var tipo = typeof(Cenario2SceneBuilder);
            if (!(bool)tipo.GetMethod("Build", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { false }))
                Debug.LogError("[Cenário 2] Não foi reconstruído");
            var chave = (string)tipo.GetProperty("VersionKey", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            EditorPrefs.SetInt(chave, (int)tipo.GetField("BuildVersion", BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue());
            AssetDatabase.SaveAssets();
            SoCenario2();
        }

        /// <summary>Só o Cenário 3, jogado por um piloto automático: anda até cada papel, sobe os caixotes até o mezanino,
        /// resolve o painel de desvios com as setas e o E e sai pela porta. Por linha de comando:
        /// -executeMethod UltimaLinha.EditorTools.PixelArtTeste.SoCenario3</summary>
        [MenuItem("Última Linha/Testes/Jogar o Cenário 3 sozinho (piloto automático, sem som)")]
        public static void SoCenario3()
        {
            SoRodar();
            SessionState.SetBool(SoC3, true);
            SessionState.SetString(Fila, "3,3");
        }

        /// <summary>Recria a cena do Cenário 3 e roda o piloto (linha de comando, depois de mudar o montador).</summary>
        public static void ReconstruirC3ERodar()
        {
            var tipo = typeof(Cenario3SceneBuilder);
            if (!(bool)tipo.GetMethod("Build", BindingFlags.NonPublic | BindingFlags.Static).Invoke(null, new object[] { false }))
                Debug.LogError("[Cenário 3] Não foi reconstruído");
            var chave = (string)tipo.GetProperty("VersionKey", BindingFlags.NonPublic | BindingFlags.Static).GetValue(null);
            EditorPrefs.SetInt(chave, (int)tipo.GetField("BuildVersion", BindingFlags.NonPublic | BindingFlags.Static).GetRawConstantValue());
            AssetDatabase.SaveAssets();
            SoCenario3();
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
                etapaC2 = fusivelC2 = 0;
                FotosC2.Clear();
                etapaC3 = 0;
                FotosC3.Clear();
                // sem foco a Unity descarta o teclado simulado; o projeto não tem InputSettings salvo, então isto vale só nesta sessão
                InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
                InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
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
                bool soC2 = SessionState.GetBool(SoC2, false);
                if (soC2)
                {
                    Cenario2(t);
                    if (t < 300.0 && etapaC2 < 99) return;
                    if (etapaC2 < 99) Debug.LogError($"[Cenário 2] Piloto não chegou ao fim em 300s (etapa {etapaC2})");
                }
                bool soC3 = SessionState.GetBool(SoC3, false);
                if (soC3)
                {
                    Cenario3(t);
                    if (t < 300.0 && etapaC3 < 99) return;
                    if (etapaC3 < 99) Debug.LogError($"[Cenário 3] Piloto não chegou ao fim em 300s (etapa {etapaC3}, alvo {alvoC3})");
                }
                if (!fotografada && t > SegundosPorCena * 0.5 && partes[0] != "0" && !soC2 && !soC3)
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
                SessionState.SetBool(SoC2, false);
                SessionState.SetBool(SoC3, false);
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

        static int etapaC2, fusivelC2;
        static double marcaC2;
        static readonly HashSet<string> FotosC2 = new HashSet<string>();
        // corrida a 5,8 u/s; pulando correndo o centro do Kai anda ~5 u no ar (subida 10,8, gravidade 25)
        const float CorridaC2 = 5.8f, AlcancePuloC2 = 4.6f, MeiaLarguraKai = 0.45f;

        static T Campo<T>(object o, string nome) => (T)o.GetType().GetField(nome, BindingFlags.NonPublic | BindingFlags.Instance).GetValue(o);
        static void Chama(object o, string metodo, params object[] args) =>
            o.GetType().GetMethod(metodo, BindingFlags.NonPublic | BindingFlags.Instance).Invoke(o, args);

        /// <summary>Tempo que a porta ainda fica sem esmagar (aberta + aviso); 0 se está fechando, fechada ou abrindo.</summary>
        static float FolgaPorta(UltimaLinha.Cenario2.CarDoor p)
        {
            if (p.Current != UltimaLinha.Cenario2.CarDoor.State.Open && p.Current != UltimaLinha.Cenario2.CarDoor.State.Warning) return 0f;
            float dur = (float)typeof(UltimaLinha.Cenario2.CarDoor).GetMethod("Duration", BindingFlags.NonPublic | BindingFlags.Instance).Invoke(p, new object[] { p.Current });
            float resta = dur - Campo<float>(p, "timer");
            return p.Current == UltimaLinha.Cenario2.CarDoor.State.Open ? resta + p.warnTime : resta;
        }

        static void FotoC2(string nome)
        {
            if (FotosC2.Add(nome) && Camera.main) Foto(Camera.main, $"jogo_c2_{nome}.png");
        }

        /// <summary>Leva o Kai até um ponto de interação e aperta E nele (o caminho até lá não é o que se testa).</summary>
        static void UsaAqui(UltimaLinha.Cenario2.Cenario2Director d, Interactable alvo)
        {
            var p = d.player;
            p.Teleport(new Vector3(alvo.transform.position.x, alvo.transform.position.y + Cenario1SceneBuilder.ColliderHeight / 2f + 0.02f, 0f));
            if (d.cameraFollow) d.cameraFollow.Snap();
            alvo.Interact();
        }

        /// <summary>Cenário 2: passa as falas e os papéis (Enter, para não pular), abre o cadeado (um código errado antes),
        /// puxa a retenção e corre, pega os três fusíveis e liga a caixa, e atravessa as passagens esperando cada porta
        /// abrir e pulando na beirada. Anota tempos, esmagadas e quedas, e fotografa cada parte.</summary>
        static void Cenario2(double t)
        {
            var d = UnityEngine.Object.FindFirstObjectByType<UltimaLinha.Cenario2.Cenario2Director>();
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!d)
            {
                if (etapaC2 > 0 && etapaC2 < 99)
                {
                    Debug.Log($"[Cenário 2] Saiu pela porta do fim aos {t:0.0}s e carregou {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}");
                    etapaC2 = 99;
                }
                Aperta(kb);
                return;
            }
            var p = d.player;
            float x = p.transform.position.x;
            if (etapaC2 == 0 && t > 40.0)
            {
                Debug.LogWarning($"[Cenário 2] A entrada não devolveu o controle em 40s: dialogo={DialogueBox.IsBusy} introDone={Campo<bool>(d, "introDone")} " +
                                 $"controle={p.controlEnabled} quadros={Time.frameCount} (corrotina cortada pelo primeiro Play?)");
                etapaC2 = 99;
                return;
            }
            bool papel = UltimaLinha.Salas.DocumentView.Instance && UltimaLinha.Salas.DocumentView.Instance.IsOpen;
            if (DialogueBox.IsBusy || papel) { Aperta(kb, Time.frameCount % 20 == 0 ? Key.Enter : Key.None); return; }
            int cp = Campo<int>(d, "checkpoint");

            switch (etapaC2)
            {
                case 0: // entrada
                    Aperta(kb);
                    if (Campo<bool>(d, "introDone") && p.controlEnabled)
                    {
                        Debug.Log($"[Cenário 2] Entrada terminou aos {t:0.0}s; kai x={x:0.00}");
                        FotoC2("comeco");
                        marcaC2 = t;
                        etapaC2 = 1;
                    }
                    break;
                case 1: // o cadeado, com um código errado primeiro
                    Aperta(kb);
                    UsaAqui(d, d.cadeado);
                    etapaC2 = 2;
                    marcaC2 = t;
                    break;
                case 2:
                    Aperta(kb);
                    if (d.lockUI && d.lockUI.IsOpen && t - marcaC2 > 0.5)
                    {
                        Chama(d, "OnCodeSubmitted", "1234");
                        etapaC2 = 3;
                        marcaC2 = t;
                    }
                    break;
                case 3: // depois do erro (o diretor fecha o cadeado e o Kai fala), o código certo
                    Aperta(kb);
                    if (t - marcaC2 < 1.5 || UltimaLinha.Salas.Modal.IsOpen && !d.lockUI.IsOpen) break;
                    if (!d.lockUI.IsOpen) { UsaAqui(d, d.cadeado); marcaC2 = t - 1.0; break; }
                    Chama(d, "OnCodeSubmitted", "0417");
                    etapaC2 = 4;
                    break;
                case 4:
                    Aperta(kb);
                    if (Campo<bool>(d, "lockOpen") && d.doors[0].cycling && p.controlEnabled && !UltimaLinha.Salas.Modal.IsOpen)
                    {
                        Debug.Log($"[Cenário 2] Cadeado aberto aos {t:0.0}s; portas da passagem 1 batendo={d.doors[0].cycling}; Souta no vagão {Campo<int>(d, "npcVagao") + 1}");
                        marcaC2 = t;
                        etapaC2 = 5;
                    }
                    break;
                case 5: // passagem 1
                    if (cp >= 1) { Debug.Log($"[Cenário 2] Passagem 1 em {t - marcaC2:0.0}s"); etapaC2 = 6; marcaC2 = t; break; }
                    Corre(d, kb, x);
                    break;
                case 6: // vagão 2: espera a conversa, puxa a retenção e corre
                    if (!Campo<bool>(d, "falouV2")) { Aperta(kb); break; }
                    FotoC2("vagao2");
                    if (cp >= 2) { Debug.Log($"[Cenário 2] Passagem 2 em {t - marcaC2:0.0}s (com a retenção)"); etapaC2 = 7; marcaC2 = t; break; }
                    if (!d.retencao.IsUp && x < d.carEnds[1] - 1f && p.controlEnabled) { Aperta(kb); UsaAqui(d, d.retencao); break; }
                    if (!d.retencao.IsUp && x < d.carEnds[1] - 1f) { Aperta(kb); break; }
                    Corre(d, kb, x);
                    break;
                case 7: // vagão 3: escuro; os três fusíveis e a caixa
                    if (!Campo<bool>(d, "falouV3")) { Aperta(kb); break; }
                    FotoC2("vagao3_escuro");
                    Aperta(kb);
                    if (!p.controlEnabled) break;
                    if (fusivelC2 < d.fusiveis.Length) { UsaAqui(d, d.fusiveis[fusivelC2++]); break; }
                    if (!Campo<bool>(d, "luzOk")) { UsaAqui(d, d.caixa); break; }
                    Debug.Log($"[Cenário 2] Luz do vagão 3 voltou aos {t:0.0}s; fusíveis={Campo<int>(d, "fuses")}; Souta no vagão {Campo<int>(d, "npcVagao") + 1}");
                    FotoC2("vagao3_luz");
                    etapaC2 = 8;
                    marcaC2 = t;
                    break;
                case 8: // passagem 3 até o último vagão
                    if (Campo<bool>(d, "endingStarted"))
                    {
                        Debug.Log($"[Cenário 2] Passagem 3 em {t - marcaC2:0.0}s. Chegou ao último vagão aos {t:0.0}s; esmagado {Campo<int>(d, "crushes")}x, caiu {Campo<int>(d, "falls")}x");
                        etapaC2 = 9;
                        break;
                    }
                    Corre(d, kb, x);
                    break;
                case 9: // o trem freia; espera a porta do fim
                    Aperta(kb);
                    if (Campo<bool>(d, "exitOpen"))
                    {
                        FotoC2("fim");
                        Debug.Log($"[Cenário 2] Porta do fim abriu aos {t:0.0}s");
                        etapaC2 = 10;
                    }
                    break;
                case 10:
                    Aperta(kb, Key.D);
                    break;
            }
        }

        /// <summary>Corre para a direita; espera a porta da frente abrir; pula na beirada de cada buraco, a não ser que
        /// fosse cair na frente de uma porta que vai bater (aí espera onde está: na plataforma ou no engate).</summary>
        static void Corre(UltimaLinha.Cenario2.Cenario2Director d, Keyboard kb, float x)
        {
            var p = d.player;
            if (Time.frameCount % 300 == 0)
                Debug.Log($"[Cenário 2] corrida: x={x:0.00} chão={p.IsGrounded} controle={p.controlEnabled} esmagado={Campo<int>(d, "crushes")} caiu={Campo<int>(d, "falls")} " +
                          string.Join(" ", d.doors.Select(q => $"[{q.CenterX:0.0} {q.Current} {FolgaPorta(q):0.0}]")));
            if (!p.controlEnabled) { Aperta(kb); return; } // morreu e está voltando
            if (!p.IsGrounded && p.velocity.y > 0f) { Aperta(kb, Key.D, Key.LeftShift, Key.Space); return; } // segura o pulo, como quem joga
            for (int i = 0; i * 2 + 1 < d.doors.Length; i++)
                if (x > d.doors[i * 2].CenterX - 3f && x < d.doors[i * 2 + 1].CenterX)
                {
                    FotoC2("passagem" + (i + 1));
                    if (x > d.doors[i * 2].CenterX + 1.5f) FotoC2("vao" + (i + 1));
                }
            var porta = d.doors.Where(q => x < q.CenterX + q.halfWidth + MeiaLarguraKai).OrderBy(q => q.CenterX).FirstOrDefault();
            var vao = d.buracos.Where(h => h.x > x - 0.05f).OrderBy(h => h.x).Select(h => (Vector2?)h).FirstOrDefault();
            bool Passa(UltimaLinha.Cenario2.CarDoor q) => q == null || FolgaPorta(q) > (q.CenterX + q.halfWidth + MeiaLarguraKai - x) / CorridaC2 + 0.3f;
            if (vao.HasValue && vao.Value.x - x < 0.45f && p.IsGrounded)
            {
                bool pousoNaPorta = porta && x + AlcancePuloC2 + MeiaLarguraKai > porta.CenterX - porta.halfWidth;
                if (pousoNaPorta && !Passa(porta)) Aperta(kb);
                else Aperta(kb, Key.D, Key.LeftShift, Key.Space);
            }
            else if (porta && porta.CenterX - x < 1.8f && (!vao.HasValue || vao.Value.x > porta.CenterX) && p.IsGrounded && !Passa(porta))
                Aperta(kb);
            else
                Aperta(kb, Key.D, Key.LeftShift);
        }

        static int etapaC3, alvoC3, travasC3, encostosC3, tentativaC3, npcNaFrenteC3;
        static double marcaC3, inicioAlvoC3, marcaBloqC3, marcaParadoC3, amostraC3, chegouC3 = -1, recuoC3 = -1;
        static float chaoC3, ultimoXC3, xAmostraC3;
        static readonly HashSet<string> FotosC3 = new HashSet<string>();
        static readonly List<Interactable> AlvosC3 = new List<Interactable>();
        static readonly Queue<Key> TeclasC3 = new Queue<Key>();
        static readonly List<Bounds> BeiradasC3 = new List<Bounds>();

        static void FotoC3(string nome)
        {
            if (FotosC3.Add(nome) && Camera.main) Foto(Camera.main, $"jogo_c3_{nome}.png");
        }

        static void CodigoC3(UltimaLinha.Salas.Cenario3Director d, string codigo)
        {
            var m = typeof(UltimaLinha.Salas.Cenario3Director).GetMethod("CheckCode", BindingFlags.NonPublic | BindingFlags.Instance);
            d.StartCoroutine((System.Collections.IEnumerator)m.Invoke(d, new object[] { codigo }));
        }

        /// <summary>Cenário 3: anda (com o teclado simulado) até cada lugar de E em ordem de x (bilhete, placa, moeda, cartaz,
        /// mapa, quadro, portão, comunicado, achados, máquina, escala e monitor no mezanino) e depois até a porta. No portão,
        /// erra com 0417 de propósito e acerta com 2350. Para subir, pula quando há uma beirada de mão única alcançável logo à
        /// frente (os caixotes e o mezanino) e segura o ESPAÇO enquanto sobe. No painel, setas e E até a solução. Parado 6 s:
        /// anota onde travou e teleporta até o alvo.</summary>
        static void Cenario3(double t)
        {
            var d = UnityEngine.Object.FindFirstObjectByType<UltimaLinha.Salas.Cenario3Director>();
            var kb = Keyboard.current;
            if (kb == null) return;
            if (!d)
            {
                if (etapaC3 > 0 && etapaC3 < 99)
                {
                    Debug.Log($"[Cenário 3] Saiu pela porta aos {t:0.0}s e carregou {UnityEngine.SceneManagement.SceneManager.GetActiveScene().name}; " +
                              $"travou {travasC3}x, encostou sem conseguir pular {encostosC3}x, NPC na frente do E {npcNaFrenteC3}x");
                    etapaC3 = 99;
                }
                Aperta(kb);
                return;
            }
            var p = d.player;
            float x = p.transform.position.x;
            float pes = p.GetComponent<Collider2D>().bounds.min.y;
            if (etapaC3 == 0 && t > 40.0)
            {
                Debug.LogWarning($"[Cenário 3] A entrada não devolveu o controle em 40s: dialogo={DialogueBox.IsBusy} introDone={Campo<bool>(d, "introDone")} " +
                                 $"controle={p.controlEnabled} quadros={Time.frameCount} (corrotina cortada pelo primeiro Play?)");
                etapaC3 = 99;
                return;
            }
            bool papel = UltimaLinha.Salas.DocumentView.Instance && UltimaLinha.Salas.DocumentView.Instance.IsOpen;
            if (DialogueBox.IsBusy || papel)
            {
                Aperta(kb, Time.frameCount % 20 == 0 ? Key.Enter : Key.None);
                marcaBloqC3 = marcaParadoC3 = t; // parado de propósito, não barrado
                chegouC3 = -1;
                return;
            }
            var interactor = p.GetComponent<PlayerInteractor>();

            switch (etapaC3)
            {
                case 0: // entrada
                    Aperta(kb);
                    if (Campo<bool>(d, "introDone") && p.controlEnabled && p.IsGrounded)
                    {
                        Debug.Log($"[Cenário 3] Entrada terminou aos {t:0.0}s; kai x={x:0.00}; porta do trem fechada={(d.portaTrem && !d.portaTrem.IsOpen)}");
                        FotoC3("comeco");
                        chaoC3 = pes;
                        travasC3 = encostosC3 = tentativaC3 = npcNaFrenteC3 = 0;
                        recuoC3 = chegouC3 = -1;
                        AlvosC3.Clear();
                        AlvosC3.AddRange(UnityEngine.Object.FindObjectsByType<UltimaLinha.Salas.ObjectSpot>(FindObjectsSortMode.None).OrderBy(s => s.transform.position.x));
                        AlvosC3.Add(d.door);
                        BeiradasC3.Clear();
                        foreach (var o in UnityEngine.Object.FindObjectsByType<Platformer.Mechanics.OneWayPlatform>(FindObjectsSortMode.None))
                            BeiradasC3.Add(o.GetComponent<Collider2D>().bounds);
                        alvoC3 = 0;
                        marcaC3 = inicioAlvoC3 = marcaParadoC3 = marcaBloqC3 = t;
                        ultimoXC3 = x;
                        // a dica (como se tivesse passado 75 s parado): balão nos dois; falar com o Souta desliga
                        Chama(d, "LigaDica");
                        bool ligou = d.souta.dica && d.ren.dica;
                        d.souta.Interact();
                        Debug.Log($"[Cenário 3] Dica: ligou={ligou}, fala do Souta=\"{(d.souta.hintLevers.Count > 0 ? d.souta.hintLevers[0].text : "")}\", desligou ao falar={!d.souta.dica && !d.ren.dica}");
                        etapaC3 = 1;
                    }
                    break;
                case 1: // anda até o alvo e aperta E nele
                {
                    var alvo = AlvosC3[alvoC3];
                    var spot = alvo as UltimaLinha.Salas.ObjectSpot;
                    bool portao = spot && spot == d.portao;
                    bool feito = portao ? Campo<bool>(d, "gateOpen") : spot ? spot.Uses > 0 && !spot.Busy : d.panel.IsOpen;
                    if (feito)
                    {
                        Debug.Log($"[Cenário 3] {alvo.name} em {t - inicioAlvoC3:0.0}s (aos {t:0.0}s)" +
                                  (spot ? $"; peças {Campo<bool[]>(d, "found").Count(f => f)}/{d.documents.Length}" : "; painel aberto"));
                        Aperta(kb);
                        if (!spot) { etapaC3 = 2; marcaC3 = t; break; }
                        if (portao) FotoC3("portao_aberto");
                        alvoC3++;
                        inicioAlvoC3 = marcaParadoC3 = marcaBloqC3 = t;
                        chegouC3 = -1;
                        break;
                    }
                    if (portao && d.lockUI && d.lockUI.IsOpen) // o teclado do portão: primeiro a armadilha, depois o certo
                    {
                        Aperta(kb);
                        marcaParadoC3 = t;
                        chegouC3 = -1;
                        if (t - marcaC3 < 0.6) break;
                        string codigo = tentativaC3 == 0 ? d.armadilha : d.codigo;
                        Debug.Log($"[Cenário 3] Portão: tentando {codigo} aos {t:0.0}s");
                        CodigoC3(d, codigo);
                        tentativaC3++;
                        marcaC3 = t + 2.5; // espera o resultado e a fala
                        break;
                    }
                    if (!p.controlEnabled || UltimaLinha.Salas.Modal.IsOpen) { Aperta(kb); marcaParadoC3 = t; marcaC3 = Math.Max(marcaC3, t); chegouC3 = -1; break; }
                    float ax = alvo.transform.position.x, ay = alvo.transform.position.y;
                    if (!spot) ax -= 0.6f; // a porta: fica na frente dela
                    bool sobe = ay > pes + 0.3f, perto = Mathf.Abs(ax - x) < 0.35f && Mathf.Abs(ay - pes) < 0.4f;
                    if (pes > chaoC3 + 3f) FotoC3("mezanino");
                    if (perto && p.IsGrounded)
                    {
                        FotoC3(spot ? spot.id : "porta");
                        if (chegouC3 < 0) chegouC3 = t;
                        bool meu = interactor && interactor.Current == alvo;
                        Aperta(kb, meu && Time.frameCount % 10 == 0 ? Key.E : Key.None);
                        if (!meu && t - chegouC3 > 3.0)
                        {
                            npcNaFrenteC3++;
                            var pe = new Vector2(p.Bounds.center.x, p.Bounds.min.y);
                            Debug.LogWarning($"[Cenário 3] O E de {alvo.name} não aparece (a tecla E está em {(interactor && interactor.Current ? interactor.Current.name : "nada")}; " +
                                             $"controle={p.controlEnabled} modal={UltimaLinha.Salas.Modal.Count} pode={alvo.CanInteract} alcance={alvo.InReach(pe)} " +
                                             $"ativo={alvo.isActiveAndEnabled} interactor={(interactor ? interactor.enabled : false)} letras={LetterLockUI.IsOpen} dialogo={DialogueBox.IsBusy} " +
                                             $"jogador do interactor={(interactor.player == p)}/{(interactor.player ? interactor.player.controlEnabled : false)} tecla E visível={(interactor.prompt && interactor.prompt.enabled)} " +
                                             $"na lista={Interactable.Active.Contains(alvo)} perto={string.Join(",", Interactable.Active.Where(i => i && i.InReach(pe)).Select(i => i.name + (i.CanInteract ? "+" : "-")))}); usando direto");
                            alvo.Interact();
                            chegouC3 = t;
                        }
                    }
                    else if (!p.IsGrounded)
                    {
                        recuoC3 = -1;
                        Aperta(kb, ax > x ? Key.D : Key.A, sobe ? Key.None : Key.LeftShift, p.velocity.y > 0f ? Key.Space : Key.None);
                    }
                    else
                    {
                        float lado = ax > x ? 1f : -1f;
                        // barrado: a velocidade não zera contra a parede, então olha se a posição mudou
                        if (t - amostraC3 > 0.1) { if (Mathf.Abs(x - xAmostraC3) > 0.03f) marcaBloqC3 = t; xAmostraC3 = x; amostraC3 = t; }
                        bool barrado = Mathf.Abs(ax - x) > 0.5f && t - marcaBloqC3 > 0.25;
                        if (barrado && recuoC3 < 0)
                        {
                            recuoC3 = t;
                            if (encostosC3++ == 0) Debug.Log($"[Cenário 3] Encostado em x={x:0.00} pés={pes:0.00}: recuando um passo para pular");
                        }
                        if (recuoC3 >= 0 && t - recuoC3 < 0.25) { Aperta(kb, ax > x ? Key.A : Key.D); break; }
                        // subindo: pula quando tem uma beirada de mão única logo à frente, mais alta e ao alcance do pulo (2,3 u)
                        bool beirada = sobe && BeiradasC3.Any(b => b.max.y > pes + 0.3f && b.max.y < pes + 2.1f && x + lado * 0.9f > b.min.x + 0.15f && x + lado * 0.9f < b.max.x - 0.15f);
                        bool pula = (recuoC3 >= 0 || beirada) && Time.frameCount % 4 < 2
                                    || p.velocity.y > 0f; // segura enquanto sobe (soltar corta o pulo), mesmo no 1º quadro, ainda "no chão"
                        if (recuoC3 >= 0 && t - recuoC3 > 1.0) recuoC3 = -1;
                        Aperta(kb, ax > x ? Key.D : Key.A, sobe ? Key.None : Key.LeftShift, pula ? Key.Space : Key.None);
                    }
                    if (Mathf.Abs(x - ultimoXC3) > 0.05f || perto) { ultimoXC3 = x; marcaParadoC3 = t; }
                    else if (t - marcaParadoC3 > 6.0)
                    {
                        travasC3++;
                        Debug.LogWarning($"[Cenário 3] TRAVOU indo para {alvo.name}: kai x={x:0.00} pés={pes:0.00} (alvo x={ax:0.00} y={ay:0.00}); teleportando");
                        FotoC3("travou" + travasC3);
                        p.Teleport(new Vector3(ax, ay + Cenario1SceneBuilder.ColliderHeight / 2f + 0.02f, 0f));
                        if (d.cameraFollow) d.cameraFollow.Snap();
                        marcaParadoC3 = t;
                    }
                    if (t - inicioAlvoC3 > 60.0) { Debug.LogError($"[Cenário 3] Não chegou em {alvo.name} em 60s"); etapaC3 = 99; }
                    break;
                }
                case 2: // painel: espera as peças caírem e monta as teclas (setas até cada trilho torto, E até ficar certo)
                {
                    if (t - marcaC3 < 2.0) { Aperta(kb); break; }
                    var celulas = (Array)typeof(UltimaLinha.Salas.SwitchPanelUI).GetField("cells", BindingFlags.NonPublic | BindingFlags.Instance).GetValue(d.panel);
                    int cursor = Campo<int>(d.panel, "cursor"), cols = d.panel.columns, giros = 0;
                    TeclasC3.Clear();
                    for (int i = 0; i < celulas.Length; i++)
                    {
                        var cel = celulas.GetValue(i);
                        int falta = (4 - (int)cel.GetType().GetField("turns").GetValue(cel) % 4) % 4;
                        if (falta == 0) continue;
                        for (int c = cursor % cols; c != i % cols; c += c < i % cols ? 1 : -1) TeclasC3.Enqueue(c < i % cols ? Key.RightArrow : Key.LeftArrow);
                        for (int r = cursor / cols; r != i / cols; r += r < i / cols ? 1 : -1) TeclasC3.Enqueue(r < i / cols ? Key.DownArrow : Key.UpArrow);
                        for (int k = 0; k < falta; k++) TeclasC3.Enqueue(Key.E);
                        giros += falta;
                        cursor = i;
                    }
                    Debug.Log($"[Cenário 3] Painel: {giros} giros, {TeclasC3.Count} teclas até a solução (sem errar)");
                    FotoC3("painel");
                    marcaC3 = t;
                    etapaC3 = 3;
                    break;
                }
                case 3: // uma tecla a cada 6 quadros (solta entre uma e outra, para contar como apertada)
                    if (d.panel.IsSolved)
                    {
                        Debug.Log($"[Cenário 3] Linha completa em {t - marcaC3:0.0}s de painel (aos {t:0.0}s)");
                        Aperta(kb);
                        etapaC3 = 4;
                        break;
                    }
                    if (TeclasC3.Count == 0)
                    {
                        Aperta(kb);
                        if (t - marcaC3 > 30.0) { Debug.LogError("[Cenário 3] Painel: acabaram as teclas e a linha não fechou"); etapaC3 = 99; }
                        break;
                    }
                    Aperta(kb, Time.frameCount % 6 == 0 ? TeclasC3.Dequeue() : Key.None);
                    break;
                case 4: // a porta destravou: entra
                    if (!p.controlEnabled || d.panel.IsOpen) { Aperta(kb); break; }
                    FotoC3("fim");
                    Debug.Log($"[Cenário 3] Porta aberta, controle de volta aos {t:0.0}s; Souta x={d.souta.transform.position.x:0.0}, Ren x={d.ren.transform.position.x:0.0}, " +
                              $"porta x={d.door.transform.position.x:0.0}; conversa das peças={Campo<bool>(d, "falouPecas")}");
                    etapaC3 = 5;
                    break;
                case 5:
                    Aperta(kb, interactor && interactor.Current == d.door && Time.frameCount % 10 == 0 ? Key.E : Key.None);
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
