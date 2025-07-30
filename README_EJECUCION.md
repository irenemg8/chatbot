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
2. 🧠 **NUEVO: Configura Ollama automáticamente** para DeepSeek y Claude-Style
3. 🔄 **Sincroniza** código desde Git (si está disponible)
4. 🧹 **Limpia** archivos de compilación anteriores
5. 📦 **Restaura** dependencias NuGet automáticamente
6. 🔨 **Compila** la aplicación en modo Debug
7. ⏹️ **Detiene** procesos anteriores si existen
8. 🚀 **Ejecuta** la aplicación con todos los proveedores de IA listos
9. 📊 **Genera** logs detallados del proceso

**🆕 NUEVA FUNCIONALIDAD - Configuración Automática de IA:**
- Detecta si Ollama está instalado
- Si no está instalado, abre la página de descarga automáticamente
- Configura modelos locales: DeepSeek-R1, Claude-Style, Phi-3-Mini
- ¡Todo listo para usar 4 proveedores de IA diferentes!

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
- 🧠 **Git Inteligente**: Preserva automáticamente cambios locales sin `git stash` innecesario
- 🔍 **Detección Remota**: Solo actualiza cuando hay cambios reales en el repositorio
- 🔄 **Rebase Automático**: Fusiona cambios automáticamente cuando es posible
- 🎛️ **Parámetros configurables** para diferentes escenarios  
- 📊 **Logging estructurado** con códigos de color
- 🔄 **Rotación automática** de logs antiguos
- ⚡ **Validaciones robustas** de prerrequisitos
- 🛡️ **Manejo avanzado** de errores y excepciones
- 📈 **Reportes detallados** del proceso de deployment

**🔧 Gestión Inteligente de Git:**
```
✅ Sin actualizaciones remotas → Preserva cambios locales
✅ Con actualizaciones remotas + sin cambios locales → Actualiza directamente  
✅ Con actualizaciones remotas + cambios locales → Rebase inteligente
```

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

### **Paso 2: Seleccionar tu Proveedor de IA** 🆕
Ahora tienes **4 opciones** de inteligencia artificial:

#### **🚀 Opción Automática (Recomendada para Principiantes):**
1. Si ejecutaste `ActualizarYEjecutar.bat`, **¡todo ya está configurado!**
2. Haz clic en ⚙️ **Configuración** 
3. Verás 4 proveedores disponibles:
   - **🧠 DeepSeek-R1 7B** (Razonamiento avanzado - Local)
   - **💬 Claude-Style Llama** (Conversaciones naturales - Local)  
   - **⚡ Ollama (Phi-3-Mini)** (Consultas rápidas - Local)
   - **🌐 OpenAI GPT-4** (Máximo rendimiento - Requiere API Key)

#### **🎯 ¿Cuál elegir para empezar?**
```
👥 PARA PRINCIPIANTES → 🧠 DeepSeek-R1 7B
   - Análisis inteligente y razonamiento paso a paso
   - 100% local y gratuito
   - Excelente para análisis de documentos

💼 PARA COMUNICACIÓN → 💬 Claude-Style Llama  
   - Conversaciones naturales y redacción
   - 100% local y gratuito
   - Perfecto para emails y presentaciones

⚡ PARA VELOCIDAD → ⚡ Ollama (Phi-3-Mini)
   - Respuestas rápidas a consultas simples
   - 100% local y gratuito
   - Ideal para búsquedas rápidas

🚀 PARA MÁXIMO PODER → 🌐 OpenAI GPT-4
   - La IA más avanzada disponible
   - Requiere API Key (de pago)
   - Mejor para análisis de imágenes complejas
```

#### **🔧 Configuración de OpenAI GPT-4 (Opcional):**
1. Obtén tu API Key en [platform.openai.com](https://platform.openai.com/api-keys)
2. Haz clic en ⚙️ **Configuración**
3. Selecciona **"OpenAI GPT-4"**
4. Ingresa tu API Key (formato: `sk-...`)
5. Haz clic en **"Activar OpenAI GPT-4"**

**📝 Nota:** Las API Keys se mantienen solo durante la sesión actual por seguridad.

### **Paso 3: Probar tu Configuración**
1. **Selecciona un proveedor**: Haz clic en ⚙️ y elige tu IA preferida
2. **Haz una pregunta de prueba**: "¿Cómo estás?" o "Explícame qué puedes hacer"
3. **¡Verifica que funciona!**: Deberías recibir una respuesta en 5-30 segundos

### **Paso 4: ¡Comenzar a Usar!**
- 💬 Escribe mensajes en el campo de texto inferior
- 📎 Carga documentos usando el panel derecho (PDF, Word, Excel, etc.)
- 🗂️ Revisa el historial de conversaciones en el panel izquierdo
- ⚙️ Cambia de proveedor según la tarea que necesites realizar

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

### **❌ "Error configurando DeepSeek" o "Claude no disponible"** 🆕
**Problema:** Los modelos locales no están instalados correctamente.
**Solución:**
1. Verifica que Ollama esté instalado: `ollama --version`
2. Si no está instalado, ejecuta: `.\ActualizarYEjecutar.bat` (instala automáticamente)
3. Verifica modelos disponibles: `ollama list`
4. Si faltan modelos, instálalos manualmente:
   ```bash
   ollama pull deepseek-r1:7b        # Para DeepSeek-R1
   ollama pull llama3.1-claude       # Para Claude-Style  
   ollama pull phi3:mini             # Para Phi-3-Mini
   ```

### **❌ "Proveedor 'deepseek' no está registrado"** 🆕
**Problema:** El código no incluye los nuevos proveedores.
**Solución:**
1. Asegúrate de tener la versión más reciente del código
2. Ejecuta: `git pull` (si usas Git)
3. Recompila: `.\ActualizarYEjecutar.bat`
4. Si persiste, revisa que estos archivos existan:
   - `Servicios/ServicioDeepSeek.cs`
   - `Servicios/ServicioClaude.cs`

### **❌ Ollama consume mucha RAM** 🆕
**Problema:** Los modelos locales usan 4-8GB de RAM.
**Solución:**
1. **Para equipos con menos de 8GB**: Usa solo Ollama (Phi-3-Mini)
2. **Para liberar memoria**: Cierra otras aplicaciones pesadas
3. **Alternativa**: Usa OpenAI GPT-4 (requiere internet pero menos RAM local)

### **❌ Los modelos se descargan muy lento** 🆕
**Problema:** Los modelos son archivos grandes (2-5GB cada uno).
**Solución:**
1. **Paciencia**: DeepSeek-R1 es 4.7GB, puede tomar 10-30 minutos
2. **Conexión estable**: Evita interrumpir la descarga
3. **Verificar progreso**: `ollama list` muestra modelos descargados
4. **Solo lo necesario**: Empieza con Phi-3-Mini (2.2GB, más rápido)

---

## 🎯 **WORKFLOW RECOMENDADO PARA DESARROLLO**

### **🚀 Primera Vez (Setup Completo):**
```batch
# Configuración completa automática
.\ActualizarYEjecutar.bat

# Esto configurará:
# ✅ .NET 8 SDK
# ✅ Ollama + Modelos locales (DeepSeek, Claude-Style, Phi-3)
# ✅ Compilación y ejecución
# ✅ ¡Todo listo para usar 4 proveedores de IA!
```

### **💻 Desarrollo Diario:**
```batch
# Ejecución rápida con cambios recientes
.\ActualizarYEjecutar.bat

# Prueba rápida de funcionamiento:
# 1. Abrir aplicación
# 2. ⚙️ Configuración → DeepSeek-R1 7B → Activar
# 3. Pregunta de prueba: "Explícame paso a paso cómo funcionas"
```

### **🧪 Testing de Features:**
```powershell
# Compilación limpia con logs detallados
.\ActualizarYEjecutar.ps1 -ForceRecompile -Verbose

# Testing de proveedores de IA:
# 1. Probar cada proveedor individualmente
# 2. Verificar que el cambio entre proveedores funciona
# 3. Probar con diferentes tipos de documentos
```

### **📦 Preparación para Release:**
```powershell
# Build optimizado para producción
.\ActualizarYEjecutar.ps1 -ConfigurationType Release -ForceRecompile

# Checklist pre-release:
# ✅ Todos los proveedores de IA funcionan
# ✅ Ollama se instala automáticamente
# ✅ Modelos se descargan correctamente
# ✅ Interfaz de configuración responde
# ✅ Documentación actualizada
```

### **🆕 Workflow para Nuevos Desarrolladores:**
```batch
# Paso 1: Clonar repositorio
git clone [repo-url]
cd chatbot

# Paso 2: Setup automático (¡una sola línea!)
.\ActualizarYEjecutar.bat

# Paso 3: Verificar que todo funciona
# - La aplicación se abre automáticamente
# - Configuración muestra 4 proveedores
# - DeepSeek-R1 funciona sin configuración adicional

# ¡El nuevo desarrollador está listo en minutos!
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
- ✅ **Configura automáticamente** 4 proveedores de IA diferentes
- ✅ **Instala Ollama** y modelos locales sin intervención manual
- ✅ **Mantiene** logs detallados para debugging
- ✅ **Gestiona** dependencias automáticamente
- ✅ **Optimiza** el tiempo de desarrollo
- ✅ **Garantiza** consistency en el deployment

**🎉 NUEVAS CAPACIDADES:**
- 🧠 **DeepSeek-R1 7B**: Razonamiento avanzado local
- 💬 **Claude-Style Llama**: Conversaciones naturales locales  
- ⚡ **Ollama (Phi-3-Mini)**: Consultas rápidas locales
- 🌐 **OpenAI GPT-4**: Máximo rendimiento en la nube

**¡Ejecuta y disfruta de tu Chatbot GOMARCO con Sistema Multi-Proveedor de IA!** 🤖✨

<div align="center">
  <strong>🚀 Versión 2.0.0 - Multi-Provider AI System</strong><br>
  <em>Una aplicación, cuatro inteligencias artificiales</em>
</div> 