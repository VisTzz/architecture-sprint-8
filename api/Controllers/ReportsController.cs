using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System.Text;

namespace backend_sprint8.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ReportsController : ControllerBase
    {
        [HttpGet(Name = "reports")]
        public FileContentResult Get()
        {
            string content = "Это произвольная строка в текстовом файле.";
            byte[] fileBytes = Encoding.UTF8.GetBytes(content);

            return File(fileBytes, "text/plain", "report.txt");
        }
    }
}
