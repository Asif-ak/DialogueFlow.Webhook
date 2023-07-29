using Google.Cloud.Dialogflow.V2;
using Google.Protobuf;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Net.Http;
using System.Text.Json;
using static Google.Rpc.Context.AttributeContext.Types;

namespace DialogueFlow.WebHook
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Add services to the container.
            builder.Services.AddAuthorization();

            // Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddHttpClient();

            var app = builder.Build();

            // Configure the HTTP request pipeline.
            if (app.Environment.IsDevelopment())
            {
                app.UseSwagger();
                app.UseSwaggerUI();
            }

            app.UseAuthorization();
            JsonParser jsonParser = new JsonParser(JsonParser.Settings.Default.WithIgnoreUnknownFields(true));
            app.MapPost("/orderStatus", async (HttpContext httpContext, [FromServices] HttpClient client) =>
            {

                try
                {
                    string requestJson;
                    using (TextReader reader = new StreamReader(httpContext.Request.Body))
                    {
                        requestJson = await reader.ReadToEndAsync();
                    }

                    WebhookRequest request = jsonParser.Parse<WebhookRequest>(requestJson);
                    var _params = request.QueryResult.Parameters;

                    var _client = await client.PostAsJsonAsync("https://orderstatusapi-dot-organization-project-311520.uc.r.appspot.com/api/getOrderStatus", new { orderId = $"{_params.Fields["number"]}" });
                    string _response = string.Empty;
                    if (_client.IsSuccessStatusCode)
                    {
                        _response = await _client.Content.ReadAsStringAsync();
                        var _responseKVP = JsonSerializer.Deserialize<Dictionary<string, string>>(_response);
                        var _dtt = _responseKVP["shipmentDate"].Split("T")[0];
                        var _dt = DateTime.ParseExact(_dtt, "yyyy-mm-dd", CultureInfo.InvariantCulture).ToString("dddd, dd mm yyyy");
                        WebhookResponse response = new WebhookResponse
                        {
                            //var _dt = DateTime.TryParseExact(_responseKVP.Value,"yyyy-MM-ddThh:mm:ss:fff",CultureInfo.InvariantCulture, DateTimeStyles.None,out DateTime _result) ? _result : _responseKVP.Value;
                            FulfillmentText = $"You order {_params.Fields["number"]} will be shipped on {_dt}."
                        };

                        string responseJson = response.ToString();
                        return Results.Content(responseJson, "application/json");
                    }
                    else
                    {
                        throw new HttpRequestException(_client.ReasonPhrase);
                    }

                }
                catch (Exception ex)
                {
                    WebhookResponse response = new WebhookResponse
                    {
                        FulfillmentText = "Order status unavailable."
                    };
                    string responseJson = response.ToString();
                    return Results.Content(responseJson, "application/json");
                }
            })
            .WithName("GetOrderStatus");
            app.Run();
        }
    }
}