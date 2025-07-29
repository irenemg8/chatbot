@echo off
echo ============================================
echo 🔍 DEBUG CHATBOT OPENAI - GOMARCO
echo ============================================
echo.
echo 📋 Este script ejecutará el chatbot con logs de debugging
echo    para identificar el problema con OpenAI
echo.
echo ⚠️  IMPORTANTE: 
echo    1. Cierra cualquier instancia del chatbot antes de continuar
echo    2. Los logs aparecerán en la carpeta 'logs/'
echo    3. Presiona Ctrl+C para detener el debugging
echo.
pause

echo.
echo 🧹 Limpiando archivos temporales...
if exist "bin\Debug\net8.0-windows\ChatbotGomarco.exe" (
    taskkill /f /im "ChatbotGomarco.exe" >nul 2>&1
)

echo 🚀 Iniciando chatbot con debugging...
echo.
echo 📊 MONITOREA ESTOS LOGS:
echo    - 🔍 DEBUG - Iniciando carga de API key guardada...
echo    - 🔍 DEBUG - Verificando estado IA: IADisponible=...  
echo    - 🔍 DEBUG ServicioIAOpenAI.EstaDisponible()...
echo.

start "Chatbot Debug" /MAX "bin\Debug\net8.0-windows\ChatbotGomarco.exe"

echo.
echo ✅ Chatbot iniciado en modo debug
echo 📂 Revisa los logs en tiempo real en la carpeta 'logs/'
echo.
echo 🎯 PASOS PARA EL DEBUGGING:
echo    1. Observa si aparece "IA no disponible" en los logs
echo    2. Verifica si la API key se carga correctamente
echo    3. Comprueba el estado de disponibilidad de OpenAI
echo    4. Si aparece un error, compártelo para análisis
echo.
pause 