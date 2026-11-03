"""Prévias de inspeção: referência contra sprite, rostos de perto e tamanho real sobre o Cenário 1."""
import os

from PIL import Image

import sprite

P = sprite.ASSETS
TRIO = (("kai", "Kai/kai_base_222.png"), ("souta", "Souta/souta_base_232.png"), ("ren", "Ren/ren_base_215.png"))


def main():
    sh = Image.new("RGBA", (1500, 1000), (36, 36, 38, 255))
    rostos = Image.new("RGBA", (1560, 520), (36, 36, 38, 255))
    x = 10
    for i, (id_, f) in enumerate(TRIO):
        ref = Image.open(os.path.join(sprite.AQUI, "referencias", f"{id_}_direcao.png")).convert("RGBA")
        b = Image.new("RGBA", ref.size, (36, 36, 38, 255))
        b.alpha_composite(ref)
        sh.alpha_composite(b.crop((240, 30, 640, 1760)).resize((222, 960), Image.LANCZOS), (x, 20))
        x += 225
        s = Image.open(os.path.join(P, f))
        g = s.resize((s.width * 4, s.height * 4), Image.NEAREST)
        sh.alpha_composite(g, (x, 990 - g.height))
        x += g.width + 30
        c = s.crop((0, 0, s.width, 62))
        rostos.alpha_composite(c.resize((c.width * 8, c.height * 8), Image.NEAREST), (20 + i * 510, 12))
    sh.convert("RGB").save(os.path.join(sprite.PREVIA, "tres_ref_vs_sprite.png"))
    rostos.convert("RGB").save(os.path.join(sprite.PREVIA, "tres_rostos.png"))


if __name__ == "__main__":
    main()
