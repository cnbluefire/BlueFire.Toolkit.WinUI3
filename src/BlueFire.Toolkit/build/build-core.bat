@echo off

rd /s /q .\build-files

dotnet build ..\BlueFire.Toolkit.WinUI3\BlueFire.Toolkit.WinUI3.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:Platform=anycpu -o .\build-files\AnyCPU
if ERRORLEVEL 1 goto ERROREXIT

dotnet build ..\BlueFire.Toolkit.WinUI3\BlueFire.Toolkit.WinUI3.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:Platform=x86 -o .\build-files\x86
if ERRORLEVEL 1 goto ERROREXIT

dotnet build ..\BlueFire.Toolkit.WinUI3\BlueFire.Toolkit.WinUI3.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:Platform=x64 -o .\build-files\x64
if ERRORLEVEL 1 goto ERROREXIT

dotnet build ..\BlueFire.Toolkit.WinUI3\BlueFire.Toolkit.WinUI3.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:Platform=arm64 -o .\build-files\arm64
if ERRORLEVEL 1 goto ERROREXIT

dotnet pack ..\BlueFire.Toolkit.WinUI3\BlueFire.Toolkit.WinUI3.csproj --no-build -c %PACK_CONFIGURATION%
if ERRORLEVEL 1 goto ERROREXIT

goto FINISH

:ERROREXIT
echo pack failed

:FINISH
