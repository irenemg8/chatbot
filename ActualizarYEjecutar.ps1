#Requires -Version 5.1
<#
.SYNOPSIS
    🚀 CHATBOT GOMARCO - Enterprise Auto-Updater PowerShell Edition
    
.DESCRIPTION
    Script empresarial avanzado para automatizar la actualización y ejecución
    del Chatbot GOMARCO con características enterprise-grade.
    
.PARAMETER ConfigurationType
    Tipo de configuración de build (Debug/Release)
    
.PARAMETER SkipGitPull
    Omite la actualización desde Git
    
.PARAMETER Verbose
    Muestra información detallada del proceso
    
.EXAMPLE
    .\ActualizarYEjecutar.ps1
    
.EXAMPLE
    .\ActualizarYEjecutar.ps1 -ConfigurationType Release -Verbose
    
.NOTES
    Versión: 2.0 Enterprise
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
    [switch]$ForceRecompile
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
}

# ====================================================================
# FUNCIONES UTILITARIAS ENTERPRISE
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
    $logEntry | Add-Content -Path $script:LogFile -Encoding UTF8
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
    
    # Verificar e instalar Ollama si es necesario
    Test-AndInstall-Ollama
    
    Write-LogMessage "✅ Todos los prerrequisitos verificados" -Level SUCCESS
}

function Test-AndInstall-Ollama {
    Write-LogMessage "🔍 Verificando instalación de Ollama..." -Level INFO
    
    try {
        # Verificar si Ollama está instalado
        $ollamaInstalled = $false
        try {
            $null = & ollama --version 2>$null
            if ($LASTEXITCODE -eq 0) {
                $ollamaInstalled = $true
                Write-LogMessage "✅ Ollama ya está instalado" -Level SUCCESS
            }
        }
        catch {
            # Ollama no está en PATH
        }
        
        if (-not $ollamaInstalled) {
            Write-LogMessage "📦 Ollama no detectado - iniciando instalación automática..." -Level INFO
            Install-Ollama
        }
        
        # Verificar y descargar Phi-4-Mini
        Ensure-Phi4Mini-Model
    }
    catch {
        Write-LogMessage "⚠️  Error configurando Ollama - continuando sin IA local" -Level WARN
        Write-LogMessage "    El sistema funcionará solo con OpenAI si está configurado" -Level WARN
    }
}

function Install-Ollama {
    Write-LogMessage "🚀 Instalando Ollama para IA local..." -Level INFO
    
    try {
        # Usar el enlace directo del instalador de Ollama (GitHub releases)
        $ollamaUrl = "https://github.com/ollama/ollama/releases/latest/download/OllamaSetup.exe"
        $tempPath = [System.IO.Path]::GetTempPath()
        $installerPath = Join-Path $tempPath "OllamaSetup.exe"
        
        Write-LogMessage "    └─ Descargando instalador desde GitHub releases..." -Level INFO
        
        # Limpiar instalador anterior si existe
        if (Test-Path $installerPath) {
            Remove-Item $installerPath -Force -ErrorAction SilentlyContinue
        }
        
        # Descargar instalador con mejor manejo de errores
        try {
            # Usar Invoke-WebRequest con UserAgent para evitar bloqueos
            $webRequest = @{
                Uri = $ollamaUrl
                OutFile = $installerPath
                UserAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36"
                TimeoutSec = 120
                ErrorAction = "Stop"
            }
            Invoke-WebRequest @webRequest
            
            Write-LogMessage "    └─ Descarga completada: $(Get-Item $installerPath | Select-Object -ExpandProperty Length) bytes" -Level INFO
        }
        catch {
            Write-LogMessage "❌ Error descargando: $($_.Exception.Message)" -Level ERROR
            throw "No se pudo descargar el instalador de Ollama"
        }
        
        # Verificar que el archivo descargado es válido
        if ((Test-Path $installerPath) -and (Get-Item $installerPath).Length -gt 1MB) {
            Write-LogMessage "    └─ Archivo válido detectado, ejecutando instalador..." -Level INFO
            
            # Ejecutar instalador en modo silencioso
            try {
                $installProcess = Start-Process -FilePath $installerPath -ArgumentList "/S" -Wait -PassThru -NoNewWindow
                
                if ($installProcess.ExitCode -eq 0) {
                    Write-LogMessage "✅ Ollama instalado exitosamente" -Level SUCCESS
                    
                    # Esperar para que el servicio se inicialice
                    Write-LogMessage "    └─ Esperando inicialización del servicio..." -Level INFO
                    Start-Sleep -Seconds 10
                    
                    # Verificar instalación
                    $ollamaInstalled = $false
                    for ($i = 0; $i -lt 3; $i++) {
                        try {
                            $null = & ollama --version 2>$null
                            if ($LASTEXITCODE -eq 0) {
                                $ollamaInstalled = $true
                                break
                            }
                        }
                        catch {
                            Start-Sleep -Seconds 3
                        }
                    }
                    
                    if ($ollamaInstalled) {
                        Write-LogMessage "✅ Ollama verificado y funcionando" -Level SUCCESS
                    } else {
                        Write-LogMessage "⚠️  Ollama instalado - requiere reiniciar terminal/PowerShell" -Level WARN
                        Write-LogMessage "    └─ Ejecuta: refreshenv o reinicia PowerShell" -Level INFO
                    }
                } else {
                    throw "Instalador terminó con código de error: $($installProcess.ExitCode)"
                }
            }
            catch {
                throw "Error ejecutando instalador: $($_.Exception.Message)"
            }
            
            # Limpiar archivo temporal
            Remove-Item $installerPath -Force -ErrorAction SilentlyContinue
        } else {
            throw "Archivo descargado inválido o demasiado pequeño"
        }
    }
    catch {
        Write-LogMessage "❌ Error instalando Ollama: $($_.Exception.Message)" -Level ERROR
        Write-LogMessage "" -Level INFO
        Write-LogMessage "📋 INSTALACIÓN MANUAL RECOMENDADA:" -Level INFO
        Write-LogMessage "    1. Visita: https://ollama.com/download" -Level INFO
        Write-LogMessage "    2. Descarga 'Download for Windows'" -Level INFO
        Write-LogMessage "    3. Ejecuta el instalador como administrador" -Level INFO
        Write-LogMessage "    4. Reinicia PowerShell y ejecuta: ollama pull phi3:mini" -Level INFO
        Write-LogMessage "" -Level INFO
        throw
    }
}

function Ensure-Phi4Mini-Model {
    Write-LogMessage "🧠 Verificando modelo Phi-4-Mini..." -Level INFO
    
    try {
        # Verificar si Ollama está ejecutándose
        $ollamaRunning = $false
        try {
            $response = Invoke-WebRequest -Uri "http://localhost:11434/api/version" -TimeoutSec 5 -ErrorAction SilentlyContinue
            if ($response.StatusCode -eq 200) {
                $ollamaRunning = $true
            }
        }
        catch {
            # Ollama no está ejecutándose
        }
        
        if (-not $ollamaRunning) {
            Write-LogMessage "    └─ Iniciando servicio Ollama..." -Level INFO
            
            # Intentar iniciar Ollama en background
            try {
                Start-Process -FilePath "ollama" -ArgumentList "serve" -WindowStyle Hidden -ErrorAction SilentlyContinue
                Start-Sleep -Seconds 3
            }
            catch {
                Write-LogMessage "⚠️  No se pudo iniciar Ollama automáticamente" -Level WARN
                return
            }
        }
        
        # Verificar si phi4-mini está disponible
        try {
            $models = & ollama list 2>$null
            if ($models -match "phi4.*mini" -or $models -match "phi3.*mini") {
                Write-LogMessage "✅ Modelo Phi Mini ya disponible" -Level SUCCESS
                return
            }
        }
        catch {
            Write-LogMessage "⚠️  No se pudo verificar modelos de Ollama" -Level WARN
            return
        }
        
        # Descargar phi3:mini como alternativa confiable
        Write-LogMessage "📥 Descargando modelo Phi-3-Mini (recomendado)..." -Level INFO
        Write-LogMessage "    Este proceso puede tardar varios minutos dependiendo de tu conexión" -Level INFO
        
        try {
            $pullProcess = Start-Process -FilePath "ollama" -ArgumentList "pull", "phi3:mini" -Wait -PassThru -NoNewWindow
            
            if ($pullProcess.ExitCode -eq 0) {
                Write-LogMessage "✅ Modelo Phi-3-Mini descargado exitosamente" -Level SUCCESS
                Write-LogMessage "    El chatbot ahora puede funcionar completamente offline" -Level SUCCESS
            } else {
                Write-LogMessage "⚠️  Descarga de modelo falló - se puede intentar manualmente" -Level WARN
                Write-LogMessage "    Comando: ollama pull phi3:mini" -Level INFO
            }
        }
        catch {
            Write-LogMessage "⚠️  Error descargando modelo: $($_.Exception.Message)" -Level WARN
            Write-LogMessage "    Puedes descargarlo manualmente: ollama pull phi3:mini" -Level INFO
        }
    }
    catch {
        Write-LogMessage "⚠️  Error configurando modelo Phi: $($_.Exception.Message)" -Level WARN
    }
}

function Update-SourceCode {
    if ($SkipGitPull) {
        Write-LogMessage "⏭️  Omitiendo actualización Git (parámetro -SkipGitPull)" -Level WARN
        return
    }
    
    Write-LogMessage "🔄 Verificando actualizaciones del repositorio Git..." -Level INFO
    
    # Verificar si Git está disponible
    try {
        $gitVersion = & git --version 2>$null
        if ($LASTEXITCODE -ne 0) {
            Write-LogMessage "⚠️  Git no disponible - usando código local" -Level WARN
            return
        }
    }
    catch {
        Write-LogMessage "⚠️  Git no disponible - usando código local" -Level WARN
        return
    }
    
    try {
        # Primero hacer fetch para ver si hay actualizaciones remotas
        Write-LogMessage "    └─ Verificando actualizaciones remotas..." -Level INFO
        & git fetch origin master 2>$null
        
        # Verificar si hay actualizaciones remotas disponibles
        $localCommit = & git rev-parse HEAD 2>$null
        $remoteCommit = & git rev-parse origin/master 2>$null
        
        if ($localCommit -eq $remoteCommit) {
            Write-LogMessage "✅ Repositorio ya está actualizado - preservando cambios locales" -Level SUCCESS
            return
        }
        
        # Verificar si hay cambios locales
        $gitStatus = & git status --porcelain 2>$null
        $hasLocalChanges = $gitStatus -ne $null -and $gitStatus.Length -gt 0
        
        if ($hasLocalChanges) {
            Write-LogMessage "📋 Hay cambios locales Y actualizaciones remotas disponibles" -Level INFO
            Write-LogMessage "    └─ Cambios locales detectados:" -Level INFO
            $gitStatus | ForEach-Object { Write-LogMessage "      • $_" -Level INFO }
            
            # Estrategia inteligente: intentar rebase automático
            Write-LogMessage "    └─ Intentando fusión inteligente con rebase..." -Level INFO
            & git stash push -m "Auto-stash para rebase $(Get-Date -Format 'yyyy-MM-dd HH:mm:ss')" 2>$null
            
            $pullResult = & git pull --rebase origin master 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-LogMessage "✅ Código actualizado exitosamente con rebase" -Level SUCCESS
                
                # Intentar restaurar cambios locales
                $stashList = & git stash list 2>$null
                if ($stashList -match "Auto-stash para rebase") {
                    Write-LogMessage "    └─ Restaurando cambios locales..." -Level INFO
                    $popResult = & git stash pop 2>&1
                    if ($LASTEXITCODE -eq 0) {
                        Write-LogMessage "✅ Cambios locales restaurados exitosamente" -Level SUCCESS
                    } else {
                        Write-LogMessage "⚠️  Conflictos detectados al restaurar cambios:" -Level WARN
                        Write-LogMessage "      $($popResult -join "`n")" -Level WARN
                        Write-LogMessage "    └─ Puedes resolver conflictos manualmente después" -Level INFO
                    }
                }
            } else {
                Write-LogMessage "❌ Error en rebase automático:" -Level ERROR
                Write-LogMessage "    $($pullResult -join "`n")" -Level ERROR
                
                # Restaurar stash en caso de error
                & git stash pop 2>$null
                Write-LogMessage "⚠️  Cambios locales restaurados - actualización omitida" -Level WARN
            }
        } else {
            # No hay cambios locales, pull directo
            Write-LogMessage "    └─ Sin cambios locales - actualizando directamente..." -Level INFO
            $pullResult = & git pull origin master 2>&1
            
            if ($LASTEXITCODE -eq 0) {
                Write-LogMessage "✅ Código fuente actualizado desde Git" -Level SUCCESS
            } else {
                Write-LogMessage "⚠️  Warning: No se pudo actualizar - $($pullResult -join "`n")" -Level WARN
            }
        }
    }
    catch {
        Write-LogMessage "⚠️  Error en operación Git: $($_.Exception.Message)" -Level WARN
        Write-LogMessage "    └─ Continuando con código local..." -Level INFO
    }
}

function Clear-BuildArtifacts {
    Write-LogMessage "🧹 Limpieza AGRESIVA de compilación y caché..." -Level INFO
    
    # STEP 1: Terminar TODOS los procesos relacionados
    Write-LogMessage "    └─ Terminando procesos .NET y aplicación..." -Level INFO
    Get-Process | Where-Object {
        $_.ProcessName -like "*ChatbotGomarco*" -or 
        $_.ProcessName -like "*dotnet*" -or
        $_.ProcessName -like "*MSBuild*"
    } | Stop-Process -Force -ErrorAction SilentlyContinue
    Start-Sleep -Seconds 2
    
    # STEP 2: Limpiar directorios de build
    $artifactDirs = @("bin", "obj")
    foreach ($dir in $artifactDirs) {
        if (Test-Path $dir) {
            Write-LogMessage "    └─ FORZANDO eliminación: $dir" -Level INFO
            Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
            Start-Sleep -Seconds 1
        }
    }
    
    # STEP 3: Limpiar caché de .NET y NuGet AGRESIVAMENTE
    $cacheDirs = @(
        "$env:USERPROFILE\.nuget\packages\.tools",
        "$env:LOCALAPPDATA\NuGet\Cache", 
        "$env:LOCALAPPDATA\Microsoft\VisualStudio\*\ComponentModelCache",
        "$env:TEMP\NuGetScratch",
        "$env:USERPROFILE\.dotnet\toolResolverCache",
        "$env:LOCALAPPDATA\Microsoft\dotnet\cache",
        "$env:TEMP\dotnet-*"
    )
    
    foreach ($cachePattern in $cacheDirs) {
        $matchingDirs = Get-ChildItem $cachePattern -ErrorAction SilentlyContinue
        foreach ($dir in $matchingDirs) {
            if (Test-Path $dir) {
                Write-LogMessage "    └─ Limpiando caché: $($dir.Name)" -Level INFO
                Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
            }
        }
    }
    
    # STEP 4: Limpiar caché específico de la aplicación
    $appDataPath = "$env:APPDATA\ChatbotGomarco"
    if (Test-Path $appDataPath) {
        Write-LogMessage "    └─ Limpiando configuraciones y caché de app..." -Level INFO
        Remove-Item "$appDataPath\*.tmp" -Force -ErrorAction SilentlyContinue
        Remove-Item "$appDataPath\cache" -Recurse -Force -ErrorAction SilentlyContinue
        Remove-Item "$appDataPath\*.cache" -Force -ErrorAction SilentlyContinue
    }
    
    # STEP 5: Limpiar archivos temporales del sistema
    @("$env:TEMP\ChatbotGomarco*", "$env:TEMP\*.tmp") | ForEach-Object {
        if (Test-Path $_) {
            Remove-Item $_ -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    
    Write-LogMessage "✅ Limpieza AGRESIVA completada - Forzando recompilación total" -Level SUCCESS
}

function Restore-Dependencies {
    Write-LogMessage "📦 Restaurando dependencias NuGet..." -Level INFO
    
    try {
        $restoreOutput = & dotnet restore $Config.SolutionFile 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-LogMessage "✅ Dependencias restauradas exitosamente" -Level SUCCESS
        } else {
            # Convertir restoreOutput a string para evitar errores de transformación de parámetros
            $restoreErrorString = if ($restoreOutput) { ($restoreOutput -join "`n") } else { "Sin detalles de error disponibles" }
            Write-LogMessage "❌ Error en restauración: $restoreErrorString" -Level ERROR
            throw "Falló la restauración de dependencias"
        }
    }
    catch {
        Write-LogMessage "❌ Error crítico en restauración de dependencias" -Level ERROR
        throw
    }
}

function Build-Application {
    Write-LogMessage "🔨 RECOMPILACIÓN FORZADA en modo $($Config.BuildConfig)..." -Level INFO
    
    try {
        # PASO 1: Clean build forzado
        Write-LogMessage "    └─ Ejecutando dotnet clean..." -Level INFO
        $cleanOutput = & dotnet clean $Config.SolutionFile -c $Config.BuildConfig 2>&1
        
        # PASO 2: Build con flags de forzado
        $buildArgs = @(
            "build"
            $Config.SolutionFile
            "-c", $Config.BuildConfig
            "--no-restore"
            "--force"              # CRÍTICO: Fuerza recompilación completa
            "--no-incremental"     # CRÍTICO: Evita build incremental
            "--verbosity", "normal"  # Más detalle para debug
        )
        
        Write-LogMessage "    └─ Ejecutando build completo SIN caché..." -Level INFO
        $buildOutput = & dotnet @buildArgs 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-LogMessage "✅ RECOMPILACIÓN FORZADA exitosa" -Level SUCCESS
            
            # Verificar que el executable se generó correctamente
            $exePath = "bin\$($Config.BuildConfig)\net8.0-windows\$($Config.ProjectName).exe"
            if (Test-Path $exePath) {
                $fileInfo = Get-Item $exePath
                Write-LogMessage "    └─ Ejecutable actualizado: $($fileInfo.LastWriteTime)" -Level SUCCESS
            }
        } else {
            Write-LogMessage "❌ Error en RECOMPILACIÓN FORZADA:" -Level ERROR
            # Convertir buildOutput a string para evitar errores de transformación de parámetros
            $buildErrorString = if ($buildOutput) { ($buildOutput -join "`n") } else { "Sin detalles de error disponibles" }
            Write-LogMessage $buildErrorString -Level ERROR
            throw "Falló la recompilación forzada"
        }
    }
    catch {
        Write-LogMessage "❌ Error crítico en recompilación forzada" -Level ERROR
        throw
    }
}

function Stop-ExistingProcesses {
    Write-LogMessage "⏹️  Verificando procesos existentes..." -Level INFO
    
    $processes = Get-Process -Name $Config.ProjectName -ErrorAction SilentlyContinue
    
    if ($processes) {
        Write-LogMessage "    └─ Deteniendo $($processes.Count) proceso(s) existente(s)..." -Level INFO
        $processes | Stop-Process -Force -ErrorAction SilentlyContinue
        Start-Sleep -Seconds 2
        Write-LogMessage "✅ Procesos anteriores detenidos" -Level SUCCESS
    } else {
        Write-LogMessage "✅ No hay procesos previos ejecutándose" -Level SUCCESS
    }
}

function Start-Application {
    Write-LogMessage "🚀 Iniciando aplicación actualizada..." -Level INFO
    
    try {
        # Método 1: Ejecutar desde proyecto
        Write-LogMessage "    └─ Método 1: Ejecutando desde proyecto..." -Level INFO
        
        $runArgs = @(
            "run"
            "--project", $Config.ProjectFile
            "--no-build"
        )
        
        Start-Process -FilePath "dotnet" -ArgumentList $runArgs -WindowStyle Hidden
        
        # Verificar que se inició correctamente
        Start-Sleep -Seconds 3
        
        $runningProcess = Get-Process -Name "dotnet" -ErrorAction SilentlyContinue | 
                         Where-Object { $_.ProcessName -eq "dotnet" }
        
        if ($runningProcess) {
            Write-LogMessage "✅ Aplicación iniciada correctamente (PID: $($runningProcess.Id))" -Level SUCCESS
            return $true
        } else {
            # Método 2: Ejecutar binario directo
            Write-LogMessage "    └─ Método 2: Ejecutando binario directo..." -Level INFO
            
            $exePath = "bin\$($Config.BuildConfig)\net8.0-windows\$($Config.ProjectName).exe"
            
            if (Test-Path $exePath) {
                Start-Process -FilePath $exePath
                Write-LogMessage "✅ Aplicación iniciada desde binario" -Level SUCCESS
                return $true
            } else {
                Write-LogMessage "❌ No se pudo encontrar el ejecutable" -Level ERROR
                return $false
            }
        }
    }
    catch {
        Write-LogMessage "❌ Error al iniciar aplicación: $($_.Exception.Message)" -Level ERROR
        return $false
    }
}

function Show-Summary {
    param([bool]$Success)
    
    Write-Host "`n" -NoNewline
    Write-Host "╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
    
    if ($Success) {
        Write-Host "║                     🎉 ACTUALIZACIÓN COMPLETA 🎉                ║" -ForegroundColor Green
    } else {
        Write-Host "║                     ❌ ACTUALIZACIÓN FALLIDA ❌                 ║" -ForegroundColor Red
    }
    
    Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
    
    Write-Host "`n📊 Resumen de la operación:" -ForegroundColor Cyan
    Write-Host "    • Proyecto: $($Config.ProjectName)" -ForegroundColor White
    Write-Host "    • Configuración: $($Config.BuildConfig)" -ForegroundColor White
    Write-Host "    • Timestamp: $(Get-Date)" -ForegroundColor White
    Write-Host "    • Log: $script:LogFile" -ForegroundColor White
    
    if ($Success) {
        Write-Host "`n💡 La aplicación debería estar ejecutándose ahora." -ForegroundColor Green
    } else {
        Write-Host "`n📋 Revisa el archivo de log para detalles del error." -ForegroundColor Yellow
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
        
        # Limpiar logs antiguos
        Get-ChildItem $Config.LogDirectory -Filter "deployment_*.log" | 
            Sort-Object LastWriteTime -Descending | 
            Select-Object -Skip $Config.MaxLogFiles | 
            Remove-Item -Force -ErrorAction SilentlyContinue
        
        # Header empresarial
        Write-Host "`n"
        Write-Host "╔══════════════════════════════════════════════════════════════════╗" -ForegroundColor Green
        Write-Host "║              🤖 CHATBOT GOMARCO - AUTO UPDATER 🤖              ║" -ForegroundColor Green
        Write-Host "║                PowerShell Enterprise DevOps Solution             ║" -ForegroundColor Green
        Write-Host "╚══════════════════════════════════════════════════════════════════╝" -ForegroundColor Green
        
        Write-LogMessage "=== INICIANDO PROCESO DE ACTUALIZACIÓN AUTOMÁTICA ===" -Level INFO
        
        # Pipeline de actualización
        Test-Prerequisites
        Update-SourceCode
        
        # Forzar limpieza para asegurar que el frontend se actualice correctamente
        Write-LogMessage "🔄 Forzando limpieza completa para actualización del frontend..." -Level INFO
        Clear-BuildArtifacts
        
        Restore-Dependencies
        Stop-ExistingProcesses
        Build-Application
        $appStarted = Start-Application
        
        Show-Summary -Success $appStarted
        
        Write-LogMessage "=== PROCESO COMPLETADO EXITOSAMENTE ===" -Level SUCCESS
        
    }
    catch {
        Write-LogMessage "=== ERROR CRÍTICO: $($_.Exception.Message) ===" -Level ERROR
        Show-Summary -Success $false
        throw
    }
}

# ====================================================================
# EJECUCIÓN
# ====================================================================
if ($MyInvocation.InvocationName -ne '.') {
    Main
} 