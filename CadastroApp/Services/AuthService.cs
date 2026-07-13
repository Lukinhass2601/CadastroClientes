using System.Security.Cryptography;
using System.Text;
using CadastroApp.Models;
using SQLite;

namespace CadastroApp.Services
{
    public class AuthService
    {
        private SQLiteAsyncConnection? _database;

        private string DatabasePath =>
            Path.Combine(FileSystem.AppDataDirectory, "cadastro_db3");

        public Usuario? UsuarioLogado { get; private set; }

        public bool EstaLogado => UsuarioLogado is not null;

        public bool UsuarioEhAdministrador =>
            UsuarioLogado is not null &&
            UsuarioLogado.Perfil == "Administrador";

        public bool UsuarioEhAdministradorPrincipal =>
            UsuarioLogado is not null &&
            UsuarioLogado.AdministradorPrincipal;

        private async Task Init()
        {
            if (_database is not null)
                return;

            _database = new SQLiteAsyncConnection(DatabasePath);

            await _database.CreateTableAsync<Usuario>();
        }

        public async Task<bool> ExisteAlgumUsuarioAsync()
        {
            await Init();

            var quantidade = await _database!.Table<Usuario>().CountAsync();

            return quantidade > 0;
        }

        public async Task<(bool sucesso, string mensagem)> CriarPrimeiroAdministradorAsync(
            string nome,
            string senha)
        {
            await Init();

            var existeUsuario = await ExisteAlgumUsuarioAsync();

            if (existeUsuario)
                return (false, "Já existe usuário cadastrado no sistema.");

            var nomeOriginal = nome.Trim();

            if (string.IsNullOrWhiteSpace(nomeOriginal))
                return (false, "Digite o nome do administrador.");

            if (string.IsNullOrWhiteSpace(senha))
                return (false, "Digite uma senha.");

            if (senha.Length < 4)
                return (false, "A senha deve ter pelo menos 4 caracteres.");

            var salt = GerarSalt();
            var hash = GerarHash(senha, salt);

            var usuario = new Usuario
            {
                Nome = nomeOriginal,
                Salt = salt,
                SenhaHash = hash,
                Perfil = "Administrador",
                AdministradorPrincipal = true,
                DataCriacao = DateTime.Now
            };

            await _database!.InsertAsync(usuario);

            return (true, "Administrador principal criado com sucesso.");
        }

        public async Task<(bool sucesso, string mensagem)> CriarUsuarioAsync(
            string nome,
            string senha,
            string perfil,
            string senhaAdministradorPrincipal = "")
        {
            await Init();

            var nomeOriginal = nome.Trim();
            var nomeComparacao = nomeOriginal.ToLower();

            if (string.IsNullOrWhiteSpace(nomeOriginal))
                return (false, "Digite o nome do usuário.");

            if (string.IsNullOrWhiteSpace(senha))
                return (false, "Digite uma senha.");

            if (senha.Length < 4)
                return (false, "A senha deve ter pelo menos 4 caracteres.");

            if (perfil != "Administrador" && perfil != "Comum")
                return (false, "Perfil de usuário inválido.");

            var usuarios = await _database!.Table<Usuario>().ToListAsync();

            bool usuarioJaExiste = usuarios.Any(u =>
                u.Nome.Trim().ToLower() == nomeComparacao
            );

            if (usuarioJaExiste)
                return (false, "Este usuário já existe.");

            if (perfil == "Administrador")
            {
                if (string.IsNullOrWhiteSpace(senhaAdministradorPrincipal))
                    return (false, "Digite a senha do administrador principal para criar outro administrador.");

                var adminPrincipal = usuarios.FirstOrDefault(u => u.AdministradorPrincipal);

                if (adminPrincipal is null)
                    return (false, "Administrador principal não encontrado.");

                var hashDigitado = GerarHash(
                    senhaAdministradorPrincipal,
                    adminPrincipal.Salt
                );

                if (hashDigitado != adminPrincipal.SenhaHash)
                    return (false, "Senha do administrador principal incorreta.");
            }

            var salt = GerarSalt();
            var hash = GerarHash(senha, salt);

            var usuario = new Usuario
            {
                Nome = nomeOriginal,
                Salt = salt,
                SenhaHash = hash,
                Perfil = perfil,
                AdministradorPrincipal = false,
                DataCriacao = DateTime.Now
            };

            await _database!.InsertAsync(usuario);

            return (true, "Usuário criado com sucesso.");
        }

        public async Task<(bool sucesso, string mensagem)> LoginAsync(
            string nome,
            string senha)
        {
            await Init();

            var nomeDigitado = nome.Trim().ToLower();

            if (string.IsNullOrWhiteSpace(nomeDigitado))
                return (false, "Digite o nome do usuário.");

            if (string.IsNullOrWhiteSpace(senha))
                return (false, "Digite a senha.");

            var usuarios = await _database!.Table<Usuario>().ToListAsync();

            var usuario = usuarios.FirstOrDefault(u =>
                u.Nome.Trim().ToLower() == nomeDigitado
            );

            if (usuario is null)
                return (false, "Usuário não encontrado.");

            var hashDigitado = GerarHash(senha, usuario.Salt);

            if (hashDigitado != usuario.SenhaHash)
                return (false, "Senha incorreta.");

            UsuarioLogado = usuario;

            return (true, "Login realizado com sucesso.");
        }

        public void Sair()
        {
            UsuarioLogado = null;
        }

        public async Task<List<Usuario>> ListarUsuariosAsync()
        {
            await Init();

            return await _database!.Table<Usuario>()
                .OrderBy(u => u.Nome)
                .ToListAsync();
        }

        public async Task<List<Usuario>> ListarUsuariosParaEdicaoAsync()
        {
            await Init();

            if (UsuarioLogado is null)
                return new List<Usuario>();

            // Administrador principal vê todos
            if (UsuarioEhAdministradorPrincipal)
            {
                return await _database!.Table<Usuario>()
                    .OrderBy(u => u.Nome)
                    .ToListAsync();
            }

            // Administrador secundário vê ele mesmo + usuários comuns
            if (UsuarioEhAdministrador)
            {
                var usuarios = await _database!.Table<Usuario>()
                    .ToListAsync();

                return usuarios
                    .Where(u => u.Id == UsuarioLogado.Id || u.Perfil == "Comum")
                    .OrderBy(u => u.Nome)
                    .ToList();
            }

            // Usuário comum vê somente ele mesmo
            var usuario = await _database!.Table<Usuario>()
                .Where(u => u.Id == UsuarioLogado.Id)
                .FirstOrDefaultAsync();

            if (usuario is null)
                return new List<Usuario>();

            return new List<Usuario> { usuario };
        }

        public async Task<(bool sucesso, string mensagem)> AtualizarUsuarioAsync(
    int id,
    string senhaUsuarioLogado,
    string novoNome,
    string novaSenha,
    string novoPerfil)
        {
            await Init();

            if (UsuarioLogado is null)
                return (false, "Usuário não autenticado.");

            if (string.IsNullOrWhiteSpace(senhaUsuarioLogado))
                return (false, "Digite sua senha para salvar alterações.");

            var hashSenhaLogado = GerarHash(
                senhaUsuarioLogado,
                UsuarioLogado.Salt
            );

            if (hashSenhaLogado != UsuarioLogado.SenhaHash)
                return (false, "Senha incorreta.");

            var usuario = await _database!.Table<Usuario>()
                .Where(u => u.Id == id)
                .FirstOrDefaultAsync();

            if (usuario is null)
                return (false, "Usuário não encontrado.");

            bool ehProprioUsuario = UsuarioLogado.Id == usuario.Id;
            bool usuarioLogadoEhAdminPrincipal = UsuarioEhAdministradorPrincipal;
            bool usuarioLogadoEhAdmin = UsuarioEhAdministrador;
            bool usuarioAlvoEhComum = usuario.Perfil == "Comum";
            bool usuarioAlvoEhAdministradorPrincipal = usuario.AdministradorPrincipal;

            // Permissões:
            // Admin principal edita todos.
            // Admin secundário edita ele mesmo ou usuário comum.
            // Usuário comum edita somente ele mesmo.
            if (!usuarioLogadoEhAdminPrincipal)
            {
                if (usuarioLogadoEhAdmin)
                {
                    if (!ehProprioUsuario && !usuarioAlvoEhComum)
                        return (false, "Você só pode editar seu próprio usuário ou usuários comuns.");
                }
                else
                {
                    if (!ehProprioUsuario)
                        return (false, "Você só pode editar seu próprio usuário.");
                }
            }

            var nomeOriginal = novoNome.Trim();
            var nomeComparacao = nomeOriginal.ToLower();

            if (string.IsNullOrWhiteSpace(nomeOriginal))
                return (false, "Digite o nome do usuário.");

            var usuarios = await _database!.Table<Usuario>().ToListAsync();

            bool usuarioComMesmoNome = usuarios.Any(u =>
                u.Id != id &&
                u.Nome.Trim().ToLower() == nomeComparacao
            );

            if (usuarioComMesmoNome)
                return (false, "Já existe outro usuário com este nome.");

            usuario.Nome = nomeOriginal;

            // Apenas administrador principal pode alterar perfil
            if (usuarioLogadoEhAdminPrincipal)
            {
                if (novoPerfil != "Administrador" && novoPerfil != "Comum")
                    return (false, "Perfil inválido.");

                // Protege o administrador principal para não deixar de ser administrador
                if (usuarioAlvoEhAdministradorPrincipal)
                {
                    usuario.Perfil = "Administrador";
                    usuario.AdministradorPrincipal = true;
                }
                else
                {
                    usuario.Perfil = novoPerfil;
                }
            }

            // Admin secundário e comum não alteram perfil
            // Mantém como já estava

            if (!string.IsNullOrWhiteSpace(novaSenha))
            {
                if (novaSenha.Length < 4)
                    return (false, "A nova senha deve ter pelo menos 4 caracteres.");

                var novoSalt = GerarSalt();
                var novoHash = GerarHash(novaSenha, novoSalt);

                usuario.Salt = novoSalt;
                usuario.SenhaHash = novoHash;
            }

            await _database!.UpdateAsync(usuario);

            if (UsuarioLogado.Id == usuario.Id)
            {
                UsuarioLogado = usuario;
            }

            return (true, "Usuário atualizado com sucesso.");
        }
        public async Task<(bool sucesso, string mensagem)> ExcluirUsuarioAsync(
    int id,
    string senhaUsuarioLogado)
        {
            await Init();

            if (UsuarioLogado is null)
                return (false, "Usuário não autenticado.");

            if (string.IsNullOrWhiteSpace(senhaUsuarioLogado))
                return (false, "Digite sua senha para excluir este usuário.");

            var hashSenhaLogado = GerarHash(
                senhaUsuarioLogado,
                UsuarioLogado.Salt
            );

            if (hashSenhaLogado != UsuarioLogado.SenhaHash)
                return (false, "Senha incorreta.");

            var usuarios = await _database!.Table<Usuario>().ToListAsync();

            if (usuarios.Count <= 1)
                return (false, "Não é possível excluir o último usuário do sistema.");

            var usuario = usuarios.FirstOrDefault(u => u.Id == id);

            if (usuario is null)
                return (false, "Usuário não encontrado.");

            if (usuario.Id == UsuarioLogado.Id)
                return (false, "Você não pode excluir o usuário que está logado.");

            if (usuario.AdministradorPrincipal)
                return (false, "Não é possível excluir o administrador principal.");

            // Administrador principal pode excluir usuários comuns e administradores secundários
            if (UsuarioEhAdministradorPrincipal)
            {
                await _database!.DeleteAsync(usuario);
                return (true, "Usuário excluído com sucesso.");
            }

            // Administrador secundário só pode excluir usuários comuns
            if (UsuarioEhAdministrador)
            {
                if (usuario.Perfil != "Comum")
                    return (false, "Administradores secundários só podem excluir usuários comuns.");

                await _database!.DeleteAsync(usuario);
                return (true, "Usuário comum excluído com sucesso.");
            }

            return (false, "Usuários comuns não podem excluir usuários.");
        }

        private static string GerarSalt()
        {
            var bytes = RandomNumberGenerator.GetBytes(16);
            return Convert.ToBase64String(bytes);
        }

        private static string GerarHash(string senha, string salt)
        {
            var texto = senha + salt;
            var bytes = Encoding.UTF8.GetBytes(texto);
            var hash = SHA256.HashData(bytes);

            return Convert.ToBase64String(hash);
        }
    }
}