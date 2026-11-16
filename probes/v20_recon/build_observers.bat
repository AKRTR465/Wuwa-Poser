@echo off
rem Builds the observer installer and both observer carriers into this folder.
setlocal EnableExtensions
pushd "%~dp0"
call "%~dp0..\..\tools\msvc_env.bat"
if errorlevel 1 goto failed
if not exist build\observers mkdir build\observers
set "WW_FLAGS=/nologo /std:c++17 /O2 /MD /EHsc /utf-8 /W4 /WX /D_CRT_SECURE_NO_WARNINGS"
cl %WW_FLAGS% hook_install_v20.cpp /Fo:build\observers\hook_install_v20.obj /Fe:hook_install_v20.exe /link user32.lib
if errorlevel 1 goto failed
for %%N in (native_identity_bootstrap native_graph_discovery) do (
  cl %WW_FLAGS% /LD %%N.cpp /Fo:build\observers\%%N.obj /Fe:%%N.dll /link /IMPLIB:build\observers\%%N.lib bcrypt.lib psapi.lib user32.lib
  if errorlevel 1 goto failed
)
popd
exit /b 0
:failed
popd
exit /b 1
