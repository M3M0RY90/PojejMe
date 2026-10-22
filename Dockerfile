# slika, ki jo uporabimo za osnovo/strežnik
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS base
# delovni direktorij v sliki
WORKDIR /app
# aplikacija v zabojniku posluša na vratih 8080
ENV ASPNETCORE_HTTP_PORTS=8080
# vrata, ki jih želimo odpreti
EXPOSE 8080

# slika, ki jo uporabimo za izgradnjo
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src
# kopiranje projektne datoteke v delovni direktorij
COPY ["PojejMe.Api/PojejMe.Api.csproj", "PojejMe.Api/"]
# zagon ukaza za obnovo vseh odvisnosti
RUN dotnet restore "PojejMe.Api/PojejMe.Api.csproj"
COPY . .
WORKDIR "/src/PojejMe.Api"
# zagon ukaza za izgradnjo projekta
RUN dotnet build "PojejMe.Api.csproj" -c Release -o /app/build

FROM build AS publish
# zagon ukaza za objavo projekta
RUN dotnet publish "PojejMe.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

FROM base AS final
WORKDIR /app
# oznaka commita, ki jo poda GitHub Actions (lokalno ostane "lokalno")
ARG APP_VERSION=lokalno
ENV APP_VERSION=$APP_VERSION
# kopiranje datotek iz začasne slike v končno
COPY --from=publish /app/publish .
# zagon ukaza za zagon aplikacije
ENTRYPOINT ["dotnet", "PojejMe.Api.dll"]
