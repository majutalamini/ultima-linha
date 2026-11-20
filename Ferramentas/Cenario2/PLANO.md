# Cenário 2 — plano de refinamento

Status: **implementado (3/11)**. Piloto automático do começo ao fim sem erros: cadeado aos 16 s, passagens 1/2/3 em 5,6/10,2/5,3 s, luz do vagão 3 aos 42 s, saída aos 61 s (o piloto não erra; jogando à mão leva bem mais).

Mudanças em relação ao plano: o engate ficou mais largo (3,8 u) e os buracos da passagem 2 menores (1,9 u), porque o pulo correndo do 1º buraco passava do engate e caía no 2º. A caixa de fusíveis cheia foi montada a partir da vazia (Ferramentas/Cenario2/objetos_arte.py). O "Continuar" no meio do Cenário 2 recomeça o cenário do início (não guarda os puzzles feitos).

## 1. Como testar só o Cenário 2

- **Jogar à mão:** menu `Última Linha > Abrir Cenário 2` e Play. O cenário começa sozinho, sem passar pela Abertura nem pelo Cenário 1.
- **Piloto automático (sem som):** menu `Última Linha > Testes > Jogar o Cenário 2 sozinho`. Ele passa as falas, corre, espera cada porta abrir, pula os vãos e sai pela porta do fim. Anota no Console o tempo de cada passagem e quantas vezes o Kai foi esmagado ou caiu. Fotos em `Ferramentas/PixelArt/previa/jogo_c2_*.png`. Seu "Continuar" do menu é guardado e devolvido no fim.
- Por linha de comando: `-executeMethod UltimaLinha.EditorTools.PixelArtTeste.SoCenario2` (com janela; não mexer na Unity até ela fechar sozinha).

### Resultado do teste de hoje (3/11)
- Entrada (falas) até o controle: 10,5 s. **Do controle até o último vagão: 14 s.** Passagens a 2,0 s, 4,2 s e 4,4 s uma da outra. **0 esmagadas, 0 quedas.** Saiu pela porta do fim aos 35 s e carregou o Cenário 3.
- Ou seja: correndo, o cenário inteiro passa em menos de meio minuto, sem nenhum desafio real e sem nada no caminho. As portas quase nunca obrigam a esperar.
- Fotos: `jogo_c2_comeco`, `jogo_c2_passagem1..3`, `jogo_c2_fim`.

## 2. Comparação com os Cenários 0 e 1

| | Cenários 0 e 1 (prontos) | Cenário 2 (hoje) |
|---|---|---|
| Fundo | `vagao_antigo.png` em pixel art, na grade dos personagens (58 px/u), reduzido para caber na tela (`EscalaVagao`) | `cenario2_fundo.png` antigo (pintura a 100 px/u, sem redução): o vagão aparece **maior** que no Cenário 1, os personagens parecem menores |
| Entre vagões | Ponta do vagão em corte (`ponta_esq/dir`), porta de dobradiça animada (`porta_ponta_0..5`), plataforma do engate com corrimão, túnel ao fundo (`tunel.png`), luz de trava verde/vermelha | Retângulos desenhados por código: "sanfona" preta com brilho vermelho, chapas cinza, faixas zebradas e buracos pretos. **É a parte mais feia do jogo hoje** |
| Coisas para ler/olhar | Letras em vermelho pelo vagão, painel de emergência, letreiro, mostrador, diário (Tab) | Nada. Só o diário |
| Puzzles | C1: alavancas na ordem do letreiro + alavanca emperrada (mostrador) + palavra MORTE | Só o tempo das portas (3 passagens) |
| Souta e Ren | Ficam no vagão, têm falas antes, durante e depois de cada puzzle, dão dica quando o jogador trava | Somem depois da entrada (fade preto) e só reaparecem no fim |
| Conversas pós-puzzle | Sim (letras aparecem, palavra certa, porta abriu) | Não |

## 3. O novo Cenário 2 (proposta)

O trem anda. Quatro vagões, três passagens. **Em cada vagão tem algo para ler, um puzzle e uma conversa quando ele é resolvido.** Souta e Ren vão sempre **um vagão na frente**: o Kai os reencontra no começo de cada vagão (em vez de sumirem até o fim).

### Vagão 1 — "O horário" (puzzle de código)
- A porta da ponta está presa por um **cadeado de rodas** de 4 números (reaproveita `DigitLockUI` dos Cenários 4/5).
- **Para ler:**
  - *Aviso da operadora* (placa na parede): "Com o trem em movimento, as portas entre vagões abrem e fecham com o balanço. Não permaneça no passadiço." (ensina a mecânica lendo)
  - *Celular caído* (no chão, tela acesa): 11 chamadas perdidas de "Chefe". Última mensagem: "Preciso falar com você. Amanhã, 9h, sala 704." Hora da mensagem: **04:17**.
  - *Relógio de parede parado* em **4:17**.
- Código: **0417**.
- **Conversa ao abrir:** Ren: "Quatro e dezessete... é o número do meu crachá." / Souta: "Coincidência. Esse trem é cheio delas." / Kai: *"O mesmo número do celular. Do relógio. Do crachá."* (prepara o "Quatro e dezessete, Kai. Você lembra?" do Cenário 7).
- **Passagem 1:** tutorial. As duas portas abrem juntas, devagar; vão curto.

### Vagão 2 — "Segurar a porta" (puzzle de alavanca + corrida)
- As portas da passagem 2 batem rápido demais para passar. No fundo do vagão, longe da porta, tem uma **alavanca de retenção** (reaproveita a alavanca do Cenário 1): puxando, a porta A fica aberta por ~6 s e o sinal dela fica verde. O Kai tem que puxar e correr. A porta B continua alternando: espera em cima do **engate**, no meio do vão (arte nova).
- **Para ler:** *Mapa da linha* (cartaz em cima da porta lateral). As estações são lugares da vida do Kai: "Empresa — Clínica Dr. Mori — Apartamento 12 — ... — TERMINAL". A última estação não tem nome, só "Última Linha".
- **Conversa ao passar:** Souta: "Viu? Tudo tem um mecanismo." / Ren: "Você sempre fala isso. Mas quem puxou foi o Kai." / Kai: *"Ele nunca agradece. Nunca precisou."*

### Vagão 3 — "Luzes" (puzzle de achar e encaixar)
- O vagão está quase apagado (as luzes já falham hoje). Sem energia, a **luz de aviso das portas da passagem 3 não acende**: não dá para ver quando elas vão bater.
- **Caixa de fusíveis** vazia na parede. **3 fusíveis** espalhados, cada um junto de uma coisa para ler:
  1. Em cima do bagageiro (subindo no encosto, como no Cenário 1): *bolsa com crachá* — "Kai — Operações — mesa 12". A foto do crachá está riscada.
  2. Embaixo do banco: *caixa de presente com cartão* — "Feliz aniversário, Kai. Mesmo dia que o meu, lembra? — S." (o diário diz que os dois fazem aniversário em 20 de outubro).
  3. No chão do fim do vagão: *frasco de remédio vazio* — "Dr. Mori. Tomar às 8h e às 20h. Não interromper." (o diário: "Parei há três semanas").
- Encaixa os 3 → as luzes voltam, os avisos das portas acendem **e aparece escrito em vermelho na parede** (como as letras do Cenário 1): "ELE NÃO ESTAVA NO TREM".
- **Conversa:** Ren: "Isso... estava escrito antes?" / Souta: "Não olha, Ren. Vamos." / Kai: *"Ele quem?"*
- **Passagem 3:** a mais difícil (portas irregulares, vão grande, precisa correr).

### Vagão 4 — Fim (já existe, ajustado)
- O trem freia; pelas janelas aparece a **plataforma da estação** (arte nova) em vez do túnel. Fala final atual mantida.
- Dica do jogador travado (como no Cenário 1): depois de 2 mortes seguidas na mesma passagem ou ~75 s parado num vagão, Souta/Ren ficam com o balão "..." e dão uma dica.

### Passagens (os "negócios de pular") — novo visual
- Trocar o desenho por código pelo **mesmo kit dos Cenários 0 e 1**: ponta do vagão em corte + porta de dobradiça + plataforma com corrimão + túnel ao fundo.
- A porta de dobradiça **bate com o tranco** (os 6 quadros de `porta_ponta` já existem): abre, sinal amarelo pisca, bate. Se bater com o Kai no vão da porta, ele volta ao começo do vagão (como hoje).
- O **túnel corre** atrás (parallax do `tunel.png` + faixas de luz passando) para vender que o trem está andando; no vão, um pouco de faísca dos trilhos.
- Passagem 2 ganha o **engate** visível no meio do vão (onde se espera a outra porta).

### Fundo dos vagões
- Os 4 vagões passam a usar o `vagao_antigo.png` (mesma arte do Cenário 1, na mesma escala), recortado em comprimentos diferentes para não ficar repetitivo. Vagão 3 mais escuro. Vagão 4 com a estação nas janelas no fim.

## 4. Ordem do trabalho (depois da aprovação)
1. Fundo novo + passagens com o kit dos Cenários 0/1 (colisores, câmera e checkpoints refeitos).
2. Portas de dobradiça batendo (adaptar `CarDoor` para animar `porta_ponta`).
3. Souta e Ren um vagão na frente + dicas.
4. Puzzles 1, 2 e 3 com leituras e conversas.
5. Fim com a estação nas janelas.
6. Roteiro (`Ferramentas/Roteiro/roteiro.md`) regenerado, piloto automático atualizado para os puzzles, teste completo e fotos.

## 5. Artes para gerar

Mesmo jeito das artes de objetos do Cenário 1: **uma peça por imagem, fundo verde puro (#00FF00)**, pixel art, vista de lado. O script recorta o verde, reduz para a grade de 58 px e prende na paleta do vagão.

**Anexe em todos os pedidos (referência de estilo):**
- `Ferramentas/Cenario1/referencias/objetos/painel_emergencia.png`
- `Ferramentas/PixelArt/previa/jogo_cenario1_conversa.png`

Coloque as imagens geradas em `Ferramentas/Cenario2/referencias/objetos/` com o nome indicado.

**Texto base (cole no começo de cada prompt):**
> Pixel art de um objeto isolado para um jogo de terror 2D de vista lateral, dentro de um vagão de metrô japonês antigo, abandonado e sujo. Mesmo estilo, traço, tamanho de pixel e paleta das imagens anexas: tons de cinza, marrom e ferrugem dessaturados, sombras quase pretas, toques de vermelho escuro. Vista de frente/de lado, sem perspectiva de cima. Fundo verde puro #00FF00 liso, sem sombra no chão, sem texto, sem moldura. Só o objeto, centralizado.

| Arquivo | Prompt (depois do texto base) | Anexo extra |
|---|---|---|
| `engate.png` | O engate entre dois vagões de trem visto de lado: uma barra de acoplamento grossa de ferro com a garra no meio, mangueiras de ar penduradas e uma corrente frouxa, enferrujado. Comprido na horizontal (proporção 3:1). Em cima da barra, uma chapa estreita e gasta onde dá para ficar em pé. | `Assets/Art/Cenario1/Objetos/ponta_dir.png` e `Ferramentas/PixelArt/previa/jogo_cenario0_vao.png` |
| `relogio.png` | Relógio de parede redondo de vagão de trem, moldura de metal, vidro rachado, ponteiros parados marcando 4:17. | — |
| `celular.png` | Celular antigo caído de lado no chão, tela rachada e acesa com um brilho azul fraco, sem texto legível. | — |
| `cadeado_rodas.png` | Trava de porta de trem com cadeado de segredo de 4 rodas numeradas, presa numa chapa de metal com parafusos e uma corrente passando pela maçaneta. | — |
| `mapa_linha.png` | Cartaz de mapa de linha de metrô emoldurado, papel amarelado e manchado, uma linha vermelha com bolinhas de estação, sem texto legível. Proporção 3:1 na horizontal. | — |
| `aviso_operadora.png` | Placa de aviso de metal, retangular, com um pictograma de porta e um ponto de exclamação, tinta descascada, sem texto legível. | — |
| `caixa_fusiveis_vazia.png` | Caixa de fusíveis de parede aberta, portinhola de metal pendurada, três encaixes vazios em fila, fios soltos. | `Assets/Art/Cenario1/Objetos/painel_emergencia.png` |
| `caixa_fusiveis_cheia.png` | **Mesma caixa da imagem anexa**, igual em tudo, mas com três fusíveis cerâmicos encaixados e uma luzinha verde acesa. | a `caixa_fusiveis_vazia.png` que você gerou |
| `fusivel.png` | Um fusível cerâmico cilíndrico de trem com as pontas de metal, pequeno, com um brilho fraco em volta. | — |
| `bolsa_cracha.png` | Bolsa de trabalho de couro caída e aberta, com um crachá de empresa pendurado para fora pela fita. | — |
| `presente.png` | Caixa de presente pequena amassada, papel escuro, laço vermelho desfeito e um cartão dobrado encostado nela. | — |
| `remedio.png` | Frasco de remédio de farmácia de plástico laranja translúcido, vazio, tampa caída do lado, rótulo branco sem texto legível. | — |
| `estacao_janela.png` | Vista de uma plataforma de estação de metrô subterrânea vazia, como vista pela janela de um trem parado: parede de azulejos brancos sujos, uma placa de estação escura sem texto, luz fluorescente fria, banco vazio. **Proporção 21:9**, sem fundo verde (preenche a imagem toda). | `Ferramentas/Cenario2/referencias/notion_cenario2.jpg` |

As letras vermelhas na parede ("ELE NÃO ESTAVA NO TREM") são feitas no jogo com a fonte de letra de mão, não precisa gerar.
