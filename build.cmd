@ECHO OFF

setlocal

set CONFIG=Release
set SOURCEPATH=%~dp0\Source
set BUILDPATH=%~dp0\Build
set PACKAGES=%~dp0\packages

REM get buildversion from build environment, if available.
if not "%APPVEYOR_BUILD_VERSION%" == "" set BUILD_VERSION=%APPVEYOR_BUILD_VERSION%
if not "%1" == "" set BUILD_VERSION=%1
if "%BUILD_VERSION%" == "" echo ERROR: BUILD_VERSION not set && goto :END

if not exist "%BUILDPATH%\." goto :END

set ILMERGE_EXE=%PACKAGES%\ILMerge.3.0.29\tools\net452\ILMerge.exe
set NUGET_EXE=%PACKAGES%\Nuget.CommandLine.5.6.0\tools\Nuget.exe

:CLEAN
ECHO.
ECHO Cleaning Build directory
pushd %BUILDPATH%
for /d %%d in (*) do rmdir /s /q %%d

echo %BUILD_VERSION% >%BUILDPATH%\LastBuildVersion

:BUILD
msbuild %SOURCEPATH%\codesync-cli\codesync-cli.csproj /p:Configuration=Release /p:OutputPath=%BUILDPATH%\Release
if errorlevel 1 goto :ERROR

:MERGE
ECHO.
ECHO Performing merge:
mkdir %BUILDPATH%\NugetPack 2>NUL
if errorlevel 1 goto :ERROR

cd %BUILDPATH%\Release
if errorlevel 1 goto :ERROR

echo.
ECHO ILMerge /out:%BUILDPATH%\Nugetpack\codesync.exe codesync.exe CommandLine.dll codesync-core.dll
%ILMERGE_EXE% /out:%BUILDPATH%\Nugetpack\codesync.exe codesync.exe CommandLine.dll codesync-core.dll
if errorlevel 1 goto :ERROR
ECHO ILMerge successful

:NUGET_PACKAGE
ECHO.
ECHO Building Nuget Package:
pushd %BUILDPATH%\NugetPack
copy %~dp0\CPCodeSync.CommandLine.nuspec .
if errorlevel 1 goto :ERROR

%NUGET_EXE% pack CPCodeSync.CommandLine.nuspec -Version %BUILD_VERSION%
if errorlevel 1 goto :ERROR
popd

:NUGET_UPLOAD
ECHO.
ECHO Uploading Nuget package:
pushd %BUILDPATH%\NugetPack
REM %NUGET_EXE% push CPCodeSync.CommandLine.%BUILD_VERSION%.nupkg -verbosity detailed
if errorlevel 1 goto :ERROR
popd

goto :END

:ERROR
popd
echo ERROR occurred.

:END
ECHO.
tree %BUILDPATH% /f /a
