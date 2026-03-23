// server/app.js
import express from 'express';
import http from 'http';
import { Server as SocketIO } from 'socket.io';
import { readFile, writeFile } from 'fs/promises';
import fs from 'fs';                             // ← именно отсюда watchFile
import path from 'path';
import { fileURLToPath } from 'url';

const __filename = fileURLToPath(import.meta.url);
const __dirname  = path.dirname(__filename);

const app    = express();
const server = http.createServer(app);
const io     = new SocketIO(server, { cors: { origin: '*' } });

const PORT = 3000;

app.use(express.static(path.join(__dirname, '..', 'public')));
app.use(express.json());

const dataFile = name => path.join(__dirname, '..', 'data', `${name}.json`);

app.get('/api/:f', async (req, res) => {
  try { res.json(JSON.parse(await readFile(dataFile(req.params.f), 'utf8'))); }
  catch { res.sendStatus(404); }
});
app.post('/api/:f', async (req, res) => {
  try {
    await writeFile(dataFile(req.params.f), JSON.stringify(req.body, null, 2));
    res.sendStatus(204);
  } catch { res.sendStatus(500); }
});

// ‑‑‑ отслеживаем изменения и шлём клиентам
function watchAndEmit(fileName, eventName){
  const filePath = dataFile(fileName);

  // 1) при новом подключении сразу отправляем актуальный файл
  io.on('connection', async socket => {
    const json = await readFile(filePath, 'utf8');
    socket.emit(eventName, JSON.parse(json));
  });

  // 2) любое изменение файла -> push всем
  fs.watchFile(filePath, async () => {
    const json = await readFile(filePath, 'utf8');
    io.emit(eventName, JSON.parse(json));
  });
}

watchAndEmit('scores',           'scores:update');
watchAndEmit('active_modifiers', 'mods:update');

server.listen(PORT, () => console.log(`🚀  http://localhost:${PORT}`));
