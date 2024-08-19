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
            Console.WriteLine("생성");
            _environment = environment;
            _httpClient = new HttpClient();
        }


        [HttpGet("hello")]
        public IActionResult Hello()
        {
            Console.WriteLine("Ok");
            return Ok("hello");
        }

        [HttpPost("upload")]
        public async Task<IActionResult> Upload([FromForm] string fileUrl)
        {
            if (string.IsNullOrEmpty(fileUrl))
            {
                return BadRequest("File URL is missing.");
            }
            
            var fileExtension = ".jpg";
            var fileName = Guid.NewGuid().ToString() + fileExtension;
            var uploadsDir = Path.Combine(_environment.WebRootPath, "main-images");
            if (!Directory.Exists(uploadsDir))
            {
                Directory.CreateDirectory(uploadsDir);
            }

            var filePath = Path.Combine(uploadsDir, fileName);

            try
            {
                using (var response = await _httpClient.GetAsync(fileUrl))
                {
                    if (!response.IsSuccessStatusCode)
                    {
                        return BadRequest("Failed to download the file.");
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

                var fileUrlOnServer = $"{Request.Scheme}://{Request.Host}/uploads/{fileName}";
                return Ok(new { url = fileUrlOnServer });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Internal server error: {ex.Message}");
            }
        }
    }
}
