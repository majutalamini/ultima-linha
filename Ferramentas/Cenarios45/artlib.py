"""Ferramentas de desenho (numpy + PIL) para os cenários 4 e 5: pintura escura, texturizada, sem animação."""
import numpy as np
from PIL import Image, ImageDraw, ImageFilter, ImageFont

FONT_SANS = '/home/claude/ultima-linha/Assets/UI/MainMenu/Fonts/Oswald-Regular.ttf'
FONT_SERIF = '/home/claude/ultima-linha/Assets/UI/MainMenu/Fonts/CormorantGaramond-Medium.ttf'
FONT_HAND = '/home/claude/ultima-linha/Assets/UI/Cutscene/Fonts/Caveat-Medium.ttf'


def noise(h, w, s, seed):
    r = np.random.default_rng(seed).normal(0, 1, (h, w)).astype(np.float32)
    if s > 0:
        im = Image.fromarray(np.clip(r * 40 + 128, 0, 255).astype(np.uint8)).filter(ImageFilter.GaussianBlur(s))
        r = np.asarray(im).astype(np.float32) - 128
    return r / (r.std() + 1e-6)


class Canvas:
    def __init__(self, w, h, base=(20, 20, 22)):
        self.w, self.h = w, h
        self.a = np.zeros((h, w, 3), np.float32) + np.array(base, np.float32)

    # ---- primitivas ----
    def _clip(self, x0, y0, x1, y1):
        return max(0, int(x0)), max(0, int(y0)), min(self.w, int(x1)), min(self.h, int(y1))

    def rect(self, x0, y0, x1, y1, color, tex=0.0, seed=1, alpha=1.0):
        x0, y0, x1, y1 = self._clip(x0, y0, x1, y1)
        if x1 <= x0 or y1 <= y0:
            return
        reg = self.a[y0:y1, x0:x1]
        c = np.array(color, np.float32)
        new = np.zeros_like(reg) + c
        if tex:
            new += noise(y1 - y0, x1 - x0, 1.0, seed + x0 + y0)[..., None] * tex
        self.a[y0:y1, x0:x1] = reg * (1 - alpha) + new * alpha

    def grad(self, x0, y0, x1, y1, c0, c1, axis=0, tex=0.0, seed=2, alpha=1.0):
        x0, y0, x1, y1 = self._clip(x0, y0, x1, y1)
        if x1 <= x0 or y1 <= y0:
            return
        reg = self.a[y0:y1, x0:x1]
        n = (y1 - y0) if axis == 0 else (x1 - x0)
        t = np.linspace(0, 1, n, dtype=np.float32)
        c0 = np.array(c0, np.float32)
        c1 = np.array(c1, np.float32)
        ramp = c0[None, :] * (1 - t[:, None]) + c1[None, :] * t[:, None]
        new = np.broadcast_to(ramp[:, None, :], reg.shape).copy() if axis == 0 else np.broadcast_to(ramp[None, :, :], reg.shape).copy()
        if tex:
            new += noise(y1 - y0, x1 - x0, 1.0, seed + x0)[..., None] * tex
        self.a[y0:y1, x0:x1] = reg * (1 - alpha) + new * alpha

    def _mask_paint(self, mask, color, alpha=1.0, tex=0.0, seed=3):
        m = (np.asarray(mask).astype(np.float32) / 255.0) * alpha
        col = np.zeros_like(self.a) + np.array(color, np.float32)
        if tex:
            col += noise(self.h, self.w, 1.0, seed)[..., None] * tex
        self.a = self.a * (1 - m[..., None]) + col * m[..., None]

    def _bbox_mask(self, drawfn, bbox, blur=0.0):
        x0, y0, x1, y1 = self._clip(*bbox)
        if x1 <= x0 or y1 <= y0:
            return None, (x0, y0, x1, y1)
        im = Image.new('L', (x1 - x0, y1 - y0), 0)
        d = ImageDraw.Draw(im)
        drawfn(d, -x0, -y0)
        if blur:
            im = im.filter(ImageFilter.GaussianBlur(blur))
        return np.asarray(im).astype(np.float32) / 255.0, (x0, y0, x1, y1)

    def _paint_local(self, m, box, color, alpha=1.0, tex=0.0, seed=3):
        if m is None:
            return
        x0, y0, x1, y1 = box
        reg = self.a[y0:y1, x0:x1]
        col = np.zeros_like(reg) + np.array(color, np.float32)
        if tex:
            col += noise(y1 - y0, x1 - x0, 1.0, seed + x0)[..., None] * tex
        mm = (m * alpha)[..., None]
        self.a[y0:y1, x0:x1] = reg * (1 - mm) + col * mm

    def poly(self, pts, color, alpha=1.0, tex=0.0, blur=0.0):
        xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
        pad = int(blur * 3) + 2
        bbox = (min(xs) - pad, min(ys) - pad, max(xs) + pad, max(ys) + pad)
        m, box = self._bbox_mask(lambda d, ox, oy: d.polygon([(x + ox, y + oy) for x, y in pts], fill=255), bbox, blur)
        self._paint_local(m, box, color, alpha, tex)

    def ellipse(self, cx, cy, rx, ry, color, alpha=1.0, tex=0.0, blur=0.0):
        pad = int(blur * 3) + 2
        bbox = (cx - rx - pad, cy - ry - pad, cx + rx + pad, cy + ry + pad)
        m, box = self._bbox_mask(lambda d, ox, oy: d.ellipse([cx - rx + ox, cy - ry + oy, cx + rx + ox, cy + ry + oy], fill=255), bbox, blur)
        self._paint_local(m, box, color, alpha, tex)

    def line(self, pts, color, width=2, alpha=1.0):
        xs = [p[0] for p in pts]; ys = [p[1] for p in pts]
        bbox = (min(xs) - width - 2, min(ys) - width - 2, max(xs) + width + 2, max(ys) + width + 2)
        m, box = self._bbox_mask(lambda d, ox, oy: d.line([(x + ox, y + oy) for x, y in pts], fill=255, width=width), bbox)
        self._paint_local(m, box, color, alpha)

    def round_rect(self, x0, y0, x1, y1, r, color, alpha=1.0, tex=0.0):
        m, box = self._bbox_mask(lambda d, ox, oy: d.rounded_rectangle([x0 + ox, y0 + oy, x1 + ox, y1 + oy], r, fill=255), (x0, y0, x1, y1))
        self._paint_local(m, box, color, alpha, tex)

    def text(self, x, y, s, size, color, font=FONT_SANS, alpha=1.0, anchor='la', spacing=0, rot=0):
        f = ImageFont.truetype(font, size)
        im = Image.new('L', (int(size * (len(s) + 2) * 0.9) + 20, int(size * 2) + 10), 0)
        d = ImageDraw.Draw(im)
        d.text((10, 10), s, font=f, fill=255)
        if rot:
            im = im.rotate(rot, resample=Image.BICUBIC, expand=True)
        bb = im.getbbox()
        if not bb:
            return
        im = im.crop(bb)
        arr = np.asarray(im).astype(np.float32) / 255.0
        w, h = im.size
        if anchor[0] == 'm':
            x = x - w // 2
        if len(anchor) > 1 and anchor[1] == 'm':
            y = y - h // 2
        x0, y0, x1, y1 = self._clip(x, y, x + w, y + h)
        if x1 <= x0 or y1 <= y0:
            return
        arr = arr[y0 - int(y):y1 - int(y), x0 - int(x):x1 - int(x)]
        reg = self.a[y0:y1, x0:x1]
        self.a[y0:y1, x0:x1] = reg * (1 - arr[..., None] * alpha) + np.array(color, np.float32) * arr[..., None] * alpha

    # ---- luz e acabamento ----
    def glow(self, cx, cy, rx, ry, color, strength=1.0):
        pad = 2
        x0, y0, x1, y1 = self._clip(cx - rx - pad, cy - ry - pad, cx + rx + pad, cy + ry + pad)
        if x1 <= x0 or y1 <= y0:
            return
        yy, xx = np.mgrid[y0:y1, x0:x1].astype(np.float32)
        d = np.sqrt(((xx - cx) / rx) ** 2 + ((yy - cy) / ry) ** 2)
        f = np.clip(1 - d, 0, 1) ** 2.2 * strength
        self.a[y0:y1, x0:x1] += f[..., None] * np.array(color, np.float32)

    def cone(self, x, y0, y1, w0, w1, color, strength=1.0):
        """Feixe de luz vertical do teto (trapézio com borda suave)."""
        x0, x1 = int(x - max(w0, w1) / 2), int(x + max(w0, w1) / 2)
        x0c, y0c, x1c, y1c = self._clip(x0, y0, x1, y1)
        if x1c <= x0c or y1c <= y0c:
            return
        yy, xx = np.mgrid[y0c:y1c, x0c:x1c].astype(np.float32)
        t = (yy - y0) / max(1.0, (y1 - y0))
        w = (w0 + (w1 - w0) * t) / 2
        dx = np.abs(xx - x)
        edge = np.clip(1 - (dx / np.maximum(w, 1)) ** 2, 0, 1)
        f = edge * (1 - t) ** 1.4 * strength
        self.a[y0c:y1c, x0c:x1c] += f[..., None] * np.array(color, np.float32)

    def vignette(self, strength=0.5, x_only=False):
        yy, xx = np.mgrid[0:self.h, 0:self.w].astype(np.float32)
        nx = (xx / self.w - 0.5) * 2
        ny = (yy / self.h - 0.5) * 2
        v = 1 - strength * np.clip((nx ** 2 if x_only else (nx ** 2 * 0.35 + ny ** 2 * 0.9)), 0, 1.5)
        self.a *= v[..., None]

    def grime(self, seed=7, amount=10, streaks=True):
        n = noise(self.h, self.w, 9, seed) * 0.7 + noise(self.h, self.w, 2.2, seed + 1) * 0.5
        self.a *= (1 + n[..., None] * amount / 255.0 * 3)
        if streaks:
            rng = np.random.default_rng(seed)
            for _ in range(int(self.w / 60)):
                x = rng.integers(0, self.w); y0 = rng.integers(0, int(self.h * 0.6)); L = rng.integers(60, 320)
                wd = rng.integers(2, 7)
                x0, y0c, x1, y1c = self._clip(x, y0, x + wd, y0 + L)
                if x1 > x0 and y1c > y0c:
                    fall = np.linspace(1, 0, y1c - y0c)[:, None, None]
                    self.a[y0c:y1c, x0:x1] *= (1 - 0.35 * fall)

    def finish(self, grain=2.5, seed=11):
        self.a += noise(self.h, self.w, 0.6, seed)[..., None] * grain
        return Image.fromarray(np.clip(self.a, 0, 255).astype(np.uint8), 'RGB')


def save_rgba(arr_rgb, alpha, path):
    """arr_rgb: HxWx3 float, alpha: HxW 0..1"""
    rgba = np.dstack([np.clip(arr_rgb, 0, 255), np.clip(alpha, 0, 1) * 255]).astype(np.uint8)
    Image.fromarray(rgba, 'RGBA').save(path)
