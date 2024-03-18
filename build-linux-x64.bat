@echo off
call cd src
call dotnet publish -c Release -r linux-x64
pause