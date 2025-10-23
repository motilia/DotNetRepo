using System;
using System.Collections.Generic;
using System.Linq;

Console.WriteLine("Lab1 - Library Book and Borrower Tracker\n");

Ex1();
Ex2();
var enteredBooks = Ex3();
Ex4();
Ex5(enteredBooks);

void Ex1()
{
    Console.WriteLine("Ex1: Modeling with record and with");
    var books = new List<Book> { new("Clean Code", "Robert C. Martin", 2008) };

    var borrower1 = new Borrower(1, "Moraru Otilia-Gabriela", new List<Book>(books));
    
    var borrower2 = borrower1 with
    {
        BorrowedBooks = borrower1.BorrowedBooks.Append(new Book("The Pragmatic Programmer", "Andrew Hunt", 1999)).ToList()
    };
    
    Console.WriteLine($"Borrower original: {borrower1.Name}, numar carti imprumutate: {borrower1.BorrowedBooks.Count}");
    foreach (var b in borrower1.BorrowedBooks)
        Console.WriteLine($" - {b.Title} by {b.Author} ({b.YearPublished})");

    Console.WriteLine($"Borrower clona cu with: {borrower2.Name}, numar carti imprumutate: {borrower2.BorrowedBooks.Count}");
    foreach (var b in borrower2.BorrowedBooks)
        Console.WriteLine($" - {b.Title} by {b.Author} ({b.YearPublished})");

    Console.WriteLine();
}

void Ex2()
{
    Console.WriteLine("Ex2: Use of init-only properties");
    var librarian = new Librarian
    {
        Name = "Librarian1",
        Email = "librarian1@gmail.com",
        LibrarySection = "historical books"
    };

    Console.WriteLine($"Librarian nou creat: {librarian}");
    Console.WriteLine();
}

List<Book> Ex3()
{
    Console.WriteLine("Ex3: Top-level statements:");
    Console.WriteLine("Introdu titlul cartii (scrie 'stop' pentru a termina).");

    var enteredBooks = new List<Book>();
    string? title;
    while (true)
    {
        Console.Write("\nTitlu (sau 'stop'): ");
        title = Console.ReadLine();
        if (title is null) break;
        if (title.Trim().ToLower() == "stop") break;
        if (string.IsNullOrWhiteSpace(title))
        {
            Console.WriteLine("Titlu gol, incearca din nou.");
            continue;
        }

        Console.Write("Autor (apasă Enter pentru 'Unknown'): ");
        var author = Console.ReadLine();
        if (string.IsNullOrWhiteSpace(author))
            author = "Unknown";

        Console.Write("An publicare (apasă Enter pentru 0): ");
        var yearLine = Console.ReadLine();
        int year = 0;
        if (!string.IsNullOrWhiteSpace(yearLine) && int.TryParse(yearLine.Trim(), out var parsed))
            year = parsed;

        enteredBooks.Add(new Book(title.Trim(), author.Trim(), year));
    }

    Console.WriteLine("\nCarti introduse:");
    if (enteredBooks.Count == 0)
        Console.WriteLine("(nici o carte introdusa)");
    else
        enteredBooks.ForEach(b => Console.WriteLine($"- {b.Title} by {b.Author} ({b.YearPublished})"));

    Console.WriteLine();
    return enteredBooks;
}

void Ex4()
{
    Console.WriteLine("Ex4: Pattern matching");
    var book = new Book("Artificial Intelligence: A Modern Approach", "Russell & Norvig", 2010);
    var borrower = new Borrower(2, "Popa Radu", new List<Book> { book, new Book("English Grammar", "Author X", 2005) });
    var number = 42;

    DisplayInfo(book);
    DisplayInfo(borrower);
    DisplayInfo(number);
    Console.WriteLine();

    void DisplayInfo(object obj)
    {
        switch (obj)
        {
            case Book b:
                Console.WriteLine($"Book: {b.Title}, Year: {b.YearPublished}");
                break;
            case Borrower br:
                Console.WriteLine($"Borrower: {br.Name}, number of borrowed books: {br.BorrowedBooks.Count}");
                break;
            default:
                Console.WriteLine("Unknown type");
                break;
        }
    }
}

void Ex5(List<Book> enteredBooks)
{
    Console.WriteLine("Ex5: Static Lambda Filtering:");
    var books = new List<Book>
    {
        new("Clean Architecture", "Robert C. Martin", 2017),
        new("Introduction to Algorithms", "Cormen et al.", 2009),
        new("C# in Depth", "Jon Skeet", 2019),
        new("Old Book", "Some Author", 2000)
    };

    //Cartile adaugate de la ex3 sunt puse in aceasta lista
    if (enteredBooks != null && enteredBooks.Any())
    {
        foreach (var b in enteredBooks)
            books.Add(b);
    }

    var filtered = books.Where(static b => b.YearPublished > 2010).ToList();

    Console.WriteLine("\nCarti publicate dupa 2010:");
    if (filtered.Count == 0)
        Console.WriteLine("(nici o carte gasita)");
    else
        foreach (var b in filtered)
            Console.WriteLine($"- {b.Title} by {b.Author} ({b.YearPublished})");

    Console.WriteLine();
}



public record Book(string Title, string Author, int YearPublished);

public record Borrower(int Id, string Name, List<Book> BorrowedBooks);

public class Librarian
{
    public required string Name { get; init; }
    public required string Email { get; init; }
    public required string LibrarySection { get; init; }

    public override string ToString() => $"Nume: {Name}   Email: {Email}   Sectiune: {LibrarySection}";
}

