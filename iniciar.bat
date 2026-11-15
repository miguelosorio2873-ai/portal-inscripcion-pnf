@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"
title Portal PNF - Impresora Visual

powershell -NoProfile -Command "try{$c=New-Object Net.Sockets.TcpClient;$c.Connect('127.0.0.1',3306);$c.Close();exit 0}catch{exit 1}"
if errorlevel 1 (
  echo [AVISO] MySQL no esta iniciado.
  echo Abre XAMPP Control Panel y pulsa Start en MySQL.
  echo.
  pause
  exit /b 1
)

start "Portal PNF - NO CIERRES ESTA VENTANA" cmd /k "npm start"
timeout /t 5 /nobreak >nul
start "" "http://localhost:3000"
endlocal
