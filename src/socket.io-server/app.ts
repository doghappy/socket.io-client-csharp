import * as socket from "socket.io"
import * as http from "http";
import * as https from "https";
import * as fs from "fs";

console.log('socket.io-server');

const server = http.createServer();
//const server = https.createServer({
//    key: fs.readFileSync("cert/server-key.pem").toString(),
//    cert: fs.readFileSync("cert/server-crt.pem").toString(),
//    ca: fs.readFileSync("cert/ca-crt.pem").toString(),
//    requestCert: true,
//    rejectUnauthorized: true
//});
const io = socket(server, {
    pingInterval: 10000,
    pingTimeout: 5000,
    transports: ["websocket"],
    //path: "/path"
});

io.use((socket, next) => {
    if (socket.handshake.query.token === "io") {
        next();
    } else {
        next(new Error("Authentication error"));
    }
})

io.on("connection", socket => {
    console.log(`connect: ${socket.id}`);
    //console.log(`cert: ${socket.client.request.client.getPeerCertificate().toString()}`)

    socket.on("disconnect", reason => {
        console.log(`disconnect: ${reason}`);
    });

    socket.on("hi", name => {
        socket.emit("hi", `hi ${name}, You are connected to the server`);
    });

    socket.on("ContinuousBinary", data => {
        socket.emit("ContinuousBinary", {
            progress: data.progress,
            length: data.binary.length
        });
    })

    socket.on("ack", (name, fn) => {
        fn({
            result: true,
            message: `ack(${name})`
        });
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

    socket.on("sever disconnect", close => {
        socket.disconnect(close)
    });

    socket.on("binary ack", (name, data, fn) => {
        const bytes = Buffer.from(data.bytes.toString() + " - server - " + name, "utf-8");
        fn({
            clientSource: data.source,
            source: "server",
            bytes
        });
    });

    socket.on("change", (val1, val2) => {
        socket.emit("change", val2, val1);
    });

    socket.on("client message callback", (msg) => {
        socket.emit("client message callback", msg + " - server", clientMsg => {
            console.log(clientMsg);
            socket.emit("server message callback called");
        });
    });

    socket.on("client binary callback", (msg) => {
        const binaryMessage = Buffer.from(msg.toString() + " - server", "utf-8");
        socket.emit("client binary callback", binaryMessage, clientMsg => {
            console.log(clientMsg);
            socket.emit("server binary callback called");
        });
    });
});

const nsp = io.of("/nsp");
nsp.on("connection", socket => {
    console.log(`connect: ${socket.id}`);

    socket.on("disconnect", reason => {
        console.log(`disconnect: ${reason}`);
        if (reason === 'io server disconnect') {
            // the disconnection was initiated by the server, you need to reconnect manually
        }
    });

    socket.on("hi", name => {
        socket.emit("hi", `hi ${name}, You are connected to the server - nsp`);
    });

    socket.on("ack", (name, fn) => {
        fn({
            result: true,
            message: `ack(${name})`
        });
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

    socket.on("sever disconnect", close => {
        socket.disconnect(close)
    });

    socket.on("binary ack", (name, data, fn) => {
        const bytes = Buffer.from(data.bytes.toString() + " - server - " + name, "utf-8");
        fn({
            clientSource: data.source,
            source: "server",
            bytes
        });
    });

    socket.on("change", (val1, val2) => {
        socket.emit("change", val2, val1);
    });

    socket.on("change", (val1, val2) => {
        socket.emit("change", val2, val1);
    });

    socket.on("client message callback", (msg) => {
        socket.emit("client message callback", msg + " - server", clientMsg => {
            console.log(clientMsg);
            socket.emit("server message callback called");
        });
    });

    socket.on("client binary callback", (msg) => {
        const binaryMessage = Buffer.from(msg.toString() + " - server", "utf-8");
        socket.emit("client binary callback", binaryMessage, clientMsg => {
            console.log(clientMsg);
            socket.emit("server binary callback called");
        });
    });
});

server.listen(11000, () => {
    console.log(`Listening HTTPS on 11000`);
});