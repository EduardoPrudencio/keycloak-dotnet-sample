
# keycloak.Adapter

Link do projeto no Nuget:
https://www.nuget.org/packages/KeycloakAdapter/2.4.1

Depois de muito tempo procurando algo que me permitisse integrar diretamente com o Keycloak sem obter sucesso, decidi investir um pouco mais nos meus estudos e 
cheguei nesse projeto que agora compartilho com mais quem achar que pode ser ajudado por ele.
Ainda pretendo melhora muita coisa e sei que tem muito que pode ser melhorado, tanto em código C# como na comunicação com o Keycloak, por exemplo, eu não vi uma forma de retornar os dados de um usuário logo depois de criá-lo no Keycloak e por isso esse passo é realizado com dois acessos. 

Fique a vontade para contribuir como achar possível. 

Eu usei o Docker para rodar o Keycloack com o seguinte comando:

docker run -p 8080:8080 -e KEYCLOAK-USER=admin -e KEYCLOAK-PASSWORD=admin quay.io/keycloak/keycloak:12.0.4

De acordo com a configuração que definimos, poderemos acessar o Keycloak no link localhost:8080, e o console de administração com o login admin e a senha admin.
No menu lateral, acesse Client/admin-cli e na aba Settings altere o Access Type para confidencial e deixe as opções Enable, Direct Access Grants Enabled e Service Accounts Enabled com o valor ON.

![keycloak_1](https://user-images.githubusercontent.com/41458425/113722331-b20fc100-96c6-11eb-9678-91539daf4376.png)

Na aba credentials poderemos obter o ClientSecret que será usado na nossa configuração.

Em seguida vamos na aba Mapper e clicar em create para criar um novo Mapper Protocol.

informe os seguintes valores no formulário:

- `Name: Audience`
- `Mapper type: Audience`
- `Included Client Audience: [Client ID]`

![keycloak_01](https://user-images.githubusercontent.com/41458425/113723977-44649480-96c8-11eb-8fae-3cda99c8e72a.png)

### Vamos criar a nossa role

Vá na aba Role  e clique em Add Role
Digite o nome desejado no campo Role Name, que no nosso caso é administrator e salve.

`Para o nosso primeiro acesso, essa role será atribuída manualmente ao nosso usuário mas depois de tudo configurado isso poderá ser feito atravéz da nossa biblioteca.`

Volte ao menu lateral, selecione Users e clique no id do usuário desejado para receber a role.
Na aba Role Mappings vá até client roles e selecione o client, nesse caso, admin-cli.
As roles disponíveis irão aparecer na caixa Available Roles. Selecione a role e clique em Add Selected.

![keycloak3](https://user-images.githubusercontent.com/41458425/113726747-e08f9b00-96ca-11eb-9c3c-c3ee830b7f12.png)


Pronto! A configuração do Keycloak foi finalizada.

### Variáveis de ambiente
No nosso exemple a url que aponta para a interface de admin do Keycloak é http://localhost:8080 por default, mas se você quiser passar esse valor
por uma variável de ambiente, bem comum em containers docker, basta definir o endereço desejado para a variável  SECURITY_URL

As opções para variáveis de ambiete são:

DB-ADDRESS: 172.17.0.1
KEYTOCREATETOKEN: admin-admin
SECURITY_URL: http://172.17.0.1:8080/
ADDUSER_ROLE_URL: auth/admin/realms/master/users/[USER_UUID]/role-mappings/clients/[CLIENT_UUID]
METADATA_URL: auth/realms/master/.well-known/openid-configuration
USER_URL: auth/admin/realms/master/users
CLIENT_ID: admin-cli
CLIENT_SECRET: 07d35c9a-410c-4b61-9137-afb7bef2f8e8
ROLES: "[{\"id\":\"66f92784-1f53-4c4c-b1a3-cbc49337b8de\",\"name\":\"administrator\",\"composite\":false,\"clientRole\":true,\"containerId\":\"9e4c5cac-a281-4dcc-839f-6b5f3b364bc6\"},{\"id\":\"91177d54-d041-42f9-b573-e83d18a22dc0\",\"name\":\"user\",\"composite\":false,\"clientRole\":true,\"containerId\":\"9e4c5cac-a281-4dcc-839f-6b5f3b364bc6\"}]"


# Agora vamos configurar nosso projeto dotnet

Vamos alterar o arquivo Startup.cs nos seguintes pontos:

```
 public Startup(IWebHostEnvironment env, IConfiguration configuration)
 {
    Configuration = configuration;
    Environment = env;
 }
 ```
 ```
 ...
 public IWebHostEnvironment Environment { get; }
 ...
 ```

### Alterabdo o método ConfigureServices

```
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
```
        
 No método Configure adcione a chamada para UseAuthentication
```
  ...
  app.UseRouting();
  app.UseAuthentication();
  app.UseAuthorization();
  ...
  ```

Crie o método MapKeycloakRolesToRoleClaims
```
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
```
        
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

Essa é um exemplo de chamada para realizar o login no Keycloak com o Adapter

```
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
```

Esse método cria o usuário e atribui uma role para ele.
A role está no appsettings.json com a chave Roles.

```
[HttpPost]
[Authorize(Roles = "administrator")]
public async Task<IActionResult> Post([FromBody] User accessUser)
{
      IActionResult result = default;

      string jwt = Request.Headers["Authorization"];
      int statusCodeResult = accessKeycloakData.TryCreateUser(jwt, accessUser).Result;

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
```
    
