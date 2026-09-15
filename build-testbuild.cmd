@echo off
powershell.exe -NoProfile -ExecutionPolicy Bypass -File "%~dp0build-testbuild.ps1" %*
