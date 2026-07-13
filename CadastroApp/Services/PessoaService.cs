using CadastroApp.Helpers;
using CadastroApp.Models;
using SQLite;


namespace CadastroApp.Services
{
    public class PessoaService
    {
        private SQLiteAsyncConnection? _database;
        private readonly AuthService _authService;

        public PessoaService(AuthService authService)
        {
            _authService = authService;
        }

        private string DatabasePath =>
            Path.Combine(FileSystem.AppDataDirectory, "cadastro_db3");

        private async Task Init()
        {
            if (_database is not null)
                return;

            _database = new SQLiteAsyncConnection(DatabasePath);

            await _database.CreateTableAsync<Usuario>();
            await _database.CreateTableAsync<Pessoa>();
        }

        private int ObterUsuarioId()
        {
            if (_authService.UsuarioLogado is null)
                return 0;

            return _authService.UsuarioLogado.Id;
        }

        public async Task<List<Pessoa>> ListarAsync()
        {
            await Init();

            var usuarioId = ObterUsuarioId();

            return await _database!.Table<Pessoa>()
                .Where(p => p.UsuarioId == usuarioId)
                .ToListAsync();
        }

        public async Task<Pessoa?> BuscarPorIdAsync(int id)
        {
            await Init();

            var usuarioId = ObterUsuarioId();

            return await _database!.Table<Pessoa>()
                .Where(p => p.Id == id && p.UsuarioId == usuarioId)
                .FirstOrDefaultAsync();
        }

        public async Task<bool> CpfJaExisteAsync(string cpf, int idAtual = 0)
        {
            await Init();

            var usuarioId = ObterUsuarioId();
            var cpfNumeros = CpfHelper.SomenteNumeros(cpf);

            var pessoas = await _database!.Table<Pessoa>()
                .Where(p => p.UsuarioId == usuarioId)
                .ToListAsync();

            return pessoas.Any(p =>
                CpfHelper.SomenteNumeros(p.CPF) == cpfNumeros &&
                p.Id != idAtual
            );
        }

        public async Task<int> SalvarAsync(Pessoa pessoa)
        {
            await Init();

            var usuarioId = ObterUsuarioId();

            if (usuarioId == 0)
                throw new Exception("Nenhum usuário logado.");

            pessoa.UsuarioId = usuarioId;

            if (pessoa.Id != 0)
            {
                return await _database!.UpdateAsync(pessoa);
            }
            else
            {
                return await _database!.InsertAsync(pessoa);
            }
        }

        public async Task<int> ExcluirAsync(Pessoa pessoa)
        {
            await Init();

            var usuarioId = ObterUsuarioId();

            if (pessoa.UsuarioId != usuarioId)
                throw new Exception("Este cadastro não pertence ao usuário logado.");

            return await _database!.DeleteAsync(pessoa);
        }

        public async Task<string> ObterCaminhoBancoAsync()
        {
            await Init();

            return DatabasePath;
        }

        public async Task FecharConexaoAsync()
        {
            if (_database is not null)
            {
                await _database.CloseAsync();
                _database = null;
            }
        }

        public async Task RestaurarBackupAsync(Stream backupStream)
        {
            await FecharConexaoAsync();

            using var arquivoDestino = File.Create(DatabasePath);

            await backupStream.CopyToAsync(arquivoDestino);

            await Init();
        }
    }
}