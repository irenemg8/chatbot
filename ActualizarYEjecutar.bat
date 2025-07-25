@echo off
:: ====================================================================
:: 🚀 SCRIPT DE ACTUALIZACIÓN AUTOMÁTICA - CHATBOT GOMARCO
:: Versión: 2.0 Enterprise
:: Desarrollado por: DevOps Team GOMARCO
:: ====================================================================

setlocal EnableDelayedExpansion
color 0A

echo.
echo ╔══════════════════════════════════════════════════════════════════╗
echo ║              🤖 CHATBOT GOMARCO - AUTO UPDATER 🤖              ║
echo ║                     Enterprise DevOps Solution                    ║
echo ╚══════════════════════════════════════════════════════════════════╝
echo.

:: Variables de configuración empresarial
set PROJECT_NAME=ChatbotGomarco
set BUILD_CONFIG=Debug
set SOLUTION_FILE=chatbot.sln
set PROJECT_FILE=ChatbotGomarco.csproj
set LOG_FILE=deployment_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%.log

:: Crear directorio de logs si no existe
if not exist "logs" mkdir logs

echo [%time%] === INICIANDO PROCESO DE ACTUALIZACIÓN === >> logs\%LOG_FILE%

:: ====================================================================
:: FASE 1: VALIDACIÓN DEL ENTORNO
:: ====================================================================
echo 🔍 FASE 1: Verificando entorno de desarrollo...

:: Verificar .NET SDK
echo    └─ Verificando .NET 8 SDK...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo ❌ ERROR: .NET 8 SDK no está instalado
    echo [%time%] ERROR: .NET SDK no encontrado >> logs\%LOG_FILE%
    pause
    exit /b 1
)
echo    ✅ .NET SDK verificado

:: Verificar archivos del proyecto
echo    └─ Verificando archivos del proyecto...
if not exist "%SOLUTION_FILE%" (
    echo ❌ ERROR: Archivo de solución %SOLUTION_FILE% no encontrado
    echo [%time%] ERROR: Solución no encontrada >> logs\%LOG_FILE%
    pause
    exit /b 1
)
if not exist "%PROJECT_FILE%" (
    echo ❌ ERROR: Archivo de proyecto %PROJECT_FILE% no encontrado
    echo [%time%] ERROR: Proyecto no encontrado >> logs\%LOG_FILE%
    pause
    exit /b 1
)
echo    ✅ Archivos del proyecto verificados

:: ====================================================================
:: FASE 2: ACTUALIZACIÓN DE CÓDIGO FUENTE
:: ====================================================================
echo.
echo 🔄 FASE 2: Sincronizando con repositorio...

:: Verificar si existe Git
git --version >nul 2>&1
if not errorlevel 1 (
    echo    └─ Actualizando desde repositorio Git...
    git status >> logs\%LOG_FILE% 2>&1
    git pull origin master >> logs\%LOG_FILE% 2>&1
    if errorlevel 1 (
        echo ⚠️  WARNING: No se pudo actualizar desde Git (continuando...)
        echo [%time%] WARNING: Git pull falló >> logs\%LOG_FILE%
    ) else (
        echo    ✅ Código fuente actualizado desde Git
        echo [%time%] INFO: Git pull exitoso >> logs\%LOG_FILE%
    )
) else (
    echo    ⚠️  Git no disponible, usando código local
    echo [%time%] INFO: Git no disponible >> logs\%LOG_FILE%
)

:: ====================================================================
:: FASE 3: LIMPIEZA DE ARTEFACTOS PREVIOS
:: ====================================================================
echo.
echo 🧹 FASE 3: Limpiando artefactos de compilación...

echo    └─ Limpiando directorios bin y obj...
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
echo    ✅ Artefactos anteriores eliminados

:: ====================================================================
:: FASE 4: RESTAURACIÓN DE DEPENDENCIAS
:: ====================================================================
echo.
echo 📦 FASE 4: Restaurando dependencias NuGet...

echo    └─ Ejecutando dotnet restore...
dotnet restore "%SOLUTION_FILE%" >> logs\%LOG_FILE% 2>&1
if errorlevel 1 (
    echo ❌ ERROR: Falló la restauración de dependencias
    echo [%time%] ERROR: dotnet restore falló >> logs\%LOG_FILE%
    pause
    exit /b 1
)
echo    ✅ Dependencias restauradas exitosamente

:: ====================================================================
:: FASE 5: COMPILACIÓN OPTIMIZADA
:: ====================================================================
echo.
echo 🔨 FASE 5: Compilando aplicación...

echo    └─ Ejecutando dotnet build en modo %BUILD_CONFIG%...
dotnet build "%SOLUTION_FILE%" -c %BUILD_CONFIG% --no-restore >> logs\%LOG_FILE% 2>&1
if errorlevel 1 (
    echo ❌ ERROR: Falló la compilación
    echo [%time%] ERROR: dotnet build falló >> logs\%LOG_FILE%
    echo.
    echo 📋 Revisa los errores de compilación en: logs\%LOG_FILE%
    pause
    exit /b 1
)
echo    ✅ Compilación exitosa

:: ====================================================================
:: FASE 6: DETENER PROCESOS EXISTENTES
:: ====================================================================
echo.
echo ⏹️  FASE 6: Verificando procesos existentes...

tasklist | findstr "%PROJECT_NAME%.exe" >nul 2>&1
if not errorlevel 1 (
    echo    └─ Deteniendo instancias previas...
    taskkill /IM "%PROJECT_NAME%.exe" /F >nul 2>&1
    timeout /t 2 /nobreak >nul
    echo    ✅ Procesos anteriores detenidos
) else (
    echo    ✅ No hay procesos previos ejecutándose
)

:: ====================================================================
:: FASE 7: EJECUCIÓN DE LA APLICACIÓN
:: ====================================================================
echo.
echo 🚀 FASE 7: Iniciando aplicación actualizada...

echo [%time%] INFO: Iniciando aplicación >> logs\%LOG_FILE%

:: Intentar diferentes métodos de ejecución
echo    └─ Método 1: Ejecutando desde proyecto...
start "" dotnet run --project "%PROJECT_FILE%" --no-build >> logs\%LOG_FILE% 2>&1

:: Esperar un momento para verificar si se inició correctamente
timeout /t 3 /nobreak >nul

:: Verificar si el proceso se está ejecutando
tasklist | findstr "dotnet.exe" >nul 2>&1
if not errorlevel 1 (
    echo    ✅ Aplicación iniciada correctamente
    echo [%time%] SUCCESS: Aplicación ejecutándose >> logs\%LOG_FILE%
) else (
    echo    └─ Método 2: Ejecutando binario directo...
    if exist "bin\%BUILD_CONFIG%\net8.0-windows\%PROJECT_NAME%.exe" (
        start "" "bin\%BUILD_CONFIG%\net8.0-windows\%PROJECT_NAME%.exe"
        echo    ✅ Aplicación iniciada desde binario
        echo [%time%] SUCCESS: Binario ejecutado >> logs\%LOG_FILE%
    ) else (
        echo ❌ ERROR: No se pudo iniciar la aplicación
        echo [%time%] ERROR: No se pudo ejecutar >> logs\%LOG_FILE%
    )
)

:: ====================================================================
:: INFORMACIÓN FINAL
:: ====================================================================
echo.
echo ╔══════════════════════════════════════════════════════════════════╗
echo ║                     🎉 ACTUALIZACIÓN COMPLETA 🎉                ║
echo ╚══════════════════════════════════════════════════════════════════╝
echo.
echo 📊 Resumen de la operación:
echo    • Proyecto: %PROJECT_NAME%
echo    • Configuración: %BUILD_CONFIG%
echo    • Timestamp: %date% %time%
echo    • Log: logs\%LOG_FILE%
echo.
echo 💡 La aplicación debería estar ejecutándose ahora.
echo 📋 Si hay problemas, revisa el archivo de log para detalles.
echo.

:: Abrir el directorio de logs si hay errores
if exist "logs\%LOG_FILE%" (
    echo 📁 ¿Deseas abrir el directorio de logs? (S/N)
    set /p OPEN_LOGS=
    if /i "!OPEN_LOGS!"=="S" (
        explorer logs
    )
)

echo [%time%] === PROCESO COMPLETADO === >> logs\%LOG_FILE%
echo.
echo Presiona cualquier tecla para cerrar esta ventana...
pause >nul 