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

        public BasicController(IWebHostEnvironment environment)
        {
            _environment = environment;
            _httpClient = new HttpClient();
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] List<string> fileUrls)
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

            return Ok(new { urls = fileUrlsOnServer });
        }
    }
}
