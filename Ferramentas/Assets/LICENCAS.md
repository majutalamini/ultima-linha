# Assets de terceiros: fontes, licenças e uso

Conferido em 11/11/2026, nas páginas oficiais. Os arquivos baixados ficam em `Ferramentas/Assets/originais/<pacote>/`, intactos e **fora do git**: as licenças da IceMaan e da Willosuna não deixam redistribuir os arquivos, e o repositório é público. Para regenerar o que o jogo usa, baixe os pacotes nessas pastas e rode `python Ferramentas/Assets/trata.py`.

## Usados no jogo

| Pacote | Autor e fonte | Licença | O que entra | Alterações |
|---|---|---|---|---|
| Free Pixel VFX Essentials | IceMaan, https://icemaan.itch.io/pixel-vfx-essentials (`Pixel VFX Essentials.rar`, 3,3 MB, preço livre, baixado por 0) | Uso comercial e pessoal livre; pode modificar; atribuição não é obrigatória. **Proibido** revender ou redistribuir os efeitos como pacote, ou subir em outra loja ou em conjunto de treino de IA | `Lightning/Spark Burst` vira `Assets/Art/Efeitos/faisca_0..4.png`; `Ambient/Puddle Splash` vira `respingo_0..4.png` | Recorte da folha em quadros de 32 px e troca do matiz azul por branco-amarelado (faísca) ou cinza frio (respingo), mantendo brilho e alfa. Sem reescala: 32 px ficam do tamanho certo na grade de 58 px |

## Baixados, mas fora do jogo

| Pacote | Autor e fonte | Licença | Por que ficou de fora |
|---|---|---|---|
| Free Halloween Horror VFX | IceMaan, https://icemaan.itch.io/free-halloween-horror-vfx (`Halloween Horror VFX.rar`, 319 kB) | A mesma da IceMaan acima | Fantasmas, olhos e mãos em estilo cartunesco, contra o guia de arte. Fica para consulta |
| Pixel Weather VFX Lite | Willosuna, https://willosuna.itch.io/pixel-weather-vfx-lite (`pixel-weather-vfx-lite-rate-r2.zip`, 1,5 MB) | Uso comercial livre, pode editar, atribuição não é obrigatória. **Proibido** compartilhar os arquivos, originais ou modificados | Neblina e respingo foram feitos por código (`EmissorAmbiente`); como o repositório é público, nada desse pacote entra nele |
| Horror Tile Set | Ansimuz, https://opengameart.org/content/horror-tile-set (`grunge-tileset-files.zip`, 77 kB) | CC0 | Tiles de 16 px: precisariam ser ampliados, quebrando a regra "1 pixel do fundo = 1 pixel do Kai". Serve de referência de textura |

## Avaliados e não baixados

| Pacote | Licença | Motivo |
|---|---|---|
| Industrial Punk (GiMiCkO) | CC0 | Tiles de 16 px e cidade colorida com grafite: escala e estilo |
| Haunted Nightmares (SakPix) | Pago (US$ 4); a demo grátis tem 80 peças | Pacote pago, e o 8 bits grosso não combina |
| Factory and Industrial Tileset (reodesuxz) | Grátis | Robôs, laser e sci-fi colorido, em 16 px |
| Kenney Pixel Platformer Industrial Expansion | CC0 | Plataforma colorida, tiles de 18 px |
| Industrial Parallax Background (Ansimuz) | CC0 | Exterior industrial noturno, e o jogo se passa debaixo da terra |
| Seoul Subway Free Mini (glowcompany) | Uso comercial; proibido revender | Só ícones e dois tiles; nada de vista lateral |

## Feito no projeto (sem terceiros)

- Paleta oficial (`paleta.py`): as 80 cores de `estacao.png` e `vagao_antigo.png`.
- Tecla E (`Ferramentas/Interface/tecla_e.py`), chiado de monitor e silhueta (`trata.py`), corredor do portão e sombra da porta do C3 (`Ferramentas/Cenario3/profundidade.py`), arte do C4 em pixel art (`Ferramentas/Cenario4/c4_pixel.py`, a partir da arte procedural antiga do próprio projeto).
- Poeira, goteira, vapor, faísca e neblina são partículas de quadradinhos na grade (`EmissorAmbiente`).
