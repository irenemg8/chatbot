# Chatbot GOMARCO - Asistente de IA Corporativo

<div align="center">
  <h3>🛏️ Descansa como te mereces - GOMARCO</h3>
  <p><strong>Asistente de Inteligencia Artificial Corporativo para Empleados GOMARCO</strong></p>
  
  [![.NET](https://img.shields.io/badge/.NET-8.0-blue.svg)](https://dotnet.microsoft.com/download/dotnet/8.0)
  [![Windows](https://img.shields.io/badge/Windows-11%20%7C%2010-brightgreen.svg)](https://www.microsoft.com/windows)
  [![License](https://img.shields.io/badge/License-Proprietary-red.svg)]()
  [![Security](https://img.shields.io/badge/Security-AES--256-green.svg)]()
</div>

## 📋 Tabla de Contenidos

- [Descripción General](#descripción-general)
- [Características Principales](#características-principales)
- [Arquitectura del Sistema](#arquitectura-del-sistema)
- [Requisitos del Sistema](#requisitos-del-sistema)
- [Instalación](#instalación)
- [Configuración](#configuración)
- [Uso de la Aplicación](#uso-de-la-aplicación)
- [Seguridad y Privacidad](#seguridad-y-privacidad)
- [Estructura del Proyecto](#estructura-del-proyecto)
- [Desarrollo](#desarrollo)
- [Solución de Problemas](#solución-de-problemas)
- [Soporte Técnico](#soporte-técnico)

## 🎯 Descripción General

El **Chatbot GOMARCO** es un asistente de inteligencia artificial corporativo diseñado específicamente para empleados de GOMARCO. La aplicación proporciona un entorno seguro y eficiente para la consulta de información corporativa, análisis de documentos confidenciales y soporte en procesos empresariales.

### Objetivos Principales

- **Seguridad Máxima**: Cifrado AES-256 para todos los archivos y datos sensibles
- **Facilidad de Uso**: Interfaz intuitiva diseñada para usuarios sin conocimientos técnicos
- **Productividad**: Análisis rápido de documentos y respuestas contextuales inteligentes
- **Privacidad**: Almacenamiento local de datos sin dependencias externas

## ✨ Características Principales

### 🔒 Seguridad Avanzada
- **Cifrado AES-256**: Todos los archivos se cifran automáticamente
- **Claves Únicas**: Generación de claves basada en datos únicos de la máquina
- **Almacenamiento Local**: Los datos nunca salen del equipo del usuario
- **Verificación de Integridad**: Hash SHA-256 para validar archivos

### 📁 Gestión de Archivos
- **Carga Múltiple**: Soporte para múltiples archivos simultáneamente
- **Formatos Compatibles**: PDF, Word, Excel, PowerPoint, imágenes, texto
- **Límite de Seguridad**: Máximo 1GB por archivo
- **Eliminación Segura**: Borrado completo de archivos cifrados

### 💬 Chatbot Inteligente
- **Respuestas Contextuales**: Análisis basado en archivos cargados
- **Historial Completo**: Almacenamiento de todas las conversaciones
- **Sugerencias Inteligentes**: Respuestas rápidas basadas en contexto
- **Análisis de Documentos**: Procesamiento automático de contenido

### 🎨 Interfaz de Usuario
- **Diseño Responsive**: Adaptable a diferentes tamaños de pantalla
- **Colores Corporativos**: Paleta de colores GOMARCO
- **Experiencia Intuitiva**: Diseño UX/UI optimizado para productividad
- **Material Design**: Componentes modernos y profesionales

## 🏗️ Arquitectura del Sistema

### Patrón de Diseño: MVVM (Model-View-ViewModel)

```
┌─────────────────────────────────────────────────────────────┐
│                    PRESENTACIÓN (WPF)                      │
├─────────────────────────────────────────────────────────────┤
│  Vistas/                    │  ViewModelos/                │
│  ├── VentanaPrincipal.xaml  │  ├── ViewModeloVentanaPrincipal│
│  └── ...                    │  └── ...                     │
├─────────────────────────────────────────────────────────────┤
│                      LÓGICA DE NEGOCIO                     │
├─────────────────────────────────────────────────────────────┤
│  Servicios/                                                 │
│  ├── ServicioChatbot        │  ├── ServicioArchivos        │
│  ├── ServicioCifrado        │  ├── ServicioHistorialChats  │
│  └── ...                    │  └── ...                     │
├─────────────────────────────────────────────────────────────┤
│                    ACCESO A DATOS                          │
├─────────────────────────────────────────────────────────────┤
│  Datos/                     │  Modelos/                    │
│  ├── ContextoBaseDatos      │  ├── SesionChat              │
│  └── ...                    │  ├── MensajeChat             │
│                              │  ├── ArchivoSubido           │
│                              │  └── ...                     │
├─────────────────────────────────────────────────────────────┤
│                    PERSISTENCIA                            │
├─────────────────────────────────────────────────────────────┤
│  SQLite Database            │  Archivos Cifrados AES-256   │
│  ├── Sesiones              │  ├── Documentos Seguros      │
│  ├── Mensajes              │  └── Archivos Temporales     │
│  └── Metadatos             │                               │
└─────────────────────────────────────────────────────────────┘
```

### Tecnologías Utilizadas

| Componente | Tecnología | Versión | Propósito |
|------------|------------|---------|-----------|
| **Framework** | .NET | 8.0 | Plataforma de desarrollo |
| **UI Framework** | WPF | 8.0 | Interfaz de usuario |
| **Design System** | Material Design | 4.9.0 | Componentes UI |
| **MVVM** | CommunityToolkit.Mvvm | 8.2.2 | Patrón arquitectónico |
| **Base de Datos** | SQLite | 8.0.0 | Almacenamiento local |
| **ORM** | Entity Framework Core | 8.0.0 | Acceso a datos |
| **Cifrado** | AES-256 | Nativo .NET | Seguridad de archivos |
| **Hash** | SHA-256 | Nativo .NET | Integridad de datos |
| **Logging** | Microsoft.Extensions.Logging | 8.0.0 | Registro de eventos |
| **DI Container** | Microsoft.Extensions.DependencyInjection | 8.0.0 | Inyección de dependencias |

## 💻 Requisitos del Sistema

### Requisitos Mínimos

| Componente | Especificación |
|------------|----------------|
| **Sistema Operativo** | Windows 10 (1909) o Windows 11 |
| **Procesador** | Intel Core i3 o AMD equivalente |
| **Memoria RAM** | 4 GB |
| **Espacio en Disco** | 500 MB libres |
| **Framework** | .NET 8.0 Runtime (se instala automáticamente) |
| **Permisos** | Usuario local (no requiere administrador después de instalación) |



## 🚀 Instalación

### Instalación Automática (Recomendada)

1. **Descargar el paquete de instalación**
   ```
   ChatbotGomarco-v1.0.0-Setup.zip
   ```

2. **Extraer y ejecutar como administrador**
   ```batch
   # Extraer el archivo
   # Clic derecho en "InstalarChatbotGomarco.bat"
   # Seleccionar "Ejecutar como administrador"
   ```

3. **Seguir las instrucciones en pantalla**
   - El instalador verificará dependencias
   - Descargará .NET 8 si es necesario
   - Configurará permisos automáticamente
   - Creará accesos directos

### Instalación Manual

1. **Instalar .NET 8 Runtime**
   ```bash
   # Descargar desde: https://dotnet.microsoft.com/download/dotnet/8.0
   windowsdesktop-runtime-8.0.0-win-x64.exe
   ```

2. **Crear directorios**
   ```batch
   mkdir "C:\Program Files\GOMARCO\ChatbotGomarco"
   ```

3. **Copiar archivos de la aplicación**
   ```batch
   xcopy /E /I ".\ChatbotGomarco\*" "C:\Program Files\GOMARCO\ChatbotGomarco\"
   ```

4. **Configurar permisos**
   ```batch
   icacls "C:\Program Files\GOMARCO\ChatbotGomarco" /grant Users:(OI)(CI)M
   ```

## ⚙️ Configuración

### Configuración Automática

La aplicación se configura automáticamente en el primer inicio:

- ✅ **Base de datos SQLite**: Se crea en `%APPDATA%\GOMARCO\ChatbotGomarco\`
- ✅ **Directorios de trabajo**: Se generan automáticamente
- ✅ **Claves de cifrado**: Se generan basadas en datos únicos de la máquina
- ✅ **Logs**: Se almacenan en `%APPDATA%\GOMARCO\ChatbotGomarco\Logs\`

### Ubicaciones de Archivos

| Tipo | Ubicación | Descripción |
|------|-----------|-------------|
| **Aplicación** | `C:\Program Files\GOMARCO\ChatbotGomarco\` | Archivos del programa |
| **Base de Datos** | `%APPDATA%\GOMARCO\ChatbotGomarco\chatbot.db` | SQLite database |
| **Archivos Cifrados** | `%APPDATA%\GOMARCO\ChatbotGomarco\ArchivosSegurosCifrados\` | Documentos cifrados |
| **Archivos Temporales** | `%TEMP%\ChatbotGomarco\ArchivosTemporales\` | Archivos temporales |
| **Logs** | `%APPDATA%\GOMARCO\ChatbotGomarco\Logs\` | Archivos de registro |

## 📖 Uso de la Aplicación

### Inicio Rápido

1. **Ejecutar la aplicación**
   - Doble clic en el icono del escritorio
   - O desde el menú inicio: "GOMARCO > Chatbot GOMARCO"

2. **Primera conversación**
   - La aplicación crea automáticamente una nueva sesión
   - Aparece un mensaje de bienvenida explicando las capacidades
   - Escribir mensaje en el campo inferior y presionar "Enviar"

### Gestión de Archivos

#### Cargar Archivos
```
1. Clic en el botón "📎 Archivo"
2. Seleccionar uno o múltiples archivos
3. Los archivos se cifran automáticamente
4. Aparecen en el panel derecho "Archivos Cargados"
5. El chatbot analiza el contenido automáticamente
```

#### Formatos Soportados
- **Documentos**: PDF, DOC, DOCX, PPT, PPTX, XLS, XLSX
- **Texto**: TXT, CSV, JSON, XML
- **Imágenes**: JPG, JPEG, PNG, GIF, BMP

#### Eliminar Archivos
```
1. En el panel "Archivos Cargados"
2. Clic en el botón "✖" junto al archivo
3. Confirmar eliminación
4. El archivo se elimina permanentemente (cifrado)
```

### Historial de Conversaciones

#### Crear Nueva Conversación
```
1. Clic en "+ Nueva Conversación"
2. Se crea una sesión nueva automáticamente
3. Los archivos se mantienen independientes por sesión
```

#### Navegar Historial
```
1. Panel izquierdo muestra todas las conversaciones
2. Clic en cualquier conversación para cargarla
3. Se muestran: título, fecha, número de mensajes, archivos
```

#### Eliminar Conversaciones
```
1. Clic en "✖" junto a la conversación
2. Confirmar eliminación
3. Se eliminan mensajes y archivos asociados
```

### Funciones Avanzadas

#### Sugerencias Inteligentes
- Aparecen automáticamente basadas en el contexto
- Clic en cualquier sugerencia para usarla
- Se actualizan después de cada mensaje

#### Búsqueda en Historial
- Funcionalidad de búsqueda incorporada en el servicio
- Búsqueda por contenido de mensajes
- Límite de 50 resultados por consulta

## 🔐 Seguridad y Privacidad

### Cifrado de Archivos

#### Algoritmo AES-256
```csharp
// Implementación del cifrado
- Algoritmo: Advanced Encryption Standard (AES)
- Modo: CBC (Cipher Block Chaining)
- Tamaño de clave: 256 bits
- Vector de inicialización: Aleatorio por archivo
- Relleno: PKCS7
```

#### Generación de Claves
```csharp
// La clave se genera basada en:
string datosUnicos = $"GOMARCO-{MachineName}-{UserName}-CHATBOT-2024";
byte[] clave = SHA256.ComputeHash(Encoding.UTF8.GetBytes(datosUnicos));
```

### Verificación de Integridad

#### Hash SHA-256
- Cada archivo tiene un hash único
- Verificación automática al acceder
- Detección de corrupción o modificación

### Políticas de Privacidad

#### ✅ Garantías
- **Datos Locales**: Toda la información permanece en el equipo
- **Sin Conexión Externa**: No se envían datos a servidores externos
- **Cifrado Automático**: Todos los archivos se cifran automáticamente
- **Borrado Seguro**: Eliminación completa de archivos temporales

#### ❌ Limitaciones
- **Clave Única por Máquina**: Los archivos solo son accesibles en el equipo original
- **Sin Respaldo Automático**: El usuario debe hacer respaldos manuales
- **Dependencia del Usuario**: Si se cambia el usuario de Windows, se pierde acceso

## 📁 Estructura del Proyecto

```
ChatbotGomarco/
├── 📁 Datos/                          # Capa de acceso a datos
│   └── ContextoBaseDatos.cs           # DbContext de Entity Framework
├── 📁 Instalacion/                    # Scripts de instalación
│   └── InstalarChatbotGomarco.bat     # Instalador automático
├── 📁 Modelos/                        # Modelos de datos
│   ├── ArchivoSubido.cs               # Entidad de archivos
│   ├── MensajeChat.cs                 # Entidad de mensajes
│   └── SesionChat.cs                  # Entidad de sesiones
├── 📁 Resources/                      # Recursos de la aplicación
│   └── gomarco-icon.ico               # Icono corporativo
├── 📁 Servicios/                      # Lógica de negocio
│   ├── IServicioArchivos.cs           # Interfaz gestión archivos
│   ├── ServicioArchivos.cs            # Implementación archivos
│   ├── IServicioChatbot.cs            # Interfaz chatbot
│   ├── ServicioChatbot.cs             # Implementación chatbot
│   ├── IServicioCifrado.cs            # Interfaz cifrado
│   ├── ServicioCifrado.cs             # Implementación cifrado AES-256
│   ├── IServicioHistorialChats.cs     # Interfaz historial
│   └── ServicioHistorialChats.cs      # Implementación historial
├── 📁 Utilidades/                     # Utilidades auxiliares
│   └── ConvertidoresValores.cs        # Convertidores WPF
├── 📁 ViewModelos/                    # ViewModels MVVM
│   └── ViewModeloVentanaPrincipal.cs  # ViewModel principal
├── 📁 Vistas/                         # Interfaces de usuario
│   ├── VentanaPrincipal.xaml          # Vista principal
│   └── VentanaPrincipal.xaml.cs       # Código subyacente
├── 📄 App.xaml                        # Configuración de aplicación
├── 📄 App.xaml.cs                     # Configuración de servicios
├── 📄 ChatbotGomarco.csproj           # Archivo de proyecto
└── 📄 README.md                       # Documentación técnica
```

### Descripción de Componentes

#### Capa de Datos (`Datos/`)
- **ContextoBaseDatos**: Configuración de Entity Framework con SQLite
- **Migraciones**: Automáticas al iniciar la aplicación
- **Índices**: Optimizados para consultas frecuentes

#### Modelos (`Modelos/`)
- **SesionChat**: Representa una conversación completa
- **MensajeChat**: Representa un mensaje individual
- **ArchivoSubido**: Metadatos de archivos cifrados

#### Servicios (`Servicios/`)
- **ServicioCifrado**: Manejo de cifrado AES-256 y hashing
- **ServicioArchivos**: Gestión completa de archivos
- **ServicioChatbot**: Lógica de inteligencia artificial simulada
- **ServicioHistorialChats**: Gestión de conversaciones

#### Utilidades (`Utilidades/`)
- **Convertidores**: Para binding de datos en WPF
- **Extensiones**: Métodos auxiliares

## 👨‍💻 Desarrollo

### Configuración del Entorno

#### Requisitos de Desarrollo
```bash
# Instalar Visual Studio 2022 o superior
# Con los siguientes workloads:
- .NET desktop development
- Windows application development

# O Visual Studio Code con:
- C# Extension
- .NET SDK 8.0
```

#### Clonar y Configurar
```bash
git clone <repository-url>
cd ChatbotGomarco
dotnet restore
dotnet build
```

#### Ejecutar en Modo Debug
```bash
dotnet run --project ChatbotGomarco.csproj
```

### Arquitectura de Desarrollo

#### Principios SOLID
- **S**: Cada servicio tiene una responsabilidad específica
- **O**: Abierto para extensión mediante interfaces
- **L**: Las implementaciones son intercambiables
- **I**: Interfaces segregadas por funcionalidad
- **D**: Dependencias inyectadas mediante contenedor

#### Patrones Implementados
- **MVVM**: Separación clara de responsabilidades
- **Repository**: Abstracción de acceso a datos
- **Service Layer**: Lógica de negocio encapsulada
- **Dependency Injection**: Loose coupling entre componentes

### Convenciones de Código

#### Nomenclatura
```csharp
// Clases: PascalCase
public class ServicioChatbot

// Métodos: PascalCase
public async Task ProcesarMensajeAsync()

// Variables: camelCase
private readonly ILogger _logger;

// Propiedades: PascalCase
public string MensajeUsuario { get; set; }

// Interfaces: I + PascalCase
public interface IServicioCifrado
```

#### Comentarios
```csharp
/// <summary>
/// Descripción del método en español
/// </summary>
/// <param name="parametro">Descripción del parámetro</param>
/// <returns>Descripción del valor de retorno</returns>
public async Task<string> MetodoEjemplo(string parametro)
```

### Testing

#### Estructura de Pruebas
```
ChatbotGomarco.Tests/
├── 📁 Servicios/
│   ├── ServicioCifradoTests.cs
│   ├── ServicioArchivosTests.cs
│   └── ServicioChatbotTests.cs
├── 📁 Utilidades/
│   └── ConvertidoresTests.cs
└── 📁 Integracion/
    └── BaseDatosTests.cs
```

#### Ejecutar Pruebas
```bash
dotnet test
dotnet test --verbosity normal
dotnet test --collect:"XPlat Code Coverage"
```

## 🔧 Solución de Problemas

### Problemas Comunes

#### Error: "No se puede inicializar la base de datos"
```
Causa: Permisos insuficientes en %APPDATA%
Solución:
1. Ejecutar como administrador una vez
2. Verificar permisos en carpeta GOMARCO
3. Comprobar antivirus no bloquee SQLite
```

#### Error: "Archivo no se puede cifrar"
```
Causa: Archivo en uso o permisos
Solución:
1. Cerrar aplicaciones que usen el archivo
2. Verificar que el archivo no esté protegido
3. Comprobar espacio en disco disponible
```

#### Error: ".NET Runtime no encontrado"
```
Causa: .NET 8 no instalado
Solución:
1. Ejecutar instalador automático
2. O descargar manualmente desde Microsoft
3. Reiniciar después de instalación
```

#### Rendimiento Lento
```
Causa: Muchos archivos o base de datos grande
Solución:
1. Limpiar archivos temporales
2. Eliminar conversaciones antiguas
3. Verificar espacio en disco SSD
4. Reiniciar aplicación
```

### Logs de Diagnóstico

#### Ubicación de Logs
```
%APPDATA%\GOMARCO\ChatbotGomarco\Logs\
├── chatbot-20241201.log     # Log del día actual
├── chatbot-20241130.log     # Logs anteriores
└── errors.log               # Solo errores críticos
```

#### Niveles de Log
- **Information**: Operaciones normales
- **Warning**: Advertencias no críticas
- **Error**: Errores que afectan funcionalidad
- **Critical**: Errores que impiden funcionamiento

### Recuperación de Datos

#### Respaldo Manual
```batch
# Crear respaldo de datos
xcopy /E /I "%APPDATA%\GOMARCO" "C:\Respaldos\ChatbotGomarco\"

# Restaurar desde respaldo
xcopy /E /I "C:\Respaldos\ChatbotGomarco\" "%APPDATA%\GOMARCO"
```

#### Migración de Equipo
```
Nota: Los archivos cifrados están vinculados al equipo original.
Para migrar:
1. Exportar base de datos SQLite
2. Descargar archivos temporalmente (descifrados)
3. Instalar en nuevo equipo
4. Importar archivos nuevamente
```

## 📞 Soporte Técnico

### Información de Contacto

| Área | Contacto | Horario |
|------|----------|---------|
| **Soporte Técnico** | soporte.ti@gomarco.com | Lunes-Viernes 8:00-18:00 |
| **Administrador del Sistema** | admin.sistemas@gomarco.com | 24/7 para emergencias |
| **Mesa de Ayuda** | Extensión 1234 | Lunes-Viernes 7:00-19:00 |

### Información para Reportes

#### Incluir en Reportes de Errores
```
1. Versión de la aplicación (visible en título)
2. Sistema operativo y versión
3. Descripción detallada del problema
4. Pasos para reproducir
5. Adjuntar logs relevantes
6. Captura de pantalla si aplica
```

#### Plantilla de Reporte
```
Asunto: [CHATBOT GOMARCO] Descripción breve del problema

Usuario: [Nombre del empleado]
Equipo: [Nombre del equipo/ID]
Fecha/Hora: [Cuando ocurrió]
Versión App: [1.0.0]

Descripción:
[Descripción detallada]

Pasos para reproducir:
1. [Paso 1]
2. [Paso 2]
3. [Paso 3]

Error esperado vs real:
[Qué esperaba vs qué pasó]

Archivos adjuntos:
- [Logs, capturas, etc.]
```

### FAQ - Preguntas Frecuentes

#### Q: ¿Puedo usar la aplicación sin conexión a internet?
**A**: Sí, la aplicación funciona completamente offline. No requiere conexión a internet.

#### Q: ¿Qué pasa si olvido la contraseña de mi equipo?
**A**: Los archivos cifrados están vinculados al usuario y equipo. Si cambia la contraseña del usuario, los archivos siguen siendo accesibles.

#### Q: ¿Puedo instalar en múltiples equipos?
**A**: Sí, pero cada instalación es independiente. Los archivos cifrados no son compatibles entre equipos.

#### Q: ¿Hay límite en el número de archivos?
**A**: No hay límite en cantidad, pero cada archivo no puede exceder 1GB.

#### Q: ¿Cómo actualizo a una nueva versión?
**A**: Ejecute el nuevo instalador. Mantendrá datos existentes.

## 📄 Licencias y Copyright

```
Copyright © 2024 GOMARCO
Todos los derechos reservados.

Este software es propiedad exclusiva de GOMARCO y está
destinado únicamente para uso interno por empleados
autorizados de la empresa.

Está prohibida la distribución, copia, modificación
o uso fuera del ámbito laboral sin autorización expresa.

Tecnologías de terceros utilizadas bajo sus respectivas licencias:
- .NET Framework: MIT License
- Entity Framework: MIT License  
- Material Design In XAML: MIT License
- SQLite: Public Domain
```

---

<div align="center">
  <p><strong>Desarrollado con ❤️ para GOMARCO</strong></p>
  <p><em>🛏️ Descansa como te mereces</em></p>
  
  **Versión 1.0.0** | **Diciembre 2024**
</div>
