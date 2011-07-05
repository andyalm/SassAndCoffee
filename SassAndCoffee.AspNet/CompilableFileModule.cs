﻿namespace SassAndCoffee.AspNet
{
    using System.IO;
    using System.Web;

    using SassAndCoffee.Core;
    using SassAndCoffee.Core.Caching;

//#define MEMORY_CACHE

    public class CompilableFileModule : IHttpModule, ICompilerHost
    {
        IContentCompiler _compiler;

        IHttpHandler _handler;

	// XXX: Can we hold onto the App pointer like this? Won't ASP.NET hate 
	// on us if we try to touch this outside of an active request?
        HttpApplication _application;

        public void Init(HttpApplication context)
        {
            _application = context;

            // TODO - add web.config entry to allow cache configuration

#if MEMORY_CACHE
            _compiler = new ContentCompiler(this, new InMemoryCache());
#else
            var cachePath = Path.Combine(System.Web.Hosting.HostingEnvironment.MapPath("~/App_Data"), ".sassandcoffeecache");
            if (!Directory.Exists(cachePath)) {
                Directory.CreateDirectory(cachePath);
            }

            _compiler = new ContentCompiler(this, new FileCache(cachePath)); 
#endif            

            _handler = new CompilableFileHandler(this._compiler);

            context.PostResolveRequestCache += (o, e) => {
                var app = o as HttpApplication;

                if (!_compiler.CanCompile(app.Request.Path)) {
                    return;
                }

                app.Context.RemapHandler(_handler);
            };
        }

        public string ApplicationBasePath {
            get {
                return _application.Request.PhysicalApplicationPath;
            }
        }

        public string MapPath(string path)
        {
            return _application.Server.MapPath(path);
        }

        public void Dispose()
        {
        }
    }
}
