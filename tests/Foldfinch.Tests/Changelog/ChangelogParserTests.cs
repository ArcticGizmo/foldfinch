using Foldfinch.Core.Changelog;

namespace Foldfinch.Tests.Changelog;

public class ChangelogParserTests
{
    private const string Sample = """
        # Changelog

        ## [v0.3.0] - 2026-07-22
        - Rotate pages

        ## [v0.2.0] - 2026-07-20
        - Combine PDFs

        ## [v0.1.0] - 2026-07-19
        - First release
        """;

    [Fact]
    public void Parse_finds_all_versioned_sections_newest_first()
    {
        var sections = ChangelogParser.Parse(Sample);

        Assert.Equal(3, sections.Count);
        Assert.Equal("v0.3.0", sections[0].Display);
        Assert.Equal(new Version(0, 3, 0), sections[0].Version);
    }

    [Fact]
    public void UnseenSince_returns_only_newer_sections()
    {
        var unseen = ChangelogParser.UnseenSince(Sample, lastSeen: "0.1.0", current: "0.3.0");

        Assert.Equal(2, unseen.Count);
        Assert.Equal("v0.3.0", unseen[0].Display);
        Assert.Equal("v0.2.0", unseen[1].Display);
    }

    [Fact]
    public void UnseenSince_is_empty_for_a_fresh_install()
    {
        Assert.Empty(ChangelogParser.UnseenSince(Sample, lastSeen: null, current: "0.3.0"));
    }

    [Fact]
    public void UnseenSince_is_empty_when_up_to_date()
    {
        Assert.Empty(ChangelogParser.UnseenSince(Sample, lastSeen: "0.3.0", current: "0.3.0"));
    }
}
