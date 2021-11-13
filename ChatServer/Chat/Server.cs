using System;
using System.Collections;
using System.Threading;
using System.Net;
using System.Net.Sockets;
using Chat;
using Serialization;
using Npgsql;

namespace ChatServer.Chat
{
    class Server
    {
        Socket socket;
        Thread listenThread;
        Hashtable usersTable;
        NpgsqlConnection conn;
        public Server()
        {
            try
            {
                IPHostEntry host = Dns.GetHostEntry("localhost");
                IPAddress addr = host.AddressList[0];
                IPEndPoint endPoint = new IPEndPoint(addr, 4404);

                conn = new NpgsqlConnection("Server = localhost; User Id = postgres; Password = bnm123123; Database = postgres");

                socket = new Socket(endPoint.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                socket.Bind(endPoint);
                socket.Listen(10);

                listenThread = new Thread(this.Listen);
                listenThread.Start();
                usersTable = new Hashtable();
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e.Message);
            }
        }

        public void Conectar()
        {
            conn.Open();
        }

        public void Desconectar()
        {
            conn.Close();
        }

        /// <summary>
        /// Ready to accept connection from clients
        /// </summary>
        private void Listen()
        {
            Socket client;
            while(true)
            {
                client = this.socket.Accept();
                listenThread = new Thread(this.ListenClient);
                listenThread.Start(client);
            }
        }

        /// <summary>
        /// Listen to client
        /// </summary>
        /// <param name="o">Socket client</param>
        private void ListenClient(object o)
        {
            Socket client = (Socket)o;
            object received;
            do
            {
                received = this.Receive(client);
                
            } while (!(received is User));

            this.usersTable.Add(received, client);
            this.BroadCast(received);
            this.SendAllUsers(client);

            while (true)
            {
                received = this.Receive(client);
                if(received is Message)
                {
                    this.SendMessage((Message) received);
                    Message recibido = (Message)received;
                    Console.WriteLine("ID " + recibido.from.id.ToString() + " , NICK " + recibido.from.nick.ToString() + " , MSG " + recibido.msg.ToString());
                    this.Insertar(recibido.from.id.ToString(), recibido.from.nick.ToString(), recibido.msg.ToString());
                }
            }

        }

        public void Insertar(string id, string nick, string msg)
        {
            string query = "Insert into \"Mensajes\" (id, nick, msg) values('" + id + "','" + nick + "','" + msg + "')";

            NpgsqlCommand ejecutor = new NpgsqlCommand(query, conn);

            try
            {
                this.conn.Open();
                ejecutor.ExecuteNonQuery();
                this.conn.Close();
            }
            catch (Exception e)
            {
                Console.WriteLine("{0}", e.Message);
            }
        }

        public void ObtenerMensajesBD()
        {
            string query = "";

            NpgsqlCommand ejecutor = new NpgsqlCommand(query, conn);

            try
            {
                this.conn.Open();
                
            } 
            catch (Exception e)
            {
                Console.WriteLine("{0}", e.Message);
            }
        }

        /// <summary>
        /// Send a object to all users connected
        /// </summary>
        /// <param name="o">object to send</param>
        private void BroadCast(object o)
        {
            foreach(DictionaryEntry d in this.usersTable)
            {
                this.Send((Socket)d.Value, o);
            }
        }

        /// <summary>
        /// Send all connected users to the client
        /// </summary>
        /// <param name="s">Socket client</param>
        private void SendAllUsers(Socket s)
        {
            foreach (DictionaryEntry d in this.usersTable)
            {
                this.Send(s, d.Key);
            }
        }

        /// <summary>
        /// Send a message to the destinatary
        /// </summary>
        /// <param name="m">Message to send</param>
        private void SendMessage(Message m)
        {
            User tmpUser;

            foreach (DictionaryEntry d in this.usersTable)
            {
                tmpUser = (User)d.Key;
                if(tmpUser.id == m.to.id)
                {
                    this.Send((Socket)d.Value, m);
                    break;
                }
            }
        }

        /// <summary>
        /// Send a object to the client
        /// </summary>
        /// <param name="s">Socket client</param>
        /// <param name="o">Object to send</param>
        private void Send(Socket s, object o)
        {
            byte[] buffer = new byte[1024];
            byte[] obj = BinarySerialization.Serializate(o);
            Array.Copy(obj, buffer, obj.Length);
            s.Send(buffer);
        }

        /// <summary>
        /// Receive all the serialized object
        /// </summary>
        /// <param name="s">Socket that receive the object</param>
        /// <returns>Object received from client</returns>
        private object Receive(Socket s)
        {
            byte[] buffer = new byte[1024];
            s.Receive(buffer);
            return BinarySerialization.Deserializate(buffer);
        }

        
    }
}
