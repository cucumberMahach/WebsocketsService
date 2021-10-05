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
        protected Thread thread = null;
        public WSService()
        {

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
                    if (e.Data == "stop")
                    {
                        Console.WriteLine("Stop");
                    }
                    clientWs.SendAsync(e.Data, x => { });
                }
            }
        }

        protected override void OnOpen()
        {
            thread = new Thread(() =>
            {
                clientWs = new WebSocket("ws://192.168.0.53:81/");

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

                clientWs.OnOpen += (sender, e) =>
                {

                };

                clientWs.OnClose += (sender, e) =>
                {
                    Sessions.CloseSession(ID);
                };

                clientWs.OnError += (sender, e) =>
                {
                    Error(e.Message, e.Exception);
                };

                clientWs.Connect();
            });
            thread.Start();
        }

        protected override void OnClose(CloseEventArgs e)
        {
            clientWs.Close(e.Code, e.Reason);
        }

        protected override void OnError(ErrorEventArgs e)
        {
            Console.WriteLine("Error: (" + e.Exception + ") " + e.Message);
        }
    }

    public class Program
    {
        public static void Main(string[] args)
        {
            var wssv = new WebSocketServer(8181, true);
            //wssv.SslConfiguration.ServerCertificate = new X509Certificate2("x509-cert.pfx", "yj5t1u068nyrw1g8yk10s165a1v9");
            wssv.SslConfiguration.ServerCertificate = new X509Certificate2("ospanel.pfx", "AF90673611D8DD9B");
            wssv.AddWebSocketService("/audio", () => new WSService());
            wssv.Start();
            Console.WriteLine("WS started, press any key to stop...");
            Console.ReadKey(true);
            wssv.Stop();
        }
    }
}