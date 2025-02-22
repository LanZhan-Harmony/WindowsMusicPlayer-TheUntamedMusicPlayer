using System.Collections.Specialized;

namespace The_Untamed_Music_Player.Contracts.Services;
public interface IAppNotificationService
{
    void Initialize();

    bool Show(string payload);

    NameValueCollection ParseArguments(string arguments);

    void Unregister();
}