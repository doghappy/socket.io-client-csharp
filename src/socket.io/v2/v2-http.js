'use strict';

const http = require('http');
const socket = require('socket.io');
// const express = require('express')
// const app = express()
// const server = http.createServer(app);
const server = http.createServer();
const port = 11201;

// app.use('/static', express.static('static'))

var io = socket(server, {
    transports: ["polling"]
});

io.on('connection', socket => {
    socket.on('1:emit', data => {
        socket.emit('1:emit', data);
    });
    socket.on('2:emit', (d1, d2) => {
        socket.emit('2:emit', d1, d2);
    });
    socket.on('1:ack', (data, cb) => {
        cb(data);
    });
    socket.on('get_auth', cb => {
        cb(socket.handshake.auth);
    });
    socket.on('get_headers', cb => {
        cb(socket.handshake.headers.customheader);
    });
    socket.on('disconnect', close => {
        socket.disconnect(close);
    });
    socket.on('callback_step1', () => {
        socket.emit('callback_step2', (arg1) => {
            socket.emit("callback_step3", arg1 + '-server');
        });
    });
});

const nsp = io.of("/nsp");
nsp.on("connection", socket => {
    socket.on('1:emit', data => {
        socket.emit('1:emit', data);
    });
    socket.on('2:emit', (d1, d2) => {
        socket.emit('2:emit', d1, d2);
    });
    socket.on('1:ack', (data, cb) => {
        cb(data);
    });
    socket.on('get_auth', cb => {
        cb(socket.handshake.auth);
    });
    socket.on('get_headers', cb => {
        cb(socket.handshake.headers.customheader);
    });
    socket.on('disconnect', close => {
        socket.disconnect(close);
    });
    socket.on('callback_step1', () => {
        socket.emit('callback_step2', (arg1) => {
            socket.emit("callback_step3", arg1 + '-server');
        });
    });
});

server.listen(port, () => {
    console.log(`v2-http: ${port}`);
});