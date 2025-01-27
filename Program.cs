using Herta.Core.Server;

class Program
{
    static void Main(string[] args)
    {
        var api = new HertaApiService(debug: false);
        api.Run();
    }
}