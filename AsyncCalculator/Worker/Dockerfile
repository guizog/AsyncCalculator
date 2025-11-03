FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

COPY *.csproj ./
RUN dotnet restore

COPY . ./
RUN dotnet publish -c Release -o /app

FROM mcr.microsoft.com/dotnet/runtime:9.0
WORKDIR /app
COPY --from=build /app .

ENV DOTNET_UNBUFFERED=1

ENV RABBITMQ_HOST=localhost
ENV RABBITMQ_USER=guest
ENV RABBITMQ_PASS=guest
ENV RABBITMQ_PORT=5672

ENTRYPOINT ["dotnet", "Worker.dll"]
