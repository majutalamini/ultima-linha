"""Gera Assets/Editor/UltimaLinha/RigLayout.cs a partir do rig_layout.json (pivôs e medidas do esqueleto)."""
import json
L = json.load(open('out/Art/Cenario1/Rig/rig_layout.json'))
SK = L['_skeleton']
lines = []
for cid in ('kai', 'souta', 'ren'):
    for part, v in L[cid].items():
        lines.append(f'            {{ "{cid}_{part}", new Vector2({v["pivot"][0]}f, {v["pivot"][1]}f) }},')
u = lambda px: f'{px/100:.4f}f'
cs = f'''// GERADO por Ferramentas/Cenario1/rig_cs.py a partir de rig_layout.json. Não edite à mão.
using System.Collections.Generic;
using UnityEngine;

namespace UltimaLinha.EditorTools
{{
    /// <summary>Pivôs das partes dos personagens articulados e medidas do esqueleto (em unidades).</summary>
    public static class RigLayout
    {{
        public const float PixelsPerUnit = {L["_ppu"]}f;
        public const float Thigh = {u(SK["thigh"])}, Shin = {u(SK["shin"])}, Ankle = {u(SK["ankle"])};
        public const float Upper = {u(SK["upper"])}, Fore = {u(SK["fore"])};
        public static readonly Vector2 Hip = new Vector2({u(SK["hip"][0])}, {u(SK["hip"][1])});
        public static readonly Vector2 Shoulder = new Vector2({u(SK["shoulder"][0])}, {u(SK["shoulder"][1])});
        public static readonly Vector2 Neck = new Vector2({u(SK["neck"][0])}, {u(SK["neck"][1])});
        public static float HipHeight => Thigh + Shin + Ankle;

        public static readonly Dictionary<string, Vector2> Pivots = new Dictionary<string, Vector2>
        {{
{chr(10).join(lines)}
        }};
    }}
}}
'''
open('/home/claude/proj/Assets/Editor/UltimaLinha/RigLayout.cs', 'w').write(cs)
print(cs[:900])
