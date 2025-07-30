using System.Threading.Tasks;
using System.Collections.Generic;

namespace ChatbotGomarco.Servicios
{
    /// <summary>
    /// Interfaz para análisis especializado de facturas
    /// </summary>
    public interface IAnalizadorFacturas
    {
        /// <summary>
        /// Detecta si el contenido es una factura
        /// </summary>
        bool EsFactura(string contenido);

        /// <summary>
        /// Analiza una factura extrayendo información estructurada
        /// </summary>
        Task<AnalisisFactura> AnalizarFacturaAsync(string contenidoFactura, string? preguntaEspecifica = null);

        /// <summary>
        /// Genera un prompt optimizado para análisis de facturas según el proveedor de IA
        /// </summary>
        string GenerarPromptAnalisisFactura(string contenidoFactura, string pregunta, TipoProveedorIA tipoProveedor);
    }

    /// <summary>
    /// Resultado del análisis de una factura
    /// </summary>
    public class AnalisisFactura
    {
        public bool EsFacturaValida { get; set; }
        public string NumeroFactura { get; set; } = "";
        public string Fecha { get; set; } = "";
        public string Proveedor { get; set; } = "";
        public string Cliente { get; set; } = "";
        public decimal? Total { get; set; }
        public decimal? Subtotal { get; set; }
        public decimal? Impuestos { get; set; }
        public List<ItemFactura> Items { get; set; } = new();
        public string Moneda { get; set; } = "";
        public string FechaVencimiento { get; set; } = "";
        public string AnalisisCompleto { get; set; } = "";
        public List<string> DatosAdicionales { get; set; } = new();
    }

    /// <summary>
    /// Item individual de una factura
    /// </summary>
    public class ItemFactura
    {
        public string Descripcion { get; set; } = "";
        public int Cantidad { get; set; }
        public decimal? PrecioUnitario { get; set; }
        public decimal? Total { get; set; }
        public string Codigo { get; set; } = "";
    }

    /// <summary>
    /// Tipo de proveedor de IA para optimizar prompts
    /// </summary>
    public enum TipoProveedorIA
    {
        DeepSeek,    // Razonamiento paso a paso
        Claude,      // Conversacional y natural  
        OpenAI,      // Máxima potencia
        Ollama       // Rápido y eficiente
    }
}