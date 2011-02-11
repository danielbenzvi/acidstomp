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
using System.Net.Sockets;
using System.Net;

namespace AcidStomp
{
    public class StompServerClient
    {
        public class MyBuffer
        {
            byte[] _buffer;
            int _cursor;

            public MyBuffer(int size)
            {
                _cursor = 0;
                _buffer = new byte[size];
            }

            public int Size
            {
                get
                {
                    return _buffer.Length;
                }
            }

            public void Remove(int length)
            {
                if (_buffer.Length - length > 0)
                {
                    byte[] newArray = new byte[_buffer.Length - length];
                    Array.Copy(_buffer, length, newArray, 0, _buffer.Length - length);

                    _buffer = newArray;

                    if (_cursor - length >= 0)
                        _cursor -= length;
                }
                else
                {
                    _cursor = 0;
                }
            }
            
            public void Resize(int size)
            {
                Array.Resize<byte>(ref _buffer, size);


                if (_cursor > size)
                    _cursor = size;

            }

            public int Cursor
            {
                get
                {
                    return _cursor;
                }
                set
                {
                    _cursor = value;
                }
            }

            public byte[] Buffer
            {
                get
                {
                    return _buffer;
                }
            }
        }        

        MyBuffer _buffer;
        Socket _socket;
        IPAddress _ip;

        Guid _guid;
        bool _isSendOperationRunning;

        public delegate void OnDisconnectDelegate(StompServerClient client);
        public delegate void OnMessageReceivedDelegate(StompServerClient client, StompMessage message);

        public event OnDisconnectDelegate OnDisconnect;
        public event OnMessageReceivedDelegate OnMessageReceived;

        List<ArraySegment<byte>> _outgoingBuffers;

        //public void delegate OnDisconnectDelegate(StompServerClient client);

        public StompServerClient(Socket socket)
        {
            if (StompConfiguration.ForceOverlappedIo)
                socket.UseOnlyOverlappedIO = true;

            _guid = Guid.NewGuid();
            _socket = socket;
            _ip = ((IPEndPoint)socket.RemoteEndPoint).Address;
            _outgoingBuffers = new List<ArraySegment<byte>>();
            _buffer = new MyBuffer(StompConfiguration.MinClientBufferSize);
        }

        public Guid SessionId
        {
            get
            {
                return _guid;
            }
        }

        public override string ToString()
        {
            return "(StompServerClient [" + RemoteEndpoint.ToString() + "] " + _guid + ")";
        }

        public IPAddress RemoteEndpoint
        {
            get
            {
                return _ip;
            }
        }

        public void Start()
        {
            BeginReceive();
        }
        
        public void Stop()
        {
            try
            {
                _socket.Close();
            }
            catch (Exception) { }
        }

        public void Send(StompMessage message)
        {
            byte[] buffer = message.ToBuffer();

            Send(buffer, 0, buffer.Length);
        }

        void Flush()
        {
            
            if (!_isSendOperationRunning)
            {
                
                lock (_outgoingBuffers)
                {
                    if (_outgoingBuffers.Count > 0)
                    {
                        
                        try
                        {
                            /* Mono has a little problem with sending a list of ArraySegments. 
                             * This is a little workaround -danielb */

                            if (AppCompatibility.IsMono)
                            {
                                _socket.BeginSend(_outgoingBuffers[0].Array, _outgoingBuffers[0].Offset, _outgoingBuffers[0].Count, SocketFlags.None, new AsyncCallback(OnClientSend), _socket);
                                _outgoingBuffers.RemoveAt(0);
                            }
                            else
                            {
                                _socket.BeginSend(_outgoingBuffers, SocketFlags.None, new AsyncCallback(OnClientSend), _socket);
                                _outgoingBuffers = new List<ArraySegment<byte>>();
                            }                        
                        }
                        catch (Exception)
                        {
                            OnInternalDisconnect();
                        }
                    }
                }
            }                            
        }

        public void Send(byte[] buffer, int startIndex, int length)
        {

            lock (_outgoingBuffers)
            {
                _outgoingBuffers.Add(new ArraySegment<byte>(buffer, startIndex, length));                
                Flush();
            }
        }

        private void OnClientSend(IAsyncResult ar)
        {
            try
            {                
                int sent = _socket.EndSend(ar);                

                if (sent == 0)
                {
                    OnInternalDisconnect();
                }
                else
                {
                    // yofi
                }

                lock (_outgoingBuffers)
                {
                    _isSendOperationRunning = false;
                    Flush();
                }
            }
            catch (Exception)
            {
                OnInternalDisconnect();
            }
        }

        private void OnClientReceive(IAsyncResult ar)
        {            
            try
            {
                int readBytes = _socket.EndReceive(ar);

                if (readBytes > 0)
                {                 
                    _buffer.Cursor += readBytes;

                    int localCursor = 0;

                    for (int i = 0; i < _buffer.Cursor; i++)
                    {
                        if (_buffer.Buffer[i] == '\0') // match
                        {
                            StompMessage message = null;

                            try
                            {
                                message = new StompMessage(_buffer.Buffer, localCursor, i - localCursor);

                                if (OnMessageReceived != null)
                                {
                                    try
                                    {
                                        OnMessageReceived(this, message);
                                    }
                                    catch (Exception) { }
                                }
                            }
                            catch (Exception ex)
                            {
                                StompLogger.LogException("Failed to parse stomp packet", ex);
                            }


                            localCursor = i+1;                            
                        }
                    }

                    if (localCursor > 0)
                    {
                        _buffer.Remove(localCursor);                        
                    }
                    

                    BeginReceive();
                }
                else
                {
                    throw new Exception("Connection closed.");
                }
            }
            catch (Exception)
            {
                OnInternalDisconnect();              
            }
        }


        private void BeginReceive()
        {
            try
            {
                if (_buffer.Size - _buffer.Cursor < 512)
                {
                    if (_buffer.Size + 1024 > StompConfiguration.MaxClientBufferSize && _buffer.Size + 1 >= StompConfiguration.MaxClientBufferSize)
                        throw new Exception("Maximum buffer size reached");

                    int growSize = _buffer.Size + 1024;
                    
                    if (growSize > StompConfiguration.MaxClientBufferSize)
                        growSize = StompConfiguration.MaxClientBufferSize;

                    _buffer.Resize(growSize);                    
                }

                _socket.BeginReceive(_buffer.Buffer, _buffer.Cursor, _buffer.Size - _buffer.Cursor, SocketFlags.None, new AsyncCallback(OnClientReceive), null);
            }
            catch (Exception)
            {
                OnInternalDisconnect();
            }
        }

        void OnInternalDisconnect()
        {
            try
            {
                _socket.Close();
            }
            catch (Exception) { }
       
            if (OnDisconnect != null)
                OnDisconnect(this);

        }

    }
}
