@echo off
setlocal EnableExtensions
for %%I in ("%~dp0..\..\..") do set "WW_OWNED_ROOT=%%~fI"
set "WW_OWNED_OUTPUT=%WW_OWNED_ROOT%\build\owned_mmd"
if not "%~1"=="" for %%I in ("%~1") do set "WW_OWNED_OUTPUT=%%~fI"
set "WW_OWNED_INTERMEDIATE=%WW_OWNED_ROOT%\build\owned_mmd_compile\native"
pushd "%~dp0"
call "%WW_OWNED_ROOT%\tools\msvc_env.bat"
if errorlevel 1 goto failed
if not exist "%WW_OWNED_OUTPUT%" mkdir "%WW_OWNED_OUTPUT%"
if errorlevel 1 goto failed
if not exist "%WW_OWNED_INTERMEDIATE%" mkdir "%WW_OWNED_INTERMEDIATE%"
if errorlevel 1 goto failed
cl /nologo /std:c++17 /O2 /MD /EHsc /utf-8 /W4 /WX /wd4505 /D_CRT_SECURE_NO_WARNINGS /LD n1_observer.cpp /Fo:"%WW_OWNED_INTERMEDIATE%\n1_observer.obj" /Fd:"%WW_OWNED_INTERMEDIATE%\observer.pdb" /Fe:"%WW_OWNED_OUTPUT%\ownedmmd_lite_observer.dll" /link /IMPLIB:"%WW_OWNED_INTERMEDIATE%\ownedmmd_lite_observer.lib" /PDB:"%WW_OWNED_INTERMEDIATE%\ownedmmd_lite_observer.pdb" bcrypt.lib psapi.lib user32.lib
if errorlevel 1 goto failed
cl /nologo /std:c++17 /O2 /MD /EHsc /utf-8 /W4 /WX /wd4505 /D_CRT_SECURE_NO_WARNINGS n1_installer.cpp /Fo:"%WW_OWNED_INTERMEDIATE%\n1_installer.obj" /Fd:"%WW_OWNED_INTERMEDIATE%\installer.pdb" /Fe:"%WW_OWNED_OUTPUT%\ownedmmd_lite_installer.exe" /link /PDB:"%WW_OWNED_INTERMEDIATE%\ownedmmd_lite_installer.pdb" bcrypt.lib psapi.lib user32.lib advapi32.lib
if errorlevel 1 goto failed
popd
exit /b 0
:failed
popd
exit /b 1
