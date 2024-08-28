using LiteDB;
using Microsoft.AspNetCore.Mvc;
using SixLabors.ImageSharp;

namespace DdadduServer.Controllers
{
    [ApiController]
    [Route("api")]
    public class BasicController : ControllerBase
    {
        private readonly IWebHostEnvironment _environment;
        private readonly HttpClient _httpClient;

        private readonly LiteDatabase _liteDatabase;
        private readonly ILiteCollection<Publication> _publications;

        public BasicController(IWebHostEnvironment environment, LiteDatabase liteDatabase)
        {
            _environment = environment;
            _httpClient = new HttpClient();
            _liteDatabase = liteDatabase;
            _publications = _liteDatabase.GetCollection<Publication>("publications");
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromBody] List<string> fileUrls)
        {
            if (fileUrls == null || fileUrls.Count == 0)
            {
                return BadRequest("File URLs are missing.");
            }

            var fileUrlsOnServer = new List<string>();
            var fileExtension = ".jpg";
            var uploadsDir = Path.Combine(_environment.WebRootPath, "main-images");

            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            foreach (var fileUrl in fileUrls)
            {
                var fileName = Guid.NewGuid().ToString() + fileExtension;
                var filePath = Path.Combine(uploadsDir, fileName);

                try
                {
                    using (var response = await _httpClient.GetAsync(fileUrl))
                    {
                        if (!response.IsSuccessStatusCode)
                        {
                            return BadRequest($"Failed to download the file from {fileUrl}.");
                        }

                        using (var inputStream = await response.Content.ReadAsStreamAsync())
                        {
                            using (var image = await Image.LoadAsync(inputStream))
                            {
                                using (var outputStream = new FileStream(filePath, FileMode.Create))
                                {
                                    await image.SaveAsync(outputStream, new SixLabors.ImageSharp.Formats.Jpeg.JpegEncoder());
                                }
                            }
                        }
                    }

                    var fileUrlOnServer = $"{Request.Scheme}://{Request.Host}/main-images/{fileName}";
                    fileUrlsOnServer.Add(fileUrlOnServer);
                }
                catch (Exception ex)
                {
                    return StatusCode(500, $"Internal server error: {ex.Message}");
                }
            }

            return Ok(fileUrlsOnServer);
        }

        public static readonly List<string> ExclusionList = new List<string>{
            "プレイボーイ",
            "漫画アクション",
            "プロレス",
            "ＦＲＩＤＡＹ",
            "ヤングマガジン",
            "ヤングチャンピオン"
        };

        [HttpPost("validate")]
        public async Task<IActionResult> Validate([FromBody] List<string> titleList)
        {
            var statusList = new List<string>();

            foreach (var title in titleList)
            {
                if (ExclusionList.Any(ex => title.Contains(ex))) {
                    statusList.Add("필터링");
                    continue;
                }
                var exists = _publications.FindOne(p => p.Title == title) != null;

                if (exists) 
                {
                    statusList.Add("중복");
                    continue;
                }

                var newPublication = new Publication{ Title = title };
                _publications.Insert(newPublication);
                statusList.Add("");
            }

            return Ok(statusList);

        }

        [HttpGet("db/initialize")]
        public async Task<IActionResult> DbInitialize()
        {
            _publications.DeleteAll();
            return Ok();
        }

    }

    public class Publication
    {
        public string Title { get; set; }
    }
}
