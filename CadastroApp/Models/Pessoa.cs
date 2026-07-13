using SQLite;
using System.ComponentModel.DataAnnotations;

namespace CadastroApp.Models
{
    public class Pessoa
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Required]
        public string Nome { get; set; } = string.Empty;

        [Required]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string CPF { get; set; } = string.Empty;

        public string Telefone { get; set; } = string.Empty;

        public DateTime DataCadastro { get; set; } = DateTime.Now;
        
        public int UsuarioId { get; set; }

        public string CEP { get; set; } = string.Empty;

        public string Cidade { get; set; } = string.Empty;

        public string Estado { get; set; } = string.Empty;

        public string Bairro { get; set; } = string.Empty;

        public string Rua { get; set; } = string.Empty;

        public string Numero { get; set; } = string.Empty;

    }
}