using DevExpress.ExpressApp.Security;
using DevExpress.Persistent.Base;
using DevExpress.ExpressApp.Xpo;
using DevExpress.Persistent.BaseImpl.PermissionPolicy;
using DevExpress.Data.Filtering;
using System.Security.Principal;
using System.Security.Claims;
using Microsoft.Identity.Web;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using GitHubSecuritySample.WebApi.JWT;
using DevExpress.ExpressApp.Security.Authentication.ClientServer;
using DevExpress.ExpressApp;
using DevExpress.ExpressApp.ApplicationBuilder;

namespace GitHubSecuritySample.WebApi;

public class Startup {
    public Startup(IConfiguration configuration) {
        Configuration = configuration;
    }

    public IConfiguration Configuration { get; }

    // This method gets called by the runtime. Use this method to add services to the container.
    // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
    public void ConfigureServices(IServiceCollection services) {
        services.AddScoped<IAuthenticationTokenProvider, JwtTokenProviderService>();

        services.AddXafMiddleTier(Configuration, builder => {
            builder.ConfigureDataServer(options => {
                options.UseConnectionString(Configuration.GetConnectionString("ConnectionString"));
                options.UseDataStorePool(true);
            });

            builder.Modules
                .AddAuditTrailXpo()
                .Add<GitHubSecuritySample.Module.GitHubSecuritySampleModule>();


            builder.Security
                .UseIntegratedMode(options => {
                    options.Lockout.Enabled = true;

                    options.RoleType = typeof(PermissionPolicyRole);
                    // ApplicationUser descends from PermissionPolicyUser and supports the OAuth authentication. For more information, refer to the following topic: https://docs.devexpress.com/eXpressAppFramework/402197
                    // If your application uses PermissionPolicyUser or a custom user type, set the UserType property as follows:
                    options.UserType = typeof(GitHubSecuritySample.Module.BusinessObjects.ApplicationUser);
                    // ApplicationUserLoginInfo is only necessary for applications that use the ApplicationUser user type.
                    // If you use PermissionPolicyUser or a custom user type, comment out the following line:
                    options.UserLoginInfoType = typeof(GitHubSecuritySample.Module.BusinessObjects.ApplicationUserLoginInfo);
                    options.UseXpoPermissionsCaching();
                    options.Events.OnSecurityStrategyCreated += securityStrategy => {
                        //((SecurityStrategy)securityStrategy).AnonymousAllowedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.ModelDifference));
                        //((SecurityStrategy)securityStrategy).AnonymousAllowedTypes.Add(typeof(DevExpress.Persistent.BaseImpl.ModelDifferenceAspect));
                        ((SecurityStrategy)securityStrategy).PermissionsReloadMode = PermissionsReloadMode.CacheOnFirstAccess;
                    };
                })
                .AddPasswordAuthentication(options => {
                    options.IsSupportChangePassword = true;
                })
                .AddAuthenticationProvider<GitHubSecuritySample.Module.CustomAuthenticationProvider>();

            builder.AddBuildStep(application => {
                application.ApplicationName = "SetupApplication.GitHubSecuritySample";
                application.CheckCompatibilityType = DevExpress.ExpressApp.CheckCompatibilityType.DatabaseSchema;
                //application.CreateCustomModelDifferenceStore += (s, e) => {
                    //    e.Store = new ModelDifferenceDbStore((XafApplication)sender!, typeof(ModelDifference), true, "Win");
                    //    e.Handled = true;
                //};
#if !RELEASE
                if(application.CheckCompatibilityType == CheckCompatibilityType.DatabaseSchema) {
                    application.DatabaseUpdateMode = DatabaseUpdateMode.UpdateDatabaseAlways;
                    application.DatabaseVersionMismatch += (s, e) => {
                        e.Updater.Update();
                        e.Handled = true;
                    };
                }
#endif
            });
        });

        services.AddAuthentication()
            .AddJwtBearer(options => {
                options.TokenValidationParameters = new TokenValidationParameters() {
                    ValidateIssuerSigningKey = true,
                    //ValidIssuer = Configuration["Authentication:Jwt:Issuer"],
                    //ValidAudience = Configuration["Authentication:Jwt:Audience"],
                    ValidateIssuer = false,
                    ValidateAudience = false,
                    IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration["Authentication:Jwt:IssuerSigningKey"]))
                };
            })
            //Configure OAuth2 Identity Providers based on your requirements. For more information, see
            //https://docs.devexpress.com/eXpressAppFramework/402197/task-based-help/security/how-to-use-active-directory-and-oauth2-authentication-providers-in-blazor-applications
            //https://developers.google.com/identity/protocols/oauth2
            //https://docs.microsoft.com/en-us/azure/active-directory/develop/v2-oauth2-auth-code-flow
            //https://developers.facebook.com/docs/facebook-login/manually-build-a-login-flow
            .AddMicrosoftIdentityWebApi(
                jwtBearerOptions => {
                    jwtBearerOptions.TokenValidationParameters.NameClaimType = "preferred_username";
                },
                msIdentityOptions => {
                    Configuration.Bind("Authentication:AzureAd", msIdentityOptions);
                },
                jwtBearerScheme: "AzureAd");

        services.AddAuthorization(options => {
            options.DefaultPolicy = new AuthorizationPolicyBuilder(
                JwtBearerDefaults.AuthenticationScheme,
                "AzureAd")
                .RequireAuthenticatedUser()
                .RequireXafAuthentication()
                .Build();
        });
    }

    // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
    public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime hostApplicationLifetime) {
        if(env.IsDevelopment()) {
            app.UseDeveloperExceptionPage();
        }
        else {
            app.UseExceptionHandler("/Error");
            // The default HSTS value is 30 days. To change this for production scenarios, see: https://aka.ms/aspnetcore-hsts.
            app.UseHsts();
        }
        app.UseHttpsRedirection();
        app.UseRequestLocalization();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseXafMiddleTier();
        app.UseEndpoints(endpoints => {
            endpoints.MapControllers();
        });
    }
}
