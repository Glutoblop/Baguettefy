using Discord;

namespace Baguettefy
{
    public class EmbedCreator
    {
        public static EmbedBuilder CreateTranslatedEmbed(string subType, string english, string french)
        {
            var embedBuilder = new EmbedBuilder()
            {
                Title = $"{subType} Found",
                ThumbnailUrl = "https://api.dofusdu.de/dofus2/img/item/15950-800.png"
            };

            embedBuilder.WithFields(new[]
            {
                    new EmbedFieldBuilder()
                        .WithName($"English Name")
                        .WithValue($"{english}")
                        .WithIsInline(false),

                    new EmbedFieldBuilder()
                        .WithName($"French Name")
                        .WithValue($"{french}")
                        .WithIsInline(false)

            });

            return embedBuilder;
        }
    }
}
