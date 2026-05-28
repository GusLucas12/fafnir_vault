FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build

WORKDIR /src

COPY fanfnir_back.csproj ./
RUN dotnet restore fanfnir_back.csproj

COPY . ./
RUN dotnet publish fanfnir_back.csproj -c Release -o /app/publish /p:UseAppHost=false

FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime

WORKDIR /app

ENV ASPNETCORE_URLS=http://+:8080
ENV DOTNET_RUNNING_IN_CONTAINER=true

EXPOSE 8080

COPY --from=build /app/publish ./

ENTRYPOINT ["dotnet", "fanfnir_back.dll"]
