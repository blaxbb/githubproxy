using System.Net;
using System.Net.Http.Headers;

string? CLIENT_SECRET = Environment.GetEnvironmentVariable("CLIENT_SECRET");

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddCors();

var app = builder.Build();

app.UseCors(x => x
.AllowAnyOrigin()
.AllowAnyMethod()
.AllowAnyHeader());

// Configure the HTTP request pipeline.

app.UseHttpsRedirection();

HttpClient client = new HttpClient();
client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

app.MapPost("/login/device/code", async (context) => {
    if (!context.Request.HasJsonContentType())
    {
        context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
        return;
    }

    var oauth = await context.Request.ReadFromJsonAsync<OauthFlowRequest>();
    if (oauth == null)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    var response = await client.PostAsJsonAsync("https://github.com/login/device/code", oauth);
    if (!response.IsSuccessStatusCode)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    var oauthResp = await response.Content.ReadFromJsonAsync<OauthFlowResponse>();
    if(oauthResp == null)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    await context.Response.WriteAsJsonAsync(oauthResp);
});


app.MapPost("/login/oauth/access_token", async (context) => {
    if (!context.Request.HasJsonContentType())
    {
        context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
        return;
    }

    var oauth = await context.Request.ReadFromJsonAsync<OauthTokenRequest>();
    if (oauth == null)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    if (!string.IsNullOrWhiteSpace(CLIENT_SECRET))
    {
        oauth = oauth with { client_secret = CLIENT_SECRET };
    }

    var response = await client.PostAsJsonAsync("https://github.com/login/oauth/access_token", oauth);
    if (!response.IsSuccessStatusCode)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }
    var str = await response.Content.ReadAsStringAsync();
    var oauthResp = await response.Content.ReadFromJsonAsync<OauthTokenResponse>();
    if (oauthResp == null)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    await context.Response.WriteAsJsonAsync(oauthResp);
});

app.Run();

internal record OauthFlowRequest(string client_id, List<string> scopes);
internal record OauthFlowResponse(string device_code, string user_code, string verification_uri, int expires_in, int interval);

internal record OauthTokenRequest(string client_id, string code, string client_secret, string device_code, string user_code, string grant_type);
internal record OauthTokenResponse(string token_type, string access_token, string scope, string error, string error_description, string error_uri);