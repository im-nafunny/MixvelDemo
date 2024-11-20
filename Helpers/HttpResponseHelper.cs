namespace RouteDemo.Helpers
{
    internal static class HttpResponseHelper
    {
        public static async Task<T?> ReadFromHttpResponse<T>(HttpResponseMessage response)
        {
            var text = await response.Content.ReadAsStringAsync();
            return Newtonsoft.Json.JsonConvert.DeserializeObject<T>(text);
        }
    }
}
