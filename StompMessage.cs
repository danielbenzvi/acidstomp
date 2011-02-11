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
using System.IO;

namespace AcidStomp
{
    public class StompMessage
    {
        Dictionary<string, string> _headers;
        string _command;
        string _body;
        byte[] _internalBuffer;
        
        public StompMessage(byte[] packet)
        {
            _internalBuffer = packet;

            StringReader reader = new StringReader(Encoding.UTF8.GetString(packet));

            _command = reader.ReadLine();

            _headers = new Dictionary<string, string>();

            string header = reader.ReadLine();

            while (header != "")
            {
                String[] split = header.Split(':');

                if (split.Length < 2)
                    throw new Exception("Malformed header: " + header);

                _headers[split[0]] = split[1];

                header = reader.ReadLine();
            }


            _body = reader.ReadToEnd().TrimEnd('\r', '\n', '\0');            
        }

        public byte[] ToBuffer()
        {
            if (_internalBuffer != null)
                return _internalBuffer;

            StringBuilder buffer = new StringBuilder((_body != null ? _body.Length : 0) + _command.Length + (_headers != null ? _headers.Count * 32 : 0) + 100);

            buffer.Append(_command + "\n");

            foreach (KeyValuePair<string, string> header in _headers)
            {
                buffer.Append(header.Key + ":" + header.Value + "\n");
            }

            buffer.Append("\n");

            buffer.Append(_body);
            buffer.Append('\0');

            _internalBuffer = Encoding.UTF8.GetBytes(buffer.ToString());

            return _internalBuffer;
        }

        public StompMessage(string command)
        {
            _command = command;
        }

        public bool HeaderExists(string key)
        {
            return _headers != null && _headers.ContainsKey(key);
        }

        public string this[string key]
        {
            get
            {
                if (_headers != null)
                    return _headers[key];

                return null;
            }
            set
            {
                if (_headers == null)
                {
                    _headers = new Dictionary<string, string>();
                }

                _headers[key] = value;
                _internalBuffer = null;
            }
        }


        public string Body
        {
            get
            {
                return _body;
            }
            set
            {
                _internalBuffer = null;
                _body = value;
            }
        }
        public string Command
        {
            get
            {
                return _command;
            }
            set
            {
                _internalBuffer = null;
                _command = value;
            }
        }
    }
}
