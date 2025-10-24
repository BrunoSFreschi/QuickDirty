using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);


builder.Services.AddDbContext<DataBase>(options
    => options.UseSqlite("Data Source=pessoas.db"));

var app = builder.Build();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<DataBase>();
    db.Database.EnsureCreated();
}


app.MapPost("/pessoas", async (Person pessoa, DataBase db) =>
{
    db.Pessoas.Add(pessoa);
    await db.SaveChangesAsync();
    return Results.Created($"/pessoas/{pessoa.Id}", pessoa);
});

app.MapGet("/pessoas", async (DataBase db) =>
{
    var pessoas = await db.Pessoas.ToListAsync();
    return Results.Ok(pessoas);
});

app.MapGet("/pessoas/{id}", async (int id, DataBase db) =>
{
    var pessoa = await db.Pessoas.FindAsync(id);

    return pessoa != null ? Results.Ok(pessoa) : Results.NotFound();
});


app.MapGet("/", () => "Hello World!");

app.Run();

internal class DataBase : DbContext
{
    public DataBase(DbContextOptions<DataBase> options) : base(options) { }
    public DbSet<Person> Pessoas => Set<Person>();
}

public class Person
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreateAt { get; set; } = DateTime.UtcNow;
    public bool Ative { get; set; } = true;
}