using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Threading;
using System.Windows.Forms;
using WebSocketSharp;
using WebSocketSharp.Server;

namespace WebsockService
{

    public class WSService : WebSocketBehavior
    {
        protected WebSocket clientWs = null;
        protected string clientWSUrl;
        public WSService(string clientWSUrl)
        {
            this.clientWSUrl = clientWSUrl;
        }

        protected override void OnMessage(MessageEventArgs e)
        {
            if (clientWs != null && clientWs.IsAlive)
            {
                if (e.IsBinary)
                {
                    clientWs.SendAsync(e.RawData, x => { });
                }
                else if (e.IsText)
                {
                    clientWs.SendAsync(e.Data, x => { });
                }
            }
        }

        protected override void OnOpen()
        {
            clientWs = new WebSocket(clientWSUrl);

            clientWs.OnMessage += (sender, e) =>
            {
                if (e.IsBinary)
                {
                    SendAsync(e.RawData, x => { });
                }
                else if (e.IsText)
                {
                    SendAsync(e.Data, x => { });
                }
            };
            bool opened = false;

            clientWs.OnOpen += (sender, e) =>
            {
                opened = true;
            };

            clientWs.OnClose += (sender, e) =>
            {
                Sessions.CloseSession(ID);
            };

            clientWs.OnError += (sender, e) =>
            {
                Error(e.Message, e.Exception);
            };

            clientWs.ConnectAsync();

            Thread.Sleep(1000);

            if (!opened)
            {
                clientWs.Close();
                Sessions.CloseSession(ID);
            }
        }

        protected override void OnClose(CloseEventArgs e)
        {
            clientWs.Close(e.Code, e.Reason);
        }

        protected override void OnError(WebSocketSharp.ErrorEventArgs e)
        {
            
        }
    }

    public class Config
    {
        public ConfigSert X509Certificate2 { get; set; }
        public int wsPort { get; set; }
        public ConfigWs[] tunnelClients { get; set; }
    }

    public class ConfigWs
    {
        public string wsUrl { get; set; }
        public string wsPath { get; set; }
    }

    public class ConfigSert
    {
        public string path { get; set; }
        public string password { get; set; }
    }

    public class Program
    {
        private static Config readConfig(string path)
        {
            var json = File.ReadAllText(path, System.Text.Encoding.UTF8);
            var config = JsonSerializer.Deserialize<Config>(json);
            return config;
        }

        public static void Main(string[] args)
        {
            Config cfg = null;
            try
            {
                cfg = readConfig("config.json");
            }catch(Exception e)
            {
                MessageBox.Show("Ошибка загрузки config.json: " + e.Message + " (" + e.ToString() + ")");
                return;
            }

            try
            {

                var wssv = new WebSocketServer(cfg.wsPort, true);
                wssv.SslConfiguration.EnabledSslProtocols = SslProtocols.Tls12 | SslProtocols.Tls11 | SslProtocols.Tls | SslProtocols.Ssl2 | SslProtocols.Ssl3;
                wssv.SslConfiguration.ServerCertificate = new X509Certificate2(cfg.X509Certificate2.path, cfg.X509Certificate2.password); //"ospanel.pfx", "AF90673611D8DD9B"
                wssv.Log.File = DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss") + ".txt";
                wssv.Log.Level = LogLevel.Error;

                Console.WriteLine("Adding clients...");
                foreach (var client in cfg.tunnelClients)
                {
                    wssv.AddWebSocketService(client.wsPath, () => new WSService(client.wsUrl)); //"/audio" ws://192.168.0.53:81/
                    Console.WriteLine("Added client: " + client.wsUrl + " => " + client.wsPath);
                }
                

                wssv.Start();
                Console.WriteLine("WebSocket server started");
                Console.WriteLine("Press Q to stop");
                ConsoleKeyInfo key;
                while ((key = Console.ReadKey()).Key != ConsoleKey.Q) ;
                wssv.Stop();
            }catch(Exception e)
            {
                MessageBox.Show("Ошибка: " + e.Message + " (" + e.ToString() + ")");
                return;
            }
        }
    }
}