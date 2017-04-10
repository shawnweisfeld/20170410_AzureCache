using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using System.Web.UI;
using System.Configuration;
using StackExchange.Redis;
using System.Data.SqlClient;
using Microsoft.ApplicationInsights;

namespace CacheDemo.Controllers
{
    public class HomeController : Controller
    {
        TelemetryClient _ai = null;
        public HomeController()
        {
            _ai = new TelemetryClient();
        }

        public ActionResult Index()
        {
            return View();
        }

        public ActionResult Database()
        {
            var connString = ConfigurationManager.ConnectionStrings["AdventureWorks"].ConnectionString;
            var sql = "SELECT GETDATE()";
            var date = DateTime.MinValue;
            using (SqlConnection conn = new SqlConnection(connString))
            {
                SqlCommand cmd = new SqlCommand(sql, conn);
                try
                {
                    conn.Open();
                    date = (DateTime)cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    _ai.TrackException(ex);
                }
            }

            var message = $"The time is now {date.ToLongTimeString()}";
            var vm = new ViewModels.Home.TheTimeViewModel()
            {
                TheTime = message
            };

            return View("TheTime", vm);
        }
        
        public ActionResult NoCache()
        {
            var message = $"The time is now {DateTime.Now.ToLongTimeString()}";
            var vm = new ViewModels.Home.TheTimeViewModel()
            {
                TheTime = message
            };

            return View("TheTime", vm);
        }

        public ActionResult CacheCacheControl()
        {
            HttpContext.Response.AddHeader("Cache-Control", "private, max-age=10");

            var message = $"The time is now {DateTime.Now.ToLongTimeString()}";
            var vm = new ViewModels.Home.TheTimeViewModel()
            {
                TheTime = message
            };

            return View("TheTime", vm);
        }

        public ActionResult CacheETag()
        {
            var myETag = $"HelloWorld-{DateTime.Now:yyyyMMddHHmm}";

            var requestedETag = Request.Headers["If-None-Match"];
            if (requestedETag == myETag)
                return new HttpStatusCodeResult(HttpStatusCode.NotModified);

            HttpContext.Response.AddHeader("ETag", myETag);

            var message = $"The time is now {DateTime.Now.ToLongTimeString()}";
            var vm = new ViewModels.Home.TheTimeViewModel()
            {
                TheTime = message
            };

            return View("TheTime", vm);
        }

        public ActionResult CDN()
        {
            return View();
        }

        public ActionResult CacheSession()
        {
            var key = "session-key";
            var message = Session[key] as string;

            if (string.IsNullOrEmpty(message))
            {
                message = $"The time is now {DateTime.Now.ToLongTimeString()}";
                Session[key] = message;
            }
             
            var vm = new ViewModels.Home.TheTimeViewModel()
            {
                TheTime = message
            };

            return View("TheTime", vm);

        }

        public ActionResult CacheSessionClear()
        {
            Session.Clear();
            return View("Index");
        }


        // Redis Connection string info
        private static Lazy<ConnectionMultiplexer> lazyConnection = new Lazy<ConnectionMultiplexer>(() =>
        {
            string cacheConnection = ConfigurationManager.AppSettings["CacheConnection"].ToString();
            return ConnectionMultiplexer.Connect(cacheConnection);
        });

        public static ConnectionMultiplexer Connection
        {
            get
            {
                return lazyConnection.Value;
            }
        }

        public ActionResult CacheRedis()
        {
            var key = "redis-key";
            IDatabase cache = Connection.GetDatabase();
            var message = string.Empty;

            if (cache.KeyExists(key))
            {
                message = cache.StringGet(key);
            }
            else
            {
                message = $"The time is now {DateTime.Now.ToLongTimeString()}";
                cache.StringSet(key, message);
            }

            var vm = new ViewModels.Home.TheTimeViewModel()
            {
                TheTime = message
            };

            return View("TheTime", vm);

        }

        public ActionResult CacheRedisClear()
        {
            var key = "redis-key";
            IDatabase cache = Connection.GetDatabase();
            cache.KeyDelete(key);

            return View("Index");
        }
    }
}