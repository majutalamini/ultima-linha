"""Sons dos Cenários 4 e 5 (WAV 44,1 kHz mono). Tudo passa por filtro passa-baixa: nada de chiado."""
import os, wave
import numpy as np
from scipy.signal import butter, lfilter

OUT = 'out/Audio/Salas'
os.makedirs(OUT, exist_ok=True)
SR = 44100
rng = np.random.default_rng(45)


def write(name, x, gain=0.85):
    x = np.asarray(x, np.float64)
    x = x / (np.max(np.abs(x)) or 1) * gain
    with wave.open(f'{OUT}/{name}.wav', 'wb') as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((np.clip(x, -1, 1) * 32767).astype('<i2').tobytes())


def t(sec):
    return np.arange(int(sec * SR)) / SR


def lp(x, cutoff, order=3):
    b, a = butter(order, cutoff / (SR / 2), 'low')
    return lfilter(b, a, x)


def hp(x, cutoff, order=2):
    b, a = butter(order, cutoff / (SR / 2), 'high')
    return lfilter(b, a, x)


def bp(x, lo, hi, order=2):
    b, a = butter(order, [lo / (SR / 2), hi / (SR / 2)], 'band')
    return lfilter(b, a, x)


def env(n, attack=0.004, decay=0.15):
    tt = np.arange(n) / SR
    return np.minimum(tt / attack, 1) * np.exp(-tt / decay)


def noise(n):
    return rng.normal(0, 1, n)


def thump(sec=0.5, f0=70, decay=0.14, drop=0.5):
    tt = t(sec)
    f = f0 * (1 - drop * (1 - np.exp(-tt / 0.05)))
    return np.sin(2 * np.pi * np.cumsum(f) / SR) * np.exp(-tt / decay)


def metal(sec=0.5, f0=420, decay=0.12, partials=(1, 2.76, 5.4)):
    tt = t(sec)
    x = sum(np.sin(2 * np.pi * f0 * p * tt + rng.random() * 6) / (i + 1) * np.exp(-tt / (decay / (1 + i * 0.7))) for i, p in enumerate(partials))
    return x


def place(buf, x, at):
    i = int(at * SR)
    n = min(len(x), len(buf) - i)
    if n > 0:
        buf[i:i + n] += x[:n]
    return buf


# ---------------- Cenário 4 ----------------
def ambiente_sala():
    n = int(8 * SR); tt = t(8)
    x = 0.55 * np.sin(2 * np.pi * 48 * tt) + 0.25 * np.sin(2 * np.pi * 96 * tt + 1) + 0.12 * np.sin(2 * np.pi * 144 * tt)
    x *= 1 + 0.12 * np.sin(2 * np.pi * 0.25 * tt)
    buf = x.copy()
    for at, f in ((1.3, 95), (4.6, 80), (6.2, 110)):                     # canos batendo ao longe
        k = lp(metal(0.9, f, 0.22), 700) * 0.5
        place(buf, k, at)
    return lp(buf, 900)


def gerador_loop():
    tt = t(6)
    x = np.sin(2 * np.pi * 60 * tt) * 0.5 + np.sin(2 * np.pi * 120 * tt) * 0.28 + np.sin(2 * np.pi * 180 * tt) * 0.12
    x *= 1 + 0.2 * np.sin(2 * np.pi * 2 * tt)                               # vibração do motor
    x += 0.1 * np.sin(2 * np.pi * 30 * tt)
    return lp(x, 700)


def gerador_liga():
    sec = 4.2; tt = t(sec)
    buf = np.zeros(len(tt))
    place(buf, thump(0.7, 60, 0.2), 0.0)
    place(buf, lp(metal(0.5, 320, 0.1), 1500) * 0.6, 0.35)                 # relé
    f = 24 + 38 * (1 - np.exp(-tt / 1.3))
    x = np.sin(2 * np.pi * np.cumsum(f) / SR) * (1 - np.exp(-tt / 0.8)) * 0.8
    x += 0.35 * np.sin(2 * np.pi * np.cumsum(f * 2) / SR) * (1 - np.exp(-tt / 1.0))
    buf += x
    place(buf, thump(0.6, 70, 0.14) * 0.6, 1.6)
    return lp(buf, 800)


def cadeado_clique():
    k = env(int(0.05 * SR), 0.0006, 0.007) * (bp(noise(int(0.05 * SR)), 1200, 3800))
    return lp(k + metal(0.05, 1800, 0.01) * 0.3, 5000)


def cadeado_erro():
    buf = np.zeros(int(0.5 * SR))
    place(buf, thump(0.3, 110, 0.07) * 0.9, 0)
    place(buf, lp(metal(0.2, 240, 0.04), 1200) * 0.5, 0.0)
    place(buf, lp(metal(0.2, 200, 0.04), 1200) * 0.4, 0.14)
    return buf


def cadeado_abre():
    buf = np.zeros(int(0.9 * SR))
    for at in (0.0, 0.1, 0.19):
        place(buf, cadeado_clique() * 0.8, at)
    place(buf, lp(metal(0.5, 1300, 0.12, (1, 1.51, 2.3)), 4000) * 0.5, 0.3)    # mola
    place(buf, thump(0.3, 90, 0.07) * 0.8, 0.28)
    return buf


def gaveta_abre():
    sec = 0.9; n = int(sec * SR); tt = t(sec)
    scrape = bp(noise(n), 160, 900) * (np.minimum(tt / 0.1, 1) * np.exp(-tt / 0.5)) * (1 + 0.5 * np.sin(2 * np.pi * 22 * tt))
    buf = scrape * 0.9
    place(buf, thump(0.3, 80, 0.07) * 0.8, 0.58)
    return lp(buf, 1400)


def chave_pega():
    buf = np.zeros(int(1.0 * SR))
    for at, f in ((0.0, 2100), (0.07, 2650), (0.14, 1900), (0.26, 2400)):
        place(buf, lp(metal(0.4, f, 0.07, (1, 2.4, 3.9)), 7000) * 0.6, at)
    return buf


def papel():
    n = int(0.6 * SR); tt = t(0.6)
    x = bp(noise(n), 400, 3200) * np.abs(np.sin(2 * np.pi * 7 * tt + 1)) ** 2 * np.exp(-tt / 0.28)
    return lp(x, 3500)


def vazio_encaixe():
    buf = np.zeros(int(0.6 * SR))
    place(buf, lp(metal(0.5, 260, 0.1), 900) * 0.8, 0)
    place(buf, thump(0.2, 140, 0.05) * 0.5, 0)
    return buf


# ---------------- Cenário 5 ----------------
def ambiente_escritorio():
    sec = 10; n = int(sec * SR); tt = t(sec)
    x = 0.4 * np.sin(2 * np.pi * 50 * tt) + 0.2 * np.sin(2 * np.pi * 100 * tt + 2)
    x *= 1 + 0.1 * np.sin(2 * np.pi * 0.2 * tt)
    buf = x.copy()
    tick = lp(env(int(0.04 * SR), 0.0005, 0.006) * bp(noise(int(0.04 * SR)), 800, 2600) + metal(0.04, 1400, 0.008) * 0.2, 3000) * 0.5
    for s in range(sec):                                                    # relógio na parede, um tique por segundo
        place(buf, tick * (1.0 if s % 2 == 0 else 0.8), s + 0.02)
    place(buf, lp(thump(1.2, 40, 0.5), 200) * 0.5, 6.6)                      # rangido grave ao longe
    return lp(buf, 1500)


def coracao():
    bpm = 74; beat = 60 / bpm; beats = 6
    buf = np.zeros(int(beat * beats * SR))
    for b in range(beats):
        at = b * beat
        place(buf, lp(thump(0.25, 58, 0.07), 160) * 1.0, at)
        place(buf, lp(thump(0.2, 50, 0.06), 140) * 0.7, at + 0.22)
    return buf


def arma_pega():
    buf = np.zeros(int(0.6 * SR))
    place(buf, lp(metal(0.4, 900, 0.05, (1, 2.2, 4.1)), 5000) * 0.8, 0)
    place(buf, thump(0.2, 120, 0.04) * 0.7, 0.02)
    return buf


def arma_engatilha():
    buf = np.zeros(int(0.8 * SR))
    place(buf, lp(metal(0.2, 1500, 0.03, (1, 2.6, 4.7)), 6000) * 0.8, 0.0)
    place(buf, lp(metal(0.3, 1100, 0.05, (1, 2.3, 3.8)), 5000) * 1.0, 0.18)
    place(buf, thump(0.15, 160, 0.03) * 0.5, 0.18)
    return buf


def porta_batida():
    buf = np.zeros(int(1.6 * SR))
    place(buf, thump(1.0, 52, 0.28), 0)
    place(buf, lp(metal(0.8, 190, 0.22, (1, 2.1, 3.3)), 1400) * 0.5, 0)
    place(buf, lp(noise(int(0.15 * SR)) * env(int(0.15 * SR), 0.002, 0.03), 2200) * 0.5, 0)
    place(buf, lp(metal(0.6, 140, 0.18), 900) * 0.3, 0.55)                    # vibração da porta
    return buf


def porta_tranca():
    buf = np.zeros(int(1.2 * SR))
    place(buf, lp(metal(0.3, 700, 0.05, (1, 2.4, 4.4)), 4500) * 0.9, 0.0)
    place(buf, thump(0.2, 100, 0.05) * 0.7, 0.02)
    place(buf, lp(metal(0.5, 520, 0.09, (1, 2.2)), 4000) * 1.0, 0.4)            # o ferrolho
    place(buf, thump(0.2, 90, 0.05) * 0.8, 0.4)
    return buf


def batida_na_porta():
    buf = np.zeros(int(2.0 * SR))
    for at in (0.0, 0.28, 0.56):
        place(buf, lp(thump(0.3, 140, 0.05), 400) * 0.9, at)
        place(buf, lp(noise(int(0.08 * SR)) * env(int(0.08 * SR), 0.001, 0.015), 1400) * 0.4, at)
    return buf


def alavanca_pega():
    buf = np.zeros(int(0.7 * SR))
    place(buf, lp(metal(0.5, 1700, 0.06, (1, 2.5, 4.1)), 6000) * 0.5, 0)
    place(buf, thump(0.2, 90, 0.04) * 0.5, 0.01)
    return buf


SOUNDS = dict(ambiente_sala=ambiente_sala, gerador_loop=gerador_loop, gerador_liga=gerador_liga, cadeado_clique=cadeado_clique,
              cadeado_erro=cadeado_erro, cadeado_abre=cadeado_abre, gaveta_abre=gaveta_abre, chave_pega=chave_pega, papel=papel,
              vazio_encaixe=vazio_encaixe, ambiente_escritorio=ambiente_escritorio, coracao=coracao, arma_pega=arma_pega,
              arma_engatilha=arma_engatilha, porta_batida=porta_batida, porta_tranca=porta_tranca, batida_na_porta=batida_na_porta,
              alavanca_pega=alavanca_pega)

if __name__ == '__main__':
    for name, fn in SOUNDS.items():
        write(name, fn())
        print('ok', name)
