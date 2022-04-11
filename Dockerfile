FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY src/Contrast.K8s.AgentOperator.Host/Contrast.K8s.AgentOperator.Host.csproj /source/src/Contrast.K8s.AgentOperator.Host/
WORKDIR /source/src/Contrast.K8s.AgentOperator.Host/
RUN dotnet restore

COPY . /source/
ARG BUILD_VERSION=0.0.1
RUN dotnet publish -c Release -o /app -p:Version=${BUILD_VERSION}

FROM base AS final
WORKDIR /app
COPY --from=build /app .

ENTRYPOINT ["dotnet", "Contrast.K8s.AgentOperator.Host.dll"]
