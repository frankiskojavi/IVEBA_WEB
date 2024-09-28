using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Services.IVE13ME;

var builder = WebApplication.CreateBuilder(args);

// Mapeos de DBContext
// builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("IVEBA")));

// Registro de DbHelper
builder.Services.AddSingleton<DbHelper>();

// Inyección de dependencias para servicios y repositorios
builder.Services.AddScoped<IIVE13MEHelperService, IVE13MEHelperService>();

// Add services to the container.
builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();
