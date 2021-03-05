using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

namespace SimpleMail_Server {
    class Program {
        private TcpListener TCPClient = new TcpListener(IPAddress.Any, 25);
        private bool TCPRunning;
        private Thread listen_Thread;

        private void Write(TcpClient Client, String strMessage) {
            NetworkStream Streamer = Client.GetStream();
            ASCIIEncoding Encode = new ASCIIEncoding();
            Byte[] strBuffer = Encode.GetBytes(strMessage + "\r\n");
            Streamer.Write(strBuffer, 0, strBuffer.Length);
            Streamer.Flush();
        }

        private void HandleIncommingConnections(object Connection) {
            TcpClient Client = (TcpClient)Connection;
            NetworkStream netStream = Client.GetStream();
            StreamReader Reader = new StreamReader(netStream);

            IPEndPoint IncommingIpAddress = Client.Client.RemoteEndPoint as IPEndPoint;
            Console.WriteLine(string.Format("Incomming Connection.. - {0}\n", IncommingIpAddress));

            string MyIP = new WebClient().DownloadString("http://icanhazip.com");
            Console.WriteLine(MyIP);
            Write(Client, "220 "+ MyIP + " SimpleMailc# service ready");

            string Message = String.Empty;
            while (true) { 
                try {
                    Message = Reader.ReadLine();
                }
                catch (Exception ex) {
                    IPEndPoint Address = Client.Client.RemoteEndPoint as IPEndPoint;
                    Console.WriteLine(string.Format("{0} - {1} \n", Address, ex));
                    Reader.Close();
                    Client.Close();
                    return;
                }
                if (Message.Length > 0) {
                    if (Message.StartsWith("HELO")) Write(Client, "250 " + MyIP + ", I am glad to meet you");
                    if (Message.StartsWith("EHLO")) Write(Client, "250 Ok");
                    if (Message.StartsWith("MAIL FROM")) Write(Client, "250 Ok");
                    if (Message.StartsWith("RCPT TO")) Write(Client, "250 Ok");
                    if (Message.StartsWith("RSET")) Write(Client, "250 Ok");
                    if (Message.StartsWith("DATA")) {
                        Write(Client, "354 Send Message Content; end with <CRLF>.<CRLF>");
                        string messageData = "";
                        byte[] buffer = new byte[1024];

                        while (true) {
                            int count = netStream.Read(buffer, 0, buffer.Length);
                            messageData += Encoding.ASCII.GetString(buffer, 0, count);
                            if (count < buffer.Length) break;
                        }

                        MimeKit.MimeMessage message1 = MimeKit.MimeMessage.Load(new MemoryStream(Encoding.UTF8.GetBytes(messageData)));
                        var attachments = message1.Attachments.ToList();

                        foreach (var part in message1.BodyParts) if (part.IsAttachment || !part.ContentType.MediaType.Contains("text")) attachments.Add(part);
                 
                        Console.WriteLine("From: {0}", message1.From[0]);
                        Console.WriteLine("To: {0}", message1.To[0]);
                        Console.WriteLine("Subject: {0}", message1.Subject);
                        Console.WriteLine("Attachments: {0}", attachments.Count);
                        Console.WriteLine("Date: {0}", message1.Date);
                        Console.WriteLine("BodyParts: {0}", message1.BodyParts.Count());
                        Console.WriteLine("Body: {0}", message1.TextBody);

                       Write(Client, "250 OK, message accepted for delivery");
                    }

                    if (Message.StartsWith("QUIT")) {
                        try {
                            Write(Client, "221 Goodbye!");
                        }
                        catch (Exception Excep) { Console.WriteLine("Connection Closed."); }
                        Reader.Close();
                        Client.Close();
                        break;
                    }
                }

            }
        }
        private Exception ValidateHandle() {
            try {
                TCPClient.Start();
                TCPClient.Stop();
                return null;
            } catch (Exception ex) {
                return ex;
            }
        }
        private void Begin() {
            this.listen_Thread = new Thread(new ThreadStart(this.ListenForClients));
            this.TCPRunning = true;
            this.listen_Thread.Start();
        }

        private void ListenForClients() {
            TCPClient.Start();
            while (true) {
                Thread.Sleep(100);
                if (!TCPRunning) return;
                if (TCPClient.Pending()) new Thread(new ParameterizedThreadStart(HandleIncommingConnections)).Start(TCPClient.AcceptTcpClient());
            }
        }

        private static bool Initialize() {
            Program Server = new Program();
            Console.WriteLine(string.Format("Running on port {0}", 25));
            Exception ex = Server.ValidateHandle();
            if (ex != null) { Console.WriteLine(string.Format("FAILED - PORT IN USE {0}", ex.Message)); return false; }
            else Server.Begin();
            return true;
        }

        static void Main(string[] args) {
            Console.BackgroundColor = ConsoleColor.Black;
            Console.Clear();

            if (Initialize()) Console.WriteLine("-------------------------------------------\n");
            else Console.WriteLine("Failed to Initialize\n");            
        }
    }
    
}
