
using Herta.Core.Server;

public class Program
{
    public static void Main()
    {
        var server = new HertaApiServer(debug: true, needAuthentication: true);
        server.Run();
    }
}
