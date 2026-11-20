using System.Linq;
using UltimaLinha.Cenario1;
using UltimaLinha.Visual;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using static UltimaLinha.EditorTools.Cenario1SceneBuilder;

namespace UltimaLinha.EditorTools
{
    /// <summary>
    /// Peças visuais reutilizáveis para os montadores de cena: luz 2D (fluorescente, emergência, global), partículas de
    /// ambiente, efeitos de quadros, sombra passageira, sprites Lit e mapa de normais. Arte dos efeitos em
    /// Assets/Art/Efeitos (Ferramentas/Assets/trata.py). Guia: Ferramentas/Assets/GUIA_ARTE.md.
    /// </summary>
    internal static class EfeitosBuild
    {
        const string Pasta = "Assets/Art/Efeitos";
        internal static readonly Color Fria = new Color(0.78f, 0.88f, 0.92f);
        internal static readonly Color Emergencia = new Color(1f, 0.16f, 0.1f);

        internal static void ImportaEfeitos()
        {
            foreach (var path in AssetDatabase.FindAssets("t:Texture2D", new[] { Pasta }).Select(AssetDatabase.GUIDToAssetPath))
                ConfigureSprite(path, path.Contains("sombra") ? new Vector2(0.5f, 0f) : new Vector2(0.5f, 0.5f), PixelArtImporter.Ppu, 512, FilterMode.Point);
        }

        static Sprite[] Quadros(string prefixo)
        {
            var lista = new System.Collections.Generic.List<Sprite>();
            for (int i = 0; ; i++)
            {
                var s = AssetDatabase.LoadAssetAtPath<Sprite>($"{Pasta}/{prefixo}_{i}.png");
                if (!s) break;
                lista.Add(s);
            }
            return lista.ToArray();
        }

        static Material Mat(string caminho) => AssetDatabase.LoadAssetAtPath<Material>(caminho);

        /// <summary>Passa para Sprite-Lit os sprites do cenário e dos personagens (ordem abaixo de 20); halos, brilhos,
        /// a tecla E e os balões continuam Unlit (são luz ou interface, não superfície).</summary>
        internal static int LitNaCena()
        {
            var lit = Mat(Renderizador2D.SpriteLit);
            if (!lit) { Debug.LogWarning("[Última Linha] Sprite-Lit-Default não encontrado"); return 0; }
            int n = 0;
            foreach (var sr in Object.FindObjectsByType<SpriteRenderer>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                string nome = sr.name;
                if (sr.sortingOrder >= 20 || sr.GetComponent<EfeitoQuadros>() || nome.Contains("Halo") || nome.Contains("Brilho") || nome.Contains("Luz") || nome.StartsWith("Dica") || nome == "TeclaE") continue;
                sr.sharedMaterial = lit;
                n++;
            }
            return n;
        }

        /// <summary>Mapa de normais como textura secundária (_NormalMap) do sprite: a luz 2D ganha relevo.</summary>
        internal static void Normais(string spritePath, string normalPath)
        {
            if (AssetImporter.GetAtPath(normalPath) is TextureImporter n)
            {
                n.textureType = TextureImporterType.NormalMap;
                n.filterMode = FilterMode.Point;
                n.mipmapEnabled = false;
                n.maxTextureSize = 4096;
                n.textureCompression = TextureImporterCompression.Uncompressed;
                n.SaveAndReimport();
            }
            var tex = AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath);
            if (!tex || !(AssetImporter.GetAtPath(spritePath) is TextureImporter imp)) return;
            imp.secondarySpriteTextures = new[] { new SecondarySpriteTexture { name = "_NormalMap", texture = tex } };
            imp.SaveAndReimport();
        }

        internal static Light2D Global(Transform pai, float intensidade, Color cor) =>
            Renderizador2D.Luz("LuzGlobal", pai, Vector3.zero, Light2D.LightType.Global, cor, intensidade);

        /// <summary>Luz de uma luminária: segue o halo (o FluorescentLight dele já falha e volta) e treme sozinha.</summary>
        internal static LuzOscilante Fluorescente(Transform pai, Vector3 pos, SpriteRenderer halo, float intensidade = 1.1f, float raio = 6f)
        {
            var l = Renderizador2D.Luz("Luz2D", pai, pos, Light2D.LightType.Point, Fria, intensidade, raio);
            var o = l.gameObject.AddComponent<LuzOscilante>();
            o.intensidade = intensidade;
            o.cor = Fria;
            o.segue = halo;
            o.falhasPorMinuto = halo ? 0f : 1.5f; // com halo, quem falha é o FluorescentLight
            return o;
        }

        /// <summary>Luz vermelha de emergência: pulsa sozinha, ou segue um halo que outro script já anima (e desliga).</summary>
        internal static LuzOscilante DeEmergencia(Transform pai, Vector3 pos, SpriteRenderer halo, float intensidade = 1.4f, float raio = 5f)
        {
            var l = Renderizador2D.Luz("Luz2D_Emergencia", pai, pos, Light2D.LightType.Point, Emergencia, intensidade, raio);
            var o = l.gameObject.AddComponent<LuzOscilante>();
            o.modo = halo ? LuzOscilante.Modo.Fluorescente : LuzOscilante.Modo.Emergencia;
            o.oscilacao = 0f;
            o.falhasPorMinuto = 0f;
            o.velocidade = 0.25f;
            o.intensidade = intensidade;
            o.cor = Emergencia;
            o.segue = halo;
            return o;
        }

        internal static EmissorAmbiente Emissor(Transform pai, string nome, Vector3 pos, EmissorAmbiente.Tipo tipo, Vector2 area, Color cor, float opacidade,
            float intensidade = 1f, int ordem = 30)
        {
            var go = new GameObject(nome);
            go.transform.SetParent(pai, false);
            go.transform.position = pos;
            var ps = go.AddComponent<ParticleSystem>();
            ps.GetComponent<ParticleSystemRenderer>().sharedMaterial = Mat(Renderizador2D.SpriteUnlit);
            var e = go.AddComponent<EmissorAmbiente>();
            e.tipo = tipo;
            e.area = area;
            e.cor = cor;
            e.opacidade = opacidade;
            e.intensidade = intensidade;
            e.ordem = ordem;
            if (tipo == EmissorAmbiente.Tipo.Faisca) e.intervalo = new Vector2(3f, 9f);
            e.Aplica();
            return e;
        }

        /// <summary>Goteira: a gota cai de "pos" até o chão e um respingo aparece lá embaixo no mesmo ritmo (mais ou menos).</summary>
        internal static void Goteira(Transform pai, Vector3 pos, float chao, Vector2 intervalo)
        {
            var g = Emissor(pai, "Goteira", pos, EmissorAmbiente.Tipo.Goteira, new Vector2(0f, pos.y - chao), new Color(0.6f, 0.66f, 0.7f), 0.8f, 1f, 5);
            g.intervalo = intervalo;
            var resp = Quadros("respingo");
            if (resp.Length == 0) return;
            var sr = SpriteObj("Respingo", pai, null, new Vector3(pos.x, chao + 0.2f, 0f), 5);
            var q = sr.gameObject.AddComponent<EfeitoQuadros>();
            q.quadros = resp;
            q.intervalo = intervalo;
            q.opacidade = 0.7f;
        }

        internal static void Faiscas(Transform pai, Vector3 pos)
        {
            Emissor(pai, "Faiscas", pos, EmissorAmbiente.Tipo.Faisca, Vector2.one * 0.1f, new Color(1f, 0.92f, 0.7f), 1f, 1f, 25);
            var fs = Quadros("faisca");
            if (fs.Length == 0) return;
            var sr = SpriteObj("Faisca", pai, null, pos, 26);
            var q = sr.gameObject.AddComponent<EfeitoQuadros>();
            q.quadros = fs;
            q.fps = 18f;
            q.intervalo = new Vector2(3f, 9f);
        }

        /// <summary>Tela de monitor chiando (EfeitoQuadros em laço com os quadros de chiado).</summary>
        internal static EfeitoQuadros Chiado(Transform pai, Vector3 pos, float escala, int ordem)
        {
            var sr = SpriteObj("Chiado", pai, null, pos, ordem);
            sr.transform.localScale = Vector3.one * escala;
            var q = sr.gameObject.AddComponent<EfeitoQuadros>();
            q.quadros = Quadros("chiado");
            q.emLaco = true;
            q.fps = 10f;
            q.opacidade = 0.8f;
            return q;
        }

        internal static SombraPassageira Sombra(Transform pai, Vector3 pos, Transform alvo, float percurso, int ordem = -90)
        {
            var sr = SpriteObj("SombraPassageira", pai, AssetDatabase.LoadAssetAtPath<Sprite>(Pasta + "/sombra_pessoa.png"), pos, ordem);
            var s = sr.gameObject.AddComponent<SombraPassageira>();
            s.alvo = alvo;
            s.percurso = percurso;
            return s;
        }
    }
}
