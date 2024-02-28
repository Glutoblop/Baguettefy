using System.Text;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Services;
using Google.Apis.Sheets.v4;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;

namespace Baguettefy.Core.Sheets
{
    [Serializable]
    class LocalWorksheet
    {
        public string Title;
        public IList<IList<object>> Data;
    }

    public struct OptionRange
    {
        public string Column;
        public int Row;

        public OptionRange(string column, int row)
        {
            Row = row;
            Column = column;
        }

        public override string ToString()
        {
            return $"{Column}{Row}";
        }
    }

    public class GoogleSheetsService
    {
        private SheetsService _Service;

        private string? spreadsheetId;

        private static string CacheFolderName => $"{Directory.GetCurrentDirectory()}\\GoogleSheetCache";
        private List<LocalWorksheet>? _Worksheets;

        public async Task Init(IConfigurationRoot config)
        {
            //https://code-maze.com/google-sheets-api-with-net-core/
            //https://developers.google.com/sheets/api/guides/field-masks
            byte[] serviceAccountBytes = Encoding.ASCII.GetBytes(config["serviceaccount"]);
            GoogleCredential credential = GoogleCredential.FromServiceAccountCredential(ServiceAccountCredential.FromServiceAccountData(new MemoryStream(serviceAccountBytes)));
            _Service = new SheetsService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "Dofus LFG Data"
            });

            spreadsheetId = config["googlesheetid"];

            _Worksheets = new List<LocalWorksheet>();

            var worksheets = await _Service.Spreadsheets.Get(spreadsheetId).ExecuteAsync();

            if (Directory.Exists(CacheFolderName))
            {
                var directory = new DirectoryInfo(CacheFolderName);
                foreach (FileInfo file in directory.GetFiles()) file.Delete();
                foreach (DirectoryInfo subDirectory in directory.GetDirectories()) subDirectory.Delete(true);
                Directory.Delete(CacheFolderName);
            }
            Directory.CreateDirectory(CacheFolderName);

            foreach (var worksheet in worksheets.Sheets)
            {
                var local = new LocalWorksheet
                {
                    Title = worksheet.Properties.Title
                };
                var data = await _Service.Spreadsheets.Values.Get(spreadsheetId, $"{local.Title}!A1:Z300").ExecuteAsync();
                local.Data = data.Values;

                _Worksheets.Add(local);

                var json = JsonConvert.SerializeObject(local);
                var folder = $"{CacheFolderName}\\{local.Title}";
                await File.WriteAllTextAsync($"{folder}.json", json);
            }
        }

        public async Task<IList<IList<object>>?> Get(string sheetTab, OptionRange initialRange, OptionRange? secondRange = null)
        {
            IList<IList<object>>? values = null;

            if (_Worksheets == null)
            {
                string range = secondRange == null
                    ? $"{sheetTab}!{initialRange}"
                    : $"{sheetTab}!{initialRange}:{secondRange}";

                var valueCollection = await _Service.Spreadsheets.Values
                    .Get(spreadsheetId, range)
                    .ExecuteAsync();

                values = valueCollection.Values;
            }
            else
            {
                var fileName = $"{CacheFolderName}\\{sheetTab}.json";
                if (File.Exists(fileName))
                {
                    values = JsonConvert.DeserializeObject<LocalWorksheet>(await File.ReadAllTextAsync(fileName))?.Data;
                }
            }

            return values;

        }
    }
}
