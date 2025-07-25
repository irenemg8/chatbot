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
    
    Write-LogMessage "✅ Todos los prerrequisitos verificados" -Level SUCCESS
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
    Write-LogMessage "🧹 Limpiando artefactos de compilación..." -Level INFO
    
    $artifactDirs = @("bin", "obj")
    
    foreach ($dir in $artifactDirs) {
        if (Test-Path $dir) {
            Write-LogMessage "    └─ Eliminando directorio: $dir" -Level INFO
            Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
    
    Write-LogMessage "✅ Artefactos eliminados exitosamente" -Level SUCCESS
}

function Restore-Dependencies {
    Write-LogMessage "📦 Restaurando dependencias NuGet..." -Level INFO
    
    try {
        $restoreOutput = & dotnet restore $Config.SolutionFile 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-LogMessage "✅ Dependencias restauradas exitosamente" -Level SUCCESS
        } else {
            Write-LogMessage "❌ Error en restauración: $restoreOutput" -Level ERROR
            throw "Falló la restauración de dependencias"
        }
    }
    catch {
        Write-LogMessage "❌ Error crítico en restauración de dependencias" -Level ERROR
        throw
    }
}

function Build-Application {
    Write-LogMessage "🔨 Compilando aplicación en modo $($Config.BuildConfig)..." -Level INFO
    
    try {
        $buildArgs = @(
            "build"
            $Config.SolutionFile
            "-c", $Config.BuildConfig
            "--no-restore"
            "--verbosity", "minimal"
        )
        
        $buildOutput = & dotnet @buildArgs 2>&1
        
        if ($LASTEXITCODE -eq 0) {
            Write-LogMessage "✅ Compilación exitosa" -Level SUCCESS
        } else {
            Write-LogMessage "❌ Error en compilación:" -Level ERROR
            Write-LogMessage $buildOutput -Level ERROR
            throw "Falló la compilación"
        }
    }
    catch {
        Write-LogMessage "❌ Error crítico en compilación" -Level ERROR
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
        
        if ($ForceRecompile) {
            Clear-BuildArtifacts
        }
        
        Restore-Dependencies
        Build-Application
        Stop-ExistingProcesses
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