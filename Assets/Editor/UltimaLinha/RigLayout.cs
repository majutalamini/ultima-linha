// GERADO por Ferramentas/Cenario1/rig_cs.py a partir de rig_layout.json. Não edite à mão.
using System.Collections.Generic;
using UnityEngine;

namespace UltimaLinha.EditorTools
{
    /// <summary>Pivôs das partes dos personagens articulados e medidas do esqueleto (em unidades).</summary>
    public static class RigLayout
    {
        public const float PixelsPerUnit = 200f;
        public const float Thigh = 0.8000f, Shin = 0.7400f, Ankle = 0.1000f;
        public const float Upper = 0.5800f, Fore = 0.5200f;
        public static readonly Vector2 Hip = new Vector2(0.0000f, -0.0200f);
        public static readonly Vector2 Shoulder = new Vector2(-0.0400f, 1.1000f);
        public static readonly Vector2 Neck = new Vector2(0.0100f, 1.1800f);
        public static float HipHeight => Thigh + Shin + Ankle;

        public static readonly Dictionary<string, Vector2> Pivots = new Dictionary<string, Vector2>
        {
            { "kai_torso", new Vector2(0.5f, 0.1412f) },
            { "kai_head", new Vector2(0.4737f, 0.102f) },
            { "kai_thigh_f", new Vector2(0.5f, 0.8306f) },
            { "kai_shin_f", new Vector2(0.5f, 0.8426f) },
            { "kai_foot_f", new Vector2(0.3333f, 0.6538f) },
            { "kai_upper_f", new Vector2(0.5f, 0.8152f) },
            { "kai_fore_f", new Vector2(0.5f, 0.8529f) },
            { "kai_thigh_b", new Vector2(0.5f, 0.8306f) },
            { "kai_shin_b", new Vector2(0.5f, 0.8426f) },
            { "kai_foot_b", new Vector2(0.3333f, 0.6538f) },
            { "kai_upper_b", new Vector2(0.5f, 0.8152f) },
            { "kai_fore_b", new Vector2(0.5f, 0.8529f) },
            { "souta_torso", new Vector2(0.5f, 0.1412f) },
            { "souta_head", new Vector2(0.4737f, 0.102f) },
            { "souta_thigh_f", new Vector2(0.5f, 0.8306f) },
            { "souta_shin_f", new Vector2(0.5f, 0.8426f) },
            { "souta_foot_f", new Vector2(0.3333f, 0.6538f) },
            { "souta_upper_f", new Vector2(0.5f, 0.8152f) },
            { "souta_fore_f", new Vector2(0.5f, 0.8529f) },
            { "souta_thigh_b", new Vector2(0.5f, 0.8306f) },
            { "souta_shin_b", new Vector2(0.5f, 0.8426f) },
            { "souta_foot_b", new Vector2(0.3333f, 0.6538f) },
            { "souta_upper_b", new Vector2(0.5f, 0.8152f) },
            { "souta_fore_b", new Vector2(0.5f, 0.8529f) },
            { "ren_torso", new Vector2(0.5f, 0.2151f) },
            { "ren_head", new Vector2(0.4737f, 0.102f) },
            { "ren_thigh_f", new Vector2(0.5f, 0.8306f) },
            { "ren_shin_f", new Vector2(0.5f, 0.8426f) },
            { "ren_foot_f", new Vector2(0.3333f, 0.6538f) },
            { "ren_upper_f", new Vector2(0.5f, 0.8152f) },
            { "ren_fore_f", new Vector2(0.5f, 0.8529f) },
            { "ren_thigh_b", new Vector2(0.5f, 0.8306f) },
            { "ren_shin_b", new Vector2(0.5f, 0.8426f) },
            { "ren_foot_b", new Vector2(0.3333f, 0.6538f) },
            { "ren_upper_b", new Vector2(0.5f, 0.8152f) },
            { "ren_fore_b", new Vector2(0.5f, 0.8529f) },
        };
    }
}
