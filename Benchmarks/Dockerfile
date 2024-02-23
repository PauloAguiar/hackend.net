#See https://aka.ms/customizecontainer to learn how to customize your debug container and how Visual Studio uses this Dockerfile to build your images for faster debugging.

FROM mcr.microsoft.com/dotnet/sdk:8.0
WORKDIR /src
COPY ./Benchmarks.csproj ./Benchmarks.csproj
RUN dotnet restore
COPY ./Program.cs ./Program.cs
RUN dotnet build -c Release -o /src/bin
RUN dotnet publish -c Release -o /src/bin/publish
WORKDIR /src/bin/publish
ENTRYPOINT ["dotnet", "Benchmarks.dll"]