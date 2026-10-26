using Microsoft.EntityFrameworkCore;
using Project1.Data;

var builder = WebApplication.CreateBuilder(args);

// Registrar el DbContext con SQL Server (Conexión a la base de datos)
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("CadenaSQL")));

// Agregar servicios al contenedor
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configurar el entorno de desarrollo para usar Swagger
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// NUEVO: Esto crea la base de datos y la actualiza automáticamente al arrancar con las columnas nuevas
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.EnsureDeleted(); // <-- Esto borra la base vieja
    context.Database.EnsureCreated(); // <-- Esto la crea nueva con la fecha
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

app.Run();