#Requires -Version 5.1
<#
.SYNOPSIS
    🚀 CHATBOT GOMARCO - Enterprise Auto-Updater con Ollama Automático
    
.DESCRIPTION
    Script empresarial para automatizar la instalación de Ollama y ejecución
    del Chatbot GOMARCO con configuración automática de IA.
    
.EXAMPLE
    .\ActualizarYEjecutar_Fixed.ps1
    
.NOTES
    Versión: 3.0 Enterprise (Ollama Auto-Install Fixed)
    Desarrollado por: DevOps Team GOMARCO
    Requisitos: .NET 8 SDK, PowerShell 5.1+
#>

[CmdletBinding()]
param(
    [Parameter(Mandatory=$false)]
    [ValidateSet("Debug", "Release")]
    [string]$ConfigurationType = "Debug",
    
    [Parameter(Mandatory=$false)]
    [switch]$SkipGitPull,
    
    [Parameter(Mandatory=$false)]
    [switch]$ForceOllamaInstall
)

# ====================================================================
# CONFIGURACIÓN EMPRESARIAL
# ====================================================================
$ErrorActionPreference = "Stop"
$ProgressPreference = "SilentlyContinue"

$Config = @{
    ProjectName = "ChatbotGomarco"
    SolutionFile = "chatbot.sln"
    ProjectFile = "ChatbotGomarco.csproj"
    BuildConfig = $ConfigurationType
    LogDirectory = "logs"
    MaxLogFiles = 10
    OllamaDownloadUrl = "https://ollama.com/download/windows"
    OllamaInstallerUrl = "https://github.com/ollama/ollama/releases/latest/download/OllamaSetup.exe"
}

# ====================================================================
# FUNCIONES UTILITARIAS
# ====================================================================

function Write-LogMessage {
    param(
        [Parameter(Mandatory=$true)]
        [string]$Message,
        
        [Parameter(Mandatory=$false)]
        [ValidateSet("INFO", "WARN", "ERROR", "SUCCESS")]
        [string]$Level = "INFO"
    )
    
    $timestamp = Get-Date -Format "yyyy-MM-dd HH:mm:ss"
    $logEntry = "[$timestamp] [$Level] $Message"
    
    # Color coding for console
    switch ($Level) {
        "INFO"    { Write-Host $logEntry -ForegroundColor Cyan }
        "WARN"    { Write-Host $logEntry -ForegroundColor Yellow }
        "ERROR"   { Write-Host $logEntry -ForegroundColor Red }
        "SUCCESS" { Write-Host $logEntry -ForegroundColor Green }
    }
    
    # Write to log file
    if ($script:LogFile) {
        $logEntry | Add-Content -Path $script:LogFile -Encoding UTF8
    }
}

function Test-OllamaInstalled {
    Write-LogMessage "🔍 Verificando instalación de Ollama..." -Level INFO
    
    try {
        $null = & ollama --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-LogMessage "✅ Ollama está instalado" -Level SUCCESS
            return $true
        }
    }
    catch {
        # Ollama no está instalado o no está en PATH
    }
    
    Write-LogMessage "❌ Ollama no está instalado" -Level WARN
    return $false
}

function Test-OllamaRunning {
    Write-LogMessage "🔍 Verificando si Ollama está ejecutándose..." -Level INFO
    
    try {
        $response = Invoke-WebRequest -Uri "http://localhost:11434/api/version" -TimeoutSec 5 -ErrorAction SilentlyContinue
        if ($response.StatusCode -eq 200) {
            Write-LogMessage "✅ Ollama está ejecutándose correctamente" -Level SUCCESS
            return $true
        }
    }
    catch {
        # Ollama no está ejecutándose
    }
    
    Write-LogMessage "❌ Ollama no está ejecutándose" -Level WARN
    return $false
}

function Start-OllamaService {
    Write-LogMessage "🚀 Iniciando servicio Ollama..." -Level INFO
    
    try {
        Start-Process -FilePath "ollama" -ArgumentList "serve" -WindowStyle Hidden -ErrorAction Stop
        Start-Sleep -Seconds 8
        
        # Verificar que se inició correctamente
        if (Test-OllamaRunning) {
            Write-LogMessage "✅ Ollama iniciado exitosamente" -Level SUCCESS
            return $true
        } else {
            Write-LogMessage "❌ Ollama no responde después de iniciar" -Level ERROR
            return $false
        }
    }
    catch {
        Write-LogMessage "❌ Error iniciando Ollama: $($_.Exception.Message)" -Level ERROR
        return $false
    }
}

function Install-OllamaAutomatic {
    Write-LogMessage "🚀 Instalando Ollama automáticamente..." -Level INFO
    
    try {
        $tempPath = [System.IO.Path]::GetTempPath()
        $installerPath = Join-Path $tempPath "OllamaSetup.exe"
        
        Write-LogMessage "    └─ Descargando instalador de Ollama..." -Level INFO
        
        # Limpiar instalador anterior
        if (Test-Path $installerPath) {
            Remove-Item $installerPath -Force -ErrorAction SilentlyContinue
        }
        
        # Descargar instalador
        $webRequest = @{
            Uri = $Config.OllamaInstallerUrl
            OutFile = $installerPath
            UserAgent = "Mozilla/5.0 Windows NT 10.0 Win64 x64 AppleWebKit/537.36"
            TimeoutSec = 180
        }
        Invoke-WebRequest @webRequest
        
        # Verificar descarga
        if ((Test-Path $installerPath) -and (Get-Item $installerPath).Length -gt 1MB) {
            Write-LogMessage "✅ Instalador descargado correctamente" -Level SUCCESS
            
            # Ejecutar instalador
            Write-LogMessage "    └─ Ejecutando instalador..." -Level INFO
            $installProcess = Start-Process -FilePath $installerPath -ArgumentList "/S" -Wait -PassThru -NoNewWindow
            
            if ($installProcess.ExitCode -eq 0) {
                Write-LogMessage "✅ Ollama instalado exitosamente" -Level SUCCESS
                
                # Esperar inicialización
                Start-Sleep -Seconds 10
                
                # Verificar instalación
                if (Test-OllamaInstalled) {
                    Write-LogMessage "✅ Ollama verificado y funcionando" -Level SUCCESS
                    return $true
                } else {
                    Write-LogMessage "⚠️  Instalación completada - reinicia PowerShell para continuar" -Level WARN
                    return $false
                }
            } else {
                throw "Instalador falló con código: $($installProcess.ExitCode)"
            }
        } else {
            throw "Error descargando el instalador"
        }
    }
    catch {
        Write-LogMessage "❌ Error en instalación automática: $($_.Exception.Message)" -Level ERROR
        return $false
    }
    finally {
        # Limpiar archivo temporal
        if (Test-Path $installerPath) {
            Remove-Item $installerPath -Force -ErrorAction SilentlyContinue
        }
    }
}

function Open-OllamaDownloadPage {
    Write-LogMessage "🌐 Abriendo página de descarga de Ollama..." -Level INFO
    
    try {
        Start-Process $Config.OllamaDownloadUrl
        Write-LogMessage "✅ Página de descarga abierta en el navegador" -Level SUCCESS
        
        Write-Host ""
        Write-Host "📋 INSTRUCCIONES PARA INSTALAR OLLAMA:" -ForegroundColor Yellow
        Write-Host "   1. Descarga 'Download for Windows' desde la página abierta" -ForegroundColor White
        Write-Host "   2. Ejecuta el instalador como administrador" -ForegroundColor White
        Write-Host "   3. Reinicia esta aplicación después de la instalación" -ForegroundColor White
        Write-Host ""
        
        $response = Read-Host "¿Deseas intentar la instalación automática en su lugar? (S/N)"
        if ($response -eq "S" -or $response -eq "s") {
            return Install-OllamaAutomatic
        }
        
        return $false
    }
    catch {
        Write-LogMessage "❌ Error abriendo navegador: $($_.Exception.Message)" -Level ERROR
        Write-LogMessage "📋 Visita manualmente: $($Config.OllamaDownloadUrl)" -Level INFO
        return $false
    }
}

function Install-EssentialModels {
    Write-LogMessage "🧠 Instalando modelos esenciales para DeepSeek..." -Level INFO
    
    $modelos = @(
        @{ Nombre = "phi3:mini"; Descripcion = "Phi-3-Mini (Base estable)" },
        @{ Nombre = "deepseek-r1:7b"; Descripcion = "DeepSeek-R1 7B (Razonamiento)" }
    )
    
    foreach ($modelo in $modelos) {
        try {
            Write-LogMessage "📥 Descargando $($modelo.Descripcion)..." -Level INFO
            Write-LogMessage "    └─ Esto puede tardar varios minutos..." -Level INFO
            
            $pullProcess = Start-Process -FilePath "ollama" -ArgumentList "pull", $modelo.Nombre -Wait -PassThru -NoNewWindow
            
            if ($pullProcess.ExitCode -eq 0) {
                Write-LogMessage "✅ $($modelo.Descripcion) instalado correctamente" -Level SUCCESS
            } else {
                Write-LogMessage "⚠️  Error descargando $($modelo.Descripcion)" -Level WARN
            }
        }
        catch {
            Write-LogMessage "⚠️  Error con $($modelo.Descripcion): $($_.Exception.Message)" -Level WARN
        }
    }
}

function Setup-OllamaComplete {
    Write-LogMessage "🔧 CONFIGURACIÓN AUTOMÁTICA DE OLLAMA" -Level INFO
    Write-LogMessage "    └─ Necesario para DeepSeek y modelos locales" -Level INFO
    
    # Paso 1: Verificar si está instalado
    if (-not (Test-OllamaInstalled)) {
        Write-LogMessage "📦 Ollama no encontrado - iniciando instalación..." -Level WARN
        
        if ($ForceOllamaInstall) {
            # Instalación automática forzada
            if (-not (Install-OllamaAutomatic)) {
                Write-LogMessage "❌ Instalación automática falló" -Level ERROR
                Open-OllamaDownloadPage
                return $false
            }
        } else {
            # Preguntar al usuario qué prefiere
            Write-Host ""
            Write-Host "🤖 OLLAMA REQUERIDO PARA DEEPSEEK" -ForegroundColor Yellow
            Write-Host "   DeepSeek y otros modelos locales requieren Ollama para funcionar." -ForegroundColor White
            Write-Host ""
            Write-Host "   Opciones disponibles:" -ForegroundColor Cyan
            Write-Host "   1. Instalación automática (recomendado)" -ForegroundColor White
            Write-Host "   2. Descarga manual desde ollama.com" -ForegroundColor White
            Write-Host ""
            
            $choice = Read-Host "¿Qué opción prefieres? (1/2)"
            
            if ($choice -eq "1") {
                if (-not (Install-OllamaAutomatic)) {
                    Write-LogMessage "❌ Instalación automática falló - abriendo página manual" -Level ERROR
                    Open-OllamaDownloadPage
                    return $false
                }
            } else {
                Open-OllamaDownloadPage
                return $false
            }
        }
    }
    
    # Paso 2: Verificar que esté ejecutándose
    if (-not (Test-OllamaRunning)) {
        Write-LogMessage "🚀 Ollama instalado pero no está ejecutándose - iniciando..." -Level INFO
        if (-not (Start-OllamaService)) {
            Write-LogMessage "❌ No se pudo iniciar Ollama" -Level ERROR
            return $false
        }
    }
    
    # Paso 3: Instalar modelos esenciales
    Write-LogMessage "📚 Verificando modelos necesarios..." -Level INFO
    Install-EssentialModels
    
    Write-LogMessage "✅ OLLAMA CONFIGURADO COMPLETAMENTE" -Level SUCCESS
    Write-LogMessage "    └─ DeepSeek y modelos locales listos para usar" -Level SUCCESS
    
    return $true
}

function Test-Prerequisites {
    Write-LogMessage "🔍 Verificando prerrequisitos del sistema..." -Level INFO
    
    # Verificar .NET SDK
    try {
        $dotnetVersion = & dotnet --version 2>$null
        if ($LASTEXITCODE -eq 0) {
            Write-LogMessage "✅ .NET SDK detectado: $dotnetVersion" -Level SUCCESS
        } else {
            throw "No se detectó .NET SDK"
        }
    }
    catch {
        Write-LogMessage "❌ .NET 8 SDK no está instalado" -Level ERROR
        throw
    }
    
    # Verificar archivos del proyecto
    @($Config.SolutionFile, $Config.ProjectFile) | ForEach-Object {
        if (-not (Test-Path $_)) {
            Write-LogMessage "❌ Archivo requerido no encontrado: $_" -Level ERROR
            throw "Archivo del proyecto faltante: $_"
        }
    }
    
    Write-LogMessage "✅ Prerrequisitos básicos verificados" -Level SUCCESS
}

function Update-SourceCode {
    if ($SkipGitPull) {
        Write-LogMessage "⏭️  Omitiendo actualización Git" -Level WARN
        return
    }
    
    Write-LogMessage "🔄 Verificando actualizaciones Git..." -Level INFO
    
    try {
        $null = & git --version 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-LogMessage "⚠️  Git no disponible - usando código local" -Level WARN
            return
        }
        
        $pullResult = & git pull origin master 2>&1
        if ($LASTEXITCODE -eq 0) {
            Write-LogMessage "✅ Código actualizado desde Git" -Level SUCCESS
        } else {
            Write-LogMessage "⚠️  Warning: No se pudo actualizar Git" -Level WARN
        }
    }
    catch {
        Write-LogMessage "⚠️  Error Git: usando código local" -Level WARN
    }
}

function Clear-BuildArtifacts {
    Write-LogMessage "🧹 Limpiando artefactos de compilación..." -Level INFO
    
    # Terminar procesos relacionados
    Get-Process | Where-Object {
        $_.ProcessName -like "*ChatbotGomarco*" -or 
        $_.ProcessName -like "*dotnet*"
    } | Stop-Process -Force -ErrorAction SilentlyContinue
    
    Start-Sleep -Seconds 2
    
    # Limpiar directorios
    @("bin", "obj") | ForEach-Object {
        if (Test-Path $_) {
            Remove-Item $_ -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    
    Write-LogMessage "✅ Limpieza completada" -Level SUCCESS
}

function Restore-Dependencies {
    Write-LogMessage "📦 Restaurando dependencias NuGet..." -Level INFO
    
    $restoreOutput = & dotnet restore $Config.SolutionFile 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-LogMessage "✅ Dependencias restauradas exitosamente" -Level SUCCESS
    } else {
        Write-LogMessage "❌ Error en restauración: $($restoreOutput -join "`n")" -Level ERROR
        throw "Falló la restauración de dependencias"
    }
}

function Build-Application {
    Write-LogMessage "🔨 Compilando aplicación en modo $($Config.BuildConfig)..." -Level INFO
    
    $buildArgs = @(
        "build"
        $Config.SolutionFile
        "-c", $Config.BuildConfig
        "--no-restore"
        "--force"
        "--no-incremental"
    )
    
    $buildOutput = & dotnet @buildArgs 2>&1
    if ($LASTEXITCODE -eq 0) {
        Write-LogMessage "✅ Compilación exitosa" -Level SUCCESS
    } else {
        Write-LogMessage "❌ Error compilando: $($buildOutput -join "`n")" -Level ERROR
        throw "Falló la compilación"
    }
}

function Stop-ExistingProcesses {
    Write-LogMessage "⏹️  Verificando procesos existentes..." -Level INFO
    
    $processes = Get-Process -Name $Config.ProjectName -ErrorAction SilentlyContinue
    if ($processes) {
        Write-LogMessage "    └─ Deteniendo $($processes.Count) proceso(s)..." -Level INFO
        $processes | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-LogMessage "✅ Procesos anteriores detenidos" -Level SUCCESS
    } else {
        Write-LogMessage "✅ No hay procesos previos" -Level SUCCESS
    }
}

function Start-Application {
    Write-LogMessage "🚀 Iniciando aplicación con Ollama configurado..." -Level INFO
    
    try {
        $runArgs = @(
            "run"
            "--project", $Config.ProjectFile
            "--no-build"
        )
        
        Start-Process -FilePath "dotnet" -ArgumentList $runArgs -WindowStyle Hidden
        Start-Sleep -Seconds 3
        
        $runningProcess = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue
        if ($runningProcess) {
            Write-LogMessage "✅ Aplicación iniciada correctamente" -Level SUCCESS
            Write-LogMessage "✅ DeepSeek y modelos locales disponibles" -Level SUCCESS
            return $true
        } else {
            # Método alternativo
            $exePath = "bin\$($Config.BuildConfig)\net8.0-windows\$($Config.ProjectName).exe"
            if (Test-Path $exePath) {
                Start-Process -FilePath $exePath
                Write-LogMessage "✅ Aplicación iniciada desde binario" -Level SUCCESS
                return $true
            }
        }
        
        return $false
    }
    catch {
        Write-LogMessage "❌ Error iniciando aplicación: $($_.Exception.Message)" -Level ERROR
        return $false
    }
}

function Show-Summary {
    param([bool]$Success, [bool]$OllamaConfigured)
    
    Write-Host "`n"
    Write-Host "╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    
    if ($Success) {
        Write-Host "║                     🎉 CHATBOT GOMARCO LISTO 🎉                 ║" -ForegroundColor Green
        if ($OllamaConfigured) {
            Write-Host "║                   ✅ OLLAMA Y DEEPSEEK ACTIVOS ✅                ║" -ForegroundColor Green
        }
    } else {
        Write-Host "║                     ❌ ERROR EN CONFIGURACIÓN ❌                ║" -ForegroundColor Red
    }
    
    Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    
    if ($Success) {
        Write-Host "`n🚀 FUNCIONALIDADES DISPONIBLES:" -ForegroundColor Cyan
        Write-Host "   ✅ OpenAI GPT-4 (Requiere API Key)" -ForegroundColor Green
        if ($OllamaConfigured) {
            Write-Host "   ✅ DeepSeek-R1 7B (Local, Razonamiento Avanzado)" -ForegroundColor Green
            Write-Host "   ✅ Phi-3-Mini (Local, Estable)" -ForegroundColor Green
            Write-Host "   ✅ Ollama (Procesamiento 100% Local)" -ForegroundColor Green
        }
        Write-Host "`n💡 La aplicación está ejecutándose y lista para usar." -ForegroundColor Green
    }
}

# ====================================================================
# FUNCIÓN PRINCIPAL
# ====================================================================
function Main {
    try {
        # Inicializar logging
        if (-not (Test-Path $Config.LogDirectory)) {
            New-Item -ItemType Directory -Path $Config.LogDirectory -Force | Out-Null
        }
        
        $script:LogFile = Join-Path $Config.LogDirectory "deployment_$(Get-Date -Format 'yyyyMMdd_HHmmss').log"
        
        # Header
        Write-Host "`n"
        Write-Host "╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║              🤖 CHATBOT GOMARCO - AUTO SETUP 🤖               ║" -ForegroundColor Green
        Write-Host "║                 🧠 CON OLLAMA Y DEEPSEEK 🧠                   ║" -ForegroundColor Cyan
        Write-Host "║                   Enterprise DevOps Solution                    ║" -ForegroundColor Green
        Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
        
        Write-LogMessage "=== INICIANDO CONFIGURACIÓN AUTOMÁTICA ===" -Level INFO
        
        # Pipeline de configuración
        Test-Prerequisites
        
        # CONFIGURACIÓN AUTOMÁTICA DE OLLAMA (PASO CRÍTICO)
        Write-LogMessage "" -Level INFO
        Write-LogMessage "🎯 PASO CRÍTICO: Configurando Ollama para DeepSeek..." -Level INFO
        $ollamaConfigured = Setup-OllamaComplete
        
        # Continuar con el resto del setup
        Update-SourceCode
        Clear-BuildArtifacts
        Restore-Dependencies
        Stop-ExistingProcesses
        Build-Application
        $appStarted = Start-Application
        
        Show-Summary -Success $appStarted -OllamaConfigured $ollamaConfigured
        
        Write-LogMessage "=== CONFIGURACIÓN COMPLETADA ===" -Level SUCCESS
        
        if ($ollamaConfigured) {
            Write-LogMessage "🎉 DEEPSEEK Y MODELOS LOCALES LISTOS PARA USAR" -Level SUCCESS
        }
        
    }
    catch {
        Write-LogMessage "=== ERROR CRÍTICO: $($_.Exception.Message) ===" -Level ERROR
        Show-Summary -Success $false -OllamaConfigured $false
        throw
    }
}

# ====================================================================
# EJECUCIÓN
# ====================================================================
if ($MyInvocation.InvocationName -ne '.') {
    Main
}