# Cenários 3, 6 e 7 — arte e sons

Rode dentro desta pasta (precisa de numpy, Pillow, scipy):

    python3 cenarios_finais.py   # gera out/Art/Cenario3, 6 e 7
    python3 sons_finais.py       # gera out/Audio/Salas/*.wav (junto com os sons dos cenários 4 e 5, via sons_salas.py)

Copie `out/Art/CenarioN` para `Assets/Art/CenarioN` e os `.wav` para `Assets/Audio/Salas`. As medidas usadas pelos builders
(`Assets/Editor/UltimaLinha/Cenario3|6|7SceneBuilder.cs`) são as mesmas deste script.
`artlib.py` aponta para as fontes em `Assets/UI/...` do projeto (ajuste o caminho se rodar em outro lugar).
