'use strict';

var amqp = require('amqplib/callback_api');
var crypto = require('crypto');
var http = require('http');
const { MongoClient } = require('mongodb');


var port = process.env.PORT || 1337;
var amqpHost = process.env.RABBITMQ_HOST || 'rabbitmq';
var uri = "mongodb://mongodb:27017";
const client = new MongoClient(uri);


http.createServer(async function (req, res) {

    res.setHeader('Access-Control-Allow-Origin', '*');
    res.setHeader('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.setHeader('Access-Control-Allow-Headers', 'Content-Type');

    if (req.method === 'OPTIONS') {
        res.writeHead(204);
        res.end();
        return;
    }

    console.log(` [x] Logging request: ${req.url} : ${req.method}`);
    var id = '';

    var splitUrl = req.url.split("/").filter(Boolean);
    if (req.method == "GET") {
        if (splitUrl[0] == "GetResult" && splitUrl[1] != null) {
            var result;
            do {
                result = await QueryResult(splitUrl[1]);
            } while (result == null)

            console.log(" [x] FINAL RESULT: ", result);

            res.writeHead(200, { 'Content-Type': 'application/json' });
            res.end(JSON.stringify(result));
        }
    }
    if (req.method == "POST") {
        if (splitUrl[0] == "CalcAsync") {
            var body = '';

            console.log(` [x] POST request to CalcAsync`);

            req.on('data', chunk => {
                body += chunk.toString();
            });

            req.on('end', async () => {
                var data = JSON.parse(body);
                console.log(`Parsed payload: ${JSON.stringify(data)}`);
                id = await Calculate(data.n1, data.n2);

                console.log(` [x] messageId from Calculate(): ${id}`);

                res.writeHead(200, { 'Content-Type': 'application/json' });
                const responsePayload = { messageId: id };
                res.end(JSON.stringify(responsePayload));
            })

        }
    }

}).listen(port);

async function Calculate(n1, n2) {

    return new Promise((resolve, reject) => {
        amqp.connect(`amqp://${amqpHost}`, function (error0, connection) {
            if (error0) {
                reject(error0);
            }
            connection.createChannel(async function (error1, channel) {
                if (error1)
                    reject(error1);

                try {
                    var queue = "calc_queue";
                    var messageId = crypto.randomUUID();
                    await InsertSum(messageId, n1, n2);

                    await channel.assertQueue(queue, {
                        durable: true
                    });

                    channel.sendToQueue(queue, Buffer.from(messageId));
                    console.log(" [x] Sent %s", messageId);

                    resolve(messageId);
                } catch (err) {
                    reject(err);
                } finally {
                    setTimeout(() => connection.close(), 500);
                }
            });
        });
    })
}


async function QueryResult(uuid) {
    try {
        await client.connect();
        console.log(" [x] Connected to MongoDB");

        const db = client.db("asynccalculator");
        const collection = db.collection("records");

        console.log(" [x] Querying if record is status FINISHED");

        const record = await collection.findOne({ _id: uuid });

        console.log(" [x] Record queried for status:", record);

        return (record.status == "FINISHED") ? record.result : null;

    } catch (err) {
        console.error(err);
    } finally {
        await client.close();
    }
}

async function InsertSum(uuid, num1, num2) {
    try {
        await client.connect();
        console.log(" [x] Connected to MongoDB");

        const db = client.db("asynccalculator");
        const collection = db.collection("records");

        const insertResult = await collection.insertOne(
            {
                _id: uuid,
                number1: num1,
                number2: num2,
                result: 0,
                status: "PENDING"
            });
        console.log(" [x] Inserted document:", insertResult.insertedId);

    } catch (err) {
        console.error(err);
    } finally {
        await client.close();
    }
}

