using System.Collections.Generic;
using System.Threading.Tasks;
using ChatbotGomarco.Modelos;

namespace ChatbotGomarco.Servicios.LLM
{
    public interface IGeneradorRespuestas
    {
        Task<string> GenerarRespuestaAnalisisDocumento(string pregunta, string contextoArchivos, string contextoCompleto);
        Task<string> GenerarConversacionProfunda(string mensaje, string contexto, List<MensajeChat>? historial);
        Task<string> GenerarRespuestaTecnica(string mensaje, string contextoArchivos, string contextoCompleto);
        Task<string> GenerarCharlaCasual(string mensaje, string contexto);
        Task<string> GenerarRespuestaIntegrativa(string mensaje, string contextoArchivos, string contextoCompleto, List<MensajeChat>? historial);
    }
} 