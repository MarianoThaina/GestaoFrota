// Servidor local simples, so para visualizar o prototipo no navegador.
// Rode com: node servidor.cjs   e abra http://localhost:5173
const http = require('http');
const fs = require('fs');
const path = require('path');

const TIPOS = { '.html':'text/html; charset=utf-8', '.css':'text/css; charset=utf-8', '.js':'text/javascript; charset=utf-8' };

http.createServer((req, res) => {
  let arquivo = decodeURIComponent(req.url.split('?')[0]);
  if (arquivo === '/') arquivo = '/index.html';
  const caminho = path.join(__dirname, arquivo);
  fs.readFile(caminho, (erro, dados) => {
    if (erro) { res.writeHead(404); res.end('Nao encontrado'); return; }
    res.writeHead(200, { 'Content-Type': TIPOS[path.extname(caminho)] || 'application/octet-stream' });
    res.end(dados);
  });
}).listen(5173, () => console.log('Prototipo em http://localhost:5173'));
