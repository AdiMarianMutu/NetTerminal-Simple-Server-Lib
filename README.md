# NetTerminal - Simple and Fast Server Lib
**Hello World!**

I wrote this simple library just for *fun* and as a *personal challenge* while I'm learning to write programs which are able to send/receive data through the internet.
## [General Info]
First of all I want to let you know that the server created with this library is able to support **only one client per time**. *(I plan to implement multi-client connection soon)*

The library does all of the underlaying work *asynchronously* using **async**/**await**.

## [Fast Example]

In order to setup the server we must use 3 classes

```csharp
Server server = new Server(IP, Port, AuthPassword);
Connection connection = new Connection(server);
Connection.Client client = new Connection.Client(connection);
```

Now we will *start* the server which will bind to the selected *port* and start to *listen* for incoming connections

```csharp
connection.Start();
```

And finally we will wait for the *client to connect*

```csharp
while (client.Status == Connection.Client.StatusFlag.UnknownOrDisconnected)
	Thread.Sleep(10);

Console.WriteLine("Client connected!");
```

# 
## [Documentation]
#### Server Class
Properties:

- [**bool**] **AuthRequired** : *Indicates if the client will be asked to authenticate in order to talk with the server*
- [**long**] **AuthTimeoutMilliseconds** : *If using the inbuilt Auth Protocol and the client does not authenticate into the range of time selected, the data received will not be read*
- [**Encoding**] **EncodingChar** : *Data encoding*
- [**string**] **IP** : *The IP used for the connection. Leave empty or null to use any IP*
- [**string**] **Password** : *Password. If this value is not null the server will create a basic authorization access based on this*
- [**int**] **Port** : *Port to listen. Leave empty or 0 to use the first available port*

#### Connection Class
Properties:

- [**int**] **ClientCheckStatusMilliseconds** : *Time in milliseconds to check the client connection status*
- [**StatusFlag**] **Status** : *Retrieves the server connection status*

Methods:

- [**void**] **Start and StartAsync** : *Starts the server connection*
- [**void**] **End and EndAsync** : *Ends the server connection*

#### Client Class
Properties:

- [**bool**] **Connected** : *Returns true if the Status.Flag is equal to any of the below values: 
Connected, AuthNoPassword, AuthPassword, AuthWrongPassword, AuthTimeout*
- [**StatusFlag**] **Status** : *Retrieves the server connection status*
- [**string**] **IP** : *Returns the client IP*
- [**bool**] **IsAuth** : *Returns true if the client is authenticated*
- [**int**] **Port** : *Returns the client port*
- [**StatusFlag**] **Port** : *Retrieves the actual client status*

Methods:

- [**bool**] **AskAuth (3 overload)** : *Starts the inbuilt Auth Protocol and returns true if the client successfully passed the auth phase*
- [**void**] **Disconnect** : *Closes the connection with the client (the server will still be running and listening for a new client)*
- [**byte[]**] **ReadBytes and ReadBytesAsync** : *Reads the raw bytes received from the client*
- [**string**] **ReadLine and ReadLineAsync** : *Will read the next line (set of characters) sent by the client*
- [**void (in: string)**] **Write and WriteAsync** : *Writes on the same line*
- [**void (in: byte[])**] **WriteBytes and WriteBytesAsync** : *Writes the buffer to the client*
- [**void (in: string)**] **WriteLine and WriteLineAsync** : *Writes on the same line and will append a new line at the end*

# 
# 

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
