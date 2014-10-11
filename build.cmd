@ECHO OFF

setlocal

set CONFIG=Release
set SOURCEPATH=%~dp0\Source
set BUILDPATH=%~dp0\Build
set PACKAGES=%~dp0\packages

REM get buildversion from build environment, if available.
if not "%APPVEYOR_BUILD_VERSION%" == "" set BUILD_VERSION=%APPVEYOR_BUILD_VERSION%
if "%BUILD_VERSION%" == "" set BUILD_VERSION=1.0.0

if not exist "%BUILDPATH%\." goto :END

:CLEAN
ECHO.
ECHO Cleaning Build directory
pushd %BUILDPATH%
for /d %%d in (*) do rmdir /s /q %%d

:BUILD
msbuild %SOURCEPATH%\CPCodeSyncronize\CPCodeSyncronize.csproj /p:Configuration=Release;OutputPath=%BUILDPATH%\bin\%config%

:MERGE
ECHO.
ECHO Performing merge:
mkdir %BUILDPATH%\NugetPack 2>NUL

pushd %BUILDPATH%\bin\%config%
ECHO ILMerge /out:%BUILDPATH%\Nugetpack\cpcodesync.exe cpcodesync.exe CommandLine.dll
%PACKAGES%\ILMerge.2.13.0307\ilmerge.exe /out:%BUILDPATH%\Nugetpack\cpcodesync.exe cpcodesync.exe CommandLine.dll
ECHO Done.
popd

:NUGET_PACKAGE
ECHO.
ECHO Building Nuget Package:
pushd %BUILDPATH%\NugetPack
copy %~dp0\CPCodeSync.CommandLine.nuspec .
%PACKAGES%\Nuget.CommandLine.2.8.2\tools\Nuget.exe pack CPCodeSync.CommandLine.nuspec
popd

:NUGET_UPLOAD
ECHO.
ECHO Uploading Nuget package:
pushd %BUILDPATH%\NugetPack
%PACKAGES%\Nuget.CommandLine.2.8.2.\tools\Nuget.exe push CPCodeSync.CommandLine.nupkg -Version %BUILD_VERSION% -Source http://nuget.lightmaker.com/ 
popd

ECHO.
tree %BUILDPATH% /F
