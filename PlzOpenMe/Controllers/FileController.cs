using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using PlzOpenMe.Models;
using Serilog;

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
            if (String.IsNullOrWhiteSpace(id))
            {
                // a blank ID will get us nowhere, just respond with the error page
                Log.Warning($"A client attempted to load a file without an ID, responding with error 400");
                return View("NotFound", new NotFoundViewModel()
                {
                    ErrorCode = 400,
                    ErrorDetail = "This is not a valid link, please double check the URL and try again.",
                    ErrorTitle = "Bad Request"
                });
            }
            
            // so open a query for this id
            var linkQuery = from lq in _dbContext.PomLinks
                where lq.Link == id
                select lq;
            PomLink foundLink = linkQuery.FirstOrDefault();
            
            // if we can't find that link we need to 404
            if (foundLink == null)
            {
                Log.Warning($"A client attempted to load a file with the ID {id} but this link could not be found, responding 404");
                return View("NotFound", new NotFoundViewModel()
                {
                    ErrorCode = 404,
                    ErrorDetail = "The requested file couldn't be found. It may have been moved or deleted. " +
                                  "You may want to double check the link and make sure this is the correct URL.",
                    ErrorTitle = "File Not Found"
                });
            }
            
            // so we have a link, has this link been deactivated
            if (foundLink.RemovedOn.HasValue)
            {
                Log.Warning($"A client attempted to load a file with the ID {id} but this link was previously removed, responding 404");
                return View("NotFound", new NotFoundViewModel()
                {
                    ErrorCode = 404,
                    ErrorDetail = "The requested file couldn't be found. It may have been moved or deleted. " +
                                  "You may want to double check the link and make sure this is the correct URL.",
                    ErrorTitle = "File Not Found"
                });
            }
            
            // build the query for the file
            var fileQuery = from fq in _dbContext.PomFiles
                where fq.Id == foundLink.File
                select fq;
            PomFile foundFile = fileQuery.FirstOrDefault();

            // see if there is a file for this link
            if (foundFile == null)
            {
                Log.Warning($"Link {foundLink.Id} pointed to file {foundLink.File} that doesn't exist, responding 404");
                return View("NotFound", new NotFoundViewModel()
                {
                    ErrorCode = 404,
                    ErrorDetail = "The requested file couldn't be found. It may have been moved or deleted. " +
                                  "You may want to double check the link and make sure this is the correct URL.",
                    ErrorTitle = "File Not Found"
                });
            }
            
            // see if that file still exists
            if (foundFile.DeletedOn.HasValue)
            {
                Log.Warning($"A client requested file {foundFile.Id} that was previously deleted, responding 404");
                return View("NotFound", new NotFoundViewModel()
                {
                    ErrorCode = 404,
                    ErrorDetail = "The requested file couldn't be found. It may have been moved or deleted. " +
                                  "You may want to double check the link and make sure this is the correct URL.",
                    ErrorTitle = "File Not Found"
                });
            }
            
            // is there a thumbnail
            PomFile thumbFile = null;
            if (foundLink.Thumbnail.HasValue)
            {
                // lets try to find the thumbnail, if there is none thumbnail will stay null
                var thumbQuery = from tq in _dbContext.PomFiles
                    where tq.Id == foundLink.Thumbnail
                    select tq;
                thumbFile = thumbQuery.FirstOrDefault();
            }

            // if there is a thumbnail we need to grab its info too
            PomPhoto thumbPhoto = null;
            if (thumbFile != null)
            {
                // so we found a file so we should be able to find a photo entry for it
                var thumbPhotoQuery = from pq in _dbContext.PomPhotos
                    where pq.FileId == thumbFile.Id
                    select pq;
                thumbPhoto = thumbPhotoQuery.FirstOrDefault();
            }
            
            // we are about to render the page so lets increment the view counter
            foundLink.Views++;
            _dbContext.PomLinks.Update(foundLink);
            int incrementResult = _dbContext.SaveChanges();
            Log.Information($"Link {foundLink.Id} view count was incremented by one with result {incrementResult}");

            // alright now what version of the page they get depends on the file type
            switch (foundFile.Type.ToLower())
            {
                case "animation":
                    // get the animations information
                    var animeQuery = from aq in _dbContext.PomAnimations
                        where aq.FileId == foundFile.Id
                        select aq;

                    // return the animation view with all the information we need
                    return View("Animation", new AnimationViewModel()
                    {
                        Animation = animeQuery.FirstOrDefault(),
                        File = foundFile,
                        Link = foundLink,
                        ThumbFile = thumbFile,
                        ThumbPhoto = thumbPhoto
                    });
                    break;
                
                case "audio":
                    // get the audio file information
                    var audioQuery = from aq in _dbContext.PomAudios
                        where aq.FileId == foundFile.Id
                        select aq;
                    
                    // return the audio view with all in the information we need
                    return View("Audio", new AudioViewModel()
                    {
                        Audio = audioQuery.FirstOrDefault(),
                        File = foundFile,
                        Link = foundLink,
                        ThumbFile = thumbFile,
                        ThumbPhoto = thumbPhoto
                    });
                    break;
                
                case "document":
                    // there is no additional information needed for a document
                    // return what we already have
                    return View("Document", new DocumentViewModel()
                    {
                        File = foundFile,
                        Link = foundLink,
                        ThumbFile = thumbFile,
                        ThumbPhoto = thumbPhoto
                    });
                    break;
                
                case "photo":
                    // get the photo information
                    var photoQuery = from pq in _dbContext.PomPhotos
                        where pq.FileId == foundFile.Id
                        select pq;
                    
                    // return the photo with all the information
                    return View("Photo", new PhotoViewModel()
                    {
                        Photo = photoQuery.FirstOrDefault(),
                        File = foundFile,
                        Link = foundLink,
                        ThumbFile = thumbFile,
                        ThumbPhoto = thumbPhoto
                    });
                    break;
                
                case "sticker":
                    // get the sticker information
                    var stickerQuery = from sq in _dbContext.PomStickers
                        where sq.FileId == foundFile.Id
                        select sq;
                    
                    // return the sticker with all of the information we have
                    return View("Sticker", new StickerViewModel()
                    {
                        File = foundFile,
                        Sticker = stickerQuery.FirstOrDefault(),
                        Link = foundLink,
                        ThumbFile = thumbFile,
                        ThumbPhoto = thumbPhoto
                    });
                    break;
                
                case "video":
                    // get the video information
                    var videoQuery = from vq in _dbContext.PomVideos
                        where vq.FileId == foundFile.Id
                        select vq;
                    
                    // return the video and all the information we have
                    return View("Video", new VideoViewModel()
                    {
                        File = foundFile,
                        Link = foundLink,
                        Video = videoQuery.FirstOrDefault(),
                        ThumbFile = thumbFile,
                        ThumbPhoto = thumbPhoto
                    });
                    break;
                
                case "videonote":
                    // get the video note information
                    var videoNoteQuery = from vq in _dbContext.PomVideoNotes
                        where vq.FileId == foundFile.Id
                        select vq;
                    
                    // return the video note view with all the information
                    return View("VideoNote", new VideoNoteViewModel()
                    {
                        VideoNote = videoNoteQuery.FirstOrDefault(),
                        File = foundFile,
                        Link = foundLink,
                        ThumbFile = thumbFile,
                        ThumbPhoto = thumbPhoto
                    });
                    break;
                
                case "voice":
                    // get the voice recording information
                    var voiceQuery = from vq in _dbContext.PomVoices
                        where vq.FileId == foundFile.Id
                        select vq;
                    
                    // return the view and all the information we need
                    return View("Voice", new VoiceViewModel()
                    {
                        File = foundFile,
                        Link = foundLink,
                        Voice = voiceQuery.FirstOrDefault()
                    });
                    break;
                
                default:
                    // theoretically this should never happen but its here just in case
                    Log.Error($"Link {foundLink.Id} pointed to file {foundLink.File} has an unexpected file type \"{foundFile.Type}\", responding 500");
                    return View("NotFound", new NotFoundViewModel()
                    {
                        ErrorCode = 500,
                        ErrorDetail = "The file type of the requested file is invalid. There is no view for the unexpected file type.",
                        ErrorTitle = "Internal Server Error"
                    });
                    break;
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