function start(name, port, server, options, onCreated) {
    const socket = require('socket.io');
    const io = socket(server, options);
    onCreated(io);
    server.listen(port, () => {
        console.log(`Started server '${name}' on port ${port}`);
    });
}

function useAuthMiddlewares(io){
    useIOAuthMiddleware(io);
    useNspAuthMiddleware(io);
}

function useIOAuthMiddleware(io){
    io.use((socket, next) => {
        if (socket.handshake.query.token === "abc") {
            next();
        } else {
            next(new Error("Authentication error"));
        }
    });
}

function useNspAuthMiddleware(io){
    const nsp = io.of("/nsp");
    nsp.use((socket, next) => {
        if (socket.handshake.query.token === "abc") {
            next();
        } else {
            next(new Error("Authentication error"));
        }
    });
}

function registerEvents(io) {
    registerIOEvents(io);
    registerNspEvents(io);
}

function registerIOEvents(io) {
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
}

function registerNspEvents(io) {
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

module.exports.start = start;
module.exports.useAuthMiddlewares = useAuthMiddlewares;
module.exports.useIOAuthMiddleware = useIOAuthMiddleware;
module.exports.useNspAuthMiddleware = useNspAuthMiddleware;
module.exports.registerEvents = registerEvents;
module.exports.registerIOEvents = registerIOEvents;
