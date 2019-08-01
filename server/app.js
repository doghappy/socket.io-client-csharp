'use strict';

const server = require('http').createServer();
const io = require('socket.io')(server);
const pathNsp = io.of("/path");

io.on('connection', client => {
    client.on('test', data => {
        const type = typeof data;
        if (type === "string") {
            client.emit("test", data + " - server");
        } else if (type === "object") {
            data.source = "server";
            client.emit("test", data);
        } else if (type === "object") {
            if (Array.isArray(data)) {
                client.emit("test", data);
            } else {
                data.source = "server";
                client.emit("test", data);
            }
        } else {
            client.emit("test", "unknow type - server");
        }
    });
    client.on('disconnect', () => {
        console.log(`disconnect: ${client.id}`);
    });
});

pathNsp.on('connection', client => {
    client.on('test', data => {
        const type = typeof data;
        if (type === "string") {
            client.emit("test", data + " - server/path");
        } else if (type === "object") {
            data.source = "server/path";
            client.emit("test", data);
        } else if (type === "object") {
            if (Array.isArray(data)) {
                client.emit("test", data);
            } else {
                data.source = "server/path";
                client.emit("test", data);
            }
        } else {
            client.emit("test", "unknow type - server/path");
        }
    });
    client.on('disconnect', () => {
        console.log(`disconnect: ${client.id}`);
    });
});

server.listen(3000);

console.log('Socket IO server started');