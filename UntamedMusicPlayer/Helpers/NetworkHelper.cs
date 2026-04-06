namespace UntamedMusicPlayer.Helpers;

public static class NetworkHelper
{
    public static async Task<bool> IsInternetAvailableAsync()
    {
        for (var i = 0; i < 3; i++)
        {
            try
            {
                using var client = new HttpClient();
                client.Timeout = TimeSpan.FromSeconds(5);
                var response = await client.GetAsync("https://music.163.com");
                if (response.IsSuccessStatusCode)
                {
                    return true;
                }
            }
            catch { }
            if (i < 2)
            {
                await Task.Delay(300);
            }
        }
        return false;
    }
}
