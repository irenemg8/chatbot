@echo off
echo ================================================
echo    INSTALACION AUTOMATICA - CHATBOT GOMARCO
echo ================================================
echo.
echo Este script instalara automaticamente el Chatbot GOMARCO
echo en tu equipo Windows 11.
echo.
echo Descansa como te mereces - GOMARCO
echo.
echo IMPORTANTE: Este script debe ejecutarse como ADMINISTRADOR
echo.
pause

:: Verificar permisos de administrador
echo Verificando permisos de administrador...
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo.
    echo ERROR: Este script requiere permisos de administrador.
    echo.
    echo Por favor:
    echo 1. Cierre esta ventana
    echo 2. Haga clic derecho en el archivo InstalarChatbotGomarco.bat
    echo 3. Seleccione "Ejecutar como administrador"
    echo.
    pause
    exit /b 1
)

:: Verificar si es Windows 10/11
echo Verificando compatibilidad del sistema...
for /f "tokens=4-5 delims=. " %%i in ('ver') do set VERSION=%%i.%%j
if "%VERSION%" LSS "10.0" (
    echo ERROR: Este software requiere Windows 10 o superior.
    echo Su version: %VERSION%
    pause
    exit /b 1
)
echo Sistema compatible: Windows %VERSION%

:: Verificar que existan los archivos de la aplicacion
echo Verificando archivos de la aplicacion...
if not exist "App\ChatbotGomarco.exe" (
    echo.
    echo ERROR: No se encontraron los archivos de la aplicacion.
    echo.
    echo Asegurese de que esta estructura este presente:
    echo %~dp0App\ChatbotGomarco.exe
    echo %~dp0App\ChatbotGomarco.dll
    echo %~dp0App\Resources\
    echo.
    echo Para compilar la aplicacion ejecute:
    echo dotnet publish ChatbotGomarco.csproj -c Release -o App
    echo.
    pause
    exit /b 1
)
echo Archivos de aplicacion encontrados: OK

:: Crear directorios necesarios
echo Creando directorios de instalacion...
if not exist "%ProgramFiles%\GOMARCO" (
    mkdir "%ProgramFiles%\GOMARCO"
    if errorlevel 1 (
        echo ERROR: No se pudo crear el directorio GOMARCO
        pause
        exit /b 1
    )
)

if not exist "%ProgramFiles%\GOMARCO\ChatbotGomarco" (
    mkdir "%ProgramFiles%\GOMARCO\ChatbotGomarco"
    if errorlevel 1 (
        echo ERROR: No se pudo crear el directorio ChatbotGomarco
        pause
        exit /b 1
    )
)
echo Directorios creados: OK

:: Verificar .NET 8 Runtime
echo Verificando .NET 8 Runtime...
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo .NET 8 Runtime no encontrado. Descargando e instalando...
    
    :: URL correcta de .NET 8 Desktop Runtime
    set DOTNET_URL=https://download.microsoft.com/download/a/b/c/abc0d272-7fb8-49e7-9a02-5bdcb45e39b5/windowsdesktop-runtime-8.0.0-win-x64.exe
    
    echo Descargando desde: %DOTNET_URL%
    powershell -Command "try { Invoke-WebRequest -Uri '%DOTNET_URL%' -OutFile 'dotnet-runtime-installer.exe' -UseBasicParsing } catch { Write-Host 'Error al descargar .NET 8 Runtime'; exit 1 }"
    
    if exist "dotnet-runtime-installer.exe" (
        echo Instalando .NET 8 Runtime...
        start /wait dotnet-runtime-installer.exe /quiet
        del dotnet-runtime-installer.exe
        echo .NET 8 Runtime instalado.
    ) else (
        echo ERROR: No se pudo descargar .NET 8 Runtime.
        echo.
        echo Descargue manualmente desde:
        echo https://dotnet.microsoft.com/download/dotnet/8.0
        echo.
        pause
        exit /b 1
    )
) else (
    for /f "tokens=*" %%i in ('dotnet --version 2^>nul') do set DOTNET_VERSION=%%i
    echo .NET encontrado: %DOTNET_VERSION%
)

:: Copiar archivos de la aplicacion
echo Copiando archivos de la aplicacion...
xcopy /E /I /Y /Q "App\*" "%ProgramFiles%\GOMARCO\ChatbotGomarco\"
if errorlevel 1 (
    echo ERROR: No se pudieron copiar los archivos de la aplicacion
    pause
    exit /b 1
)
echo Archivos copiados: OK

:: Crear acceso directo en el escritorio
echo Creando acceso directo en el escritorio...
powershell -Command "try { $WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%PUBLIC%\Desktop\Chatbot GOMARCO.lnk'); $Shortcut.TargetPath = '%ProgramFiles%\GOMARCO\ChatbotGomarco\ChatbotGomarco.exe'; $Shortcut.IconLocation = '%ProgramFiles%\GOMARCO\ChatbotGomarco\Resources\gomarco-icon.ico'; $Shortcut.Description = 'Asistente de IA Corporativo GOMARCO'; $Shortcut.Save() } catch { Write-Host 'Advertencia: No se pudo crear acceso directo en escritorio' }"

:: Crear entrada en el menu inicio
echo Creando entrada en el menu de inicio...
if not exist "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\GOMARCO" mkdir "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\GOMARCO"
powershell -Command "try { $WshShell = New-Object -comObject WScript.Shell; $Shortcut = $WshShell.CreateShortcut('%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\GOMARCO\Chatbot GOMARCO.lnk'); $Shortcut.TargetPath = '%ProgramFiles%\GOMARCO\ChatbotGomarco\ChatbotGomarco.exe'; $Shortcut.IconLocation = '%ProgramFiles%\GOMARCO\ChatbotGomarco\Resources\gomarco-icon.ico'; $Shortcut.Description = 'Asistente de IA Corporativo GOMARCO'; $Shortcut.Save() } catch { Write-Host 'Advertencia: No se pudo crear entrada en menu inicio' }"

:: Configurar permisos de directorios
echo Configurando permisos de usuario...
icacls "%ProgramFiles%\GOMARCO\ChatbotGomarco" /grant "Users":(OI)(CI)RX >nul 2>&1
icacls "%ProgramFiles%\GOMARCO\ChatbotGomarco" /grant "Authenticated Users":(OI)(CI)RX >nul 2>&1
echo Permisos configurados: OK

:: Registrar en Programas y Caracteristicas
echo Registrando en el sistema...
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ChatbotGomarco" /v "DisplayName" /t REG_SZ /d "Chatbot GOMARCO" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ChatbotGomarco" /v "Publisher" /t REG_SZ /d "GOMARCO" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ChatbotGomarco" /v "DisplayVersion" /t REG_SZ /d "1.0.0" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ChatbotGomarco" /v "InstallLocation" /t REG_SZ /d "%ProgramFiles%\GOMARCO\ChatbotGomarco" /f >nul 2>&1
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ChatbotGomarco" /v "UninstallString" /t REG_SZ /d "%ProgramFiles%\GOMARCO\ChatbotGomarco\Desinstalar.bat" /f >nul 2>&1
echo Registro completado: OK

:: Crear script de desinstalacion
echo Creando script de desinstalacion...
(
echo @echo off
echo echo Desinstalando Chatbot GOMARCO...
echo taskkill /F /IM ChatbotGomarco.exe 2^>nul
echo timeout /t 2 /nobreak ^>nul
echo rd /S /Q "%ProgramFiles%\GOMARCO\ChatbotGomarco"
echo del "%PUBLIC%\Desktop\Chatbot GOMARCO.lnk" 2^>nul
echo rd /S /Q "%ALLUSERSPROFILE%\Microsoft\Windows\Start Menu\Programs\GOMARCO"
echo reg delete "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ChatbotGomarco" /f 2^>nul
echo echo Chatbot GOMARCO desinstalado correctamente.
echo pause
) > "%ProgramFiles%\GOMARCO\ChatbotGomarco\Desinstalar.bat"

:: Verificar instalacion
echo Verificando instalacion...
if exist "%ProgramFiles%\GOMARCO\ChatbotGomarco\ChatbotGomarco.exe" (
    echo Aplicacion instalada: OK
    
    echo Iniciando la aplicacion por primera vez...
    start "" "%ProgramFiles%\GOMARCO\ChatbotGomarco\ChatbotGomarco.exe"
) else (
    echo ERROR: La instalacion no se completo correctamente.
    pause
    exit /b 1
)

echo.
echo ================================================
echo    INSTALACION COMPLETADA EXITOSAMENTE
echo ================================================
echo.
echo El Chatbot GOMARCO ha sido instalado correctamente.
echo.
echo Ubicacion: %ProgramFiles%\GOMARCO\ChatbotGomarco
echo Acceso directo creado en el escritorio
echo Disponible en el menu de inicio
echo.
echo IMPORTANTE:
echo - Todos los archivos se cifran automaticamente
echo - Los datos se almacenan localmente de forma segura
echo - Compatible con documentos PDF, Word, Excel e imagenes
echo.
echo Descansa como te mereces - GOMARCO
echo.
pause 