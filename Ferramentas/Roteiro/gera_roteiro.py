"""Gera o roteiro completo do jogo a partir do código (as falas moram nos construtores das cenas).

python gera_roteiro.py   ->   roteiro.md e roteiro.html nesta pasta

Roda de novo sempre que uma fala mudar: o documento nunca fica diferente do jogo.
"""
import html
import os
import re

AQUI = os.path.dirname(os.path.abspath(__file__))
RAIZ = os.path.dirname(os.path.dirname(AQUI))
ED = os.path.join(RAIZ, "Assets", "Editor", "UltimaLinha")
SCRIPTS = os.path.join(RAIZ, "Assets", "Scripts")

CENAS = [
    ("Abertura (Cenário 0)", "Cenario0SceneBuilder.cs", "Cenario1/Cenario0Director.cs"),
    ("Cenário 1 — O vagão parado", "Cenario1SceneBuilder.cs", "Cenario1/Cenario1Director.cs"),
    ("Cenário 2 — Os vagões em movimento", "Cenario2SceneBuilder.cs", None),
    ("Cenário 3 — A estação", "Cenario3SceneBuilder.cs", None),
    ("Cenário 4 — A sala de controle", "Cenario4SceneBuilder.cs", None),
    ("Cenário 5 — O escritório do chefe", "Cenario5SceneBuilder.cs", None),
    ("Cenário 6 — O túnel", "Cenario6SceneBuilder.cs", None),
    ("Cenário 7 — A última plataforma", "Cenario7SceneBuilder.cs", None),
]
NOMES = {"kai": "KAI", "souta": "SOUTA", "ren": "REN"}
CAMPOS = {
    "firstTalk": "primeira conversa", "repeatTalk": "conversas seguintes", "afterSolved": "depois do puzzle",
    "afterDoor": "depois que a porta abre", "first": "primeira vez", "again": "outras vezes", "intro": "abertura da cena",
    "doorLocked": "porta trancada", "firstFail": "primeiro erro", "lettersAppear": "as letras aparecem",
    "doorNeedsWord": "a porta pede a palavra", "allLetters": "todas as letras", "wordSolved": "palavra certa", "solved": "porta aberta",
    "goAhead": "pode ir na frente", "alone": "sozinho", "firstCrush": "primeira vez esmagado", "firstFall": "primeira queda",
    "braking": "o trem freia", "ending": "fim da cena", "doorClosed": "porta fechada", "allFound": "tudo encontrado",
    "doorUnlocked": "porta destrancada", "acorda": "acorda", "portaTrancada": "porta trancada", "antesDoPulo": "antes do pulo",
    "primeiraQueda": "primeira queda",
}

STR = r'"((?:[^"\\]|\\.)*)"'
TOKEN = re.compile(
    r"(?P<lines>(?P<alvo>[\w\.\[\]]+)\s*=\s*Lines\()"
    r"|(?P<fala>\(\s*\"(?P<quem>kai|souta|ren)\",\s*" + STR + r"\s*\))"
    r"|(?P<page>Page\(" + STR + r"(?P<titulo>, title: true)?\))"
    r"|(?P<texto>(?P<campo>\w+(?:Title|Body|Footer|Headline|Ticker|Channel|Subtitle\w*|title|subtitle))\s*=\s*(?P<valor>(?:" + STR + r"\s*\+?\s*)+);)"
    r"|(?P<opcoes>(?P<campo2>choiceOptions|choiceNotes)\s*=\s*new\[\]\s*\{(?P<lista>[^}]*)\})"
    r"|(?P<hud>(?:SetObjective|Toast)\(" + STR + r")"
    r"|(?P<diario>\(\"(?P<dtit>[^\"]+)\",\s*\n\s*" + STR + r"\))",
    re.S,
)


def limpa(s):
    s = s.replace('\\"', '"').replace("\\n", "\n")
    return s


def literais(trecho):
    return "".join(limpa(m.group(1)) for m in re.finditer(STR, trecho))


def rotulo(alvo):
    parte = alvo.split(".")
    campo = parte[-1]
    quem = parte[0] if len(parte) > 1 else ""
    desc = CAMPOS.get(campo, re.sub(r"([a-z])([A-Z])", r"\1 \2", campo).lower())
    quem = {"souta": "Souta", "ren": "Ren", "dir": "Cena", "painel": "Painel"}.get(quem, quem.capitalize())
    return f"{quem} — {desc}" if quem and quem != "Cena" else desc.capitalize()


def blocos(arquivo):
    """Itens em ordem: ('sec', título) | ('fala', quem, texto) | ('nota', texto) | ('pagina', texto, titulo?)"""
    s = open(arquivo, encoding="utf-8-sig").read()
    itens = []
    for m in TOKEN.finditer(s):
        if m.group("lines"):
            itens.append(("sec", rotulo(m.group("alvo"))))
        elif m.group("fala"):
            itens.append(("fala", m.group("quem"), limpa(m.group(m.re.groupindex["quem"] + 1))))
        elif m.group("page"):
            itens.append(("pagina", limpa(m.group(m.re.groupindex["page"] + 1)), bool(m.group("titulo"))))
        elif m.group("texto"):
            itens.append(("nota", f"{m.group('campo')}: {literais(m.group('valor'))}"))
        elif m.group("opcoes"):
            itens.append(("nota", f"{m.group('campo2')}: " + " | ".join(limpa(x) for x in re.findall(STR, m.group("lista")))))
        elif m.group("hud"):
            itens.append(("hud", limpa(m.group(m.re.groupindex["hud"] + 1))))
    return itens


def diario_kai():
    s = open(os.path.join(ED, "Cenario1SceneBuilder.cs"), encoding="utf-8-sig").read()
    a = s.index("PaginasDiario =")
    b = s.index("};", a)
    return [(t, limpa(x)) for t, x in re.findall(r'\("([^"]+)",\s*\n\s*' + STR + r"\)", s[a:b])]


def paginas_abertura():
    s = open(os.path.join(ED, "Cenario1SceneBuilder.cs"), encoding="utf-8-sig").read()
    a = s.index("PaginasAbertura()")
    b = s.index("};", a)
    return [("pagina", limpa(t), bool(ti)) for t, ti in re.findall(r"Page\(" + STR + r"(, title: true)?\)", s[a:b])]


def coleta():
    cenas = []
    for titulo, builder, diretor in CENAS:
        itens = blocos(os.path.join(ED, builder))
        if builder == "Cenario0SceneBuilder.cs":
            itens = [("sec", "Diário (cutscene de abertura)")] + paginas_abertura() + itens
        if builder == "Cenario1SceneBuilder.cs":
            # as páginas do diário de abertura já aparecem na Abertura
            itens = [i for i in itens if i[0] != "pagina"]
        if diretor:
            hud = [i for i in blocos(os.path.join(SCRIPTS, diretor)) if i[0] == "hud"]
            if hud:
                itens += [("sec", "Tutorial e objetivos na tela")] + hud
        cenas.append((titulo, itens))
    return cenas


def markdown(cenas, diario):
    out = ["# Última Linha — Roteiro completo", "",
           "_Gerado automaticamente a partir do código do jogo (Ferramentas/Roteiro/gera_roteiro.py). Falas em itálico são pensamentos do Kai._", ""]
    for titulo, itens in cenas:
        out += [f"## {titulo}", ""]
        for i in itens:
            if i[0] == "sec":
                out += ["", f"### {i[1]}", ""]
            elif i[0] == "fala":
                texto = i[2].replace("<i>", "*").replace("</i>", "*").replace("\n", " ")
                out.append(f"**{NOMES[i[1]]}:** {texto}  ")
            elif i[0] == "pagina":
                out.append(("#### " if i[2] else "> ") + i[1].replace("\n", " "))
            elif i[0] == "nota":
                out.append(f"- *{i[1].replace(chr(10), ' ')}*")
            elif i[0] == "hud":
                out.append(f"- `{i[1]}`")
        out.append("")
    out += ["## Diário do Kai (tecla Tab)", ""]
    for t, x in diario:
        out += [f"### {t}", "", x.replace("\n", "  \n"), ""]
    return "\n".join(out)


def pagina_html(cenas, diario):
    e = html.escape
    corpo = []
    for titulo, itens in cenas:
        corpo.append(f"<section><h2>{e(titulo)}</h2>")
        for i in itens:
            if i[0] == "sec":
                corpo.append(f"<h3>{e(i[1])}</h3>")
            elif i[0] == "fala":
                pensa = "<i>" in i[2]
                texto = e(i[2].replace("<i>", "").replace("</i>", "")).replace("\n", "<br>")
                corpo.append(f'<p class="fala {i[1]}{" pensa" if pensa else ""}"><b>{NOMES[i[1]]}</b><span>{texto}</span></p>')
            elif i[0] == "pagina":
                corpo.append(f'<p class="diario{" data" if i[2] else ""}">{e(i[1]).replace(chr(10), "<br>")}</p>')
            elif i[0] == "nota":
                corpo.append(f'<p class="nota">{e(i[1]).replace(chr(10), "<br>")}</p>')
            elif i[0] == "hud":
                corpo.append(f'<p class="hud">{e(i[1])}</p>')
        corpo.append("</section>")
    corpo.append("<section><h2>Diário do Kai (tecla Tab)</h2>")
    for t, x in diario:
        corpo.append(f'<h3>{e(t)}</h3><p class="diario">{e(x).replace(chr(10), "<br>")}</p>')
    corpo.append("</section>")
    return PAGINA.replace("{{CORPO}}", "\n".join(corpo))


PAGINA = """<title>Roteiro Última Linha</title>
<meta name="claude:viewport-fit-sides" content="cover">
<link rel="stylesheet" href="https://fonts.googleapis.com/css2?family=Cormorant+Garamond:ital,wght@0,500;0,600;1,500&family=Oswald:wght@400;500&display=swap">
<style>
/* Layout: uma coluna de leitura, como um script de teatro: nome de quem fala à esquerda, fala à direita */
:root{--fundo:#f3f1ec;--papel:#faf8f4;--texto:#1f1d1b;--suave:#68625b;--linha:#d9d3c9;--kai:#7a6a52;--souta:#3f5a78;--ren:#86672c;--hud:#2f5444;--sangue:#8e2a22;
--serif:"Cormorant Garamond",Georgia,"Times New Roman",serif;--rotulo:"Oswald","Arial Narrow",system-ui,sans-serif;--mono:ui-monospace,Consolas,monospace}
@media (prefers-color-scheme: dark){:root:not([data-theme="light"]){--fundo:#121110;--papel:#181715;--texto:#e8e1d4;--suave:#9b9488;--linha:#2d2a26;--kai:#c9bfae;--souta:#a9b8c9;--ren:#cbb98e;--hud:#8fc0a7;--sangue:#d0574b;color-scheme:dark}}
:root[data-theme="dark"]{--fundo:#121110;--papel:#181715;--texto:#e8e1d4;--suave:#9b9488;--linha:#2d2a26;--kai:#c9bfae;--souta:#a9b8c9;--ren:#cbb98e;--hud:#8fc0a7;--sangue:#d0574b;color-scheme:dark}
body{background:var(--fundo);color:var(--texto);font:19px/1.55 var(--serif);padding-left:max(16px,env(safe-area-inset-left,0px));padding-right:max(16px,env(safe-area-inset-right,0px))}
main{max-width:720px;margin:0 auto;padding-block:48px 96px}
.marca{font:500 13px var(--rotulo);letter-spacing:.32em;text-transform:uppercase;color:var(--sangue);margin:0 0 10px}
h1{font:600 44px/1.05 var(--serif);margin:0 0 10px;text-wrap:balance}
.sub{color:var(--suave);font-size:17px;margin:0 0 8px}
section{border-top:1px solid var(--linha);margin-top:44px;padding-top:26px}
h2{font:500 15px var(--rotulo);letter-spacing:.2em;text-transform:uppercase;margin:0 0 6px;text-wrap:balance}
h3{font:400 12px var(--rotulo);letter-spacing:.18em;text-transform:uppercase;color:var(--suave);margin:26px 0 8px}
.fala{display:grid;grid-template-columns:76px minmax(0,1fr);gap:14px;margin:5px 0}
.fala b{font:500 12px/2 var(--rotulo);letter-spacing:.16em}
.kai b{color:var(--kai)}.souta b{color:var(--souta)}.ren b{color:var(--ren)}
.pensa span{font-style:italic;color:var(--suave)}
.diario{font-style:italic;border-left:2px solid var(--linha);padding-left:14px;margin:6px 0}
.diario.data{font-style:normal;font-weight:600}
.nota{font:15px/1.5 var(--rotulo);font-weight:400;color:var(--suave);margin:6px 0}
.hud{font:14px var(--mono);color:var(--hud);margin:3px 0}
@media (max-width:520px){h1{font-size:34px}.fala{grid-template-columns:minmax(0,1fr);gap:0}}
</style>
<main>
<p class="marca">Última Linha</p>
<h1>Roteiro completo</h1>
<p class="sub">Todas as falas do jogo, na ordem das cenas. Em itálico, os pensamentos do Kai. Gerado a partir do código do jogo.</p>
{{CORPO}}
</main>
"""


def main():
    cenas = coleta()
    diario = diario_kai()
    open(os.path.join(AQUI, "roteiro.md"), "w", encoding="utf-8").write(markdown(cenas, diario))
    open(os.path.join(AQUI, "roteiro.html"), "w", encoding="utf-8").write(pagina_html(cenas, diario))
    falas = sum(1 for _, it in cenas for i in it if i[0] == "fala")
    assert falas > 100, falas  # se cair muito, algum construtor mudou de formato e o gerador perdeu falas
    print("falas", falas, "| por cena:", [sum(1 for i in it if i[0] == "fala") for _, it in cenas])


if __name__ == "__main__":
    main()
