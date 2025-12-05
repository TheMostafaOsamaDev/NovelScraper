using System.Collections.Generic;

namespace NovelScraper.Domain.Entities.Covers;

public class NovelCoverUpdateReport
{
    public NovelCoverUpdateReport(
        string novelName,
        IReadOnlyList<string> updatedVolumes,
        IReadOnlyList<string> missingCoverVolumes,
        bool coversFolderMissing,
        bool hasVolumes)
    {
        NovelName = novelName;
        UpdatedVolumes = updatedVolumes;
        MissingCoverVolumes = missingCoverVolumes;
        CoversFolderMissing = coversFolderMissing;
        HasVolumes = hasVolumes;
    }

    public string NovelName { get; }
    public IReadOnlyList<string> UpdatedVolumes { get; }
    public IReadOnlyList<string> MissingCoverVolumes { get; }
    public bool CoversFolderMissing { get; }
    public bool HasVolumes { get; }

    public bool AllVolumesUpdated => MissingCoverVolumes.Count == 0 && HasVolumes;
}
