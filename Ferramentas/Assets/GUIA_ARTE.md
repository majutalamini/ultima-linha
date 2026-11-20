# Guia de arte: Última Linha

**Referências que valem:** os Cenários 0 a 2 e a plataforma do C3 (`vagao_antigo.png`, `estacao.png`). Tudo novo é comparado lado a lado com eles e com o Kai.

## Direção
Terror psicológico japonês, metrô antigo e abandonado de madrugada, vista lateral, pixel art. Silencioso e opressivo:
- azulejos brancos encardidos e metal enferrujado;
- marrom e verde-oliva dessaturados, cinzas frios, sombras quase pretas;
- luz fluorescente fria;
- vermelho escuro só em momentos marcados: emergência, sangue, o 04:17.

Nada de fantasia, cyberpunk, cores de plataforma ou terror cartunesco (fantasmas de lençol, olhos amarelos).

## Grade
- **58 px por unidade**, câmera ortográfica 4,655 (540 px de altura de arte, escala 2 exata em 1080p).
- Um pixel do fundo = um pixel do personagem. Nada de arte ampliada 2x ou 3x nem reduzida sem passar pela grade.
- Filtro **Point**, sem mipmap, sem compressão, transparência de 1 bit nos objetos.
- O Kai tem 222 px (3,2 u ≈ 1,75 m): confira a altura dos objetos ao lado dele.
- Pixel Perfect Camera não é usado: em resoluções que não são múltiplas de 540 ele mostraria além das bordas dos fundos.

## Paleta
- `paleta_oficial.png`: 80 cores, quase todas abaixo de 40 % de brilho.
- `paleta.prende(rgb, dither=...)` leva qualquer arte para ela.
- Um cenário pode somar poucos **acentos** que sejam pista ou leitura: o vermelho `B8322A` (letras e luz de emergência), o creme dos mostradores, a madeira (ver `ACENTOS` em `Ferramentas/Cenario4/c4_pixel.py`).

## Camadas (sortingOrder)
| Ordem | O quê |
|---|---|
| -130 a -100 | Fundo: parede, túnel, vagão |
| -96 a -90 | Peças na parede (porta da ponta -93, painel -94, capa da luminária -90, sombra passageira) |
| -62 a -40 | Portas, objetos de cenário, painéis |
| 2 a 10 | Personagens (NPCs 6, Kai 10), gotas e respingos (5) |
| 20 a 30 | Halos de luz, faíscas, poeira (sempre Unlit) |
| 40 / 50 | Balão de dica / tecla E |

## Luz
- **Renderizador:** 2D Renderer da URP (`Assets/Settings/URP_Renderer2D.asset`, criado por `Renderizador2D.cs`). O material padrão é **Unlit**: as cenas antigas ficam iguais.
- Para um cenário ganhar luz, ele chama `EfeitosBuild.LitNaCena()`. Os sprites com ordem abaixo de 20 viram Sprite-Lit, e os halos e a interface continuam Unlit.
- **Receita (C4):**
  - luz global fria a 0,4;
  - uma luz fria (`LuzOscilante`) embaixo de cada luminária, presa ao halo do `FluorescentLight`;
  - vermelho de emergência seguindo o halo que o diretor pulsa;
  - uma luz quente pequena só onde há uma fonte desenhada (mesa).
- **Normal maps:** só em fundos com relevo (canos, painéis). São gerados pela luminância (`normais()` em `c4_pixel.py`) e ligados como `_NormalMap` por `EfeitosBuild.Normais()`.
- Regiões escuras continuam legíveis: a luz global nunca vai a zero, e o que é pista (relógio, bilhete, alavancas) fica dentro de uma luz.

## Componentes (Assets/Scripts/Visual)
| Componente | Para quê | Montagem |
|---|---|---|
| `LuzOscilante` | Fluorescente com tremor por ruído e falhas aleatórias (Poisson), ou emergência pulsando; pode seguir um halo | `EfeitosBuild.Fluorescente`, `DeEmergencia` |
| `EmissorAmbiente` | Poeira, goteira, vapor, faísca e neblina em quadradinhos da grade; liga só com a câmera perto | `EfeitosBuild.Emissor`, `Goteira`, `Faiscas` |
| `EfeitoQuadros` | Sequência de quadros de vez em quando ou em laço (faísca, respingo, chiado de monitor) | `EfeitosBuild.Chiado` |
| `Tremor` | Porta, fio ou cano tremendo, com rangido opcional, arredondado na grade | adicionar ao objeto |
| `SombraPassageira` | Silhueta que cruza a parede quando o Kai chega perto, poucas vezes | `EfeitosBuild.Sombra` |
| `ParallaxLayer` (já existia) | Camadas de fundo | — |

Todos têm intensidade, cor, opacidade, velocidade, frequência ou intervalo, duração e distância de ativação no Inspector.

**Limites:** até cerca de 300 partículas por emissor, 2 emissores de poeira por tela e no máximo uma aparição por trecho.

## Pipeline de uma arte nova
1. Origem: uma arte do usuário (fundo verde `#00FF00`, como nos C0 a C3) ou a arte procedural antiga do projeto.
2. Um script em `Ferramentas/CenarioN/` recorta, reduz para a grade, prende na paleta e grava em `Assets/Art/CenarioN/...`. O original nunca é sobrescrito.
3. O SceneBuilder importa com 58 px/u e filtro Point (`ImportFolder(..., PixelArtImporter.Ppu, FilterMode.Point)`).
4. Teste com `PixelArtTeste` (Reconstruir e rodar), compare as fotos em `Ferramentas/PixelArt/previa` com as de antes e veja o Kai do lado.
