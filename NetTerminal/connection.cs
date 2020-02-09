namespace NetTerminal {
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.IO;
    using System.Threading.Tasks;
    using System.Diagnostics;
    using System.Security.Cryptography;
    using System.Linq;

    public class Connection {
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

        public class Client {
            private Connection connection;

            /// <summary>
            /// The client which will connect to the server
            /// </summary>
            public Client(Connection connection) {
                this.connection = connection;
            }

            #region [PROPERTIES]
            /// <summary>
            /// Client enum status
            /// </summary>
            public enum StatusFlag {
                UnknownOrDisconnected = 0,
                Connected             = 1,
                AuthNoPassword        = 2,
                AuthPassword          = 3,
                AuthWrongPassword     = 4,
                AuthTimeout           = 5
            }
            /// <summary>
            /// Retrieves the actual client status
            /// </summary>
            public StatusFlag Status { get { return (StatusFlag)(int)connection.ClientStatus; } }
            /// <summary>
            /// Returns the client IP
            /// </summary>
            public string IP { get { return connection.ClientIP.ToString(); } }
            /// <summary>
            /// Returns the client port
            /// </summary>
            public int Port { get { return connection.ClientPort; } }
            /// <summary>
            /// Returns true if the client is authenticated
            /// </summary>
            public bool IsAuth { get { return connection.ClientIsAuth; } }
            /// <summary>
            /// <para>Returns true if the Status.Flag is equal to any of the below values:</para>
            /// <br>Connected</br>
            /// <br>AuthNoPassword</br>
            /// <br>AuthPassword</br>
            /// <br>AuthWrongPassword</br>
            /// <br>AuthTimeout</br>
            /// </summary>
            public bool Connected { get { return connection.ClientIsConnected; } }
            #endregion
            /// <summary>
            /// Starts the inbuilt Auth Protocol and returns true if the client successfully passed the auth phase
            /// </summary>
            public bool AskAuth() {
                if (Status == StatusFlag.Connected || Status == StatusFlag.AuthWrongPassword || Status == StatusFlag.AuthTimeout) {
                    try { connection.ClientAuthHandlerAsync().Wait(); } catch { return false; }
                } else if (Status == StatusFlag.AuthNoPassword)
                    throw new _Exception.Client.AuthBuiltinNotActive("You cannot call this method because the inbuilt Auth Protocol is not enabled\r\n\r\n : > To use the inbuilt Auth Protocol initialize the server obj with a non <null> value password");

                return IsAuth;
            }
            /// <summary>
            /// Starts the inbuilt Auth Protocol and returns true if the client successfully passed the auth phase
            /// </summary>
            /// <param name="sendAuthMessage">Initialize and start the inbuilt Auth Protocol and sends a string to the client</param>
            public bool AskAuth(string sendAuthMessage) {
                if (Status == StatusFlag.Connected || Status == StatusFlag.AuthWrongPassword || Status == StatusFlag.AuthTimeout) {
                    try { connection.ClientAuthHandlerAsync(connection.server.EncodingChar.GetBytes(sendAuthMessage)).Wait(); } catch { return false; }
                } else if (Status == StatusFlag.AuthNoPassword)
                    throw new _Exception.Client.AuthBuiltinNotActive("You cannot call this method because the inbuilt Auth Protocol is not enabled\r\n\r\n : > To use the inbuilt Auth Protocol initialize the server obj with a non <null> value password");

                return IsAuth;
            }
            /// <summary>
            /// Starts the inbuilt Auth Protocol and returns true if the client successfully passed the auth phase
            /// </summary>
            /// <param name="sendAuthMessage">Initialize and start the inbuilt Auth Protocol and sends an array of bytes to the client</param>
            public bool AskAuth(byte[] sendAuthMessage) {
                if (Status == StatusFlag.Connected || Status == StatusFlag.AuthWrongPassword || Status == StatusFlag.AuthTimeout) {
                    try { connection.ClientAuthHandlerAsync(sendAuthMessage).Wait(); } catch { return false; }
                } else if (Status == StatusFlag.AuthNoPassword)
                    throw new _Exception.Client.AuthBuiltinNotActive("You cannot call this method because the inbuilt Auth Protocol is not enabled\r\n\r\n : > To use the inbuilt Auth Protocol initialize the server obj with a non <null> value password");

                return IsAuth;
            }
            /// <summary>
            /// Closes the connection with the client
            /// </summary>
            public void Disconnect() {
                connection.ClientDisconnectedCallbackAsync();
            }

            #region [IO]
            /// <summary>
            /// Reads the raw bytes received from the client
            /// </summary>
            /// <param name="bufferSize">Default 2048 bytes</param>
            public byte[] ReadBytes(int bufferSize = 2048) { return connection.io._ReadBytes(bufferSize); }
            /// <summary>
            /// Reads the raw bytes received from the client (asynchronously)
            /// </summary>
            /// <param name="bufferSize">Default 2048 bytes</param>
            public async Task<byte[]> ReadBytesAsync(int bufferSize = 2048) { return await connection.io._ReadBytesAsync(bufferSize); }
            /// <summary>
            /// Writes the buffer to the client
            /// </summary>
            /// <param name="buffer">Buffer to write</param>
            public void WriteBytes(byte[] buffer) { connection.io._WriteBytes(buffer); }
            /// <summary>
            /// Writes the buffer to the client (asynchronously)
            /// </summary>
            /// <param name="buffer">Buffer to write</param>
            public async Task WriteBytesAsync(byte[] buffer) { await connection.io._WriteBytesAsync(buffer); }
            /// <summary>
            /// Will read the next line (set of characters) sent by the client
            /// </summary>
            public string ReadLine() { return connection.io._ReadLine(); }
            /// <summary>
            /// Will read the next line (set of characters) sent by the client (asynchronously)
            /// </summary>
            public async Task<string> ReadLineAsync() { return await connection.io._ReadLineAsync(); }
            /// <summary>
            /// Writes on the same line
            /// </summary>
            /// <param name="data">String to write</param>
            public void Write(string data) { connection.io._Write(data); }
            /// <summary>
            /// Writes on the same line (asynchronously)
            /// </summary>
            /// <param name="data">String to write</param>
            public async Task WriteAsync(string data) { await connection.io._WriteAsync(data); }
            /// <summary>
            /// Writes on the same line and will append a new line at the end
            /// </summary>
            /// <param name="data">String to write</param>
            public void WriteLine(string data) { connection.io._WriteLine(data); }
            /// <summary>
            /// Writes on the same line and will append a new line at the end (asynchronously)
            /// </summary>
            /// <param name="data">String to write</param>
            public async Task WriteLineAsync(string data) { await connection.io._WriteLineAsync(data); }
            #endregion
        }

        private class IO {
            Connection connection;

            public IO(Connection connection) {
                this.connection = connection;
            }

            public async Task<byte[]> _AuthReadPswAsync(int bufferSize) {
                if (connection.ClientStatus == ClientStatusFlag.UnknownOrDisconnected)
                    return null;

                try {
                    byte[] data = new byte[bufferSize];

                    using (MemoryStream ms = new MemoryStream()) {
                        int numBytesRead;

                        while (!connection.netStream.DataAvailable && connection.ClientStatus != ClientStatusFlag.UnknownOrDisconnected) {
                            if (connection.authTimeout.ElapsedMilliseconds > connection.server.AuthTimeoutMilliseconds)
                                return null;

                            await Task.Delay(100);
                        }

                        while (connection.netStream.DataAvailable && connection.ClientStatus != ClientStatusFlag.UnknownOrDisconnected) {
                            if (connection.authTimeout.ElapsedMilliseconds > connection.server.AuthTimeoutMilliseconds)
                                return null;

                            numBytesRead = connection.netStream.Read(data, 0, data.Length);
                            ms.Write(data, 0, numBytesRead);
                        }

                        return ms.ToArray();
                    }
                } catch {
                    return null;
                }
            }
            public byte[] _ReadBytes(int bufferSize) {
                if (connection.server.AuthRequired && connection.ClientStatus == ClientStatusFlag.Connected)
                    throw new _Exception.Client.NotAuthenticated("The client is not authenticated!");

                try {
                    if (connection.ClientIsConnected && connection.Status == StatusFlag.Active) {
                        byte[] data = new byte[bufferSize];

                        using (MemoryStream ms = new MemoryStream()) {
                            int numBytesRead;

                            while (!connection.netStream.DataAvailable && connection.ClientStatus != ClientStatusFlag.UnknownOrDisconnected)
                                Task.Delay(100).Wait();

                            while (connection.netStream.DataAvailable && connection.ClientStatus != ClientStatusFlag.UnknownOrDisconnected) {
                                numBytesRead = connection.netStream.Read(data, 0, data.Length);
                                ms.Write(data, 0, numBytesRead);
                            }

                            return ms.ToArray();
                        }
                    } else
                        return null;
                } catch {
                    return null;
                }
            }
            public async Task<byte[]> _ReadBytesAsync(int bufferSize) {
                if (connection.server.AuthRequired && connection.ClientStatus == ClientStatusFlag.Connected)
                    throw new _Exception.Client.NotAuthenticated("The client is not authenticated!");

                try {
                    if (connection.ClientIsConnected && connection.Status == StatusFlag.Active) {
                        byte[] data = new byte[bufferSize];

                        using (MemoryStream ms = new MemoryStream()) {
                            int numBytesRead;

                            while (!connection.netStream.DataAvailable && connection.ClientStatus != ClientStatusFlag.UnknownOrDisconnected)
                                await Task.Delay(100);

                            while (connection.netStream.DataAvailable && connection.ClientStatus != ClientStatusFlag.UnknownOrDisconnected) {
                                numBytesRead = await connection.netStream.ReadAsync(data, 0, data.Length);
                                ms.Write(data, 0, numBytesRead);
                            }

                            return ms.ToArray();
                        }
                    } else
                        return null;
                } catch {
                    return null;
                }
            }
            public void _WriteBytes(byte[] buffer, bool authMessageMode = false) {
                if (connection.server.AuthRequired && authMessageMode == false && connection.ClientStatus == ClientStatusFlag.Connected)
                    throw new _Exception.Client.NotAuthenticated("The client is not authenticated!");

                try {
                    if (authMessageMode)
                        connection.netStream.Write(buffer, 0, buffer.Length);
                    else if (connection.ClientIsConnected && connection.Status == StatusFlag.Active)
                        connection.netStream.Write(buffer, 0, buffer.Length);
                } catch {

                }
            }
            public async Task _WriteBytesAsync(byte[] buffer) {
                if (connection.server.AuthRequired && connection.ClientStatus == ClientStatusFlag.Connected)
                    throw new _Exception.Client.NotAuthenticated("The client is not authenticated!");

                try {
                    if (connection.ClientIsConnected && connection.Status == StatusFlag.Active)
                        await connection.netStream.WriteAsync(buffer, 0, buffer.Length);
                } catch {

                }
            }
            public string _ReadLine() {
                if (connection.server.AuthRequired && connection.ClientStatus == ClientStatusFlag.Connected)
                    throw new _Exception.Client.NotAuthenticated("The client is not authenticated!");

                if (connection.ClientIsConnected && connection.Status == StatusFlag.Active) {
                    try {
                        return connection.netSReader.ReadLine();
                    } catch {
                        return null;
                    }
                }

                return null;
            }
            public async Task<string> _ReadLineAsync() {
                if (connection.server.AuthRequired && connection.ClientStatus == ClientStatusFlag.Connected)
                    throw new _Exception.Client.NotAuthenticated("The client is not authenticated!");

                if (connection.ClientIsConnected && connection.Status == StatusFlag.Active) {
                    try {
                        return await connection.netSReader.ReadLineAsync();
                    } catch {
                        return null;
                    }
                }

                return null;
            }
            public void _Write(string d) {
                if (connection.server.AuthRequired && connection.ClientStatus == ClientStatusFlag.Connected)
                    throw new _Exception.Client.NotAuthenticated("The client is not authenticated!");

                if (connection.ClientIsConnected && connection.Status == StatusFlag.Active) {
                    try {
                        connection.netSWriter.Write(d);
                    } catch { }
                }
            }
            public async Task _WriteAsync(string d) {
                if (connection.server.AuthRequired && connection.ClientStatus == ClientStatusFlag.Connected)
                    throw new _Exception.Client.NotAuthenticated("The client is not authenticated!");

                if (connection.ClientIsConnected && connection.Status == StatusFlag.Active) {
                    try {
                        await connection.netSWriter.WriteAsync(d);
                    } catch { }
                }
            }
            public void _WriteLine(string d) {
                if (connection.server.AuthRequired && connection.ClientStatus == ClientStatusFlag.Connected)
                    throw new _Exception.Client.NotAuthenticated("The client is not authenticated!");

                if (connection.ClientIsConnected && connection.Status == StatusFlag.Active) {
                    try {
                        connection.netSWriter.WriteLine(d);
                    } catch { }
                }
            }
            public async Task _WriteLineAsync(string d) {
                if (connection.server.AuthRequired && connection.ClientStatus == ClientStatusFlag.Connected)
                    throw new _Exception.Client.NotAuthenticated("The client is not authenticated!");

                if (connection.ClientIsConnected && connection.Status == StatusFlag.Active) {
                    try {
                        await connection.netSWriter.WriteLineAsync(d);
                    } catch { }
                }
            }
        }
    }
}
