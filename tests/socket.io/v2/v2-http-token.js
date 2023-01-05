'use strict';

const http = require('http');
const socket = require('socket.io');
const server = http.createServer();
const port = 11211;

var io = socket(server, {
    transports: ["polling"]
});

io.use((socket, next) => {
    if (socket.handshake.query.token === "abc") {
        next();
    } else {
        next(new Error("Authentication error"));
    }
})

io.on('connection', socket => {
});

const nsp = io.of("/nsp");
nsp.use((socket, next) => {
    if (socket.handshake.query.token === "abc") {
        next();
    } else {
        next(new Error("Authentication error"));
    }
});

nsp.on("connection", socket => {
});

server.listen(port, () => {
    console.log(`v2-http-token: ${port}`);
});