using Discord;
using Discord.Interactions;
using Newtonsoft.Json;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Png;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.Text;
using Tesseract;
using Image = SixLabors.ImageSharp.Image;

namespace Baguettefy.Commands
{
    public class ContextCommands : InteractionModuleBase<InteractionContext>
    {
        private IServiceProvider _Services;

        public ContextCommands(IServiceProvider services)
        {
            _Services = services;
        }

        public class ChangeCharacterModal : IModal
        {
            [ModalTextInput("CharacterName")]
            public string CharacterName { get; set; }

            public string Title => "What is the Character name?";
        }

        [MessageCommand("Translate")] // User context menu
        public async Task Translate(IMessage msg)
        {
            await DeferAsync();

            var images = msg.Attachments?.Where(s => s.ContentType.StartsWith("image"))?.ToList() ?? new();
            var hasImages = images.Any();
            if (hasImages)
            {
                List<string> translated = new();

                foreach (var image in images)
                {
                    try
                    {
                        Image<Rgba32> sourceImage = await LoadImageFromUrlAsync(image.Url);

                        using var engine = new TesseractEngine("Content", "fra", EngineMode.Default);
                        //engine.SetVariable("tessedit_char_whitelist", "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ-");
                        engine.SetVariable("preserve_interword_spaces", "1");

                        await ModifyOriginalResponseAsync(properties => { properties.Content = $"Preprocessing {image.Filename}.."; });
                        Console.WriteLine($"Preprocessing {image.Filename}..");

                        // Preprocess
                        var pImage = PreprocessImage(sourceImage);
                        var processedImage = pImage.CloneAs<Rgba32>();

                        using var ms = new MemoryStream();
                        await processedImage.SaveAsync(ms, new PngEncoder());
                        ms.Position = 0; // Reset stream
                        byte[] imageBytes = ms.ToArray();
                        using var pix = Pix.LoadFromMemory(imageBytes);
                        using var page = engine.Process(pix, PageSegMode.SingleBlock);

                        await ModifyOriginalResponseAsync(properties => { properties.Content = $"Fetching text from {image.Filename}.."; });
                        Console.WriteLine($"Fetching text from {image.Filename}..");

                        var imageText = page.GetText().ToLowerInvariant();

                        await ModifyOriginalResponseAsync(properties => { properties.Content = $"Translating text from {image.Filename}.."; });
                        Console.WriteLine($"Translating text from {image.Filename}..");

                        var translatedText = await TranslateTextAsync(imageText, "fr", "en");
                        if (!string.IsNullOrEmpty(translatedText))
                        {
                            translated.Add(translatedText);
                            continue;
                        }
                    }
                    catch (Exception ex)
                    {

                    }
                    translated.Add($"🥖 Failed to find and translate text :(");
                }

                await ModifyOriginalResponseAsync(s => s.Content = $"🥖 Baguettefy Translations to follow 🥖");

                string translatedImagesText = "";
                foreach (var t in translated)
                {
                    try
                    {
                        await msg.Channel.SendMessageAsync($"🥖 Baguettefy Translated:\n\n```{t}```",
                            messageReference: new MessageReference(msg.Id, msg.Channel.Id, Context.Interaction.GuildId));
                    }
                    catch (Exception ex)
                    {
                        await msg.Channel.SendMessageAsync($"🥖 Baguettefy Failed:\n\nCan only translate 2000 characters at a time, split up big images.",
                            messageReference: new MessageReference(msg.Id, msg.Channel.Id, Context.Interaction.GuildId));
                    }
                }
                return;
            }
            else
            {
                try
                {
                    var translated = await TranslateTextAsync(msg.Content, "fr", "en");
                    if (!string.IsNullOrEmpty(translated))
                    {
                        await ModifyOriginalResponseAsync(s => s.Content = $"🥖 Baguettefy Translated:\n\n```{translated}```");
                    }
                    return;
                }
                catch (Exception ex)
                {

                }
            }

            await ModifyOriginalResponseAsync(s => s.Content = "🥖 Failed :(");
        }

        public static async Task<string> TranslateTextAsync(string text, string sourceLang, string targetLang)
        {
            var client = new HttpClient
            {
                Timeout = TimeSpan.FromSeconds(10)
            };

            var requestBody = new
            {
                q = text,
                source = "fr",
                target = "en",
                format = "text"
            };

            var json = JsonConvert.SerializeObject(requestBody);
            var content = new StringContent(json, Encoding.UTF8, "application/json");

            try
            {
                HttpResponseMessage response;
#if DEBUG
                response = await client.PostAsync("http://localhost:5000/translate", content);
#else
                response = await client.PostAsync("http://libretranslate:5000/translate", content);
#endif
                response.EnsureSuccessStatusCode();

                var result = await response.Content.ReadAsStringAsync();
                dynamic parsed = JsonConvert.DeserializeObject(result);
                Console.WriteLine($"Translated Result: {parsed}");
                return parsed.translatedText;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error: {ex.Message}");
            }

            return null;
        }

        public static async Task<Image<Rgba32>> LoadImageFromUrlAsync(string url)
        {
            using var httpClient = new HttpClient();
            var response = await httpClient.GetAsync(url);

            if (!response.IsSuccessStatusCode)
            {
                return null;
            }

            using var stream = await response.Content.ReadAsStreamAsync();
            var image = await Image.LoadAsync<Rgba32>(stream);

            return image;
        }

        public static Image<Rgba32> PreprocessImage(Image<Rgba32> input)
        {
            // 1. Convert to Grayscale
            input.Mutate(x => x.Grayscale());
#if DEBUG
            input.SaveAsPng("0_greyscale.png");
#endif

            // 2. Increase Contrast
            input.Mutate(x => x.Contrast(1.6f)); // 1.0 = normal, >1 = more contrast
#if DEBUG
            input.SaveAsPng("1_contrast.png");
#endif

            //            // 3. Binarize (Thresholding at 0.4 brightness)
            //            input.Mutate(ctx => ctx.BinaryThreshold(0.4f)); // 0.0 - 1.0
            //#if DEBUG
            //            input.SaveAsPng("2_binarize.png");
            //#endif
            //
            //
            //            // 4. Median Blur (simple 3x3 kernel)
            //            input = ApplyMedianBlur(input, 3);
            //#if DEBUG
            //            input.SaveAsPng("3_blur.png");
            //#endif

            // 5. Upscale by 2x
            int scaleFactor = CalculateScalingRatio(input.Width, input.Height, 4000f);
            input.Mutate(x => x.Resize(input.Width * scaleFactor, input.Height * scaleFactor, KnownResamplers.Bicubic));

            input.Mutate(x => x.GaussianSharpen(1.0f));

            input.Mutate(x => x.Invert());

#if DEBUG
            // Save if you want to debug
            input.SaveAsPng("4_final.png");
#endif

            return input;
        }

        private static Image<Rgba32> ApplyMedianBlur(Image<Rgba32> image, int kernelSize = 3)
        {
            var blurred = image.Clone();

            int radius = kernelSize / 2;

            for (int y = radius; y < image.Height - radius; y++)
            {
                for (int x = radius; x < image.Width - radius; x++)
                {
                    var neighborhood = new List<byte>();

                    for (int ky = -radius; ky <= radius; ky++)
                    {
                        for (int kx = -radius; kx <= radius; kx++)
                        {
                            Rgba32 pixel = image[x + kx, y + ky];
                            neighborhood.Add(pixel.R); // Already grayscale so R=G=B
                        }
                    }

                    neighborhood.Sort();
                    byte median = neighborhood[neighborhood.Count / 2];
                    blurred[x, y] = new Rgba32(median, median, median);
                }
            }

            return blurred;
        }

        static int CalculateScalingRatio(int width, int height, float maxLimit)
        {
            // Calculate the scaling factors for both x and y directions
            float scaleX = maxLimit / (float)width;
            float scaleY = maxLimit / (float)height;

            // Find the minimum scaling factor to maintain the aspect ratio
            return (int)Math.Min(scaleX, scaleY);
        }
        public static (int width, int height) ResizeKeepAspectRatio(int originalWidth, int originalHeight, int targetWidth)
        {
            if (originalWidth <= 0 || originalHeight <= 0 || targetWidth <= 0)
                throw new ArgumentException("All dimensions must be greater than zero.");

            double aspectRatio = (double)originalHeight / originalWidth;
            int newHeight = (int)Math.Round(targetWidth * aspectRatio);

            return (targetWidth, newHeight);
        }

    }


}