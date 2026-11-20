# Cenário 3 — plano de refinamento

Status: **implementado (4/11)**. Piloto automático do começo ao fim sem erros, sem travar e sem teleporte: portão aos 24 s (errando 0417 antes de 2350), quatro peças aos 41 s, linha completa aos 46 s, saída aos 51,5 s (o piloto não pensa nem lê; jogando à mão deve levar uns 5 a 8 minutos). Antes/depois: `Ferramentas/Cenario3/comparacao_depois.png`.

### O que ficou diferente do plano
- **Cartaz da clínica** foi para a plataforma, dentro da moldura vazia de `estacao.png` (ele é vertical como a moldura); o **mapa da rede** (o `mapa_linha` do C2) foi para a parede vazia da plataforma.
- **Mezanino sem a escada** que veio desenhada (ela era íngreme demais para o Kai andar): sobe-se pelos caixotes, agora **de mão única** como os bancos do C1. O chão do mezanino fica acima da cabeça do Kai (3,9 u).
- **Painel:** os trilhos fora do caminho já começam certos; o caminho pede 10 giros no mínimo (o piloto, só com E, dá 20).
- **Teto:** a parede da estação tem a altura da tela, mas o chão fica 35 px acima da borda de baixo (como no C2); em cima de cada parede o script espelha as vigas e escurece até o preto.
- **Souta e Ren** andam na frente depois de cada puzzle; quando eles e o lugar de chegada saem da câmera, vão direto para lá (as falas e os papéis param o tempo do jogo, e andando eles ficariam muito para trás do Kai correndo).
- O "Continuar" no meio do cenário guarda portão, moeda, peças e linha (`GameSave.Mark`). **Não testado** no piloto.

### Bugs achados pelo piloto e corrigidos
- Encostado nos caixotes o pulo não saía (resolvido com os caixotes de mão única).
- Ao fechar, o teclado do portão (e o painel) devolvia o controle ao Kai no meio da animação: dava para sair andando, abrir um papel e o papel cortava a conversa do portão, que nunca terminava (Souta e Ren nunca saíam da plataforma). Agora o diretor espera o teclado sumir antes de prender o Kai.
- Souta e Ren começavam em cima da moeda e roubavam o E dela; agora ficam entre o bilhete e a moeda, à vista no primeiro quadro.

### Resultado final do piloto (4/11)
| Parte | Tempo do piloto |
|---|---|
| Entrada: a porta do trem bate atrás do Kai, conversa | 4,5 s |
| Dica ligada à força: balão nos dois, falar com o Souta desliga | ok |
| Bilhete (peça 1), moeda, placa, cartaz, mapa | aos 14,7 s |
| Quadro de partidas (peça 2) | aos 17,5 s |
| Portão: 0417 (armadilha, fala do Kai), depois 2350; conversa | aos 24,2 s |
| Comunicado, achados e perdidos | aos 32,4 s |
| Máquina com a moeda (peça 3) | aos 34,2 s |
| Caixotes → mezanino: escala, monitor (peça 4); conversa das peças | aos 40,6 s |
| Painel: 20 giros / 30 teclas | aos 45,6 s |
| Porta abre (Souta e Ren já na porta), conversa, sai | **51,5 s, 0 erros, 0 travas** |

Não testado no piloto: o Ren entregando a última peça (só acontece faltando uma), o código errado comum (só a armadilha), o "Continuar" e o som.

## 1. Como testar só o Cenário 3

- **Jogar à mão:** menu `Última Linha > Abrir Cenário 3` e Play.
- **Piloto automático (sem som):** menu `Última Linha > Testes > Jogar o Cenário 3 sozinho`. Ele passa as falas, **anda de verdade** (teclado simulado) até cada lugar de E em ordem de x, erra o portão com 0417 e acerta com 2350, sobe os caixotes até o mezanino (pula quando há uma beirada alcançável à frente e segura o ESPAÇO enquanto sobe), abre o painel, gira os trilhos com as setas e o E e sai pela porta. Anota no Console o tempo de cada parte e onde travou; se ficar 6 s parado, fotografa (`jogo_c3_travouN`) e teleporta até o alvo. Fotos em `Ferramentas/PixelArt/previa/jogo_c3_*.png`. Seu "Continuar" é guardado e devolvido no fim.
- Por linha de comando: `-executeMethod UltimaLinha.EditorTools.PixelArtTeste.SoCenario3` (ou `ReconstruirC3ERodar`, que recria a cena antes). Com janela; não mexer na Unity até ela fechar sozinha.
- Artes: `python Ferramentas/Cenario3/objetos_arte.py` (prévia das peças com o Kai em `previa_objetos.png`).

### Resultado do teste da fase 1 (3/11, cenário antigo)
| Parte | Tempo do piloto |
|---|---|
| Entrada (falas puladas na hora) | 4,0 s |
| Quadro de horários (peça 1) | 1,8 s |
| Bilhete (peça 2) | 3,1 s |
| Subir os caixotes até o guichê do mezanino (peça 3) | 6,0 s |
| Recibo da máquina (peça 4) | 2,9 s |
| Até a porta, painel aberto | 2,2 s |
| Painel: 32 giros / 52 teclas, sem errar | 0,4 s |
| Porta abre (fala) e sai | 5,4 s |
| **Total** | **27,8 s, 0 erros** |

- **Onde trava: nos caixotes.** Encostado neles, o Kai **não consegue pular**: o motor de física do template prende o Kai na parede e corta a subida a cada quadro (o pulo chega a 0,1 u). Quem segura D contra os caixotes e aperta ESPAÇO fica parado ali. Só sobe quem recua um passo e pula de longe. O piloto agora faz isso; antes ele travava sempre no mesmo ponto (x = 27,3).
- O piloto não pensa: uma pessoa lê os 4 papéis e resolve o painel (são 15 trilhos, 32 giros se girar tudo) em uns **3 a 5 minutos**. Mesmo assim, o cenário é um corredor reto com 4 papéis e um puzzle só.
- Fotos: `jogo_c3_comeco`, `_horarios`, `_bilhete`, `_mezanino`, `_guiche`, `_recibo`, `_porta`, `_fim`. Lado a lado com os Cenários 1 e 2: `Ferramentas/Cenario3/comparacao.png`.

## 2. Comparação com os Cenários 0, 1 e 2

| | Cenários 0, 1 e 2 (prontos) | Cenário 3 (hoje) |
|---|---|---|
| Escala e grade | Tudo na grade dos personagens: 58 px por unidade, câmera 4,655. Um pixel do fundo = um pixel do Kai | Personagens a 58 px/u, mas o fundo é `cenario3_fundo.png` a **100 px/u** (4800×941): pixels do fundo menores e lisos, como um desenho vetorial ao lado dos personagens em pixel art |
| Fundo | Pixel art escura e suja (vagao_antigo, pontas, túnel). O C2 termina na **estação de `estacao.png`**: azulejos encardidos, colunas com faixa vermelha, banco vermelho, placa escura | Pintura antiga verde-acinzentada, clara e limpa, outra estação. **Quebra a continuidade**: o jogador vê uma estação pela janela do trem e entra em outra |
| Chão | Chão do vagão com sujeira e reflexo; no C2, a plataforma com a faixa tátil e os trilhos embaixo | Faixa preta lisa. Sem beirada de plataforma, sem trilho |
| Mezanino e caixotes | C1/C2: bancos e bagageiro em que dá para subir (plataformas de mão única) | Caixotes pintados no fundo com colisores sólidos (o Kai **não pula encostado** neles). O "mezanino" é o balcão do guichê: não tem chão, corrimão nem escada desenhados |
| Portas | Porta da ponta de dobradiça animada, luz de trava, portas que batem (C2) | Porta da administração pintada (`porta3_*`), folhas que deslizam; está boa de mecânica, fora do estilo |
| Painel de desvios | — | Bom puzzle, mas só existe na tela; no mundo é um teclado verde pintado ao lado da porta |
| Coisas para ler | C2: aviso, relógio, celular, mapa, crachá, cartão, remédio; cada uma avança a história | 4 papéis (horários, bilhete, escala, recibo) que só servem para entregar a peça. Nada sobre o chefe, a demissão, o 04:17, o Dr. Mori, o Souta ou o Ren |
| Puzzles | C1: 3. C2: 3 (cadeado, retenção, fusíveis) + portas | 1: achar 4 peças e girar os trilhos |
| Souta e Ren | Vão um vagão na frente, reaparecem a cada puzzle | Ficam parados ao lado do trem a cena toda; o Ren só se mexe para entregar a última peça |
| Conversas pós-puzzle | Uma por puzzle, com o pensamento do Kai no fim | Só "Vai. A gente vem logo atrás." |
| Dicas | Balão "..." depois de 75 s sem avançar (ou mortes), dica do Souta e do Ren por parte | Brilho nos papéis e o Ren entregando a última peça. Sem balão, sem dica por tempo |
| Entrada | C0 → C1 sem corte (mesmo quadro) | Fade preto; primeira imagem não tem nada a ver com a última do C2 |

## 3. O novo Cenário 3 (proposta)

A mesma estação vista da janela no fim do C2, agora por dentro. Três partes, da esquerda para a direita (~56 u, o C2 tem ~110):

1. **Plataforma** (0–26 u): à esquerda, a **ponta do último vagão com a porta aberta** (kit dos Cenários 0/1, igual ao último quadro do C2). O Kai sai por ela: **entrada sem corte**, o primeiro quadro do C3 é o último do C2. Atrás, a parede de `estacao.png` (o mesmo banco, a mesma placa) e depois a mesma parede vazia; embaixo, a beirada e os trilhos.
2. **Saguão** (30–48 u): bilheteria, máquina automática, achados e perdidos, caixotes e o **mezanino** dos funcionários (cabine com monitor).
3. **Administração** (48–56 u): painel de desvios na parede e a porta.

**Em cada parte tem algo para ler, um puzzle e uma conversa quando ele é resolvido.** Souta e Ren vão uma parte na frente: começam na plataforma; depois do puzzle 1 esperam no saguão; depois do 2, na porta da administração.

### Parte 1 — "A última partida" (puzzle de código)
- O caminho para o saguão está fechado por um **portão de grade de serviço** com teclado de 4 números (`DigitLockUI`, como o cadeado do C2).
- **Para ler:**
  - *Placa da estação* (a placa escura de `estacao.png`): letras quase apagadas, **TERMINAL**. Kai: *"Terminal. A última parada. Mas o mapa do trem tinha mais uma."*
  - *Mapa da rede* (o mesmo `mapa_linha` do C2, continuidade): "VOCÊ ESTÁ AQUI: TERMINAL". Depois do Terminal, uma bolinha a mais, sem trilho ligando: **ÚLTIMA LINHA**.
  - *Quadro de partidas* (pendurado): "22:10 · 23:00 · **23:50 — último trem**". Embaixo, num papel colado, à mão, em vermelho e numa letra torta: **"04:17 — ÚLTIMA LINHA. Não embarque."** Atrás do papel, presa com fita, a **1ª peça de trilho** (a de hoje).
  - *Aviso no portão:* "Portão de serviço. Fecha depois da última partida. Código: o horário da última partida, sem os dois pontos."
  - *Bilhete de embarque* no chão, saindo do trem (o de hoje): "ÚLTIMA LINHA · uma viagem, sem volta" + **peça**. É o primeiro papel, perto da porta do trem: ensina a ler e a pegar peça.
- Código: **2350**. **0417 é a armadilha**: o portão recusa e o Kai pensa *"Quatro e dezessete não é partida nenhuma. Alguém escreveu por cima."*
- **Conversa ao abrir:** Souta: "Vinte e três e cinquenta. O último trem de verdade." / Ren: "Então o nosso... não estava na lista?" / Souta: "Trem fora de horário. Acontece." / Kai: *"A letra do 04:17 é torta, apertada. Igual à do meu diário."* (prepara o relógio do C4 e o "São quatro e dezessete" do C7, e o Kai escrevendo coisas que não lembra, como as letras vermelhas do C1/C2.)
- **Dica (75 s parado):** Souta: "O portão fecha depois da última partida. O quadro ali diz qual foi." / Ren: "Não o que está escrito à mão... o que está impresso."

### Parte 2 — "Troco" (moeda → máquina, e o mezanino)
Três lugares, duas peças:
- *Achados e perdidos* (prateleira com caderno de registro): "Óculos redondos, de criança — ninguém veio buscar." · "Guarda-chuva preto." · "Bolsa de trabalho, crachá 0417 — devolvida ao dono." · "Uma moeda, debaixo do banco da plataforma — **não recolhida**." Ren põe a mão nos óculos: "Óculos redondos... iguais aos meus." / Kai: *"Iguais aos que eu usava na escola."* (o diário diz isso; prepara o Ren.)
- *Moeda* debaixo do banco da plataforma (brilha). Volta-se à parte 1 (dentro da mesma tela larga).
- *Máquina automática:* sem moeda, *"Não tenho moeda."*; com a moeda, cai uma água e o **recibo** de hoje ("troco: uma peça de trilho") com a **2ª peça**.
- *Bilheteria* (papel colado no vidro, só leitura): "COMUNICADO INTERNO — Reestruturação do setor de operações. Os funcionários abaixo devem comparecer à **sala 704** (7º andar), às 9h, para tratar do seu desligamento: … **K.** …". Kai: *"Sala 704. A mensagem do celular."* / *"O prédio tem sete andares. Eu contei."* (o chefe e a demissão chegam aqui como ameaça; **o C5 é que mostra o que aconteceu na sala**, nada aqui fala de morte.)
- *Mezanino* (subindo nos caixotes, agora de mão única, como os bancos): a cabine dos funcionários.
  - *Escala* (a de hoje, mudada): "Madrugada: **K.** — afastado. Atestado: Clínica Dr. Mori." Embaixo, outra letra: "voltou?" Kai: *"Afastado. Eu não lembro de ter sido afastado."* (prepara o noticiário final: Kai trabalhava na companhia.)
  - *Monitor da câmera* (azul, chiando): "CÂMERA 2 — PLATAFORMA. A imagem treme. Dá para ver o banco, o trem parado... e **uma pessoa só**, de pé na beira, olhando os trilhos." Gaveta da mesa: **3ª peça**.
  - *Cartaz da clínica* (na parede do saguão): "Dormir pouco. Esquecer coisas. Conversar com quem não está lá. Você não precisa passar por isso sozinho. — Clínica Dr. Mori." Ren: "Esse cartaz parece que está olhando pra gente."
- **Conversa com as 3 peças** (quando o Kai desce): Ren: "Kai... a câmera da plataforma. Tinha alguém na beira." / Souta: "Era reflexo. Não tem ninguém aqui além de nós três." / Kai: *"Nós três."* (prepara o C7: o Ren na beira, e o final em que não havia ninguém.)
- **Dica:** Souta: "A máquina pede moeda. Debaixo do banco da plataforma brilhou alguma coisa." / Ren: "Os funcionários ficavam lá em cima. Dá para subir pelos caixotes, mas pula de longe."
- O **Ren entregando a última peça** continua (se o Kai chegar ao painel faltando uma).

### Parte 3 — "A linha" (painel de desvios, o de hoje)
- Mantém a grade e a solução. O painel ganha **corpo no mundo** (caixa de sinalização na parede, lâmpada vermelha que fica verde) e a peça que falta aparece encaixando.
- Diminuir o tédio: conferir quantos giros o caminho exige de fato e deixar os trilhos fora do caminho já na posição certa (o piloto, girando tudo para a posição da solução, deu 32 giros).
- **Conversa ao abrir:** Souta: "Administração. Se tem um jeito de fazer o trem andar de novo, é lá dentro." / Ren: "E se a gente só esperasse o próximo trem?" / Souta: "Que próximo? O último foi às vinte e três e cinquenta." / Kai: *"E o das quatro e dezessete."* (liga a parte 1 ao C4.)
- **Dica:** Souta: "A luz entra pela plataforma e tem que sair embaixo, na administração. Gira só o que está no caminho." / Ren: "Começa pela porta e volta até a entrada."

### O que fica de fora de propósito
- Nada de jornal, polícia, sangue ou do chefe morto (é o C5). O chefe aparece só como "sala 704" e "desligamento".
- O 04:17 aparece **uma vez**, escrito à mão, e como resposta errada. O relógio parado e o surto do Souta continuam sendo do C4.
- A câmera mostra "uma pessoa só" sem dizer quem; o Souta explica como reflexo.

### Mudanças de mecânica
- Caixotes como **plataformas de mão única** (iguais aos bancos do C1), para o pulo encostado funcionar.
- Dicas por tempo (75 s sem avançar), balão "..." e falas por parte: copiar do `Cenario2Director` (`LigaDica`, `OnTalked`, `Progresso`).
- Souta e Ren mudam de lugar entre as partes enquanto a câmera não os vê (como no C2).
- "Continuar" no meio do cenário guarda as peças, o portão e a moeda (`GameSave.Mark`), como hoje guarda as peças.
- Entrada sem corte: o diretor do C2 já sai pela porta do fim para o `PisoEstacao`; o C3 começa com o Kai nessa mesma posição relativa à ponta do vagão e sem fade.

## 4. Ordem do trabalho (depois da aprovação e das artes)
1. `Ferramentas/Cenario3/objetos_arte.py`: recorta o verde, reduz para a grade de 58 px, prende na paleta de `estacao.png`; monta as peças derivadas (portão aberto, folhas da porta, interior escuro). Prévia de cada peça ao lado do Kai (`previa_objetos.png`).
2. Fundo novo (plataforma + saguão + administração) e colisores; ponta do vagão à esquerda; câmera; entrada sem corte com o C2.
3. Portão com código, moeda → máquina, mezanino, painel no mundo.
4. Souta e Ren por parte, conversas e dicas.
5. Piloto atualizado para os puzzles novos (código errado 0417 antes do certo, moeda, máquina, mezanino), rodar até passar sem erros, conferir fotos.
6. Atualizar este PLANO e regenerar o roteiro (`Ferramentas/Roteiro/gera_roteiro.py`).

## 5. Artes para gerar

Mesmo jeito das do Cenário 2: **uma peça por imagem, fundo verde puro (#00FF00)**, pixel art, vista de lado. O script recorta o verde, reduz para a grade de 58 px e prende na paleta da estação. As duas paredes (1 e 2) são exceção: preenchem a imagem toda.

**Anexe em todos os pedidos (referência de estilo e paleta):**
- `Assets/Art/Cenario2/Objetos/estacao.png` (a estação que aparece no fim do C2)
- `Ferramentas/Cenario1/referencias/objetos/painel_emergencia.png` (traço dos objetos)

Coloque as imagens geradas em `Ferramentas/Cenario3/referencias/objetos/` com o nome indicado. A coluna "altura no jogo" é para conferência (o Kai tem 3,2 u ≈ 1,75 m).

**Texto base (cole no começo de cada prompt):**
> Pixel art de um objeto isolado para um jogo de terror 2D de vista lateral, numa estação de metrô japonesa subterrânea, antiga, vazia e suja, de madrugada. Mesmo estilo, traço, tamanho de pixel e paleta das imagens anexas: azulejos brancos encardidos, cinza, marrom e ferrugem dessaturados, sombras quase pretas, toques de vermelho escuro, luz fluorescente fria. Vista de frente/de lado, sem perspectiva de cima. Fundo verde puro #00FF00 liso, sem sombra no chão, sem texto legível, sem moldura. Só o objeto, centralizado.

| Arquivo | Prompt (depois do texto base) | Anexo extra | Altura no jogo |
|---|---|---|---|
| `parede_plataforma.png` | **Sem o texto base e sem fundo verde.** A mesma plataforma da imagem anexa, mesmo enquadramento, mesma altura do chão, da faixa tátil e dos trilhos, mesma luz, mas **vazia**: sem o banco, sem a placa, sem o cartaz e sem a caixa da parede. Só parede de azulejos com manchas escorrendo, canos, duas luminárias e uma coluna com faixa vermelha em cada ponta, cortada bem no meio, para emendar uma cópia do lado da outra. Proporção 21:9, preenchendo a imagem toda. | `Ferramentas/Cenario2/referencias/objetos/estacao_janela.png` | tela inteira (9,3 u) |
| `parede_saguao.png` | **Sem o texto base e sem fundo verde.** O saguão da mesma estação, continuação da plataforma da imagem anexa: mesma parede de azulejos, mesmas luminárias e colunas, mesma altura do chão, mas **sem trilhos e sem beirada**: o chão de ladrilho sujo continua até a borda de baixo. Meia-parede de azulejo escuro embaixo, rodapé encardido. Vazio, colunas cortadas no meio nas pontas para emendar. Proporção 21:9, preenchendo a imagem toda. | `Ferramentas/Cenario2/referencias/objetos/estacao_janela.png` | tela inteira |
| `quadro_partidas.png` | Painel de partidas antigo de estação pendurado do teto por duas hastes: caixa preta com plaquinhas giratórias (split-flap) em três linhas de caracteres brancos amarelados ilegíveis, a linha de baixo apagada. Colado embaixo da caixa, um pedaço de papel com um rabisco à mão em vermelho escuro, ilegível. Proporção 2:1. | — | 1,6 u |
| `portao_servico.png` | Portão de grade de correr de serviço, da altura de uma porta, barras verticais de metal pintadas de amarelo desbotado e enferrujadas, preso num trilho em cima. Na coluna da direita, uma caixinha de teclado numérico com uma luz vermelha e uma plaquinha de aviso. Proporção 2:3. | — | 4,2 u |
| `achados_perdidos.png` | Prateleira de metal baixa de "achados e perdidos" encostada na parede: um guarda-chuva preto, uma bolsa, uma caixa de papelão, um par de óculos redondos pequenos em cima e um caderno de registro aberto. Proporção 3:2. | — | 2,0 u |
| `moeda.png` | Uma moeda velha caída no chão, pequena, vista quase de frente, com um brilho fraco amarelado. | — | 0,2 u |
| `maquina_automatica.png` | Máquina automática de bebidas japonesa antiga, alta: frente de vidro com fileiras de latas e garrafas, botões, a entrada da moeda e a bandeja embaixo; luz interna fraca e fria, adesivos rasgados, ferrugem nos cantos. Proporção 1:2. | — | 3,4 u |
| `bilheteria.png` | Guichê de bilheteria de estação visto de frente: cabine de metal e vidro sujo com uma abertura de atendimento embaixo, cortina meio fechada lá dentro, um papel colado no vidro com fita. Proporção 1:1. | — | 3,8 u |
| `caixotes.png` | Pilha de caixotes de madeira e engradados de manutenção em **dois degraus**, vista de lado: um caixote baixo na frente à esquerda e dois empilhados atrás à direita, madeira escura e úmida, uma lona rasgada caída por cima. Proporção 4:3. | `Ferramentas/PixelArt/previa/jogo_c3_mezanino.png` (a pilha de hoje, para a forma) | 1,1 u e 1,8 u |
| `mezanino.png` | Mezanino de funcionários de estação visto de lado: plataforma elevada de chapa de metal sobre duas colunas, guarda-corpo de canos, e em cima uma cabine pequena com janela, uma mesa, um monitor de câmera aceso em azul e uma luminária pendurada. Embaixo da plataforma, só escuro. Proporção 3:2. | `Ferramentas/PixelArt/previa/jogo_c3_mezanino.png` | chão do mezanino a 2,5 u; total ~6,5 u |
| `painel_desvios.png` | Painel de sinalização ferroviária de parede, antigo: caixa de metal com tampa de vidro, dentro um diagrama de trilhos com desvios feito de plaquinhas, alguns encaixes vazios, uma lâmpada vermelha em cima e cabos saindo por baixo. Proporção 3:4. | — | 1,5 u |
| `porta_administracao.png` | Porta dupla de metal de sala de administração de estação, **fechada**, com uma janelinha de vidro aramado em cada folha, placa em cima sem texto, batente de metal, ferrugem e marcas de mão. Proporção 1:2. | `Assets/Art/Cenario3/porta3_folha_esq.png` (a de hoje, para a proporção) | 4,6 u |
| `cartaz_clinica.png` | Cartaz de propaganda de clínica emoldurado, pregado na parede: ilustração desbotada de uma pessoa de costas, sozinha numa plataforma, e uma mão estendida do lado; papel amarelado, rasgado num canto, sem texto legível. Proporção 2:3. | — | 1,8 u |
| `bilhete.png` | Bilhete de metrô de papel caído no chão, amassado e meio dobrado. | — | 0,2 u |

**Eu monto (não precisa gerar):**
- A primeira parede da plataforma é a própria `estacao.png` (é a que o jogador viu pela janela); a `parede_plataforma` continua a partir dela.
- Portão aberto: o mesmo portão correndo para o lado dentro de uma máscara (como a porta da administração faz hoje).
- Folhas e interior da porta da administração: corto a `porta_administracao.png` ao meio e escureço o vão.
- A palavra **TERMINAL** na placa, o papel do 04:17 legível e a luz do painel e do monitor: feitos no jogo (fonte de letra de mão e brilho), como as letras vermelhas do C2.
- Mapa da rede: reaproveito o `mapa_linha.png` do C2.
- Ponta do vagão com a porta aberta: kit dos Cenários 0/1 (`ponta_dir` e o quadro aberto de `porta_ponta_0..5`).
