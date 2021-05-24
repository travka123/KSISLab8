using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Newtonsoft.Json;
using System.IO;

namespace KSISLab8
{
    public class Startup {
        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services) {
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            app.UseAuthorization();

            var routeBuilder = new RouteBuilder(app);

            routeBuilder.MapPut("path/to/{file}",
                async context => {
                    using (FileStream fs = File.Create($"Content\\{context.GetRouteValue("file")}")) {
                        await context.Request.Body.CopyToAsync(fs);
                    }
                });

            routeBuilder.MapGet("path/to/{file}",
                async context => {
                    try {
                        await context.Response.WriteAsync(File.ReadAllText($"Content\\{context.GetRouteValue("file")}"));
                    }
                    catch {
                        context.Response.StatusCode = 404;
                    }
                });

            routeBuilder.MapDelete("path/to/{file}",
                async context => {
                    FileInfo fileInfo = new FileInfo($"Content\\{context.GetRouteValue("file")}");
                    if (fileInfo.Exists) {
                        fileInfo.Delete();
                    }
                    else {
                        context.Response.StatusCode = 404;
                    }
                });

            routeBuilder.MapRoute("path/to/{file}",
                async context => {
                    try {
                        FileInfo fileInfo = new FileInfo($"Content\\{context.GetRouteValue("file")}");
                        if (fileInfo.Exists) {
                            if (HttpMethods.IsHead(context.Request.Method)) {
                                context.Response.Headers.Add("File-FullName", fileInfo.FullName);
                                context.Response.Headers.Add("File-Length", fileInfo.Length.ToString());
                                context.Response.Headers.Add("File-CreationTime", fileInfo.CreationTime.ToString());
                            }
                        }
                        else {
                            context.Response.StatusCode = 404;
                        }
                    }
                    catch {
                        context.Response.StatusCode = 404;
                    }
                });

            routeBuilder.MapGet("api/files",
                async context => {
                    await context.Response.WriteAsync(JsonConvert.SerializeObject(Directory.GetFiles("Content"), Formatting.Indented));
                });

            app.UseRouter(routeBuilder.Build());
        }
    }
}
