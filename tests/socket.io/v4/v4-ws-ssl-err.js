'use strict';

const https = require('https');
const socket = require('socket.io');
const fs = require('fs');

const server = https.createServer({
    key: fs.readFileSync('cert/private.key'),
    cert: fs.readFileSync('cert/cert.crt')
}, (req, res) => {
    if (req.method === 'GET' && req.url === '/hello') {
        res.end('Hello World!\n');
    }
});
const port = process.env.PORT || 11404;

var io = socket(server, {
    pingInterval: 5000,
    pingTimeout: 10000,
    cors: {
        origin: "*",
        methods: ["GET", "POST"]
    }
});

const registerEvents = require('./common');
registerEvents(io);

server.listen(port, () => {
    console.log(`v4-ws-ssl-err: ${port}`);
});