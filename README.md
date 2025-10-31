
# Asynccalculator

Projeto que implementa uma calculadora simples com apenas a operação de soma, porém de maneira assincrona através de um message broker e com arquitetura de microsserviços rodando em docker.
Deploye todos os microsserviços e acesse em http://localhost:8080

# Deployando container do mongodb
    1. docker pull mongo
    2. docker run --name mongodb -p 27017:27017 -v mongo-data:/data/db -d mongo
    3. docker network create rabbit_network
    4. docker connect network rabbit_network mongodb

# Buildando worker e deployando o container
    1. cd Worker
    2. dotnet add package MongoDB.Driver
    3. dotnet add package RabbitMQ.Client
    4. dotnet build
    5. docker build -t rabbitmq-worker .
    6. docker compose up -d --build 

# Buildando API e deployando o container
    1. cd API
    2. npm install
    3. docker build -t apiserver .
    4. docker compose up -d --build 

# Buildando Frontend angular e deployando o container
    1. cd Frontend
    2. npm install -g @angular/cli
    3. docker build -t angularserver .
    4. docker compose up -d --build 

