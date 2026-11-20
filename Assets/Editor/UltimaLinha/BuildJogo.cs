using System;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Compila o jogo para Windows (64 bits) com as cenas da lista de build, menos a SampleScene (sobra do template).
    /// Menu Última Linha > Compilar para Windows, ou por linha de comando:
    /// -batchmode -quit -executeMethod UltimaLinha.EditorTools.BuildJogo.Windows -saida "C:\pasta\UltimaLinha.exe"
    /// </summary>
    public static class BuildJogo
    {
        [MenuItem("Última Linha/Compilar para Windows")]
        public static void Windows()
        {
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-saida");
            string saida = i >= 0 && i + 1 < args.Length ? args[i + 1] : "Builds/Windows/UltimaLinha.exe";
            var cenas = EditorBuildSettings.scenes.Where(s => s.enabled && !s.path.Contains("SampleScene")).Select(s => s.path).ToArray();
            var rel = BuildPipeline.BuildPlayer(new BuildPlayerOptions
            {
                scenes = cenas,
                locationPathName = saida,
                target = BuildTarget.StandaloneWindows64,
                options = BuildOptions.None,
            });
            var r = rel.summary;
            Debug.Log($"[Build] {r.result}: {cenas.Length} cenas, {r.totalSize / (1024 * 1024)} MB, {r.totalErrors} erros -> {saida}");
            if (Application.isBatchMode) EditorApplication.Exit(r.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
