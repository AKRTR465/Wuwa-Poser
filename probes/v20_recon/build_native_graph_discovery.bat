@echo off
rem Build the current native graph discovery DLL.
setlocal EnableExtensions
pushd "%~dp0"
call "%~dp0..\..\tools\msvc_env.bat"
if errorlevel 1 goto failed
if not exist build\native_graph_discovery mkdir build\native_graph_discovery
cl /nologo /std:c++17 /O2 /MD /EHsc /utf-8 /W4 /WX /wd4505 /D_CRT_SECURE_NO_WARNINGS /LD ^
 native_graph_discovery.cpp /Fo:build\native_graph_discovery\native_graph_discovery.obj ^
 /Fe:native_graph_discovery.dll /link bcrypt.lib psapi.lib user32.lib
if errorlevel 1 goto failed
popd
exit /b 0
:failed
popd
exit /b 1
