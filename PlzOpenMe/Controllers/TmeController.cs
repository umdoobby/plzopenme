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
        public IActionResult Hook_SecretHookAddress()
        {
            // create a memory stream and read the request into it
            MemoryStream ms = new MemoryStream();
            HttpContext.Request.Body.CopyToAsync(ms);
            
            // convert the body into the update object
            Update update = JsonSerializer.JsonConvert.DeserializeObject<Update>(Encoding.UTF8.GetString(ms.ToArray()));

            // pull out that user and the message
            User updateFrom = update.Message.From;
            Message updateMessage = update.Message;
            
            // see if we are dealing with a bot
            if (updateFrom.IsBot)
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, "I'm sorry but I can't help other bots.");
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
                // so we don't know this user, they only have a few options
                
                // if they aren't sending me a command that they are allowed to say right now then we will tell them what they can do
                if (updateMessage.Entities == null)
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id, "Hi stranger! I can help you share media and files with people outside of Telegram.\nTo get started say /start or you can say /help for what you can do right now.");
                    return Json(true);
                }

                // if they are asking for help we need to tell them what they can do
                if (updateMessage.Text.StartsWith("/help"))
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id, "Say /start to get me ready to start receiving your files.\nSay /info to learn more about me.\nFinally, say /help to have this repeated.");
                    return Json(true);
                }
                
                // if they want info, lets tell them about us
                if (updateMessage.Text.StartsWith("/info"))
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id, "Let me introduce myself, I am PlzOpenMe, a file and media sharing platform integrated with Telegram. I will take any files you send me, repack them for anyone to see, " +
                                                                     "and give you a link that you can use to share with your friends outside of Telegram. For more information please go to https://plzopen.me.");
                    return Json(true);
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
                        Log.Information($"The user {updateFrom.Id}/{updateFrom.Username} has started the bot successfully with result code {makeUserResult}");
                    }
                    catch (Exception e)
                    {
                        // we failed to set up the user for some reason
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id, "This is embarrassing... I encountered an error trying to handle your request. I have alerted by sysadmin about the failure, please try again in a few minutes.");
                        Log.Error(e, $"There was an error while trying to create the user for {updateFrom.Id}/{updateFrom.Username}");
                        return Json(false);
                    }

                    // respond with the agreement message
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id, "Wonderful! To start sharing files, I just need to you to do one thing.\n" +
                                                                     "PlzOpenMe is a file sharing platform and so I need you to understand what you are " +
                                                                     "allowed to share, what I'm willing to accept, and what I do with your information.\n" +
                                                                     "Please read these two pages; https://plzopen.me/Home/Privacy and " +
                                                                     "https://plzopen.me/Home/Terms.\n\n" +
                                                                     "Respond with /agree to say that you have read, understand, and agree to the PlzOpen.Me " +
                                                                     "privacy policy and terms of service. If you do not agree, respond with /stop and I will " +
                                                                     "delete any information I have collected about your Telegram account.");
                    return Json(true);
                }
                
                // sending anything else we should just tell them that I don't understand
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, "I'm sorry but I don't understand that right now. You can say /help for what you can do right now " +
                                                                 "or you can say /start to get started.");
                return Json(true);


            }
            
            // so if we have the user there is a chance that they haven't actually agreed yet
            if (!fromUser.Agreed.HasValue)
            {
                // are you sending me a command
                if (updateMessage.Entities == null)
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id, "I'm sorry but I can only continue to help if you agree to my terms of service and " +
                                                                     "my privacy policy. I'm not a lawyer so they aren't long or complicated, I promise! You can say " +
                                                                     "/start to have the start message repeated or /help to see all of the commands you can use right now.");
                    return Json(true);
                }
                
                // if they want to start the service
                if (updateMessage.Text.StartsWith("/start"))
                {
                    // respond with the agreement message
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id, "To start sharing files, I just need to you to do one thing.\n" +
                                                                     "PlzOpenMe is a file sharing platform and so I need you to understand what you are " +
                                                                     "allowed to share, what I'm willing to accept, and what I do with your information.\n" +
                                                                     "Please read these two pages; https://plzopen.me/Home/Privacy and " +
                                                                     "https://plzopen.me/Home/Terms.\n\n" +
                                                                     "Respond with /agree to say that you have read, understand, and agree to the PlzOpen.Me " +
                                                                     "privacy policy and terms of service. If you do not agree, respond with /stop and I " +
                                                                     "will delete any information I have collected about your Telegram account from my database.");
                    return Json(true);
                }
                
                // are they asking for info
                if (updateMessage.Text.StartsWith("/info"))
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id, "Let me introduce myself, I am PlzOpenMe, a file and media sharing platform integrated with Telegram. I will take any files you send me, repack them for anyone to see, " +
                                                                     "and give you a link that you can use to share with your friends outside of Telegram. For more information please go to https://plzopen.me.");
                    return Json(true);
                }
                
                // if they are asking for help we need to tell them what they can do
                if (updateMessage.Text.StartsWith("/help"))
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id, "Say /start to have me repeat the starting instructions again.\n" +
                                                                     "Unfortunately, I cannot help you anymore until you fallow my instructions in my " +
                                                                     "/start welcome message. Other then that, say /info to learn more about me or say " +
                                                                     "/help to have this repeated.");
                    return Json(true);
                }

                // so the user doesn't want our help? fine we will remove them
                if (updateMessage.Text.StartsWith("/stop"))
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
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id, "I'm sorry I couldn't be more help. Your Telegram information has been removed from my database.");
                        return Json(true);
                    }
                    catch (Exception e)
                    {
                        // for some reason we failed to remove the user
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id, "This is embarrassing... I encountered an error trying to handle your request. I have alerted by sysadmin about the failure, please try again in a few minutes.");
                        Log.Error(e, $"There was an error while attempting to remove user {updateFrom.Id}/{updateFrom.Username}");
                        return Json(false);
                    }
                }
                
                // they agreed! go ahead and set up their account for use
                if (updateMessage.Text.StartsWith("/agree"))
                {
                    try
                    {
                        // add the agreement info to their account and update them
                        fromUser.Agreed = true;
                        fromUser.AgreedOn = DateTime.Now;
                        _dbContext.Update(fromUser);
                        int agreeUserResult = _dbContext.SaveChanges();
                        
                        // log success
                        Log.Information($"The user {updateFrom.Id}/{updateFrom.Username} and agreed and their account has been updated with result {agreeUserResult}");
                        
                        // respond with success
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id, "Awesome! Alright, with that out of the way your officially all set up and I'm ready to help! " +
                                                                         "All you have to do is send me a message with some kind of media attached to it and I will automatically " +
                                                                         "set it up for you to share. When I'm done packaging it, I will respond with a URL that you can share with " +
                                                                         "anyone, over any messaging platform. It doesn't matter if you are forwarding me a message from a group, " +
                                                                         "another bot, a direct message, or you sent me it directly. I will always try to grab that file, repack it " +
                                                                         "and give you a sharable URL.");
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id, "Also if you are interested in my development, how I work, or just generally curious; I'm open source! " +
                                                                         "You can see issues, upcoming features, recommend new features, report bugs, etc at my GitHub repository!\n" +
                                                                         "https://github.com/umdoobby/plzopenme \n" +
                                                                         "If you really like me, maybe give me a star and/or a watch on there as well? No pressure though.");
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id, "Finally, you might want to consider joining my update channel.\n" +
                                                                         "https://t.me/pomUpdates\n" +
                                                                         "That's where I announce new features, major issues, etc. In case you want to hear about the latest " +
                                                                         "in protogen packaging technology!... Or if I crash or something I guess... That's all I have to share, " +
                                                                         "thanks again for giving me a shot!");
                        return Json(true);
                    }
                    catch (Exception e)
                    {
                        // for some reason we failed update the user
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id, "This is embarrassing... I encountered an error trying to handle your request. I have alerted by sysadmin about the failure, please try again in a few minutes.");
                        Log.Error(e, $"There was an error while attempting to save the agreement details of user {updateFrom.Id}/{updateFrom.Username}");
                        return Json(false);
                    }
                }
                
                // sending anything else we should just tell them that I don't understand
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, "I'm sorry but I don't understand that right now. You can say /help for what you can do right now " +
                                                                 "or you can say /start to get started.");
                return Json(true);
            }
            
            // check and see if they did not agree and their account wasn't deleted for some reason
            if (!fromUser.Agreed.Value)
            {
                // this realistically should never happen but I don't want to chance it
                // fallback response
                Log.Information($"The user {updateFrom.Id}/{updateFrom.Username} was blocked with agreement status {fromUser.Agreed.Value}");
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, "I'm sorry but you did not agree to my Terms of Service and my Privacy Policy. " +
                                                                 "There is nothing more that I can do for you. If you believe you are getting this message in error, please report it " +
                                                                 "in my GitHub at https://github.com/umdoobby/plzopenme. Sorry I can't be more help.");
                return Json(true);
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
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"I'm sorry but your account was banned from PlzOpen.Me on {fromUser.BannedOn.Value.ToString("F")}.");
                    return Json(true);
                }
                
                // respond with banned message
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"I'm sorry but your account was banned from PlzOpen.Me.");
                return Json(true);
            }
            
            // at this point we know that we have a valid, not banned user lets get into the commands that they can run

            // if there are no entities then there better be a file attached or i won't know what to do
            if (updateMessage.Entities == null)
            {
                // set up a few variables for grabbing a file if we find it
                bool foundFile = false;
                string fileId = "";
                string fuid = "";
                string fileName = "";
                string fileMimeType = "";
                int fileSize = 0;
                string filetype = "";
                
                // see if there is an animation in this message
                if (updateMessage.Animation != null)
                {
                    foundFile = true;
                    fileId = updateMessage.Animation.FileId;
                    fuid = updateMessage.Animation.FileUniqueId;
                    fileName = updateMessage.Animation.FileName;
                    fileMimeType = updateMessage.Animation.MimeType;
                    fileSize = updateMessage.Animation.FileSize;
                    filetype = "Animation";
                }
                
                // see if there is an audio file in this message
                if (!foundFile && updateMessage.Audio != null)
                {
                    foundFile = true;
                    fileId = updateMessage.Audio.FileId;
                    fuid = updateMessage.Audio.FileUniqueId;
                    fileName = updateMessage.Audio.Title;
                    fileMimeType = updateMessage.Audio.MimeType;
                    fileSize = updateMessage.Audio.FileSize;
                    filetype = "Audio";
                }
                
                // see if there is a generic file in this message
                if (!foundFile && updateMessage.Document != null)
                {
                    foundFile = true;
                    fileId = updateMessage.Document.FileId;
                    fuid = updateMessage.Document.FileUniqueId;
                    fileName = updateMessage.Document.FileName;
                    fileMimeType = updateMessage.Document.MimeType;
                    fileSize = updateMessage.Document.FileSize;
                    filetype = "Document";
                }
                
                // see if there is a photo in this message
                if (!foundFile && updateMessage.Photo != null)
                {
                    Log.Information("Sent a file but it had photos in it.");
                }
                
                // see if there is a sticker in this message
                if (!foundFile && updateMessage.Sticker != null)
                {
                    foundFile = true;
                    fileId = updateMessage.Sticker.FileId;
                    fuid = updateMessage.Sticker.FileUniqueId;
                    fileName = updateMessage.Sticker.SetName + " | " + updateMessage.Sticker.Emoji;
                    fileMimeType = "sticker";
                    fileSize = updateMessage.Sticker.FileSize;
                    filetype = "Sticker";
                }
                
                // see if there is a video in this message
                if (!foundFile && updateMessage.Video != null)
                {
                    foundFile = true;
                    fileId = updateMessage.Video.FileId;
                    fuid = updateMessage.Video.FileUniqueId;
                    fileName = "video";
                    fileMimeType = updateMessage.Video.MimeType;
                    fileSize = updateMessage.Video.FileSize;
                    filetype = "Video";
                }
                
                // see if there is a video note in this message
                if (!foundFile && updateMessage.VideoNote != null)
                {
                    foundFile = true;
                    fileId = updateMessage.VideoNote.FileId;
                    fuid = updateMessage.VideoNote.FileUniqueId;
                    fileName = "videoNote";
                    fileMimeType = "videoNote";
                    fileSize = updateMessage.VideoNote.FileSize;
                    filetype = "VideoNote";
                }
                
                // see if there is a voice message in the message
                if (!foundFile && updateMessage.Voice != null)
                {
                    foundFile = true;
                    fileId = updateMessage.Voice.FileId;
                    fuid = updateMessage.Voice.FileUniqueId;
                    fileName = "voice";
                    fileMimeType = updateMessage.Voice.MimeType;
                    fileSize = updateMessage.Voice.FileSize;
                    filetype = "Voice";
                }

                // if we have a file we can work with, here is where we do it
                if (foundFile)
                {
                    // see if the file is too big

                    string filePath = _bot.GetFileAsync(fileId).Result.FilePath;
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"Filepath = {filePath}", 
                        ParseMode.Default, false, false, updateMessage.MessageId);
                    return Json(true);
                }
                
                // placeholder response
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"Sorry but there doesn't seem to be any files attached to that message.", 
                    ParseMode.Default, false, false, updateMessage.MessageId);
                return Json(true);
            }
            
            // so there is a command lets see what they want to do and do it
            // start with the help command
            if (updateMessage.Text.StartsWith("/help"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"You account is fully set up and active! To upload a file you simply " +
                                                                 $"need to forward me the message containing the file. However, I can help with a few other things. " +
                                                                 $"Here's a list of all valid commands:\n" +
                                                                 $"/help to repeat this info\n" +
                                                                 $"/info to learn about PlzOpen.Me\n" +
                                                                 $"/getLinks to return a list of all valid links tied to your account\n" +
                                                                 $"/deleteLink <PlzOpen.Me link> to delete the specified file and link\n" +
                                                                 $"/getMyInfo to return a text report of all information we have on your account\n" +
                                                                 $"/stop to delete your account and all files associated to it");
                return Json(true);
            }
            
            // info command
            if (updateMessage.Text.StartsWith("/info"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"PlzOpen.Me is an open source file sharing platform allowing you to easily " +
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
                return Json(true);
            }
            
            // start command
            if (updateMessage.Text.StartsWith("/start"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"Your account is already set up! You don't have to start again.");
                return Json(true);
            }
            
            // stop and delete
            if (updateMessage.Text.StartsWith("/stop"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"placeholder for deleting the account");
                return Json(true);
            }
            
            // credits
            if (updateMessage.Text.StartsWith("/credits"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"this is the credits");
                return Json(true);
            }
            
            // list my links
            if (updateMessage.Text.StartsWith("/getLinks"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"This is a list of your links");
                return Json(true);
            }
            
            // get account report
            if (updateMessage.Text.StartsWith("/getMyInfo"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"This is all of the info we have on you");
                return Json(true);
            }
            
            // delete a file
            if (updateMessage.Text.StartsWith("/deleteLink"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"This is all of the info we have on you");
                return Json(true);
            }
            
            // finally you must have sent a command that i don't know
            _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"I'm sorry but I don't know that command. Use the /help command for a list of all valid commands.");
            return Json(true);
        }
    }
}