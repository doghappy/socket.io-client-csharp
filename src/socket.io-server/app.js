"use strict";
Object.defineProperty(exports, "__esModule", { value: true });
const socket = require("socket.io");
const http = require("http");
console.log('socket.io-server');
const server = http.createServer();
const io = socket(server, {
    path: "/path",
    pingInterval: 10000,
    pingTimeout: 5000,
    transports: ["websocket"]
});
io.on("connection", socket => {
    socket.on("hi", name => {
        socket.emit("hi", `hi ${name}, You are connected to the server`);
    });
});
const nsp = io.of("/nsp");
nsp.on("connection", socket => {
    socket.on("hi", name => {
        socket.emit("hi", `hi ${name}, You are connected to the server - nsp`);
    });
});
server.listen(11000);
//# sourceMappingURL=app.js.map