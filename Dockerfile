FROM mcr.microsoft.com/dotnet/core/sdk:latest AS build-env
WORKDIR /app

# copy csproj and restore as distinct layers
COPY lmdb_dupbug/*.csproj ./lmdb_dupbug/
WORKDIR /app/lmdb_dupbug
RUN dotnet restore

# copy and build app and libraries
WORKDIR /app/
COPY lmdb_dupbug/. ./lmdb_dupbug/
WORKDIR /app/lmdb_dupbug
RUN dotnet publish -c Release -r linux-x64 -o out --self-contained true /p:PublishTrimmed=true

# test application -- see: dotnet-docker-unit-testing.md
#FROM build-env AS testrunner
#WORKDIR /app/lmdb_dupbug
#RUN apt-get update
#RUN apt-get install -qq liblmdb-dev
#RUN dotnet test --logger:"console;verbosity=normal"



FROM mcr.microsoft.com/dotnet/core/runtime-deps:latest AS runtime
WORKDIR /app

RUN apt-get update
RUN apt-get install -qq liblmdb-dev

COPY --from=build-env /app/lmdb_dupbug/out ./
ENTRYPOINT ["./lmdb_dupbug"]
