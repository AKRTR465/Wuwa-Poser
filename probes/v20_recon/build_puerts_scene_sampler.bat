@echo off
setlocal EnableExtensions
pushd "%~dp0"
call "%~dp0..\..\tools\msvc_env.bat"
if errorlevel 1 goto failed
if not exist build\puerts_scene_sampler mkdir build\puerts_scene_sampler
cl /nologo /std:c++17 /O2 /MD /EHsc /utf-8 /W4 /WX /wd4505 /D_CRT_SECURE_NO_WARNINGS /LD puerts_scene_sampler.cpp /Fo:build\puerts_scene_sampler\puerts_scene_sampler.obj /Fe:puerts_scene_sampler.dll /link bcrypt.lib psapi.lib user32.lib
if errorlevel 1 goto failed
popd
exit /b 0
:failed
popd
exit /b 1
