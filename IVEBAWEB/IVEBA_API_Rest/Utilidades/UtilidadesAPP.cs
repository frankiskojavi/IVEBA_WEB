namespace IVEBA_API_Rest.Utilidades
{
    public class UtilidadesAPP
    {
        public string FormatearFecha(int fechaEntero)
        {
            string fechaString = fechaEntero.ToString();
            string mesDia = fechaString.Substring(2, 4);
            return mesDia;
        }

        public string FormateoString(string stringAFormatear, int digitos, string relleno, string orientacion = "")
        {
            stringAFormatear = stringAFormatear.Trim();
            if (stringAFormatear.Length <= digitos)
            {
                return orientacion == "SI"
                    ? stringAFormatear.PadRight(digitos, char.Parse(relleno))
                    : stringAFormatear.PadLeft(digitos, char.Parse(relleno));
            }
            else
            {
                return stringAFormatear.Substring(0, digitos);
            }
        }

        public string FormateoMontos(string montoATransformar)
        {
            if (decimal.TryParse(montoATransformar, out decimal monto))
            {
                return monto.ToString("0.00");
            }
            return "0.00";
        }

        public string QuitoTildes(string stringQuitar)
        {
            return stringQuitar
                .Replace("Á", "A")
                .Replace("É", "E")
                .Replace("Í", "I")
                .Replace("Ó", "O")
                .Replace("Ú", "U")
                .Replace("-", " ")
                .Replace("/", " ")
                .Replace("$", " ")
                .Replace("&", " ");
        }

        public string QuitoCaracter(string stringQuitar)
        {
            string stringTemporal = stringQuitar.Replace(",", "").Replace("-", "");
            return stringTemporal;
        }
    }
}
