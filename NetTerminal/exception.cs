namespace NetTerminal {
    using System;

    public partial class _Exception {
        public class Server {
            public class IP : Exception {
                public IP(string m) : base("\r\n" + m) { }
                public IP(string m, Exception inner) : base("\r\n" + m, inner) { }
            }
            public class Port : Exception {
                public Port(string m) : base("\r\n" + m) { }
                public Port(string m, Exception inner) : base("\r\n" + m, inner) { }
            }
        }
        public class Session {
            public class StartAsync : Exception {
                public StartAsync(string m) : base("\r\n" + m) { }
                public StartAsync(string m, Exception inner) : base("\r\n" + m, inner) { }
            }
            public class EndAsync : Exception {
                public EndAsync(string m) : base("\r\n" + m) { }
                public EndAsync(string m, Exception inner) : base("\r\n" + m, inner) { }
            }
        }
        public class Client {
            public class ConnectedCallback : Exception {
                public ConnectedCallback(string m) : base("\r\n" + m) { }
                public ConnectedCallback(string m, Exception inner) : base("\r\n" + m, inner) { }
            }
            public class AuthBuiltinNotActive : Exception {
                public AuthBuiltinNotActive(string m) : base("\r\n" + m) { }
                public AuthBuiltinNotActive(string m, Exception inner) : base("\r\n" + m, inner) { }
            }
            public class NotAuthenticated : Exception {
                public NotAuthenticated(string m) : base("\r\n" + m) { }
                public NotAuthenticated(string m, Exception inner) : base("\r\n" + m, inner) { }
            }
            public class CheckStatusCallbackAsync : Exception {
                public CheckStatusCallbackAsync(string m) : base("\r\n" + m) { }
                public CheckStatusCallbackAsync(string m, Exception inner) : base("\r\n" + m, inner) { }
            }
            public class DisconnectedCallbackAsync : Exception {
                public DisconnectedCallbackAsync(string m) : base("\r\n" + m) { }
                public DisconnectedCallbackAsync(string m, Exception inner) : base("\r\n" + m, inner) { }
            }
        }
    }
}