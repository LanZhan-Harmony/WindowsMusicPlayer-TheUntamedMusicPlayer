namespace The_Untamed_Music_Player.Helpers;

public static class NetworkHelper
{
    public static async Task<bool> IsInternetAvailableAsync()
    {
        try
        {
            using var client = new HttpClient();
            client.Timeout = TimeSpan.FromSeconds(5);
            var response = await client.GetAsync("https://www.baidu.com");
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }
}
