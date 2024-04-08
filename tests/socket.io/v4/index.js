const https = require('https');
const http = require('http');
const socket = require('socket.io');
const fs = require('fs');
const path = require('path');
const template = require('./template');

const wsServers = [
    {
        name: 'v4-ws',
        port: 11400,
        server: () => http.createServer(),
        options: {
            pingInterval: 5000,
            pingTimeout: 10000,
            cors: {
                origin: "*",
                methods: ["GET", "POST"]
            }
        },
        onCreated: template.registerEvents
    },
    {
        name: 'v4-ws-token',
        port: 11401,
        server: () => http.createServer(),
        options: {},
        onCreated: template.useAuthMiddlewares
    },
    {
        name: 'v4-ws-mp',
        port: 11402,
        server: () => http.createServer(),
        options: {
            parser: require('socket.io-msgpack-parser'),
            pingInterval: 5000,
            pingTimeout: 10000,
            cors: {
                origin: "*",
                methods: ["GET", "POST"]
            }
        },
        onCreated: template.registerEvents
    },
    {
        name: 'v4-ws-token-mp',
        port: 11403,
        server: () => http.createServer(),
        options: {
            parser: require('socket.io-msgpack-parser')
        },
        onCreated: template.useAuthMiddlewares
    },
    {
        name: 'v4-ws-ssl-err',
        port: 11404,
        server: () => {
            const server = https.createServer({
                key: fs.readFileSync(path.join(__dirname, 'cert', 'private.key')),
                cert: fs.readFileSync(path.join(__dirname, 'cert', 'cert.crt'))
            }, (req, res) => {
                if (req.method === 'GET' && req.url === '/hello') {
                    res.end('Hello World!\n');
                }
            });
            return server;
        },
        options: {
            pingInterval: 5000,
            pingTimeout: 10000,
            cors: {
                origin: "*",
                methods: ["GET", "POST"]
            }
        },
        onCreated: template.registerEvents
    }
];

const httpServers = [
    {
        name: 'v4-http',
        port: 11410,
        server: () => http.createServer(),
        options: {
            transports: ["polling"],
            pingInterval: 5000,
            pingTimeout: 10000,
        },
        onCreated: template.registerEvents
    },
    {
        name: 'v4-http-token',
        port: 11411,
        server: () => http.createServer(),
        options: {
            transports: ["polling"]
        },
        onCreated: template.useAuthMiddlewares
    },
    {
        name: 'v4-http-mp',
        port: 11412,
        server: () => http.createServer(),
        options: {
            parser: require('socket.io-msgpack-parser'),
            transports: ["polling"],
            pingInterval: 5000,
            pingTimeout: 10000,
        },
        onCreated: template.registerEvents
    },
    {
        name: 'v4-http-token-mp',
        port: 11413,
        server: () => http.createServer(),
        options: {
            parser: require('socket.io-msgpack-parser'),
            transports: ["polling"]
        },
        onCreated: template.useAuthMiddlewares
    },
    {
        name: 'v4-http-ssl-err',
        port: 11414,
        server: () => {
            const server = https.createServer({
                key: fs.readFileSync(path.join(__dirname, 'cert', 'private.key')),
                cert: fs.readFileSync(path.join(__dirname, 'cert', 'cert.crt'))
            }, (req, res) => {
                if (req.method === 'GET' && req.url === '/hello') {
                    res.end('Hello World!\n');
                }
            });
            return server;
        },
        options: {
            transports: ["polling"],
            pingInterval: 5000,
            pingTimeout: 10000,
        },
        onCreated: template.registerEvents
    }
];

let servers = [
    ...wsServers,
    ...httpServers
];

if (process.env.PORT && process.env.NAME){
    const server = servers.find(s => s.name === process.env.NAME);
    server.port = process.env.PORT;
    servers = [server];
}

for (const server of servers) {
    console.log(`Starting server '${server.name}' on port ${server.port}...`);
    template.start(server.name, server.port, server.server(), server.options, server.onCreated);
}