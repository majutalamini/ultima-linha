"""Sons dos finais do Cenário 7 (WAV 44,1 kHz mono), feitos com as mesmas peças de Ferramentas/Cenarios367/sons_salas.py
mais o som gosmento enviado para o sangue (Ferramentas/Cenario5/origem/sangue_gosma.wav).

- c7_queda_vala    o Ren batendo no fundo da vala: baque surdo, ossos estalando, cascalho e o úmido do corpo
- c7_esmagamento   o trem passando por cima do Kai: pancada de metal, ossos sendo triturados, o úmido e o freio gritando
- c7_luta          a briga pela arma: o som enviado (origem/luta_objeto_quebrando.mp3), convertido para wav
Grava direto em Assets/Audio/Salas. Uso: python Ferramentas/Cenario7/sons_c7.py"""
import os
import sys
import wave

import numpy as np
from scipy.signal import resample_poly

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
sys.path.insert(0, os.path.join(RAIZ, "Ferramentas", "Cenarios367"))
os.chdir(os.path.join(RAIZ, "Ferramentas", "Cenarios367"))
import sons_salas as S  # noqa: E402
from sons_salas import SR, bp, env, lp, metal, noise, place, t, thump  # noqa: E402

SAIDA = os.path.join(RAIZ, "Assets", "Audio", "Salas")
rng = np.random.default_rng(717)


def gosma():
    with wave.open(os.path.join(RAIZ, "Ferramentas", "Cenario5", "origem", "sangue_gosma.wav")) as w:
        sr, ch, n = w.getframerate(), w.getnchannels(), w.getnframes()
        x = np.frombuffer(w.readframes(n), np.int16).astype(float).reshape(-1, ch).mean(1) / 32768
    return resample_poly(x, SR, sr) if sr != SR else x


GOSMA = None


def trecho_gosma(a, b, ganho=1.0):
    g = GOSMA[int(a * SR):int(b * SR)].copy()
    n = len(g)
    g *= np.minimum(np.arange(n) / (0.01 * SR), 1) * np.minimum((n - np.arange(n)) / (0.08 * SR), 1)
    return g / (np.abs(g).max() or 1) * ganho


def estalo(sec=0.05, lo=900, hi=5200, decay=0.012):
    n = int(sec * SR)
    return bp(noise(n), lo, hi) * env(n, 0.0004, decay)


def triturar(buf, inicio, dur, densidade, amp):
    """Uma sequência de estalos irregulares (ossos quebrando), cada vez mais fracos."""
    k = 0.0
    while k < dur:
        k += rng.exponential(1.0 / densidade)
        if k >= dur:
            break
        f = amp * (1 - k / dur) ** 0.7 * rng.uniform(0.5, 1.0)
        place(buf, estalo(0.06, rng.uniform(600, 1400), rng.uniform(3500, 5600), rng.uniform(0.006, 0.02)) * f, inicio + k)


def c7_queda_vala():
    buf = np.zeros(int(2.6 * SR))
    place(buf, thump(0.9, 52, 0.26, 0.55) * 1.3, 0.0)                         # o corpo bate
    place(buf, lp(noise(int(0.35 * SR)), 380) * env(int(0.35 * SR), 0.002, 0.09) * 1.1, 0.0)
    place(buf, estalo(0.08, 700, 5000, 0.02) * 1.1, 0.012)                    # o estalo seco
    triturar(buf, 0.03, 0.18, 70, 0.7)
    place(buf, trecho_gosma(0.66, 1.0, 0.55), 0.04)                           # o úmido
    for k in range(14):                                                       # cascalho espalhando
        place(buf, lp(metal(0.12, rng.uniform(900, 2400), 0.02, (1, 2.3)), 4000) * 0.12 * (1 - k / 14), 0.05 + k * 0.04 + rng.uniform(0, 0.03))
    place(buf, lp(metal(1.8, 172, 0.45, (1, 2.71, 5.1)), 2200) * 0.18, 0.02)  # o trilho ressoa
    place(buf, thump(0.4, 70, 0.1) * 0.4, 0.32)                               # um último assentar do corpo
    place(buf, trecho_gosma(3.2, 3.5, 0.3), 0.36)
    return lp(buf, 6000)


def c7_esmagamento():
    dur = 4.2
    buf = np.zeros(int(dur * SR))
    tt = t(dur)
    # o trem já em cima: rodas batendo nas juntas do trilho
    for k in range(10):
        place(buf, thump(0.25, 60, 0.07) * 0.5, k * 0.19)
    place(buf, thump(1.2, 42, 0.32, 0.4) * 1.4, 0.05)                         # a pancada
    place(buf, lp(metal(1.6, 118, 0.5, (1, 2.4, 4.9, 7.3)), 3000) * 0.7, 0.05)
    triturar(buf, 0.07, 0.9, 55, 1.0)                                         # ossos sendo triturados
    triturar(buf, 0.3, 0.6, 30, 0.6)
    place(buf, trecho_gosma(0.62, 1.25, 0.9), 0.1)                            # o úmido, mais de uma vez
    place(buf, trecho_gosma(1.62, 2.4, 0.75), 0.42)
    place(buf, trecho_gosma(3.1, 3.85, 0.55), 0.85)
    # o freio gritando, cada vez mais longe
    f = 1750 + 120 * np.sin(2 * np.pi * 3.3 * tt) + rng.normal(0, 20, len(tt)).cumsum() * 0.001
    grito = np.sin(2 * np.pi * np.cumsum(f) / SR) * 0.25 + bp(noise(len(tt)), 1500, 3800) * 0.25
    grito *= np.minimum(tt / 0.15, 1) * np.exp(-tt / 1.6)
    place(buf, grito, 0.0)
    return lp(buf, 6000)


def c7_luta():
    dur = 3.2
    buf = np.zeros(int(dur * SR))
    k = 0.0
    while k < dur - 0.2:                                                      # roupa raspando
        n = int(rng.uniform(0.08, 0.22) * SR)
        place(buf, bp(noise(n), 300, 2400) * env(n, 0.02, 0.06) * rng.uniform(0.25, 0.5), k)
        k += rng.uniform(0.07, 0.2)
    for at in np.sort(rng.uniform(0.1, dur - 0.3, 9)):                       # sapatos arrastando
        n = int(0.14 * SR)
        place(buf, bp(noise(n), 900, 3200) * env(n, 0.01, 0.05) * 0.35, at)
    for at in (0.35, 1.1, 1.7, 2.5):                                          # corpos se chocando
        place(buf, thump(0.3, 85, 0.07) * 0.7, at)
        place(buf, lp(noise(int(0.1 * SR)), 600) * env(int(0.1 * SR), 0.002, 0.03) * 0.5, at)
    for at in (0.8, 2.05):                                                    # a arma batendo
        place(buf, lp(metal(0.2, 1300, 0.03, (1, 2.6)), 4500) * 0.3, at)
    return lp(buf, 6000)


def luta_enviada():
    """O som da briga escolhido pela Maria Júlia (mp3), em mono 44,1 kHz."""
    import subprocess
    bruto = subprocess.run(["ffmpeg", "-loglevel", "error", "-i", os.path.join(AQUI, "origem", "luta_objeto_quebrando.mp3"),
                            "-ac", "1", "-ar", str(SR), "-f", "s16le", "-"], capture_output=True, check=True).stdout
    x = np.frombuffer(bruto, np.int16).astype(float) / 32768
    n = int(0.03 * SR)
    x[-n:] *= np.linspace(1, 0, n)
    return x


def grava(nome, x, ganho=0.85):
    x = np.asarray(x, float)
    x = x / (np.abs(x).max() or 1) * ganho
    with wave.open(os.path.join(SAIDA, nome + ".wav"), "wb") as w:
        w.setnchannels(1); w.setsampwidth(2); w.setframerate(SR)
        w.writeframes((np.clip(x, -1, 1) * 32767).astype("<i2").tobytes())
    print(nome, f"{len(x) / SR:.2f} s")


def main():
    global GOSMA
    GOSMA = gosma()
    grava("c7_queda_vala", c7_queda_vala(), 0.8)
    grava("c7_esmagamento", c7_esmagamento(), 0.85)
    grava("c7_luta", luta_enviada(), 0.6)   # c7_luta() (sintetizado) ficou de reserva


if __name__ == "__main__":
    main()
