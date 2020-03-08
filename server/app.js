'use strict';

const server = require('http').createServer();
const io = require('socket.io')(server);
const pathNsp = io.of("/path");
const sleep = require("await-sleep");

//io.on("connect", async client => {
//    console.log(new Date().getSeconds());
//    await sleep(11000);
//    console.log(new Date().getSeconds());
//});

io.use(function (socket, next) {
    if (socket.request._query.throw) {
        next(new Error("Authentication error"));
    }
    next();
});

io.on('connection', client => {

    //client.use((packet, next) => {
    //    console.log(packet, "============");
    //    next();
    //});

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
    client.on('emit-noting', data => {
        client.emit("emit-noting");
    });
    client.on('callback', (data, fn) => {
        fn(data + " - server");
    });
    client.on('*', data => {
        client.emit("*", "**");
    });
    client.on('UnhandledEvent', data => {
        client.emit("UnhandledEvent-Server", data + " - server");
    });
    client.on('create room', data => {
        console.log("join room:" + data);
        client.join(data);
        io.to(data).emit(data, "I joined the room: " + data);
    });
    client.on('emit\\args\"', data => {
        console.log(typeof (data), "-----");
        client.emit("emit\\args\"", "channel", "emit-args-server");
    });
    client.on('args', data => {
        client.emit("args", "string", 1, false, null, undefined, { code: 200, message: { text: "qe", data: true } });
    });
    client.on('disconnect', () => {
        console.log(`disconnect: ${client.id}`);
    });
});


pathNsp.use(function (socket, next) {
    if (socket.request._query.throw) {
        next(new Error("Authentication error -- Ns"));
    }
    next();
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

    client.on('callback', (data, fn) => {
        fn(data + " - server/path");
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


// path
const serverWithPath = require('http').createServer();
const ioWithPath = require('socket.io')(serverWithPath, {
    path: "/test"
});
ioWithPath.on('connection', client => {

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
});
serverWithPath.listen(3001);