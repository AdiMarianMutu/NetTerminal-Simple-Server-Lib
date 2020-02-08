namespace NetTerminal {
    using System;
    using System.Net;
    using System.Net.Sockets;
    using System.Security.Cryptography;
    using System.Text;

    public class Server {
        private IPAddress ip;
        private int       port;
        private string    psw;

        /// <param name="IP">The IP used for the connection. Leave empty or null to use any IP</param>
        /// <param name="PORT">Port to listen. Leave empty or 0 to use the first available port</param>
        /// <param name="PSW">Password. If this value is not null the server will create a basic authorization access based on this</param>
        public Server(string IP = null, int PORT = 0, string PSW = null) {
            this.IP       = IP;
            this.Port     = PORT;
            this.Password = PSW;
        }
        /// <param name="encodingChar">The encoding which will be used to send/receive the data. Default is ASCII</param>
        /// <param name="IP">The IP used for the connection. Leave empty or null to use any IP</param>
        /// <param name="PORT">Port to listen. Leave empty or 0 to use the first available port</param>
        /// <param name="PSW">Password. If this value is not null the server will use the inbuilt Auth Protocol</param>
        public Server(Encoding encodingChar, string IP = null, int PORT = 0, string PSW = null) {
            this.IP           = IP;
            this.Port         = PORT;
            this.Password     = PSW;
            this.EncodingChar = encodingChar;
        }

        /// <summary>
        /// Data encoding
        /// </summary>
        public Encoding EncodingChar { get; set; } = Encoding.ASCII;
        /// <summary>
        /// <para>If using the inbuilt Auth Protocol and the client does not authenticate into the range of time selected, the data received will not be read</para>
        /// <br>Default is 1 minute</br>
        /// </summary>
        public long AuthTimeoutMilliseconds { get; set; } = 1000 * 60;
        /// <summary>
        /// Server IP
        /// </summary>
        public string IP {
            get { return ip.ToString(); }
            set {
                if (string.IsNullOrEmpty(value))
                    ip = IPAddress.Any;
                else {
                    try {
                        ip = IPAddress.Parse(value);
                    } catch (Exception e) {
                        throw new _Exception.Server.IP("Invalid IPv4 address", e);
                    }
                }
            }
        }
        /// <summary>
        /// Server port
        /// </summary>
        public int Port {
            get { return port; }
            set {
                if (value == 0)
                    port = GetAvailablePort;
                else {
                    if (value >= 1 && value <= 65535)
                        port = value;
                    else
                        throw new _Exception.Server.Port("Port is less than 0 or great than 65535");
                }
            }
        }
        /// <summary>
        /// Authentication password
        /// </summary>
        public string Password {
            get { return psw; }
            set { psw = value == null ? null : Convert.ToBase64String(SHA256.Create().ComputeHash(EncodingChar.GetBytes(value))); }
        }
        /// <summary>
        /// Indicates if the client will be asked to authenticate in order to talk with the server
        /// </summary>
        public bool AuthRequired { get { return Password == null ? false : true; } }
        private int GetAvailablePort {
            get {
                try {
                    using (Socket s = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)) {
                        s.Bind(new IPEndPoint(IPAddress.Loopback, port: 0));
                        return ((IPEndPoint)s.LocalEndPoint).Port;
                    }
                } catch (Exception e) {
                    throw new _Exception.Server.Port("Unable to find an available free port. Try by adding the port manually", e);
                }
            }
        }
    }
}