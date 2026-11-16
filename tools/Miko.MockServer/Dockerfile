FROM mcr.microsoft.com/dotnet/sdk:11.0 AS build
WORKDIR /source
COPY Miko.MockServer.csproj ./
RUN dotnet restore Miko.MockServer.csproj
COPY . ./
RUN dotnet publish Miko.MockServer.csproj -c Release -o /app --no-restore -p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:11.0
WORKDIR /app
COPY --from=build /app ./
ENV ASPNETCORE_URLS=http://+:8080
EXPOSE 8080 5900 8554
USER $APP_UID
ENTRYPOINT ["dotnet", "Miko.MockServer.dll"]
