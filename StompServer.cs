/*
    Ultrafast & lightweight .NET stomp broker
    Copyright (C) 2011 Daniel Ben-Zvi [daniel.benzvi@gmail.com]

    This program is free software; you can redistribute it and/or
    modify it under the terms of the GNU General Public License
    as published by the Free Software Foundation; either version 2
    of the License, or (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program; if not, write to the Free Software
    Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301, USA.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AcidStomp
{
    public class StompServer
    {
        Dictionary<String, Action<StompServerClient, StompMessage>> _actionMap;
        Dictionary<String, StompPath> _paths;
        HashSet<StompServerClient> _clients;        

        StompListener _listener;

        public StompServer(int port)
        {
            _actionMap = new Dictionary<string, Action<StompServerClient, StompMessage>>();
            _clients = new HashSet<StompServerClient>();
            _paths = new Dictionary<string, StompPath>();
            _listener = new StompListener(port, StompConfiguration.ListenBacklog);

            InitEvents();
            InitActionMap();
        }

        public Boolean IsRunning
        {
            get
            {
                return _listener.IsListening;
            }
        }

        void InitClientsEvents(StompServerClient client)
        {
            client.OnDisconnect += new StompServerClient.OnDisconnectDelegate(client_OnDisconnect);
            client.OnMessageReceived += new StompServerClient.OnMessageReceivedDelegate(client_OnMessageReceived);
        }

        void client_OnMessageReceived(StompServerClient client, StompMessage message)
        {
            if (_actionMap.ContainsKey(message.Command))
            {
                try
                {
                    StompLogger.LogDebug(client.ToString() + " sent a command " + message.Command);

                    _actionMap[message.Command].DynamicInvoke(client, message);                    
                }
                catch (Exception ex)
                {
                    StompLogger.LogException(client.ToString() + " sent a command " + message.Command + " which caused an exception", ex);
                }
            }
            else
            {
                StompLogger.LogWarning(client.ToString() + " sent an unhandled command " + message.Command);
            }
        }

        void client_OnDisconnect(StompServerClient client)
        {
            lock (this)
            {
                StompLogger.LogDebug(client.ToString() + " quitted");
                _clients.Remove(client);
            }          
        }

        void InitEvents()
        {
            _listener.OnConnect += new StompListener.OnConnectDelegate(_listener_OnConnect);
        }

        void _listener_OnConnect(StompServerClient client)
        {
            lock (this)
            {
                if (_clients.Count + 1 > StompConfiguration.MaxClients)
                {
                    StompLogger.LogWarning("Maximum clients ("+StompConfiguration.MaxClients+") reached. Disconnecting " + client.ToString());
                    client.Stop();
                    return;
                }

                _clients.Add(client);
            }

            InitClientsEvents(client);

            client.Start();

            StompLogger.LogDebug(client.ToString() + " connected");
        }


        public void Start()
        {
            _listener.Start();
        }

        void InitActionMap()
        {
            _actionMap["CONNECT"] = new Action<StompServerClient, StompMessage>(OnStompCommand_Connect);
            _actionMap["SUBSCRIBE"] = new Action<StompServerClient, StompMessage>(OnStompCommand_Subscribe);
            _actionMap["UNSUBSCRIBE"] = new Action<StompServerClient, StompMessage>(OnStompCommand_Unsubscribe);
            _actionMap["SEND"] = new Action<StompServerClient, StompMessage>(OnStompCommand_Send);
        }

        private void OnStompCommand_Connect(StompServerClient client, StompMessage message)
        {
            StompLogger.LogDebug(client.ToString() + " connected with session-id " + client.SessionId.ToString());

            StompMessage result = new StompMessage("CONNECTED");
            result["session-id"] = client.SessionId.ToString();

            client.Send(result);
        }

        private void OnStompCommand_Subscribe(StompServerClient client, StompMessage message)
        {
            StompPath path = null;
            String destination = message["destination"];

            StompLogger.LogDebug(client.ToString() + " subscribes to " + destination);
            
            lock (this)
            {
                if (_paths.ContainsKey(destination))
                    path = _paths[destination];
                else
                {
                    path = new StompPath(destination);
                    path.OnLastClientRemoved += new StompPath.OnLastClientRemovedDelegate(path_OnLastClientRemoved);
                    _paths[destination] = path;
                }               
            }
            
            path.AddClient(client);            
        }

        void path_OnLastClientRemoved(StompPath path)
        {
            lock (this)
            {
                if (_paths.ContainsKey(path.Name))
                    _paths.Remove(path.Name);
            }

            StompLogger.LogDebug("Path " + path.Name + " destroyed");

        }

        private void OnStompCommand_Unsubscribe(StompServerClient client, StompMessage message)
        {
            StompPath path = null;
            String destination = message["destination"];

            StompLogger.LogDebug(client.ToString() + " unsubscribes from " + destination);

            lock (this)
            {
                if (_paths.ContainsKey(destination))
                    path = _paths[destination];
            }

            if (path != null)
                path.RemoveClient(client);       
        }

        private void OnStompCommand_Send(StompServerClient client, StompMessage message)
        {
            StompPath path = null;
            String destination = message["destination"];

            StompLogger.LogDebug(client.ToString() + " sent data to " + destination);

            lock (this)
            {
                if (_paths.ContainsKey(destination))
                    path = _paths[destination];
            }

            if (path != null)
            {
                List<StompServerClient> pathClients = path.Clients;

                message["message-id"] = Guid.NewGuid().ToString();
                message.Command = "MESSAGE";

                foreach (StompServerClient pathClient in pathClients)
                {
                    try
                    {
                        StompLogger.LogDebug("Sending " + message.Command + " to " + pathClient.ToString());
                        pathClient.Send(message);
                    }
                    catch (Exception ex)
                    {
                        StompLogger.LogException(pathClient.ToString() + " has thrown an exception while sending data", ex);
                    }
                }
            }
        }

    }
}
