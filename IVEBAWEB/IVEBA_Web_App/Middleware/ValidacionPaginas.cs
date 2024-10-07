using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using IVEBA_Web_App.Services.SeguridadAPP;

namespace IVEBA_Web_App.Middleware;
public class ValidacionPaginas : IAsyncPageFilter
{
    private readonly iSeguridadaAPPService _seguridadaAPPService;

    public ValidacionPaginas(iSeguridadaAPPService seguridadaAPPService)
    {
        _seguridadaAPPService = seguridadaAPPService;
    }    
    public Task OnPageHandlerSelectionAsync(PageHandlerSelectedContext context)
    {        
        return Task.CompletedTask;
    }

    // Este es el método donde realizarás la validación de la página
    public async Task OnPageHandlerExecutionAsync(PageHandlerExecutingContext context, PageHandlerExecutionDelegate next)
    {        
        var paginaValidar = context.HttpContext.Request.Path.Value;        
        var menuOpciones = await _seguridadaAPPService.ConsultarOpcionesMenuWebApp();

        if(paginaValidar == "/Error" || paginaValidar == "/")
        {
            await next();
        }

        // Verificar si la página solicitada está en las opciones permitidas
        if (!menuOpciones.Any(opcion => opcion.menuPagina == paginaValidar))
        {
            context.Result = new RedirectToPageResult("/Error");
            return;
        }
        
        await next();
    }
}
