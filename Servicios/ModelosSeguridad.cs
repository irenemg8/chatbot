using System.Collections.Generic;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Estrategias de procesamiento según el nivel de sensibilidad del contenido
    /// Define cómo y dónde se procesa la información según políticas de seguridad
    /// </summary>
    public enum EstrategiaProcesamiento
    {
        /// <summary>Procesamiento con OpenAI API estándar</summary>
        OpenAIEstandar,
        
        /// <summary>Procesamiento con OpenAI Enterprise con headers de seguridad</summary>
        OpenAIEnterprise,
        
        /// <summary>Procesamiento con OpenAI Enterprise y configuración ultra-segura</summary>
        OpenAIEnterpriseSeguro,
        
        /// <summary>Procesamiento local únicamente (no enviar a servicios externos)</summary>
        ProcesamientoLocal,
        
        /// <summary>Procesamiento rechazado por políticas de seguridad</summary>
        Rechazado
    }

    /// <summary>
    /// Resultado del procesamiento seguro de contenido con metadatos de seguridad
    /// Incluye información de anonimización y decisiones de estrategia
    /// </summary>
    public class ResultadoProcesamientoSeguro
    {
        /// <summary>Contenido anonimizado listo para procesamiento</summary>
        public string ContenidoAnonimizado { get; set; } = string.Empty;
        
        /// <summary>Mapa para restaurar datos anonimizados si es necesario</summary>
        public Dictionary<string, string> MapaAnonimizacion { get; set; } = new();
        
        /// <summary>Nivel de sensibilidad detectado en el contenido</summary>
        public NivelSensibilidad NivelSensibilidad { get; set; }
        
        /// <summary>Estrategia de procesamiento determinada</summary>
        public EstrategiaProcesamiento EstrategiaProcesamiento { get; set; }
        
        /// <summary>Indica si el contenido puede procesarse según políticas enterprise</summary>
        public bool PuedeProceaarse { get; set; } = true;
        
        /// <summary>Mensaje explicativo en caso de rechazo</summary>
        public string? MensajeRechazo { get; set; }
        
        /// <summary>Metadatos adicionales de auditoría</summary>
        public Dictionary<string, object> MetadatosAuditoria { get; set; } = new();
    }

    /// <summary>
    /// Configuración específica por estrategia de procesamiento
    /// Define parámetros optimizados para cada nivel de seguridad
    /// </summary>
    public class ConfiguracionPorEstrategia
    {
        /// <summary>Modelo de IA a utilizar</summary>
        public string Modelo { get; set; } = "gpt-4o";
        
        /// <summary>Temperatura para controlar determinismo</summary>
        public decimal Temperatura { get; set; } = 0.3m;
        
        /// <summary>Máximo de tokens para limitar exposición</summary>
        public int MaxTokens { get; set; } = 8192;
        
        /// <summary>Top-p para control de variabilidad</summary>
        public decimal TopP { get; set; } = 0.9m;
        
        /// <summary>Headers adicionales específicos para esta estrategia</summary>
        public Dictionary<string, string> HeadersAdicionales { get; set; } = new();
        
        /// <summary>Configuración específica para esta estrategia</summary>
        public static Dictionary<EstrategiaProcesamiento, ConfiguracionPorEstrategia> ObtenerConfiguraciones()
        {
            return new Dictionary<EstrategiaProcesamiento, ConfiguracionPorEstrategia>
            {
                [EstrategiaProcesamiento.OpenAIEstandar] = new()
                {
                    Modelo = "gpt-4o",
                    Temperatura = 0.7m,
                    MaxTokens = 16384,
                    TopP = 1.0m
                },
                [EstrategiaProcesamiento.OpenAIEnterprise] = new()
                {
                    Modelo = "gpt-4o",
                    Temperatura = 0.3m,
                    MaxTokens = 8192,
                    TopP = 0.9m,
                    HeadersAdicionales = new Dictionary<string, string>
                    {
                        ["X-Content-Level"] = "INTERNAL"
                    }
                },
                [EstrategiaProcesamiento.OpenAIEnterpriseSeguro] = new()
                {
                    Modelo = "gpt-4o",
                    Temperatura = 0.1m,
                    MaxTokens = 4096,
                    TopP = 0.8m,
                    HeadersAdicionales = new Dictionary<string, string>
                    {
                        ["X-Content-Level"] = "CONFIDENTIAL",
                        ["X-Processing-Mode"] = "SECURE"
                    }
                }
            };
        }
    }
} 