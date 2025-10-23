namespace Lab3.Common.Exceptions;

public abstract class DomainException(string message) : Exception(message);

public sealed class NotFoundException(string message) : DomainException(message);

public sealed class BadRequestDomainException(string message) : DomainException(message);