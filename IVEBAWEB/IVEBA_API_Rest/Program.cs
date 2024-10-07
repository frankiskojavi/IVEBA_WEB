using IVEBA_API_Rest.Helpers;
using IVEBA_API_Rest.Services.IVE13ME;
using IVEBA_API_Rest.Services.IVECH;
using IVEBA_API_Rest.Services.SeguridadAPP;

var builder = WebApplication.CreateBuilder(args);


// Mapeos de DBContext
// builder.Services.AddDbContext<ApplicationDbContext>(options => options.UseSqlServer(builder.Configuration.GetConnectionString("IVEBA")));

// Registro de DbHelper
builder.Services.AddSingleton<DbHelper>();

// Cargar el archivo JSON adicional
builder.Configuration.AddJsonFile("opcionesMenuIVEBA_WEB_APP.json", optional: false, reloadOnChange: true);

// Inyecci�n de dependencias para servicios y repositorios
builder.Services.AddScoped<IIVE13MEHelperService, IVE13MEHelperService>();
builder.Services.AddScoped<IIVECHHelperService, IVECHHelperService>();
builder.Services.AddScoped<iSeguridadaAPPService, SeguridadaAPPService>();

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

app.UseHsts();

app.Run();


