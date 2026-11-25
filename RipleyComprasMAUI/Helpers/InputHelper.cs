using System;
using System.Globalization;
using System.Text;
using RipleyComprasMAUI.Models;

namespace RipleyComprasMAUI.Helpers;


public static class InputHelper
{
    private static readonly CultureInfo Cultura = CultureInfo.InvariantCulture;

    public const int LongitudCodigo = 13;
    public const int LongitudDni = 8;
    public const int CantidadMinima = 1;
    public const int CantidadMaxima = 99;
    public const decimal PrecioMaximo = 99999.99m;


    public static bool TryLeerEntero(string? texto, string campo, int minimo, int maximo, out int valor, out string error)
    {
        valor = 0;
        string limpio = (texto ?? "").Trim();

        if (limpio.Length == 0)
        {
            error = $"{campo}: escriba un número entero.";
            return false;
        }
        if (!EsEnteroConSigno(limpio))
        {
            error = $"{campo}: debe ser un número entero (sin decimales, espacios ni letras).";
            return false;
        }
        if (!int.TryParse(limpio, NumberStyles.AllowLeadingSign, Cultura, out valor))
        {
            error = $"{campo}: el número es demasiado grande.";      // desbordamiento de int
            return false;
        }
        if (valor < minimo || valor > maximo)
        {
            error = $"{campo}: debe estar entre {minimo} y {maximo}.";
            return false;
        }

        error = "";
        return true;
    }

    public static bool TryLeerDecimal(string? texto, string campo, decimal minimo, decimal maximo, out decimal valor, out string error)
    {
        valor = 0;
        if (!TryPrepararNumero(texto, campo, 2, out string normalizado, out error))
        {
            return false;
        }
        if (!decimal.TryParse(normalizado, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, Cultura, out valor))
        {
            error = $"{campo}: el número es demasiado grande.";
            return false;
        }
        if (valor < minimo || valor > maximo)
        {
            error = $"{campo}: debe estar entre {minimo.ToString("0.00", Cultura)} y {maximo.ToString("0.00", Cultura)}.";
            return false;
        }

        error = "";
        return true;
    }

    public static bool TryLeerDouble(string? texto, string campo, double minimo, double maximo, int maxDecimales, out double valor, out string error)
    {
        valor = 0;
        if (!TryPrepararNumero(texto, campo, maxDecimales, out string normalizado, out error))
        {
            return false;
        }
        if (!double.TryParse(normalizado, NumberStyles.AllowLeadingSign | NumberStyles.AllowDecimalPoint, Cultura, out valor)
            || !double.IsFinite(valor))
        {
            error = $"{campo}: no es un número válido.";
            return false;
        }
        if (valor < minimo || valor > maximo)
        {
            error = $"{campo}: debe estar entre {minimo.ToString("0.##", Cultura)} y {maximo.ToString("0.##", Cultura)}.";
            return false;
        }

        error = "";
        return true;
    }

    private static bool TryPrepararNumero(string? texto, string campo, int maxDecimales, out string normalizado, out string error)
    {
        normalizado = "";
        string limpio = (texto ?? "").Trim();
        if (limpio.Length == 0)
        {
            error = $"{campo}: escriba un número.";
            return false;
        }

        StringBuilder resultado = new StringBuilder();
        int separadores = 0;
        int digitosEnteros = 0;
        int decimales = 0;

        for (int i = 0; i < limpio.Length; i++)
        {
            char c = limpio[i];
            if ((c == '-' || c == '+') && i == 0)
            {
                resultado.Append(c);
                continue;
            }
            if (c == '.' || c == ',')
            {
                separadores++;
                if (separadores > 1)
                {
                    error = $"{campo}: use un solo separador decimal y no use separador de miles.";
                    return false;
                }
                resultado.Append('.');
                continue;
            }
            if (!EsDigitoAscii(c))
            {
                error = $"{campo}: solo se admiten dígitos y un separador decimal (punto o coma).";
                return false;
            }
            if (separadores == 0)
            {
                digitosEnteros++;
            }
            else
            {
                decimales++;
            }
            resultado.Append(c);
        }

        if (digitosEnteros == 0)
        {
            error = $"{campo}: escriba al menos un dígito antes del separador (por ejemplo, 0.5).";
            return false;
        }
        if (separadores == 1 && decimales == 0)
        {
            error = $"{campo}: complete los decimales después del separador.";
            return false;
        }
        if (decimales > maxDecimales)
        {
            error = $"{campo}: admite como máximo {maxDecimales} decimales.";
            return false;
        }

        normalizado = resultado.ToString();
        error = "";
        return true;
    }

    public static bool EsImporteValido(decimal valor)
    {
        return valor > 0 && valor <= PrecioMaximo && decimal.Round(valor, 2) == valor;
    }


    public static bool EsCodigoValido(string? codigo)
    {
        return codigo != null && codigo.Length == LongitudCodigo && SoloDigitosAscii(codigo);
    }

    public static bool TryValidarCodigo(string? texto, out string codigo, out string error)
    {
        codigo = (texto ?? "").Trim();
        if (codigo.Length == 0)
        {
            error = "Escriba el código de la prenda (13 dígitos).";
            return false;
        }
        if (!EsCodigoValido(codigo))
        {
            error = $"Código inválido: debe tener exactamente 13 dígitos (0-9); tiene {codigo.Length} caracteres.";
            return false;
        }

        error = "";
        return true;
    }

    public static bool EsDniValido(string? dni)
    {
        return dni != null && dni.Length == LongitudDni && SoloDigitosAscii(dni);
    }

    public static bool TryValidarDni(string? texto, out string dni, out string error)
    {
        dni = (texto ?? "").Trim();
        if (dni.Length == 0)
        {
            error = "Escriba el DNI de 8 dígitos.";
            return false;
        }
        if (!EsDniValido(dni))
        {
            error = "DNI inválido: debe tener exactamente 8 dígitos (0-9).";
            return false;
        }

        error = "";
        return true;
    }

    /// <summary>DNI opcional: vacío significa que el cliente no lo registró.</summary>
    public static bool TryValidarDniOpcional(string? texto, out string? dni, out string error)
    {
        dni = null;
        if (string.IsNullOrWhiteSpace(texto))
        {
            error = "";
            return true;
        }
        if (!TryValidarDni(texto, out string valido, out error))
        {
            return false;
        }
        dni = valido;
        return true;
    }

    public static bool TryValidarCorreo(string? texto, out string? correo, out string error)
    {
        correo = null;
        string limpio = (texto ?? "").Trim();
        if (limpio.Length == 0)
        {
            error = "";
            return true;
        }
        if (limpio.Length > 80)
        {
            error = "Correo inválido: admite como máximo 80 caracteres.";
            return false;
        }

        int arroba = limpio.IndexOf('@');
        bool unaSolaArroba = arroba > 0 && arroba == limpio.LastIndexOf('@');
        string dominio = unaSolaArroba ? limpio.Substring(arroba + 1) : "";
        bool dominioValido = dominio.Contains('.') && !dominio.StartsWith('.') && !dominio.EndsWith('.') && !dominio.Contains("..");

        if (!unaSolaArroba || !dominioValido || !SoloCaracteresDeCorreo(limpio))
        {
            error = "Correo inválido: use el formato nombre@dominio.com, sin espacios ni tildes.";
            return false;
        }

        correo = limpio;
        error = "";
        return true;
    }

    public static bool TryValidarNombrePersona(string? texto, out string? nombre, out string error)
    {
        nombre = null;
        string limpio = UnirEspacios(texto);
        if (limpio.Length == 0)
        {
            error = "";
            return true;
        }
        if (limpio.Length < 2 || limpio.Length > 60)
        {
            error = "Nombre: debe tener entre 2 y 60 caracteres.";
            return false;
        }
        if (!char.IsLetter(limpio[0]))
        {
            error = "Nombre: debe empezar con una letra.";
            return false;
        }
        foreach (char c in limpio)
        {
            bool permitido = char.IsLetter(c) || c == ' ' || c == '-' || c == '\'' || c == '’' || EsMarcaDiacritica(c);
            if (!permitido)
            {
                error = $"Nombre: el carácter '{c}' no está permitido (use letras, espacios, guion o apóstrofo).";
                return false;
            }
        }

        nombre = limpio;
        error = "";
        return true;
    }

    public static bool TryValidarNombreProducto(string? texto, out string nombre, out string error)
    {
        nombre = UnirEspacios(texto);
        if (nombre.Length == 0 || nombre.Length > 60)
        {
            error = "el nombre está vacío o supera 60 caracteres";
            return false;
        }
        foreach (char c in nombre)
        {
            if (char.IsControl(c))
            {
                error = "el nombre tiene caracteres de control";
                return false;
            }
        }

        error = "";
        return true;
    }

    public static bool TryValidarTextoBusqueda(string? texto, out string consulta, out string error)
    {
        consulta = UnirEspacios(texto);
        if (consulta.Length == 0)
        {
            error = "Escriba un nombre o parte del nombre para buscar.";
            return false;
        }
        if (consulta.Length > 40)
        {
            error = "La búsqueda admite como máximo 40 caracteres.";
            return false;
        }
        foreach (char c in consulta)
        {
            bool permitido = char.IsLetterOrDigit(c) || c == ' ' || EsMarcaDiacritica(c) || "-./&'()#".IndexOf(c) >= 0;
            if (!permitido)
            {
                error = $"La búsqueda no admite el carácter '{c}'.";
                return false;
            }
        }

        error = "";
        return true;
    }

    public static bool TryLeerTarjeta(int indiceSeleccionado, out TipoTarjeta tarjeta, out string error)
    {
        switch (indiceSeleccionado)
        {
            case 0:
                tarjeta = TipoTarjeta.TarjetaRipley;
                error = "";
                return true;
            case 1:
                tarjeta = TipoTarjeta.OtraTarjeta;
                error = "";
                return true;
            default:
                tarjeta = TipoTarjeta.SinSeleccionar;
                error = "Seleccione Tarjeta Ripley u Otra tarjeta.";
                return false;
        }
    }

    public static bool EsTarjetaPermitida(TipoTarjeta tarjeta)
    {
        return tarjeta == TipoTarjeta.TarjetaRipley || tarjeta == TipoTarjeta.OtraTarjeta;
    }


    public static string NormalizarParaBusqueda(string? texto)
    {
        string descompuesto = (texto ?? "").Normalize(NormalizationForm.FormD);
        StringBuilder resultado = new StringBuilder(descompuesto.Length);
        bool espacioPendiente = false;

        foreach (char c in descompuesto)
        {
            if (EsMarcaDiacritica(c))
            {
                continue;                                    
            }
            if (char.IsWhiteSpace(c))
            {
                espacioPendiente = resultado.Length > 0;
                continue;   // el espacio se agrega una sola vez, antes de la siguiente letra
            }
            if (espacioPendiente)
            {
                resultado.Append(' ');
                espacioPendiente = false;
            }
            resultado.Append(char.ToUpperInvariant(c));
        }

        return resultado.ToString();
    }

    public static bool NombreContiene(string? nombre, string? consulta)
    {
        string consultaNormal = NormalizarParaBusqueda(consulta);
        if (consultaNormal.Length == 0)
        {
            return false;
        }
        return NormalizarParaBusqueda(nombre).Contains(consultaNormal, StringComparison.Ordinal);
    }


    public static string FormatearSoles(decimal monto)
    {
        return "S/ " + monto.ToString("0.00", Cultura);
    }

    public static string FormatearFechaHora(DateTime fecha)
    {
        return fecha.ToString("dd/MM/yyyy HH:mm:ss", Cultura);
    }

    public static string FormatearFecha(DateTime fecha)
    {
        return fecha.ToString("dd/MM/yyyy", Cultura);
    }

    public static string OcultarDni(string? dni)
    {
        if (dni == null || !EsDniValido(dni))
        {
            return "sin DNI";
        }
        return dni[0] + new string('x', 5) + dni.Substring(6);
    }

    public static string CrearRegistro(string accion, string referencia, string resultado)
    {
        return $"{FormatearFechaHora(DateTime.Now)} | {accion} | {referencia} | {resultado}";
    }


    private static bool EsDigitoAscii(char c)
    {
        return c >= '0' && c <= '9';
    }

    private static bool SoloDigitosAscii(string texto)
    {
        foreach (char c in texto)
        {
            if (!EsDigitoAscii(c))
            {
                return false;
            }
        }
        return true;
    }

    private static bool EsEnteroConSigno(string texto)
    {
        int inicio = (texto[0] == '-' || texto[0] == '+') ? 1 : 0;
        if (inicio == texto.Length)
        {
            return false;                                  
        }
        for (int i = inicio; i < texto.Length; i++)
        {
            if (!EsDigitoAscii(texto[i]))
            {
                return false;
            }
        }
        return true;
    }

    private static bool SoloCaracteresDeCorreo(string texto)
    {
        foreach (char c in texto)
        {
            bool permitido = char.IsAsciiLetterOrDigit(c) || c == '@' || c == '.' || c == '_' || c == '-' || c == '+';
            if (!permitido)
            {
                return false;
            }
        }
        return true;
    }

    private static bool EsMarcaDiacritica(char c)
    {
        return CharUnicodeInfo.GetUnicodeCategory(c) == UnicodeCategory.NonSpacingMark;
    }

    private static string UnirEspacios(string? texto)
    {
        StringBuilder resultado = new StringBuilder();
        bool espacioPendiente = false;
        foreach (char c in texto ?? "")
        {
            if (char.IsWhiteSpace(c))
            {
                espacioPendiente = resultado.Length > 0;
                continue;
            }
            if (espacioPendiente)
            {
                resultado.Append(' ');
                espacioPendiente = false;
            }
            resultado.Append(c);
        }
        return resultado.ToString();
    }
}
