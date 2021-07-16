using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json.Linq;
using System.Security.Claims;

namespace KeycloakTry2
{
    public class Startup
    {
        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            Configuration = configuration;
            Environment = env;
        }

        public IConfiguration Configuration { get; }
        public IWebHostEnvironment Environment { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            //services.AddCors();
            services.AddControllers();
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo { Title = "KeycloakTry2", Version = "v1" });
            });

            //EXEMPLO
            //services.AddCors(options =>
            //{
            //    options.AddDefaultPolicy(builder =>
            //    builder
            //    .WithMethods("GET", "POST", "PUT")
            //    .WithOrigins("http://exemplo.com","http://outroendereco.com")
            //    .SetIsOriginAllowedToAllowWildcardSubdomains()
            //    //.WithHeaders(HeaderNames.ContentType, "x-custom-header")
            //    .AllowAnyHeader());
            //});


            ////EXEMPLO
            //services.AddCors(options =>
            //{
            //    options.AddPolicy("Development", builder =>
            //    builder
            //    .WithMethods("GET", "POST", "PUT")
            //    .WithOrigins("http://exemplo.com","http://outroendereco.com")
            //    .SetIsOriginAllowedToAllowWildcardSubdomains()
            //    //.WithHeaders(HeaderNames.ContentType, "x-custom-header")
            //    .AllowAnyHeader());
            //});

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;

            }).AddJwtBearer(o =>
            {
                string urlBase = System.Environment.GetEnvironmentVariable("SECURITY_URL") ?? Configuration["keycloakData:UrlBase"];

                o.Authority = urlBase + Configuration["keycloakData:Authority"];
                o.Audience = Configuration["keycloakData:ClientId"];
                o.SaveToken = false;
                o.RequireHttpsMetadata = false;
                o.IncludeErrorDetails = true;
                o.RequireHttpsMetadata = false;
                o.MetadataAddress = urlBase + Configuration["keycloakData:MetadataUrl"];

                //o.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
                //{
                //    ValidAudiences = new string[] { "master-realm", "account", "lamp-app" }
                //};

                o.Events = new JwtBearerEvents()
                {
                    OnAuthenticationFailed = c =>
                    {
                        c.NoResult();

                        string errorTextDEfault = "An error occured processing your authentication.";

                        //c.Response.StatusCode = 500;
                        c.Response.StatusCode = 401;
                        c.Response.ContentType = "text/plain";

                        if (Environment.IsDevelopment())
                        {
                            //return c.Response.WriteAsync(c.Exception.ToString());
                            return c.Response.WriteAsync(errorTextDEfault);
                        }

                        return c.Response.WriteAsync(errorTextDEfault);
                    },

                    //OnMessageReceived = context =>
                    //{
                    //    return context.Response.WriteAsync(errorTextDEfault);
                    //},

                    OnTokenValidated = async context =>
                    {
                        MapKeycloakRolesToRoleClaims(context);
                    }


                };
            });

            //services.AddAuthorization(options =>
            //{
            //    options.AddPolicy("Admin", policy => policy.RequireClaim("user_roles", "[administrator]"));
            //});

        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseCors("Development");
                app.UseDeveloperExceptionPage();
                app.UseSwagger();
                app.UseSwaggerUI(c => c.SwaggerEndpoint("/swagger/v1/swagger.json", "KeycloakTry2 v1"));
            }

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();


            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

        }

        private static void MapKeycloakRolesToRoleClaims(TokenValidatedContext context)
        {
            var resourceAccess = JObject.Parse(context.Principal.FindFirst("resource_access").Value);
            var clientResource = resourceAccess[context.Principal.FindFirstValue("aud")];
            var clientRoles = clientResource["roles"];
            var claimsIdentity = context.Principal.Identity as ClaimsIdentity;

            if (claimsIdentity == null)
            {
                return;
            }

            foreach (var clientRole in clientRoles)
            {
                claimsIdentity.AddClaim(new Claim(ClaimTypes.Role, clientRole.ToString()));
            }

        }
    }
}
