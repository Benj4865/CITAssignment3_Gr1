using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;



public class Server
{
    private readonly int _port;

    public Server(int port)
    {
        _port = port;

    }
    public void Run()
    {

        var server = new TcpListener(IPAddress.Loopback, _port);
        server.Start();

        Console.WriteLine($"Server started on port {_port}");

        while (true)
        {
            var client = server.AcceptTcpClient();
            Console.WriteLine("Client Connected");

            Task.Run(() => HandleClient(client));

        }
    }

    private void HandleClient(TcpClient client)
    {

        var stream = client.GetStream();
        string msg = ReadFromStream(stream);

        Console.WriteLine("Message from client: " + msg);
        try
        {
            var json = FromJson<string>(msg);

            var errorlist = new List<string>();

            if (json.Method == null)
            {
                errorlist.Add("missing method");
            }
            else
            {
                string[] validMethods = { "create", "delete", "read", "update", "echo" };

                if (!validMethods.Contains(json.Method))
                {
                    errorlist.Add("illegal method");
                }
            }

            if (json.Method == "echo")
            {
                var response = new Response
                {
                    Status = "1 Ok",
                    Body = json.Body
                };
                var json_resp = ToJson(response);
                WriteToStream(stream, json_resp);
                return;
            }

            if (json.Path == null)
            {
                errorlist.Add("missing resource");
            }

            if (json.Date == null)
            {
                errorlist.Add("missing date");
            }
            else
            {
                if (!int.TryParse(json.Date, out var n))
                {
                    errorlist.Add("illegal date");
                }


            }

            if (json.Body != null)
            {
                try
                {
                    var json_body = FromJson<string>(json.Body);
                }
                catch
                {
                    errorlist.Add("illegal body");
                }
            }



            if (errorlist.Count > 0)
            {
                var response = new Response
                {
                    Status = "4 " + string.Join(", ", errorlist)
                };
                var json_resp = ToJson(response);
                WriteToStream(stream, json_resp);
                return;
            }
        }
        catch
        {

            var response = new Response
            {
                Status = "4 Bad Request (Not Json)"
            };
            var json_resp = ToJson(response);
            WriteToStream(stream, json_resp);

        }
    }

    private string ReadFromStream(NetworkStream stream)
    {
        var buffer = new byte[1024];
        var readCount = stream.Read(buffer);
        return Encoding.UTF8.GetString(buffer, 0, readCount);
    }

    private void WriteToStream(NetworkStream stream, string msg)
    {
        var buffer = Encoding.UTF8.GetBytes(msg);
        stream.Write(buffer);
    }

    public static string ToJson(Response response)
    {
        return JsonSerializer.Serialize(response, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

    public static Request? FromJson<T>(string element)
    {
        return JsonSerializer.Deserialize<Request>(element, new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });
    }

}
