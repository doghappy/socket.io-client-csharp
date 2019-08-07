'use strict';

const server = require('http').createServer();
const io = require('socket.io')(server);
const pathNsp = io.of("/path");

io.on('connection', client => {
    console.log("aaaaaaaaaaaaa");
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
    client.on('close', data => {
        if (data === "close") {
            client.disconnect();
        }
    });
    client.on('create room', data => {
        console.log("join room:" + data);
        client.join(data);
        io.to(data).emit(data, "I joined the room: " + data);
    });
    client.on('disconnect', () => {
        console.log(`disconnect: ${client.id}`);
    });
});

pathNsp.on('connection', client => {
    console.log(client.id);
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
    client.on('close', data => {
        console.log("bbbbbbbbbbbb");
        if (data === "close") {
            client.disconnect();
        }
    });
    client.on('ws_message -new', data => {
        console.log(data);
        client.emit("ws_message -new", "message from server");
    });
    client.on('disconnect', () => {
        console.log(`disconnect: ${client.id}`);
    });
});

server.listen(3000);

//https://stackoverflow.com/questions/19150220/creating-rooms-in-socket-io

console.log('Socket IO server started');