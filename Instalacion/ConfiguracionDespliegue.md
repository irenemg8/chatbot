# Configuración de Despliegue Empresarial - Chatbot GOMARCO

## 📋 Preparación para Distribución

### 1. Compilación de Release

```bash
# Compilar aplicación en modo Release
dotnet publish ChatbotGomarco.csproj -c Release -r win-x64 --self-contained false

# Optimizar para tamaño
dotnet publish -c Release -r win-x64 --self-contained false -p:PublishTrimmed=true
```

### 2. Creación del Paquete de Instalación

```
ChatbotGomarco-Distribucion/
├── 📁 App/                           # Aplicación compilada
│   ├── ChatbotGomarco.exe
│   ├── ChatbotGomarco.dll
│   ├── *.dll (dependencias)
│   └── Resources/
├── 📁 Scripts/
│   ├── InstalarChatbotGomarco.bat    # Instalador automático
│   ├── DesinstalarSilencioso.bat    # Desinstalador
│   └── VerificarInstalacion.ps1     # Script de verificación
├── 📁 Documentacion/
│   ├── README.md                     # Documentación técnica
│   ├── Manual-Usuario.pdf            # Manual para empleados
│   └── Guia-TI.pdf                   # Guía para TI
└── 📄 LEEME.txt                      # Instrucciones básicas
```

## 🔧 Scripts de Despliegue Masivo

### Instalación Silenciosa (GPO/SCCM)

```batch
:: InstalarSilencioso.bat
@echo off
setlocal EnableDelayedExpansion

echo Iniciando instalacion silenciosa Chatbot GOMARCO...

:: Verificar permisos de administrador
net session >nul 2>&1
if %errorLevel% neq 0 (
    echo ERROR: Este script requiere permisos de administrador
    exit /b 1
)

:: Instalar .NET 8 Runtime si no existe
dotnet --version >nul 2>&1
if errorlevel 1 (
    echo Instalando .NET 8 Runtime...
    start /wait dotnet-runtime-installer.exe /quiet /norestart
)

:: Crear directorios
if not exist "%ProgramFiles%\GOMARCO" mkdir "%ProgramFiles%\GOMARCO"
if not exist "%ProgramFiles%\GOMARCO\ChatbotGomarco" mkdir "%ProgramFiles%\GOMARCO\ChatbotGomarco"

:: Copiar archivos
xcopy /E /I /Y /Q ".\App\*" "%ProgramFiles%\GOMARCO\ChatbotGomarco\"

:: Configurar permisos
icacls "%ProgramFiles%\GOMARCO\ChatbotGomarco" /grant "Domain Users":(OI)(CI)RX
icacls "%ProgramFiles%\GOMARCO\ChatbotGomarco" /grant "Authenticated Users":(OI)(CI)RX

:: Registrar en sistema
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ChatbotGomarco" /v "DisplayName" /t REG_SZ /d "Chatbot GOMARCO" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ChatbotGomarco" /v "Publisher" /t REG_SZ /d "GOMARCO" /f
reg add "HKLM\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ChatbotGomarco" /v "DisplayVersion" /t REG_SZ /d "1.0.0" /f

echo Instalacion completada exitosamente
exit /b 0
```

## 🤖 Configuración de Claude 4 API para Despliegue Empresarial

### Gestión Centralizada de API Keys

Para despliegues empresariales, se recomienda:

1. **Variable de Entorno (Recomendado)**
```batch
:: Configurar via GPO o script de despliegue
setx CLAUDE_API_KEY "sk-ant-api03-..." /M
```

2. **Archivo de Configuración Compartido**
```powershell
# Crear archivo de configuración en red compartida
$config = @{
    ApiKey = "sk-ant-api03-..."
    Model = "claude-sonnet-4-20250514"
    MaxTokens = 4096
}
$config | ConvertTo-Json | Out-File "\\servidor\configs\chatbot-claude.json"
```

3. **Configuración via Registry**
```batch
:: Establecer clave en registro (cifrada)
reg add "HKLM\SOFTWARE\GOMARCO\ChatbotGomarco" /v "ClaudeApiKey" /t REG_SZ /d "ENCRYPTED_KEY_HERE" /f
```

### Script de Configuración Automatizada

```powershell
# ConfigurarClaudeAPI.ps1
param(
    [Parameter(Mandatory=$true)]
    [string]$ApiKey,
    
    [string]$TargetComputers = "localhost"
)

# Función para configurar Claude API en equipos remotos
function Set-ClaudeAPIKey {
    param(
        [string]$ComputerName,
        [string]$ApiKey
    )
    
    Invoke-Command -ComputerName $ComputerName -ScriptBlock {
        param($key)
        
        # Crear variable de entorno del sistema
        [Environment]::SetEnvironmentVariable("CLAUDE_API_KEY", $key, "Machine")
        
        # Opcional: Guardar en registro cifrado
        $encrypted = ConvertTo-SecureString $key -AsPlainText -Force | ConvertFrom-SecureString
        New-ItemProperty -Path "HKLM:\SOFTWARE\GOMARCO\ChatbotGomarco" -Name "ClaudeApiKey" -Value $encrypted -Force
        
    } -ArgumentList $ApiKey
}

# Aplicar a todos los equipos
$computers = $TargetComputers -split ','
foreach ($computer in $computers) {
    Write-Host "Configurando Claude API en $computer..." -ForegroundColor Yellow
    Set-ClaudeAPIKey -ComputerName $computer -ApiKey $ApiKey
}
```

### Límites y Cuotas Empresariales

Para controlar el uso de la API:

```json
{
  "claude_config": {
    "api_key": "sk-ant-api03-...",
    "model": "claude-sonnet-4-20250514",
    "limits": {
      "max_requests_per_hour": 1000,
      "max_tokens_per_request": 4096,
      "max_cost_per_month": 500.00
    },
    "features": {
      "vision_enabled": true,
      "document_analysis": true,
      "excel_analysis": true,
      "pdf_deep_analysis": true
    }
  }
}
```

### Monitoreo de Uso

```powershell
# MonitorearUsoClaude.ps1
# Script para monitorear el uso de Claude API en la organización

$logPath = "\\servidor\logs\claude-usage"
$date = Get-Date -Format "yyyy-MM-dd"

# Recopilar métricas de uso
$metrics = @{
    Date = $date
    TotalRequests = 0
    TotalTokens = 0
    EstimatedCost = 0.0
    TopUsers = @()
}

# Análisis de logs...
# [Código de análisis aquí]

# Generar reporte
$metrics | ConvertTo-Json | Out-File "$logPath\usage-$date.json"
```

### Script de Verificación PowerShell

```powershell
# VerificarInstalacion.ps1
param(
    [switch]$Detailed,
    [switch]$FixIssues
)

Write-Host "=== Verificacion Chatbot GOMARCO ===" -ForegroundColor Green

# Verificar instalación
$appPath = "$env:ProgramFiles\GOMARCO\ChatbotGomarco\ChatbotGomarco.exe"
$installed = Test-Path $appPath

Write-Host "Aplicacion instalada: " -NoNewline
if ($installed) {
    Write-Host "SI" -ForegroundColor Green
    if ($Detailed) {
        $version = (Get-ItemProperty $appPath).VersionInfo.FileVersion
        Write-Host "Version: $version" -ForegroundColor Cyan
    }
} else {
    Write-Host "NO" -ForegroundColor Red
    if ($FixIssues) {
        Write-Host "Iniciando instalacion automatica..." -ForegroundColor Yellow
        & ".\InstalarSilencioso.bat"
    }
}

# Verificar .NET 8
Write-Host ".NET 8 Runtime: " -NoNewline
try {
    $dotnetVersion = & dotnet --version 2>$null
    if ($dotnetVersion -match "^8\.") {
        Write-Host "SI ($dotnetVersion)" -ForegroundColor Green
    } else {
        Write-Host "VERSION INCORRECTA ($dotnetVersion)" -ForegroundColor Yellow
    }
} catch {
    Write-Host "NO INSTALADO" -ForegroundColor Red
}

# Verificar permisos
Write-Host "Permisos de usuario: " -NoNewline
$canExecute = [System.IO.File]::Exists($appPath) -and (Get-Acl $appPath).Access.Where({$_.IdentityReference -match "Users|Authenticated Users"}).Count -gt 0
if ($canExecute) {
    Write-Host "OK" -ForegroundColor Green
} else {
    Write-Host "PROBLEMAS" -ForegroundColor Red
}

# Verificar registro
Write-Host "Registro del sistema: " -NoNewline
$regKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\ChatbotGomarco"
if (Test-Path $regKey) {
    Write-Host "OK" -ForegroundColor Green
} else {
    Write-Host "FALTA" -ForegroundColor Yellow
}

if ($Detailed) {
    Write-Host "`n=== Informacion Detallada ===" -ForegroundColor Cyan
    Write-Host "Ruta de instalacion: $appPath"
    Write-Host "Datos de usuario: $env:APPDATA\GOMARCO\ChatbotGomarco"
    Write-Host "Sistema operativo: $((Get-WmiObject Win32_OperatingSystem).Caption)"
    Write-Host "Arquitectura: $env:PROCESSOR_ARCHITECTURE"
}

Write-Host "`nVerificacion completada." -ForegroundColor Green
```

## 📦 Configuración de Group Policy (GPO)

### Distribución Automática via GPO

```xml
<!-- Configuración MSI/GPO -->
<Configuration>
  <Package>
    <Name>Chatbot GOMARCO</Name>
    <Version>1.0.0</Version>
    <InstallCommand>\\servidor\aplicaciones\ChatbotGomarco\InstalarSilencioso.bat</InstallCommand>
    <UninstallCommand>\\servidor\aplicaciones\ChatbotGomarco\DesinstalarSilencioso.bat</UninstallCommand>
    <Architecture>x64</Architecture>
    <RequiredOS>Windows 10, Windows 11</RequiredOS>
  </Package>
  
  <TargetGroups>
    <Group>CN=EmpleadosGomarco,OU=Usuarios,DC=gomarco,DC=local</Group>
    <Group>CN=Administrativos,OU=Grupos,DC=gomarco,DC=local</Group>
  </TargetGroups>
  
  <Prerequisites>
    <Runtime>.NET 8.0 Desktop Runtime</Runtime>
    <OS>Windows 10 1909+</OS>
    <Memory>4GB</Memory>
    <DiskSpace>500MB</DiskSpace>
  </Prerequisites>
</Configuration>
```

### Política de Registro

```reg
Windows Registry Editor Version 5.00

; Configuración corporativa para Chatbot GOMARCO
[HKEY_LOCAL_MACHINE\SOFTWARE\Policies\GOMARCO\ChatbotGomarco]
"AutoUpdate"=dword:00000001
"TelemetryEnabled"=dword:00000000
"MaxFileSize"=dword:06400000
"LogLevel"="Information"
"DataRetentionDays"=dword:00000090

; Configuración de usuario por defecto
[HKEY_CURRENT_USER\SOFTWARE\GOMARCO\ChatbotGomarco]
"FirstRun"=dword:00000001
"Theme"="Corporate"
"AutoCleanTemp"=dword:00000001
```

## 🔐 Configuración de Seguridad Empresarial

### Directorio de Datos Corporativo

```batch
:: Configurar directorio centralizado (opcional)
:: Para entornos con directorios de usuario en red

set CORPORATE_DATA_DIR=\\servidor\perfiles$\%USERNAME%\GOMARCO\ChatbotGomarco

if not exist "%CORPORATE_DATA_DIR%" (
    mkdir "%CORPORATE_DATA_DIR%"
    icacls "%CORPORATE_DATA_DIR%" /grant "%USERNAME%":(OI)(CI)M
)

:: Crear enlace simbólico
mklink /D "%APPDATA%\GOMARCO" "%CORPORATE_DATA_DIR%"
```

### Configuración de Antivirus

```xml
<!-- Exclusiones recomendadas para antivirus corporativo -->
<AntivirusExclusions>
  <Folders>
    <Path>C:\Program Files\GOMARCO\ChatbotGomarco\</Path>
    <Path>%APPDATA%\GOMARCO\ChatbotGomarco\</Path>
    <Path>%TEMP%\ChatbotGomarco\</Path>
  </Folders>
  
  <Processes>
    <Process>ChatbotGomarco.exe</Process>
    <Process>dotnet.exe</Process>
  </Processes>
  
  <Extensions>
    <Extension>.db</Extension>
    <Extension>.enc</Extension>
  </Extensions>
</AntivirusExclusions>
```

## 📊 Monitoreo y Logs Empresariales

### Configuración de Logs Centralizados

```csharp
// Configuración para envío a servidor de logs (opcional)
// Agregar en App.xaml.cs

services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.AddFile("Logs/chatbot-{Date}.log");
    
    // Para envío a servidor centralizado (opcional)
    if (Configuration.GetValue<bool>("EnableCentralLogging"))
    {
        builder.AddEventLog(new EventLogSettings
        {
            SourceName = "ChatbotGomarco",
            LogName = "Application"
        });
    }
});
```

### Script de Monitoreo

```powershell
# MonitoreoUso.ps1
# Script para ejecutar periódicamente y recopilar estadísticas

$logPath = "$env:APPDATA\GOMARCO\ChatbotGomarco\Logs"
$reportPath = "\\servidor\reportes\ChatbotGomarco"

# Recopilar estadísticas de uso
$stats = @{
    Usuario = $env:USERNAME
    Equipo = $env:COMPUTERNAME
    FechaReporte = Get-Date
    VersionApp = (Get-ItemProperty "$env:ProgramFiles\GOMARCO\ChatbotGomarco\ChatbotGomarco.exe").VersionInfo.FileVersion
    TamañoBaseDatos = (Get-Item "$env:APPDATA\GOMARCO\ChatbotGomarco\chatbot.db").Length
    UltimoUso = (Get-ChildItem $logPath -Name "chatbot-*.log" | Sort-Object LastWriteTime -Descending | Select-Object -First 1)
}

# Exportar a CSV para análisis
$stats | Export-Csv "$reportPath\uso-$env:USERNAME-$(Get-Date -Format 'yyyyMMdd').csv" -NoTypeInformation
```

## 🚀 Lista de Verificación para Despliegue

### Pre-Despliegue

- [ ] ✅ Aplicación compilada en modo Release
- [ ] ✅ Dependencias verificadas (.NET 8, Visual C++ Redistributable)
- [ ] ✅ Scripts de instalación probados en entorno de testing
- [ ] ✅ Documentación actualizada y distribuida
- [ ] ✅ Exclusiones de antivirus configuradas
- [ ] ✅ Permisos de red y directorios verificados
- [ ] ✅ Políticas de grupo preparadas (si aplica)
- [ ] ✅ Plan de rollback definido

### Despliegue

- [ ] 🔄 Notificación a usuarios finales
- [ ] 🔄 Instalación en grupo piloto (10-20 usuarios)
- [ ] 🔄 Verificación de funcionamiento en piloto
- [ ] 🔄 Resolución de issues identificados
- [ ] 🔄 Despliegue gradual por departamentos
- [ ] 🔄 Monitoreo de logs y errores
- [ ] 🔄 Soporte activo durante primera semana

### Post-Despliegue

- [ ] ⏳ Recopilación de feedback de usuarios
- [ ] ⏳ Análisis de estadísticas de uso
- [ ] ⏳ Documentación de lecciones aprendidas
- [ ] ⏳ Planificación de actualizaciones futuras
- [ ] ⏳ Capacitación adicional si es necesaria
- [ ] ⏳ Evaluación de impacto en productividad

## 📞 Contactos para Despliegue

| Rol | Responsable | Email | Teléfono |
|-----|-------------|-------|----------|
| **Administrador del Proyecto** | [Nombre] | admin.proyecto@gomarco.com | Ext. 1001 |
| **Líder Técnico** | [Nombre] | lider.tecnico@gomarco.com | Ext. 1002 |
| **Soporte de TI** | [Nombre] | soporte.ti@gomarco.com | Ext. 1234 |
| **Gestión de Cambios** | [Nombre] | cambios@gomarco.com | Ext. 1003 |

---

**Nota**: Este documento debe ser revisado y actualizado antes de cada despliegue mayor. 