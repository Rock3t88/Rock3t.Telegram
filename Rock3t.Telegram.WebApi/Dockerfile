#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["Rock3t.Telegram.WebApi/Rock3t.Telegram.WebApi.csproj", "Rock3t.Telegram.WebApi/"]
RUN dotnet restore "Rock3t.Telegram.WebApi/Rock3t.Telegram.WebApi.csproj"
COPY . .
WORKDIR "/src/Rock3t.Telegram.WebApi"
RUN dotnet build "Rock3t.Telegram.WebApi.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Rock3t.Telegram.WebApi.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Rock3t.Telegram.WebApi.dll"]