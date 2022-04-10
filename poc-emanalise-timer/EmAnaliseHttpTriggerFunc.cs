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
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using poc_emanalise_timer.Models;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using System;

namespace poc_emanalise_timer
{
    public class EmAnaliseHttpTriggerFunc
    {
        //private readonly ILogger<EmAnaliseHttpTriggerFunc> _logger;

        //public EmAnaliseHttpTriggerFunc(ILogger<EmAnaliseHttpTriggerFunc> log)
        //{
        //    _logger = log;
        //}

        [FunctionName("EmAnaliseHttpTriggerFunc")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "pedido" })]
        [OpenApiParameter(name: "pedido", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "The **Pedido** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "text/plain", bodyType: typeof(string), Description = "The CREATED response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Sql("select * from dbo.EmAnalise where DataCriacao <= GETDATE()",
                CommandType = CommandType.Text,
                ConnectionStringSetting = "SqlConnectionString")]
                IEnumerable<EmAnalise> itensEmAnalise,
            ILogger _logger)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            _logger.LogInformation($"Registros: {itensEmAnalise.Count()}.");

            string pedido = req.Query["pedido"];
            int pedidoId = int.Parse(pedido);
            _logger.LogInformation($"Registros iguais: {itensEmAnalise.Count(x => x.PedidoId == pedidoId)}. Pedido: {pedidoId}");

            var createdResult = new CreatedResult("api/FinalHttpTriggerFunc", itensEmAnalise.FirstOrDefault(x => x.PedidoId == pedidoId));
            _logger.LogInformation($"Chamada finalhttpfunc status code: {createdResult.StatusCode}");
            await Task.CompletedTask;
            if (createdResult.Value is not EmAnalise result || result.PedidoId < 1)
                return new ForbidResult();
            if ((createdResult.StatusCode ?? 999) >= 300)
                return new StatusCodeResult(500);
            return createdResult;
        }
    }
}

