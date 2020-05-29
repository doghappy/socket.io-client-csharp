import * as io from "socket.io-client";
import * as fs from "fs";

//const cert = fs.readFileSync("../socket.io-server/cert/localhost.crt").toString();
//console.log(cert);

const socket = io(`http://localhost:11000/`, {
    //path: "/path",
    transports: ["websocket"],
    query: {
        token: "io"
    },
    //key: fs.readFileSync("cert/client1-key.pem").toString(),
    //cert: fs.readFileSync("cert/client1-crt.pem").toString(),
    //ca: fs.readFileSync("cert/ca-crt.pem").toString(),
    timeout: 5000,
});

socket.on("connect", () => {
    console.log(`socket.connected: ${socket.connected}`);

    socket.on("disconnect", reason => {
        console.log(`disconnect: ${reason}`);
    });

    socket.on("hi", message => {
        console.log(`server: ${message}`);
    });

    socket.emit("hi", "doghappy");

    socket.on("client cb", (msg, cb) => {
        console.log(`server: ${msg}`);
        cb({
            text: "socket.io-client",
            bytes: Buffer.from("socket.io-client", "utf-8")
        });
    });

    socket.emit("client cb", {
        text: "socket.io-client",
        bytes: Buffer.from("socket.io-client", "utf-8")
    });
});

//const nspSocket = io(`http://${host}:11000/nsp`, {
//    path: "/path",
//    transports: ["websocket"],
//    query: {
//        token: "io"
//    },
//    ca
//});

//nspSocket.on("connect", () => {
//    console.log(`nspSocket.connected: ${nspSocket.connected}`);

//    nspSocket.on("disconnect", reason => {
//        console.log(`disconnect: ${reason}`);
//    });

//    nspSocket.on("hi", message => {
//        console.log(`server: ${message}`);
//    });

//    nspSocket.emit("hi", "doghappy");
//    nspSocket.emit("ack", "doghappy ack", (data) => {
//        console.log(data);
//    });

//    //nspSocket.emit("sever disconnect", false);

//    nspSocket.emit("binary ack", "name", {
//        bytes: Buffer.from("socket.io-client", "utf-8"),
//        source: "client002 - source"
//    }, response => {
//        console.log(`clientSource: ${response.clientSource}`);
//        console.log(`source: ${response.source}`);
//        console.log(`bytes: ${response.bytes.toString()}`);
//    });
//})