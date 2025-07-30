# 🤖 Chatbot GOMARCO - Asistente Empresarial Inteligente

## 🌟 Características Principales

### 🧠 **MÚLTIPLES PROVEEDORES DE IA** - Elige la mejor opción para cada tarea
- **🌐 OpenAI GPT-4**: El modelo más potente en la nube (requiere API Key)
- **🧠 DeepSeek-R1 7B**: Razonamiento avanzado 100% local (recomendado para análisis complejos)
- **💬 Claude-Style Llama**: Conversaciones naturales locales (estilo Anthropic)
- **⚡ Phi-3-Mini**: Modelo local rápido y eficiente (Microsoft)

### 📄 Análisis Avanzado de Documentos
- **Análisis Profundo Multi-IA**: Usa diferentes modelos según la complejidad del análisis
- **Visión Computacional**: Análisis completo de imágenes, extracción de texto, tablas y datos
- **PDFs**: Análisis página por página con resúmenes ejecutivos y extracción de datos clave
- **Word/Excel**: Identificación de estructura, tablas, tendencias y análisis estadístico
- **Extracción Inteligente**: Comprensión completa del contenido con diferentes especializaciones

### 🔐 Seguridad y Privacidad EMPRESARIAL
- **100% Local**: Todos los archivos se procesan y almacenan localmente
- **Encriptación AES**: Datos sensibles protegidos con encriptación de grado militar
- **Modelos Locales**: DeepSeek, Claude-Style y Phi funcionan completamente offline
- **Sin Datos en la Nube**: Tu información nunca sale de tu equipo (excepto si eliges OpenAI)

### 💼 Optimizado para Empresas
- **Multi-formato**: Soporta PDF, Word, Excel, PowerPoint, imágenes y más
- **Búsqueda Inteligente**: Encuentra información específica en todos tus documentos
- **Historial Completo**: Guarda y recupera conversaciones anteriores
- **Interfaz Profesional**: Diseño moderno con Material Design
- **Configuración Empresarial**: Administración centralizada de proveedores de IA

### 🚀 Tecnología de Vanguardia
- **Sistema Multi-Proveedor**: Arquitectura modular que soporta múltiples modelos de IA
- **Ollama Integration**: Backend local para modelos avanzados como DeepSeek-R1
- **OCR Avanzado**: Extracción de texto de imágenes y documentos escaneados
- **Procesamiento Natural del Lenguaje**: Comprende el contexto con diferentes especializaciones
- **Análisis de Metadatos**: Extrae información técnica y estructural

## 📋 Requisitos del Sistema

### Requisitos Mínimos
- **Sistema Operativo**: Windows 10/11 (64-bit)
- **Framework**: .NET 8.0 Runtime
- **RAM**: 4 GB mínimo (8 GB recomendado)
- **Almacenamiento**: 500 MB para la aplicación + espacio para archivos
- **Procesador**: Intel Core i3 o equivalente

## 🚀 Instalación

### Opción 1: Instalador Automático
1. Descarga el instalador desde la carpeta `Instalacion/`
2. Ejecuta `InstalarChatbotGomarco.bat` como administrador
3. Sigue las instrucciones en pantalla

### Opción 2: Instalación Manual
1. Instala .NET 8.0 Runtime desde [Microsoft](https://dotnet.microsoft.com/download/dotnet/8.0)
2. Copia la carpeta `App/` a `C:\Program Files\GOMARCO\ChatbotGomarco\`
3. Crea un acceso directo del ejecutable en el escritorio

## ⚙️ Configuración de Proveedores de IA

### 🚀 **CONFIGURACIÓN AUTOMÁTICA** (Recomendado para Principiantes)
Al ejecutar `ActualizarYEjecutar.bat`, el sistema:
1. **Detecta automáticamente** si Ollama está instalado
2. **Descarga e instala Ollama** si no está presente
3. **Configura modelos locales** (DeepSeek-R1, Claude-Style, Phi-3)
4. **¡Todo listo para usar!** - Sin configuración manual

### 🧠 **OPCIONES DE IA DISPONIBLES**

#### **1. 🌐 OpenAI GPT-4** (En la Nube - Requiere API Key)
- **Cuándo usar**: Análisis más complejos y tareas que requieren máximo rendimiento
- **Configuración**:
  1. Obtén tu API Key en [OpenAI](https://platform.openai.com/api-keys)
  2. Abre Chatbot GOMARCO → ⚙️ **Configuración**
  3. Selecciona **"OpenAI GPT-4"**
  4. Ingresa tu API Key y haz clic en **"Activar"**

#### **2. 🧠 DeepSeek-R1 7B** (Local - Recomendado)
- **Cuándo usar**: Análisis lógico, razonamiento complejo, resolución de problemas
- **Configuración**: ¡Completamente automática!
  1. Ejecuta `ActualizarYEjecutar.bat` (instala Ollama automáticamente)
  2. Abre Chatbot GOMARCO → ⚙️ **Configuración**
  3. Selecciona **"DeepSeek-R1 7B"**
  4. Haz clic en **"Activar DeepSeek-R1 7B"**

#### **3. 💬 Claude-Style Llama** (Local - Conversacional)
- **Cuándo usar**: Conversaciones naturales, escritura creativa, análisis conversacional
- **Configuración**: ¡Completamente automática!
  1. Ejecuta `ActualizarYEjecutar.bat` (instala Ollama automáticamente)
  2. Abre Chatbot GOMARCO → ⚙️ **Configuración**
  3. Selecciona **"Claude-Style Llama"**
  4. Haz clic en **"Activar Claude-Style"**

#### **4. ⚡ Ollama (Phi-3-Mini)** (Local - Rápido)
- **Cuándo usar**: Consultas rápidas, tareas simples, cuando necesitas velocidad
- **Configuración**: ¡Completamente automática!
  1. Ejecuta `ActualizarYEjecutar.bat` (instala Ollama automáticamente)
  2. Abre Chatbot GOMARCO → ⚙️ **Configuración**
  3. Selecciona **"Ollama"**
  4. Haz clic en **"Activar Ollama"**

### 🔧 **Configuración Manual de Ollama** (Solo si la automática falla)
1. Descarga Ollama desde [ollama.com](https://ollama.com/download/windows)
2. Instala el programa
3. Abre terminal y ejecuta:
   ```bash
   ollama pull deepseek-r1:7b        # Para DeepSeek-R1
   ollama pull llama3.1-claude       # Para Claude-Style
   ollama pull phi3:mini             # Para Phi-3-Mini
   ```

### Primera Ejecución
1. Al iniciar por primera vez, se creará la estructura de carpetas necesaria
2. La base de datos SQLite se inicializará automáticamente
3. Los archivos de configuración se generarán en `%APPDATA%\GOMARCO\ChatbotGomarco\`

## 📖 Uso de la Aplicación

### 🎯 **¿QUÉ IA USAR PARA CADA TAREA?**

#### **Para Razonamiento Complejo y Análisis Lógico** → 🧠 **DeepSeek-R1 7B**
```
✅ Ideal para:
• Análisis de problemas complejos paso a paso
• Resolución de problemas matemáticos o lógicos  
• Análisis de contratos y documentos legales
• Detección de inconsistencias en datos
• Razonamiento causal y análisis de riesgos

Ejemplo: "Analiza este contrato y identifica todos los riesgos legales potenciales"
```

#### **Para Conversaciones Naturales y Escritura** → 💬 **Claude-Style Llama**
```
✅ Ideal para:
• Conversaciones naturales y fluidas
• Escritura creativa y redacción de documentos
• Análisis de textos con matices emocionales
• Asistencia en comunicaciones empresariales
• Generación de contenido amigable

Ejemplo: "Ayúdame a redactar un email profesional pero amigable"
```

#### **Para Máximo Rendimiento** → 🌐 **OpenAI GPT-4**
```
✅ Ideal para:
• Análisis de imágenes complejas (facturas, gráficos)
• Tareas que requieren el máximo nivel de precisión
• Análisis de documentos de más de 100 páginas
• Traducciones especializadas
• Análisis financieros complejos

Ejemplo: "Analiza esta imagen de un gráfico financiero complejo"
```

#### **Para Consultas Rápidas** → ⚡ **Ollama (Phi-3-Mini)**
```
✅ Ideal para:
• Consultas rápidas y simples
• Búsquedas en documentos
• Resúmenes básicos
• Cuando necesitas velocidad sobre precisión

Ejemplo: "¿Cuál es la fecha de vencimiento en este documento?"
```

### 📄 **Cómo Usar Cada Proveedor**

#### **Paso 1: Seleccionar el Proveedor**
1. Haz clic en ⚙️ **Configuración**
2. Selecciona el proveedor ideal para tu tarea
3. Haz clic en **"Activar [Proveedor]"**
4. La ventana se cerrará automáticamente

#### **Paso 2: Cargar Archivos**
- Haz clic en "📎 Cargar archivos" o arrastra y suelta
- Formatos soportados: PDF, Word, Excel, PowerPoint, imágenes, texto

#### **Paso 3: Hacer Preguntas**
- Escribe tu pregunta en el campo de texto
- El proveedor seleccionado analizará el contenido
- Recibirás respuestas optimizadas según el proveedor elegido

### 🔥 **Características Avanzadas por Proveedor**

#### **🧠 DeepSeek-R1**: Razonamiento Paso a Paso
- **Análisis Lógico**: "Explica paso a paso por qué esta decisión es correcta"
- **Detección de Problemas**: "Identifica todas las inconsistencias en estos datos"
- **Análisis Causal**: "¿Qué factores causaron esta tendencia?"

#### **💬 Claude-Style**: Conversación Natural
- **Comunicación**: "Ayúdame a explicar este concepto de manera simple"
- **Redacción**: "Escribe un resumen ejecutivo amigable de este reporte"
- **Contexto Social**: "¿Cómo debería presentar esta información al equipo?"

#### **🌐 OpenAI GPT-4**: Máxima Potencia
- **Análisis Visual**: "Analiza todos los elementos visuales de esta imagen"
- **Documentos Masivos**: "Resume este documento de 200 páginas"
- **Análisis Multimodal**: "Combina la información de estos 5 documentos diferentes"

#### **⚡ Ollama**: Velocidad Local
- **Búsqueda Rápida**: "busca información sobre..."
- **Extracción Simple**: "Extrae todas las fechas de este documento"
- **Resúmenes Básicos**: "Resume este documento en 3 puntos"

## 🔒 Seguridad Empresarial

### 🛡️ **Seguridad por Proveedor de IA**
- **🧠 DeepSeek-R1**: **100% Local** - Tus datos nunca salen de tu equipo
- **💬 Claude-Style**: **100% Local** - Procesamiento completamente offline  
- **⚡ Ollama (Phi-3)**: **100% Local** - Máxima privacidad garantizada
- **🌐 OpenAI GPT-4**: **En la nube** - Solo si eliges usarlo (opcional)

### 🔐 **Protección de Datos**
- **Encriptación AES-256**: Todos los archivos se encriptan con grado militar
- **Claves únicas**: Cada instalación tiene sus propias claves de encriptación
- **Auto-limpieza**: Los archivos temporales se eliminan automáticamente
- **API Keys seguras**: Las claves de OpenAI no se almacenan permanentemente
- **Procesamiento local**: La mayoría de proveedores funcionan completamente offline

### 🏢 **Cumplimiento Empresarial**
- **GDPR Ready**: Los modelos locales cumplen con regulaciones de privacidad
- **Sin telemetría**: Los datos no se envían a terceros (excepto OpenAI si lo eliges)
- **Auditoría completa**: Logs detallados de todas las operaciones
- **Control total**: Tú decides qué datos procesar con qué proveedor

## 📞 Soporte

Para soporte técnico o preguntas:
- **Email**: soporte@gomarco.com
- **Documentación Técnica**: Ver `README_TECNICO.md`

---

<div align="center">
  <strong>Desarrollado con ❤️ para GOMARCO</strong><br>
  <em>Versión 2.0.0 - Multi-Provider AI System</em><br>
  <small>🧠 DeepSeek-R1 • 💬 Claude-Style • 🌐 OpenAI GPT-4 • ⚡ Ollama</small>
</div>
