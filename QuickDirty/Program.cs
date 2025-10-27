using Microsoft.EntityFrameworkCore;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Configurar SQLite e EF Core
builder.Services.AddDbContext<BancoDeDados>(options =>
    options.UseSqlite("Data Source=pessoas.db"));

// Evitar ciclos ao serializar JSON
builder.Services.AddControllers().AddJsonOptions(opt =>
{
    opt.JsonSerializerOptions.ReferenceHandler = ReferenceHandler.IgnoreCycles;
});

var app = builder.Build();

// Seed inicial e criação do banco
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<BancoDeDados>();
    db.Database.EnsureCreated();

    if (!db.TiposEnderecos.Any())
    {
        db.TiposEnderecos.AddRange(
            new TipoEndereco { Nome = "Outro" },
            new TipoEndereco { Nome = "Casa" },
            new TipoEndereco { Nome = "Apartamento" },
            new TipoEndereco { Nome = "Condomínio" },
            new TipoEndereco { Nome = "Sítio" },
            new TipoEndereco { Nome = "Fazenda" },
            new TipoEndereco { Nome = "Chácara" },
            new TipoEndereco { Nome = "Kitnet" },
            new TipoEndereco { Nome = "Sobrado" }
        );
    }

    if (!db.TiposTelefones.Any())
    {
        db.TiposTelefones.AddRange(
            new TipoTelefone { Nome = "Outro" },
            new TipoTelefone { Nome = "Celular" },
            new TipoTelefone { Nome = "Fixo" },
            new TipoTelefone { Nome = "Comercial" },
            new TipoTelefone { Nome = "Residencial" },
            new TipoTelefone { Nome = "Recado" }
        );
    }

    if (!db.TiposEmails.Any())
    {
        db.TiposEmails.AddRange(
            new TipoEmail { Nome = "Outro" },
            new TipoEmail { Nome = "Pessoal" },
            new TipoEmail { Nome = "Profissional" },
            new TipoEmail { Nome = "Institucional" },
            new TipoEmail { Nome = "Suporte" },
            new TipoEmail { Nome = "Financeiro" }
        );
    }

    db.SaveChanges();
}

// === ENDPOINTS ===

app.MapPost("/pessoa", async (Pessoa pessoa, BancoDeDados db) =>
{
    if (string.IsNullOrWhiteSpace(pessoa.Nome))
        return Results.BadRequest(new { Erro = "O nome da pessoa é obrigatório." });

    // Lookup
    pessoa.Telefones.ForEach(t => t.TipoTelefone = null!);
    pessoa.Emails.ForEach(e => e.TipoEmail = null!);
    if (pessoa.Endereco != null) pessoa.Endereco.TipoEndereco = null!;

    db.Pessoas.Add(pessoa);
    await db.SaveChangesAsync();

    var pessoaCompleta = await db.Pessoas
        .Include(p => p.Telefones).ThenInclude(t => t.TipoTelefone)
        .Include(p => p.Emails).ThenInclude(e => e.TipoEmail)
        .Include(p => p.Endereco).ThenInclude(e => e.TipoEndereco)
        .Include(p => p.Usuario)
        .AsSplitQuery()
        .FirstAsync(p => p.Id == pessoa.Id);

    return Results.Created($"/pessoa/{pessoa.Id}", pessoaCompleta);
});

app.MapGet("/pessoas", async (BancoDeDados db) =>
{
    var pessoas = await db.Pessoas
        .Include(p => p.Telefones).ThenInclude(t => t.TipoTelefone)
        .Include(p => p.Emails).ThenInclude(e => e.TipoEmail)
        .Include(p => p.Endereco).ThenInclude(e => e.TipoEndereco)
        .Include(p => p.Usuario)
        .AsSplitQuery()
        .ToListAsync();

    return Results.Ok(pessoas);
});

app.MapGet("/pessoa/{id:int}", async (int id, BancoDeDados db) =>
{
    var pessoa = await db.Pessoas
        .Include(p => p.Telefones).ThenInclude(t => t.TipoTelefone)
        .Include(p => p.Emails).ThenInclude(e => e.TipoEmail)
        .Include(p => p.Endereco).ThenInclude(e => e.TipoEndereco)
        .Include(p => p.Usuario)
        .AsSplitQuery()
        .FirstOrDefaultAsync(p => p.Id == id);

    return pessoa is null ? Results.NotFound(new { Erro = "Pessoa não encontrada." }) : Results.Ok(pessoa);
});

app.Run();


// === CLASSES E MODELOS ===
public class Pessoa
{
    public int Id { get; set; }
    public string Nome { get; set; } = "";
    public int Idade { get; set; }
    public string Documento { get; set; } = "";
    public TipoPessoa TipoPessoa { get; set; } = TipoPessoa.Fisica;

    public List<Telefone> Telefones { get; set; } = new();
    public List<Email> Emails { get; set; } = new();
    public Endereco? Endereco { get; set; }
    public Usuario? Usuario { get; set; }
}

public class Telefone
{
    public int Id { get; set; }
    public string Numero { get; set; } = "";
    public int TipoTelefoneId { get; set; }
    public TipoTelefone TipoTelefone { get; set; } = null!;
    public int PessoaId { get; set; }
    [JsonIgnore]
    public Pessoa Pessoa { get; set; } = null!;
}

public class Email
{
    public int Id { get; set; }
    public string Endereco { get; set; } = "";
    public int TipoEmailId { get; set; }
    public TipoEmail TipoEmail { get; set; } = null!;
    public int PessoaId { get; set; }
    [JsonIgnore]
    public Pessoa Pessoa { get; set; } = null!;
}

public class Endereco
{
    public int Id { get; set; }
    public string Rua { get; set; } = "";
    public string Numero { get; set; } = "";
    public string Cidade { get; set; } = "";
    public string Estado { get; set; } = "";
    public string Cep { get; set; } = "";
    public int TipoEnderecoId { get; set; }
    public TipoEndereco TipoEndereco { get; set; } = null!;
    public int PessoaId { get; set; }
    [JsonIgnore]
    public Pessoa Pessoa { get; set; } = null!;
}

public class Usuario
{
    public int Id { get; set; }
    public string NomeUsuario { get; set; } = "";
    public string SenhaHash { get; set; } = "";
    public string? SenhaSalt { get; set; }
    public Role Role { get; set; } = Role.User;
    public bool Ativo { get; set; } = true;
    public DateTime CriadoEm { get; set; } = DateTime.UtcNow;
    public DateTime? UltimoAcesso { get; set; }
    public int PessoaId { get; set; }
    [JsonIgnore]
    public Pessoa Pessoa { get; set; } = null!;
}

public class BancoDeDados : DbContext
{
    public BancoDeDados(DbContextOptions<BancoDeDados> options) : base(options) { }

    public DbSet<Pessoa> Pessoas { get; set; } = null!;
    public DbSet<Telefone> Telefones { get; set; } = null!;
    public DbSet<Email> Emails { get; set; } = null!;
    public DbSet<Endereco> Enderecos { get; set; } = null!;
    public DbSet<Usuario> Usuarios { get; set; } = null!;
    public DbSet<TipoEndereco> TiposEnderecos { get; set; } = null!;
    public DbSet<TipoTelefone> TiposTelefones { get; set; } = null!;
    public DbSet<TipoEmail> TiposEmails { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Pessoa>()
            .HasOne(p => p.Usuario)
            .WithOne(u => u.Pessoa)
            .HasForeignKey<Usuario>(u => u.PessoaId);

        modelBuilder.Entity<Telefone>()
            .HasOne(t => t.TipoTelefone)
            .WithMany()
            .HasForeignKey(t => t.TipoTelefoneId);

        modelBuilder.Entity<Email>()
            .HasOne(e => e.TipoEmail)
            .WithMany()
            .HasForeignKey(e => e.TipoEmailId);

        modelBuilder.Entity<Endereco>()
            .HasOne(e => e.TipoEndereco)
            .WithMany()
            .HasForeignKey(e => e.TipoEnderecoId);

        base.OnModelCreating(modelBuilder);
    }
}

// ENUMS
public enum Role { Admin, User, Manager, Guest }
public enum TipoPessoa { Fisica, Juridica }

// LOOKUP TABLES
public class TipoEndereco { public int Id { get; set; } public string Nome { get; set; } = ""; }
public class TipoTelefone { public int Id { get; set; } public string Nome { get; set; } = ""; }
public class TipoEmail { public int Id { get; set; } public string Nome { get; set; } = ""; }
