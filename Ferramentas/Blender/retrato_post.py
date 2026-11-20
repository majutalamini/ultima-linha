"""Pos-processa o busto renderizado pelo Blender no estilo dos retratos do jogo:
escuro, luz vermelha de contorno no lado esquerdo, brilho vermelho ao redor e fade na base.

python retrato_post.py <entrada_raw.png> <saida.png>
"""
import sys

import numpy as np
from PIL import Image, ImageFilter

RED = np.array([235, 28, 22], dtype=np.float32)


def smoothstep(a, b, x):
    t = np.clip((x - a) / (b - a), 0, 1)
    return t * t * (3 - 2 * t)


def main(src, dst):
    im = Image.open(src).convert("RGBA")
    w, h = im.size
    a = np.asarray(im.getchannel("A"), dtype=np.float32) / 255.0
    rgb = np.asarray(im.convert("RGB"), dtype=np.float32)

    # 1) mais escuro e frio; lado esquerdo recebe a luz vermelha
    xs = np.linspace(0, 1, w, dtype=np.float32)[None, :, None]
    red_wash = (1.0 - smoothstep(0.0, 0.85, xs)) * 0.30
    rgb = rgb * np.array([0.66, 0.64, 0.72], dtype=np.float32)
    rgb = rgb * (1 - red_wash) + RED * 0.55 * red_wash
    # lado direito um pouco mais escuro (luz vem da esquerda)
    rgb *= (1.0 - 0.28 * smoothstep(0.45, 1.0, xs))

    # 2) contorno vermelho nas bordas voltadas para a esquerda (e um pouco para cima)
    def shifted(arr, dx, dy):
        out = np.zeros_like(arr)
        ys0, ys1 = max(dy, 0), h + min(dy, 0)
        xs0, xs1 = max(dx, 0), w + min(dx, 0)
        out[ys0:ys1, xs0:xs1] = arr[ys0 - dy:ys1 - dy, xs0 - dx:xs1 - dx]
        return out

    rim = a * (1.0 - shifted(a, 8, 0))
    rim_img = Image.fromarray((np.clip(rim, 0, 1) * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(1.6))
    rim = np.asarray(rim_img, dtype=np.float32)[..., None] / 255.0 * 1.25
    rgb = rgb * (1 - np.clip(rim, 0, 1) * 0.95) + RED * np.clip(rim, 0, 1) * 0.95

    # 3) fade na base (todo o conteudo some perto da borda de baixo)
    ys = np.linspace(0, 1, h, dtype=np.float32)[:, None]
    fade = 1.0 - smoothstep(0.66, 0.98, ys)
    body_a = a * fade

    # 4) brilho vermelho suave atras do personagem, mais forte a esquerda
    glow_src = Image.fromarray((a * 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(46))
    glow = np.asarray(glow_src, dtype=np.float32) / 255.0
    glow = glow * (1.0 - smoothstep(0.1, 0.95, xs[:, :, 0])) * fade * 0.55
    glow = np.clip(glow * 1.6, 0, 0.55)

    # composicao: brilho por baixo, personagem por cima
    out_a = body_a + glow * (1 - body_a)
    out_rgb = (rgb * body_a[..., None] + RED * 0.8 * (glow * (1 - body_a))[..., None]) / np.maximum(out_a[..., None], 1e-5)
    out = np.dstack([np.clip(out_rgb, 0, 255), np.clip(out_a * 255, 0, 255)]).astype(np.uint8)
    Image.fromarray(out, "RGBA").save(dst)
    print("OK", dst)


if __name__ == "__main__":
    main(sys.argv[1], sys.argv[2])
