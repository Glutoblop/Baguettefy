using Newtonsoft.Json;
using System.IO.Compression;

namespace Baguettefy.Data.Nuggets
{
    public class GenerateNuggetZip
    {
        public static async Task Generate()
        {
            if (Directory.Exists(NuggetUtils.zipFileDestination))
            {
                Directory.Delete(NuggetUtils.zipFileDestination, true);
            }

            using HttpClient client = new HttpClient();

            //Uncomment this when wanting to re-create the zip file
            List<NuggetData>? nuggetData = JsonConvert.DeserializeObject<List<NuggetData>>(await File.ReadAllTextAsync("res/nugget.json"));
            if (nuggetData == null) return;
            for (var i = 0; i < nuggetData.Count; i++)
            {
                var nugget = nuggetData[i];

                await NuggetUtils.GetNuggetValue(client, nugget.AnkamaId, true);

                Console.WriteLine($"## Init {i} / {nuggetData.Count}");
            }

            File.Delete(NuggetUtils.zipFileSource);
            ZipFile.CreateFromDirectory(NuggetUtils.zipFileDestination, NuggetUtils.zipFileSource);
        }
    }
}
