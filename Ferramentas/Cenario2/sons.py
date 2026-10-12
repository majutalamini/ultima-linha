"""Efeitos sonoros do Cenário 1, sintetizados (WAV 44.1 kHz mono)."""
import os
import wave
import numpy as np

OUT = 'out/Audio/Cenario1'
os.makedirs(OUT, exist_ok=True)
SR = 44100
rng = np.random.default_rng(11)


def write(name, x, gain=0.9):
    x = np.asarray(x, np.float64)
    peak = np.max(np.abs(x)) or 1
    x = x / peak * gain
    data = (np.clip(x, -1, 1) * 32767).astype('<i2')
    with wave.open(f'{OUT}/{name}.wav', 'wb') as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(SR)
        w.writeframes(data.tobytes())


def t(sec):
    return np.arange(int(sec * SR)) / SR


def lowpass(x, cutoff):
    a = np.exp(-2 * np.pi * cutoff / SR)
    y = np.empty_like(x)
    acc = 0.0
    for i, v in enumerate(x):
        acc = (1 - a) * v + a * acc
        y[i] = acc
    return y


def env(n, attack=0.005, decay=0.2):
    tt = np.arange(n) / SR
    return np.minimum(tt / attack, 1) * np.exp(-tt / decay)


def metal_hit(sec=0.6, f0=180, decay=0.18, partials=(1, 2.76, 5.4, 8.9)):
    tt = t(sec)
    x = sum(np.sin(2 * np.pi * f0 * p * tt + rng.random() * 6) / (i + 1) * np.exp(-tt / (decay / (1 + i * 0.6)))
            for i, p in enumerate(partials))
    click = rng.normal(0, 1, len(tt)) * np.exp(-tt / 0.008)
    return x + click * 0.6


def alavanca():
    # rangido curto + batida metálica no fim do curso
    a = t(0.18)
    creak = np.sin(2 * np.pi * (300 + 900 * a) * a) * (rng.normal(0, 0.4, len(a)) + 1) * 0.25 * np.sin(np.pi * a / 0.18)
    hit = metal_hit(0.7, 140, 0.22)
    return np.concatenate([creak, hit])


def beep(freq=740, sec=0.22):
    tt = t(sec)
    x = np.sin(2 * np.pi * freq * tt) + 0.25 * np.sin(2 * np.pi * freq * 2 * tt)
    return x * env(len(tt), 0.004, 0.09)


def erro():
    # zumbido grave (porta travando) + trinco
    tt = t(0.75)
    sq = np.sign(np.sin(2 * np.pi * 92 * tt)) * 0.5 + np.sign(np.sin(2 * np.pi * 97 * tt)) * 0.5
    sq = lowpass(sq, 900) * (0.6 + 0.4 * np.sin(2 * np.pi * 9 * tt)) * np.minimum(tt / 0.01, 1) * np.clip((0.75 - tt) / 0.1, 0, 1)
    clank = metal_hit(0.6, 210, 0.15)
    out = np.zeros(int(1.3 * SR))
    out[:len(sq)] += sq * 0.8
    out[int(0.55 * SR):int(0.55 * SR) + len(clank)] += clank * 0.9
    return out


def acerto():
    tt = t(0.35)
    return sum(np.sin(2 * np.pi * f * tt) * env(len(tt), 0.004, 0.16) for f in (523, 784))


def destrava():
    # trinco abrindo + ar comprimido + portas deslizando
    clank = metal_hit(0.5, 170, 0.12)
    n = int(2.4 * SR)
    hiss = rng.normal(0, 1, n)
    hiss = hiss - lowpass(hiss, 2500)
    tt = np.arange(n) / SR
    hiss *= np.clip(tt / 0.05, 0, 1) * np.exp(-tt / 0.5) * 0.5
    rumble = lowpass(rng.normal(0, 1, n), 120) * np.clip((tt - 0.3) / 0.2, 0, 1) * np.clip((2.2 - tt) / 0.4, 0, 1) * 3
    out = np.zeros(n + len(clank))
    out[:len(clank)] += clank
    out[int(0.25 * SR):int(0.25 * SR) + n] += hiss + rumble
    return out


def ambiente(sec=16.0):
    """Vagão parado: zumbido elétrico das luminárias, ar, rangidos distantes. Fecha em loop."""
    tt = t(sec)
    n = len(tt)
    hum = (np.sin(2 * np.pi * 60 * tt) * 0.5 + np.sin(2 * np.pi * 120 * tt) * 0.35 + np.sin(2 * np.pi * 180 * tt) * 0.12)
    hum *= 0.18 * (1 + 0.15 * np.sin(2 * np.pi * 0.25 * tt))
    air = lowpass(rng.normal(0, 1, n), 400) * 0.9
    # estalos/rangidos distantes do metal
    ev = np.zeros(n)
    for start in (2.3, 7.9, 12.6):
        h = metal_hit(1.6, 60 + rng.random() * 40, 0.6, (1, 1.5, 2.2)) * 0.25
        h = lowpass(h, 700)
        i = int(start * SR)
        ev[i:i + len(h)] += h[:n - i]
    x = hum + air + ev
    # loop sem emenda: cruza o final com o começo
    f = int(1.0 * SR)
    w = np.linspace(0, 1, f)
    x[:f] = x[:f] * w + x[-f:] * (1 - w)
    return x[:-f]


def blip():
    tt = t(0.03)
    return np.sin(2 * np.pi * 330 * tt) * env(len(tt), 0.001, 0.012)


def tremor_luz():
    """Estalo elétrico curto quando a luz falha."""
    n = int(0.25 * SR)
    x = rng.normal(0, 1, n) * (rng.random(n) > 0.92)
    tt = np.arange(n) / SR
    return lowpass(x, 3000) * np.exp(-tt / 0.08)


if __name__ == '__main__':
    write('alavanca', alavanca())
    write('beep_sequencia', beep(), 0.6)
    write('erro_porta_trava', erro())
    write('acerto', acerto(), 0.5)
    write('porta_destrava', destrava())
    write('ambiente_vagao', ambiente(), 0.5)
    write('dialogo_blip', blip(), 0.3)
    write('luz_falha', tremor_luz(), 0.5)
    print(os.listdir(OUT))
