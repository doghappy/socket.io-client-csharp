'use strict';

const http = require('http');
const socket = require('socket.io');
// const express = require('express')
// const app = express()
// const server = http.createServer(app);
const server = http.createServer();
const port = process.env.PORT || 11210;

// app.use('/static', express.static('static'))

var io = socket(server, {
    transports: ["polling"],
    pingInterval: 5000,
    pingTimeout: 10000,
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
    socket.on('get_header', (key, cb) => {
        cb(socket.handshake.headers[key]);
    });
    socket.on('disconnect', close => {
        socket.disconnect(close);
    });
    socket.on('client will be sending data to server', () => {
        socket.emit('client sending data to server', (arg) => {
            socket.emit("server received data", arg);
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
    socket.on('get_header', (key, cb) => {
        cb(socket.handshake.headers[key]);
    });
    socket.on('disconnect', close => {
        socket.disconnect(close);
    });
    socket.on('client will be sending data to server', () => {
        socket.emit('client sending data to server', (arg) => {
            socket.emit("server received data", arg);
        });
    });
});

server.listen(port, () => {
    console.log(`v2-http: ${port}`);
});