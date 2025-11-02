'use strict';

const amqp = require('amqplib/callback_api');
const crypto = require('crypto');
const http = require('http');
const express = require('express');
const { MongoClient } = require('mongodb');


const port = process.env.PORT || 1337;
const amqpHost = process.env.RABBITMQ_HOST || 'rabbitmq';
const uri = "mongodb://mongodb:27017";
const client = new MongoClient(uri);
const app = express();

app.use(express.json());

app.use((req, res, next) => {
    res.header('Access-Control-Allow-Origin', '*');
    res.header('Access-Control-Allow-Methods', 'GET, POST, OPTIONS');
    res.header('Access-Control-Allow-Headers', 'Content-Type, Authorization');

    if (req.method === 'OPTIONS') {
        return res.sendStatus(204)
    }

    next();
});

app.get('/GetResult/:id', async (req, res) => {
    console.log(` [x] GET request to /GetResult`);

    try {
        var id = req.params.id;
        var result;
        if (id == null || id == undefined) {
            res.status(404)
                .header("Content-Type", "application/json");
        }

        do {
            result = await QueryResult(id);
            if (result == null) 
                await new Promise(resolve => setTimeout(resolve, 1000));

        } while (result == null)

        console.log(" [x] FINAL RESULT: ", result);

        res.status(200)
            .header("Content-Type", "application/json")
            .json({result})

    } catch (ex) {
        console.log(` [!] Internal Server Error :: ${ex}`);
        res.status(500)
            .header("Content-Type", "application/json");
    }

})

app.post('/CalcAsync', async (req, res) => {
    console.log(` [x] POST request to CalcAsync`);

    try {
        var requestPayload = req.body;
        console.log(` [x] Request payload received: ${JSON.stringify(requestPayload)}`);

        var id = await Calculate(requestPayload.n1, requestPayload.n2);
        console.log(` [x] messageId from Calculate(): ${id}`);

        const payload = { messageId: id };

        res.status(200)
            .header("Content-Type", "application/json")
            .json(payload)
    } catch (ex) {
        console.log(` [!] Internal Server Error :: ${ex}`);
        res.status(500)
            .header("Content-Type", "application/json");
    }
    
})

app.listen(port, () => {
    console.log(` [x] Server running on http://localhost:${port}`);
});


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

