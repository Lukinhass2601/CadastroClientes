using System.Text.Json.Serialization;

namespace CadastroApp.Models
{
    public class ViaCepResponse
    {
        [JsonPropertyName("cep")]
        public string Cep { get; set; } = string.Empty;

        [JsonPropertyName("bairro")]
        public string Bairro { get; set; }

        [JsonPropertyName("logradouro")]
        public string Logradouro { get; set; } = string.Empty;

        [JsonPropertyName("localidade")]
        public string Localidade { get; set; } = string.Empty;

        [JsonPropertyName("uf")]
        public string Uf { get; set; } = string.Empty;

        [JsonPropertyName("erro")]
        public bool Erro { get; set; }
    }
}