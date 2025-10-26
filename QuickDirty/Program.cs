using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<BancoDeDados>(options =>
    options.UseSqlite("Data Source=pessoas.db"));

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BancoDeDados>();
    db.Database.EnsureCreated();
}

// === ROTAS DE PESSOAS ===
app.MapPost("/pessoas", async (Pessoa pessoa, BancoDeDados db) =>
{
    db.Pessoas.Add(pessoa);
    await db.SaveChangesAsync();
    return Results.Created($"/pessoas/{pessoa.Id}", pessoa);
});

app.MapGet("/pessoas", async (BancoDeDados db) =>
{
    var pessoas = await db.Pessoas
        .Include(p => p.Telefones)
        .Include(p => p.Emails)
        .Include(p => p.Endereco)
        .ToListAsync();
    return Results.Ok(pessoas);
});

app.Run();

// === CLASSES (MODELOS) ===

public class Pessoa
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public int Idade { get; set; }

    // Relacionamentos
    public List<Telefone> Telefones { get; set; } = new();
    public List<Email> Emails { get; set; } = new();
    public Endereco? Endereco { get; set; }
}

public class Telefone
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";
    public string Tipo { get; set; } = ""; // Ex: Celular, Fixo, Trabalho

    public int PessoaId { get; set; }
}

public class Email
{
    public int Id { get; set; }
    public string EnderecoEmail { get; set; } = "";
    public string Tipo { get; set; } = ""; // Ex: Pessoal, Trabalho

    public int PessoaId { get; set; }
}

public class Endereco
{
    public int Id { get; set; }
    public string Rua { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Estado { get; set; } = "";
    public string CEP { get; set; } = "";

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