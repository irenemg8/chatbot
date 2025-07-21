using System.Threading.Tasks;

namespace ChatbotGomarco.Servicios
{
    public interface IServicioCifrado
    {
        /// <summary>
        /// Cifra un archivo de forma segura usando AES-256
        /// </summary>
        /// <param name="rutaArchivoOriginal">Ruta del archivo original</param>
        /// <param name="rutaArchivoCifrado">Ruta donde guardar el archivo cifrado</param>
        /// <returns>Vector de inicialización usado para el cifrado</returns>
        Task<string> CifrarArchivoAsync(string rutaArchivoOriginal, string rutaArchivoCifrado);

        /// <summary>
        /// Descifra un archivo previamente cifrado
        /// </summary>
        /// <param name="rutaArchivoCifrado">Ruta del archivo cifrado</param>
        /// <param name="rutaArchivoDescifrado">Ruta donde guardar el archivo descifrado</param>
        /// <param name="vectorInicializacion">Vector de inicialización original</param>
        Task DescifrarArchivoAsync(string rutaArchivoCifrado, string rutaArchivoDescifrado, string vectorInicializacion);

        /// <summary>
        /// Calcula el hash SHA-256 de un archivo para verificación de integridad
        /// </summary>
        /// <param name="rutaArchivo">Ruta del archivo</param>
        /// <returns>Hash SHA-256 en formato hexadecimal</returns>
        Task<string> CalcularHashArchivoAsync(string rutaArchivo);

        /// <summary>
        /// Genera una clave de cifrado segura basada en datos de la máquina
        /// </summary>
        /// <returns>Clave de cifrado de 256 bits</returns>
        byte[] GenerarClaveCifrado();
    }
} 