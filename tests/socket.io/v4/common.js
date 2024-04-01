'use strict';

function registerEvents(io) {
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
}

//TODO: need to refactor and reuse this function

module.exports = registerEvents