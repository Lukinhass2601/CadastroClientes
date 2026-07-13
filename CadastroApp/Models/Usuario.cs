using SQLite;

namespace CadastroApp.Models
{
    public class Usuario
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        [Indexed(Unique = true)]
        public string Nome { get; set; } = string.Empty;

        public string SenhaHash { get; set; } = string.Empty;

        public string Salt { get; set; } = string.Empty;

        public string Perfil { get; set; } = "Comum";

        public bool AdministradorPrincipal { get; set; } = false;

        public DateTime DataCriacao { get; set; } = DateTime.Now;
    }
}