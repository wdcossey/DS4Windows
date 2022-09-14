using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Windows.Documents;
using HttpProgress;
using System.Text.Json;
using Microsoft.Extensions.Options;
using MarkdownEngine = MdXaml.Markdown;

namespace DS4WinWPF.DS4Forms.ViewModels;

public class ChangelogViewModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UpdateOptions _updateOptions;
        
    private FlowDocument changelogDocument;
    public FlowDocument ChangelogDocument
    {
        get => changelogDocument;
        private set
        {
            if (changelogDocument == value) return;
            changelogDocument = value;
            ChangelogDocumentChanged?.Invoke(this, EventArgs.Empty);
        }
    }
    public event EventHandler ChangelogDocumentChanged;

    public ChangelogViewModel(IHttpClientFactory httpClientFactory, IOptions<UpdateOptions> updateOptions)
    {
        _httpClientFactory = httpClientFactory;
        _updateOptions = updateOptions.Value;
        BuildTempDocument("Retrieving changelog info.Please wait...");
    }

    private void BuildTempDocument(string message)
    {
        var flow = new FlowDocument();
        flow.Blocks.Add(new Paragraph(new Run(message)));
        ChangelogDocument = flow;
    }

    public async void RetrieveChangelogInfoAsync()
    {
        var client = _httpClientFactory.CreateClient("UpdateClient");
        var filename = Path.Combine(Path.GetTempPath(), "Changelog.min.json");
        var readFile = false;
        await using (var downloadStream = new FileStream(filename, FileMode.Create))
        {
            try
            {
                var responseMessage = await client.GetAsync(_updateOptions.Self.Changelog, downloadStream).ConfigureAwait(true); 
                readFile = responseMessage.IsSuccessStatusCode;
            }
            catch (HttpRequestException) { }
        }

        var fileExists = File.Exists(filename);
        if (fileExists && readFile)
        {
            var temp = (await File.ReadAllTextAsync(filename)).Trim();
            try
            {
                var options = new JsonSerializerOptions()
                {
                    PropertyNameCaseInsensitive = true,
                };
                options.Converters.Add(new DateTimeJsonConverter.DateTimeConverterUsingDateTimeParse());
                var tempInfo = JsonSerializer.Deserialize<ChangelogInfo>(temp, options);
                BuildChangelogDocument(tempInfo);
            }
            catch (JsonException) {}
        }
        else if (!readFile)
        {
            BuildTempDocument("Failed to retrieve information");
        }

        if (fileExists)
        {
            File.Delete(filename);
        }
    }

    private void BuildChangelogDocument(ChangelogInfo tempInfo)
    {
        var engine = new MarkdownEngine();
        var flow = new FlowDocument();
        foreach (var versionInfo in tempInfo.Changelog.Versions)
        {
            var tmpLog = versionInfo.ApplicableInfo(Global.UseLang);
                
            if (tmpLog is null)
                continue;
                
            var tmpPar = new Paragraph();
            var tmp = tmpLog.Header;
            tmpPar.Inlines.Add(new Run(tmp) { Tag = "Header" });
            flow.Blocks.Add(tmpPar);

            tmpPar.Inlines.Add(new LineBreak());
            tmpPar.Inlines.Add(new Run(versionInfo.ReleaseDate.ToUniversalTime().ToString("r")) { Tag = "ReleaseDate" });

            tmpLog.BuildDisplayText();

            var tmpDoc = engine.Transform(tmpLog.DisplayLogText);
            flow.Blocks.AddRange(new List<Block>(tmpDoc.Blocks));

            tmpPar = new Paragraph();
            flow.Blocks.Add(tmpPar);
        }

        ChangelogDocument = flow;
    }
}