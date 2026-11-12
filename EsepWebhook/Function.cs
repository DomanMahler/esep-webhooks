using System.Text;
using Amazon.Lambda.Core;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(Amazon.Lambda.Serialization.SystemTextJson.DefaultLambdaJsonSerializer))]

namespace EsepWebhook;

public class Function
{
    private static readonly HttpClient Client = new();

    public string FunctionHandler(object input, ILambdaContext context)
    {
        context.Logger.LogLine($"Received: {input}");

        JObject json = JObject.Parse(input.ToString()!);

        string? issueUrl = (string?)json["issue"]?["html_url"];
        if (string.IsNullOrEmpty(issueUrl))
        {
            context.Logger.LogLine("No issue.html_url in payload; nothing to post.");
            return "ignored";
        }

        string? slackUrl = Environment.GetEnvironmentVariable("SLACK_URL");
        if (string.IsNullOrEmpty(slackUrl))
        {
            context.Logger.LogLine("SLACK_URL environment variable is not set.");
            return "SLACK_URL not set";
        }

        string payload = JsonConvert.SerializeObject(new { text = $"Issue Created: {issueUrl}" });

        var webRequest = new HttpRequestMessage(HttpMethod.Post, slackUrl)
        {
            Content = new StringContent(payload, Encoding.UTF8, "application/json")
        };

        var response = Client.Send(webRequest);
        using var reader = new StreamReader(response.Content.ReadAsStream());
        return reader.ReadToEnd();
    }
}
