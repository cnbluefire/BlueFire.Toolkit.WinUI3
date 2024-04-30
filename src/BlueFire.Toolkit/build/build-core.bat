@echo off

for /f "tokens=1-2 delims=-" %%i in (.\version.txt) do (set VersionPrefix=%%i & set VersionSuffix=%%j)
if ERRORLEVEL 1 goto ERROREXIT

set VersionPrefix=%VersionPrefix: =%
set VersionSuffix=%VersionSuffix: =%

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

echo ^<Project^> > LocalPackageVersion.props
echo     ^<PropertyGroup^> >> LocalPackageVersion.props
echo         ^<LocalBuildPackageReleaseVersion^>%VersionPrefix%-%VersionSuffix%^</LocalBuildPackageReleaseVersion^> >> LocalPackageVersion.props
echo         ^<LocalBuildPackageDebugVersion^>%VersionPrefix%.1-%VersionSuffix%-debug^</LocalBuildPackageDebugVersion^> >> LocalPackageVersion.props
if %PACK_CONFIGURATION% == Release (
echo         ^<LocalBuildPackageVersion^>$^(LocalBuildPackageReleaseVersion^)^</LocalBuildPackageVersion^> >> LocalPackageVersion.props
) else (
echo         ^<LocalBuildPackageVersion^>$^(LocalBuildPackageDebugVersion^)^</LocalBuildPackageVersion^> >> LocalPackageVersion.props
)
echo     ^</PropertyGroup^> >> LocalPackageVersion.props
echo ^</Project^> >> LocalPackageVersion.props

goto FINISH

:ERROREXIT
echo pack failed

:FINISH
