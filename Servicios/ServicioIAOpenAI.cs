using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;
using ChatbotGomarco.Servicios.LLM;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;
using System.Net.Http.Headers;
using System.Net;
using System.Net.Http.Json;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Servicio enterprise de IA integrado con OpenAI GPT-4 para análisis avanzado de documentos y conversaciones naturales
    /// Implementa protección avanzada de datos sensibles y compliance GDPR/LOPD
    /// Compatible con GPT-4, GPT-4 Turbo, GPT-4 Vision, y GPT-3.5 Turbo con configuración Enterprise
    /// </summary>
    public class ServicioIAOpenAI : IServicioIA
    {
        private readonly ILogger<ServicioIAOpenAI> _logger;
        private readonly IAnalizadorConversacion _analizadorConversacion;
        private readonly IDetectorDatosSensibles _detectorDatosSensibles;
        private readonly IServicioProcesamientoLocal _servicioProcesamientoLocal;
        private readonly HttpClient _httpClient;
        
        private bool _iaConfigurada = false;
        private string? _apiKey = null;
        private readonly ConfiguracionEnterpriseOpenAI _configuracionEnterprise;

        // ====================================================================
        // CONFIGURACIÓN ENTERPRISE DE OPENAI
        // ====================================================================
        private const string OPENAI_API_URL = "https://api.openai.com/v1/chat/completions";
        private const string MODELO_PRINCIPAL = "gpt-4o";              // Modelo más potente y actual
        private const string MODELO_VISION = "gpt-4o";                 // Para análisis de imágenes  
        private const string MODELO_ANALISIS = "gpt-4o";               // Para análisis profundo de documentos
        private const int MAX_TOKENS = 16384;                          // Tokens suficientes para análisis complejos
        private const decimal TEMPERATURA_CONVERSACION = 0.7m;         // Más creativo para conversaciones
        private const decimal TEMPERATURA_ANALISIS = 0.3m;             // Más preciso para análisis

        public ServicioIAOpenAI(
            ILogger<ServicioIAOpenAI> logger,
            IAnalizadorConversacion analizadorConversacion,
            IDetectorDatosSensibles detectorDatosSensibles,
            IServicioProcesamientoLocal servicioProcesamientoLocal,
            HttpClient httpClient)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _analizadorConversacion = analizadorConversacion ?? throw new ArgumentNullException(nameof(analizadorConversacion));
            _detectorDatosSensibles = detectorDatosSensibles ?? throw new ArgumentNullException(nameof(detectorDatosSensibles));
            _servicioProcesamientoLocal = servicioProcesamientoLocal ?? throw new ArgumentNullException(nameof(servicioProcesamientoLocal));
            _httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            
            // Configuración enterprise con máxima seguridad
            _configuracionEnterprise = ConfiguracionEnterpriseOpenAI.CrearConfiguracionMaximaSeguridad();
            
            // Configurar headers enterprise para OpenAI
            ConfigurarHeadersEnterprise();
            
            _logger.LogInformation("ServicioIAOpenAI inicializado en modo Enterprise con protección de datos sensibles y procesamiento local");
        }

        // ====================================================================
        // MÉTODOS DE CONFIGURACIÓN ENTERPRISE
        // ====================================================================

        /// <summary>
        /// Configura headers enterprise para máxima seguridad y compliance
        /// Implementa tracking de sesiones y metadatos de seguridad
        /// </summary>
        private void ConfigurarHeadersEnterprise()
        {
            // Headers básicos enterprise
            _httpClient.DefaultRequestHeaders.Clear();
            _httpClient.DefaultRequestHeaders.Add("User-Agent", "ChatbotGomarco/2.0 (Enterprise-Security)");
            
            // Configurar timeout enterprise
            _httpClient.Timeout = _configuracionEnterprise.Endpoints.TimeoutRequest;
            
            // Headers de compliance
            if (!string.IsNullOrEmpty(_configuracionEnterprise.HeadersSeguridad.DataResidency))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Data-Residency", _configuracionEnterprise.HeadersSeguridad.DataResidency);
            }
            
            if (!string.IsNullOrEmpty(_configuracionEnterprise.HeadersSeguridad.ComplianceLevel))
            {
                _httpClient.DefaultRequestHeaders.Add("X-Compliance-Level", _configuracionEnterprise.HeadersSeguridad.ComplianceLevel);
            }
            
            // Headers de organización enterprise (si están configurados)
            if (!string.IsNullOrEmpty(_configuracionEnterprise.HeadersSeguridad.OpenAIOrganization))
            {
                _httpClient.DefaultRequestHeaders.Add("OpenAI-Organization", _configuracionEnterprise.HeadersSeguridad.OpenAIOrganization);
            }
            
            if (!string.IsNullOrEmpty(_configuracionEnterprise.HeadersSeguridad.OpenAIProject))
            {
                _httpClient.DefaultRequestHeaders.Add("OpenAI-Project", _configuracionEnterprise.HeadersSeguridad.OpenAIProject);
            }
            
            _logger.LogDebug("Headers enterprise configurados correctamente");
        }

        /// <summary>
        /// Procesa contenido con protección enterprise de datos sensibles
        /// Implementa análisis de sensibilidad, anonimización y políticas de fallback
        /// </summary>
        private async Task<ResultadoProcesamientoSeguro> ProcesarContenidoConSeguridadAsync(string contenido, string contextoArchivos = "")
        {
            try
            {
                // Combinar contenido del mensaje con contexto de archivos para análisis conjunto
                var contenidoCompleto = !string.IsNullOrEmpty(contextoArchivos) 
                    ? $"{contenido}\n\n--- CONTEXTO ARCHIVOS ---\n{contextoArchivos}"
                    : contenido;

                // Detectar y anonimizar datos sensibles
                var resultadoAnonimizacion = await _detectorDatosSensibles.AnonimizarContenidoAsync(contenidoCompleto);
                
                // Aplicar políticas de seguridad según nivel detectado
                var estrategiaProcesamiento = DeterminarEstrategiaProcesamiento(resultadoAnonimizacion.NivelDetectado);
                
                // Log de auditoría enterprise
                await RegistrarAuditoriaSeguridad(resultadoAnonimizacion, estrategiaProcesamiento);
                
                return new ResultadoProcesamientoSeguro
                {
                    ContenidoAnonimizado = resultadoAnonimizacion.ContenidoAnonimizado,
                    MapaAnonimizacion = resultadoAnonimizacion.MapaAnonimizacion,
                    NivelSensibilidad = resultadoAnonimizacion.NivelDetectado,
                    EstrategiaProcesamiento = estrategiaProcesamiento,
                    PuedeProceaarse = !resultadoAnonimizacion.RequiereProcesamientoLocal || !_configuracionEnterprise.Fallback.RechazarSiNoSeguro,
                    MensajeRechazo = resultadoAnonimizacion.RequiereProcesamientoLocal && _configuracionEnterprise.Fallback.RechazarSiNoSeguro 
                        ? _configuracionEnterprise.Fallback.MensajeContenidoRechazado 
                        : null
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error durante el procesamiento seguro de contenido");
                throw new Exception("Error en el sistema de protección de datos sensibles", ex);
            }
        }

        /// <summary>
        /// Determina la estrategia de procesamiento según el nivel de sensibilidad
        /// </summary>
        private EstrategiaProcesamiento DeterminarEstrategiaProcesamiento(NivelSensibilidad nivel)
        {
            return nivel switch
            {
                NivelSensibilidad.Publico => EstrategiaProcesamiento.OpenAIEstandar,
                NivelSensibilidad.Interno => EstrategiaProcesamiento.OpenAIEnterprise,
                NivelSensibilidad.Confidencial => EstrategiaProcesamiento.OpenAIEnterpriseSeguro,
                NivelSensibilidad.UltraSecreto => _configuracionEnterprise.Fallback.ProcesamientoLocalUltraSensible 
                    ? EstrategiaProcesamiento.ProcesamientoLocal 
                    : EstrategiaProcesamiento.Rechazado,
                _ => EstrategiaProcesamiento.OpenAIEstandar
            };
        }

        /// <summary>
        /// Registra eventos de auditoría para compliance y tracking de seguridad
        /// </summary>
        private async Task RegistrarAuditoriaSeguridad(ResultadoAnonimizacion resultado, EstrategiaProcesamiento estrategia)
        {
            if (!_configuracionEnterprise.Auditoria.LoggingDetallado)
                return;

            try
            {
                var metadatosAuditoria = new
                {
                    Timestamp = DateTime.UtcNow,
                    NivelSensibilidad = resultado.NivelDetectado.ToString(),
                    DatosAnonimizados = resultado.CantidadDatosAnonimizados,
                    TiposDatosDetectados = resultado.TiposDatosSensiblesDetectados,
                    EstrategiaProcesamiento = estrategia.ToString(),
                    SessionId = Guid.NewGuid().ToString(), // En producción, usar ID de sesión real
                    HashContenido = _configuracionEnterprise.Auditoria.HashearContenido 
                        ? resultado.ContenidoAnonimizado.GetHashCode().ToString() 
                        : "HASH_DISABLED"
                };

                var rutaLogAuditoria = Path.Combine(_configuracionEnterprise.Auditoria.RutaLogsAuditoria, 
                    $"auditoria-ia-{DateTime.Now:yyyy-MM-dd}.log");
                
                Directory.CreateDirectory(Path.GetDirectoryName(rutaLogAuditoria)!);
                
                await File.AppendAllTextAsync(rutaLogAuditoria, 
                    $"{JsonSerializer.Serialize(metadatosAuditoria)}\n");

                // Alerta para contenido crítico
                if (resultado.NivelDetectado >= NivelSensibilidad.Confidencial && 
                    _configuracionEnterprise.Auditoria.AlertasContenidoCritico)
                {
                    _logger.LogWarning("🚨 ALERTA CONTENIDO SENSIBLE: Nivel {Nivel}, {Cantidad} datos detectados, Estrategia: {Estrategia}", 
                        resultado.NivelDetectado, 
                        resultado.CantidadDatosAnonimizados, 
                        estrategia);
                }

                _logger.LogDebug("Auditoría de seguridad registrada: {Nivel} sensibilidad", resultado.NivelDetectado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al registrar auditoría de seguridad");
                // No lanzar excepción para no afectar el flujo principal
            }
        }

        /// <summary>
        /// Construye el contenido del mensaje de forma segura, utilizando datos anonimizados
        /// Mantiene la funcionalidad original pero con protección de datos sensibles
        /// </summary>
        private string BuildSecureMessageContent(string mensajeOriginal, string contextoArchivos, ResultadoProcesamientoSeguro resultadoSeguridad)
        {
            // Si hay archivos, usar el contenido anonimizado que ya incluye tanto mensaje como contexto
            if (!string.IsNullOrEmpty(contextoArchivos))
            {
                return $@"INSTRUCCIONES IMPORTANTES:
- Tienes acceso completo al contenido de los archivos proporcionados
- DEBES analizar, leer y procesar toda la información de los archivos
- Proporciona respuestas detalladas basadas en el contenido real de los archivos
- Extrae datos específicos, fechas, números, nombres cuando sea relevante
- NO digas que no puedes acceder a archivos - SÍ PUEDES porque el contenido está aquí
- NOTA: Algunos datos han sido anonimizados por políticas de seguridad

**CONTENIDO COMPLETO DE LOS ARCHIVOS (ANONIMIZADO):**
{resultadoSeguridad.ContenidoAnonimizado}

RESPUESTA REQUERIDA: Analiza el contenido anterior y responde de forma detallada y específica basándote en la información real de los archivos.";
            }

            // Si solo hay mensaje, usar la parte del contenido anonimizado que corresponde al mensaje
            return resultadoSeguridad.ContenidoAnonimizado;
        }

        // ====================================================================
        // IMPLEMENTACIÓN DE INTERFAZ IServicioIA
        // ====================================================================

        public void ConfigurarClave(string apiKey)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(apiKey))
                {
                    _logger.LogWarning("Intento de configurar API Key vacía");
                    throw new ArgumentException("La API Key no puede estar vacía o contener solo espacios en blanco");
                }

                if (!apiKey.StartsWith("sk-"))
                {
                    _logger.LogWarning("Formato de API Key inválido: no comienza con 'sk-'");
                    throw new ArgumentException("Formato de API Key inválido. Debe comenzar con 'sk-' seguido de caracteres alfanuméricos");
                }

                if (apiKey.Length < 20)
                {
                    _logger.LogWarning("API Key demasiado corta: {Length} caracteres", apiKey.Length);
                    throw new ArgumentException("La API Key parece ser demasiado corta. Verifica que sea correcta");
                }

                _apiKey = apiKey.Trim();
                
                // Limpiar headers previos de autorización
                _httpClient.DefaultRequestHeaders.Authorization = null;
                
                // Configurar autorización con la nueva clave
                _httpClient.DefaultRequestHeaders.Authorization = 
                    new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", _apiKey);
                
                _iaConfigurada = true;
                
                var maskedKey = apiKey.Length > 10 
                    ? $"{apiKey[..7]}...{apiKey[^4..]}" 
                    : "sk-***";
                    
                _logger.LogInformation("OpenAI API configurada correctamente con clave: {MaskedKey}", maskedKey);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al configurar OpenAI API");
                _iaConfigurada = false;
                _apiKey = null;
                _httpClient.DefaultRequestHeaders.Authorization = null;
                throw;
            }
        }

        public bool EstaDisponible()
        {
            // Verificación en tiempo real del estado
            var configurada = _iaConfigurada;
            var tieneApiKey = !string.IsNullOrEmpty(_apiKey);
            var httpClientConfigurado = _httpClient.DefaultRequestHeaders.Authorization != null;
            var disponible = configurada && tieneApiKey && httpClientConfigurado;
            
            // DEBUG: Log detallado del estado de disponibilidad
            _logger.LogInformation("🔍 DEBUG ServicioIAOpenAI.EstaDisponible(): configurada={Configurada}, tieneApiKey={TieneApiKey}, httpAuth={HttpAuth}, disponible={Disponible}", 
                configurada, tieneApiKey, httpClientConfigurado, disponible);
            
            if (!configurada)
            {
                _logger.LogWarning("❌ DEBUG: _iaConfigurada es FALSE - La IA no ha sido configurada");
            }
            
            if (!tieneApiKey)
            {
                _logger.LogWarning("❌ DEBUG: _apiKey está vacía o nula - Falta API key");
            }
            
            if (!httpClientConfigurado)
            {
                _logger.LogWarning("❌ DEBUG: HttpClient no tiene Authorization header configurado");
            }
            
            return disponible;
        }

        public async Task<string> GenerarRespuestaAsync(string mensaje, string contextoArchivos = "", List<MensajeChat>? historialConversacion = null)
        {
            _logger.LogInformation("🔍 DEBUG GenerarRespuestaAsync - Iniciando con mensaje: '{Mensaje}'", mensaje.Substring(0, Math.Min(50, mensaje.Length)));
            
            var disponible = EstaDisponible();
            _logger.LogInformation("🔍 DEBUG GenerarRespuestaAsync - EstaDisponible(): {Disponible}", disponible);
            
            if (!disponible)
            {
                _logger.LogError("❌ DEBUG - OpenAI API no está configurada");
                throw new InvalidOperationException("OpenAI API no está configurada. Configure su API Key primero.");
            }

            try
            {
                _logger.LogInformation("🔒 DEBUG - Iniciando procesamiento enterprise con protección de datos sensibles");

                // PASO 1: Procesamiento seguro con análisis de sensibilidad
                var resultadoSeguridad = await ProcesarContenidoConSeguridadAsync(mensaje, contextoArchivos);

                // PASO 2: Verificar si requiere procesamiento local
                if (resultadoSeguridad.EstrategiaProcesamiento == EstrategiaProcesamiento.ProcesamientoLocal)
                {
                    _logger.LogInformation("🔒 PROCESAMIENTO LOCAL activado para datos sensibles - Zero Data Leakage");
                    return await _servicioProcesamientoLocal.GenerarRespuestaLocalAsync(
                        mensaje, 
                        contextoArchivos, 
                        historialConversacion,
                        resultadoSeguridad.MapaAnonimizacion.Values.Select(v => v.Replace("[", "").Replace("_PROTEGIDO]", "")).ToList());
                }

                // PASO 2b: Verificar si el contenido puede procesarse externamente
                if (!resultadoSeguridad.PuedeProceaarse)
                {
                    _logger.LogWarning("❌ Contenido rechazado por políticas de seguridad: {Nivel}", resultadoSeguridad.NivelSensibilidad);
                    return resultadoSeguridad.MensajeRechazo ?? 
                           "No puedo procesar este contenido debido a políticas de seguridad de datos sensibles.";
                }

                // PASO 3: Determinar configuración según estrategia de procesamiento
                var configuracionesPorEstrategia = ConfiguracionPorEstrategia.ObtenerConfiguraciones();
                var config = configuracionesPorEstrategia.GetValueOrDefault(resultadoSeguridad.EstrategiaProcesamiento, 
                    configuracionesPorEstrategia[EstrategiaProcesamiento.OpenAIEstandar]);

                // PASO 4: Agregar headers de seguridad específicos
                foreach (var header in config.HeadersAdicionales)
                {
                    if (_httpClient.DefaultRequestHeaders.Contains(header.Key))
                        _httpClient.DefaultRequestHeaders.Remove(header.Key);
                    _httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                // PASO 5: Construir historial de conversación
                var mensajes = ConstruirHistorialConversacion(historialConversacion);
                
                // PASO 6: Construir mensaje con contenido anonimizado
                var contenidoMensajeSeguro = BuildSecureMessageContent(mensaje, contextoArchivos, resultadoSeguridad);
                mensajes.Add(new OpenAIMessage("user", contenidoMensajeSeguro));

                // PASO 7: Configurar solicitud enterprise con parámetros seguros
                var solicitud = new OpenAIRequest
                {
                    Model = config.Modelo,
                    Messages = mensajes,
                    MaxTokens = config.MaxTokens,
                    Temperature = config.Temperatura,
                    TopP = config.TopP,
                    FrequencyPenalty = 0.0m,
                    PresencePenalty = 0.0m
                };

                // PASO 8: Enviar solicitud con configuración enterprise
                _logger.LogInformation("🔍 DEBUG - Enviando solicitud a OpenAI API...");
                var respuesta = await EnviarSolicitudOpenAIAsync(solicitud);
                _logger.LogInformation("🔍 DEBUG - Respuesta recibida de OpenAI API: {Longitud} caracteres", respuesta?.Length ?? 0);

                // PASO 9: Log de éxito con metadatos de seguridad
                _logger.LogInformation("✅ DEBUG - Respuesta generada exitosamente - Estrategia: {Estrategia}, Nivel: {Nivel}, Datos anonimizados: {Cantidad}", 
                    resultadoSeguridad.EstrategiaProcesamiento, 
                    resultadoSeguridad.NivelSensibilidad,
                    resultadoSeguridad.MapaAnonimizacion.Count);
                
                return respuesta;
            }
            catch (HttpRequestException httpEx)
            {
                _logger.LogError(httpEx, "Error de conexión HTTP al comunicarse con OpenAI");
                throw new Exception("Error de conexión con OpenAI. Verifica tu conexión a internet y que la API de OpenAI esté disponible.", httpEx);
            }
            catch (TaskCanceledException timeoutEx)
            {
                _logger.LogError(timeoutEx, "Timeout al comunicarse con OpenAI");
                throw new Exception("La solicitud a OpenAI excedió el tiempo límite. Intenta nuevamente.", timeoutEx);
            }
            catch (Exception ex) when (ex.Message.Contains("401") || ex.Message.Contains("Unauthorized"))
            {
                _logger.LogError(ex, "Error de autenticación con OpenAI - API Key inválida");
                throw new Exception("API Key de OpenAI inválida o expirada. Por favor, verifica tu clave en https://platform.openai.com/api-keys", ex);
            }
            catch (Exception ex) when (ex.Message.Contains("429") || ex.Message.Contains("Too Many Requests"))
            {
                _logger.LogError(ex, "Límite de solicitudes excedido en OpenAI");  
                throw new Exception("Has excedido el límite de solicitudes a OpenAI. Espera unos minutos antes de intentar nuevamente.", ex);
            }
            catch (Exception ex) when (ex.Message.Contains("402") || ex.Message.Contains("insufficient_quota"))
            {
                _logger.LogError(ex, "Cuota insuficiente en OpenAI");
                throw new Exception("Créditos insuficientes en tu cuenta de OpenAI. Verifica tu saldo en https://platform.openai.com/usage", ex);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error general al generar respuesta con OpenAI");
                throw new Exception($"Error al comunicarse con OpenAI: {ex.Message}", ex);
            }
        }

        public async Task<string> AnalizarContenidoConIAAsync(string contenidoArchivos, string pregunta)
        {
            try
            {
                // PASO 1: Verificar datos sensibles antes de procesar
                var resultadoSeguridad = await ProcesarContenidoConSeguridadAsync(pregunta, contenidoArchivos);
                
                // PASO 2: Si requiere procesamiento local, usar servicio local
                if (resultadoSeguridad.EstrategiaProcesamiento == EstrategiaProcesamiento.ProcesamientoLocal)
                {
                    _logger.LogInformation("🔒 Análisis LOCAL activado para contenido con datos sensibles");
                    return await _servicioProcesamientoLocal.AnalizarContenidoLocalAsync(
                        contenidoArchivos, 
                        pregunta,
                        resultadoSeguridad.MapaAnonimizacion.Values.Select(v => v.Replace("[", "").Replace("_PROTEGIDO]", "")).ToList());
                }

                // PASO 3: Procesar con OpenAI si no hay datos sensibles
                if (!EstaDisponible())
                {
                    throw new InvalidOperationException("OpenAI API no está configurada");
                }

                // Usar contenido anonimizado para envío seguro a OpenAI
                var contenidoSeguro = resultadoSeguridad.ContenidoAnonimizado;
                var mensajeSistema = @"Eres MARCO, el asistente conversacional de GOMARCO. Te comportas como ChatGPT: natural, inteligente y útil. NO eres un robot corporativo.

🎯 **TU ESTILO:**
- Conversas como una persona real que entiende documentos
- Contextualizas naturalmente: ""Revisando tu informe de ventas...""
- Haces análisis automáticamente: promedios, totales, tendencias
- Respondes exactamente lo que preguntan, sin vomitar datos

💡 **ANÁLISIS INTELIGENTE AUTOMÁTICO:**
Para facturas → ""Tu promedio mensual fue €127, con un pico en julio de €156""
Para informes → ""GOMARCO tuvo un buen trimestre con €2.4M y crecimiento del 23%""
Para contratos → ""Las fechas clave son: inicio 15/01, renovación 31/12""
Para cualquier pregunta → Encuentra la respuesta específica en el documento

📊 **EJEMPLOS DE RESPUESTAS NATURALES:**
❌ ROBÓTICO: ""El documento contiene los siguientes elementos: fecha, precio, cantidad...""
✅ CONVERSACIONAL: ""He visto tu informe de Q2. GOMARCO cerró con €2.4M en ventas, un 23% más que el trimestre anterior. El margen del 34% está bastante bien. ¿Te interesa algún detalle específico?""

🚀 **REGLAS DE ORO:**
1. Habla como ChatGPT, no como un sistema empresarial
2. Contextualiza brevemente antes de responder
3. Haz cálculos automáticamente cuando sea útil
4. Responde específicamente lo que preguntaron
5. Ofrece insights genuinamente útiles";

                var mensaje = $@"He aquí el contenido del documento que necesitas analizar:

{contenidoSeguro}

El usuario pregunta: ""{pregunta}""

IMPORTANTE: El contenido puede contener datos enmascarados con asteriscos (*) por políticas de seguridad. Esto es normal y debes trabajar con esta información protegida.

Analiza inteligentemente y responde de forma conversacional. Contextualiza primero qué tipo de documento es, luego responde específicamente a su pregunta. Si puedes ofrecer insights adicionales útiles, hazlo. Recuerda: sé como ChatGPT - natural, útil y conversacional.";

                var mensajes = new List<OpenAIMessage>
                {
                    new OpenAIMessage("system", mensajeSistema),
                    new OpenAIMessage("user", mensaje)
                };

                var solicitud = new OpenAIRequest
                {
                    Model = MODELO_ANALISIS,
                    Messages = mensajes,
                    MaxTokens = MAX_TOKENS,
                    Temperature = TEMPERATURA_ANALISIS, // Más preciso para análisis
                    TopP = 0.9m
                };

                var respuesta = await EnviarSolicitudOpenAIAsync(solicitud);
                
                _logger.LogInformation("Análisis de contenido completado con OpenAI");
                return respuesta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al analizar contenido con OpenAI");
                throw new Exception($"Error al analizar con OpenAI: {ex.Message}", ex);
            }
        }

        public async Task<string> GenerarResumenInteligente(string contenido, string tipoResumen = "general")
        {
            try
            {
                // PASO 1: Verificar datos sensibles
                var resultadoSeguridad = await ProcesarContenidoConSeguridadAsync(contenido);
                
                // PASO 2: Si requiere procesamiento local, usar servicio local
                if (resultadoSeguridad.EstrategiaProcesamiento == EstrategiaProcesamiento.ProcesamientoLocal)
                {
                    _logger.LogInformation("🔒 Resumen LOCAL activado para contenido con datos sensibles");
                    return await _servicioProcesamientoLocal.GenerarResumenLocalAsync(
                        contenido, 
                        tipoResumen,
                        resultadoSeguridad.MapaAnonimizacion.Values.Select(v => v.Replace("[", "").Replace("_PROTEGIDO]", "")).ToList());
                }

                // PASO 3: Procesar con OpenAI si no hay datos sensibles
                if (!EstaDisponible())
                {
                    throw new InvalidOperationException("OpenAI API no está configurada");
                }

                // Usar contenido anonimizado
                var contenidoSeguro = resultadoSeguridad.ContenidoAnonimizado;
                var instruccionesResumen = tipoResumen.ToLower() switch
                {
                    "ejecutivo" => @"Genera un RESUMEN EJECUTIVO empresarial que incluya:
• **Puntos Clave:** Los 3-5 aspectos más importantes
• **Datos Críticos:** Fechas, cantidades, precios relevantes
• **Impacto:** Implicaciones empresariales o decisiones requeridas
• **Recomendaciones:** Próximos pasos sugeridos
Máximo 3 párrafos, enfoque directivo.",

                    "detallado" => @"Genera un RESUMEN DETALLADO que incluya:
• **Análisis Completo:** Todos los aspectos importantes del contenido
• **Estructura:** Organización clara por temas o secciones
• **Datos Específicos:** Todas las cifras, fechas y referencias importantes
• **Contexto:** Explicación del propósito y relevancia
Extensión: 4-6 párrafos con análisis profundo.",

                    "técnico" => @"Genera un RESUMEN TÉCNICO que incluya:
• **Especificaciones:** Detalles técnicos precisos
• **Procesos:** Metodologías y procedimientos descritos
• **Parámetros:** Configuraciones, medidas y valores técnicos
• **Conclusiones:** Resultados y hallazgos técnicos
Enfoque: Profesional técnico, precisión absoluta.",

                    _ => @"Genera un RESUMEN GENERAL que incluya:
• **Idea Principal:** El tema central del contenido
• **Puntos Importantes:** Los aspectos más relevantes
• **Información Clave:** Datos y detalles significativos
• **Conclusión:** Síntesis final del contenido
Extensión: 2-3 párrafos, equilibrado y comprensible."
                };

                var mensajeSistema = $@"Eres un experto en creación de resúmenes empresariales. Tu tarea es crear resúmenes precisos, bien estructurados y profesionales.

**INSTRUCCIONES ESPECÍFICAS:**
{instruccionesResumen}

**FORMATO REQUERIDO:**
- Responde SIEMPRE en español
- Usa formato markdown para estructura clara
- Destaca información importante con **negritas**
- Organiza con bullets y numeración cuando sea apropiado
- Mantén un tono profesional y empresarial";

                var mensaje = $@"**CONTENIDO PARA RESUMIR:**
{contenidoSeguro}

IMPORTANTE: El contenido puede contener datos enmascarados con asteriscos (*) por políticas de seguridad. Esto es normal para proteger información sensible.

Genera el resumen siguiendo las instrucciones específicas para el tipo: {tipoResumen}";

                var mensajes = new List<OpenAIMessage>
                {
                    new OpenAIMessage("system", mensajeSistema),
                    new OpenAIMessage("user", mensaje)
                };

                var solicitud = new OpenAIRequest
                {
                    Model = MODELO_PRINCIPAL,
                    Messages = mensajes,
                    MaxTokens = MAX_TOKENS,
                    Temperature = TEMPERATURA_ANALISIS,
                    TopP = 0.85m
                };

                var respuesta = await EnviarSolicitudOpenAIAsync(solicitud);
                
                _logger.LogInformation("Resumen inteligente generado: tipo {TipoResumen}", tipoResumen);
                return respuesta;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al generar resumen con OpenAI");
                throw new Exception($"Error al generar resumen: {ex.Message}", ex);
            }
        }

        public async Task<List<string>> GenerarSugerenciasPreguntasAsync(string contenidoArchivos)
        {
            try
            {
                // PASO 1: Verificar datos sensibles
                var resultadoSeguridad = await ProcesarContenidoConSeguridadAsync(contenidoArchivos);
                
                // PASO 2: Si requiere procesamiento local, usar servicio local
                if (resultadoSeguridad.EstrategiaProcesamiento == EstrategiaProcesamiento.ProcesamientoLocal)
                {
                    _logger.LogInformation("🔒 Sugerencias LOCALES activadas para contenido con datos sensibles");
                    return await _servicioProcesamientoLocal.GenerarSugerenciasLocalesAsync(
                        contenidoArchivos,
                        resultadoSeguridad.MapaAnonimizacion.Values.Select(v => v.Replace("[", "").Replace("_PROTEGIDO]", "")).ToList());
                }

                // PASO 3: Procesar con OpenAI si no hay datos sensibles
                if (!EstaDisponible())
                {
                    return new List<string>
                    {
                        "¿Puedes resumir el contenido principal?",
                        "¿Cuáles son los datos más importantes?",
                        "¿Qué información específica contiene?"
                    };
                }

                // Usar contenido anonimizado
                var contenidoSeguro = resultadoSeguridad.ContenidoAnonimizado;
                var mensajeSistema = @"Eres un experto en análisis de documentos. Tu tarea es generar preguntas inteligentes y específicas basadas en el contenido proporcionado.

**INSTRUCCIONES:**
- Genera exactamente 5 preguntas relevantes y específicas
- Las preguntas deben ser útiles para extraer información clave del documento
- Usa un formato directo y profesional
- Enfócate en datos, análisis y insights importantes
- Responde SOLO con las preguntas, una por línea, sin numeración ni bullets";

                var mensaje = $@"Basándote en este contenido, genera 5 preguntas específicas y relevantes:

{contenidoSeguro.Substring(0, Math.Min(2000, contenidoSeguro.Length))}...

NOTA: El contenido puede tener datos enmascarados con asteriscos (*) por seguridad.

Genera las preguntas:";

                var mensajes = new List<OpenAIMessage>
                {
                    new OpenAIMessage("system", mensajeSistema),
                    new OpenAIMessage("user", mensaje)
                };

                var solicitud = new OpenAIRequest
                {
                    Model = MODELO_PRINCIPAL,
                    Messages = mensajes,
                    MaxTokens = 300,
                    Temperature = 0.6m
                };

                var respuesta = await EnviarSolicitudOpenAIAsync(solicitud);
                
                var preguntas = respuesta.Split('\n', StringSplitOptions.RemoveEmptyEntries)
                    .Where(p => !string.IsNullOrWhiteSpace(p))
                    .Select(p => p.Trim().TrimStart('1', '2', '3', '4', '5', '.', '-', '*', ' '))
                    .Where(p => p.EndsWith('?'))
                    .Take(5)
                    .ToList();

                return preguntas.Any() ? preguntas : new List<string>
                {
                    "¿Puedes resumir los puntos principales?",
                    "¿Qué datos específicos contiene?",
                    "¿Cuál es la información más relevante?",
                    "¿Hay fechas o números importantes?",
                    "¿Qué conclusiones principales se pueden extraer?"
                };
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error al generar sugerencias con OpenAI, usando fallback");
                return new List<string>
                {
                    "¿Puedes analizar el contenido principal?",
                    "¿Qué información específica contiene este documento?",
                    "¿Cuáles son los datos más importantes?",
                    "¿Puedes extraer las fechas y números relevantes?",
                    "¿Qué insights empresariales puedes proporcionar?"
                };
            }
        }

        // ====================================================================
        // MÉTODOS PRIVADOS DE OPENAI
        // ====================================================================

        private List<OpenAIMessage> ConstruirHistorialConversacion(List<MensajeChat>? historialConversacion)
        {
            var mensajes = new List<OpenAIMessage>();

            // Mensaje de sistema corporativo
            mensajes.Add(new OpenAIMessage("system", ConstruirMensajeSistema()));

            // Agregar historial reciente (últimos 10 mensajes para mantener contexto sin exceder límites)
            if (historialConversacion != null && historialConversacion.Any())
            {
                foreach (var msg in historialConversacion.TakeLast(10))
                {
                    if (!string.IsNullOrEmpty(msg.Contenido))
                    {
                        var role = msg.TipoMensaje == TipoMensaje.Usuario ? "user" : "assistant";
                        mensajes.Add(new OpenAIMessage(role, msg.Contenido));
                    }
                }
            }

            return mensajes;
        }

        private string ConstruirMensajeSistema()
        {
            return @"Eres la IA de GOMARCO, el asistente conversacional de GOMARCO. Eres como ChatGPT pero especializado en análisis empresarial - natural, inteligente y útil.

🧠 **TU ESTILO CONVERSACIONAL:**
- Hablas como una persona real, no como un robot corporativo
- Entiendes el contexto y respondes exactamente lo que necesitan
- Contextualizas con naturalidad: ""He revisado tu documento de ventas..."" 
- Haces análisis inteligentes: promedios, tendencias, comparaciones
- Ofreces insights útiles sin abrumar con datos

💡 **EJEMPLOS DE RESPUESTAS NATURALES:**
Usuario: ""¿Cuánto gastó en electricidad?"" 
Tú: ""Mirando tus facturas, el promedio mensual fue de €127. Vi un pico en julio (€156) probablemente por el aire acondicionado. ¿Quieres que analice algún mes específico?""

Usuario: ""¿De qué va este documento?""
Tú: ""Es un informe financiero de Q2 2025 de GOMARCO. Básicamente, muestra un trimestre bastante bueno con €2.4M en ventas y crecimiento del 23%. ¿Te interesa algún aspecto específico?""

🎯 **REGLAS DE ORO:**
1. NUNCA vomites listas de datos sin contexto
2. SIEMPRE explica qué significa la información
3. Calcula automáticamente promedios, totales y tendencias cuando sea útil
4. Responde específicamente lo que preguntaron
5. Sé conversacional como si fueras un amigo experto ayudando
- NUNCA: Hagas listas largas de datos sin contexto
- SIEMPRE: Responde como si fueras ChatGPT en persona

💼 **SOBRE GOMARCO:**
- Empresa líder en descanso y bienestar (colchones, productos de descanso)
- Lema: ""Descansa como te mereces""

📊 **EJEMPLOS DE CÓMO RESPONDER:**
❌ MAL: ""Datos financieros: €2,458,750 ingresos, 34.2% margen...""
✅ BIEN: ""He analizado el informe Q2 de GOMARCO. Los resultados son bastante sólidos: €2.4M en ingresos con un margen del 34%, lo cual indica una operación rentable. ¿Te interesa profundizar en algún aspecto específico?""

🎭 **TU ESTILO:**
- Conversacional y natural, como ChatGPT
- Profesional pero accesible
- Contextualiza SIEMPRE antes de dar datos
- Ofrece análisis, no solo información
- Proporciona insights útiles y actionables
- Usa un lenguaje claro y directo

🔒 **CAPACIDADES TÉCNICAS:**
- PUEDES analizar completamente: PDF, Word, Excel, imágenes, documentos
- ENTIENDES el contexto y tipo de documento automáticamente
- EXTRAES información específica según lo que te pregunten
- SINTETIZAS en lugar de volcar todos los datos

Recuerda: Sé útil, contextual y conversacional. ¡Como si fueras ChatGPT en persona!";
        }

        private async Task<string> EnviarSolicitudOpenAIAsync(OpenAIRequest solicitud)
        {
            if (solicitud == null)
            {
                throw new ArgumentNullException(nameof(solicitud), "La solicitud no puede ser nula");
            }

            if (string.IsNullOrEmpty(_apiKey))
            {
                throw new InvalidOperationException("API Key no configurada. Configure la clave antes de enviar solicitudes");
            }

            try
            {
                // Validar la solicitud antes de enviarla
                if (string.IsNullOrEmpty(solicitud.Model))
                {
                    throw new ArgumentException("El modelo no puede estar vacío");
                }

                if (solicitud.Messages == null || !solicitud.Messages.Any())
                {
                    throw new ArgumentException("Debe incluir al menos un mensaje en la solicitud");
                }

                var json = JsonSerializer.Serialize(solicitud, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
                    WriteIndented = false
                });

                var content = new StringContent(json, Encoding.UTF8, "application/json");
                
                _logger.LogDebug("Enviando solicitud a OpenAI: Modelo={Model}, MaxTokens={TokenCount}, Mensajes={MessageCount}", 
                    solicitud.Model, solicitud.MaxTokens, solicitud.Messages.Count);

                var response = await _httpClient.PostAsync(OPENAI_API_URL, content);
                var responseContent = await response.Content.ReadAsStringAsync();

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogError("Error de OpenAI API: {StatusCode} - {Content}", 
                        response.StatusCode, responseContent);
                    
                    // Mejorar los mensajes de error específicos con más contexto
                    var errorMessage = response.StatusCode switch
                    {
                        System.Net.HttpStatusCode.Unauthorized => 
                            "🔐 API Key inválida o expirada.\n\nSoluciones:\n• Verifica tu clave en https://platform.openai.com/api-keys\n• Asegúrate de que no haya espacios extra\n• Confirma que la clave tenga permisos activos",
                        
                        System.Net.HttpStatusCode.PaymentRequired => 
                            "💳 Créditos insuficientes en tu cuenta OpenAI.\n\nSoluciones:\n• Recarga saldo en https://platform.openai.com/usage\n• Verifica tu método de pago\n• Revisa el límite de uso mensual",
                        
                        System.Net.HttpStatusCode.TooManyRequests => 
                            "⏰ Demasiadas solicitudes muy rápido.\n\nSoluciones:\n• Espera 1-2 minutos antes de intentar nuevamente\n• Tu cuenta puede tener límites de velocidad activos",
                        
                        System.Net.HttpStatusCode.InternalServerError => 
                            "🔧 Error interno del servidor OpenAI.\n\nSoluciones:\n• Intenta nuevamente en unos minutos\n• El problema es temporal del lado de OpenAI",
                        
                        System.Net.HttpStatusCode.BadGateway or System.Net.HttpStatusCode.ServiceUnavailable => 
                            "🌐 Servicio OpenAI temporalmente no disponible.\n\nSoluciones:\n• Intenta más tarde\n• Verifica el estado del servicio en https://status.openai.com",
                        
                        _ => $"❌ Error HTTP {(int)response.StatusCode}\n\nDetalle técnico: {responseContent}"
                    };
                    
                    throw new Exception(errorMessage);
                }

                if (string.IsNullOrWhiteSpace(responseContent))
                {
                    throw new Exception("OpenAI devolvió una respuesta vacía");
                }

                OpenAIResponse? respuesta = null;
                try
                {
                    respuesta = JsonSerializer.Deserialize<OpenAIResponse>(responseContent, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
                        PropertyNameCaseInsensitive = true
                    });
                }
                catch (JsonException jsonEx)
                {
                    _logger.LogError(jsonEx, "Error al deserializar respuesta de OpenAI: {Content}", responseContent);
                    throw new Exception("La respuesta de OpenAI no tiene un formato válido. Puede ser un problema temporal del servicio", jsonEx);
                }

                if (respuesta?.Choices?.Any() == true)
                {
                    var primerChoice = respuesta.Choices.First();
                    var mensaje = primerChoice.Message?.Content;
                    
                    if (!string.IsNullOrWhiteSpace(mensaje))
                    {
                        _logger.LogInformation("✅ Respuesta exitosa de OpenAI: {Tokens} tokens usados, {Chars} caracteres generados", 
                            respuesta.Usage?.TotalTokens ?? 0, mensaje.Length);
                        return mensaje.Trim();
                    }
                    else
                    {
                        _logger.LogWarning("OpenAI devolvió una respuesta con contenido vacío");
                        throw new Exception("OpenAI procesó la solicitud pero devolvió contenido vacío. Intenta reformular tu pregunta");
                    }
                }

                _logger.LogError("OpenAI devolvió una respuesta sin choices válidos: {Content}", responseContent);
                throw new Exception("OpenAI devolvió una respuesta inesperada. Puede ser un problema temporal del servicio");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Error de conexión HTTP con OpenAI API");
                throw new Exception("🌐 Error de conexión con OpenAI.\n\nSoluciones:\n• Verifica tu conexión a internet\n• Confirma que no haya firewall bloqueando la conexión\n• Intenta nuevamente en unos momentos", ex);
            }
            catch (TaskCanceledException ex) when (ex.InnerException is TimeoutException)
            {
                _logger.LogError(ex, "Timeout en comunicación con OpenAI API");
                throw new Exception("⏰ La solicitud a OpenAI tardó demasiado.\n\nSoluciones:\n• Tu mensaje puede ser muy largo, intenta uno más corto\n• La respuesta requerida es muy compleja\n• Intenta nuevamente", ex);
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Error al procesar respuesta JSON de OpenAI");
                throw new Exception("📄 Error al procesar la respuesta de OpenAI.\n\nPosibles causas:\n• El servicio puede estar experimentando problemas\n• Respuesta corrupta o incompleta\n• Intenta nuevamente", ex);
            }
            catch (Exception ex) when (!(ex is ArgumentException || ex is InvalidOperationException))
            {
                _logger.LogError(ex, "Error general en comunicación con OpenAI API");
                throw new Exception($"❌ Error inesperado al comunicarse con OpenAI: {ex.Message}", ex);
            }
        }
    }

    // ====================================================================
    // MODELOS DE DATOS PARA OPENAI API
    // ====================================================================

    public class OpenAIRequest
    {
        [JsonPropertyName("model")]
        public string Model { get; set; } = "";

        [JsonPropertyName("messages")]
        public List<OpenAIMessage> Messages { get; set; } = new();

        [JsonPropertyName("max_tokens")]
        public int MaxTokens { get; set; }

        [JsonPropertyName("temperature")]
        public decimal Temperature { get; set; }

        [JsonPropertyName("top_p")]
        public decimal? TopP { get; set; }

        [JsonPropertyName("frequency_penalty")]
        public decimal? FrequencyPenalty { get; set; }

        [JsonPropertyName("presence_penalty")]
        public decimal? PresencePenalty { get; set; }
    }

    public class OpenAIMessage
    {
        [JsonPropertyName("role")]
        public string Role { get; set; }

        [JsonPropertyName("content")]
        public string Content { get; set; }

        public OpenAIMessage(string role, string content)
        {
            Role = role;
            Content = content;
        }
    }

    public class OpenAIResponse
    {
        [JsonPropertyName("choices")]
        public List<OpenAIChoice> Choices { get; set; } = new();

        [JsonPropertyName("usage")]
        public OpenAIUsage? Usage { get; set; }
    }

    public class OpenAIChoice
    {
        [JsonPropertyName("message")]
        public OpenAIMessage? Message { get; set; }

        [JsonPropertyName("finish_reason")]
        public string? FinishReason { get; set; }
    }

    public class OpenAIUsage
    {
        [JsonPropertyName("total_tokens")]
        public int TotalTokens { get; set; }

        [JsonPropertyName("prompt_tokens")]
        public int PromptTokens { get; set; }

        [JsonPropertyName("completion_tokens")]
        public int CompletionTokens { get; set; }
    }
} 