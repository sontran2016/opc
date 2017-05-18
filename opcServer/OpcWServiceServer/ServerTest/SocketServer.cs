using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;

namespace ServerTest
{
    public class StateObject
    {
        // Client  socket.
        public Socket WorkSocket = null;
        // Size of receive buffer.
        public const int BufferSize = 1024;
        // Receive buffer.
        public byte[] Buffer = new byte[BufferSize];
        // Received data string.
        public StringBuilder Sb = new StringBuilder();
    }
    class SocketServer
    {
        // Thread signal.
        public ManualResetEvent AllDone = new ManualResetEvent(false);
        private Socket Listener=null;
        //public bool _isConnected = false;
        private bool _isStop = false;
        string _pathLog = null;

        public SocketServer() { }

        public void StartListening()
        {
            _pathLog = AppDomain.CurrentDomain.BaseDirectory + "MyLog.txt";
            // Data buffer for incoming data.
            byte[] bytes = new Byte[1024];

            // Establish the local endpoint for the socket.
            // The DNS name of the computer
            // running the listener is "host.contoso.com".
            //IPHostEntry ipHostInfo = Dns.GetHostEntry(Dns.GetHostName());
            //IPAddress ipAddress = ipHostInfo.AddressList.SingleOrDefault(x => x.AddressFamily == AddressFamily.InterNetwork);          
            IPAddress ipAddress = IPAddress.Parse("192.168.1.182");
            IPEndPoint localEndPoint = new IPEndPoint(ipAddress, 11001);//11000

            // Create a TCP/IP socket.
            //Socket listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            Listener = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);

            // Bind the socket to the local endpoint and listen for incoming connections.
            try
            {
                Listener.Bind(localEndPoint);
                Listener.Listen(100);

                while (!_isStop)
                {
                    // Set the event to nonsignaled state.
                    AllDone.Reset();

                    // Start an asynchronous socket to listen for connections.
                    //Console.WriteLine("Waiting for a connection...");
                    WriteFile("Waiting for a connection...");
                    Listener.BeginAccept(new AsyncCallback(AcceptCallback), Listener);

                    // Wait until a connection is made before continuing.
                    AllDone.WaitOne();
                }

            }
            catch (Exception e)
            {
                //Console.WriteLine(e.ToString());
                WriteFile(e.ToString());
            }
            //Console.WriteLine("\nPress ENTER to continue...");
            //Console.Read();
        }
        public void Stop()
        {
            try {
                _isStop = true;
                //Listener.Shutdown(SocketShutdown.Both);
                Listener.Close();            
            }
            catch(Exception ex)
            {
                WriteFile(ex.ToString());
            }
        }
        public void WriteFile(string st)
        {
            File.AppendAllText(_pathLog, "\r\n"+DateTime.Now.ToString()+", "+st);
        }
        public void AcceptCallback(IAsyncResult ar)
        {
            // Signal the main thread to continue.
            AllDone.Set();

            // Get the socket that handles the client request.
            Socket listener = (Socket)ar.AsyncState;
            Socket handler = listener.EndAccept(ar);

            // Create the state object.
            StateObject state = new StateObject();
            state.WorkSocket = handler;
            handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
            //_isConnected = true;
        }

        public void ReadCallback(IAsyncResult ar)
        {
            String content = String.Empty;

            // Retrieve the state object and the handler socket
            // from the asynchronous state object.
            StateObject state = (StateObject)ar.AsyncState;
            Socket handler = state.WorkSocket;

            // Read data from the client socket. 
            int bytesRead = handler.EndReceive(ar);

            if (bytesRead > 0)
            {
                // There  might be more data, so store the data received so far.
                state.Sb.Append(Encoding.ASCII.GetString(state.Buffer, 0, bytesRead));

                // Check for end-of-file tag. If it is not there, read 
                // more data.
                content = state.Sb.ToString();
                if (content.IndexOf("<EOF>") > -1)
                {
                    // All the data has been read from the 
                    // client. Display it on the console.
                    //Console.WriteLine("Read {0} bytes from socket. \n Data : {1}", content.Length, content);
                    WriteFile(string.Format("Read {0} bytes from socket. \n Data : {1}", content.Length, content));
                    // Echo the data back to the client.
                    Send(handler, content);
                }
                else
                {
                    // Not all data received. Get more.
                    handler.BeginReceive(state.Buffer, 0, StateObject.BufferSize, 0, new AsyncCallback(ReadCallback), state);
                }
            }
        }

        private void Send(Socket handler, String data)
        {
            // Convert the string data to byte data using ASCII encoding.
            byte[] byteData = Encoding.ASCII.GetBytes(data);

            // Begin sending the data to the remote device.
            handler.BeginSend(byteData, 0, byteData.Length, 0, new AsyncCallback(SendCallback), handler);
        }

        private void SendCallback(IAsyncResult ar)
        {
            try
            {
                // Retrieve the socket from the state object.
                Socket handler = (Socket)ar.AsyncState;

                // Complete sending the data to the remote device.
                int bytesSent = handler.EndSend(ar);
                //Console.WriteLine("Sent {0} bytes to client.", bytesSent);
                WriteFile(string.Format("Sent {0} bytes to client.", bytesSent));

                handler.Shutdown(SocketShutdown.Both);
                handler.Close();

            }
            catch (Exception e)
            {
                //Console.WriteLine(e.ToString());
                WriteFile(e.ToString());
            }
        }
    }
}
