namespace URMS.Application.Contracts.Infrastructure;

public interface ILocalizationService
{
    string GetLocalizedString(string key);
    string GetLocalizedString(string key, params object[] args);
}
