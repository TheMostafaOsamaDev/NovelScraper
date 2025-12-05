using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using NovelScraper.Domain.Interfaces;

namespace NovelScraper.Infrastructure.Services;

public class EpubCoverService : IEpubCoverService
{
    private static string GetImageMediaType(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "image/png" // fallback
        };
    }

    private static string GetCoverFileName(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLowerInvariant();
        return $"cover{ext}";
    }

    public void ApplyCover(string epubFilePath, string coverImagePath)
    {
        try
        {
            var coverBytes = File.ReadAllBytes(coverImagePath);
            var tempFile = Path.GetTempFileName();
            
            // Copy the EPUB to temp location
            File.Copy(epubFilePath, tempFile, overwrite: true);

            // EPUB files are ZIP archives - we can manipulate them directly
            using (var archive = ZipFile.Open(tempFile, ZipArchiveMode.Update))
            {
                // Remove existing cover if present (any image format)
                var existingCovers = archive.Entries.Where(e =>
                {
                    var name = e.FullName.ToLowerInvariant();
                    return (name.Contains("cover.") && 
                           (name.EndsWith(".png") || name.EndsWith(".jpg") || name.EndsWith(".jpeg") || 
                            name.EndsWith(".gif") || name.EndsWith(".bmp") || name.EndsWith(".webp")));
                }).ToList();

                foreach (var cover in existingCovers)
                {
                    cover.Delete();
                }

                // Add new cover image with appropriate extension
                var coverFileName = GetCoverFileName(coverImagePath);
                var coverEntry = archive.CreateEntry($"OEBPS/Images/{coverFileName}", CompressionLevel.Optimal);
                using (var entryStream = coverEntry.Open())
                {
                    entryStream.Write(coverBytes, 0, coverBytes.Length);
                }

                // Update content.opf to reference the cover
                UpdateContentOpf(archive, coverImagePath);
            }

            // Replace original file with modified version
            File.Copy(tempFile, epubFilePath, overwrite: true);
            File.Delete(tempFile);

            Console.WriteLine($"✓ Cover applied successfully to {Path.GetFileName(epubFilePath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to apply cover to {Path.GetFileName(epubFilePath)}: {ex.Message}");
        }
    }

    private void UpdateContentOpf(ZipArchive archive, string coverImagePath)
    {
        // Find content.opf file
        var opfEntry = archive.Entries.FirstOrDefault(e => 
            e.FullName.EndsWith("content.opf", StringComparison.OrdinalIgnoreCase));

        if (opfEntry == null) return;

        string opfContent;
        using (var reader = new StreamReader(opfEntry.Open()))
        {
            opfContent = reader.ReadToEnd();
        }

        try
        {
            var doc = XDocument.Parse(opfContent);
            var ns = doc.Root?.Name.Namespace ?? XNamespace.None;

            var coverFileName = GetCoverFileName(coverImagePath);
            var mediaType = GetImageMediaType(coverImagePath);

            // Add cover image to manifest if not present
            var manifest = doc.Root?.Element(ns + "manifest");
            if (manifest != null)
            {
                var coverItem = manifest.Elements(ns + "item")
                    .FirstOrDefault(e => e.Attribute("id")?.Value == "cover-image");

                if (coverItem == null)
                {
                    manifest.Add(new XElement(ns + "item",
                        new XAttribute("id", "cover-image"),
                        new XAttribute("href", $"Images/{coverFileName}"),
                        new XAttribute("media-type", mediaType),
                        new XAttribute("properties", "cover-image")));
                }
                else
                {
                    // Update existing cover item
                    coverItem.SetAttributeValue("href", $"Images/{coverFileName}");
                    coverItem.SetAttributeValue("media-type", mediaType);
                }
            }

            // Update the file
            opfEntry.Delete();
            var newOpfEntry = archive.CreateEntry(opfEntry.FullName, CompressionLevel.Optimal);
            using (var writer = new StreamWriter(newOpfEntry.Open(), Encoding.UTF8))
            {
                doc.Save(writer);
            }
        }
        catch
        {
            // If XML parsing fails, we'll just skip updating the OPF
            // The cover image is still added to the archive
        }
    }
}
