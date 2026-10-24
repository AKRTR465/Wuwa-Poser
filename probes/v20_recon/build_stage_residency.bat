@echo off
setlocal EnableExtensions
pushd "%~dp0"
call "%~dp0..\..\tools\msvc_env.bat"
if errorlevel 1 goto failed
if not exist build\stage_residency mkdir build\stage_residency
cl /nologo /std:c++17 /O2 /MD /EHsc /utf-8 /W4 /WX /wd4505 /D_CRT_SECURE_NO_WARNINGS /LD stage_residency.cpp /Fo:build\stage_residency\stage_residency.obj /Fe:stage_residency.dll /link bcrypt.lib psapi.lib user32.lib
if errorlevel 1 goto failed
popd
exit /b 0
:failed
popd
exit /b 1
