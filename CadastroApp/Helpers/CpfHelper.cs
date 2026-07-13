namespace CadastroApp.Helpers
{
    public static class CpfHelper
    {
        public static string SomenteNumeros(string cpf)
        {
            if (string.IsNullOrWhiteSpace(cpf))
                return string.Empty;

            return new string(cpf.Where(char.IsDigit).ToArray());
        }

        public static bool ValidarCpf(string cpf)
        {
            var numeros = SomenteNumeros(cpf);

            if (numeros.Length != 11)
                return false;

            // Bloqueia CPFs com todos os números iguais: 00000000000, 11111111111 etc.
            if (numeros.All(c => c == numeros[0]))
                return false;

            // Calcula o primeiro dígito verificador
            int soma = 0;

            for (int i = 0; i < 9; i++)
            {
                soma += (numeros[i] - '0') * (10 - i);
            }

            int resto = soma % 11;
            int primeiroDigito = resto < 2 ? 0 : 11 - resto;

            if ((numeros[9] - '0') != primeiroDigito)
                return false;

            // Calcula o segundo dígito verificador
            soma = 0;

            for (int i = 0; i < 10; i++)
            {
                soma += (numeros[i] - '0') * (11 - i);
            }

            resto = soma % 11;
            int segundoDigito = resto < 2 ? 0 : 11 - resto;

            if ((numeros[10] - '0') != segundoDigito)
                return false;

            return true;
        }

        public static string FormatarCpf(string cpf)
        {
            var numeros = SomenteNumeros(cpf);

            if (numeros.Length > 11)
                numeros = numeros.Substring(0, 11);

            if (numeros.Length <= 3)
                return numeros;

            if (numeros.Length <= 6)
                return $"{numeros.Substring(0, 3)}.{numeros.Substring(3)}";

            if (numeros.Length <= 9)
                return $"{numeros.Substring(0, 3)}.{numeros.Substring(3, 3)}.{numeros.Substring(6)}";

            return $"{numeros.Substring(0, 3)}.{numeros.Substring(3, 3)}.{numeros.Substring(6, 3)}-{numeros.Substring(9)}";
        }
    }
}