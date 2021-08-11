﻿using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
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
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "Hi stranger! I can help you share media and files with people outside of Telegram.\nTo get started say /start or you can say /help for what you can do right now.");
                    return Json(true);
                }

                // if they are asking for help we need to tell them what they can do
                if (updateMessage.Text.StartsWith("/help"))
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "Say /start to get me ready to start receiving your files.\nSay /info to learn more about me.\nFinally, say /help to have this repeated.");
                    return Json(true);
                }

                // if they want info, lets tell them about us
                if (updateMessage.Text.StartsWith("/info"))
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "Let me introduce myself, I am PlzOpenMe, a file and media sharing platform integrated with Telegram. I will take any files you send me, repack them for anyone to see, " +
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
                        Log.Information(
                            $"The user {updateFrom.Id}/{updateFrom.Username} has started the bot successfully with result code {makeUserResult}");
                    }
                    catch (Exception e)
                    {
                        // we failed to set up the user for some reason
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                            "This is embarrassing... I encountered an error trying to handle your request. I have alerted by sysadmin about the failure, please try again in a few minutes.");
                        Log.Error(e,
                            $"There was an error while trying to create the user for {updateFrom.Id}/{updateFrom.Username}");
                        return Json(false);
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
                    return Json(true);
                }

                // sending anything else we should just tell them that I don't understand
                _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                    "I'm sorry but I don't understand that right now. You can say /help for what you can do right now " +
                    "or you can say /start to get started.");
                return Json(true);


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
                                "thanks again for giving me a shot!");
                            return Json(true);
                        }
                        catch (Exception e)
                        {
                            // for some reason we failed update the user
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                "This is embarrassing... I encountered an error trying to handle your request. I have alerted my admin about the failure, please try again in a few minutes.");
                            Log.Error(e,
                                $"There was an error while attempting to save the agreement details of user {updateFrom.Id}/{updateFrom.Username}");
                            return Json(false);
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
                            return Json(true);
                        }
                        catch (Exception e)
                        {
                            // for some reason we failed to remove the user
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                "This is embarrassing... I encountered an error trying to handle your request. I have alerted my admin about the failure, please try again in a few minutes.");
                            Log.Error(e,
                                $"There was an error while attempting to remove user {updateFrom.Id}/{updateFrom.Username}");
                            return Json(false);
                        }
                    }
                    
                    // anything else at this stage is not acceptable
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "I'm sorry but I can only continue to help if you agree to my terms of service and " +
                        "my privacy policy. I'm not a lawyer so they aren't long or complicated, I promise! You can say " +
                        "/start to have the start message repeated or /help to see all of the commands you can use right now.");
                    return Json(true);
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
                    return Json(true);
                }

                // are they asking for info
                if (updateMessage.Text.StartsWith("/info"))
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "Let me introduce myself, I am PlzOpenMe, a file and media sharing platform integrated with Telegram. I will take any files you send me, repack them for anyone to see, " +
                        "and give you a link that you can use to share with your friends outside of Telegram. For more information please go to https://plzopen.me.");
                    return Json(true);
                }

                // if they are asking for help we need to tell them what they can do
                if (updateMessage.Text.StartsWith("/help"))
                {
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        "Say /start to have me repeat the starting instructions again.\n" +
                        "Unfortunately, I cannot help you anymore until you fallow my instructions in my " +
                        "/start welcome message. Other then that, say /info to learn more about me or say " +
                        "/help to have this repeated.");
                    return Json(true);
                }

                // sending anything else we should just tell them that I don't understand
                _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                    "I'm sorry but I don't understand that right now. You can say /help for what you can do right now " +
                    "or you can say /start to get started.");
                return Json(true);
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
                    _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                        $"I'm sorry but your account was banned from PlzOpen.Me on {fromUser.BannedOn.Value.ToString("F")}.");
                }

                // respond with banned message
                _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                    $"I'm sorry but your account was banned from PlzOpen.Me.");

                // attempt to leave the chat
                _bot.LeaveChatAsync(updateMessage.Chat.Id);
                return Json(true);
            }

            // at this point we know that we have a valid, not banned user lets get into the commands that they can run

            // if there are no entities then there better be a file attached or i won't know what to do
            if (updateMessage.Entities == null)
            {
                // set up a few variables for grabbing a file if we find it
                UploadedFile newFile = new UploadedFile();
                UploadedFile newThumb = new UploadedFile();
                bool foundFile = false;
                bool foundThumb = false;

                // see if there is an animation in this message
                if (updateMessage.Animation != null)
                {
                    try
                    {
                        Log.Debug($"User {updateFrom.Id} submitted an animation ID={updateMessage.Animation.FileId} FuID={updateMessage.Animation.FileUniqueId}");
                        
                        // attempt to save the file
                        UploadedFile temp = SaveOrFindFile(updateMessage.Animation.FileId, updateMessage.Animation.FileUniqueId,
                            updateMessage.Animation.FileSize, updateMessage.Animation.MimeType, "Animation",
                            updateMessage.Animation.FileName, updateFrom.Id);

                        // see if we actually saved the file
                        if (temp == null)
                        {
                            Log.Warning($"Failed to save an animation for user {updateFrom.Id} file ID={updateMessage.Animation.FileId} FuID={updateMessage.Animation.FileUniqueId}");
                            // we failed to upload the file
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                $"Sorry but there was an error while attempting to save this file. " +
                                $"Likely, either the file was too large for Telegram to let me download it or the file failed my virus scan.",
                                ParseMode.Default, null, false, false, updateMessage.MessageId);
                            return Json(false);
                        }
                        
                        // attempt to add the animations details
                        _dbContext.PomAnimations.Add(new PomAnimation()
                        {
                            Duration = updateMessage.Animation.Duration,
                            Height = updateMessage.Animation.Height,
                            Width = updateMessage.Animation.Width,
                            FileId = temp.Id
                        });
                        int detailSave = _dbContext.SaveChanges();
                        Log.Information($"Details for animation {temp.Id} saved with result {detailSave}");
                        
                        // we succeeded so add the file to the array
                        newFile = temp;
                        foundFile = true;
                        
                        // is there a thumbnail
                        if (updateMessage.Animation.Thumb != null)
                        {
                            // we have a thumbnail, lets try to save that too
                            temp = SaveOrFindFile(updateMessage.Animation.Thumb.FileId, updateMessage.Animation.Thumb.FileUniqueId,
                                updateMessage.Animation.Thumb.FileSize, "image/jpg", "Photo",
                                updateMessage.Animation.FileName + "-thumb", updateFrom.Id);
                            
                            // see if we actually saved the file
                            if (temp != null)
                            {
                                newThumb = temp;
                                foundThumb = true;
                                
                                // attempt to add the animation thumbnails details
                                _dbContext.PomPhotos.Add(new PomPhoto()
                                {
                                    FileId = temp.Id,
                                    Height = updateMessage.Animation.Thumb.Height,
                                    Width = updateMessage.Animation.Thumb.Width,
                                    IsThumbnail = true
                                });
                                detailSave = _dbContext.SaveChanges();
                                Log.Information($"Details for animation thumbnail {temp.Id} saved with result {detailSave}");
                            }
                            
                            // if we failed to save the thumbnail, just continue, im just not that worried about it
                        }
                    }
                    catch (Exception ex)
                    {
                        // there was an error while trying to save the file
                        Log.Error(ex,
                            $"Fatal error occured while saving {updateMessage.Animation.FileId} for user {updateFrom.Id}");
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                            $"Sorry but there was a fatal error while trying to save that file. " +
                            $"Please report this issue at https://github.com/umdoobby/plzopenme and try again later!",
                            ParseMode.Default, null, false, false, updateMessage.MessageId);
                        return Json(false);
                    }
                }

                // see if there is audio in this message
                if (!foundFile && updateMessage.Audio != null)
                {
                    try
                    {
                        Log.Debug($"User {updateFrom.Id} submitted an audio file ID={updateMessage.Audio.FileId} FuID={updateMessage.Audio.FileUniqueId}");

                        // attempt to save the file
                        UploadedFile temp = SaveOrFindFile(updateMessage.Audio.FileId, updateMessage.Audio.FileUniqueId,
                            updateMessage.Audio.FileSize, updateMessage.Audio.MimeType, "Audio",
                            updateMessage.Audio.Title, updateFrom.Id);

                        // see if we actually saved the file
                        if (temp == null)
                        {
                            Log.Warning($"Failed to save an audio file for user {updateFrom.Id} file ID={updateMessage.Audio.FileId} FuID={updateMessage.Audio.FileUniqueId}");
                            
                            // we failed to upload the file
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                $"Sorry but there was an error while attempting to save this file. " +
                                $"Likely, either the file was too large for Telegram to let me download it or the file failed my virus scan.",
                                ParseMode.Default, null, false, false, updateMessage.MessageId);
                            return Json(false);
                        }

                        // attempt to save the audio files details
                        _dbContext.PomAudios.Add(new PomAudio()
                        {
                            Duration = updateMessage.Audio.Duration,
                            FileId = temp.Id,
                            Performer = updateMessage.Audio.Performer,
                            Title = updateMessage.Audio.Title
                        });
                        int detailSave = _dbContext.SaveChanges();
                        Log.Information($"Details for audio file {temp.Id} saved with result {detailSave}");
                        
                        // we succeeded so grab the file
                        newFile = temp;
                        foundFile = true;
                        
                        // is there a thumbnail
                        if (updateMessage.Audio.Thumb != null)
                        {
                            // we have a thumbnail, lets try to save that too
                            temp = SaveOrFindFile(updateMessage.Audio.Thumb.FileId, updateMessage.Audio.Thumb.FileUniqueId,
                                updateMessage.Audio.Thumb.FileSize, "image/jpg", "Thumbnail",
                                updateMessage.Audio.Title + "-thumb", updateFrom.Id);
                            
                            // see if we actually saved the file
                            if (temp != null)
                            {
                                newThumb = temp;
                                foundThumb = true;
                                
                                // attempt to add the audio thumbnails details
                                _dbContext.PomPhotos.Add(new PomPhoto()
                                {
                                    FileId = temp.Id,
                                    Height = updateMessage.Audio.Thumb.Height,
                                    Width = updateMessage.Audio.Thumb.Width,
                                    IsThumbnail = true
                                });
                                detailSave = _dbContext.SaveChanges();
                                Log.Information($"Details for audio thumbnail {temp.Id} saved with result {detailSave}");
                            }
                            
                            // if we failed to save the thumbnail, just continue, im just not that worried about it
                        }
                    }
                    catch (Exception ex)
                    {
                        // there was an error while trying to save the file
                        Log.Error(ex,
                            $"Fatal error occured while saving {updateMessage.Audio.FileId} for user {updateFrom.Id}");
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                            $"Sorry but there was a fatal error while trying to save that file. " +
                            $"Please report this issue at https://github.com/umdoobby/plzopenme and try again later!",
                            ParseMode.Default, null, false, false, updateMessage.MessageId);
                        return Json(false);
                    }
                }
                
                // see if there is a document in this message
                if (!foundFile && updateMessage.Document != null)
                {
                    try
                    {
                        Log.Debug($"User {updateFrom.Id} submitted a document file ID={updateMessage.Document.FileId} FuID={updateMessage.Document.FileUniqueId}");
                        
                        // attempt to save the file
                        UploadedFile temp = SaveOrFindFile(updateMessage.Document.FileId, updateMessage.Document.FileUniqueId,
                            updateMessage.Document.FileSize, updateMessage.Document.MimeType, "Document",
                            updateMessage.Document.FileName, updateFrom.Id);

                        // see if we actually saved the file
                        if (temp == null)
                        {
                            Log.Warning($"Failed to save a document file for user {updateFrom.Id} file ID={updateMessage.Document.FileId} FuID={updateMessage.Document.FileUniqueId}");

                            // we failed to upload the file
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                $"Sorry but there was an error while attempting to save this file. " +
                                $"Likely, either the file was too large for Telegram to let me download it or the file failed my virus scan.",
                                ParseMode.Default, null, false, false, updateMessage.MessageId);
                            return Json(false);
                        }
                        
                        // there are no details for a generic document file
                        
                        // we succeeded so grab the file
                        newFile = temp;
                        foundFile = true;
                        
                        // is there a thumbnail
                        if (updateMessage.Document.Thumb != null)
                        {
                            // we have a thumbnail, lets try to save that too
                            temp = SaveOrFindFile(updateMessage.Document.Thumb.FileId, updateMessage.Document.Thumb.FileUniqueId,
                                updateMessage.Document.Thumb.FileSize, "image/jpg", "Thumbnail",
                                updateMessage.Document.FileName + "-thumb", updateFrom.Id);
                            
                            // see if we actually saved the file
                            if (temp != null)
                            {
                                newThumb = temp;
                                foundThumb = true;
                                
                                // attempt to add the document thumbnails details
                                _dbContext.PomPhotos.Add(new PomPhoto()
                                {
                                    FileId = temp.Id,
                                    Height = updateMessage.Document.Thumb.Height,
                                    Width = updateMessage.Document.Thumb.Width,
                                    IsThumbnail = true
                                });
                                int detailSave = _dbContext.SaveChanges();
                                Log.Information($"Details for document thumbnail {temp.Id} saved with result {detailSave}");
                            }
                            
                            // if we failed to save the thumbnail, just continue, im just not that worried about it
                        }
                    }
                    catch (Exception ex)
                    {
                        // there was an error while trying to save the file
                        Log.Error(ex,
                            $"Fatal error occured while saving {updateMessage.Document.FileId} for user {updateFrom.Id}");
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                            $"Sorry but there was a fatal error while trying to save that file. " +
                            $"Please report this issue at https://github.com/umdoobby/plzopenme and try again later!",
                            ParseMode.Default, null, false, false, updateMessage.MessageId);
                        return Json(false);
                    }
                }
                
                // see if there are any photos in this message
                if (!foundFile && updateMessage.Photo != null)
                {
                    Log.Debug($"UserID={updateFrom.Id} ChatID={updateMessage.MessageId} submitted a photo with {updateMessage.Photo.Length} sizes");
                    
                    PhotoSize original = new PhotoSize();
                    PhotoSize thumb = new PhotoSize();

                    int maxSize = updateMessage.Photo.Max(size => size.FileSize);
                    int minSize = updateMessage.Photo.Min(size => size.FileSize);
                    
                    // find values
                    foreach (PhotoSize size in updateMessage.Photo)
                    {
                        if (size.FileSize == maxSize)
                        {
                            original = size;
                            Log.Debug($"Original size found for user {updateFrom.Id}/{updateMessage.MessageId} as ID={original.FileId} FuID={original.FileUniqueId}");
                        }

                        if (size.FileSize == minSize)
                        {
                            thumb = size;
                            Log.Debug($"Thumbnail size found for user {updateFrom.Id}/{updateMessage.MessageId} as ID={thumb.FileId} FuID={thumb.FileUniqueId}");
                        }
                    }
                    
                    try
                    {
                        Log.Debug($"User {updateFrom.Id} submitted a photo ID={original.FileId} FuID={original.FileUniqueId}");

                        // attempt to save the file
                        UploadedFile temp = SaveOrFindFile(original.FileId, original.FileUniqueId,
                            original.FileSize, "image/jpg", "Photo",
                            DateTime.Now.ToString("O"), updateFrom.Id);

                        // see if we actually saved the file
                        if (temp == null)
                        {
                            Log.Warning($"Failed to save a photo for user {updateFrom.Id} file ID={original.FileId} FuID={original.FileUniqueId}");

                            // we failed to upload the file
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                $"Sorry but there was an error while attempting to save this file. " +
                                $"Likely, either the file was too large for Telegram to let me download it or the file failed my virus scan.",
                                ParseMode.Default, null, false, false, updateMessage.MessageId);
                            return Json(false);
                        }
                        
                        // attempt to save the photo files details
                        _dbContext.PomPhotos.Add(new PomPhoto()
                        {
                            Height = original.Height,
                            Width = original.Width,
                            FileId = temp.Id,
                            IsThumbnail = false
                        });
                        int detailSave = _dbContext.SaveChanges();
                        Log.Information($"Details for photo {temp.Id} saved with result {detailSave}");
                        
                        // we succeeded so grab the file
                        newFile = temp;
                        foundFile = true;
                        
                        // is there a thumbnail
                        if (original.FileUniqueId != thumb.FileUniqueId)
                        {
                            // we have a thumbnail, lets try to save that too
                            temp = SaveOrFindFile(thumb.FileId, thumb.FileUniqueId,
                                thumb.FileSize, "image/jpg", "Thumbnail",
                                DateTime.Now.ToString("O") + "-thumb", updateFrom.Id);
                            
                            // see if we actually saved the file
                            if (temp != null)
                            {
                                newThumb = temp;
                                foundThumb = true;
                                
                                // attempt to add the photo thumbnails details
                                _dbContext.PomPhotos.Add(new PomPhoto()
                                {
                                    FileId = temp.Id,
                                    Height = thumb.Height,
                                    Width = thumb.Width,
                                    IsThumbnail = true
                                });
                                detailSave = _dbContext.SaveChanges();
                                Log.Information($"Details for photo thumbnail {temp.Id} saved with result {detailSave}");
                            }
                            
                            // if we failed to save the thumbnail, just continue, im just not that worried about it
                        }
                    }
                    catch (Exception ex)
                    {
                        // there was an error while trying to save the file
                        Log.Error(ex,
                            $"Fatal error occured while saving {original.FileId} for user {updateFrom.Id}");
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                            $"Sorry but there was a fatal error while trying to save that file. " +
                            $"Please report this issue at https://github.com/umdoobby/plzopenme and try again later!",
                            ParseMode.Default, null, false, false, updateMessage.MessageId);
                        return Json(false);
                    }
                }
                
                // see if there is a sticker in this message
                if (!foundFile && updateMessage.Sticker != null)
                {
                    try
                    {
                        Log.Debug($"User {updateFrom.Id} submitted a sticker ID={updateMessage.Sticker.FileId} FuID={updateMessage.Sticker.FileUniqueId}");
                        
                        // attempt to save the file
                        UploadedFile temp = SaveOrFindFile(updateMessage.Sticker.FileId, updateMessage.Sticker.FileUniqueId,
                            updateMessage.Sticker.FileSize, "", "Sticker",
                            updateMessage.Sticker.SetName + " | " + updateMessage.Sticker.Emoji, updateFrom.Id);

                        // see if we actually saved the file
                        if (temp == null)
                        {
                            Log.Warning($"Failed to save a sticker for user {updateFrom.Id} file ID={updateMessage.Sticker.FileId} FuID={updateMessage.Sticker.FileUniqueId}");

                            // we failed to upload the file
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                $"Sorry but there was an error while attempting to save this file. " +
                                $"Likely, either the file was too large for Telegram to let me download it or the file failed my virus scan.",
                                ParseMode.Default, null, false, false, updateMessage.MessageId);
                            return Json(false);
                        }
                        
                        // attempt to save the sticker files details
                        _dbContext.PomStickers.Add(new PomSticker()
                        {
                            Height = updateMessage.Sticker.Height,
                            Width = updateMessage.Sticker.Width,
                            Emoji = updateMessage.Sticker.Emoji,
                            FileId = temp.Id,
                            SetName = updateMessage.Sticker.SetName,
                            IsAnimated = updateMessage.Sticker.IsAnimated,
                            MaskPoint = updateMessage.Sticker.MaskPosition.Point.ToString(),
                            MaskScale = updateMessage.Sticker.MaskPosition.Scale,
                            MaskShiftX = updateMessage.Sticker.MaskPosition.XShift,
                            MaskShiftY = updateMessage.Sticker.MaskPosition.YShift
                        });
                        int detailSave = _dbContext.SaveChanges();
                        Log.Information($"Details for sticker {temp.Id} saved with result {detailSave}");
                        
                        // we succeeded so grab the file
                        newFile = temp;
                        foundFile = true;

                        // is there a thumbnail
                        if (updateMessage.Sticker.Thumb != null)
                        {
                            // we have a thumbnail, lets try to save that too
                            // these can be either a jpg or a webp, for right now we are just going to assume jpeg
                            temp = SaveOrFindFile(updateMessage.Sticker.Thumb.FileId, updateMessage.Sticker.Thumb.FileUniqueId,
                                updateMessage.Sticker.Thumb.FileSize, "", "Thumbnail",
                                updateMessage.Sticker.SetName + "|" + updateMessage.Sticker.Emoji + "-thumb", updateFrom.Id);
                            
                            // see if we actually saved the file
                            if (temp != null)
                            {
                                newThumb = temp;
                                foundThumb = true;
                                
                                // attempt to add the audio thumbnails details
                                _dbContext.PomPhotos.Add(new PomPhoto()
                                {
                                    FileId = temp.Id,
                                    Height = updateMessage.Sticker.Thumb.Height,
                                    Width = updateMessage.Sticker.Thumb.Width,
                                    IsThumbnail = true
                                });
                                detailSave = _dbContext.SaveChanges();
                                Log.Information($"Details for sticker thumbnail {temp.Id} saved with result {detailSave}");
                            }
                            
                            // if we failed to save the thumbnail, just continue, im just not that worried about it
                        }
                    }
                    catch (Exception ex)
                    {
                        // there was an error while trying to save the file
                        Log.Error(ex,
                            $"Fatal error occured while saving {updateMessage.Sticker.FileId} for user {updateFrom.Id}");
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                            $"Sorry but there was a fatal error while trying to save that file. " +
                            $"Please report this issue at https://github.com/umdoobby/plzopenme and try again later!",
                            ParseMode.Default, null, false, false, updateMessage.MessageId);
                        return Json(false);
                    }
                }
                
                // see if there is a video in this message
                if (!foundFile && updateMessage.Video != null)
                {
                    try
                    {
                        Log.Debug($"User {updateFrom.Id} submitted a video ID={updateMessage.Video.FileId} FuID={updateMessage.Video.FileUniqueId}");
                        
                        // attempt to save the file
                        UploadedFile temp = SaveOrFindFile(updateMessage.Video.FileId, updateMessage.Video.FileUniqueId,
                            updateMessage.Video.FileSize, updateMessage.Video.MimeType, "Video",
                            updateMessage.Video.FileName, updateFrom.Id);

                        // see if we actually saved the file
                        if (temp == null)
                        {
                            Log.Warning($"Failed to save a video for user {updateFrom.Id} file ID={updateMessage.Video.FileId} FuID={updateMessage.Video.FileUniqueId}");

                            // we failed to upload the file
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                $"Sorry but there was an error while attempting to save this file. " +
                                $"Likely, either the file was too large for Telegram to let me download it or the file failed my virus scan.",
                                ParseMode.Default, null, false, false, updateMessage.MessageId);
                            return Json(false);
                        }
                        
                        // attempt to save the video files details
                        _dbContext.PomVideos.Add(new PomVideo()
                        {
                            Height = updateMessage.Video.Height,
                            Width = updateMessage.Video.Width,
                            Duration = updateMessage.Video.Duration,
                            FileId = temp.Id
                        });
                        int detailSave = _dbContext.SaveChanges();
                        Log.Information($"Details for video {temp.Id} saved with result {detailSave}");
                        
                        // we succeeded so grab the file
                        newFile = temp;
                        foundFile = true;
                        
                        // is there a thumbnail
                        if (updateMessage.Video.Thumb != null)
                        {
                            // we have a thumbnail, lets try to save that too
                            temp = SaveOrFindFile(updateMessage.Video.Thumb.FileId, updateMessage.Video.Thumb.FileUniqueId,
                                updateMessage.Video.Thumb.FileSize, "image/jpg", "Thumbnail",
                                DateTime.Now.ToString("O") + "-thumb", updateFrom.Id);
                            
                            // see if we actually saved the file
                            if (temp != null)
                            {
                                newThumb = temp;
                                foundThumb = true;
                                
                                // attempt to add the video thumbnails details
                                _dbContext.PomPhotos.Add(new PomPhoto()
                                {
                                    FileId = temp.Id,
                                    Height = updateMessage.Video.Thumb.Height,
                                    Width = updateMessage.Video.Thumb.Width,
                                    IsThumbnail = true
                                });
                                detailSave = _dbContext.SaveChanges();
                                Log.Information($"Details for video thumbnail {temp.Id} saved with result {detailSave}");
                            }
                            
                            // if we failed to save the thumbnail, just continue, im just not that worried about it
                        }
                    }
                    catch (Exception ex)
                    {
                        // there was an error while trying to save the file
                        Log.Error(ex,
                            $"Fatal error occured while saving {updateMessage.Video.FileId} for user {updateFrom.Id}");
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                            $"Sorry but there was a fatal error while trying to save that file. " +
                            $"Please report this issue at https://github.com/umdoobby/plzopenme and try again later!",
                            ParseMode.Default, null, false, false, updateMessage.MessageId);
                        return Json(false);
                    }
                }
                
                // see if there is a video note in this message
                if (!foundFile && updateMessage.VideoNote != null)
                {
                    try
                    {
                        Log.Debug($"User {updateFrom.Id} submitted a video note ID={updateMessage.VideoNote.FileId} FuID={updateMessage.VideoNote.FileUniqueId}");
                        
                        // attempt to save the file
                        UploadedFile temp = SaveOrFindFile(updateMessage.VideoNote.FileId, updateMessage.VideoNote.FileUniqueId,
                            updateMessage.VideoNote.FileSize, "video/mp4", "VideoNote",
                            DateTime.Now.ToString("O"), updateFrom.Id);

                        // see if we actually saved the file
                        if (temp == null)
                        {
                            Log.Warning($"Failed to save a video note for user {updateFrom.Id} file ID={updateMessage.VideoNote.FileId} FuID={updateMessage.VideoNote.FileUniqueId}");

                            // we failed to upload the file
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                $"Sorry but there was an error while attempting to save this file. " +
                                $"Likely, either the file was too large for Telegram to let me download it or the file failed my virus scan.",
                                ParseMode.Default, null, false, false, updateMessage.MessageId);
                            return Json(false);
                        }
                        
                        // attempt to save the video notes details
                        _dbContext.PomVideoNotes.Add(new PomVideoNote()
                        {
                            Length = updateMessage.VideoNote.Length,
                            Duration = updateMessage.VideoNote.Duration,
                            FileId = temp.Id
                        });
                        int detailSave = _dbContext.SaveChanges();
                        Log.Information($"Details for video note {temp.Id} saved with result {detailSave}");
                        
                        // we succeeded so grab the file
                        newFile = temp;
                        foundFile = true;
                        
                        // is there a thumbnail
                        if (updateMessage.VideoNote.Thumb != null)
                        {
                            // we have a thumbnail, lets try to save that too
                            temp = SaveOrFindFile(updateMessage.VideoNote.Thumb.FileId, updateMessage.VideoNote.Thumb.FileUniqueId,
                                updateMessage.VideoNote.Thumb.FileSize, "image/jpg", "Thumbnail",
                                DateTime.Now.ToString("O") + "-thumb", updateFrom.Id);
                            
                            // see if we actually saved the file
                            if (temp != null)
                            {
                                newThumb = temp;
                                foundThumb = true;
                                
                                // attempt to add the video notes thumbnails details
                                _dbContext.PomPhotos.Add(new PomPhoto()
                                {
                                    FileId = temp.Id,
                                    Height = updateMessage.VideoNote.Thumb.Height,
                                    Width = updateMessage.VideoNote.Thumb.Width,
                                    IsThumbnail = true
                                });
                                detailSave = _dbContext.SaveChanges();
                                Log.Information($"Details for video note thumbnail {temp.Id} saved with result {detailSave}");
                            }
                            
                            // if we failed to save the thumbnail, just continue, im just not that worried about it
                        }
                    }
                    catch (Exception ex)
                    {
                        // there was an error while trying to save the file
                        Log.Error(ex,
                            $"Fatal error occured while saving {updateMessage.VideoNote.FileId} for user {updateFrom.Id}");
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                            $"Sorry but there was a fatal error while trying to save that file. " +
                            $"Please report this issue at https://github.com/umdoobby/plzopenme and try again later!",
                            ParseMode.Default, null, false, false, updateMessage.MessageId);
                        return Json(false);
                    }
                }
                
                // see if there is a voice in this message
                if (!foundFile && updateMessage.Voice != null)
                {
                    try
                    {
                        Log.Debug($"User {updateFrom.Id} submitted a voice ID={updateMessage.Voice.FileId} FuID={updateMessage.Voice.FileUniqueId}");

                        // attempt to save the file
                        UploadedFile temp = SaveOrFindFile(updateMessage.Voice.FileId, updateMessage.Voice.FileUniqueId,
                            updateMessage.Voice.FileSize, updateMessage.Voice.MimeType, "Voice",
                            DateTime.Now.ToString("O"), updateFrom.Id);

                        // see if we actually saved the file
                        if (temp == null)
                        {
                            Log.Warning($"Failed to save a voice for user {updateFrom.Id} file ID={updateMessage.Voice.FileId} FuID={updateMessage.Voice.FileUniqueId}");

                            // we failed to upload the file
                            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                                $"Sorry but there was an error while attempting to save this file. " +
                                $"Likely, either the file was too large for Telegram to let me download it or the file failed my virus scan.",
                                ParseMode.Default, null, false, false, updateMessage.MessageId);
                            return Json(false);
                        }
                        
                        // attempt to save the voice details
                        _dbContext.PomVoices.Add(new PomVoice()
                        {
                            Duration = updateMessage.Voice.Duration,
                            FileId = temp.Id
                        });
                        int detailSave = _dbContext.SaveChanges();
                        Log.Information($"Details for video note {temp.Id} saved with result {detailSave}");
                        
                        // we succeeded so grab the file
                        newFile = temp;
                        foundFile = true;
                        
                        // there is never a thumbnail with voice
                        foundThumb = false;
                    }
                    catch (Exception ex)
                    {
                        // there was an error while trying to save the file
                        Log.Error(ex,
                            $"Fatal error occured while saving {updateMessage.Voice.FileId} for user {updateFrom.Id}");
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                            $"Sorry but there was a fatal error while trying to save that file. " +
                            $"Please report this issue at https://github.com/umdoobby/plzopenme and try again later!",
                            ParseMode.Default, null, false, false, updateMessage.MessageId);
                        return Json(false);
                    }
                }

                // if we have a file we can work with, here is where we do it
                if (foundFile)
                {
                    try
                    {
                        // create the new link
                        PomLink newLink =  new PomLink()
                        {
                            UserId = updateFrom.Id,
                            AddedOn = DateTime.Now,
                            File = newFile.Id,
                            Link = MakeLink(),
                            Views = 0,
                            Name = newFile.Name
                        };

                        if (foundThumb)
                        {
                            newLink.Thumbnail = newThumb.Id;
                        }

                        // save the links to the db
                        _dbContext.PomLinks.Add(newLink);
                        int linkResult = _dbContext.SaveChanges();
                        Log.Information(
                            $"New link \"{newLink.Link}\" created for {updateFrom.Id} with result {linkResult}");

                        // send the success message
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                            $"{_configuration.GetValue<string>("LiveUrl")}File?id={newLink.Link}",
                            ParseMode.Default, null, false, false, updateMessage.MessageId);
                        return Json(true);
                    }
                    catch (Exception ex)
                    {
                        // there was an error log the error
                        Log.Error(ex,
                            $"There was an error while trying to create the link for {updateFrom.Id} with the file {newFile.Id}");

                        // report the error to the user
                        _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                            $"Sorry, there was an unexpected error while trying to create the link. " +
                            $"Please consider reporting this issue here, https://github.com/umdoobby/plzopenme and try again later.",
                            ParseMode.Default, null, false, false, updateMessage.MessageId);
                        return Json(false);
                    }
                
                }

                // placeholder response
                _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                    $"Sorry but there doesn't seem to be any files attached to that message.",
                    ParseMode.Default, null, false, false, updateMessage.MessageId);
                return Json(true);
            }

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
                    $"/stop to delete your account and all files associated to it");
                return Json(true);
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
                return Json(true);
            }

            // start command
            if (updateMessage.Text.StartsWith("/start"))
            {
                _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                    $"Your account is already set up! You don't have to start again.");
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
            _bot.SendTextMessageAsync(updateMessage.Chat.Id,
                $"I'm sorry but I don't know that command. Use the /help command for a list of all valid commands.");
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

            throw new DataException("Max number of attempts to generate a unique string reached");
            return "";
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
            int maxAttempts = 999;
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


        /// <summary>
        /// Downloads the file from telegram if needed and returns the files POM id.
        /// This will attempt 5 times to download the file, if all 5 fail it returns a null
        /// </summary>
        /// <param name="fileId">string file ID from Telegram</param>
        /// <param name="fileUniqueId">string unique file ID from Telegram</param>
        /// <param name="size">int file size from Telegram</param>
        /// <param name="mimeType">string MIME type from Telegram</param>
        /// <param name="type">string type of file attached to the message</param>
        /// <param name="name">string name of the file in Telegram</param>
        /// <param name="fromId">long user ID that sent the file in Telegram</param>
        /// <returns>UploadedFile with the POM ID and the name of the file</returns>
        private UploadedFile SaveOrFindFile(string fileId, string fileUniqueId, int size, string mimeType, string type, string name, long fromId)
        {
            // set up a few variables for the loop
            UploadedFile rtn = null;
            bool foundFile = false;
            int attempts = 0;
            int maxAttempts = 5;
            
            // main attempt loop
            while (!foundFile && attempts < maxAttempts)
            {
                // try to find the animation
                var fileQuery = from uf in _dbContext.PomFiles
                    where uf.FileUniqueId == fileUniqueId
                    select uf;

                if (fileQuery.Any())
                {
                    rtn = new UploadedFile();
                    
                    // we found the file so we need to grab the id
                    rtn.Id = fileQuery.First().Id;

                    // save the name if there is one
                    if (String.IsNullOrWhiteSpace(name))
                    {
                        rtn.Name = $"Untitled {type}";
                    }
                    else
                    {
                        rtn.Name = name;
                    }
                    
                    // break the loop
                    foundFile = true;
                }
                else
                {
                    // we need to add the file
                    PomFile newFile = new PomFile()
                    {
                        FileId = fileId,
                        FileUniqueId = fileUniqueId,
                        Location = FindUniqueFilePath(),
                        Mime = mimeType,
                        Size = size,
                        Type = type,
                        UploadedOn = DateTime.Now
                    };
                    
                    // Get the path from telegram
                    string filePath = _bot.GetFileAsync(fileId).Result.FilePath;
                    
                    // get the file ext from telegram
                    string ext = Path.GetExtension(filePath);
                    newFile.Location += ext;

                    // save the file
                    if (Downloadfile(newFile.Location, filePath, newFile.Size))
                    {
                        // we saved the file
                        // if the mimetype is empty we want to try and get it from the file
                        if (String.IsNullOrWhiteSpace(newFile.Mime))
                        {
                            mimeType = MimeTypes.GetMimeType(newFile.Location);
                        }

                        _dbContext.Add(newFile);
                        int dbSave = _dbContext.SaveChanges();
                        Log.Information(
                            $"User {fromId} uploaded the {newFile.Type} file {fileUniqueId} as {newFile.Location} with result {dbSave}");
                    }
                    else
                    {
                        // we failed to save the file
                        Log.Warning(
                            $"User {fromId} failed to upload the {type} file {fileUniqueId} as {newFile.Location}");
                    }
                }
                
                // increment
                attempts++;
            }

            // return the result
            return rtn;
        }

    }
}