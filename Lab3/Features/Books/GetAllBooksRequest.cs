namespace Lab3.Features.Books;

public record GetAllBooksRequest(int Page = 1, int PageSize = 10, string? Author = null, string? SortBy = null, string? SortDir = null);