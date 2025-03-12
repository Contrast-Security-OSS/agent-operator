# Contrast Security, Inc licenses this file to you under the Apache 2.0 License.
# See the LICENSE file in the project root for more information.

FROM mcr.microsoft.com/dotnet/aspnet:8.0.14-bookworm-slim AS base

# To aid in debugging.
RUN set -xe \
    && apt-get update \
    && apt-get install -y --no-install-recommends curl jq \
    && apt-get clean && rm -rf /var/lib/apt/lists/*

FROM mcr.microsoft.com/dotnet/sdk:8.0.407-bookworm-slim AS build
WORKDIR /source

# Restore
COPY src/Contrast.K8s.AgentOperator/Contrast.K8s.AgentOperator.csproj /source/src/Contrast.K8s.AgentOperator/
COPY tests/Contrast.K8s.AgentOperator.Tests/Contrast.K8s.AgentOperator.Tests.csproj /source/tests/Contrast.K8s.AgentOperator.Tests/
COPY tests/Contrast.K8s.AgentOperator.FunctionalTests/Contrast.K8s.AgentOperator.FunctionalTests.csproj /source/tests/Contrast.K8s.AgentOperator.FunctionalTests/
COPY Contrast.K8s.AgentOperator.sln /source/

RUN dotnet restore

# Build
COPY . /source/
ARG BUILD_VERSION=0.0.1 \
    IS_PUBLIC_BUILD=False

RUN set -xe \
    && dotnet test -c Release -p:Version=${BUILD_VERSION} -p:IsPublicBuild=${IS_PUBLIC_BUILD} --filter Type=Unit \
    && dotnet publish -c Release -o /app -p:Version=${BUILD_VERSION} -p:IsPublicBuild=${IS_PUBLIC_BUILD}

FROM base AS final
WORKDIR /app

RUN set -xe \
    && addgroup --gid 1000 operator-group \
    && useradd -G operator-group --uid 1000 operator-user

COPY src/get-info.sh /get-info.sh
COPY --from=build /app .

RUN set -xe \
    && chown operator-user:operator-group -R . \
    && chmod +x /get-info.sh

USER 1000

ENV ASPNETCORE_URLS=https://+:5001 \
    ASPNETCORE_ENVIRONMENT=Production \
    ASPNETCORE_HOSTBUILDER__RELOADCONFIGONCHANGE=false \
    DOTNET_EnableDiagnostics=0

ENTRYPOINT ["dotnet", "Contrast.K8s.AgentOperator.dll"]
