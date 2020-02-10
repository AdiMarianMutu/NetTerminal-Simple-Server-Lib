using System.Threading.Tasks;

namespace NetTerminal {
    public partial class Connection {
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
    }
}