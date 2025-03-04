
using Herta.Core.Server;

var server = new HertaApiServer(debug: true, IgnoreConfigureWarning: true);

server.Run();
