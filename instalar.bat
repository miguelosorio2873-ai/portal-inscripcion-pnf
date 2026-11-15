@echo off
chcp 65001 >nul
setlocal
cd /d "%~dp0"
title Instalador - Portal PNF Impresora Visual

echo ============================================
echo   INSTALACION DEL PORTAL EN ESTE PC
echo ============================================
echo.

where node >nul 2>&1
if errorlevel 1 (
  echo [ERROR] Node.js no esta instalado.
  echo Descargalo de https://nodejs.org/es/download  ^(version 22 o superior^)
  echo Instalalo y vuelve a ejecutar este archivo.
  start "" "https://nodejs.org/es/download"
  pause
  exit /b 1
)

for /f "delims=" %%v in ('node -v') do set NODEVER=%%v
echo Node.js detectado: %NODEVER%
echo.

powershell -NoProfile -Command "try{$c=New-Object Net.Sockets.TcpClient;$c.Connect('127.0.0.1',3306);$c.Close();exit 0}catch{exit 1}"
if errorlevel 1 (
  echo [AVISO] MySQL no responde en 127.0.0.1:3306.
  echo Abre XAMPP Control Panel y pulsa Start en MySQL.
  echo.
  pause
)

echo Instalando dependencias del portal...
call npm install
if errorlevel 1 (
  echo [ERROR] Fallo npm install. Revisa tu conexion a Internet.
  pause
  exit /b 1
)

echo.
echo Creando las cuentas de prueba...
call npm run demo
echo.
echo Iniciando el portal...
start "Portal PNF - NO CIERRES ESTA VENTANA" cmd /k "npm start"
timeout /t 5 /nobreak >nul
start "" "http://localhost:3000"
echo.
echo Listo. Si el navegador no abre, visita http://localhost:3000
echo Cuenta de prueba: demo@pnf.edu.ve  /  Demo1234
echo.
pause
endlocal
