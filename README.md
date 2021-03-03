# keycloak.Adapter

Link do projeto no Nuget:
https://www.nuget.org/packages/KeycloakAdapter/2.1.1

Depois de muito tempo procurando algo que me permitisse integrar diretamente com o Keycloak sem obter sucesso, decidi invertir um pouco mais nos meus estudos e 
cheguei nesse projeto que agora compartilho com mais quem achar que pode ser ajudado por ele.
Ainda pretendo melhora muita coisa e sei que tem muito que pode ser melhorado, tanto em código C# com na comunicação com o Keycloak, por exemplo, eu não vi uma forma de retornar 
os dados de um usuário logo depois de criá-lo e por isso esse passo é realizado com dois acessos. 
Fique a vontade para contribuir como achar possível. 

Depois de baixar o Keycloak.Adapter os passos a seguir são:

Alterar o arquivo Startup.cs nos seguintes pontos:

 public Startup(IWebHostEnvironment env, IConfiguration configuration)
 {
    Configuration = configuration;
    Environment = env;
 }
 
 ...
 public IWebHostEnvironment Environment { get; }
 ...

Alterabdo o método ConfigureServices

public void ConfigureServices(IServiceCollection services)
        {

            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "MyHealth.Api", Version = "v1" });
            });

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(o =>
            {
                o.Authority = Configuration["keycloakData:UrlBase"] + Configuration["keycloakData:Authority"];
                o.Audience = Configuration["keycloakData:ClientId"];
                o.SaveToken = false;
                o.RequireHttpsMetadata = false;
                o.IncludeErrorDetails = true;
                o.RequireHttpsMetadata = false;
                o.MetadataAddress = Configuration["keycloakData:UrlBase"] + Configuration["keycloakData:MetadataUrl"];

                o.Events = new JwtBearerEvents()
                {
                    OnAuthenticationFailed = c =>
                    {
                        c.NoResult();

                        string errorTextDEfault = "An error occured processing your authentication.";

                        c.Response.StatusCode = 401;
                        c.Response.ContentType = "text/plain";

                        if (Environment.IsDevelopment())
                        {
                            return c.Response.WriteAsync(errorTextDEfault);
                        }

                        return c.Response.WriteAsync(errorTextDEfault);
                    },

                    OnTokenValidated = async context =>
                    {
                        MapKeycloakRolesToRoleClaims(context);
                    }
                };
            });
        }
        
        No método Configure adcione a chamada para UseAuthentication
        
         ...
         app.UseRouting();
         app.UseAuthentication();
         app.UseAuthorization();
         ...
        
        Crie o método MapKeycloakRolesToRoleClaims
        
        private static void MapKeycloakRolesToRoleClaims(TokenValidatedContext context)
        {
            var resourceAccess = JObject.Parse(context.Principal.FindFirst("resource_access").Value);
            var clientResource = resourceAccess[context.Principal.FindFirstValue("aud")];
            var clientRoles = clientResource["roles"];

            if (context.Principal.Identity is not ClaimsIdentity claimsIdentity)
            {
                return;
            }

            foreach (var clientRole in clientRoles)
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, clientRole.ToString()));
            }
        }
        
     Depois inclua o seguinte bloco no appsettings.json
     
     "keycloakData": {
      "UrlBase": "http://localhost:8080/",
      "Authority": "auth/realms/master",
      "MetadataUrl": "auth/realms/master/.well-known/openid-configuration",
      "SessionStartUrl": "auth/realms/master/protocol/openid-connect/token",
      "CreateUserUrl": "auth/admin/realms/master/users",
      "UrlAddRoleToUser": "auth/admin/realms/master/users/[USER_UUID]/role-mappings/clients/[CLIENT_UUID]",
      "ClientId": "admin-cli",
      "ClientSecret": "59031ebb-38be-47dd-8b21-010b19f29d62",
      "Roles": "[{\"id\":\"3a97fbc5-0430-4629-8768-06d24cfb04e4\",\"name\":\"administrator\",\"composite\":false,\"clientRole\":true,\"containerId\":\"f0fbec81-5906-47ba-8780-8a5d3a97cf4d\"},{\"id\":\"8231f4f1-8045-47ef-91d0-1da59789596d\",\"name\":\"user\",\"composite\":false,\"clientRole\":true,\"containerId\":\"f0fbec81-5906-47ba-8780-8a5d3a97cf4d\"}]"
    },

.....

Essa é a chamada que criei para realizar o login da api no Keycloak

[HttpPost]
public Task<string> Post(string login, string password)
{
    string answer = string.Empty;

    KeycloakManager accessKeycloakData = new KeycloakManager(_configutation);

    OpenIdConnect openIdConnect = accessKeycloakData.TryLoginExecute(login, password).Result;

    if (openIdConnect.HasError) answer = openIdConnect.error_description;

    else answer = JsonConvert.SerializeObject(openIdConnect);

    return Task.FromResult(answer);
}

Esse método cria o usuário e atribui uma role para ele.
A role está no appsettings.json com a chave Roles.

[HttpPost]
[Authorize(Roles = "administrator")]
public async Task<IActionResult> Post([FromBody] User accessUser)
{
    IActionResult result = default;
    string newUser = KeycloakManager.CreateUserData(accessUser);
    StringContent httpConent = new StringContent(newUser, Encoding.UTF8, "application/json");
    string jwt = Request.Headers["Authorization"];

    int statusCodeResult = accessKeycloakData.TryCreateUser(jwt, httpConent).Result;

    if (statusCodeResult == 201)
    {
        HttpResponseObject<User> userCreated = accessKeycloakData.FindUserByEmail(jwt, accessUser.email).Result;
        await accessKeycloakData.TryAddRole(jwt, userCreated.Object, "administrator");
        result = Created(" ", userCreated.Object);
    }
    else
    {
        result = new StatusCodeResult(statusCodeResult);
    }

    return result;
}
    
