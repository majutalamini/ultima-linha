using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Troca o renderizador da URP para o 2D Renderer (Light2D, normal maps, sombras 2D), sem mudar a cara das cenas:
    /// o material padrão dos sprites continua Unlit, então só ganha luz o que o montador do cenário passar para Lit
    /// (hoje, o Cenário 4). O renderizador antigo (3D) fica na lista, no índice 1, para voltar se precisar.
    /// Roda sozinho ao abrir o projeto; também no menu Última Linha > Configurar renderizador 2D.
    /// </summary>
    [InitializeOnLoad]
    public static class Renderizador2D
    {
        const string Caminho = "Assets/Settings/URP_Renderer2D.asset";
        internal const string SpriteLit = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Lit-Default.mat";
        internal const string SpriteUnlit = "Packages/com.unity.render-pipelines.universal/Runtime/Materials/Sprite-Unlit-Default.mat";

        static Renderizador2D()
        {
            EditorApplication.delayCall += () => { if (!EditorApplication.isPlayingOrWillChangePlaymode) Garante(); };
        }

        [MenuItem("Última Linha/Configurar renderizador 2D")]
        internal static void Garante()
        {
            var pipe = GraphicsSettings.defaultRenderPipeline as UniversalRenderPipelineAsset;
            if (!pipe) { Debug.LogWarning("[Última Linha] Sem URP ativa: renderizador 2D não configurado."); return; }
            var data = AssetDatabase.LoadAssetAtPath<Renderer2DData>(Caminho) ?? Cria();
            if (!data) return;

            var so = new SerializedObject(pipe);
            var lista = so.FindProperty("m_RendererDataList");
            if (lista.arraySize > 0 && lista.GetArrayElementAtIndex(0).objectReferenceValue == data) return;
            lista.InsertArrayElementAtIndex(0);
            lista.GetArrayElementAtIndex(0).objectReferenceValue = data;
            so.FindProperty("m_DefaultRendererIndex").intValue = 0;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(pipe);
            AssetDatabase.SaveAssets();
            Debug.Log("[Última Linha] Renderizador 2D ativo (" + Caminho + "); o 3D antigo ficou no índice 1.");
        }

        /// <summary>O mesmo que o menu Create > Rendering > URP 2D Renderer da URP (método interno, chamado por reflexão).</summary>
        static Renderer2DData Cria()
        {
            var menus = AppDomain.CurrentDomain.GetAssemblies().Select(a => a.GetType("UnityEditor.Rendering.Universal.Renderer2DMenus")).FirstOrDefault(t => t != null);
            var metodo = menus?.GetMethod("CreateRendererAsset", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
            if (metodo == null) { Debug.LogError("[Última Linha] Não achei Renderer2DMenus.CreateRendererAsset nesta versão da URP."); return null; }
            var p = metodo.GetParameters();
            var tipo = Enum.GetValues(p[1].ParameterType).Cast<object>().First(v => v.ToString().Contains("2D"));
            var args = new object[p.Length];
            args[0] = Caminho;
            args[1] = tipo;
            for (int i = 2; i < p.Length; i++) args[i] = p[i].Name == "relativePath" ? (object)false : p[i].DefaultValue;
            var data = (Renderer2DData)metodo.Invoke(null, args);
            var so = new SerializedObject(data);
            var padrao = so.FindProperty("m_DefaultMaterialType");
            if (padrao != null)
            {
                padrao.enumValueIndex = Array.IndexOf(padrao.enumNames, padrao.enumNames.First(n => n.Contains("Unlit")));
                so.ApplyModifiedPropertiesWithoutUndo();
            }
            EditorUtility.SetDirty(data);
            AssetDatabase.SaveAssets();
            return data;
        }

        /// <summary>Uma Light2D que acende todas as camadas de ordenação (o padrão de AddComponent pode vir vazio).</summary>
        internal static Light2D Luz(string nome, Transform pai, Vector3 pos, Light2D.LightType tipo, Color cor, float intensidade, float raio = 5f)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.transform.position = pos;
            var l = go.AddComponent<Light2D>();
            l.lightType = tipo;
            l.color = cor;
            l.intensity = intensidade;
            if (tipo == Light2D.LightType.Point)
            {
                l.pointLightOuterRadius = raio;
                l.pointLightInnerRadius = raio * 0.15f;
            }
            var so = new SerializedObject(l);
            var camadas = so.FindProperty("m_ApplyToSortingLayers");
            if (camadas != null)
            {
                var ids = SortingLayer.layers.Select(s => s.id).ToArray();
                camadas.arraySize = ids.Length;
                for (int i = 0; i < ids.Length; i++) camadas.GetArrayElementAtIndex(i).intValue = ids[i];
            }
            var normais = so.FindProperty("m_NormalMapQuality");
            if (normais != null && tipo != Light2D.LightType.Global) normais.intValue = (int)Light2D.NormalMapQuality.Accurate;
            so.ApplyModifiedPropertiesWithoutUndo();
            return l;
        }
    }
}
