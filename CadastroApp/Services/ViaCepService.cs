using System.Net.Http.Json;
using CadastroApp.Models;

namespace CadastroApp.Services
{
    public class ViaCepService
    {
        private readonly HttpClient _httpClient = new();

        public async Task<ViaCepResponse?> BuscarCepAsync(string cep)
        {
            var cepNumeros = new string(
                (cep ?? string.Empty).Where(char.IsDigit).ToArray()
            );

            if (cepNumeros.Length != 8)
            {
                return null;
            }

            var url = $"https://viacep.com.br/ws/{cepNumeros}/json/";

            var resposta = await _httpClient.GetFromJsonAsync<ViaCepResponse>(url);

            if (resposta is null || resposta.Erro)
            {
                return null;
            }

            return resposta;
        }
    }
}