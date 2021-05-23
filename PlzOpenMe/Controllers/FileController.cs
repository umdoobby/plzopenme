using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlzOpenMe.Models;

namespace PlzOpenMe.Controllers
{
    public class FileController : Controller
    {
        // create the context for this controller
        private readonly PlzOpenMeContext _dbContext;

        // create the configuration for this controller
        private readonly IConfiguration _configuration;

        // configure the different services required for this controller
        public FileController(PlzOpenMeContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
        }

        // main response for viewing a file
        public IActionResult Index(string id)
        {
            // see if we have a file id
            if (!String.IsNullOrWhiteSpace(id))
            {
                
            }
            
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel {RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier});
        }
    }
}