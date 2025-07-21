using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace ChatbotGomarco.Servicios
{
    public class ServicioCifrado : IServicioCifrado
    {
        private readonly ILogger<ServicioCifrado> _logger;
        private readonly byte[] _claveMaestra;
        
        public ServicioCifrado(ILogger<ServicioCifrado> logger)
        {
            _logger = logger;
            _claveMaestra = GenerarClaveCifrado();
        }

        public async Task<string> CifrarArchivoAsync(string rutaArchivoOriginal, string rutaArchivoCifrado)
        {
            try
            {
                _logger.LogInformation("Iniciando cifrado de archivo: {Archivo}", rutaArchivoOriginal);

                Directory.CreateDirectory(Path.GetDirectoryName(rutaArchivoCifrado)!);

                using var aes = Aes.Create();
                aes.Key = _claveMaestra;
                aes.GenerateIV();

                var vectorInicializacion = Convert.ToBase64String(aes.IV);

                using var streamArchivoOriginal = File.OpenRead(rutaArchivoOriginal);
                using var streamArchivoCifrado = File.Create(rutaArchivoCifrado);
                using var cifrador = aes.CreateEncryptor();
                using var streamCifrado = new CryptoStream(streamArchivoCifrado, cifrador, CryptoStreamMode.Write);

                await streamArchivoOriginal.CopyToAsync(streamCifrado);

                _logger.LogInformation("Archivo cifrado exitosamente: {ArchivoCifrado}", rutaArchivoCifrado);
                return vectorInicializacion;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cifrar archivo: {Archivo}", rutaArchivoOriginal);
                throw;
            }
        }

        public async Task DescifrarArchivoAsync(string rutaArchivoCifrado, string rutaArchivoDescifrado, string vectorInicializacion)
        {
            try
            {
                _logger.LogInformation("Iniciando descifrado de archivo: {Archivo}", rutaArchivoCifrado);

                Directory.CreateDirectory(Path.GetDirectoryName(rutaArchivoDescifrado)!);

                using var aes = Aes.Create();
                aes.Key = _claveMaestra;
                aes.IV = Convert.FromBase64String(vectorInicializacion);

                using var streamArchivoCifrado = File.OpenRead(rutaArchivoCifrado);
                using var streamArchivoDescifrado = File.Create(rutaArchivoDescifrado);
                using var descifrador = aes.CreateDecryptor();
                using var streamDescifrado = new CryptoStream(streamArchivoCifrado, descifrador, CryptoStreamMode.Read);

                await streamDescifrado.CopyToAsync(streamArchivoDescifrado);

                _logger.LogInformation("Archivo descifrado exitosamente: {ArchivoDescifrado}", rutaArchivoDescifrado);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al descifrar archivo: {Archivo}", rutaArchivoCifrado);
                throw;
            }
        }

        public async Task<string> CalcularHashArchivoAsync(string rutaArchivo)
        {
            try
            {
                using var sha256 = SHA256.Create();
                using var stream = File.OpenRead(rutaArchivo);
                var hash = await Task.Run(() => sha256.ComputeHash(stream));
                return Convert.ToHexString(hash);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al calcular hash del archivo: {Archivo}", rutaArchivo);
                throw;
            }
        }

        public byte[] GenerarClaveCifrado()
        {
            // Genera una clave basada en datos únicos de la máquina para mayor seguridad
            var nombreMaquina = Environment.MachineName;
            var nombreUsuario = Environment.UserName;
            var datosUnicos = $"GOMARCO-{nombreMaquina}-{nombreUsuario}-CHATBOT-2024";
            
            using var sha256 = SHA256.Create();
            return sha256.ComputeHash(Encoding.UTF8.GetBytes(datosUnicos));
        }
    }
} 