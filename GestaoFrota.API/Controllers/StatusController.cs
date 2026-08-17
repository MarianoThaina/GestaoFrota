using Microsoft.AspNetCore.Mvc;

//Esse código cria GET /api/status

namespace GestaoFrota.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class StatusController : ControllerBase
    {
        [HttpGet]
        public IActionResult Get()
        {
            return Ok(new
            {
                status = "API funcionando",
                projeto = "GestaoFrota"
            });
        }
    }
}
