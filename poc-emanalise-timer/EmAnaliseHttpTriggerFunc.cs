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
using System.Net.Http;
using System.Net.Http.Formatting;

namespace poc_emanalise_timer
{
    public class EmAnaliseHttpTriggerFunc
    {
        private readonly ILogger<EmAnaliseHttpTriggerFunc> _logger;
        private readonly string _url;
        static readonly HttpClient _httpClient = new();

        public EmAnaliseHttpTriggerFunc(ILogger<EmAnaliseHttpTriggerFunc> log)
        {
            _logger = log;
            _url = Environment.GetEnvironmentVariable("FuncAppUrl");
            _logger.LogInformation($"Url: {_url}.");
            _httpClient.BaseAddress = new Uri(_url);
        }


        [FunctionName("EmAnaliseHttpTriggerFunc")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "pedido" })]
        [OpenApiParameter(name: "pedido", In = ParameterLocation.Query, Required = true, Type = typeof(int), Description = "The **Pedido** parameter")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Created, contentType: "text/plain", bodyType: typeof(string), Description = "The CREATED response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.Forbidden, contentType: "text/plain", bodyType: typeof(string), Description = "The FORBIDDEN response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.InternalServerError, contentType: "text/plain", bodyType: typeof(string), Description = "The INTERNALSERVERERROR response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The BADREQUEST response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = null)] HttpRequest req,
            [Sql("select * from dbo.EmAnalise where DataCriacao <= GETDATE()",
                CommandType = CommandType.Text,
                ConnectionStringSetting = "SqlConnectionString")]
                IEnumerable<EmAnalise> itensEmAnalise)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");
            _logger.LogInformation($"Registros: {itensEmAnalise.Count()}.");

            string pedido = req.Query["pedido"];
            int pedidoId = int.Parse(pedido);
            
            _logger.LogInformation($"Registros iguais: {itensEmAnalise.Count(x => x.PedidoId == pedidoId)}. Pedido: {pedidoId}");
            
            EmAnalise primeiroItem = itensEmAnalise.FirstOrDefault(x => x.PedidoId == pedidoId);

            if (primeiroItem == null || primeiroItem.PedidoId < 1)
            {
                _logger.LogInformation($"Erro ao encontrar pedido enviado ou lista de itens em análise vazia.");
                return new BadRequestResult();
            }

            _logger.LogInformation($"Primeiro item: {JsonConvert.SerializeObject(primeiroItem)}");

            var response = await _httpClient.PostAsJsonAsync("FinalTriggerFunc", primeiroItem);
            
            if(!response?.IsSuccessStatusCode ?? false)
            {
                _logger.LogInformation($"Erro ao chamar FinalTriggerFunc Status Code: {response?.StatusCode}");
                return new ForbidResult();
            }

            _logger.LogInformation($"Chamada com sucesso: {response.IsSuccessStatusCode}. StatusCode: {response.StatusCode}");
            string content = await response.Content.ReadAsStringAsync();
            
            _logger.LogInformation($"Retorno pedido: {content}.");
            EmAnalise emAnaliseResult = JsonConvert.DeserializeObject<EmAnalise>(content);
                        
            _logger.LogInformation($"Pedido retornado finalhttpfunc {JsonConvert.SerializeObject(emAnaliseResult)}");
            
            if (emAnaliseResult?.PedidoId < 1)
                return new ForbidResult();
            if ((int)response.StatusCode >= 300)
                return new StatusCodeResult(500);
            return new CreatedResult("api/FinalHttpTriggerFunc", emAnaliseResult);
        }
    }
}

