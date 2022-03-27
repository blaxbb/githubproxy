using System.Net;
using System.Net.Http.Headers;

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

app.MapPost("/github-proxy", async (context) => {
    if (!context.Request.HasJsonContentType())
    {
        context.Response.StatusCode = (int)HttpStatusCode.UnsupportedMediaType;
        return;
    }

    var oauth = await context.Request.ReadFromJsonAsync<OauthRequest>();
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

    var oauthResp = await response.Content.ReadFromJsonAsync<OauthResponse>();
    if(oauthResp == null)
    {
        context.Response.StatusCode = (int)HttpStatusCode.BadRequest;
        return;
    }

    await context.Response.WriteAsJsonAsync(oauthResp);
});

app.Run();

internal record OauthRequest(string client_id, List<string> scopes)
{

}

internal record OauthResponse(string device_code, string user_code, string verification_uri, int expires_in, int interval)
{

}