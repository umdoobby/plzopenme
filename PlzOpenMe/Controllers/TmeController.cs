using System;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using PlzOpenMe.Models;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using JsonSerializer = Newtonsoft.Json;

namespace PlzOpenMe.Controllers
{
    [Produces("application/json")]
    public class TmeController : Controller
    {
        // create the context for this controller
        private readonly PlzOpenMeContext _dbContext;
        
        // create the configuration for this controller
        private readonly IConfiguration _configuration;
        
        // create the telegram bot client
        private readonly TelegramBotClient _bot;

        // grab injected services
        public TmeController(PlzOpenMeContext dbContext, IConfiguration configuration)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _bot = new TelegramBotClient(_configuration.GetValue<string>("TelegramApiKey"));
        }
        
        // Telegram Webhook Uri
        [HttpPost]
        [Consumes("application/json")]
        public IActionResult Hook_NUUKe7vFH5()
        {
            // create a memory stream and read the request into it
            MemoryStream ms = new MemoryStream();
            HttpContext.Request.Body.CopyToAsync(ms);
            
            // convert the body into the update object
            Update update = JsonSerializer.JsonConvert.DeserializeObject<Update>(Encoding.UTF8.GetString(ms.ToArray()));
            
            Log.Debug(Encoding.UTF8.GetString(ms.ToArray()));
            
            // pull out that user and the message
            User updateFrom = update.Message.From;
            Message updateMessage = update.Message;
            
            // see if we are dealing with a bot
            if (updateFrom.IsBot)
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, "I'm sorry my bot brethren but I cannot help you. I can only help humans.");
                return Json(true);
            }

            // build query to see if we can find that user by their telegram id
            var userQuery = from pu in _dbContext.PomUsers
                where pu.UserId == updateFrom.Id
                select pu;
            
            // pull the user
            PomUser fromUser = userQuery.FirstOrDefault<PomUser>();

            // see if we have the user
            if (fromUser == null)
            {
                // set up the new user
                fromUser = new PomUser()
                {
                    CreatedOn = DateTime.Now,
                    Agreed = null,
                    AgreedOn = null,
                    UserId = updateFrom.Id
                };

                // save the new user
                _dbContext.PomUsers.Add(fromUser);
                _dbContext.SaveChanges();
                
                // send the reply
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, "I'm sorry my bot brethren but I cannot help you. I can only help humans.");
            }
            
            


            Log.Information($"result: {update.Id}:{update.Message.Text}");
            return Json(true);
        }
    }
}