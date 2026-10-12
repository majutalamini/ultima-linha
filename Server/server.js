/**
 * Backend mínimo para o jogo "última linha".
 *
 * Responsabilidade única: guardar a ANTHROPIC_API_KEY do lado do servidor e
 * fazer proxy das chamadas à Claude API. O jogo (cliente Unity) NUNCA deve
 * ter essa chave embutida — ele só fala com este servidor.
 *
 * Rodar localmente para testes:
 *   npm install
 *   export ANTHROPIC_API_KEY="sua-key-aqui"
 *   node server.js
 *
 * Em produção: implante este servidor (Render, Fly.io, Cloud Run, uma
 * function serverless, etc.) e configure ANTHROPIC_API_KEY como secret/env
 * var na plataforma escolhida — nunca commitado no código.
 */

const express = require("express");
const cors = require("cors");
const Anthropic = require("@anthropic-ai/sdk");

const PORT = process.env.PORT || 3000;
const MODEL = process.env.CLAUDE_MODEL || "claude-haiku-4-5-20251001";

if (!process.env.ANTHROPIC_API_KEY) {
  console.error(
    "ERRO: defina a variável de ambiente ANTHROPIC_API_KEY antes de iniciar o servidor."
  );
  process.exit(1);
}

const anthropic = new Anthropic({ apiKey: process.env.ANTHROPIC_API_KEY });

const app = express();
app.use(cors()); // em produção, restrinja a origem conforme necessário
app.use(express.json({ limit: "32kb" }));

/**
 * Limite bem simples de requisições por IP, só para não deixar a rota
 * totalmente aberta durante o desenvolvimento. Para produção, troque por
 * algo mais robusto (ex.: express-rate-limit, ou rate limiting na borda).
 */
const requestsByIp = new Map();
const MAX_REQUESTS_PER_MINUTE = 20;

function isRateLimited(ip) {
  const now = Date.now();
  const windowStart = now - 60_000;
  const timestamps = (requestsByIp.get(ip) || []).filter((t) => t > windowStart);
  timestamps.push(now);
  requestsByIp.set(ip, timestamps);
  return timestamps.length > MAX_REQUESTS_PER_MINUTE;
}

/**
 * POST /api/npc-dialogue
 * Body esperado:
 * {
 *   "npcName": "Guarda da Última Linha",
 *   "npcPersonality": "cansado, cínico, mas leal",
 *   "context": "o jogador acabou de atravessar a fronteira sem permissão",
 *   "playerMessage": "opcional: o que o jogador disse ao NPC"
 * }
 */
app.post("/api/npc-dialogue", async (req, res) => {
  const ip = req.ip;
  if (isRateLimited(ip)) {
    return res.status(429).json({ error: "Muitas requisições. Tente novamente em instantes." });
  }

  const { npcName, npcPersonality, context, playerMessage } = req.body || {};

  if (!npcName || !context) {
    return res.status(400).json({ error: "npcName e context são obrigatórios." });
  }

  const systemPrompt = [
    "Você gera falas curtas de NPC para um jogo 2D chamado 'última linha'.",
    "Responda APENAS com a fala do personagem, em português do Brasil, sem aspas, sem rótulos, no máximo 2 frases.",
    "Nunca quebre o personagem nem mencione que você é uma IA.",
  ].join(" ");

  const userPrompt = [
    `Personagem: ${npcName}`,
    npcPersonality ? `Personalidade: ${npcPersonality}` : null,
    `Situação atual: ${context}`,
    playerMessage ? `O jogador disse: "${playerMessage}"` : null,
    "Gere a próxima fala deste personagem.",
  ]
    .filter(Boolean)
    .join("\n");

  try {
    const message = await anthropic.messages.create({
      model: MODEL,
      max_tokens: 120,
      system: systemPrompt,
      messages: [{ role: "user", content: userPrompt }],
    });

    const text = message.content
      .filter((block) => block.type === "text")
      .map((block) => block.text)
      .join(" ")
      .trim();

    res.json({ text });
  } catch (err) {
    console.error("Erro chamando a Claude API:", err);
    res.status(502).json({ error: "Falha ao gerar diálogo." });
  }
});

app.get("/health", (_req, res) => res.json({ ok: true }));

app.listen(PORT, () => {
  console.log(`Backend do 'última linha' rodando em http://localhost:${PORT}`);
});
