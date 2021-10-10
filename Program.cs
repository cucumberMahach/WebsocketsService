using System;
using System.Collections;
using System.Collections.Generic;
using System.Net;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
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

        protected override void OnError(ErrorEventArgs e)
        {
            
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var wssv = new WebSocketServer(8181, true);
            wssv.SslConfiguration.ServerCertificate = new X509Certificate2("ospanel.pfx", "AF90673611D8DD9B");
            wssv.Log.File = DateTime.Now.ToString("dd-MM-yyyy HH-mm-ss") + ".txt";
            wssv.Log.Level = LogLevel.Debug;

            wssv.AddWebSocketService("/audio", () => new WSService("ws://192.168.0.53:81/"));

            wssv.Start();
            Console.WriteLine("WebSocketServer started");
            Console.WriteLine("Press Q to stop");
            ConsoleKeyInfo key;
            while ((key = Console.ReadKey()).Key != ConsoleKey.Q) ;
            wssv.Stop();
        }
    }
}