@echo off

REM This batch file is used to generate the final "Unity Editor Integration Package" for the "UnityScript2CSharp" converter
REM
REM Requirements:
REM    	- 7z.exe must be in the search path
REM 	- MSBuild must be in the search path
REM 	- Unity.exe must be in the searh path 
   
set DEPENDENCIES_NOT_FOUND=0
call :CheckExecutable 7z.exe DEPENDENCIES_NOT_FOUND
call :CheckExecutable MSBuild.exe DEPENDENCIES_NOT_FOUND
call :CheckExecutable Unity.exe DEPENDENCIES_NOT_FOUND

if [%DEPENDENCIES_NOT_FOUND%] == [1] goto :eof

REM Create Project Folder
set UNITYSCRIPT2CSHARP_PROJECT_ROOT=%temp%\UnityScript2CSharp
if exist %UNITYSCRIPT2CSHARP_PROJECT_ROOT% rmdir %UNITYSCRIPT2CSHARP_PROJECT_ROOT% /S /Q
mkdir %UNITYSCRIPT2CSHARP_PROJECT_ROOT%

REM Build app
msbuild /p:Configuration=Release

if %ERRORLEVEL% EQU 0 goto BuildSucceeded
@echo Error while building converter. Error code = %errorlevel%
goto End

:BuildSucceeded

REM Get current version
UnityScript2CSharp\bin\Release\UnityScript2CSharp.exe > %temp%\UnityScript2CSharp.version 2>&1
set /p CONVERTER_OUTPUT=<%temp%\UnityScript2CSharp.version

REM Extracts version # from output like: UnityScript2CSharp 1.0.6577.20371
set CONVERTER_VERSION=%CONVERTER_OUTPUT:~19,15%

REM Create folders
set UNITYSCRIPT2CSHARP_IN_ASSETS_FOLDER=%UNITYSCRIPT2CSHARP_PROJECT_ROOT%\Assets\UnityScript2CSharp
mkdir %UNITYSCRIPT2CSHARP_IN_ASSETS_FOLDER%

REM Zip output
set OUTPUT_FILE=%UNITYSCRIPT2CSHARP_IN_ASSETS_FOLDER%\UnityScript2CSharp_%CONVERTER_VERSION%.zip
pushd UnityScript2CSharp\bin\Release\
7z.exe a -tzip %OUTPUT_FILE% *
popd


REM Copy editor integration sources..
mkdir %UNITYSCRIPT2CSHARP_IN_ASSETS_FOLDER%\Editor
xcopy EditorIntegration\Assets\UnityScript2CSharp\Editor %UNITYSCRIPT2CSHARP_IN_ASSETS_FOLDER%\Editor\

REM Create package

set EXPORTED_PACKAGE_PATH=%temp%\UnityScript2CSharp_Conversion_%CONVERTER_VERSION%.unitypackage
@echo Exporting unity package (%UNITYSCRIPT2CSHARP_IN_ASSETS_FOLDER%) to %EXPORTED_PACKAGE_PATH%
unity.exe --createProject -batchmode -projectPath %UNITYSCRIPT2CSHARP_PROJECT_ROOT% -exportPackage Assets %EXPORTED_PACKAGE_PATH% -quit

if errorlevel 0 goto PackagingSucceeded

@echo Error while exporting Unity package. Error code = %errorlevel%
goto End

:PackagingSucceeded
@echo Unity package exported successfully

:End
goto :eof

:CheckExecutable
   where %1 > NUL 2>&1

   if errorlevel 1 @echo %1 not found. Please, add it to search path.
   if [%ERRORLEVEL%]==[1] Set %~2=%ERRORLEVEL%
  
   goto :eof

