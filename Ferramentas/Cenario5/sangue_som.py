# Passos no sangue do cenário 5: recorta o som gosmento enviado (origem/sangue_gosma.wav, 8 s com vários
# "esmagos") em três passos curtos, com entrada e saída suaves, para o BodyCrossing sortear a cada passo.
# Saída: Assets/Audio/Salas/sangue_passo_1..3.wav (mono, 16 bits).
import os
import wave
import numpy as np

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.normpath(os.path.join(AQUI, "..", ".."))
ORIGEM = os.path.join(AQUI, "origem", "sangue_gosma.wav")
SAIDA = os.path.join(RAIZ, "Assets", "Audio", "Salas")

# trechos escolhidos (segundos): os três esmagos mais encorpados
TRECHOS = [(0.62, 1.25), (1.62, 2.40), (3.12, 3.85)]
PICO = 0.6   # volume máximo de cada passo (o som original vem estourado, quase em 1.0)


def le(caminho):
    with wave.open(caminho) as w:
        sr, ch, n = w.getframerate(), w.getnchannels(), w.getnframes()
        x = np.frombuffer(w.readframes(n), np.int16).astype(np.float32) / 32768
    return x.reshape(-1, ch).mean(1), sr


def grava(caminho, x, sr):
    with wave.open(caminho, "wb") as w:
        w.setnchannels(1)
        w.setsampwidth(2)
        w.setframerate(sr)
        w.writeframes((np.clip(x, -1, 1) * 32767).astype(np.int16).tobytes())


def main():
    x, sr = le(ORIGEM)
    for i, (a, b) in enumerate(TRECHOS, 1):
        y = x[int(a * sr):int(b * sr)].copy()
        ent, sai = int(0.008 * sr), int(0.12 * sr)
        y[:ent] *= np.linspace(0, 1, ent)
        y[-sai:] *= np.linspace(1, 0, sai) ** 2
        y *= PICO / max(1e-6, np.abs(y).max())
        grava(os.path.join(SAIDA, f"sangue_passo_{i}.wav"), y, sr)
        print(f"sangue_passo_{i}.wav  {len(y) / sr:.2f} s")


if __name__ == "__main__":
    main()
