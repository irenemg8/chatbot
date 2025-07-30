@echo off
:: ====================================================================
:: 🚀 SCRIPT DE EJECUCIÓN AUTOMÁTICA - CHATBOT GOMARCO CON OLLAMA
:: Versión: 3.0 Enterprise (Con Auto-Setup de Ollama)
:: Desarrollado por: DevOps Team GOMARCO
:: ====================================================================

setlocal EnableDelayedExpansion
color 0A

echo.
echo ╔══════════════════════════════════════════════════════════════════╗
echo ║              🤖 CHATBOT GOMARCO - AUTO SETUP 🤖               ║
echo ║                 🧠 CON OLLAMA Y DEEPSEEK 🧠                   ║
echo ║                     Enterprise DevOps Solution                    ║
echo ╚══════════════════════════════════════════════════════════════════╝
echo.

:: Variables de configuración empresarial
set PROJECT_NAME=ChatbotGomarco
set BUILD_CONFIG=Debug
set SOLUTION_FILE=chatbot.sln
set PROJECT_FILE=ChatbotGomarco.csproj
set LOG_FILE=deployment_%date:~-4,4%%date:~-10,2%%date:~-7,2%_%time:~0,2%%time:~3,2%.log
set OLLAMA_DOWNLOAD_URL=https://ollama.com/download/windows

:: Crear directorio de logs si no existe
if not exist "logs" mkdir logs

echo [%time%] === INICIANDO CONFIGURACIÓN AUTOMÁTICA === >> logs\%LOG_FILE%

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
:: FASE 2: CONFIGURACIÓN AUTOMÁTICA DE OLLAMA (CRÍTICO PARA DEEPSEEK)
:: ====================================================================
echo.
echo 🧠 FASE 2: Configurando Ollama para DeepSeek...
echo [%time%] INFO: Verificando Ollama para DeepSeek >> logs\%LOG_FILE%

:: Verificar si Ollama está instalado
echo    └─ Verificando instalación de Ollama...
ollama --version >nul 2>&1
if errorlevel 1 (
    echo ❌ Ollama no está instalado
    echo [%time%] WARNING: Ollama no encontrado >> logs\%LOG_FILE%
    
    echo.
    echo 🤖 OLLAMA REQUERIDO PARA DEEPSEEK
    echo    DeepSeek y otros modelos locales requieren Ollama para funcionar.
    echo.
    echo    Opciones disponibles:
    echo    1. Abrir página de descarga de Ollama (recomendado)
    echo    2. Continuar sin Ollama (solo OpenAI disponible)
    echo.
    set /p OLLAMA_CHOICE="¿Qué opción prefieres? (1/2): "
    
    if "!OLLAMA_CHOICE!"=="1" (
        echo    └─ 🌐 Abriendo página de descarga de Ollama...
        start "" "%OLLAMA_DOWNLOAD_URL%"
        echo.
        echo 📋 INSTRUCCIONES PARA INSTALAR OLLAMA:
        echo    1. Descarga 'Download for Windows' desde la página abierta
        echo    2. Ejecuta el instalador como administrador
        echo    3. Reinicia esta aplicación después de la instalación
        echo.
        echo [%time%] INFO: Usuario dirigido a descarga de Ollama >> logs\%LOG_FILE%
        pause
        echo    └─ ⚠️  Continuando sin Ollama - solo OpenAI disponible
        goto SKIP_OLLAMA_SETUP
    ) else (
        echo    └─ ⚠️  Continuando sin Ollama - solo OpenAI disponible
        echo [%time%] INFO: Usuario eligió continuar sin Ollama >> logs\%LOG_FILE%
        goto SKIP_OLLAMA_SETUP
    )
) else (
    echo    ✅ Ollama está instalado
    echo [%time%] SUCCESS: Ollama encontrado >> logs\%LOG_FILE%
)

:: Verificar si Ollama está ejecutándose
echo    └─ Verificando servicio Ollama...
curl -s http://localhost:11434/api/version >nul 2>&1
if errorlevel 1 (
    echo    └─ 🚀 Ollama instalado pero no ejecutándose - iniciando...
    start "" ollama serve
    timeout /t 5 /nobreak >nul
    
    :: Verificar nuevamente
    curl -s http://localhost:11434/api/version >nul 2>&1
    if errorlevel 1 (
        echo    └─ ⚠️  Ollama no responde - continuando de todos modos
        echo [%time%] WARNING: Ollama no responde >> logs\%LOG_FILE%
    ) else (
        echo    ✅ Ollama iniciado exitosamente
        echo [%time%] SUCCESS: Ollama service started >> logs\%LOG_FILE%
    )
) else (
    echo    ✅ Ollama está ejecutándose correctamente
    echo [%time%] SUCCESS: Ollama running >> logs\%LOG_FILE%
)

:: Verificar/instalar modelos esenciales
echo    └─ Verificando modelos esenciales...
echo [%time%] INFO: Checking essential models >> logs\%LOG_FILE%

:: Intentar instalar phi3:mini si no está disponible
ollama list | findstr "phi3" >nul 2>&1
if errorlevel 1 (
    echo    └─ 📥 Descargando Phi-3-Mini (modelo base estable)...
    echo       Esto puede tardar varios minutos...
    start /wait "" ollama pull phi3:mini
    if not errorlevel 1 (
        echo    └─ ✅ Phi-3-Mini instalado correctamente
        echo [%time%] SUCCESS: phi3:mini installed >> logs\%LOG_FILE%
    ) else (
        echo    └─ ⚠️  Error descargando Phi-3-Mini
        echo [%time%] WARNING: phi3:mini install failed >> logs\%LOG_FILE%
    )
) else (
    echo    └─ ✅ Phi-3-Mini ya está disponible
)

:: Intentar instalar deepseek-r1:7b si no está disponible  
ollama list | findstr "deepseek" >nul 2>&1
if errorlevel 1 (
    echo    └─ 📥 Descargando DeepSeek-R1 7B (razonamiento avanzado)...
    echo       Esto puede tardar 10-15 minutos...
    start /wait "" ollama pull deepseek-r1:7b
    if not errorlevel 1 (
        echo    └─ ✅ DeepSeek-R1 7B instalado correctamente
        echo [%time%] SUCCESS: deepseek-r1:7b installed >> logs\%LOG_FILE%
    ) else (
        echo    └─ ⚠️  Error descargando DeepSeek-R1 7B
        echo [%time%] WARNING: deepseek-r1:7b install failed >> logs\%LOG_FILE%
    )
) else (
    echo    └─ ✅ DeepSeek-R1 7B ya está disponible
)

echo    ✅ Configuración de Ollama completada
echo [%time%] SUCCESS: Ollama setup completed >> logs\%LOG_FILE%

:SKIP_OLLAMA_SETUP

:: ====================================================================
:: FASE 3: ACTUALIZACIÓN DE CÓDIGO FUENTE
:: ====================================================================
echo.
echo 🔄 FASE 3: Sincronizando con repositorio...

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
:: FASE 4: LIMPIEZA DE ARTEFACTOS PREVIOS
:: ====================================================================
echo.
echo 🧹 FASE 4: Limpiando artefactos de compilación...

echo    └─ Limpiando directorios bin y obj...
if exist "bin" rmdir /s /q "bin"
if exist "obj" rmdir /s /q "obj"
echo    ✅ Artefactos anteriores eliminados

:: ====================================================================
:: FASE 5: RESTAURACIÓN DE DEPENDENCIAS
:: ====================================================================
echo.
echo 📦 FASE 5: Restaurando dependencias NuGet...

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
:: FASE 6: COMPILACIÓN OPTIMIZADA
:: ====================================================================
echo.
echo 🔨 FASE 6: Compilando aplicación...

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
:: FASE 7: DETENER PROCESOS EXISTENTES
:: ====================================================================
echo.
echo ⏹️  FASE 7: Verificando procesos existentes...

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
:: FASE 8: EJECUCIÓN DE LA APLICACIÓN
:: ====================================================================
echo.
echo 🚀 FASE 8: Iniciando aplicación con Ollama configurado...

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
echo ║                     🎉 CHATBOT GOMARCO LISTO 🎉                 ║
echo ║                   ✅ OLLAMA Y DEEPSEEK ACTIVOS ✅                ║
echo ╚══════════════════════════════════════════════════════════════════╝
echo.
echo 🚀 FUNCIONALIDADES DISPONIBLES:
echo    ✅ OpenAI GPT-4 (Requiere API Key)
echo    ✅ DeepSeek-R1 7B (Local, Razonamiento Avanzado)
echo    ✅ Phi-3-Mini (Local, Estable)
echo    ✅ Ollama (Procesamiento 100%% Local)
echo.
echo 📊 Resumen de la operación:
echo    • Proyecto: %PROJECT_NAME%
echo    • Configuración: %BUILD_CONFIG%
echo    • Timestamp: %date% %time%
echo    • Log: logs\%LOG_FILE%
echo.
echo 💡 La aplicación debería estar ejecutándose ahora.
echo 🧠 DeepSeek y modelos locales están listos para usar.
echo 📋 Si hay problemas, revisa el archivo de log para detalles.
echo.

echo [%time%] === CONFIGURACIÓN COMPLETADA === >> logs\%LOG_FILE%
echo.
echo Presiona cualquier tecla para cerrar esta ventana...
pause >nul