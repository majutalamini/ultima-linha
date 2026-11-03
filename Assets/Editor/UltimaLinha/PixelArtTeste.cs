using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using UltimaLinha.Cenario1;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

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
            for (int i = 1; i <= 7; i++)
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
            EditorUtility.audioMasterMute = true; // o teste não pode tocar o som dos cenários no computador de ninguém
            SessionState.SetInt(Erros, 0);
            SessionState.SetString(Fase, "abrir");
            SessionState.SetString(Fila, "1,2,3,4,5,6,7");
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
                SessionState.SetString(Fase, "jogando");
                EditorApplication.isPlaying = true;
            }
            else if (fase == "jogando" && EditorApplication.isPlaying)
            {
                if (inicio < 0) inicio = EditorApplication.timeSinceStartup;
                double t = EditorApplication.timeSinceStartup - inicio;
                if (!fotografada && t > SegundosPorCena * 0.5)
                {
                    fotografada = true;
                    Exercita(partes[0]);
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
                EditorUtility.audioMasterMute = false;
                int erros = SessionState.GetInt(Erros, 0);
                Debug.Log($"[PixelArt] TESTE CONCLUIDO, erros em execução: {erros}");
                if (Array.IndexOf(Environment.GetCommandLineArgs(), "-executeMethod") >= 0) EditorApplication.Exit(erros == 0 ? 0 : 1);
            }
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
