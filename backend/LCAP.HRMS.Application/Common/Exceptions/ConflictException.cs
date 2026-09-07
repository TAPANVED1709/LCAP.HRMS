namespace LCAP.HRMS.Application.Common.Exceptions;

public sealed class ConflictException(string message) : Exception(message);
