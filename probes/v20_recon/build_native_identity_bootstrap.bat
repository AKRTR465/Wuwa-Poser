@echo off
rem Build the current native identity bootstrap DLL.
setlocal EnableExtensions
pushd "%~dp0"
call "%~dp0..\..\tools\msvc_env.bat"
if not "%ERRORLEVEL%"=="0" goto failed
if not exist build\native_identity_bootstrap mkdir build\native_identity_bootstrap
cl /nologo /std:c++17 /O2 /MD /EHsc /utf-8 /W4 /WX /wd4505 /D_CRT_SECURE_NO_WARNINGS /LD ^
 native_identity_bootstrap.cpp /Fo:build\native_identity_bootstrap\native_identity_bootstrap.obj ^
 /Fe:native_identity_bootstrap.dll /link bcrypt.lib psapi.lib user32.lib
if not "%ERRORLEVEL%"=="0" goto failed
popd
exit /b 0
:failed
popd
exit /b 1
