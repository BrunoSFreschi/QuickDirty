using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configurar o banco de dados SQLite
builder.Services.AddDbContext<BancoDeDados>(options =>
    options.UseSqlite("Data Source=pessoas.db"));

var app = builder.Build();

// Criar o banco de dados automaticamente
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BancoDeDados>();
    db.Database.EnsureCreated();
}

// === ROTAS DE PESSOAS ===

// Cadastrar uma pessoa com todos os dados de uma vez
app.MapPost("/pessoas", async (Pessoa pessoa, BancoDeDados db) =>
{
    // O Entity Framework automaticamente salva telefones, emails e endereço junto
    db.Pessoas.Add(pessoa);
    await db.SaveChangesAsync();
    return Results.Created($"/pessoas/{pessoa.Id}", pessoa);
});

// Buscar pessoa por ID (com todos os dados relacionados)
app.MapGet("/pessoas/{id}", async (int id, BancoDeDados db) =>
{
    var pessoa = await db.Pessoas
        .Include(p => p.Telefones)
        .Include(p => p.Emails)
        .Include(p => p.Endereco)
        .FirstOrDefaultAsync(p => p.Id == id);

    if (pessoa == null)
        return Results.NotFound("Pessoa não encontrada");

    return Results.Ok(pessoa);
});

// Listar todas as pessoas
app.MapGet("/pessoas", async (BancoDeDados db) =>
{
    var pessoas = await db.Pessoas
        .Include(p => p.Telefones)
        .Include(p => p.Emails)
        .Include(p => p.Endereco)
        .ToListAsync();
    return Results.Ok(pessoas);
});

// === ROTAS DE TELEFONES ===

// Adicionar telefone para uma pessoa
app.MapPost("/pessoas/{pessoaId}/telefones", async (int pessoaId, Telefone telefone, BancoDeDados db) =>
{
    var pessoa = await db.Pessoas.FindAsync(pessoaId);
    if (pessoa == null)
        return Results.NotFound("Pessoa não encontrada");

    telefone.PessoaId = pessoaId;
    db.Telefones.Add(telefone);
    await db.SaveChangesAsync();
    return Results.Created($"/pessoas/{pessoaId}/telefones/{telefone.Id}", telefone);
});

// === ROTAS DE EMAILS ===

// Adicionar email para uma pessoa
app.MapPost("/pessoas/{pessoaId}/emails", async (int pessoaId, Email email, BancoDeDados db) =>
{
    var pessoa = await db.Pessoas.FindAsync(pessoaId);
    if (pessoa == null)
        return Results.NotFound("Pessoa não encontrada");

    email.PessoaId = pessoaId;
    db.Emails.Add(email);
    await db.SaveChangesAsync();
    return Results.Created($"/pessoas/{pessoaId}/emails/{email.Id}", email);
});

// === ROTAS DE ENDEREÇO ===

// Adicionar ou atualizar endereço de uma pessoa
app.MapPost("/pessoas/{pessoaId}/endereco", async (int pessoaId, Endereco endereco, BancoDeDados db) =>
{
    var pessoa = await db.Pessoas.FindAsync(pessoaId);
    if (pessoa == null)
        return Results.NotFound("Pessoa não encontrada");

    // Verificar se já existe endereço
    var enderecoExistente = await db.Enderecos.FirstOrDefaultAsync(e => e.PessoaId == pessoaId);

    if (enderecoExistente != null)
    {
        // Atualizar endereço existente
        enderecoExistente.Rua = endereco.Rua;
        enderecoExistente.Numero = endereco.Numero;
        enderecoExistente.Cidade = endereco.Cidade;
        enderecoExistente.Estado = endereco.Estado;
        enderecoExistente.CEP = endereco.CEP;
    }
    else
    {
        // Criar novo endereço
        endereco.PessoaId = pessoaId;
        db.Enderecos.Add(endereco);
    }

    await db.SaveChangesAsync();
    return Results.Ok(enderecoExistente ?? endereco);
});

app.Run();

// === CLASSES (MODELOS) ===

// Classe que representa uma Pessoa
public class Pessoa
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public int Idade { get; set; }

    // Relacionamentos (uma pessoa pode ter vários telefones, emails e um endereço)
    public List<Telefone> Telefones { get; set; } = new();
    public List<Email> Emails { get; set; } = new();
    public Endereco? Endereco { get; set; }
}

// Classe que representa um Telefone
public class Telefone
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";
    public string Tipo { get; set; } = ""; // Ex: Celular, Fixo, Trabalho

    // Chave estrangeira - liga com a Pessoa
    public int PessoaId { get; set; }
}

// Classe que representa um Email
public class Email
{
    public int Id { get; set; }
    public string EnderecoEmail { get; set; } = "";
    public string Tipo { get; set; } = ""; // Ex: Pessoal, Trabalho

    // Chave estrangeira - liga com a Pessoa
    public int PessoaId { get; set; }
}

// Classe que representa um Endereço
public class Endereco
{
    public int Id { get; set; }
    public string Rua { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Estado { get; set; } = "";
    public string CEP { get; set; } = "";

    // Chave estrangeira - liga com a Pessoa (uma pessoa tem apenas um endereço)
    public int PessoaId { get; set; }
}

// Classe que representa o banco de dados
public class BancoDeDados : DbContext
{
    public BancoDeDados(DbContextOptions<BancoDeDados> options) : base(options) { }

    public DbSet<Pessoa> Pessoas { get; set; }
    public DbSet<Telefone> Telefones { get; set; }
    public DbSet<Email> Emails { get; set; }
    public DbSet<Endereco> Enderecos { get; set; }
}