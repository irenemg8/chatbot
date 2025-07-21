# 📦 Instrucciones de Instalación - Chatbot GOMARCO

## 🎯 Pasos para Instalación Exitosa

### 1. Preparación de Archivos (OBLIGATORIO)

Antes de ejecutar la instalación, debe compilar la aplicación:

```batch
# Ejecutar desde la carpeta raíz del proyecto
.\Instalacion\PreparaInstalacion.bat
```

Este script:
- ✅ Verifica que .NET 8 SDK esté instalado
- ✅ Compila la aplicación en modo Release
- ✅ Crea la carpeta `Instalacion\App\` con todos los archivos necesarios
- ✅ Verifica que todos los archivos estén presentes

### 2. Verificar Estructura de Archivos

Después de ejecutar la preparación, debe tener esta estructura:

```
Instalacion/
├── App/                              # ← Creado por PreparaInstalacion.bat
│   ├── ChatbotGomarco.exe           # ← Archivo principal
│   ├── ChatbotGomarco.dll
│   ├── *.dll                        # ← Dependencias
│   ├── Resources/
│   │   └── gomarco-icon.ico
│   └── version.txt
├── InstalarChatbotGomarco.bat       # ← Script de instalación
├── PreparaInstalacion.bat           # ← Script de preparación
└── INSTRUCCIONES.md                 # ← Este archivo
```

### 3. Ejecutar Instalación

**IMPORTANTE**: Debe ejecutarse como **Administrador**

```batch
# Clic derecho → "Ejecutar como administrador"
.\Instalacion\InstalarChatbotGomarco.bat
```

## 🔧 Solución de Errores Comunes

### Error: "Archivos de aplicación no encontrados"

**Causa**: No se ejecutó el paso de preparación

**Solución**:
```batch
# Ejecutar primero la preparación
.\Instalacion\PreparaInstalacion.bat

# Luego la instalación como administrador
.\Instalacion\InstalarChatbotGomarco.bat
```

### Error: "Acceso denegado"

**Causa**: No se ejecutó como administrador

**Solución**:
1. Cerrar la ventana actual
2. Clic derecho en `InstalarChatbotGomarco.bat`
3. Seleccionar **"Ejecutar como administrador"**

### Error: ".NET 8 SDK no encontrado"

**Causa**: .NET 8 SDK no está instalado

**Solución**:
1. Descargar .NET 8 SDK desde: https://dotnet.microsoft.com/download/dotnet/8.0
2. Instalar y reiniciar
3. Ejecutar nuevamente `PreparaInstalacion.bat`

### Error: "404 Not Found" al descargar .NET Runtime

**Causa**: URL temporal no disponible

**Solución**:
1. Descargar manualmente .NET 8 Desktop Runtime desde: https://dotnet.microsoft.com/download/dotnet/8.0
2. Instalar manualmente
3. Continuar con la instalación

## 📋 Lista de Verificación Pre-Instalación

- [ ] ✅ .NET 8 SDK instalado
- [ ] ✅ Ejecutado `PreparaInstalacion.bat` exitosamente
- [ ] ✅ Existe `Instalacion\App\ChatbotGomarco.exe`
- [ ] ✅ Permisos de administrador disponibles
- [ ] ✅ Windows 10/11 (versión compatible)
- [ ] ✅ Al menos 500MB de espacio libre

## 🚀 Instalación para Desarrollo

Si está desarrollando/modificando la aplicación:

```bash
# Clonar repositorio
git clone <repository-url>
cd ChatbotGomarco

# Restaurar dependencias
dotnet restore

# Compilar en modo Debug
dotnet build

# Ejecutar en modo desarrollo
dotnet run
```

## 📞 Soporte

Si continúa teniendo problemas:

1. **Verificar logs** en: `%APPDATA%\GOMARCO\ChatbotGomarco\Logs\`
2. **Contactar soporte**: soporte.ti@gomarco.com
3. **Incluir información**:
   - Versión de Windows
   - Mensaje de error exacto
   - Captura de pantalla
   - Logs de la aplicación

## 🔄 Desinstalación

Para desinstalar la aplicación:

```batch
# Opción 1: Desde Panel de Control
# Programas → Chatbot GOMARCO → Desinstalar

# Opción 2: Script automático
"C:\Program Files\GOMARCO\ChatbotGomarco\Desinstalar.bat"
```

---

**📝 Nota**: Estas instrucciones son específicas para Windows 10/11. La aplicación no es compatible con versiones anteriores de Windows. 