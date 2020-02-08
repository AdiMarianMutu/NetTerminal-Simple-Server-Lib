# NetTerminal - Simple and Fast Server Lib
**Hello World!**

I wrote this simple library just for *fun* and as a *personal challenge* while I'm learning to write programs which are able to send/receive data through the internet.
## [How to use]
First of all I want to let you know that the server created with this library is able to support **only one client per time.** *(I plan to implement multi-client connection soon)*

##### Code example - Simple chat between server and client with inbuilt auth:

```csharp
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

using NetTerminal; // Using namespace

namespace NetTerminalExample {
    class Program {
        // Used to read the data from the client
        public static async void read(Server serv, Connection con, Connection.Client cli) {
            byte[] read;

            while (con.Status == Connection.StatusFlag.Active && cli.IsAuth) {
                read = await cli.ReadBytesAsync(2048);

                if (read != null)
                    if (read.Length > 0)
                        Console.Write(Encoding.ASCII.GetString(read));

                await Task.Delay(100);
            }
        }

        // Used to write the data from the client
        public static async void write(Server serv, Connection con, Connection.Client cli) {
            while (con.Status == Connection.StatusFlag.Active && cli.IsAuth) {
                await cli.WriteBytesAsync(Encoding.ASCII.GetBytes(Console.ReadLine() + "\r\n"));

                await Task.Delay(100);
            }
        }


        static void Main(string[] args) {
            Server            server     = new Server(null, 2222, "xs8\n"); // '\n' used if the client is sending data from a terminal which appends a new line at the end of the data
            Connection        connection = new Connection(server);
            Connection.Client client     = new Connection.Client(connection);


        Start:
            if (connection.Status == Connection.StatusFlag.NotActive) {
                connection.StartAsync();

                Console.WriteLine("[Server created]\r\n");
            }

            Console.WriteLine(" > Waiting client...");

            while (client.Status == Connection.Client.StatusFlag.UnknownOrDisconnected)
                Thread.Sleep(10);

            Console.WriteLine($"  > Client connected : {{{client.IP}:{client.Port}}}");



            if (client.Status == Connection.Client.StatusFlag.AuthNoPassword) {
                client.WriteLine("\r\n[Successfully connected to the server!]");


                read(server, connection, client);
                write(server, connection, client);
            } else if (server.AuthRequired) {
                do {
                    if (!client.IsAuth && client.Status == Connection.Client.StatusFlag.Connected)
                        client.AskAuth("\r\n[Connected to the server]\r\n > Please insert the password\r\n");
                    else if (client.Status == Connection.Client.StatusFlag.AuthWrongPassword)
                        client.AskAuth("\r\n[Wrong password!]\r\n > Please insert the password\r\n");
                } while (!client.IsAuth && client.Status != Connection.Client.StatusFlag.AuthTimeout && client.Status != Connection.Client.StatusFlag.UnknownOrDisconnected);


                if (client.Status == Connection.Client.StatusFlag.AuthPassword) {
                    Console.WriteLine("\r\n[Auth Success]\r\n");
                    client.WriteLine("\r\n[Auth Success]\r\n");

                    read(server, connection, client);
                    write(server, connection, client);
                } else if (client.Status == Connection.Client.StatusFlag.AuthTimeout) {
                    client.WriteLine("\r\n[Disconnected from the server because of timeout]\r\n");
                    Console.WriteLine("\r\n[Auth failed! - Timeout]\r\n");

                    client.Disconnect();

                    goto Start;
                }
            }

            // We block the main thread while the client is successfully connected
            while (client.Connected)
                Thread.Sleep(100);


            if (client.Status == Connection.Client.StatusFlag.UnknownOrDisconnected) {
                Console.WriteLine("\r\n[Client disconnected!]\r\n");

                goto Start;
            }
        }
    }
}
```

# 
*Built with .NET Core 3.0*
