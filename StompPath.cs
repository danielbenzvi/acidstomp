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

namespace AcidStomp
{
    public class StompPath
    {
        string _name;
        HashSet<StompServerClient> _clients;

        public delegate void OnLastClientRemovedDelegate(StompPath path);
        public event OnLastClientRemovedDelegate OnLastClientRemoved;

        public string Name
        {
            get
            {
                return _name;
            }
        }

        public StompPath(string name)
        {
            _name = name;
            _clients = new HashSet<StompServerClient>();
        }

        public void AddClient(StompServerClient client)
        {
            lock (this)
            {
                client.OnDisconnect += new StompServerClient.OnDisconnectDelegate(client_OnDisconnect);
                _clients.Add(client);
            }
        }

        void client_OnDisconnect(StompServerClient client)
        {
            lock (this)
            {
                RemoveClient(client);
            }
        }

        public int UsersCount
        {
            get
            {
                lock (this)
                {
                    return _clients.Count;
                }
            }
        }

        public void RemoveClient(StompServerClient client)
        {
            lock (this)
            {
                client.OnDisconnect -= new StompServerClient.OnDisconnectDelegate(client_OnDisconnect);
                _clients.Remove(client);

                if (_clients.Count == 0 && OnLastClientRemoved != null)
                {
                    try
                    {
                        OnLastClientRemoved(this);
                    }
                    catch (Exception) { }
                }   
            }
        }

        public List<StompServerClient> Clients
        {
            get
            {
                lock (this)
                {
                    return _clients.ToList<StompServerClient>();
                }
            }
        }
    }
}
