using System.IO;
using System.Threading.Tasks;

namespace NetTerminal {
    public partial class Connection {
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