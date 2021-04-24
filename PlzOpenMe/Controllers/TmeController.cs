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

            // build query to see if we can find that user by their telegram id
            var userQuery = from pu in _dbContext.PomUsers
                where pu.UserId == updateFrom.Id
                select pu;
            
            // pull the user
            PomUser fromUser = userQuery.FirstOrDefault<PomUser>();

            // see if we have the user
            if (fromUser == null)
            {
                // we should try to save this user
                fromUser = new PomUser()
                {
                    Created = DateTime.Now,
                    FirstName = updateFrom.FirstName,
                    HasAgreed = false,
                    IsBot = updateFrom.IsBot,
                    LanguageCode = updateFrom.LanguageCode,
                    LastName = updateFrom.LastName,
                    UserId = updateFrom.Id,
                    Username = updateFrom.Username
                };

                _dbContext.PomUsers.Add(fromUser);
                _dbContext.SaveChanges();
            }
            
            Log.Debug($"user: {fromUser.Username}");

            // see if we have any message entities
            bool containsBotCommand = false;
            if (updateMessage.Entities != null)
            {
                // what we want is a bot command
                foreach (MessageEntity updateMessageEntity in updateMessage.Entities)
                {
                    if (updateMessageEntity.Type == MessageEntityType.BotCommand)
                    {
                        // we have a bot command
                        containsBotCommand = true;
                        break;
                    }
                }
            }

            if (fromUser.HasAgreed)
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, "You have agreed to my tos");
                
            }
            else
            {
                // did we find a bot command
                if (containsBotCommand)
                {
                    if (updateMessage.Text.EndsWith("/start", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _bot.SendTextMessageAsync(updateMessage.Chat,
                            "Welcome to PlzOpen.Me! A free file sharing platform that allows you to share content from Telegram with those friends that haven't jumped on the bandwagon yet. agree to shit.");
                    } 
                    else if (updateMessage.Text.EndsWith("/about", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _bot.SendTextMessageAsync(updateMessage.Chat,
                            "Allow me to introduce myself, I am the PlzOpen.Me bot. I will take any media you forward me, upload it to PlzOpen.Me and return to you with a link that you can share with anyone, on any platform, no accounts required!");
                    }
                    else
                    {
                        _bot.SendTextMessageAsync(updateMessage.Chat,
                            "Sorry I don't understand that command.");
                    }
                }
                else
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat,
                        "Welcome to PlzOpen.Me! To get started please enter the \"/start\" command or you can enter the \"/about\" command for more information on this bot!\nYou can also enter \"/help\" for a full list of what you can do right now."); 
                }
            }


            Log.Information($"result: {update.Id}:{update.Message.Text}");
            return Json(true);
        }
    }
}