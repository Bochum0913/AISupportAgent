FROM mcr.microsoft.com/dotnet/sdk:10.0 AS build
WORKDIR /source
COPY AISupportAgent/AISupportAgent.csproj AISupportAgent/
RUN dotnet restore AISupportAgent/AISupportAgent.csproj
COPY AISupportAgent/ AISupportAgent/
RUN dotnet publish AISupportAgent/AISupportAgent.csproj -c Release -o /publish --no-restore

FROM mcr.microsoft.com/dotnet/aspnet:10.0
WORKDIR /app
COPY --from=build /publish/ ./
ENV ASPNETCORE_HTTP_PORTS=8080
EXPOSE 8080
ENTRYPOINT ["dotnet", "AISupportAgent.dll"]
