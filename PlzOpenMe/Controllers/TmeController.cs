﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.VisualBasic;
using nClam;
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
                List<long> fileIds = new List<long>();
                List<string> fileNames = new List<string>();

                // see if there is an animation in this message
                try
                {
                    if (updateMessage.Animation != null)
                    {
                        foundFile = false;
                        while (!foundFile)
                        {
                            // try to find the animation
                            var animationQuery = from an in _dbContext.PomFiles
                                where an.FileUniqueId == updateMessage.Animation.FileUniqueId
                                select an;

                            if (animationQuery.Any())
                            {
                                // we found the file so we need to grab the id
                                fileIds.Add(animationQuery.First().Id);
                                foundFile = true;
                                
                                // save the name if there is one
                                if (String.IsNullOrWhiteSpace(updateMessage.Animation.FileName))
                                {
                                    fileNames.Add("Untitled Animation");
                                }
                                else
                                {
                                    fileNames.Add(updateMessage.Animation.FileName);
                                }
                            }
                            else
                            {
                                // we need to add the file
                                PomFile newFile = new PomFile()
                                {
                                    FileId = updateMessage.Animation.FileId,
                                    FileUniqueId = updateMessage.Animation.FileUniqueId,
                                    Location = FindUniqueFilePath(),
                                    Mime = updateMessage.Animation.MimeType,
                                    Size = updateMessage.Animation.FileSize,
                                    Type = "Animation",
                                    UploadedOn = DateTime.Now
                                };
                                
                                // save the file
                                if (Downloadfile(newFile.Location, newFile.FileId))
                                {
                                    // we saved the file
                                    _dbContext.Add(newFile);
                                    int dbSave = _dbContext.SaveChanges();
                                    Log.Information($"User {updateFrom.Id} uploaded animation {updateMessage.Animation.FileUniqueId} as {newFile.Location}");
                                }
                                else
                                {
                                    // we failed to save the file
                                    Log.Information($"User {updateFrom.Id} failed to upload animation {updateMessage.Animation.FileUniqueId} as {newFile.Location}");
                                    _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"Sorry but there was an error while attempting to save this file. " +
                                        $"Likely, either the file was too large for Telegram to let me download it or the file failed my virus scan.", 
                                        ParseMode.Default, false, false, updateMessage.MessageId);
                                    return Json(false);
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    // there was an error while trying to save the file
                    Log.Error(ex, $"Fatal error occured while saving {updateMessage.Animation.FileId} for user {updateFrom.Id}");
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"Sorry but there was a fatal error while trying to save that file. " +
                                                                     $"Please report this issue at https://github.com/umdoobby/plzopenme and try again later!", 
                        ParseMode.Default, false, false, updateMessage.MessageId);
                    return Json(false);
                }
                
                

                // if we have a file we can work with, here is where we do it
                if (foundFile)
                {
                    // see if the file is too big

                    string filePath = _bot.GetFileAsync(fileId).Result.FilePath;
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id, $"Filepath = {filePath} | size = {fileSize}", 
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

        /// <summary>
        /// Get a filename that is unique to this file
        /// </summary>
        /// <returns>Unique string</returns>
        private string FindUniqueFilePath()
        {
            bool isUnique = false;
            string newFile = "";
            int maxAttempts = 999;
            int attempt = 0;

            while (!isUnique && attempt <= maxAttempts)
            {
                // get a new file name
                newFile = KeyGenerator.GetUniqueKey(_configuration.GetValue<int>("FileNameLength"));

                // try to find it
                var nameQuery = from fn in _dbContext.PomFiles
                    where fn.Location == newFile
                    select fn;
                
                // if we found one we need to try again else continue
                if (nameQuery.Any())
                {
                    attempt++;
                }
                else
                {
                    isUnique = true;
                }
            }

            if (isUnique)
            {
                return newFile;
            } 
            
            var ex = new DataException("Max number of attempts to generate a unique string reached");
            Log.Error(ex, "Failed to generate a new filename failed to save the file");
            throw ex;
            return "";
        }

        /// <summary>
        /// Download a file from telegram and scan it for viruses
        /// </summary>
        /// <param name="name">name for the new file</param>
        /// <param name="fileId">file id of the file to download</param>
        /// <returns>True for a successful download or false for a filed download</returns>
        private bool Downloadfile(string name, string fileId)
        {
            bool rtn = false;
            string[] clamAvConnection = _configuration.GetConnectionString("ClamAvServer").Split(':');
            
            // first get the path from telegram
            string filePath = _bot.GetFileAsync(fileId).Result.FilePath;
            
            // build the download link
            string download =
                $"https://api.telegram.org/file/{_configuration.GetValue<string>("TelegramApiKey")}/{filePath}";

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