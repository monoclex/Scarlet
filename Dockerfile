FROM mcr.microsoft.com/dotnet/core/aspnet:3.1-buster-slim AS base
WORKDIR /app
EXPOSE 80
EXPOSE 443

FROM mcr.microsoft.com/dotnet/core/sdk:3.1-buster AS build
WORKDIR /src
COPY ["src/Scarlet.csproj", "./Scarlet.csproj"]
RUN dotnet restore "./Scarlet.csproj"
COPY ["src/", "."]
WORKDIR "/src/."
RUN dotnet build "Scarlet.csproj" -c Release -o /app/build

FROM build AS publish
RUN dotnet publish "Scarlet.csproj" -c Release -o /app/publish

FROM base AS final
WORKDIR /app
COPY --from=publish /app/publish .
ENTRYPOINT ["dotnet", "Scarlet.dll"]