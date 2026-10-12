# Guia — retratos de diálogo (Kai, Souta, Ren) na Unity

## Arquivos

- `kai_retrato_dialogo.png`, `souta_retrato_dialogo.png`, `ren_retrato_dialogo.png` — recorte cabeça-até-cintura, 520×860px, fundo transparente, **mesmo enquadramento nos 3** (pronto pra usar na caixa de diálogo).
- `*_frente_transparente.png` — corpo inteiro, fundo transparente (caso precise pra outra tela, ex. tela de seleção de personagem).
- `*_topdown_transparente.png` — variação de ângulo mais de cima, fundo transparente (guardar como referência/concept art; não é necessária pro diálogo).
- `contato_retratos_dialogo.png` / `contato_notion_transparente.png` — folhas de contato só pra conferência, não importar no jogo.

## Por que recortei até a cintura

Como você confirmou que o uso é **retrato de diálogo** (tipo visual novel, aparecendo na caixa de texto), a imagem de corpo inteiro tem informação demais (pernas/sapatos que não aparecem na UI). O recorte cabeça-até-cintura, com os 3 personagens no mesmo enquadramento e tamanho de canvas (520×860), facilita colocar qualquer um dos 3 no mesmo lugar da UI sem reajustar posição/escala toda vez que troca o personagem que está falando.

## Import na Unity

1. Copie os `*_retrato_dialogo.png` para `Assets/Sprites/Dialogo/` (ou pasta equivalente).
2. No Inspector de cada um:
   - **Texture Type**: `Sprite (2D and UI)`
   - **Sprite Mode**: `Single`
   - **Compression**: `None` (ou `High Quality`) — é ilustração pintada com bastante detalhe, compressão agressiva (`Normal Quality`/`Low Quality`) cria artefatos visíveis, principalmente no cabelo.
   - **Max Size**: `1024` já é suficiente (a imagem tem 520px de largura).
   - **Filter Mode**: `Bilinear`.
   - **Pivot**: `Bottom` se o retrato vai "subir" de baixo da caixa de diálogo; `Center` se for só encaixar dentro de um quadro fixo da UI.
3. Na cena, dentro do Canvas da caixa de diálogo, adicione um `UI > Image` (não precisa ser `SpriteRenderer`, já que é elemento de interface) e arraste o sprite correspondente para o campo **Source Image**. Marque **Preserve Aspect** se o RectTransform não for exatamente 520:860.
4. Ligando ao sistema de diálogo que já está no projeto (`NPCDialogueController.cs`, ver `integracao-claude-unity.md`): adicione um campo público `Image retrato` nesse script (ou num `DialogueUIController` equivalente) e, antes de mostrar a fala de cada personagem, troque `retrato.sprite` pelo sprite de Kai, Souta ou Ren conforme quem estiver falando.

## Observação

Os 3 retratos têm exatamente o mesmo recorte (mesma posição de cabeça/ombros), então dá pra trocar o `sprite` no mesmo `Image` sem a arte "pular" de posição na tela.
