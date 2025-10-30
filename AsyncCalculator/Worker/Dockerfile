# Stage 1: Build
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app

# Stage 2: Runtime
FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app .

# Force console output to appear in docker logs
ENV DOTNET_UNBUFFERED=1

# Environment variables to configure RabbitMQ connection
ENV RABBITMQ_HOST=localhost
ENV RABBITMQ_USER=guest
ENV RABBITMQ_PASS=guest
ENV RABBITMQ_PORT=5672

ENTRYPOINT ["dotnet", "Worker.dll"]
