#!/bin/bash

dotnet publish --configuration Release  /p:PublishSingleFile=true /p:IncludeNativeLibrariesForSelfExtract=true --self-contained true -r debian-x64 TSV2SMW

