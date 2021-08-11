using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using nClam;
using PlzOpenMe.Models;
using Serilog;
using Telegram.Bot;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Telegram.Bot.Types.ReplyMarkups;
using JsonSerializer = Newtonsoft.Json;

namespace PlzOpenMe.Controllers
{
    [Produces("application/json")]
    public class TelegramController : ControllerBase
    {
        // create the context for this controller
        private readonly PlzOpenMeContext _dbContext;

        // create the configuration for this controller
        private readonly IConfiguration _configuration;

        // create the log system
        private readonly ILogger<TelegramController> _logger;

        // create the telegram bot client
        private readonly TelegramBotClient _bot;

        // grab injected services
        public TelegramController(PlzOpenMeContext dbContext, IConfiguration configuration, ILogger<TelegramController> logger)
        {
            _dbContext = dbContext;
            _configuration = configuration;
            _logger = logger;
            _bot = new TelegramBotClient(_configuration.GetValue<string>("TelegramApiKey"));
        }
        
        // Telegram Webhook Uri
        [HttpPost]
        [Consumes("application/json")]
        public JsonResult Hook_SecretHookAddress()
        {
            // create a memory stream and read the request into it
            MemoryStream ms = new MemoryStream();
            HttpContext.Request.Body.CopyToAsync(ms);

            // convert the body into the update object
            Update update = JsonSerializer.JsonConvert.DeserializeObject<Update>(Encoding.UTF8.GetString(ms.ToArray()));

            // make sure we have some kind of message
            if (update.Message == null)
            {
                Log.Error("Received an empty or null message");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", false},
                    {"reason", "message was empty"}
                });
            }

            // pull out that user and the message
            User updateFrom = update.Message.From;
            Message updateMessage = update.Message;
            
            // make sure that we have a user
            if (updateFrom == null)
            {
                Log.Error($"Message {update.Id} had an empty from field");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", false},
                    {"reason", "from user was empty"}
                });
            }

            // see if we are dealing with a bot
            if (updateFrom.IsBot)
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, "I'm sorry but I can't help other bots.");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", false},
                    {"reason", "messages from other bots are not allowed"}
                });
            }
            
            // lets see if the message text was null
            if (String.IsNullOrWhiteSpace(updateMessage.Text))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                    "Sorry, that message appeared to be empty... Can you try again?",
                    ParseMode.Html);
                Log.Information($"Message {update.Id} from user {updateFrom.Id} is from a new user and appeared to be blank");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", true}
                });
            }

            // build query to see if we can find that user by their telegram id
            var userQuery = from pu in _dbContext.PomUsers
                where pu.UserId == updateFrom.Id
                select pu;

            // pull the user from the query
            PomUser fromUser = userQuery.FirstOrDefault<PomUser>();

            // see if that user exists
            if (fromUser == null)
            {
                // so we don't know this user, they only have a few options

                // if they aren't sending me a command that they are allowed to say right now then we will tell them what they can do
                if (updateMessage.Entities == null)
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "Hi stranger! I can help you share media and files with people outside of Telegram.\nTo get started say /start or you can say /help for what you can do right now.",
                        ParseMode.Html);
                    Log.Information($"Message {update.Id} from user {updateFrom.Id} is from a new user, sent them the stating welcome message");
                    return new JsonResult(new Dictionary<string, object>()
                    {
                        {"success", true}
                    });
                }

                // if they want info, lets tell them about us
                if (updateMessage.Text.StartsWith("/info"))
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "Let me introduce myself, I am PlzOpenMe, a file and media sharing platform integrated with Telegram. I will take any files you send me, repack them for anyone to see, " +
                        "and give you a link that you can use to share with your friends outside of Telegram. For more information please go to https://plzopen.me.",
                        ParseMode.Html);
                    Log.Information($"Message {update.Id} from user {updateFrom.Id} is from a new user and requested more info");
                    return new JsonResult(new Dictionary<string, object>()
                    {
                        {"success", true}
                    });
                }
                
                // if they are asking for help we need to tell them what they can do
                if (updateMessage.Text.StartsWith("/help"))
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "Say /start to get me ready to start receiving your files.\nSay /info to learn more about me.\nFinally, say /help to have this repeated.",
                        ParseMode.Html);
                    Log.Information($"Message {update.Id} from user {updateFrom.Id} is from a new user and requested the help info");
                    return new JsonResult(new Dictionary<string, object>()
                    {
                        {"success", true}
                    });
                }
                
                // if they want to start the service
                if (updateMessage.Text.StartsWith("/start"))
                {
                    // set up the new user
                    try
                    {
                        fromUser = new PomUser()
                        {
                            CreatedOn = DateTime.Now,
                            Agreed = null,
                            AgreedOn = null,
                            UserId = updateFrom.Id
                        };

                        // save the new user
                        _dbContext.PomUsers.Add(fromUser);
                        int makeUserResult = _dbContext.SaveChanges();

                        // log success
                        Log.Information(
                            $"The user {updateFrom.Id}/{updateFrom.Username} has started the bot successfully with result code {makeUserResult}");
                    }
                    catch (Exception e)
                    {
                        // we failed to set up the user for some reason
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                            "This is embarrassing... I encountered an error trying to handle your request. I have alerted my admin about the failure, please try again in a few minutes.",
                            ParseMode.Html);
                        Log.Error(e,
                            $"There was an error while trying to create the user for {updateFrom.Id}/{updateFrom.Username}");
                        return new JsonResult(new Dictionary<string, object>()
                        {
                            {"success", false},
                            {"reason", "failed to create the PlzOpen.Me user file for this Telegram user"}
                        });
                    }
                    
                    List<KeyboardButton> replyOptions = new List<KeyboardButton>();
                    replyOptions.Add(new KeyboardButton("I agree"));
                    replyOptions.Add(new KeyboardButton("Stop"));

                    // respond with the agreement message
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "Wonderful! To start sharing files, I just need to you to do one thing.\n" +
                        "PlzOpenMe is a file sharing platform and so I need you to understand what you are " +
                        "allowed to share, what I'm willing to accept, and what I do with your information.\n" +
                        "Please read these two pages; https://plzopen.me/Home/Privacy and " +
                        "https://plzopen.me/Home/Terms.\n\n" +
                        "Respond with \"I agree\" to say that you have read, understand, and agree to the PlzOpen.Me " +
                        "privacy policy and terms of service. If you do not agree, respond with \"Stop\" and I will " +
                        "delete any information I have collected about your Telegram account.",
                        ParseMode.Html,null,false,false, 0, false,
                        new ReplyKeyboardMarkup(replyOptions));
                    Log.Information($"Message {update.Id} from user {updateFrom.Id} has started the bot, send them the privacy and TOS pages");
                    return new JsonResult(new Dictionary<string, object>()
                    {
                        {"success", true}
                    });
                }
                
                // sending anything else we should just tell them that I don't understand
                _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                    "I'm sorry but I don't understand that right now. You can say /help for what you can do right now " +
                    "or you can say /start to get started.", ParseMode.Html);
                Log.Information($"Message {update.Id} from user {updateFrom.Id} has sent me a command that we don't understand");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", true},
                    {"result", "unknown command at this stage"}
                });
            }
            
            // so if we have the user there is a chance that they haven't actually agreed yet
            if (!fromUser.Agreed.HasValue)
            {
                // are you sending me a command
                if (updateMessage.Entities == null)
                {
                    // see if they agreed
                    if (updateMessage.Text.StartsWith("I agree"))
                    {
                        try
                        {
                            // add the agreement info to their account and update them
                            fromUser.Agreed = true;
                            fromUser.AgreedOn = DateTime.Now;
                            _dbContext.Update(fromUser);
                            int agreeUserResult = _dbContext.SaveChanges();

                            // log success
                            Log.Information(
                                $"The user {updateFrom.Id}/{updateFrom.Username} and agreed and their account has been updated with result {agreeUserResult}");

                            // respond with success
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                "Awesome! Alright, with that out of the way you're officially all set up and I'm ready to help! " +
                                "All you have to do is send me a message with some kind of media attached to it and I will automatically " +
                                "set it up for you to share. When I'm done packaging it, I will respond with a URL that you can share with " +
                                "anyone, over any messaging platform. It doesn't matter if you are forwarding me a message from a group, " +
                                "another bot, a direct message, or you sent me it directly. I will always try to grab that file, repack it " +
                                "and give you a sharable URL. However, know that Telegram won't let me download any files over 20MB.\n\n" +
                                "Also if you are interested in my development, how I work, or just generally curious; I'm open source! " +
                                "You can see issues, upcoming features, recommend new features, report bugs, etc at my GitHub repository!\n" +
                                "https://github.com/umdoobby/plzopenme \n" +
                                "If you really like me, maybe give me a star and/or a watch on there as well? No pressure though.\n\n" +
                                "Finally, you might want to consider joining my update channel.\n" +
                                "https://t.me/pomUpdates\n" +
                                "That's where I announce new features, major issues, etc. In case you want to hear about the latest " +
                                "in protogen packaging technology!... Or if I crash or something I guess... That's all I have to share, " +
                                "thanks again for giving me a shot!", ParseMode.Html);
                            Log.Information($"Message {update.Id} from user {updateFrom.Id} has agreed to the terms and their account was set up");
                            return new JsonResult(new Dictionary<string, object>()
                            {
                                {"success", true},
                                {"result", "user's account was configured correctly"}
                            });
                        }
                        catch (Exception e)
                        {
                            // for some reason we failed update the user
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                "This is embarrassing... I encountered an error trying to handle your request. I have alerted my admin about the failure, please try again in a few minutes.");
                            Log.Error(e,
                                $"There was an error while attempting to save the agreement details of user {updateFrom.Id}/{updateFrom.Username}");
                            return new JsonResult(new Dictionary<string, object>()
                            {
                                {"success", false},
                                {"result", "user's account was configured correctly"}
                            });
                        }
                    }
                    
                    // see if the user wanted to stop
                    if (updateMessage.Text.StartsWith("Stop"))
                    {
                        try
                        {
                            // remove them
                            _dbContext.Remove(fromUser);
                            int delUserResult = _dbContext.SaveChanges();

                            // log success
                            Log.Information(
                                $"The user {updateFrom.Id}/{updateFrom.Username} requested to stop the bot and has been removed from the database with result code {delUserResult}");

                            // respond with success
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                "I'm sorry I couldn't be more help. Your Telegram information has been removed from my database.");
                            Log.Information($"Message {update.Id} from user {updateFrom.Id} has requested to stop the bot");
                            return new JsonResult(new Dictionary<string, object>()
                            {
                                {"success", true},
                                {"result", "user's account was deleted successfully"}
                            });
                        }
                        catch (Exception e)
                        {
                            // for some reason we failed to remove the user
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                "This is embarrassing... I encountered an error trying to handle your request. I have alerted my admin about the failure, please try again in a few minutes.");
                            Log.Error(e,
                                $"There was an error while attempting to remove user {updateFrom.Id}/{updateFrom.Username}");
                            return new JsonResult(new Dictionary<string, object>()
                            {
                                {"success", false},
                                {"result", "user's account could not be deleted"}
                            });
                        }
                    }
                    
                    // anything else at this stage is not acceptable
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "I'm sorry but I can only continue to help if you agree to my terms of service and " +
                        "my privacy policy. I'm not a lawyer so they aren't long or complicated, I promise! You can say " +
                        "/start to have the start message repeated or /help to see all of the commands you can use right now.");
                    Log.Information($"Message {update.Id} from user {updateFrom.Id} has sent me a command that we don't understand before agreeing the bot");
                    return new JsonResult(new Dictionary<string, object>()
                    {
                        {"success", true},
                        {"result", "unknown command given at this state"}
                    });
                }

                // if they want to start the service
                if (updateMessage.Text.StartsWith("/start"))
                {
                    List<KeyboardButton> replyOptions = new List<KeyboardButton>();
                    replyOptions.Add(new KeyboardButton("I agree"));
                    replyOptions.Add(new KeyboardButton("Stop"));
                    
                    // respond with the agreement message
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "To start sharing files, I just need to you to do one thing.\n" +
                        "PlzOpenMe is a file sharing platform and so I need you to understand what you are " +
                        "allowed to share, what I'm willing to accept, and what I do with your information.\n" +
                        "Please read these two pages; https://plzopen.me/Home/Privacy and " +
                        "https://plzopen.me/Home/Terms.\n\n" +
                        "Select \"I agree\" to say that you have read, understand, and agree to the PlzOpen.Me " +
                        "privacy policy and terms of service. If you do not agree, choose \"Stop\" and I " +
                        "will delete any information I have collected about your Telegram account from my database.",
                        ParseMode.Html,null,false,false, 0, false,
                        new ReplyKeyboardMarkup(replyOptions));
                    Log.Information($"Message {update.Id} from user {updateFrom.Id} has sent me the start command");
                    return new JsonResult(new Dictionary<string, object>()
                    {
                        {"success", true},
                        {"result", "responded with signup info"}
                    });
                }

                // are they asking for info
                if (updateMessage.Text.StartsWith("/info"))
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "Let me introduce myself, I am PlzOpenMe, a file and media sharing platform integrated with Telegram. I will take any files you send me, repack them for anyone to see, " +
                        "and give you a link that you can use to share with your friends outside of Telegram. For more information please go to https://plzopen.me.");
                    Log.Information($"Message {update.Id} from user {updateFrom.Id} has sent me the info command");
                    return new JsonResult(new Dictionary<string, object>()
                    {
                        {"success", true},
                        {"result", "responded with bot info"}
                    });
                }

                // if they are asking for help we need to tell them what they can do
                if (updateMessage.Text.StartsWith("/help"))
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "Say /start to have me repeat the starting instructions again.\n" +
                        "Unfortunately, I cannot help you anymore until you fallow my instructions in my " +
                        "/start welcome message. Other then that, say /info to learn more about me or say " +
                        "/help to have this repeated.", ParseMode.Html);
                    Log.Information($"Message {update.Id} from user {updateFrom.Id} has sent me the help command");
                    return new JsonResult(new Dictionary<string, object>()
                    {
                        {"success", true},
                        {"result", "responded with help info"}
                    });
                }

                // sending anything else we should just tell them that I don't understand
                _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                    "I'm sorry but I don't understand that right now. You can say /help for what you can do right now " +
                    "or you can say /start to get started.", ParseMode.Html);
                Log.Information($"Message {update.Id} from user {updateFrom.Id} has sent me a message that didn't contain any commands before agreeing to the bot");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", false},
                    {"result", "no command found in the message"}
                });
            }
            
            // check and see if they did not agree and their account wasn't deleted for some reason
            if (!fromUser.Agreed.Value)
            {
                // this realistically should never happen but I don't want to chance it
                // fallback response
                Log.Information(
                    $"The user {updateFrom.Id}/{updateFrom.Username} was blocked with agreement status {fromUser.Agreed.Value}");
                _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                    "I'm sorry but you did not agree to my Terms of Service and my Privacy Policy. " +
                    "There is nothing more that I can do for you until you review those and agree. If you believe you are getting this message in error, please report it " +
                    "in my GitHub at https://github.com/umdoobby/plzopenme. Sorry I can't be more help.", ParseMode.Html);
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", false},
                    {"result", "user has not agreed to the bot's terms"}
                });
            }
            
            // check and see if this user is banned
            if (fromUser.Banned)
            {
                // log the unwelcome communication
                Log.Information($"The user {updateFrom.Id}/{updateFrom.Username} was blocked due to being banned");

                // just to make sure there is never a null date
                if (fromUser.BannedOn.HasValue)
                {
                    // respond with banned message
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        $"I'm sorry but your account was banned from PlzOpen.Me on {fromUser.BannedOn.Value.ToString("F")}.");
                }

                // respond with banned message
                _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                    $"I'm sorry but your account was banned from PlzOpen.Me.");

                // attempt to leave the chat
                _bot.LeaveChatAsync(updateMessage.Chat.Id);
                Log.Information($"Automatically left the chat id {updateMessage.Chat.Id}");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", false},
                    {"result", "user has been banned from using this bot"}
                });
            }
            
            // at this point we know that we have a valid, not banned user 
            
            // if there are no entities then there better be a file attached or i won't know what to do
            if (updateMessage.Entities == null)
            {
                try
                {
                    // set up some variables
                    UploadedFile main = null;
                    UploadedFile thumb = null;
                    bool foundFile = false;
                    Log.Information($"Found a file for {updateFrom.Id}/{updateFrom.Username}");
                    
                    // Animation file
                    if (updateMessage.Animation != null)
                    {
                        Log.Debug($"User {updateFrom.Id} submitted an animation ID={updateMessage.Animation.FileId} FuID={updateMessage.Animation.FileUniqueId}");
                        foundFile = true;
                    }
            
                    // Audio file
                    if (updateMessage.Audio != null && !foundFile)
                    {
                        Log.Debug($"User {updateFrom.Id} submitted an audio file ID={updateMessage.Audio.FileId} FuID={updateMessage.Audio.FileUniqueId}");
                        foundFile = true;
                    }

                    // Document file
                    if (updateMessage.Document != null && !foundFile)
                    {
                        Log.Debug($"User {updateFrom.Id} submitted a document file ID={updateMessage.Document.FileId} FuID={updateMessage.Document.FileUniqueId}");
                        foundFile = true;
                    }

                    // Photo files
                    if (updateMessage.Photo != null && !foundFile)
                    {
                        Log.Debug($"User {updateFrom.Id} submitted a document file ID={updateMessage.Document.FileId} FuID={updateMessage.Document.FileUniqueId}");
                        foundFile = true;
                    }
                }
                catch (Exception e)
                {
                    // so there was an error while trying to save the file
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "This is embarrassing... I encountered an error trying to handle your request. I have alerted my admin about the failure, please try again in a few minutes.\n" +
                        "If you can, I'd highly recommend you report this issue on https://github.com/umdoobby/plzopenme so that it might help everyone.");
                    Log.Error(e,
                        $"There was an error while trying to save a file for {updateFrom.Id}/{updateFrom.Username}");
                    return new JsonResult(new Dictionary<string, object>()
                    {
                        {"success", false},
                        {"result", "user's account could not be deleted"}
                    });
                }
            }

            // so lets go through the different chat options first
            // so there is a command lets see what they want to do and do it
            // start with the help command
            if (updateMessage.Text.StartsWith("/help"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                    $"You account is fully set up and active! To upload a file you simply " +
                    $"need to forward me the message containing the file. However, I can help with a few other things. " +
                    $"Here's a list of all valid commands:\n" +
                    $"/help to repeat this info\n" +
                    $"/info to learn about PlzOpen.Me\n" +
                    $"/getLinks to return a list of all valid links tied to your account\n" +
                    $"/deleteLink <PlzOpen.Me link> to delete the specified file and link\n" +
                    $"/getMyInfo to return a text report of all information we have on your account\n" +
                    $"/stop to delete your account and all files associated to it", ParseMode.Html);
                Log.Information($"POM User {updateFrom.Id}/{updateFrom.Username} requested help information");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", true},
                    {"result", "responded with help information"}
                });
            }

            // info command
            if (updateMessage.Text.StartsWith("/info"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                    $"PlzOpen.Me is an open source file sharing platform allowing you to easily " +
                    $"send files from Telegram to anyone on any messaging platform. All you have to do is " +
                    $"forward any message containing a file, image, video, etc. and I will respond with a sharable link " +
                    $"to the file on the PlzOpen.Me website.\n\n" +
                    $"Please report any issues, bugs, or whatever else on the PlzOpen.Me GitHub page.\n" +
                    $"https://github.com/umdoobby/plzopenme\n" +
                    $"please consider giving the project a star and maybe a watch if you have found it useful!\n\n" +
                    $"If you are interested in keeping up with the latest news, upcoming features, " +
                    $"or any announcements please consider joining the PlzOpen.Me announcements channel here on Telegram.\n" +
                    $"https://t.me/pomUpdates\n\n" +
                    $"More information about this service can be found on https://plzopen.me\n" +
                    $"I hope you found this helpful!");
                Log.Information($"POM User {updateFrom.Id}/{updateFrom.Username} requested the bot's information");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", true},
                    {"result", "responded with bot information"}
                });
            }

            // start command
            if (updateMessage.Text.StartsWith("/start"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                    $"Your account is already set up! You don't have to start again.");
                Log.Information($"POM User {updateFrom.Id}/{updateFrom.Username} tried to start the bot again");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", false},
                    {"result", "user has already started the bot"}
                });
            }

            // stop and delete
            if (updateMessage.Text.StartsWith("/stop"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"placeholder for deleting the account");
                Log.Information($"POM User {updateFrom.Id}/{updateFrom.Username} tried to stop the bot");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", false},
                    {"result", "stop placeholder"}
                });
            }

            // credits
            if (updateMessage.Text.StartsWith("/credits"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"this is the credits");
                Log.Information($"POM User {updateFrom.Id}/{updateFrom.Username} tried to request bot's credits");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", false},
                    {"result", "credits placeholder"}
                });
            }

            // list my links
            if (updateMessage.Text.StartsWith("/getLinks"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"This is a list of your links");
                Log.Information($"POM User {updateFrom.Id}/{updateFrom.Username} tried to request their links");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", false},
                    {"result", "links placeholder"}
                });
            }

            // get account report
            if (updateMessage.Text.StartsWith("/getMyInfo"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"This is all of the info we have on you");
                Log.Information($"POM User {updateFrom.Id}/{updateFrom.Username} tried to request all of their info");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", false},
                    {"result", "get my info placeholder"}
                });
            }

            // delete a file
            if (updateMessage.Text.StartsWith("/deleteLink"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"This is you wanting to delete a link");
                Log.Information($"POM User {updateFrom.Id}/{updateFrom.Username} tried to request a link be removed");
                return new JsonResult(new Dictionary<string, object>()
                {
                    {"success", false},
                    {"result", "delete link placeholder"}
                });
            }

            // finally you must have sent a command that i don't know
            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                $"I'm sorry but I don't know that command. Use the /help command for a list of all valid commands.");
            Log.Information($"POM User {updateFrom.Id}/{updateFrom.Username} tried to request bot's credits");
            return new JsonResult(new Dictionary<string, object>()
            {
                {"success", false},
                {"result", "credits placeholder"}
            });
        }


        public UploadedFile SaveAnimation(Animation animation)
        {
            UploadedFile rtn = null;
            int max = _configuration.GetValue<int>("MaxAttempts:FileId");
            int attempt = 0;

            while (rtn == null && attempt <= max)
            {
                // try to find the animation
                var fileQuery = from uf in _dbContext.PomFiles
                    where uf.FileUniqueId == animation.FileUniqueId
                    select uf;

                // see if the file is already in the database
                if (fileQuery.Any())
                {
                    
                }
                
                // add one to the attempt
                attempt++;
            }

            return rtn;
        }

        /// <summary>
        /// build a unique link to a file
        /// </summary>
        /// <returns>unique string</returns>
        /// <exception cref="DataException">failed to make a new link string</exception>
        private string MakeLink()
        {
            // make a couple of variables
            string rtn = "";
            bool unique = false;
            int maxAttempts = _configuration.GetValue<int>("MaxAttempts:LinkId");
            int attempt = 0;

            // try to make a unique string
            while (!unique && attempt <= maxAttempts)
            {
                // make the string
                rtn = KeyGenerator.GetUniqueKey(_configuration.GetValue<int>("LinkLength"));

                // built the query
                var linkQuery = from fl in _dbContext.PomLinks
                    where fl.Link == rtn
                    select fl;

                // is it unique
                if (linkQuery.Any())
                {
                    attempt++;
                }
                else
                {
                    unique = true;
                }
            }

            // did we make a new link
            if (unique)
            {
                // yes so return it
                return rtn;
            }

            // failed to make a new link
            throw new DataException("Max number of attempts to generate a unique links reached");
        }
        
        /// <summary>
        /// Download a file from telegram and scan it for viruses
        /// </summary>
        /// <param name="name">name for the new file</param>
        /// <param name="filePath">file path of the file to download</param>
        /// <param name="fileSize">file size in bytes of the file to download</param>
        /// <returns>True for a successful download or false for a filed download</returns>
        private bool Downloadfile(string name, string filePath, int fileSize)
        {
            bool rtn = false;

            // see if its too big
            if (fileSize > _configuration.GetValue<int>("MaxFileSizeBytes"))
            {
                // its too big, exit now
                return rtn;
            }

            string[] clamAvConnection = _configuration.GetConnectionString("ClamAvServer").Split(':');

            // build the download link
            string download =
                $"https://api.telegram.org/file/bot{_configuration.GetValue<string>("TelegramApiKey")}/{filePath}";

            // open a web client and download the file
            using (WebClient webClient = new WebClient())
            {
                Log.Information($"Attempting to fetch {download} to {name}");
                webClient.DownloadFile(download, _configuration.GetValue<string>("FileStore") + name);
            }

            // now that we have the file lets scan it
            Log.Information($"Scanning {name} for viruses");
            var clam = new ClamClient(clamAvConnection[0], int.Parse(clamAvConnection[1]));
            var scanResult = clam.ScanFileOnServerAsync(_configuration.GetValue<string>("FileStore") + name).Result;

            // what was the result
            switch (scanResult.Result)
            {
                case ClamScanResults.Clean:
                    // clean is an ok by me
                    Log.Information($"File {name} is clean");
                    rtn = true;
                    break;
                case ClamScanResults.VirusDetected:
                    // there is a threat in the file, just dispose of it
                    Log.Error($"File {name} contains a threat: {scanResult.InfectedFiles.First().VirusName}");
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    System.IO.File.Delete(_configuration.GetValue<string>("FileStore") + name);
                    rtn = false;
                    break;
                case ClamScanResults.Error:
                    // we failed to scan it for some reason, too risky just delete the file
                    Log.Warning($"Failed to scan file {name}\n{scanResult.RawResult}");
                    GC.WaitForPendingFinalizers();
                    GC.Collect();
                    System.IO.File.Delete(_configuration.GetValue<string>("FileStore") + name);
                    rtn = false;
                    break;
            }

            // return the result
            return rtn;
        }
        
    }
}