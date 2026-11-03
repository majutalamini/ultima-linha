"""Retoques à mão aplicados depois da extração: poucos pixels que a redução não preserva (olhos, óculos)."""

LINHA = (24, 19, 24, 255)
IRIS = (92, 94, 106, 255)
OCULOS = (40, 36, 44, 255)

CORES = {"k": LINHA, "e": IRIS, "o": OCULOS}

# id -> altura -> lista de (x, y, grade); '.' mantém o pixel extraído
PATCHES = {
    "kai": {222: [(32, 26, ("kkkk.",
                            ".kek."))]},
    "souta": {232: [(31, 21, ("kkkk",
                              "..ke"))]},
    "ren": {215: [(26, 26, ("oooo.ooo.",
                            ".....oeko",
                            "......ooo"))]},
}


def aplica(img, id_, h):
    px = img.load()
    for x0, y0, grade in PATCHES.get(id_, {}).get(h, []):
        for j, ln in enumerate(grade):
            for i, c in enumerate(ln):
                if c != ".":
                    px[x0 + i, y0 + j] = CORES[c]
