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
    public void ApplyCover(string epubFilePath, string coverImagePath)
    {
        if (!File.Exists(epubFilePath))
        {
            Console.WriteLine($"✗ EPUB file not found: {epubFilePath}");
            return;
        }

        if (!File.Exists(coverImagePath))
        {
            Console.WriteLine($"✗ Cover image not found: {coverImagePath}");
            return;
        }

        var backupPath = epubFilePath + ".backup";
        var workDirectory = Path.Combine(Path.GetTempPath(), "NovelScraper", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(workDirectory);

        try
        {
            File.Copy(epubFilePath, backupPath, overwrite: true);
            ExtractEpub(epubFilePath, workDirectory);

            var opfRelativePath = ResolveOpfPath(workDirectory);
            var opfAbsolutePath = Path.Combine(workDirectory, ToSystemPath(opfRelativePath));
            var opfDirectory = Path.GetDirectoryName(opfAbsolutePath)!;

            var opfDocument = XDocument.Load(opfAbsolutePath);
            var ns = opfDocument.Root?.Name.Namespace ?? XNamespace.None;
            var manifest = opfDocument.Root?.Element(ns + "manifest") ?? throw new InvalidOperationException("OPF manifest section is missing.");
            var metadata = opfDocument.Root?.Element(ns + "metadata") ?? throw new InvalidOperationException("OPF metadata section is missing.");
            var spine = opfDocument.Root?.Element(ns + "spine") ?? throw new InvalidOperationException("OPF spine section is missing.");

            var mediaType = GetImageMediaType(coverImagePath);
            var coverFileName = "cover" + Path.GetExtension(coverImagePath).ToLowerInvariant();

            var imagesFolderRelative = DetermineImagesFolder(manifest, ns, opfDirectory);
            var coverImageRelativePath = string.IsNullOrEmpty(imagesFolderRelative)
                ? coverFileName
                : CombineRelative(imagesFolderRelative, coverFileName);
            var coverImageAbsolutePath = Path.Combine(opfDirectory, ToSystemPath(coverImageRelativePath));
            Directory.CreateDirectory(Path.GetDirectoryName(coverImageAbsolutePath)!);
            File.Copy(coverImagePath, coverImageAbsolutePath, overwrite: true);

            UpdateManifestForCover(manifest, ns, coverImageRelativePath, mediaType);
            UpdateMetadataForCover(metadata, ns);

            var coverPageRelativePath = EnsureCoverPage(opfDirectory, manifest, ns, coverImageRelativePath, coverImageAbsolutePath);
            UpdateSpineForCover(spine, ns, coverPageRelativePath);

            opfDocument.Save(opfAbsolutePath, SaveOptions.None);

            RepackEpub(workDirectory, epubFilePath);

            File.Delete(backupPath);
            Console.WriteLine($"✓ Cover applied successfully to {Path.GetFileName(epubFilePath)}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"✗ Failed to apply cover to {Path.GetFileName(epubFilePath)}: {ex.Message}");
            RestoreBackup(epubFilePath, backupPath);
        }
        finally
        {
            if (Directory.Exists(workDirectory))
            {
                try
                {
                    Directory.Delete(workDirectory, recursive: true);
                }
                catch
                {
                    // ignore temp cleanup failures
                }
            }
        }
    }

    private static void ExtractEpub(string epubFilePath, string destinationDirectory)
    {
        ZipFile.ExtractToDirectory(epubFilePath, destinationDirectory, overwriteFiles: true);
    }

    private static string ResolveOpfPath(string rootDirectory)
    {
        var containerPath = Path.Combine(rootDirectory, "META-INF", "container.xml");
        if (!File.Exists(containerPath))
        {
            throw new FileNotFoundException("container.xml not found inside EPUB.");
        }

        var containerDoc = XDocument.Load(containerPath);
        var rootfile = containerDoc
            .Descendants()
            .FirstOrDefault(e => e.Name.LocalName == "rootfile")
            ?? throw new InvalidOperationException("Unable to locate OPF rootfile in container.xml.");

        var opfPath = rootfile.Attribute("full-path")?.Value;
        if (string.IsNullOrWhiteSpace(opfPath))
        {
            throw new InvalidOperationException("OPF path is missing in container.xml.");
        }

        return NormalizeRelativePath(opfPath);
    }

    private static string DetermineImagesFolder(XElement manifest, XNamespace ns, string opfDirectory)
    {
        var candidates = new List<string?>();

        var coverItem = manifest.Elements(ns + "item")
            .FirstOrDefault(e => e.Attribute("id")?.Value == "cover-image" ||
                                 (e.Attribute("properties")?.Value?.Contains("cover-image", StringComparison.OrdinalIgnoreCase) ?? false));
        if (coverItem != null)
        {
            candidates.Add(GetDirectoryPart(coverItem.Attribute("href")?.Value));
        }

        var firstImageItem = manifest.Elements(ns + "item")
            .FirstOrDefault(e => (e.Attribute("media-type")?.Value ?? string.Empty)
                .StartsWith("image/", StringComparison.OrdinalIgnoreCase));
        if (firstImageItem != null)
        {
            candidates.Add(GetDirectoryPart(firstImageItem.Attribute("href")?.Value));
        }

        candidates.Add("Images");
        candidates.Add(string.Empty);

        foreach (var candidate in candidates)
        {
            var relative = NormalizeRelativePath(candidate ?? string.Empty);
            var absolute = Path.Combine(opfDirectory, ToSystemPath(relative));
            if (Directory.Exists(absolute))
            {
                return relative;
            }
        }

        var defaultPath = Path.Combine(opfDirectory, "Images");
        Directory.CreateDirectory(defaultPath);
        return "Images";
    }

    private static void UpdateManifestForCover(XElement manifest, XNamespace ns, string coverHref, string mediaType)
    {
        var coverItem = manifest.Elements(ns + "item")
            .FirstOrDefault(e => e.Attribute("id")?.Value == "cover-image" ||
                                 (e.Attribute("properties")?.Value?.Contains("cover-image", StringComparison.OrdinalIgnoreCase) ?? false));

        if (coverItem == null)
        {
            coverItem = new XElement(ns + "item");
            manifest.AddFirst(coverItem);
        }

        coverItem.SetAttributeValue("id", "cover-image");
        coverItem.SetAttributeValue("href", NormalizeRelativePath(coverHref));
        coverItem.SetAttributeValue("media-type", mediaType);
        coverItem.SetAttributeValue("properties", "cover-image");
    }

    private static void UpdateMetadataForCover(XElement metadata, XNamespace ns)
    {
        var coverMeta = metadata.Elements(ns + "meta")
            .FirstOrDefault(e => e.Attribute("name")?.Value == "cover");

        if (coverMeta == null)
        {
            coverMeta = new XElement(ns + "meta",
                new XAttribute("name", "cover"),
                new XAttribute("content", "cover-image"));
            metadata.Add(coverMeta);
        }
        else
        {
            coverMeta.SetAttributeValue("content", "cover-image");
        }
    }

    private static string EnsureCoverPage(
        string opfDirectory,
        XElement manifest,
        XNamespace ns,
        string coverImageRelativePath,
        string coverImageAbsolutePath)
    {
        var textFolderRelative = DetermineTextFolder(manifest, ns, opfDirectory);
        var coverPageRelativePath = string.IsNullOrEmpty(textFolderRelative)
            ? "cover.xhtml"
            : CombineRelative(textFolderRelative, "cover.xhtml");
        var coverPageAbsolutePath = Path.Combine(opfDirectory, ToSystemPath(coverPageRelativePath));
        Directory.CreateDirectory(Path.GetDirectoryName(coverPageAbsolutePath)!);

        var coverPageContent = BuildCoverPageHtml(coverPageAbsolutePath, coverImageAbsolutePath);
        File.WriteAllText(coverPageAbsolutePath, coverPageContent, Encoding.UTF8);

        var coverPageItem = manifest.Elements(ns + "item")
            .FirstOrDefault(e => e.Attribute("id")?.Value == "cover-page");
        if (coverPageItem == null)
        {
            coverPageItem = new XElement(ns + "item");
            manifest.AddFirst(coverPageItem);
        }

        coverPageItem.SetAttributeValue("id", "cover-page");
        coverPageItem.SetAttributeValue("href", NormalizeRelativePath(coverPageRelativePath));
        coverPageItem.SetAttributeValue("media-type", "application/xhtml+xml");

        return NormalizeRelativePath(coverPageRelativePath);
    }

    private static string DetermineTextFolder(XElement manifest, XNamespace ns, string opfDirectory)
    {
        var textItem = manifest.Elements(ns + "item")
            .FirstOrDefault(e => string.Equals(e.Attribute("media-type")?.Value, "application/xhtml+xml", StringComparison.OrdinalIgnoreCase) &&
                                 !string.IsNullOrWhiteSpace(e.Attribute("href")?.Value));

        var relativeFolder = GetDirectoryPart(textItem?.Attribute("href")?.Value);
        if (!string.IsNullOrEmpty(relativeFolder))
        {
            return NormalizeRelativePath(relativeFolder);
        }

        var textPath = Path.Combine(opfDirectory, "Text");
        if (!Directory.Exists(textPath))
        {
            Directory.CreateDirectory(textPath);
        }

        return "Text";
    }

    private static void UpdateSpineForCover(XElement spine, XNamespace ns, string coverPageRelativePath)
    {
        var coverItemRef = spine.Elements(ns + "itemref")
            .FirstOrDefault(e => e.Attribute("idref")?.Value == "cover-page");
        coverItemRef?.Remove();

        var itemRef = new XElement(ns + "itemref", new XAttribute("idref", "cover-page"));
        spine.AddFirst(itemRef);
    }

    private static string BuildCoverPageHtml(string coverPageAbsolutePath, string coverImageAbsolutePath)
    {
        var coverDirectory = Path.GetDirectoryName(coverPageAbsolutePath)!;
        var relativeImagePath = Path.GetRelativePath(coverDirectory, coverImageAbsolutePath).Replace('\\', '/');

        // Build XHTML cover page
        return $@"<?xml version=""1.0"" encoding=""utf-8""?>
<!DOCTYPE html>
<html xmlns=""http://www.w3.org/1999/xhtml"" xmlns:epub=""http://www.idpf.org/2007/ops"">
<head>
    <title>Cover</title>
    <style type=""text/css"">
        body {{ margin: 0; padding: 0; text-align: center; background-color: #000; }}
        img {{ max-width: 100%; height: auto; }}
    </style>
</head>
<body>
    <div id=""cover"">
        <img src=""{relativeImagePath}"" alt=""Cover"" />
    </div>
</body>
</html>";
    }

    private static void RepackEpub(string sourceDirectory, string targetFilePath)
    {
        var tempArchivePath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString("N") + ".epub");

        using (var archive = ZipFile.Open(tempArchivePath, ZipArchiveMode.Create))
        {
            var mimetypePath = Path.Combine(sourceDirectory, "mimetype");
            if (File.Exists(mimetypePath))
            {
                var entry = archive.CreateEntry("mimetype", CompressionLevel.NoCompression);
                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(mimetypePath);
                fileStream.CopyTo(entryStream);
            }

            foreach (var filePath in Directory.EnumerateFiles(sourceDirectory, "*", SearchOption.AllDirectories))
            {
                var relativePath = Path.GetRelativePath(sourceDirectory, filePath).Replace('\\', '/');
                if (relativePath == "mimetype")
                {
                    continue;
                }

                var entry = archive.CreateEntry(relativePath, CompressionLevel.Optimal);
                using var entryStream = entry.Open();
                using var fileStream = File.OpenRead(filePath);
                fileStream.CopyTo(entryStream);
            }
        }

        File.Copy(tempArchivePath, targetFilePath, overwrite: true);
        File.Delete(tempArchivePath);
    }

    private static void RestoreBackup(string epubFilePath, string backupPath)
    {
        if (!File.Exists(backupPath))
        {
            Console.WriteLine("  ⚠️  Backup file not found. Please restore manually if needed.");
            return;
        }

        try
        {
            File.Copy(backupPath, epubFilePath, overwrite: true);
            File.Delete(backupPath);
            Console.WriteLine("  ↻ EPUB restored from backup");
        }
        catch (Exception restoreEx)
        {
            Console.WriteLine($"  ⚠️  Failed to restore backup automatically: {restoreEx.Message}");
            Console.WriteLine($"  Backup is located at: {backupPath}");
        }
    }

    private static string GetImageMediaType(string filePath)
    {
        return Path.GetExtension(filePath).ToLowerInvariant() switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => "image/png"
        };
    }

    private static string NormalizeRelativePath(string value)
    {
        return (value ?? string.Empty).Replace('\\', '/');
    }

    private static string GetDirectoryPart(string? relativePath)
    {
        if (string.IsNullOrWhiteSpace(relativePath))
        {
            return string.Empty;
        }

        var normalized = NormalizeRelativePath(relativePath);
        var lastSlash = normalized.LastIndexOf('/');
        return lastSlash >= 0 ? normalized[..lastSlash] : string.Empty;
    }

    private static string CombineRelative(string left, string right)
    {
        if (string.IsNullOrEmpty(left))
        {
            return NormalizeRelativePath(right);
        }

        if (string.IsNullOrEmpty(right))
        {
            return NormalizeRelativePath(left);
        }

        return NormalizeRelativePath($"{left.TrimEnd('/')}/{right.TrimStart('/')}");
    }

    private static string ToSystemPath(string relativePath)
    {
        return NormalizeRelativePath(relativePath).Replace('/', Path.DirectorySeparatorChar);
    }
}
