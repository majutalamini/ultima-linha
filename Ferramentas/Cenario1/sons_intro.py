"""Sons da cutscene de abertura: drone grave durante o texto e o último trem chegando."""
import numpy as np, wave, os
from sons import write, lowpass, metal_hit, t, SR, rng

def drone(sec=20.0):
    tt = t(sec)
    x = (np.sin(2*np.pi*55*tt)*0.5 + np.sin(2*np.pi*82.4*tt)*0.25 + np.sin(2*np.pi*110.3*tt)*0.12)
    x *= 0.6 + 0.4*np.sin(2*np.pi*0.07*tt)
    x += lowpass(rng.normal(0, 1, len(tt)), 300) * 1.2
    f = int(2*SR); w = np.linspace(0, 1, f)
    x[:f] = x[:f]*w + x[-f:]*(1-w)
    return x[:-f]

def trem(sec=9.0):
    """Trem chegando e passando: ronco grave que cresce, batidas dos trilhos, chiado do freio no fim."""
    tt = t(sec); n = len(tt)
    env = np.clip(tt/5.0, 0, 1)**1.6 * np.clip((sec-tt)/1.8, 0, 1)
    rumble = lowpass(rng.normal(0, 1, n), 160) * 6
    body = lowpass(rng.normal(0, 1, n), 900) * 0.8
    x = (rumble + body) * env
    # batidas das juntas do trilho (pares), acelerando
    clacks = np.zeros(n); tm = 1.0; gap = 0.9
    while tm < sec-1.5:
        for d in (0.0, 0.12):
            h = lowpass(metal_hit(0.25, 90+rng.random()*30, 0.05, (1, 2.1, 3.3)), 1500)
            i = int((tm+d)*SR); clacks[i:i+len(h)] += h[:n-i]
        tm += gap; gap = max(0.32, gap*0.85)
    x += clacks * env * 1.5
    # freio: chiado agudo no fim
    st = sec-3.0; m = tt > st
    sq = np.sin(2*np.pi*(2400+300*np.sin(2*np.pi*3*tt))*tt) * np.clip((tt-st)/0.6, 0, 1) * np.clip((sec-tt)/1.2, 0, 1)
    x += sq * m * 0.18
    return x

if __name__ == '__main__':
    write('intro_drone', drone(), 0.45)
    write('trem_ultimo', trem(), 0.9)
    print(sorted(os.listdir('out/Audio/Cenario1')))
