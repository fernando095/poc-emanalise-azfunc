using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using poc_emanalise_timer.Models;

namespace poc_emanalise_http
{
    public class HttpTriggerFunc
    {
        private readonly ILogger<HttpTriggerFunc> _logger;

        public HttpTriggerFunc(ILogger<HttpTriggerFunc> log)
        {
            _logger = log;
        }

        [FunctionName("HttpTriggerFunc")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "pedido" })]
        [OpenApiParameter(name: "pedido", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "The **Pedido** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "text/plain", bodyType: typeof(string), Description = "The CREATED response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", "post", Route = null)] HttpRequest req,
            [Sql("select * from dbo.EmAnalise where DataCriacao <= GETDATE()",
                CommandType = CommandType.Text,
                ConnectionStringSetting = "Server=tcp:study-sql-01.database.windows.net,1433;Initial Catalog=poc-cronjob;Persist Security Info=False;User ID=fernando095;Password=Fernando.095;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;")]
                IEnumerable<EmAnalise> itensEmAnalise)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            _logger.LogInformation($"Registros: {itensEmAnalise.Count()}.");

            string pedido = req.Query["pedido"];
            int pedidoId = int.Parse(pedido);
            _logger.LogInformation($"Registros iguais: {itensEmAnalise.Count(x => x.PedidoId == pedidoId)}.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);

            //string responseMessage = string.IsNullOrEmpty(name)
            //    ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
            //    : $"Hello, {name}. This HTTP triggered function executed successfully.";

            return new CreatedResult("https://poc-study-funcapp.azurewebsites.net/api/HttpTrigger1?code=EjKv6SEh9OwFegiyYSHl6tXkIkMJFy3aLS/ox/f86OZQEx82/dlQvg==", pedidoId);
        }
    }
}

