# Prompts dos fundos dos Cenários 4 a 7

Cada cenário é dividido em **painéis**: imagens largas em 21:9, colocadas lado a lado da esquerda para a direita. Cada painel vale a largura de uma parede do saguão do C3.

- **Móveis fixos vêm pintados no fundo**, no lugar exato onde está o colisor.
- **Não pinte o que se mexe** (alavancas, portas, gavetas, o corpo do chefe, as anteparas do túnel): isso entra por cima, no jogo.

**Anexe em TODOS os pedidos:**
- `Assets/Art/Cenario3/Objetos/parede_saguao.png`: estilo, paleta, tamanho do pixel e **enquadramento**.
- `Assets/Art/Cenario2/Objetos/estacao.png`: a mesma estação.

**Texto base (cole no começo de cada prompt):**
> Pixel art de fundo para um jogo de terror psicológico 2D de vista lateral, numa estação de metrô japonesa antiga, abandonada e suja, de madrugada. Mesmo estilo, traço, tamanho de pixel, paleta e enquadramento das imagens anexas: azulejos brancos encardidos, metal enferrujado, marrom e verde-oliva dessaturados, cinzas frios, sombras quase pretas, luz fluorescente fria vinda de cima, toques de vermelho escuro. Vista totalmente de frente/de lado, sem perspectiva de cima e sem ponto de fuga. Proporção 21:9, preenchendo a imagem toda, sem moldura, sem texto legível, sem pessoas. **Mesma altura de chão da imagem anexa**: a base da parede um pouco acima da linha onde os personagens pisam, com uma faixa de chão de ladrilho molhado com reflexos visível na frente da parede, até a borda de baixo. Teto escuro com canos e duas luminárias fluorescentes compridas, nas mesmas alturas da imagem anexa. As bordas esquerda e direita cortam uma coluna no meio, para emendar com o painel vizinho.

As posições estão em **% da largura do painel, da esquerda**. O personagem tem a altura de uma porta pequena (~1,75 m).

Salve em `Ferramentas/Cenarios4a7/referencias/` com o nome indicado.

---

## Cenário 4: sala de controle (2 painéis)

| Arquivo | Prompt (depois do texto base) |
|---|---|
| `c4_painel_a.png` | Sala de controle de funcionários dentro da estação. Parede de azulejos com meia-parede de chapa escura embaixo. **10–18 %**: quatro armários de vestiário de metal verde-acinzentado, um entreaberto. **34 %**: um papel preso na parede com alfinete vermelho, na altura da cabeça. **38–56 %**: mesa de supervisor de metal com tampo de madeira, gaveteiros dos dois lados, papéis e telefone velho em cima. **64 %**: relógio de parede redondo, grande, no alto da parede, acima da cabeça, de mostrador creme. **89–97 %**: caixote de madeira de manutenção, baixo (altura da cintura). |
| `c4_painel_b.png` | Continuação da mesma sala. **0–33 %**: passarela de grade metálica com guarda-corpo de canos, na altura do ombro do personagem, sobre pernas de metal até o chão, com faixa amarela e preta de aviso embaixo do deque. **35–48 %**: gerador industrial verde-escuro, da altura do peito, com mostrador redondo, aletas, cabo grosso e faixa de aviso. **52–55 %**: painel de energia na parede, com quatro fendas verticais de encaixe de alavanca, a última com fios soltos. **57–69 %**: só a parede lisa e o batente vazio de uma porta (a porta entra por cima no jogo). **70–100 %**: parede com canos e uma luz vermelha de emergência apagada no alto. |

## Cenário 5: escritório do chefe (2 painéis)

| Arquivo | Prompt (depois do texto base) |
|---|---|
| `c5_painel_a.png` | Um andar de escritórios da companhia, abandonado: carpete escuro e manchado no lugar do ladrilho, parede de painéis de madeira escura até meia altura e papel de parede bege descascando em cima, persianas quebradas. **20–35 %**: mesas de escritório comuns com computadores velhos de tubo, cadeiras caídas. **65–72 %**: um arquivo de aço alto de quatro gavetas, com uma plaquinha vazia em cima. **85–100 %**: a mesa grande do diretor, de madeira escura, com abajur verde aceso e papéis espalhados; atrás dela, uma janela de vidro fosco escuro. |
| `c5_painel_b.png` | Continuação do mesmo escritório. **5–30 %**: chão livre de carpete, com uma mancha escura (deixe o espaço livre: o corpo entra por cima). **35–42 %, 43–50 %, 52–59 %**: três arquivos de aço altos de quatro gavetas lado a lado, cada um com uma plaquinha vazia em cima. **66–75 %**: o batente vazio de uma porta de madeira (a porta entra por cima). **76–100 %**: parede com quadros tortos e um vaso de planta morta. |

## Cenário 6: o túnel (3 painéis, o do meio repete)

| Arquivo | Prompt (depois do texto base, **trocando** a parte da estação por: *túnel de metrô de concreto, de lado, com trilhos no chão*) |
|---|---|
| `c6_inicio.png` | Começo do túnel de serviço: parede curva de concreto com manchas de umidade, cabos grossos presos na parede, chão de cascalho com um passadiço de concreto onde os personagens andam. **20–30 %**: um painel de tração ferroviário grande na parede, de metal, com um encaixe de alavanca vazio no meio e luzes apagadas. Lâmpadas de túnel em gaiola, de luz fria, espaçadas. |
| `c6_meio.png` | O mesmo túnel, só o trecho reto: parede de concreto, cabos e lâmpadas de gaiola espaçadas, para repetir lado a lado sem emenda (as bordas esquerda e direita combinam). Sem objetos. |
| `c6_fim.png` | Fim do túnel: o mesmo concreto. **60–80 %**: a frente do último vagão de um trem antigo parado, de lado, com a porta da ponta aberta e luz fraca lá dentro. Depois do trem, só escuro. |

## Cenário 7: a última plataforma (2 painéis)

| Arquivo | Prompt (depois do texto base) |
|---|---|
| `c7_painel_a.png` | A plataforma final, mais velha e mais escura que as outras, com azulejos faltando e água escorrendo pela parede. **30–50 %**: um banco de plataforma quebrado. **70 %**: um cartaz rasgado e ilegível. |
| `c7_painel_b.png` | Continuação. **20–35 %**: uma moldura grande e vazia de placa de estação na parede (o nome entra no jogo). **40 %**: relógio de estação redondo no alto, de mostrador creme. **0–38 %**: a plataforma continua; em **38 %** ela **termina numa beirada com faixa amarela**, e depois dela há um fosso fundo com trilhos lá embaixo (o fosso desce além da borda de baixo da imagem). **65–90 %**: a boca escura de um túnel do outro lado dos trilhos. |

---

## O que entra por cima (não precisa gerar)

Arte que já existe ou que eu monto ou busco:
- alavancas (Cenário 1);
- porta da administração com batente e corredor (Cenário 3);
- gaveta aberta;
- placas legíveis dos arquivos e o "ÚLTIMA LINHA" (texto no jogo);
- anteparas do túnel (porta do C2);
- luzes, poeira, goteira, vapor e faíscas (efeitos do jogo).

O **corpo do chefe** (C5) continua com a arte atual até eu achar coisa melhor.
