using System.Net.Http;
using System.Threading.Tasks;

public static class CsvDownloader
{
    private static readonly HttpClient client = new();

    public static async Task<string> Download ( string url )
    {
        return await client.GetStringAsync(url);
    }
}