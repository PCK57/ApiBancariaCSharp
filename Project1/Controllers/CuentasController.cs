using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using Project1.Data;

namespace Project1.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class CuentasController : ControllerBase
    {
        private readonly AppDbContext _context;

        public CuentasController(AppDbContext context)
        {
            _context = context;
        }

        private string FormatearNumeroCuenta(string numero)
        {
            if (string.IsNullOrEmpty(numero)) return string.Empty;
            if (numero.Contains("-")) return numero;
            if (numero.Length > 4)
            {
                return numero.Insert(4, "-");
            }
            return numero;
        }

        private string EnmascararNumeroCuenta(string numeroCuenta)
        {
            string cuentaLimpia = numeroCuenta.Replace("-", "");
            if (cuentaLimpia.Length <= 4) return cuentaLimpia;

            string ultimosCuatro = cuentaLimpia.Substring(cuentaLimpia.Length - 4);
            return "****-****-" + ultimosCuatro;
        }

        // 1. EL GET: Ordenado de la más reciente a la más antigua con fecha automática
        [HttpGet]
        public IActionResult ObtenerCuentas()
        {
            try
            {
                var cuentas = _context.Cuentas.OrderByDescending(c => c.FechaCreacion).ToList();

                var cuentasSeguras = cuentas.Select(c => new
                {
                    NumeroCuentaFormateado = FormatearNumeroCuenta(c.NumeroCuenta),
                    NumeroCuentaSeguro = EnmascararNumeroCuenta(c.NumeroCuenta),
                    TipoCuenta = c.TipoCuenta,
                    Titular = c.Titular,
                    Saldo = c.Saldo,
                    FechaCreacion = c.FechaCreacion.ToString("yyyy-MM-dd HH:mm:ss")
                });

                return Ok(cuentasSeguras);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Mensaje = "Error interno del servidor.", Detalle = ex.Message });
            }
        }

        // 2. EL POST: Recibe exclusivamente los 4 datos del cliente (sin fecha)
        [HttpPost]
        public IActionResult CrearCuenta([FromBody] ModeloCrearCuenta nuevaCuentaDto)
        {
            try
            {
                if (string.IsNullOrEmpty(nuevaCuentaDto.NumeroCuenta))
                {
                    return BadRequest(new { Mensaje = "Error: El número de cuenta es obligatorio." });
                }

                string numeroLimpio = nuevaCuentaDto.NumeroCuenta.Trim();

                if (numeroLimpio.Length < 8 || numeroLimpio.Length > 16)
                {
                    return BadRequest(new { Mensaje = "Error: El número de cuenta debe tener entre 8 y 16 caracteres." });
                }

                numeroLimpio = FormatearNumeroCuenta(numeroLimpio);

                if (nuevaCuentaDto.Saldo < 0)
                {
                    return BadRequest(new { Mensaje = "Error: El saldo inicial no puede ser negativo." });
                }

                if (nuevaCuentaDto.TipoCuenta != "Ahorros" && nuevaCuentaDto.TipoCuenta != "Corriente" && nuevaCuentaDto.TipoCuenta != "Empresarial")
                {
                    return BadRequest(new { Mensaje = "Error: El tipo de cuenta debe ser 'Ahorros', 'Corriente' o 'Empresarial'." });
                }

                var cuentaExistente = _context.Cuentas.Find(numeroLimpio);
                if (cuentaExistente != null)
                {
                    return BadRequest(new { Mensaje = "Error: Ya existe una cuenta con este mismo número." });
                }

                // Creamos la entidad real que va a la base de datos asignando la fecha de hoy solitos
                var nuevaCuenta = new ModeloCuenta
                {
                    NumeroCuenta = numeroLimpio,
                    TipoCuenta = nuevaCuentaDto.TipoCuenta,
                    Titular = nuevaCuentaDto.Titular,
                    Saldo = nuevaCuentaDto.Saldo,
                    FechaCreacion = DateTime.Now // ¡Automática por el servidor!
                };

                _context.Cuentas.Add(nuevaCuenta);
                _context.SaveChanges();

                return Ok(new
                {
                    Mensaje = "¡Cuenta creada con éxito!",
                    CuentaCreada = new
                    {
                        NumeroCuenta = nuevaCuenta.NumeroCuenta,
                        NumeroOculto = EnmascararNumeroCuenta(nuevaCuenta.NumeroCuenta),
                        TipoCuenta = nuevaCuenta.TipoCuenta,
                        Titular = nuevaCuenta.Titular,
                        Saldo = nuevaCuenta.Saldo,
                        FechaCreacion = nuevaCuenta.FechaCreacion.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Mensaje = "Error interno al intentar crear la cuenta.", Detalle = ex.Message });
            }
        }

        // 3. EL PUT: Actualiza los datos
        [HttpPut("{numeroCuenta}")]
        public IActionResult ActualizarCuenta(string numeroCuenta, [FromBody] ModeloActualizarCuenta cuentaActualizada)
        {
            try
            {
                numeroCuenta = FormatearNumeroCuenta(numeroCuenta);
                var cuentaDb = _context.Cuentas.Find(numeroCuenta);
                if (cuentaDb == null)
                {
                    return NotFound(new { Mensaje = "Error: No se encontró ninguna cuenta con ese número." });
                }

                cuentaDb.Titular = cuentaActualizada.Titular;
                cuentaDb.Saldo = cuentaActualizada.Saldo;

                if (!string.IsNullOrEmpty(cuentaActualizada.TipoCuenta))
                {
                    cuentaDb.TipoCuenta = cuentaActualizada.TipoCuenta;
                }

                _context.SaveChanges();

                return Ok(new { Mensaje = "¡Cuenta actualizada con éxito!", DatosActualizados = cuentaDb });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Mensaje = "Error interno al intentar actualizar.", Detalle = ex.Message });
            }
        }

        // 4. EL DELETE: Elimina una cuenta
        [HttpDelete("{numeroCuenta}")]
        public IActionResult EliminarCuenta(string numeroCuenta)
        {
            try
            {
                numeroCuenta = FormatearNumeroCuenta(numeroCuenta);
                var cuentaDb = _context.Cuentas.Find(numeroCuenta);
                if (cuentaDb == null)
                {
                    return NotFound(new { Mensaje = "Error: No se encontró la cuenta." });
                }

                _context.Cuentas.Remove(cuentaDb);
                _context.SaveChanges();

                return Ok(new { Mensaje = $"¡La cuenta fue eliminada con éxito!" });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Mensaje = "Error interno al intentar eliminar.", Detalle = ex.Message });
            }
        }

        // 5. TRANSFERENCIAS (Con validación personalizada de cuentas inexistentes y fecha)
        [HttpPost("transferir")]
        public IActionResult Transferir([FromBody] ModeloTransferencia transferencia)
        {
            try
            {
                transferencia.CuentaOrigen = FormatearNumeroCuenta(transferencia.CuentaOrigen);
                transferencia.CuentaDestino = FormatearNumeroCuenta(transferencia.CuentaDestino);

                var cuentaOrigen = _context.Cuentas.Find(transferencia.CuentaOrigen);
                if (cuentaOrigen == null)
                {
                    return BadRequest(new { Mensaje = "¡Pailas! Transferencia rechazada, la cuenta de origen no existe, por favor verifíquela." });
                }

                var cuentaDestino = _context.Cuentas.Find(transferencia.CuentaDestino);
                if (cuentaDestino == null)
                {
                    return BadRequest(new { Mensaje = "¡Pailas! Transferencia rechazada, la cuenta de destino no existe, por favor verifíquela." });
                }

                if (transferencia.CuentaOrigen == transferencia.CuentaDestino)
                {
                    return BadRequest(new { Mensaje = "Error: No puedes transferir a la misma cuenta." });
                }

                if (transferencia.Monto <= 0)
                {
                    return BadRequest(new { Mensaje = "Error: El monto debe ser mayor a cero." });
                }

                if (cuentaOrigen.Saldo < transferencia.Monto)
                {
                    return BadRequest(new { Mensaje = "Error: Saldo insuficiente." });
                }

                cuentaOrigen.Saldo -= transferencia.Monto;
                cuentaDestino.Saldo += transferencia.Monto;

                _context.SaveChanges();

                return Ok(new
                {
                    Mensaje = "¡Transferencia realizada con éxito!",
                    Origen = EnmascararNumeroCuenta(cuentaOrigen.NumeroCuenta),
                    NuevoSaldoOrigen = cuentaOrigen.Saldo,
                    Destino = EnmascararNumeroCuenta(cuentaDestino.NumeroCuenta),
                    NuevoSaldoDestino = cuentaDestino.Saldo,
                    FechaTransferencia = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Mensaje = "Error crítico en el servidor.", Detalle = ex.Message });
            }
        }

        // 6. REPORTE / RESUMEN FINANCIERO (JSON)
        [HttpGet("reporte-financiero")]
        public IActionResult ObtenerReporteFinanciero()
        {
            try
            {
                var cuentas = _context.Cuentas.ToList();

                if (cuentas.Count == 0)
                {
                    return Ok(new
                    {
                        Mensaje = "No hay cuentas registradas en el sistema para generar un reporte.",
                        TotalCuentas = 0,
                        DineroTotalBanco = 0m,
                        PromedioPorCuenta = 0m,
                        FechaReporte = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    });
                }

                int totalCuentas = cuentas.Count;
                decimal dineroTotal = cuentas.Sum(c => c.Saldo);
                decimal promedio = dineroTotal / totalCuentas;

                return Ok(new
                {
                    Mensaje = "¡Reporte financiero generado con éxito!",
                    Estadisticas = new
                    {
                        TotalCuentas = totalCuentas,
                        DineroTotalBanco = dineroTotal,
                        PromedioPorCuenta = Math.Round(promedio, 2),
                        FechaReporte = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")
                    }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Mensaje = "Error interno al generar el reporte financiero.", Detalle = ex.Message });
            }
        }
    }

    // Modelos de datos
    public class ModeloCuenta
    {
        [Key]
        public string NumeroCuenta { get; set; }
        public string TipoCuenta { get; set; }
        public string Titular { get; set; }
        public decimal Saldo { get; set; }
        public DateTime FechaCreacion { get; set; }
    }

    public class ModeloCrearCuenta
    {
        public string NumeroCuenta { get; set; }
        public string TipoCuenta { get; set; }
        public string Titular { get; set; }
        public decimal Saldo { get; set; }
    }

    public class ModeloActualizarCuenta
    {
        public string Titular { get; set; }
        public decimal Saldo { get; set; }
        public string TipoCuenta { get; set; }
    }

    public class ModeloTransferencia
    {
        public string CuentaOrigen { get; set; }
        public string CuentaDestino { get; set; }
        public decimal Monto { get; set; }
    }
}