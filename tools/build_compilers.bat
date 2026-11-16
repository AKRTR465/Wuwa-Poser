@echo off
rem Builds the three offline VMD compilers into tools\mmd_outputs.
setlocal EnableExtensions
pushd "%~dp0"
call "%~dp0msvc_env.bat"
if errorlevel 1 goto failed
if not exist mmd_outputs mkdir mmd_outputs
for %%N in (mmd_vmd_cli mmd_face_compile mmd_camera_compile) do (
  cl /nologo /std:c++17 /O2 /W4 /WX /MD /EHsc /utf-8 %%N.cpp /Fe:mmd_outputs\%%N.exe /Fo:mmd_outputs\%%N.obj
  if errorlevel 1 goto failed
)
popd
exit /b 0
:failed
popd
exit /b 1
