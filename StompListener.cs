using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;

namespace AcidStomp
{
    class StompListener
    {
        

        Dictionary<TcpClient, StompServerClient> _clients;
        TcpListener _listener;
        bool _isListening;
        int _backLog;

        public delegate void OnConnectDelegate(StompServerClient client);
        public event OnConnectDelegate OnConnect;

        public StompListener(int port, int backlog)
        {
            _backLog = backlog;
            _clients = new Dictionary<TcpClient, StompServerClient>();
            _listener = new TcpListener(IPAddress.Any, port);            
        }

        public Boolean IsListening
        {
            get
            {
                return _isListening;
            }
        }
        public void Start()
        {
            if (_isListening)
                throw new InvalidOperationException("Socket is already listening");

            _listener.Start(_backLog);

            _listener.BeginAcceptTcpClient(new AsyncCallback(OnAcceptTcpClient), null);

            _isListening = true;
        }

        public void OnAcceptTcpClient(IAsyncResult ar)
        {
            try
            {
                Socket socket = _listener.EndAcceptSocket(ar);
                StompServerClient client = new StompServerClient(socket);
                
                if (OnConnect != null)
                {
                    OnConnect(client);
                }                      
            }
            catch (Exception ex)
            {
                StompLogger.LogException("Listener failed to initialize the client", ex);
            }

            try
            {
                _listener.BeginAcceptTcpClient(new AsyncCallback(OnAcceptTcpClient), null);
            }
            catch (Exception ex)
            {
                StompLogger.LogException("Failed to begin accept", ex);
                _isListening = false;
            }
        }

    }
}
