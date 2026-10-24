@echo off
rem One compiler setup contract for the repository. Override explicitly if VS is elsewhere.
if not defined WW_VCVARS64 set "WW_VCVARS64=C:\Program Files (x86)\Microsoft Visual Studio\2022\BuildTools\VC\Auxiliary\Build\vcvars64.bat"
if not exist "%WW_VCVARS64%" (
  echo MSVC setup missing. Set WW_VCVARS64 to your vcvars64.bat path. 1>&2
  exit /b 1
)
call "%WW_VCVARS64%" >nul
exit /b %errorlevel%
