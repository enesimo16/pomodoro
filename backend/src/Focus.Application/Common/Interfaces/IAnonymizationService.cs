namespace Focus.Application.Common.Interfaces;

public interface IAnonymizationService
{
    string AnonymizeAndGeneralize(string text, string? userDisplayName = null);
}
