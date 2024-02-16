@echo off

for /f "tokens=1-2 delims=-" %%i in (.\version.txt) do (set VersionPrefix=%%i & set VersionSuffix=%%j)
if ERRORLEVEL 1 goto ERROREXIT

rd /s /q .\build-files

dotnet build ..\BlueFire.Toolkit.WinUI3.Core\BlueFire.Toolkit.WinUI3.Core.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true -p:Platform=anycpu -o .\build-files\AnyCPU
if ERRORLEVEL 1 goto ERROREXIT
dotnet build ..\BlueFire.Toolkit.WinUI3\BlueFire.Toolkit.WinUI3.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true -p:Platform=anycpu -o .\build-files\AnyCPU
if ERRORLEVEL 1 goto ERROREXIT
dotnet build ..\BlueFire.Toolkit.WinUI3.TextView\BlueFire.Toolkit.WinUI3.TextView.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true -p:Platform=anycpu -o .\build-files\AnyCPU
if ERRORLEVEL 1 goto ERROREXIT

dotnet build ..\BlueFire.Toolkit.WinUI3.Core\BlueFire.Toolkit.WinUI3.Core.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true -p:Platform=x86 -o .\build-files\x86
if ERRORLEVEL 1 goto ERROREXIT
dotnet build ..\BlueFire.Toolkit.WinUI3\BlueFire.Toolkit.WinUI3.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true -p:Platform=x86 -o .\build-files\x86
if ERRORLEVEL 1 goto ERROREXIT
dotnet build ..\BlueFire.Toolkit.WinUI3.TextView\BlueFire.Toolkit.WinUI3.TextView.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true -p:Platform=x86 -o .\build-files\x86
if ERRORLEVEL 1 goto ERROREXIT

dotnet build ..\BlueFire.Toolkit.WinUI3.Core\BlueFire.Toolkit.WinUI3.Core.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true -p:Platform=x64 -o .\build-files\x64
if ERRORLEVEL 1 goto ERROREXIT
dotnet build ..\BlueFire.Toolkit.WinUI3\BlueFire.Toolkit.WinUI3.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true -p:Platform=x64 -o .\build-files\x64
if ERRORLEVEL 1 goto ERROREXIT
dotnet build ..\BlueFire.Toolkit.WinUI3.TextView\BlueFire.Toolkit.WinUI3.TextView.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true -p:Platform=x64 -o .\build-files\x64
if ERRORLEVEL 1 goto ERROREXIT

dotnet build ..\BlueFire.Toolkit.WinUI3.Core\BlueFire.Toolkit.WinUI3.Core.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true -p:Platform=arm64 -o .\build-files\arm64
if ERRORLEVEL 1 goto ERROREXIT
dotnet build ..\BlueFire.Toolkit.WinUI3\BlueFire.Toolkit.WinUI3.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true -p:Platform=arm64 -o .\build-files\arm64
if ERRORLEVEL 1 goto ERROREXIT
dotnet build ..\BlueFire.Toolkit.WinUI3.TextView\BlueFire.Toolkit.WinUI3.TextView.csproj -c %PACK_CONFIGURATION% -p:EnableMsixTooling=true -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true -p:Platform=arm64 -o .\build-files\arm64
if ERRORLEVEL 1 goto ERROREXIT

dotnet pack ..\BlueFire.Toolkit.WinUI3.Core\BlueFire.Toolkit.WinUI3.Core.csproj --no-build -c %PACK_CONFIGURATION% -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true
if ERRORLEVEL 1 goto ERROREXIT
dotnet pack ..\BlueFire.Toolkit.WinUI3\BlueFire.Toolkit.WinUI3.csproj --no-build -c %PACK_CONFIGURATION% -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true
if ERRORLEVEL 1 goto ERROREXIT
dotnet pack ..\BlueFire.Toolkit.WinUI3.TextView\BlueFire.Toolkit.WinUI3.TextView.csproj --no-build -c %PACK_CONFIGURATION% -p:VersionPrefix=%VersionPrefix% -p:VersionSuffix=%VersionSuffix% -p:NugetPacking=true
if ERRORLEVEL 1 goto ERROREXIT

goto FINISH

:ERROREXIT
echo pack failed

:FINISH
