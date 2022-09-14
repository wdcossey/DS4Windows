﻿using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Windows.Documents;
using System.Text.Json;
using System.Text.Json.Serialization;
using HttpProgress;
using Microsoft.Extensions.Options;
using MarkdownEngine = MdXaml.Markdown;

namespace DS4WinWPF.DS4Forms.ViewModels;

public class UpdaterWindowViewModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly UpdateOptions _updateOptions;
    private readonly string? _newVersion;
    
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


    public UpdaterWindowViewModel(IHttpClientFactory httpClientFactory, IOptions<UpdateOptions> updateOptions, string newVersion)
    {
        BuildTempDocument("Retrieving changelog info.Please wait...");
        _httpClientFactory = httpClientFactory;
        _updateOptions = updateOptions.Value;
        _newVersion = newVersion;
        //RetrieveChangelogInfo();
    }
    
    public async void RetrieveChangelogInfo()
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
            catch (JsonException) { }
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
            var versionNumber = versionInfo.VersionNumberInfo.GetVersionNumber();
            if (versionNumber > Global.exeversionLong)
            {
                var tmpLog = versionInfo.ApplicableInfo(Global.UseLang);
                if (tmpLog != null)
                {
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
            }
        }

        ChangelogDocument = flow;
    }

    private void BuildTempDocument(string message)
    {
        var flow = new FlowDocument();
        flow.Blocks.Add(new Paragraph(new Run(message)));
        ChangelogDocument = flow;
    }

    public void SetSkippedVersion()
    {
        if (!string.IsNullOrEmpty(_newVersion))
        {
            Global.LastVersionChecked = _newVersion;
        }
    }

    public void BlankSkippedVersion() => 
        Global.LastVersionChecked = string.Empty;
    
}

public class ChangelogInfo
{
    [JsonPropertyName("latest_version")]
    public string LatestVersion { get; set; }
    
    [JsonPropertyName("updated_at")]
    public DateTime UpdatedAt { get; set; }

    [JsonPropertyName("changelog")]
    public ChangelogVersions Changelog { get; set; }

    [JsonPropertyName("latest_version_number_info")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public ChangeVersionNumberInfo LatestVersionInfo { get; set; }
}

public class ChangeVersionNumberInfo
{
    [JsonPropertyName("majorPart")]
    public ushort MajorPart { get; set; }

    [JsonPropertyName("minorPart")]
    public ushort MinorPart { get; set; }

    [JsonPropertyName("buildPart")]
    public ushort BuildPart { get; set; }

    [JsonPropertyName("privatePart")]
    public ushort PrivatePart { get; set; }

    public ulong GetVersionNumber()
    {
        var temp = (ulong)MajorPart << 48 | (ulong)MinorPart << 32 |
                   (ulong)BuildPart << 16 | PrivatePart;
        return temp;
    }
}

public class ChangelogVersions
{
    [JsonPropertyName("versions")]
    public List<ChangeVersionInfo> Versions { get; set; }
}

public class ChangeVersionInfo
{
    [JsonPropertyName("version_str")]
    public string Version { get; set; }

    [JsonPropertyName("base_header")]
    public string BaseHeader { get; set; }

    [JsonPropertyName("release_date")]
    public DateTime ReleaseDate { get; set; }

    [JsonPropertyName("locales")]
    public List<VersionLogLocale> VersionLocales { get; set; }

    [JsonPropertyName("version_number_info")]
    [JsonIgnore(Condition = JsonIgnoreCondition.Never)]
    public ChangeVersionNumberInfo VersionNumberInfo { get; set; }

    public VersionLogLocale? ApplicableInfo(string culture)
    {
        var tempDict = VersionLocales.ToDictionary(logLoc => logLoc.Code);

        VersionLogLocale? result = null;
        CultureInfo? hairyLegs = null;
        try
        {
            if (!string.IsNullOrEmpty(culture))
            {
                hairyLegs = CultureInfo.GetCultureInfo(culture);
            }
        }
        catch (CultureNotFoundException) { }

        if (hairyLegs != null)
        {
            if (tempDict.ContainsKey(hairyLegs.Name))
            {
                result = tempDict[hairyLegs.Name];
            }
            else if (tempDict.ContainsKey(hairyLegs.TwoLetterISOLanguageName))
            {
                result =
                    tempDict[hairyLegs.TwoLetterISOLanguageName];
            }
        }

        if (result == null && VersionLocales.Count > 0)
        {
            // Default to first entry if specific culture info not found
            result = VersionLocales[0];
        }

        return result;
    }
}

public class VersionLogLocale
{
    public string DisplayLogText { get; private set; }

    public string Code { get; set; }

    public string Header { get; set; }

    [JsonPropertyName("log_text")]
    public List<string> LogText { get; set; }

    [JsonPropertyName("editor")]
    public string Editor { get; set; }

    [JsonPropertyName("editors_note")]
    public List<string> EditorsNote { get; set; }

    [JsonPropertyName("updated_at")]
    public DateTime EditedAt { get; set; }

    public void BuildDisplayText()
    {
        DisplayLogText = string.Join("\n", LogText);
    }
}