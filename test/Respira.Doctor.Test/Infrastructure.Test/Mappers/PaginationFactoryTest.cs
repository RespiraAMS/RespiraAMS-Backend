using Respira.Doctor.Infrastructure.Mappers;
using Respira.ServiceDefaults.Contracts.Pagination;
using X.PagedList;

namespace Respira.Doctor.Infrastructure.Test.Mappers;

public class PaginationFactoryTest
{
    private readonly PaginationFactory _factory = new();

    private sealed class Item
    {
        public string Name { get; init; } = string.Empty;
    }

    private static List<Item> Items(params string[] names) =>
        names.Select(name => new Item { Name = name }).ToList();

    [Fact]
    public void Create_CarriesTheItemsOverUnchanged()
    {
        var items = Items("first", "second", "third");
        var pagedList = new StaticPagedList<Item>(items, 1, 10, items.Count);

        var pagination = _factory.Create(pagedList);

        var carried = pagination.Items.ToList();
        Assert.Equal(items.Count, carried.Count);
        for (var i = 0; i < items.Count; i++)
        {
            Assert.Same(items[i], carried[i]);
        }
    }

    [Fact]
    public void Create_UsesThePageNumberOfThePagedList()
    {
        var pagedList = new StaticPagedList<Item>(Items("a", "b"), 2, 10, 42);

        var pagination = _factory.Create(pagedList);

        Assert.Equal(2, pagination.Metadata.CurrentPage);
    }

    [Fact]
    public void Create_UsesThePageSizeOfThePagedList()
    {
        var pagedList = new StaticPagedList<Item>(Items("a", "b"), 1, 25, 42);

        var pagination = _factory.Create(pagedList);

        Assert.Equal(25, pagination.Metadata.PageSize);
    }

    [Fact]
    public void Create_UsesTheTotalItemCountOfThePagedList()
    {
        var pagedList = new StaticPagedList<Item>(Items("a", "b"), 1, 10, 42);

        var pagination = _factory.Create(pagedList);

        Assert.Equal(42, pagination.Metadata.TotalItemCount);
    }

    [Fact]
    public void Create_UsesThePageCountOfThePagedList()
    {
        var pagedList = new StaticPagedList<Item>(Items("a", "b"), 1, 10, 42);

        var pagination = _factory.Create(pagedList);

        Assert.Equal(5, pagination.Metadata.PageCount);
        Assert.Equal(pagedList.PageCount, pagination.Metadata.PageCount);
    }

    [Fact]
    public void Create_UsesTheHasNextPageFlagOfThePagedList()
    {
        var pagedList = new StaticPagedList<Item>(Items("a", "b"), 1, 10, 42);

        var pagination = _factory.Create(pagedList);

        Assert.True(pagedList.HasNextPage);
        Assert.Equal(pagedList.HasNextPage, pagination.Metadata.HasNextPage);
    }

    [Fact]
    public void Create_UsesTheHasPreviousPageFlagOfThePagedList()
    {
        var pagedList = new StaticPagedList<Item>(Items("a", "b"), 3, 10, 42);

        var pagination = _factory.Create(pagedList);

        Assert.True(pagedList.HasPreviousPage);
        Assert.Equal(pagedList.HasPreviousPage, pagination.Metadata.HasPreviousPage);
    }

    [Fact]
    public void Create_WithEmptyPagedList_ReturnsEmptyItemsAndZeroedMetadata()
    {
        var pagedList = new StaticPagedList<Item>([], 1, 10, 0);

        var pagination = _factory.Create(pagedList);

        Assert.Empty(pagination.Items);
        Assert.Equal(0, pagination.Metadata.TotalItemCount);
        Assert.Equal(0, pagination.Metadata.PageCount);
        Assert.False(pagination.Metadata.HasNextPage);
        Assert.False(pagination.Metadata.HasPreviousPage);
    }

    [Fact]
    public void Create_WithSinglePage_ReportsNoNextAndNoPreviousPage()
    {
        var items = Items("only");
        var pagedList = new StaticPagedList<Item>(items, 1, 10, 1);

        var pagination = _factory.Create(pagedList);

        Assert.Equal(1, pagination.Metadata.PageCount);
        Assert.False(pagination.Metadata.HasNextPage);
        Assert.False(pagination.Metadata.HasPreviousPage);
    }

    [Fact]
    public void Create_WithMorePages_ReportsHasNextPage()
    {
        var pagedList = new StaticPagedList<Item>(Items("a", "b"), 1, 2, 5);

        var pagination = _factory.Create(pagedList);

        Assert.Equal(3, pagination.Metadata.PageCount);
        Assert.True(pagination.Metadata.HasNextPage);
        Assert.False(pagination.Metadata.HasPreviousPage);
    }

    [Fact]
    public void Create_WithMiddlePage_ReportsHasNextPageAndHasPreviousPage()
    {
        var pagedList = new StaticPagedList<Item>(Items("c", "d"), 2, 2, 5);

        var pagination = _factory.Create(pagedList);

        Assert.Equal(2, pagination.Metadata.CurrentPage);
        Assert.True(pagination.Metadata.HasNextPage);
        Assert.True(pagination.Metadata.HasPreviousPage);
    }

    [Fact]
    public void Create_WithLastPage_ReportsHasPreviousPageAndNoNextPage()
    {
        var pagedList = new StaticPagedList<Item>(Items("e"), 3, 2, 5);

        var pagination = _factory.Create(pagedList);

        Assert.Equal(3, pagination.Metadata.CurrentPage);
        Assert.False(pagination.Metadata.HasNextPage);
        Assert.True(pagination.Metadata.HasPreviousPage);
    }

    [Fact]
    public void Create_ReturnsAPaginationHoldingTheSameItemSequence()
    {
        var items = Items("first", "second");
        var pagedList = new StaticPagedList<Item>(items, 1, 10, 2);

        var pagination = _factory.Create(pagedList);

        Assert.IsType<Pagination<Item>>(pagination);
        Assert.Same(pagedList, pagination.Items);
    }

    [Fact]
    public void Create_WithATotalThatIsNotAMultipleOfThePageSize_KeepsThePageCountOfThePagedList()
    {
        // 7 items with a page size of 3 means 3 pages with a partially filled last page.
        var pagedList = new StaticPagedList<Item>(Items("a", "b", "c"), 1, 3, 7);

        var pagination = _factory.Create(pagedList);

        Assert.Equal(3, pagination.Metadata.PageCount);
        Assert.Equal(pagedList.PageCount, pagination.Metadata.PageCount);
        Assert.Equal(7, pagination.Metadata.TotalItemCount);
        Assert.True(pagination.Metadata.HasNextPage);
        Assert.False(pagination.Metadata.HasPreviousPage);
    }
}
