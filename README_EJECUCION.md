# 🚀 **GUÍA DE EJECUCIÓN - CHATBOT GOMARCO**

## **Ejecutar la Aplicación de Forma Automática y Profesional**

Esta guía te explica cómo ejecutar correctamente el Chatbot GOMARCO utilizando los scripts de automatización enterprise creados para optimizar tu workflow de desarrollo.

---

## 📋 **REQUISITOS PREVIOS**

Antes de ejecutar cualquier script, verifica que tengas:

- ✅ **Windows 10/11** (64-bit)
- ✅ **.NET 8 SDK** instalado
- ✅ **PowerShell** (para scripts avanzados)
- ✅ Permisos de **administrador** (para instalaciones)

---

## 🎯 **MÉTODOS DE EJECUCIÓN**

### **MÉTODO 1: Script Automático Básico (Recomendado)**

**Archivo:** `ActualizarYEjecutar.bat`

```batch
# Ejecutar desde el directorio del proyecto
.\ActualizarYEjecutar.bat
```

**✨ ¿Qué hace este script?**
1. 🔍 **Verifica** que .NET 8 SDK esté instalado
2. 🔄 **Sincroniza** código desde Git (si está disponible)
3. 🧹 **Limpia** archivos de compilación anteriores
4. 📦 **Restaura** dependencias NuGet automáticamente
5. 🔨 **Compila** la aplicación en modo Debug
6. ⏹️ **Detiene** procesos anteriores si existen
7. 🚀 **Ejecuta** la aplicación actualizada
8. 📊 **Genera** logs detallados del proceso

---

### **MÉTODO 2: Script PowerShell Avanzado (Enterprise)**

**Archivo:** `ActualizarYEjecutar.ps1`

#### **Ejecución Básica:**
```powershell
.\ActualizarYEjecutar.ps1
```

#### **Ejecución con Parámetros Avanzados:**
```powershell
# Compilar en modo Release (producción)
.\ActualizarYEjecutar.ps1 -ConfigurationType Release

# Omitir sincronización Git
.\ActualizarYEjecutar.ps1 -SkipGitPull

# Forzar recompilación completa
.\ActualizarYEjecutar.ps1 -ForceRecompile

# Combinación para producción
.\ActualizarYEjecutar.ps1 -ConfigurationType Release -ForceRecompile -Verbose
```

**✨ Características Empresariales del Script PowerShell:**
- 🎛️ **Parámetros configurables** para diferentes escenarios
- 📊 **Logging estructurado** con códigos de color
- 🔄 **Rotación automática** de logs antiguos
- ⚡ **Validaciones robustas** de prerrequisitos
- 🛡️ **Manejo avanzado** de errores y excepciones
- 📈 **Reportes detallados** del proceso de deployment

---

### **MÉTODO 3: Ejecución Manual (Para Desarrollo)**

Si prefieres ejecutar manualmente paso a paso:

```batch
# 1. Restaurar dependencias
dotnet restore chatbot.sln

# 2. Compilar aplicación
dotnet build chatbot.sln -c Debug

# 3. Ejecutar aplicación
dotnet run --project ChatbotGomarco.csproj
```

---

## 🔧 **CONFIGURACIÓN INICIAL DE LA APLICACIÓN**

### **Paso 1: Ejecutar la Aplicación**
Una vez que la aplicación esté corriendo, verás la interfaz principal del chatbot.

### **Paso 2: Configurar Claude 4 IA (Opcional pero Recomendado)**
1. Haz clic en el **botón de configuración** (⚙️) en la parte superior derecha
2. Se abrirá un diálogo para ingresar tu API Key de Anthropic Claude
3. Ingresa tu clave API (formato: `sk-ant-api03-...`)
4. La IA avanzada se activará automáticamente

**📝 Nota:** La API Key solo se mantiene durante la sesión actual por seguridad.

### **Paso 3: ¡Comenzar a Usar!**
- 💬 Escribe mensajes en el campo de texto inferior
- 📎 Carga documentos usando el panel derecho
- 🗂️ Revisa el historial de conversaciones en el panel izquierdo

---

## 📊 **MONITORING Y LOGS**

### **Ubicaciones de Logs:**
```
📁 Proyecto/
├── 📁 logs/                          # Logs de los scripts de deployment
│   ├── deployment_YYYYMMDD_HHMMSS.log
│   └── deployment_*.log
└── 📁 %APPDATA%/GOMARCO/ChatbotGomarco/Logs/    # Logs de la aplicación
    ├── chatbot-YYYY-MM-DD.log
    └── errors.log
```

### **Verificar Estado de la Aplicación:**
```powershell
# Ver procesos ejecutándose
tasklist | findstr "ChatbotGomarco\|dotnet"

# Ver últimos logs
Get-Content "logs\deployment_*.log" -Tail 20
```

---

## ⚡ **SOLUCIÓN RÁPIDA DE PROBLEMAS**

### **❌ Error: "Archivo de proyecto no encontrado"**
**Solución:** Asegúrate de ejecutar los scripts desde el directorio raíz del proyecto donde están `chatbot.sln` y `ChatbotGomarco.csproj`.

### **❌ Error: ".NET 8 SDK no está instalado"**
**Solución:** 
1. Descarga .NET 8 SDK desde: https://dotnet.microsoft.com/download/dotnet/8.0
2. Instala y reinicia
3. Vuelve a ejecutar el script

### **❌ Error: "Acceso denegado" al limpiar archivos**
**Solución:** 
1. Cierra la aplicación si está ejecutándose
2. Ejecuta: `taskkill /IM "ChatbotGomarco.exe" /F`
3. Vuelve a ejecutar el script

### **❌ Error: "Git no disponible"**
**Solución:** Esto es solo una advertencia. El script continuará usando el código local disponible.

### **❌ La aplicación no se inicia**
**Solución:**
1. Revisa los logs en `logs/deployment_*.log`
2. Verifica que la compilación fue exitosa
3. Ejecuta manualmente: `dotnet run --project ChatbotGomarco.csproj`

---

## 🎯 **WORKFLOW RECOMENDADO PARA DESARROLLO**

### **Desarrollo Diario:**
```batch
# Ejecución rápida con cambios recientes
.\ActualizarYEjecutar.bat
```

### **Testing de Features:**
```powershell
# Compilación limpia con logs detallados
.\ActualizarYEjecutar.ps1 -ForceRecompile -Verbose
```

### **Preparación para Release:**
```powershell
# Build optimizado para producción
.\ActualizarYEjecutar.ps1 -ConfigurationType Release -ForceRecompile
```

---

## 📞 **SOPORTE Y CONTACTO**

Si encuentras problemas que no se resuelven con esta guía:

1. **Revisa los logs** en los directorios indicados
2. **Verifica prerrequisitos** (.NET 8 SDK, permisos)
3. **Ejecuta diagnósticos** con el parámetro `-Verbose`

---

## 🎉 **¡LISTO PARA USAR!**

Con estos scripts automatizados, tienes un workflow de desarrollo profesional que:
- ✅ **Automatiza** todo el proceso de compilación y ejecución
- ✅ **Mantiene** logs detallados para debugging
- ✅ **Gestiona** dependencias automáticamente
- ✅ **Optimiza** el tiempo de desarrollo
- ✅ **Garantiza** consistency en el deployment

**¡Ejecuta y disfruta de tu Chatbot GOMARCO con IA Claude 4 integrada!** 🤖✨ 