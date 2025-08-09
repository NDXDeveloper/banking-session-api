# Utilise l'image officielle .NET 8 SDK pour la construction
FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /app

# Copie les fichiers de projet et restaure les dépendances
COPY src/BankingSessionAPI/*.csproj ./src/BankingSessionAPI/
RUN dotnet restore src/BankingSessionAPI/BankingSessionAPI.csproj

# Copie tout le code source
COPY . .

# Construit l'application
WORKDIR /app/src/BankingSessionAPI
RUN dotnet publish -c Release -o /app/publish --no-restore

# Utilise l'image runtime pour l'exécution
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Installe les certificats et outils de sécurité
RUN apt-get update && apt-get install -y \
    ca-certificates \
    curl \
    && rm -rf /var/lib/apt/lists/*

# Crée un utilisateur non-root pour la sécurité
RUN addgroup --system --gid 1001 banking && \
    adduser --system --uid 1001 --gid 1001 banking

# Crée les répertoires nécessaires
RUN mkdir -p /app/logs && \
    chown -R banking:banking /app

# Copie les binaires depuis l'étape de construction
COPY --from=build /app/publish .

# Change vers l'utilisateur non-root
USER banking

# Expose le port
EXPOSE 8080
EXPOSE 8081

# Variables d'environnement
ENV ASPNETCORE_ENVIRONMENT=Production
ENV ASPNETCORE_URLS=http://+:8080;https://+:8081
ENV DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=false

# Point d'entrée
ENTRYPOINT ["dotnet", "BankingSessionAPI.dll"]

# Métadonnées
LABEL maintainer="Nicolas DEOUX <NDXdev@gmail.com>"
LABEL description="API Banking Sessions .NET 8 - Gestion sécurisée des sessions bancaires"
LABEL version="1.0.0"