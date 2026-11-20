# Última Linha — Game Design Document

> Versão do documento: 04/11/2026 · baseada na branch `personagens-pixel-art` (commit `e4a43c1` + alterações locais do Cenário 3), que está à frente do `main` do GitHub.
> Fontes: código e cenas do projeto, `Ferramentas/Roteiro/roteiro.md` (gerado do código), `Ferramentas/Cenario2/PLANO.md`, `Ferramentas/Cenario3/PLANO.md`, os `LEIA-ME.md` das ferramentas e `Assets/Documentation/GUIA_RETRATOS_DIALOGO_UNITY.md`.
> O que está marcado como **[a definir]** não aparece em nenhuma dessas fontes.

---

## 1. Visão geral

| | |
|---|---|
| **Título** | Última Linha |
| **Gênero** | Terror psicológico narrativo, 2D de vista lateral, com puzzles e plataforma leve |
| **Motor** | Unity 6 (6000.6.0f1), partindo do template *2D Platformer* da Unity |
| **Plataforma** | PC (teclado; controle aceito: Start = Esc). Outras **[a definir]** |
| **Idioma** | Português (Brasil) |
| **Duração estimada** | 30 a 60 min **[estimativa; só o C3 tem tempo medido: 5 a 8 min jogando à mão]** |
| **Público** | Jovens adultos e adultos que gostam de terror narrativo. Classificação indicativa **[a definir]**: o jogo tem sangue, morte, arma de fogo, suicídio e homicídio |
| **Estado** | Os 8 cenários (0 a 7) e o menu estão montados e jogáveis. Os cenários 0 a 3 já foram refeitos em pixel art com puzzles, leituras e dicas; os cenários 4 a 7 ainda usam a arte gerada por código |

### Pitch
Kai, um analista apagado de 27 anos, cochila no último trem da noite e acorda num vagão vazio e envelhecido. Com o colega Souta, que sempre se dá bem, e o tímido Ren, de quem ninguém lembra, ele atravessa vagões, uma estação e os escritórios da empresa resolvendo puzzles. Aos poucos, cada papel, relógio e mensagem aponta para a mesma hora, **04:17**, e para uma verdade que Kai não quer lembrar.

### Pilares
1. **Tudo que se lê conta a história.** Cada documento, objeto ou fala é ao mesmo tempo pista de puzzle e pedaço da verdade sobre Kai.
2. **O jogador desconfia antes do protagonista.** As contradições (o crachá de Ren com o número de Kai, a câmera que mostra "uma pessoa só") aparecem cedo, mas só ganham sentido no fim.
3. **Puzzles curtos e justos.** Um puzzle por espaço, resolvido com o que está em volta, com dica dos companheiros quando o jogador trava.
4. **Continuidade sem cortes.** Sempre que dá, um cenário termina no quadro em que o próximo começa (C0 → C1, C2 → C3).

---

## 2. História

### Premissa
- **Kai** (27 anos) trabalha há seis anos na mesma mesa (mesa 12, perto da janela que não abre). Dorme pouco, esquece coisas e parou há três semanas o remédio receitado pelo Dr. Mori.
- O chefe ia demiti-lo (sala 704, 9h). No dia 20 de outubro, aniversário de Kai, ele pega o último trem.
- **A verdade, revelada aos poucos:** Kai tem esquizofrenia. Souta e Ren não existem: são projeções dele. Souta é quem Kai queria ser (mesmo dia de entrada na empresa, mesmo aniversário, gerente bem-sucedido). Ren é o Kai inseguro (usa o crachá 0417, os óculos redondos que Kai usava na escola, ninguém o conhece). Kai matou o chefe no escritório e não lembra. O trem, a estação e a "última linha" são o caminho da cabeça dele até a plataforma, às 04:17.

### O 04:17
É o motivo que costura o jogo inteiro: o relógio parado do C2 e do C4, o celular (última mensagem do chefe), o crachá "de Ren" (0417), o papel à mão no quadro de partidas do C3 ("04:17 — ÚLTIMA LINHA. Não embarque."), o surto de Souta no C4 ("Às quatro e dezessete todos nós vamos morrer") e a hora dos dois finais.

### Personagens

| Personagem | Papel | Como fala e age | O que esconde |
|---|---|---|---|
| **Kai** | Protagonista e jogador. Os pensamentos dele aparecem em itálico | Cansado, ressentido com Souta, irônico consigo mesmo | Assassino do chefe, doença sem tratamento |
| **Souta** | Companheiro "líder". Explica as mecânicas e dá as dicas diretas | Seguro, mandão: "Tudo tem um mecanismo." Vai sempre na frente | Alucinação. No C4 tem um surto e depois não lembra. No C5 pega a arma do chefe e tranca Kai |
| **Ren** | Companheiro medroso. Dá as dicas indiretas | Gagueja, pede desculpas, tem medo de tudo | Alucinação, um espelho de Kai. No C7 está na beira dos trilhos |
| **O chefe (diretor)** | Antagonista ausente | Só aparece em documentos, até o C5 | Está morto no próprio escritório |
| **Dr. Mori** | Psiquiatra de Kai | Só em documentos (remédio, cartaz, atestado) | — |

### Estrutura dos atos
- **Ato 1, o estranhamento (C0 a C2):** acordar sozinho, conhecer Souta e Ren, primeiras pistas (MORTE escrita nas paredes, 04:17, "ELE NÃO ESTAVA NO TREM").
- **Ato 2, a descida (C3 a C5):** a estação que não está no horário, a câmera que mostra uma pessoa só, o surto de Souta, o corpo do chefe e a acusação.
- **Ato 3, a última linha (C6 e C7):** fuga contra o tempo e a escolha final.

---

## 3. Fluxo do jogo

```
Menu inicial ─► C0 Abertura ─► C1 Vagão parado ─► C2 Vagões em movimento ─► C3 Estação
            ─► C4 Sala de controle ─► C5 Escritório do chefe ─► C6 Túnel ─► C7 Última plataforma ─► Final A ou B
```

Ordem das cenas no build: `MainMenu`, `Cenario1`, `Cenario0`, `Cenario2` … `Cenario7`, `SampleScene` (sobra do template).

---

## 4. Cenários

### C0 — Abertura
- **Cutscene:** página do diário de Kai, escrita à mão ("Dia 20 de outubro…").
- **O que acontece:** Kai acorda num vagão vazio e envelhecido. A porta da ponta está trancada e se abre pelo painel de emergência. A passarela entre os vagões não está lá: é preciso correr e pular.
- **Ensina:** andar (A/D), pular (Espaço), subir e descer dos bancos (segurar S), correr (Shift), interagir (E), diário (Tab).
- **Transição:** sem corte para o C1.

### C1 — O vagão parado
- **Personagens:** Souta e Ren aparecem pela primeira vez.
- **Puzzle 1, alavancas:** três alavancas (encostos e assento). O letreiro acende numa ordem (meio, esquerda, direita), que é a ordem para puxá-las. Errar trava tudo de novo.
- **Puzzle 1b, alavanca emperrada:** a da esquerda só solta com um tranco dado quando o ponteiro do mostrador passa no verde.
- **Puzzle 2, palavra:** as luzes piscam e aparecem letras vermelhas pelo vagão (chão, porta, janela alta). A trava pede 5 letras: **MORTE**.
- **Tema:** Souta resolve tudo, Ren é esquecido, Kai se sente cobrado.

### C2 — Os vagões em movimento
O trem volta a andar. São quatro vagões e três passagens, e Souta e Ren vão sempre um vagão na frente.
- **Vagão 1, cadeado de 4 números:** o relógio de parede parado e o celular caído marcam a mesma hora. Código **0417**. Ren: "é o número do meu crachá."
- **Passagem:** as portas de dobradiça abrem, piscam e batem com o balanço. Se baterem com Kai no vão, ele volta ao começo do vagão. No vão é preciso pular correndo.
- **Vagão 2, alavanca de retenção:** segura a porta aberta por pouco tempo. É puxar e correr, e esperar em cima do engate se a outra porta estiver fechada.
- **Vagão 3, fusíveis:** sem energia, a luz de aviso das portas não acende. São três fusíveis, cada um junto de algo para ler: a bolsa com o crachá de Kai (0417), o cartão de aniversário de Souta ("mesmo dia que o meu") e o frasco de remédio vazio do Dr. Mori. Com a luz de volta aparece na parede "ELE NÃO ESTAVA NO TREM".
- **Leituras:** aviso da operadora, relógio, celular (11 chamadas do chefe, sala 704), mapa da linha (as estações são lugares da vida de Kai e a última se chama "ÚLTIMA LINHA").
- **Fim:** o trem freia numa estação, "Terminal".

### C3 — A estação
É a mesma estação vista pela janela no fim do C2, e a entrada é sem corte. Tem duas partes, com um puzzle em cada (simplificado em 11/11/2026: antes eram três partes, onze leituras e quatro peças espalhadas).
- **Plataforma, portão de serviço:** teclado de 4 números. O quadro de partidas fica pendurado logo ao lado do portão: o último trem saiu às 23:50, e embaixo um papel à mão diz "04:17". O aviso no teclado pede "o horário da última partida". Código **2350**. Digitar **0417 é a armadilha**: o portão recusa e Kai percebe que a letra é igual à do diário dele. Atrás da grade há um corredor escuro, para o portão ler como passagem.
- **Saguão, moeda, máquina e painel:** a moeda brilha no chão perto da bilheteria e paga a máquina automática, que devolve a única peça de trilho. O painel de desvios fica preso ao batente da porta da administração: encaixa-se a peça e giram-se 5 trilhos até a luz ligar a plataforma à porta. A porta tem batente com sombra e uma luz fria em cima.
- **Leituras:** só três. Bilhete de embarque ("ligar a linha sozinho"), quadro de partidas e comunicado interno (sala 704, desligamento de "K."). A placa TERMINAL fica como cenário. O "uma pessoa só" da câmera passou para a conversa da porta: Ren vê na tela do painel alguém na beira da plataforma.

### C4 — A sala de controle
- **Puzzle 1, energia:** levantar as três alavancas (chão, passarela e em cima do gerador, subindo pelo caixote). O quarto encaixe fica vazio até o C6.
- **Puzzle 2, gaveta:** o bilhete do supervisor diz que a senha é a hora em que o relógio parou: **0417**. Dentro está a chave "SAÍDA TÉCNICA".
- **Momento-chave:** ao olhar o relógio, Souta entra em surto ("Às quatro e dezessete todos nós vamos morrer") e logo depois não lembra de nada.
- **Saída:** a porta precisa de energia e da chave. Kai reconhece o corredor.

### C5 — O escritório do chefe
- **Cena:** o chefe está morto atrás da mesa, com sangue no chão. Ao atravessar o corpo a cabeça se solta.
- **Revelação:** no bolso estão a **quarta alavanca** e o recorte de jornal ("polícia procura funcionário Kai"). Kai lembra: "Foi o que eu fiz."
- **Confronto:** Souta pega a arma da gaveta, Ren tem medo, e os dois trancam Kai do lado de fora.
- **Puzzle, a chave reserva:** o memorando diz que ela fica junto dos papéis de demissão. Há quatro gavetas de arquivo (admissões, avaliações "insuficiente", demissões, salário riscado) e a chave está na de demissões.

### C6 — O túnel
- Kai encaixa a alavanca no painel de tração. O metrô liga e começa uma **contagem regressiva**.
- **Desafio:** correr pelo túnel passando pelas anteparas, que fecham no ritmo do alarme (passar quando a luz fica verde). Se o tempo acabar, o trem espera e há outra chance.
- Kai embarca: "O mesmo vagão do começo."

### C7 — A última plataforma
- A parede diz ÚLTIMA LINHA e o relógio está parado em 04:17. Ren está na beira dos trilhos e Souta, armado, logo atrás.
- **Escolha** (`ChoiceUI`, no estilo de *Detroit*):
  - **"Acabar com eles"** — Kai empurra Ren e atira em Souta. *Final A:* o telejornal noticia que um funcionário foi preso por tentativa de massacre. Ele conversava sozinho com "Souta e Ren", e os dois colegas reais ficaram feridos.
  - **"Acabar comigo"** — Kai vai para a beira e o trem chega. *Final B:* o telejornal noticia um suicídio no local de trabalho. Kai tinha esquizofrenia e parou o tratamento, e não existe registro de ninguém com esses nomes. O letreiro mostra o **CVV 188**.

---

## 5. Mecânicas

### Controles

| Ação | Teclado |
|---|---|
| Andar | A / D |
| Pular | Espaço (pulo mais alto segurando) |
| Correr | Segurar Shift (necessário para os vãos) |
| Descer de banco ou plataforma | Segurar S |
| Interagir, ler, falar | E (separado da tecla de avançar fala) |
| Diário do Kai | Tab |
| Fechar painel, documento ou diário | Esc (Start no controle). Menu de pausa **[a confirmar]** |

### Movimento e plataforma
Usa o motor de física cinemática do template. Bancos, bagageiros e caixotes são **plataformas de mão única** (sobe-se por baixo e desce-se com S). Morte por queda no vão ou por porta batendo devolve o jogador ao começo do trecho, sem tela de game over.

### Interação e leitura
- `ObjectSpot` / `DocumentView`: documentos em tela cheia com título, corpo e rodapé. A primeira leitura dispara um pensamento de Kai; as seguintes, uma fala curta.
- Objetos que dão item (peça, fusível, moeda, chave) ficam brilhando até serem pegos.
- `NewsView`: telas de telejornal (C5 e finais).

### Tipos de puzzle

| Tipo | Componente | Onde |
|---|---|---|
| Ordem de alavancas | `LeverPuzzle`, `TrainLever` | C1 |
| Tempo (ponteiro no verde) | mostrador da alavanca emperrada | C1 |
| Palavra com letras achadas | `LetterPuzzle`, `LetterLockUI` | C1 |
| Código de 4 dígitos | `DigitLockUI` | C2, C3, C4 |
| Timing de portas | `CarDoor`, retenção | C2, C6 |
| Coletar e encaixar | fusíveis, peças de trilho, alavancas, chave | C2 a C6 |
| Painel de desvios (girar trilhos) | `SwitchPanelUI`, `SwitchTileCell` | C3 |
| Revistar gavetas | `ObjectSpot` | C5 |
| Contagem regressiva | `Cenario6Director` | C6 |
| Escolha moral | `ChoiceUI` | C7 |

### Sistema de dicas
Se o jogador ficar **75 s sem avançar** ou **morrer 3 vezes** no mesmo trecho, Souta e Ren ficam com um balão "…" e aparece o aviso *"Talvez o Souta ou o Ren saibam de alguma coisa."* Souta dá a dica direta e Ren a indireta. Cada parte de cada cenário tem a sua dica.

### Diário (Tab)
Páginas fixas: Lembretes, Trabalho, Kai, Souta, Ren e 20 de outubro. É a base de leitura que o jogador cruza com o que encontra (aniversário, crachá 0417, óculos, remédio parado, "Não lembro dele entrando").

### Companheiros
Souta e Ren vão uma parte na frente. Quando saem da câmera, mudam de lugar sozinhos. Têm uma fala de primeira conversa, falas seguintes, uma conversa depois de cada puzzle e a dica.

### Salvamento
- `GameSave` em PlayerPrefs: cena atual, marcas de progresso (`Mark`/`Has`), posição do jogador e horário. O menu mostra **Continuar** quando existe um save.
- Dentro do cenário: o C3 guarda portão, moeda, peças e linha (ainda não testado). O C2 recomeça do início do cenário.
- `GameSettings`: volume geral, volume da música e tela cheia.

---

## 6. Arte e som

### Direção visual
- **Pixel art** escura, suja e dessaturada: cinza, marrom e ferrugem, sombras quase pretas, toques de vermelho escuro, luz fluorescente fria. O cenário é um metrô japonês antigo e abandonado de madrugada.
- **Grade única:** 58 px por unidade, câmera ortográfica 4,655. Um pixel do fundo vale um pixel do personagem. Kai mede 3,2 u (≈ 1,75 m).
- **Personagens:** sprites feitos a partir das artes do usuário, na grade nativa (sem 3D e sem desenho à mão por cima). Os retratos de diálogo de Kai, Souta e Ren são de 520×860 px, todos no mesmo enquadramento.
- **Pipeline de objetos:** uma peça por imagem gerada com fundo verde `#00FF00`. Um script em Python (`Ferramentas/CenarioN/objetos_arte.py`) recorta o verde, reduz para a grade de 58 px e prende na paleta do cenário.
- **Texto no mundo** (letras vermelhas, papéis legíveis, TERMINAL) é feito no jogo com uma fonte de letra de mão.
- **Fontes:** Cormorant Garamond e IM Fell English (menu), Oswald, Caveat (diário e letra de mão). Todas OFL.
- **Guia de arte, paleta oficial e licenças:** `Ferramentas/Assets/GUIA_ARTE.md`, `paleta_oficial.png` (80 cores) e `LICENCAS.md`.
- **Luz 2D:** o projeto usa o 2D Renderer da URP com material Unlit por padrão. O C4 é o piloto com Light2D, normal map e efeitos (`Assets/Scripts/Visual`).
- **Pendente:** o C4 já está na grade de 58 px (piloto, `Ferramentas/Cenario4/c4_pixel.py`). Os C5 a C7 ainda usam fundos a 100 px/u desenhados por código e seguem a mesma receita depois que o piloto for aprovado.

### Som
- Ambientes por cenário (`Assets/Audio/Cenario1`, `Cenario2`, `Salas`) gerados por código (numpy/scipy), mais o som do freio do trem.
- Música de menu em `Assets/Audio/Menu`.
- Os sons do template (pulo, queda, passos) continuam no projeto. **[revisar quais ainda são usados]**
- Trilha musical dentro dos cenários: **[a definir]**

### Interface
- Menu inicial: vagão com letreiro piscando, névoa e vinheta. Botões: Começar, Continuar, Configurações, Sair.
- HUD mínimo: objetivo atual e dica de tecla em texto curto (`Hud`), caixa de diálogo com nome do personagem, toast de dica.

---

## 7. Tecnologia e ferramentas

| Área | Onde |
|---|---|
| Montagem das cenas por código | `Assets/Editor/UltimaLinha/CenarioNSceneBuilder.cs` (menu `Última Linha > …`) |
| Lógica de cada cenário | `CenarioNDirector.cs` (`Scripts/Cenario1`, `Cenario2`, `Salas`) |
| Teste automático | `PixelArtTeste.cs`: um "piloto" que joga o cenário sozinho, mede o tempo de cada parte e fotografa onde trava (`Última Linha > Testes > Jogar o Cenário N sozinho`) |
| Roteiro | `Ferramentas/Roteiro/gera_roteiro.py` extrai todas as falas do código para `roteiro.md` e `roteiro.html` |
| Arte e som procedurais | `Ferramentas/*` em Python (numpy, Pillow, scipy) |
| Base | template *2D Platformer* (`PlatformerModel`, eventos de `Gameplay`) |

**Regra de manutenção:** as medidas dos cenários estão repetidas nos scripts Python e nos `SceneBuilder`. Se mudar um número num lado, é preciso mudar no outro.

---

## 8. Riscos e pendências

1. **Cenários 4 a 7 fora do estilo:** falta refazê-los em pixel art e dar a eles leituras, puzzles e dicas no nível dos C0 a C3 (seguindo o modelo dos `PLANO.md`).
2. **GitHub desatualizado:** o `main` está atrás desta branch, e o Cenário 3 novo ainda não foi commitado.
3. **Save no meio do cenário:** o C2 recomeça do início e o do C3 não foi testado.
4. **Sobras no projeto:** `SampleScene` no build, os scripts do template (inimigos, tokens, vitória), `ClaudeApiClient`/`NPCDialogueController` (não são chamados em lugar nenhum) e o guia de retratos, que cita um `integracao-claude-unity.md` que não existe.
5. **Tema sensível:** suicídio, homicídio e esquizofrenia. Recomenda-se um aviso de conteúdo no início, cuidado para não associar a doença a violência (o Final B e o letreiro com o CVV já vão nessa direção) e uma revisão da classificação indicativa.
6. **A definir:** plataforma de lançamento, duração-alvo, trilha musical, suporte completo a controle, acessibilidade (legendas, tamanho de texto, velocidade das falas) e localização.
