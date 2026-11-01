# Cenários 4 e 5

Scripts que desenham a arte e geram os sons (sem IA, tudo por código; Python com numpy, PIL e scipy).

- `cenario4.py`: sala de controle (fundo, folhas da porta, gaveta aberta) → `out/Art/Cenario4/`
- `cenario5.py`: escritório (fundo, folhas da porta, gaveta de arquivo, pistola) → `out/Art/Cenario5/`
- `sons_salas.py`: sons dos dois cenários → `out/Audio/Salas/`
- `artlib.py`: ferramentas de desenho usadas pelos dois.

Rode a partir desta pasta: `python3 cenario4.py && python3 cenario5.py && python3 sons_salas.py`.
Depois copie `out/Art/CenarioN` para `Assets/Art/` e `out/Audio/Salas` para `Assets/Audio/`.
As medidas do cenário estão repetidas nos builders (`Cenario4SceneBuilder.cs`, `Cenario5SceneBuilder.cs`): se mudar um número aqui, mude lá.
