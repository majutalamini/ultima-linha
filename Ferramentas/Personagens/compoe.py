"""Compõe um render transparente sobre fundo escuro para inspeção."""
import sys

from PIL import Image

n = sys.argv[1]
im = Image.open(f"build/{n}.png").convert("RGBA")
bg = Image.new("RGBA", im.size, (40, 40, 42, 255))
bg.alpha_composite(im)
bg.save(f"build/{n}_v.png")
