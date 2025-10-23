namespace Lab3.Features.Books;

public record UpdateBookRequest(int Id, string Title, string Author, int Year);