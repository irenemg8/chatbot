@echo off
echo ================================================
echo   PREPARACION DE INSTALACION - CHATBOT GOMARCO
echo ================================================
echo.
echo Este script prepara los archivos necesarios para la instalacion
echo del Chatbot GOMARCO.
echo.

:: Verificar que estamos en el directorio correcto
if not exist "ChatbotGomarco.csproj" (
    echo ERROR: Este script debe ejecutarse desde la carpeta raiz del proyecto
    echo donde se encuentra el archivo ChatbotGomarco.csproj
    echo.
    echo Directorio actual: %CD%
    echo.
    pause
    exit /b 1
)

:: Verificar que .NET 8 SDK este instalado
echo Verificando .NET 8 SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ERROR: .NET 8 SDK no encontrado.
    echo.
    echo Descargue e instale .NET 8 SDK desde:
    echo https://dotnet.microsoft.com/download/dotnet/8.0
    echo.
    pause
    exit /b 1
)

for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
echo .NET SDK encontrado: %DOTNET_VERSION%

:: Limpiar compilaciones anteriores
echo Limpiando compilaciones anteriores...
if exist "Instalacion\App" rmdir /S /Q "Instalacion\App"
dotnet clean >nul 2>&1

:: Restaurar paquetes NuGet
echo Restaurando paquetes NuGet...
dotnet restore
if errorlevel 1 (
    echo ERROR: No se pudieron restaurar los paquetes NuGet
    pause
    exit /b 1
)

:: Compilar la aplicacion en modo Release
echo Compilando aplicacion en modo Release...
dotnet publish ChatbotGomarco.csproj -c Release -r win-x64 --self-contained false -o "Instalacion\App"
if errorlevel 1 (
    echo ERROR: No se pudo compilar la aplicacion
    pause
    exit /b 1
)

:: Verificar que los archivos principales existan
echo Verificando archivos compilados...
if not exist "Instalacion\App\ChatbotGomarco.exe" (
    echo ERROR: No se genero el archivo ejecutable principal
    pause
    exit /b 1
)

if not exist "Instalacion\App\ChatbotGomarco.dll" (
    echo ERROR: No se genero la libreria principal
    pause
    exit /b 1
)

:: Crear directorio de recursos si no existe
if not exist "Instalacion\App\Resources" mkdir "Instalacion\App\Resources"

:: Copiar recursos adicionales si existen
if exist "Resources\gomarco-icon.ico" copy "Resources\gomarco-icon.ico" "Instalacion\App\Resources\" >nul

:: Crear archivo de version
echo 1.0.0 > "Instalacion\App\version.txt"
echo %DATE% %TIME% >> "Instalacion\App\version.txt"

:: Mostrar resumen
echo.
echo ================================================
echo    PREPARACION COMPLETADA EXITOSAMENTE
echo ================================================
echo.
echo Archivos preparados en: %CD%\Instalacion\App\
echo.
echo Archivos principales:
dir "Instalacion\App\ChatbotGomarco.*" /B
echo.
echo Total de archivos: 
for /f %%i in ('dir "Instalacion\App" /S /A-D ^| find "File(s)"') do echo %%i

echo.
echo SIGUIENTE PASO:
echo Ejecutar como administrador: Instalacion\InstalarChatbotGomarco.bat
echo.
pause 