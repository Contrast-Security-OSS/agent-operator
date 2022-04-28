FROM mcr.microsoft.com/dotnet/aspnet:6.0 AS base
WORKDIR /app

FROM mcr.microsoft.com/dotnet/sdk:6.0 AS build
COPY src/Contrast.K8s.AgentOperator/Contrast.K8s.AgentOperator.csproj /source/src/Contrast.K8s.AgentOperator/
COPY tests/Contrast.K8s.AgentOperator.Tests/Contrast.K8s.AgentOperator.Tests.csproj /source/tests/Contrast.K8s.AgentOperator.Tests/
COPY Contrast.K8s.AgentOperator.sln /source/

WORKDIR /source
RUN dotnet restore

COPY . /source/
ARG BUILD_VERSION=0.0.1

RUN set -xe \
    && dotnet test -c Release -p:Version=${BUILD_VERSION} \
    && dotnet publish -c Release -o /app -p:Version=${BUILD_VERSION}

FROM base AS final

RUN set -xe \
    && addgroup operator-group \
    && useradd -G operator-group operator-user

WORKDIR /app
COPY --from=build /app .
RUN chown operator-user:operator-group -R .

USER operator-user

ENV ASPNETCORE_URLS=https://+:5001 \
    ASPNETCORE_ENVIRONMENT=Production

ENTRYPOINT ["dotnet", "Contrast.K8s.AgentOperator.dll"]
