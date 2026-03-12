using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.IO.Compression;

namespace BE_QLKH.Controllers;

[ApiController]
[Route("api/[controller]")]
[Authorize]
public class ExportController : ControllerBase
{
    [HttpPost("word")]
    public async Task<IActionResult> ExportWord([FromForm] IFormFile file, [FromForm] string data)
    {
        try
        {
            if (file == null || file.Length == 0)
                return BadRequest("Vui lòng upload file template");

            List<Dictionary<string, string>>? dataList;
            try 
            {
                // Try to deserialize as List first (Bulk export)
                dataList = System.Text.Json.JsonSerializer.Deserialize<List<Dictionary<string, string>>>(data);
            }
            catch (Exception ex)
            {
                try
                {
                    // Fallback: deserialize as Single Dictionary and wrap in List
                    var single = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, string>>(data);
                    dataList = single != null ? new List<Dictionary<string, string>> { single } : null;
                }
                catch
                {
                    return BadRequest($"Dữ liệu thay thế không đúng định dạng JSON: {ex.Message}");
                }
            }

            if (dataList == null || dataList.Count == 0)
                return BadRequest("Dữ liệu thay thế không được để trống");

            // Read template file to byte array once
            using var templateMs = new MemoryStream();
            await file.CopyToAsync(templateMs);
            var templateBytes = templateMs.ToArray();

            // If only 1 item, return DOCX
            if (dataList.Count == 1)
            {
                var resultBytes = GenerateDocx(templateBytes, dataList[0]);
                return File(resultBytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", "exported_document.docx");
            }

            // If multiple items, return ZIP
            using var zipStream = new MemoryStream();
            using (var archive = new ZipArchive(zipStream, ZipArchiveMode.Create, true))
            {
                // Track used filenames to avoid duplicates
                var usedFileNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                for (int i = 0; i < dataList.Count; i++)
                {
                    var replacements = dataList[i];
                    var bytes = GenerateDocx(templateBytes, replacements);
                    
                    // Naming: Use _FileName if present, otherwise Document_i.docx
                    string baseFileName = $"Document_{i + 1}";
                    if (replacements.ContainsKey("_FileName") && !string.IsNullOrWhiteSpace(replacements["_FileName"]))
                    {
                        baseFileName = replacements["_FileName"];
                    }

                    // Ensure unique filename
                    string fileName = baseFileName;
                    if (!fileName.EndsWith(".docx", StringComparison.OrdinalIgnoreCase))
                        fileName += ".docx";
                    
                    int duplicateCount = 1;
                    while (usedFileNames.Contains(fileName))
                    {
                        string nameWithoutExt = Path.GetFileNameWithoutExtension(baseFileName);
                        if (nameWithoutExt.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)) // handle case where baseFileName already had extension
                             nameWithoutExt = Path.GetFileNameWithoutExtension(nameWithoutExt);

                        fileName = $"{nameWithoutExt}_{duplicateCount}.docx";
                        duplicateCount++;
                    }
                    usedFileNames.Add(fileName);
                    
                    var entry = archive.CreateEntry(fileName);
                    using var entryStream = entry.Open();
                    await entryStream.WriteAsync(bytes, 0, bytes.Length);
                }
            }

            zipStream.Position = 0;
            return File(zipStream.ToArray(), "application/zip", "exported_documents.zip");
        }
        catch (Exception ex)
        {
            return StatusCode(500, $"Lỗi server: {ex.Message} - {ex.StackTrace}");
        }
    }

    private byte[] GenerateDocx(byte[] templateBytes, Dictionary<string, string> replacements)
    {
        using var memoryStream = new MemoryStream();
        memoryStream.Write(templateBytes, 0, templateBytes.Length);
        memoryStream.Position = 0;
        
        using (var doc = WordprocessingDocument.Open(memoryStream, true))
        {
            ApplyReplacements(doc, replacements);
            doc.Save();
        }
        return memoryStream.ToArray();
    }

    private static void ApplyReplacements(WordprocessingDocument doc, Dictionary<string, string> replacements)
    {
        if (doc.MainDocumentPart == null) return;

        var sortedReplacements = replacements
            .Where(k => !string.IsNullOrWhiteSpace(k.Key))
            .OrderByDescending(k => k.Key.Length)
            .Select(k => new KeyValuePair<string, string>(k.Key, k.Value ?? string.Empty))
            .ToList();

        if (sortedReplacements.Count == 0) return;

        if (doc.MainDocumentPart.Document != null)
        {
            ReplaceInRoot(doc.MainDocumentPart.Document, sortedReplacements);
        }

        foreach (var headerPart in doc.MainDocumentPart.HeaderParts)
        {
            if (headerPart.Header != null)
            {
                ReplaceInRoot(headerPart.Header, sortedReplacements);
            }
        }

        foreach (var footerPart in doc.MainDocumentPart.FooterParts)
        {
            if (footerPart.Footer != null)
            {
                ReplaceInRoot(footerPart.Footer, sortedReplacements);
            }
        }

        if (doc.MainDocumentPart.FootnotesPart?.Footnotes != null)
        {
            ReplaceInRoot(doc.MainDocumentPart.FootnotesPart.Footnotes, sortedReplacements);
        }

        if (doc.MainDocumentPart.EndnotesPart?.Endnotes != null)
        {
            ReplaceInRoot(doc.MainDocumentPart.EndnotesPart.Endnotes, sortedReplacements);
        }
    }

    private static void ReplaceInRoot(OpenXmlPartRootElement root, List<KeyValuePair<string, string>> sortedReplacements)
    {
        foreach (var paragraph in root.Descendants<Paragraph>())
        {
            var texts = paragraph.Descendants<Text>().ToList();
            if (texts.Count == 0) continue;

            var original = string.Concat(texts.Select(t => t.Text));
            if (string.IsNullOrEmpty(original)) continue;

            var expected = original;
            foreach (var kvp in sortedReplacements)
            {
                if (expected.Contains(kvp.Key))
                {
                    expected = expected.Replace(kvp.Key, kvp.Value);
                }
            }

            if (string.Equals(expected, original, StringComparison.Ordinal)) continue;

            foreach (var text in texts)
            {
                foreach (var kvp in sortedReplacements)
                {
                    if (!text.Text.Contains(kvp.Key)) continue;
                    text.Text = text.Text.Replace(kvp.Key, kvp.Value);
                }
            }

            var after = string.Concat(texts.Select(t => t.Text));
            if (string.Equals(after, expected, StringComparison.Ordinal)) continue;

            texts[0].Text = expected;
            texts[0].Space = SpaceProcessingModeValues.Preserve;
            for (var i = 1; i < texts.Count; i++)
            {
                texts[i].Text = string.Empty;
            }
        }
    }
}
