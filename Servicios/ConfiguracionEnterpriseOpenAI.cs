using System;
using System.Collections.Generic;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Configuración enterprise de OpenAI con controles avanzados de seguridad y compliance
    /// Implementa estándares corporativos para protección de datos sensibles
    /// </summary>
    public class ConfiguracionEnterpriseOpenAI
    {
        /// <summary>
        /// Configuración de retención de datos según política enterprise
        /// </summary>
        public class PoliticaRetencionDatos
        {
            /// <summary>Zero data retention - datos no almacenados por OpenAI</summary>
            public bool ZeroDataRetention { get; set; } = true;
            
            /// <summary>Días máximos de retención para monitoreo de abuso (máximo 30)</summary>
            public int DiasMaximosRetencion { get; set; } = 0;
            
            /// <summary>Deshabilitar uso de datos para entrenamiento de modelos</summary>
            public bool DeshabilitarEntrenamiento { get; set; } = true;
            
            /// <summary>Requerir eliminación explícita después del procesamiento</summary>
            public bool EliminacionExplicita { get; set; } = true;
        }

        /// <summary>
        /// Headers de seguridad enterprise para requests a OpenAI
        /// </summary>
        public class HeadersSeguridadEnterprise
        {
            /// <summary>Identificador único de la organización enterprise</summary>
            public string? OpenAIOrganization { get; set; }
            
            /// <summary>Project ID específico para facturación y tracking</summary>
            public string? OpenAIProject { get; set; }
            
            /// <summary>Header personalizado para compliance GDPR</summary>
            public string? DataResidency { get; set; } = "EU";
            
            /// <summary>Nivel de compliance requerido</summary>
            public string? ComplianceLevel { get; set; } = "ENTERPRISE";
            
            /// <summary>Identificador de sesión para auditoría</summary>
            public string? SessionTrackingId { get; set; }
            
            /// <summary>Indicador de contenido sensible</summary>
            public string? ContentSensitivityLevel { get; set; }
        }

        /// <summary>
        /// Configuración de modelos enterprise optimizados para seguridad
        /// </summary>
        public class ModelosEnterprise
        {
            /// <summary>Modelo principal para conversaciones generales</summary>
            public string ModeloPrincipal { get; set; } = "gpt-4o";
            
            /// <summary>Modelo para análisis de documentos sensibles</summary>
            public string ModeloAnalisisSensible { get; set; } = "gpt-4o";
            
            /// <summary>Modelo con capacidades de visión para OCR</summary>
            public string ModeloVision { get; set; } = "gpt-4o";
            
            /// <summary>Temperatura más conservadora para datos sensibles</summary>
            public decimal TemperaturaSegura { get; set; } = 0.1m;
            
            /// <summary>Tokens máximos para prevenir exposición excesiva</summary>
            public int MaxTokensSeguro { get; set; } = 8192;
            
            /// <summary>Top-p más restrictivo para outputs determinísticos</summary>
            public decimal TopPSeguro { get; set; } = 0.8m;
        }

        /// <summary>
        /// Configuración de logging y auditoría enterprise
        /// </summary>
        public class AuditoriaEnterprise
        {
            /// <summary>Habilitar logging detallado de todas las interacciones</summary>
            public bool LoggingDetallado { get; set; } = true;
            
            /// <summary>Registrar metadatos de sensibilidad de contenido</summary>
            public bool LogearSensibilidad { get; set; } = true;
            
            /// <summary>Almacenar hashes de contenido para tracking sin exponer datos</summary>
            public bool HashearContenido { get; set; } = true;
            
            /// <summary>Generar alertas para contenido ultra-sensible</summary>
            public bool AlertasContenidoCritico { get; set; } = true;
            
            /// <summary>Ruta para logs de auditoría</summary>
            public string RutaLogsAuditoria { get; set; } = "logs/auditoria-ia";
            
            /// <summary>Días de retención de logs locales</summary>
            public int DiasRetencionLogs { get; set; } = 365;
        }

        /// <summary>
        /// Políticas de fallback para contenido ultra-sensible
        /// </summary>
        public class PoliticasFallback
        {
            /// <summary>Procesar contenido ultra-sensible localmente</summary>
            public bool ProcesamientoLocalUltraSensible { get; set; } = true;
            
            /// <summary>Rechazar procesamiento si no se puede garantizar seguridad</summary>
            public bool RechazarSiNoSeguro { get; set; } = true;
            
            /// <summary>Requerir confirmación del usuario para contenido sensible</summary>
            public bool RequerirConfirmacionUsuario { get; set; } = false;
            
            /// <summary>Mensaje alternativo para contenido rechazado</summary>
            public string MensajeContenidoRechazado { get; set; } = 
                "Por políticas de seguridad, este contenido no puede procesarse con servicios externos. " +
                "Considere usar herramientas de análisis local para datos ultra-sensibles.";
        }

        /// <summary>
        /// Endpoints enterprise de OpenAI con mayor seguridad
        /// </summary>
        public class EndpointsEnterprise
        {
            /// <summary>URL base para API enterprise (puede ser endpoint dedicado)</summary>
            public string BaseUrl { get; set; } = "https://api.openai.com/v1";
            
            /// <summary>Endpoint específico para chat completions</summary>
            public string ChatCompletions { get; set; } = "/chat/completions";
            
            /// <summary>Timeout para requests enterprise (más conservador)</summary>
            public TimeSpan TimeoutRequest { get; set; } = TimeSpan.FromMinutes(2);
            
            /// <summary>Número máximo de reintentos</summary>
            public int MaxReintentos { get; set; } = 2;
            
            /// <summary>Delay entre reintentos</summary>
            public TimeSpan DelayEntreReintentos { get; set; } = TimeSpan.FromSeconds(5);
        }

        // Propiedades de configuración
        public PoliticaRetencionDatos Retencion { get; set; } = new();
        public HeadersSeguridadEnterprise HeadersSeguridad { get; set; } = new();
        public ModelosEnterprise Modelos { get; set; } = new();
        public AuditoriaEnterprise Auditoria { get; set; } = new();
        public PoliticasFallback Fallback { get; set; } = new();
        public EndpointsEnterprise Endpoints { get; set; } = new();

        /// <summary>
        /// Validar que la configuración cumple con estándares enterprise
        /// </summary>
        public List<string> ValidarConfiguracionEnterprise()
        {
            var errores = new List<string>();

            // Validar retención de datos
            if (!Retencion.ZeroDataRetention && Retencion.DiasMaximosRetencion > 30)
            {
                errores.Add("Enterprise: Días de retención no pueden exceder 30 días");
            }

            if (!Retencion.DeshabilitarEntrenamiento)
            {
                errores.Add("Enterprise: El entrenamiento con datos corporativos debe estar deshabilitado");
            }

            // Validar modelos de seguridad
            if (Modelos.TemperaturaSegura > 0.3m)
            {
                errores.Add("Enterprise: Temperatura para datos sensibles debe ser ≤ 0.3");
            }

            if (Modelos.MaxTokensSeguro > 16384)
            {
                errores.Add("Enterprise: Max tokens debe limitarse para reducir exposición");
            }

            // Validar configuración de auditoría
            if (!Auditoria.LoggingDetallado)
            {
                errores.Add("Enterprise: Logging detallado es obligatorio para compliance");
            }

            if (Auditoria.DiasRetencionLogs < 365)
            {
                errores.Add("Enterprise: Logs de auditoría deben retenerse al menos 365 días");
            }

            return errores;
        }

        /// <summary>
        /// Generar configuración enterprise por defecto con máxima seguridad
        /// </summary>
        public static ConfiguracionEnterpriseOpenAI CrearConfiguracionMaximaSeguridad()
        {
            return new ConfiguracionEnterpriseOpenAI
            {
                Retencion = new PoliticaRetencionDatos
                {
                    ZeroDataRetention = true,
                    DiasMaximosRetencion = 0,
                    DeshabilitarEntrenamiento = true,
                    EliminacionExplicita = true
                },
                HeadersSeguridad = new HeadersSeguridadEnterprise
                {
                    DataResidency = "EU",
                    ComplianceLevel = "ENTERPRISE_MAX",
                    ContentSensitivityLevel = "HIGH"
                },
                Modelos = new ModelosEnterprise
                {
                    TemperaturaSegura = 0.1m,
                    MaxTokensSeguro = 4096,
                    TopPSeguro = 0.7m
                },
                Auditoria = new AuditoriaEnterprise
                {
                    LoggingDetallado = true,
                    LogearSensibilidad = true,
                    HashearContenido = true,
                    AlertasContenidoCritico = true,
                    DiasRetencionLogs = 730 // 2 años
                },
                Fallback = new PoliticasFallback
                {
                    ProcesamientoLocalUltraSensible = true,  // ACTIVADO: Procesamiento local para datos ultra-sensibles
                    RechazarSiNoSeguro = false,              // Permitir procesamiento local en lugar de rechazar
                    RequerirConfirmacionUsuario = false,
                    MensajeContenidoRechazado = "Por políticas enterprise de zero data leakage, este contenido se procesa completamente en local sin transmisión externa."
                }
            };
        }
    }
} 