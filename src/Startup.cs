using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Mvc;
using System.Net;
using System.Web;
using System.Collections.Generic;
using System.Collections;
using System.IO;
using ServiceStack;
using ServiceStack.Host.Handlers;
using ServiceStack.VirtualPath;
using ServiceStack.Templates;
using ServiceStack.Web;
using ServiceStack.Text;
using ServiceStack.Mvc;

namespace MvcTemplates
{
    public class Startup
    {
        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit http://go.microsoft.com/fwlink/?LinkID=398940

        static Dictionary<string,string> DocLinks = new Dictionary<string, string> 
        {
            { "installation", "Installation"},
            { "introduction", "Introduction"},
            { "syntax", "Syntax"},
            { "page-formats", "Page Formats"},
            { "arguments", "Arguments"},
            { "filters", "Filters"},
            { "default-filters", "Default Filters"},
            { "partials", "Partials"},
            { "protected-filters", "Protected Filters"},
            { "transformers", "Transformers"},
            { "view-engine", "View Engine"},
            { "model-view-controller", "Model View Controller"},
            { "code-pages", "Code Pages"},
            { "sandbox", "Sandbox"},
            { "api-reference", "API Reference"},
        };

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddMvc();

            var customFilters = new CustomTemplateFilters();

            var i = 1;
            DocLinks.ForEach((page,title) => customFilters.DocsIndex[i++] = new KeyValuePair<string, string>(
                "http://templates.netcore.io/docs/" + page,
                title
            ));

            var context = new TemplateContext { 
                TemplateFilters = { customFilters }
            };
            services.AddSingleton(context);
            services.AddSingleton(context.Pages);
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env, ILoggerFactory loggerFactory)
        {
            loggerFactory.AddConsole();

            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseStaticFiles();

            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });

            var context = app.ApplicationServices.GetService<TemplateContext>();
            context.VirtualFiles = new FileSystemVirtualFiles(env.WebRootPath);
            context.Init();
        }
    }

    public class HomeController : Controller
    {
        ITemplatePages pages;
        public HomeController(ITemplatePages pages) => this.pages = pages;

        public ActionResult Index()
        {
            return new PageResult(pages.GetPage("index")).ToMvcResult();
        }
    }

    public class CustomTemplateFilters : TemplateFilter
    {
        public Dictionary<int, KeyValuePair<string, string>> DocsIndex { get; } = new Dictionary<int, KeyValuePair<string, string>>();

        public object prevDocLink(int order)
        {
            if (DocsIndex.TryGetValue(order - 1, out KeyValuePair<string,string> entry))
                return entry;
            return null;
        }

        public object nextDocLink(int order)
        {
            if (DocsIndex.TryGetValue(order + 1, out KeyValuePair<string,string> entry))
                return entry;
            return null;
        }

        List<KeyValuePair<string,string>> sortedDocLinks;
        public object docLinks() => sortedDocLinks ?? (sortedDocLinks = sortLinks(DocsIndex));

        public List<KeyValuePair<string,string>> sortLinks(Dictionary<int, KeyValuePair<string,string>> links)
        {
            var sortedKeys = links.Keys.ToList();
            sortedKeys.Sort();

            var to = new List<KeyValuePair<string,string>>();

            foreach (var key in sortedKeys)
            {
                var entry = links[key];
                to.Add(entry);
            }

            return to;
        }
    }
}
