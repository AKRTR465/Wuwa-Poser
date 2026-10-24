@echo off
setlocal
cd /d "%~dp0"
call "%~dp0msvc_env.bat"
if errorlevel 1 exit /b 1
if not exist mmd_outputs mkdir mmd_outputs
cl /nologo /std:c++17 /O2 /W4 /MD /EHsc /utf-8 /I..\core_math\third_party mmd_vmd_cli.cpp /Fe:mmd_outputs\mmd_vmd_cli.exe /Fo:mmd_outputs\mmd_vmd_cli.obj
exit /b %errorlevel%
