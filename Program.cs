
using Herta.Core.Server;

public class Program
{
    public static void Main()
    {
        var server = new HertaApiService(debug: true, needAuthentication: true);
        server.Run();
    }
}
