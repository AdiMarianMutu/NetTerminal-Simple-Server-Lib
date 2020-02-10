using System;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Security.Cryptography;
using System.Linq;

namespace NetTerminal {
    public partial class Connection {
        private Server        server;
        private TcpListener   tcpListener;
        private TcpClient     tcpClient;
        private NetworkStream netStream;
        private StreamReader  netSReader;
        private StreamWriter  netSWriter;
        private IO            io;
        private Stopwatch     authTimeout;

        public Connection(Server server) {
            this.server      = server;
            this.tcpListener = new TcpListener(IPAddress.Parse(server.IP), server.Port);
            this.io          = new IO(this);
        }

        /// <summary>
        /// Server connection enum status
        /// </summary>
        public enum StatusFlag {
            NotActive = 0,
            Active    = 1,
        }
        /// <summary>
        /// Retrieves the server connection status
        /// </summary>
        public StatusFlag Status { get; private set; }


        private enum ClientStatusFlag {
            UnknownOrDisconnected = 0,
            Connected             = 1,
            AuthNoPassword        = 2,
            AuthPassword          = 3,
            AuthWrongPassword     = 4,
            AuthTimeout           = 5
        }
        private ClientStatusFlag ClientStatus { get; set; }
        private IPAddress ClientIP { get; set; }
        private int ClientPort { get; set; }
        private bool ClientIsAuth { get { return (ClientStatus == ClientStatusFlag.AuthNoPassword || ClientStatus == ClientStatusFlag.AuthPassword) ? true : false; } }
        private bool ClientIsConnected {
            get {
                return (
                     ClientStatus == ClientStatusFlag.Connected
                  || ClientStatus == ClientStatusFlag.AuthNoPassword
                  || ClientStatus == ClientStatusFlag.AuthPassword
                  || ClientStatus == ClientStatusFlag.AuthWrongPassword
                  || ClientStatus == ClientStatusFlag.AuthTimeout)
                  ? true : false;
            }
        }
        /// <summary>
        /// <para>Time in milliseconds to check the client connection status</para>
        /// <br>Default is 1 ms</br>
        /// </summary>
        public int ClientCheckStatusMilliseconds { get; set; } = 1;

        /// <summary>
        /// Starts the server connection
        /// </summary>
        public void Start() {
            try {
                if (Status == StatusFlag.NotActive) {
                    Status = StatusFlag.Active;

                    tcpListener = new TcpListener(IPAddress.Parse(server.IP), server.Port);
                    tcpListener.Start();

                    ClientWaitConnectionAsync();
                }
            } catch (Exception e) {
                throw new _Exception.Session.StartAsync("Failed to start the session!", e);
            }
        }
        /// <summary>
        /// Ends the server connection
        /// </summary>
        public void End() {
            Status = StatusFlag.NotActive;

            try {
                netSWriter.Dispose();
                netSReader.Dispose();
                netStream.Dispose();
                tcpClient.Dispose();
                tcpListener.Stop();
            } catch (Exception e) {
                throw new _Exception.Session.EndAsync("Failed to end the session!", e);
            }
        }
        /// <summary>
        /// Asynchronously starts the server connection
        /// </summary>
        public async Task StartAsync() {
            try {
                if (Status == StatusFlag.NotActive) {
                    Status = StatusFlag.Active;

                    tcpListener = new TcpListener(IPAddress.Parse(server.IP), server.Port);
                    tcpListener.Start();

                    await ClientWaitConnectionAsync();
                }
            } catch (Exception e) {
                throw new _Exception.Session.StartAsync("Failed to start the session!", e);
            }
        }
        /// <summary>
        /// Asynchronously ends the server connection
        /// </summary>
        public async Task EndAsync() {
            Status = StatusFlag.NotActive;

            try {
                await netSWriter.DisposeAsync();
                netSReader.Dispose();
                await netStream.DisposeAsync();
                tcpClient.Dispose();
                tcpListener.Stop();
            } catch (Exception e) {
                throw new _Exception.Session.EndAsync("Failed to end the session!", e);
            }
        }
        private async Task ClientWaitConnectionAsync() {
            tcpClient  = await tcpListener.AcceptTcpClientAsync();
            netStream  = tcpClient.GetStream();
            netSReader = new StreamReader(netStream, server.EncodingChar);
            netSWriter = new StreamWriter(netStream, server.EncodingChar) { AutoFlush = true };

            await ClientConnectedCallbackAsync();
        }
        private async Task ClientConnectedCallbackAsync() {
            try {
                ClientStatus = ClientStatusFlag.Connected;
                ClientIP     = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Address;
                ClientPort   = ((IPEndPoint)tcpClient.Client.RemoteEndPoint).Port;

                if (!server.AuthRequired)
                    ClientStatus = ClientStatusFlag.AuthNoPassword;

                await ClientCheckStatusCallbackAsync();
            } catch (Exception e) {
                throw new _Exception.Client.ConnectedCallback("Failed to get the client IP/Port!", e);
            }
        }
        private async Task ClientCheckStatusCallbackAsync() {
            try {
                while (await _clientConnected())
                    await Task.Delay(ClientCheckStatusMilliseconds);

                ClientDisconnectedCallbackAsync();
            } catch (Exception e) {
                throw new _Exception.Client.CheckStatusCallbackAsync("Failed to check the connection status of the client!", e);
            }
        }
        private async Task<bool> _clientConnected() {
            try {
                if (!((tcpClient.Client.Poll(1000, SelectMode.SelectRead) && (tcpClient.Client.Available == 0)) || !tcpClient.Client.Connected) == false)
                    return false;
                
                return true;
            } catch {
                return false;
            }
        }
        private async Task ClientAuthHandlerAsync(byte[] authMessage = null) {
            if (ClientStatus == ClientStatusFlag.UnknownOrDisconnected)
                return;

            if (authTimeout == null)
                authTimeout = new Stopwatch();

            if (authMessage != null)
                io._WriteBytes(authMessage, true);

            if (!authTimeout.IsRunning)
                authTimeout.Start();

            byte[] sPsw = Convert.FromBase64String(server.Password);

            while (authTimeout.ElapsedMilliseconds < server.AuthTimeoutMilliseconds) {
                byte[] clPsw = await io._AuthReadPswAsync(64);

                if (authTimeout.ElapsedMilliseconds > server.AuthTimeoutMilliseconds)
                    break;

                // Wrong password
                // The new byte[0] is used to avoid to return a null type if io._AuthReadPsw returns null
                if (sPsw.SequenceEqual(SHA256.Create().ComputeHash(clPsw == null ? new byte[0] : clPsw)) == false) {
                    // If returned new byte[0] (Length will be 0), the client is with the status UnknownOrDisconnected
                    if (clPsw.Length > 0)
                        ClientStatus = ClientStatusFlag.AuthWrongPassword;

                    return;
                }

                // When this point is reached => correct password
                authTimeout.Reset();
                ClientStatus = ClientStatusFlag.AuthPassword;

                return;
            }

            // Auth timeout
            authTimeout.Reset();

            if (ClientStatus != ClientStatusFlag.UnknownOrDisconnected)
                ClientStatus = ClientStatusFlag.AuthTimeout;
        }
        private async Task ClientDisconnectedCallbackAsync() {
            try {
                ClientStatus = ClientStatusFlag.UnknownOrDisconnected;
                ClientIP     = null;
                ClientPort   = 0;

                if (authTimeout != null)
                    authTimeout.Reset();

                netSWriter.Dispose();
                netSReader.Dispose();
                netStream.Dispose();
                tcpClient.Dispose();

                if (Status == StatusFlag.Active)
                    await ClientWaitConnectionAsync();
            } catch (Exception e) {
                throw new _Exception.Client.DisconnectedCallbackAsync("An error occured after the client disconnected!", e);
            }
        }
    }
}
