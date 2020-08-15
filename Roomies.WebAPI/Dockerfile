#See https://aka.ms/containerfastmode to understand how Visual Studio uses this Dockerfile to build your images for faster debugging.

ARG BASE_TAG=3.1-buster-slim
FROM mcr.microsoft.com/dotnet/core/aspnet:${BASE_TAG} AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY Roomies.WebAPI/Roomies.WebAPI.csproj Roomies.WebAPI/
RUN dotnet restore "Roomies.WebAPI/Roomies.WebAPI.csproj"
COPY . .
WORKDIR "/src/Roomies.WebAPI"
RUN dotnet build "Roomies.WebAPI.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Roomies.WebAPI.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Roomies.WebAPI.dll"]
