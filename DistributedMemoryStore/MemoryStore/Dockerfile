#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
WORKDIR /src
COPY ["MemoryStore/MemoryStore.csproj", "MemoryStore/"]
RUN dotnet restore "MemoryStore/MemoryStore.csproj"
COPY . .
WORKDIR "/src/MemoryStore"
RUN dotnet build "MemoryStore.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "MemoryStore.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "MemoryStore.dll"]