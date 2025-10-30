'use strict';

var amqp = require('amqplib/callback_api');
var crypto = require('crypto');
var http = require('http');
const { MongoClient } = require('mongodb');


var port = process.env.PORT || 1337;
var amqpHost = process.env.RABBITMQ_HOST || 'rabbitmq';
var uri = "mongodb://mongodb:27017";
const client = new MongoClient(uri);


http.createServer(function (req, res) {

    console.log(` [x] Logging request: ${req}`);

    if (req.method == "GET") {
        var splitUrl = req.url.split("/").filter(Boolean);
        //console.log(`--------URL: ${splitUrl}`);

        if (req.url == "/") {
            //replace for /index.html file
            res.writeHead(200, { 'Content-Type': 'text/plain' });
            res.end('Hello World\n');
        }

        if (splitUrl[0] == "GetResult" && splitUrl[1] != null) {
            QueryResult(splitUrl[1]);
        }

        if (req.url == "/CalcAsync") { // for testing without interface
            Calculate();
        }

    }
    if (req.method == "POST") {
        if (req.url == "/CalcAsync"){
            Calculate();
        }
    }
    
}).listen(port);

async function Calculate() {
    amqp.connect(`amqp://${amqpHost}`, function (error0, connection) {
        if (error0) {
            throw error0;
        }
        connection.createChannel(async function (error1, channel) {
            if (error1)
                throw error1;

            var queue = "calc_queue";
            var messageId = crypto.randomUUID();
            var num1 = 1;
            var num2 = 3;
            await InsertSum(messageId, num1, num2);

            await channel.assertQueue(queue, {
                durable: true
            });

            channel.sendToQueue(queue, Buffer.from(messageId));
            console.log(" [x] Sent %s", messageId);

            do {
                var result = await QueryResult(messageId);
            } while (result.status != "FINISHED")

            console.log(" [x] FINAL RESULT: ", result.result);

        });
    });

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

        return record;

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

        //const docs = await collection.find({}).toArray();
        //console.log(" [x] Documents in collection:", docs);
    } catch (err) {
        console.error(err);
    } finally {
        await client.close();
    }
}

