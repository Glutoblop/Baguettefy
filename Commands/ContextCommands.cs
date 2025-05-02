using Discord;
using Discord.Interactions;
using Newtonsoft.Json;
using System.Text;

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
                var response = await client.PostAsync("http://libretranslate:5000/translate", content);
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

    }


}