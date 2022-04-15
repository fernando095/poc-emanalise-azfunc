using System;
using System.Data;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Attributes;
using Microsoft.Azure.WebJobs.Extensions.OpenApi.Core.Enums;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using poc_emanalise_timer.Models;

namespace poc_emanalise_timer
{
    public class FinalHttpTriggerFunc
    {
        private readonly ILogger<FinalHttpTriggerFunc> _logger;

        public FinalHttpTriggerFunc(ILogger<FinalHttpTriggerFunc> log)
        {
            _logger = log;
        }

        [FunctionName("FinalHttpTriggerFunc")]
        [OpenApiOperation(operationId: "Run", tags: new[] { "final" })]
        [OpenApiSecurity("function_key", SecuritySchemeType.ApiKey, Name = "code", In = OpenApiSecurityLocationType.Query)]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.OK, contentType: "text/plain", bodyType: typeof(string), Description = "The OK response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.NotFound, contentType: "text/plain", bodyType: typeof(string), Description = "The NOT FOUND response")]
        [OpenApiResponseWithBody(statusCode: HttpStatusCode.BadRequest, contentType: "text/plain", bodyType: typeof(string), Description = "The BAD REQUEST response")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
        {
            _logger.LogInformation("C# HTTP trigger function processed a request.");

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            _logger.LogInformation($"Body retorno: {requestBody}.");

            EmAnalise emAnalise = JsonConvert.DeserializeObject<EmAnalise>(requestBody);

            var connStr = Environment.GetEnvironmentVariable("SqlConnectionString");
            _logger.LogInformation($"Conection: {connStr}.");

            if (emAnalise.PedidoId < 0)
                return new BadRequestResult();

            int qtdAtt;

            using (SqlConnection conn = new(connStr))
            {
                conn.Open();
                var text = @"UPDATE [dbo].[EmAnalise] SET QtdAtualizacao = EmAnaliseAux.QtdAtualizacao
                            FROM (
                                SELECT ISNULL(QtdAtualizacao, 0) + 1 AS QtdAtualizacao 
                                FROM [dbo].[EmAnalise] 
                                WHERE PedidoId = @PedidoId) AS EmAnaliseAux
                            WHERE PedidoId = @PedidoId";

                using SqlCommand cmd = new(text, conn);
                cmd.Parameters.AddWithValue("PedidoId", emAnalise.PedidoId);
                qtdAtt = await cmd.ExecuteNonQueryAsync();
                _logger.LogInformation($"{qtdAtt} linhas atualizadas");
            }
            return qtdAtt > 0 ? 
                new OkObjectResult(emAnalise) :
                new NotFoundResult();

        }
    }
}

