var app = require('express')();
var http = require('http').createServer(app);
var io = require('socket.io')(http, {
    pingInterval: 10000,
    pingTimeout: 5000
});

app.get('/', (req, res) => {
    res.sendFile(__dirname + '/index.html');
});

io.use((socket, next) => {
    if (socket.handshake.query.token === "v3") {
        next();
    } else {
        next(new Error("Authentication error"));
    }
})

io.on('connection', socket => {
    console.log('connected: ' + socket.id);
    socket.on('hi', (msg) => {
        console.log('console log message: ' + msg);
        io.emit('hi', 'io: ' + msg);
    });

    socket.on("bytes", (name, data) => {
        const bytes = Buffer.from(data.bytes.toString() + " - server - " + name, "utf-8");
        socket.emit("bytes", {
            clientSource: data.source,
            source: "server",
            bytes
        });
    });

    socket.on('binary', (data) => {
        io.emit("binary", Buffer.from(data));
    });

    socket.on('binary-obj', (data) => {
        io.emit("binary-obj", {
            data: Buffer.from(data)
        });
    });

    socket.on('binary', (data) => {
        io.emit("binary", Buffer.from(data));
    });

    socket.on('binary-obj', (data) => {
        io.emit("binary-obj", {
            data: Buffer.from(data)
        });
    });

    socket.on("client binary callback", (msg) => {
        const binaryMessage = Buffer.from(msg.toString() + " - server", "utf-8");
        socket.emit("client binary callback", binaryMessage, clientMsg => {
            console.log(clientMsg);
            socket.emit("server binary callback called");
        });
    });

    socket.on("sever disconnect", close => {
        socket.disconnect(close)
    });
});

const nsp = io.of("/nsp");
nsp.on("connection", socket => {
    socket.on('hi', (msg) => {
        console.log('console log message: ' + msg);
        nsp.emit('hi', 'nsp: ' + msg);
    });

    socket.on("sever disconnect", close => {
        socket.disconnect(close)
    });
});

http.listen(11003, () => {
    console.log('listening on *:11003');
});