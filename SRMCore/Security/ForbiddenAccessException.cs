namespace SRMCore.Security;

public class ForbiddenAccessException(string message) : Exception(message)
{
}
