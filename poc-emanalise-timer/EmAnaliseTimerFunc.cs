using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using poc_emanalise_timer.Models;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace poc_emanalise_timer
{
    public class EmAnaliseTimerFunc
    {
        static readonly HttpClient _httpClient = new();
        [FunctionName("EmAnaliseTimerFunc")]
        public async Task Run(
            [TimerTrigger("10 * * * * *")]TimerInfo myTimer,
            [Sql("select PedidoId, DataCriacao from dbo.EmAnalise where DataCriacao <= GETDATE()",
                CommandType = CommandType.Text,
                ConnectionStringSetting = "SqlConnectionString")]
                IEnumerable<EmAnalise> itensEmAnalise,
            ILogger log)
        {
            log.LogInformation($"C# Timer trigger function executed at: {DateTime.Now}");

            log.LogInformation($"Quantidade registros expirados: {itensEmAnalise.Count()}");
            var response = await _httpClient.GetAsync("https://poc-study-funcapp.azurewebsites.net/api/EmAnaliseHttpTriggerFunc?pedido=" + itensEmAnalise.First().PedidoId);
            log.LogInformation($"Chamada com sucesso: {response.IsSuccessStatusCode}. StatusCode: {response.StatusCode}");
            string content = await response.Content.ReadAsStringAsync();
            log.LogInformation($"Retorno pedido: {content}.");
            EmAnalise emAnaliseResult = JsonConvert.DeserializeObject<EmAnalise>(content);
            if (emAnaliseResult == null || emAnaliseResult.PedidoId < 1)
                log.LogError("Retorno incorreto");
            else
                log.LogInformation("Retorno correto");
        }
    }
}
