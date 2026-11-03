"""
Confere o painel de desvios do Cenário 3 (mesmos números de Cenario3SceneBuilder.BuildSwitchPanel):
- na posição inicial a linha NÃO está completa;
- com todas as peças na posição de "masks" a corrente vai da PLATAFORMA (esquerda, linha 1) à ADMINISTRAÇÃO (direita, linha 2);
- quantos cliques (girando só no sentido horário) o caminho principal pede.
Uso: python3 painel_desvios.py
"""
N, E, S, W = 1, 2, 4, 8
R, C = 3, 5
MASKS = [S | E, W | E, W | S, W | S, N | S,
         W | N, N | E | S, N | S, N | E | W, W | N,
         N | E, W | E, N | E, W | E, W | E]
START = [1, 1, 2, 3, 0,
         2, 0, 1, 1, 1,
         2, 1, 3, 1, 1]
ENTRY, EXIT = 1, 2
STEP = {N: (-1, 0), E: (0, 1), S: (1, 0), W: (0, -1)}
OPP = {N: S, S: N, E: W, W: E}


def rot(m, r):
    for _ in range(r % 4):
        m = ((m << 1) | (m >> 3)) & 15
    return m


def solved(turns):
    m = lambda i: rot(MASKS[i], turns[i])
    start = ENTRY * C
    if not m(start) & W:
        return False
    seen, stack = {start}, [start]
    while stack:
        i = stack.pop()
        r, c = divmod(i, C)
        for d, (dr, dc) in STEP.items():
            rr, cc = r + dr, c + dc
            j = rr * C + cc
            if m(i) & d and 0 <= rr < R and 0 <= cc < C and j not in seen and m(j) & OPP[d]:
                seen.add(j)
                stack.append(j)
    last = EXIT * C + C - 1
    return last in seen and bool(m(last) & E)


assert not solved(START), 'a posição inicial já resolve o painel'
assert solved([0] * (R * C)), 'a solução não fecha a linha'
path = [5, 0, 1, 2, 7, 12, 13, 14]
clicks = sum(next(k for k in range(4) if rot(MASKS[i], START[i] + k) == MASKS[i]) for i in path)
print('ok: começa aberto, a solução fecha; caminho principal pede', clicks, 'cliques')
