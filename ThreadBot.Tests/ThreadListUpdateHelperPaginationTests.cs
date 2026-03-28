using System.Reflection;
using Discord;
using Microsoft.Extensions.Logging;
using ThreadBot.Models;

namespace ThreadBot.Tests;

public class ThreadListUpdateHelperPaginationTests
{
    private const int MaxEmbedFieldValueLength = 1024;
    private const int MaxFieldsPerPage = 25;
    private const int MaxEmbedContentLength = 5800;

    #region BuildFieldsForChannelGroup Tests

    private static List<EmbedFieldBuilder> InvokeBuildFieldsForChannelGroup(string channelName, List<ThreadChannelPartial> threads)
    {
        var method = typeof(ThreadListUpdateHelper).GetMethod("BuildFieldsForChannelGroup",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (List<EmbedFieldBuilder>)method.Invoke(null, [channelName, threads])!;
    }

    private static ThreadChannelPartial MakeThread(string name, string? mention = null)
    {
        return new ThreadChannelPartial(name, mention ?? $"<#{name}>", "test-channel", "#test-channel");
    }

    [Fact]
    public void BuildFieldsForChannelGroup_EmptyThreads_ReturnsEmptyList()
    {
        var result = InvokeBuildFieldsForChannelGroup("general", []);
        Assert.Empty(result);
    }

    [Fact]
    public void BuildFieldsForChannelGroup_SingleThread_ReturnsSingleField()
    {
        var threads = new List<ThreadChannelPartial> { MakeThread("thread-1") };

        var result = InvokeBuildFieldsForChannelGroup("general", threads);

        Assert.Single(result);
        Assert.Equal("#general", result[0].Name);
        Assert.Equal("<#thread-1>", result[0].Value);
    }

    [Fact]
    public void BuildFieldsForChannelGroup_MultipleThreadsFitInOneField_ReturnsSingleField()
    {
        var threads = Enumerable.Range(1, 5)
            .Select(i => MakeThread($"thread-{i}"))
            .ToList();

        var result = InvokeBuildFieldsForChannelGroup("general", threads);

        Assert.Single(result);
        Assert.Equal("#general", result[0].Name);
        var value = (string)result[0].Value;
        Assert.Contains("<#thread-1>", value);
        Assert.Contains("<#thread-5>", value);
    }

    [Fact]
    public void BuildFieldsForChannelGroup_ThreadsOrderedByName()
    {
        var threads = new List<ThreadChannelPartial>
        {
            MakeThread("zebra"),
            MakeThread("alpha"),
            MakeThread("middle")
        };

        var result = InvokeBuildFieldsForChannelGroup("general", threads);

        Assert.Single(result);
        var value = (string)result[0].Value;
        var alphaIndex = value.IndexOf("<#alpha>", StringComparison.Ordinal);
        var middleIndex = value.IndexOf("<#middle>", StringComparison.Ordinal);
        var zebraIndex = value.IndexOf("<#zebra>", StringComparison.Ordinal);
        Assert.True(alphaIndex < middleIndex);
        Assert.True(middleIndex < zebraIndex);
    }

    [Fact]
    public void BuildFieldsForChannelGroup_ManyThreads_SplitsIntoMultipleFields()
    {
        // Create enough threads to exceed 1024 chars. Each mention is ~25 chars, so ~42 threads should exceed it.
        var threads = Enumerable.Range(1, 60)
            .Select(i => MakeThread($"thread-{i:D3}", $"<#mention-thread-{i:D3}>"))
            .ToList();

        var result = InvokeBuildFieldsForChannelGroup("general", threads);

        Assert.True(result.Count > 1, $"Expected multiple fields but got {result.Count}");
        Assert.Equal("#general", result[0].Name);
        Assert.Equal("#general (cont.)", result[1].Name);

        // Verify each field value is within the limit
        foreach (var field in result)
        {
            Assert.True(((string)field.Value).Length <= MaxEmbedFieldValueLength,
                $"Field value length {((string)field.Value).Length} exceeds {MaxEmbedFieldValueLength}");
        }
    }

    [Fact]
    public void BuildFieldsForChannelGroup_AllMentionsPresent()
    {
        var threads = Enumerable.Range(1, 60)
            .Select(i => MakeThread($"thread-{i}", $"<#mention-{i}>"))
            .ToList();

        var result = InvokeBuildFieldsForChannelGroup("general", threads);

        var allValues = string.Join("\n", result.Select(f => (string)f.Value));
        for (int i = 1; i <= 60; i++)
        {
            Assert.Contains($"<#mention-{i}>", allValues);
        }
    }

    [Fact]
    public void BuildFieldsForChannelGroup_268Threads_AllFieldsUnderLimit()
    {
        // Simulate the real scenario with 268 threads
        var threads = Enumerable.Range(1, 268)
            .Select(i => MakeThread($"thread-{i:D3}", $"<#1234567890{i:D3}>"))
            .ToList();

        var result = InvokeBuildFieldsForChannelGroup("big-channel", threads);

        Assert.True(result.Count > 1);
        foreach (var field in result)
        {
            var valueLength = ((string)field.Value).Length;
            Assert.True(valueLength <= MaxEmbedFieldValueLength,
                $"Field value length {valueLength} exceeds {MaxEmbedFieldValueLength}");
            Assert.True(valueLength > 0, "Field value should not be empty");
        }
    }

    #endregion

    #region BuildThreadEmbed Tests

    private static EmbedBuilder InvokeBuildThreadEmbed(List<EmbedFieldBuilder> pageFields, int pageIndex)
    {
        var method = typeof(ThreadListUpdateHelper).GetMethod("BuildThreadEmbed",
            BindingFlags.NonPublic | BindingFlags.Static);
        Assert.NotNull(method);
        return (EmbedBuilder)method.Invoke(null, [pageFields, pageIndex])!;
    }

    [Fact]
    public void BuildThreadEmbed_WithFields_TitleIsActiveThreads()
    {
        var fields = new List<EmbedFieldBuilder>
        {
            new() { Name = "#general", Value = "<#thread-1>", IsInline = false }
        };

        var result = InvokeBuildThreadEmbed(fields, 0);

        Assert.Equal("Active Threads", result.Title);
    }

    [Fact]
    public void BuildThreadEmbed_EmptyFields_TitleIsNoActiveThreads()
    {
        var result = InvokeBuildThreadEmbed([], 0);

        Assert.Equal("No Active Threads", result.Title);
        Assert.Single(result.Fields);
        Assert.Equal("No Active Threads", result.Fields[0].Name);
        Assert.Equal("No threads are currently active on the server.", result.Fields[0].Value);
    }

    [Fact]
    public void BuildThreadEmbed_PageIndex_ReflectedInFooter()
    {
        var fields = new List<EmbedFieldBuilder>
        {
            new() { Name = "#general", Value = "<#thread-1>", IsInline = false }
        };

        var result = InvokeBuildThreadEmbed(fields, 0);
        Assert.Contains("Page 1", result.Footer.Text);

        var result2 = InvokeBuildThreadEmbed(fields, 3);
        Assert.Contains("Page 4", result2.Footer.Text);
    }

    [Fact]
    public void BuildThreadEmbed_FooterContainsRegards()
    {
        var result = InvokeBuildThreadEmbed([], 0);
        Assert.Contains("Regards, Theodore", result.Footer.Text);
    }

    #endregion

    #region BuildThreadListMessage (Button Logic) Tests

    private static (EmbedBuilder threadEmbed, ComponentBuilder buttonBuilder) InvokeBuildThreadListMessage(
        object instance, EmbedBuilder threadEmbed, int newPageIndex, int totalPages)
    {
        var method = typeof(ThreadListUpdateHelper).GetMethod("BuildThreadListMessage",
            BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.NotNull(method);
        return ((EmbedBuilder, ComponentBuilder))method.Invoke(instance, [threadEmbed, newPageIndex, totalPages])!;
    }

    private static ThreadListUpdateHelper CreateHelperInstance()
    {
        var businessLayer = NSubstitute.Substitute.For<BusinessLayer.IThreadBotBusinessLayer>();
        var discordFormatter = NSubstitute.Substitute.For<DiscordDotNetUtilities.Interfaces.IDiscordFormatter>();
        var logger = NSubstitute.Substitute.For<ILogger<DiscordBot>>();
        return new ThreadListUpdateHelper(businessLayer, discordFormatter, logger);
    }

    [Fact]
    public void BuildThreadListMessage_FirstPageSinglePage_NoButtons()
    {
        var helper = CreateHelperInstance();
        var embed = new EmbedBuilder { Title = "Test" };

        var (_, buttonBuilder) = InvokeBuildThreadListMessage(helper, embed, 0, 1);

        var built = buttonBuilder.Build();
        var buttons = built.Components
            .OfType<ActionRowComponent>()
            .SelectMany(r => r.Components)
            .OfType<ButtonComponent>()
            .ToList();
        Assert.Empty(buttons);
    }

    [Fact]
    public void BuildThreadListMessage_FirstPageMultiplePages_NextButtonOnly()
    {
        var helper = CreateHelperInstance();
        var embed = new EmbedBuilder { Title = "Test" };

        var (_, buttonBuilder) = InvokeBuildThreadListMessage(helper, embed, 0, 3);

        var built = buttonBuilder.Build();
        var buttons = built.Components
            .OfType<ActionRowComponent>()
            .SelectMany(r => r.Components)
            .OfType<ButtonComponent>()
            .ToList();
        Assert.Single(buttons);
        Assert.Equal("Next", buttons[0].Label);
    }

    [Fact]
    public void BuildThreadListMessage_MiddlePage_BothButtons()
    {
        var helper = CreateHelperInstance();
        var embed = new EmbedBuilder { Title = "Test" };

        var (_, buttonBuilder) = InvokeBuildThreadListMessage(helper, embed, 1, 3);

        var built = buttonBuilder.Build();
        var buttons = built.Components
            .OfType<ActionRowComponent>()
            .SelectMany(r => r.Components)
            .OfType<ButtonComponent>()
            .ToList();
        Assert.Equal(2, buttons.Count);
        Assert.Contains(buttons, b => b.Label == "Previous");
        Assert.Contains(buttons, b => b.Label == "Next");
    }

    [Fact]
    public void BuildThreadListMessage_LastPage_PreviousButtonOnly()
    {
        var helper = CreateHelperInstance();
        var embed = new EmbedBuilder { Title = "Test" };

        var (_, buttonBuilder) = InvokeBuildThreadListMessage(helper, embed, 2, 3);

        var built = buttonBuilder.Build();
        var buttons = built.Components
            .OfType<ActionRowComponent>()
            .SelectMany(r => r.Components)
            .OfType<ButtonComponent>()
            .ToList();
        Assert.Single(buttons);
        Assert.Equal("Previous", buttons[0].Label);
    }

    [Fact]
    public void BuildThreadListMessage_ButtonCustomIdsContainPageIndex()
    {
        var helper = CreateHelperInstance();
        var embed = new EmbedBuilder { Title = "Test" };

        var (_, buttonBuilder) = InvokeBuildThreadListMessage(helper, embed, 2, 5);

        var built = buttonBuilder.Build();
        var buttons = built.Components
            .OfType<ActionRowComponent>()
            .SelectMany(r => r.Components)
            .OfType<ButtonComponent>()
            .ToList();

        var prevButton = buttons.First(b => b.Label == "Previous");
        var nextButton = buttons.First(b => b.Label == "Next");
        Assert.Equal("currentIndexPrev_2", prevButton.CustomId);
        Assert.Equal("currentIndexNext_2", nextButton.CustomId);
    }

    #endregion

    #region Page-Splitting Logic Tests

    /// <summary>
    /// Tests the page-splitting logic by simulating what GetPaginatedEmbedFields does
    /// after BuildFieldsForChannelGroup produces the fields. This avoids needing
    /// SocketThreadChannel instances.
    /// </summary>
    private static (List<List<EmbedFieldBuilder>> pages, int totalPages) SimulatePagination(List<EmbedFieldBuilder> allFields)
    {
        var pages = new List<List<EmbedFieldBuilder>>();
        var currentPage = new List<EmbedFieldBuilder>();
        var currentPageCharCount = 0;

        foreach (var field in allFields)
        {
            var fieldCharCount = field.Name.Length + ((string)field.Value).Length;

            if (currentPage.Count > 0 &&
                (currentPage.Count >= MaxFieldsPerPage || currentPageCharCount + fieldCharCount > MaxEmbedContentLength))
            {
                pages.Add(currentPage);
                currentPage = new List<EmbedFieldBuilder>();
                currentPageCharCount = 0;
            }

            currentPage.Add(field);
            currentPageCharCount += fieldCharCount;
        }

        if (currentPage.Count > 0)
        {
            pages.Add(currentPage);
        }

        if (pages.Count == 0)
        {
            pages.Add([]);
        }

        return (pages, pages.Count);
    }

    [Fact]
    public void Pagination_NoFields_ReturnsSingleEmptyPage()
    {
        var (pages, totalPages) = SimulatePagination([]);

        Assert.Equal(1, totalPages);
        Assert.Single(pages);
        Assert.Empty(pages[0]);
    }

    [Fact]
    public void Pagination_FewFields_FitOnOnePage()
    {
        var fields = Enumerable.Range(1, 5)
            .Select(i => new EmbedFieldBuilder { Name = $"#ch{i}", Value = $"<#thread-{i}>", IsInline = false })
            .ToList();

        var (pages, totalPages) = SimulatePagination(fields);

        Assert.Equal(1, totalPages);
        Assert.Equal(5, pages[0].Count);
    }

    [Fact]
    public void Pagination_MoreThan25Fields_SplitsIntoMultiplePages()
    {
        var fields = Enumerable.Range(1, 30)
            .Select(i => new EmbedFieldBuilder { Name = $"#ch{i}", Value = $"<#thread-{i}>", IsInline = false })
            .ToList();

        var (pages, totalPages) = SimulatePagination(fields);

        Assert.Equal(2, totalPages);
        Assert.Equal(25, pages[0].Count);
        Assert.Equal(5, pages[1].Count);
    }

    [Fact]
    public void Pagination_LargeFieldValues_SplitsOnCharacterCount()
    {
        // Create fields with long values that exceed the char limit before hitting 25 fields
        var fields = Enumerable.Range(1, 10)
            .Select(i => new EmbedFieldBuilder
            {
                Name = $"#channel-{i}",
                Value = new string('x', 1000), // Each field uses ~1010 chars
                IsInline = false
            })
            .ToList();

        var (pages, totalPages) = SimulatePagination(fields);

        Assert.True(totalPages > 1, "Should split into multiple pages based on char count");
        foreach (var page in pages)
        {
            var pageCharCount = page.Sum(f => f.Name.Length + ((string)f.Value).Length);
            Assert.True(pageCharCount <= MaxEmbedContentLength + 1100,
                $"Page char count {pageCharCount} significantly exceeds limit");
        }
    }

    [Fact]
    public void Pagination_268Threads_AllFieldsFitWithinLimits()
    {
        // Simulate 268 threads in one channel as split fields
        var threads = Enumerable.Range(1, 268)
            .Select(i => new ThreadChannelPartial($"thread-{i:D3}", $"<#1234567890{i:D3}>", "big-channel", "#big-channel"))
            .ToList();

        var allFields = InvokeBuildFieldsForChannelGroup("big-channel", threads);
        var (pages, totalPages) = SimulatePagination(allFields);

        Assert.True(totalPages >= 1);

        // Verify all pages respect both limits
        foreach (var page in pages)
        {
            Assert.True(page.Count <= MaxFieldsPerPage,
                $"Page has {page.Count} fields, exceeding limit of {MaxFieldsPerPage}");

            var pageCharCount = page.Sum(f => f.Name.Length + ((string)f.Value).Length);
            Assert.True(pageCharCount <= MaxEmbedContentLength,
                $"Page char count {pageCharCount} exceeds limit of {MaxEmbedContentLength}");

            foreach (var field in page)
            {
                Assert.True(((string)field.Value).Length <= MaxEmbedFieldValueLength,
                    $"Field value length {((string)field.Value).Length} exceeds {MaxEmbedFieldValueLength}");
            }
        }

        // Verify all 268 thread mentions are present across all pages
        var allMentions = pages
            .SelectMany(p => p)
            .SelectMany(f => ((string)f.Value).Split('\n'))
            .ToList();
        Assert.Equal(268, allMentions.Count);
    }

    [Fact]
    public void Pagination_MultipleChannels_FieldsFromAllChannelsPresent()
    {
        var fields = new List<EmbedFieldBuilder>();
        for (int ch = 1; ch <= 3; ch++)
        {
            var threads = Enumerable.Range(1, 10)
                .Select(i => new ThreadChannelPartial($"thread-{i}", $"<#mention-ch{ch}-t{i}>", $"channel-{ch}", $"#channel-{ch}"))
                .ToList();
            fields.AddRange(InvokeBuildFieldsForChannelGroup($"channel-{ch}", threads));
        }

        var (pages, totalPages) = SimulatePagination(fields);

        var allValues = string.Join("\n", pages.SelectMany(p => p).Select(f => (string)f.Value));
        for (int ch = 1; ch <= 3; ch++)
        {
            for (int t = 1; t <= 10; t++)
            {
                Assert.Contains($"<#mention-ch{ch}-t{t}>", allValues);
            }
        }
    }

    #endregion
}
