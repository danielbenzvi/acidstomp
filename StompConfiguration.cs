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
using System.Configuration;

namespace AcidStomp
{
    public static class StompConfiguration
    {
        [ThreadStatic]
        private static Dictionary<string, object> _cache;

        private static object GetValueOrDefaultInternal(string key, object def)
        {
            object ret = def;

            if (ConfigurationSettings.AppSettings[key] != null)
            {
                String val = ConfigurationSettings.AppSettings[key];
 
                if (def != null)
                {
                    if (def is Int32)
                    {
                        ret = (object)Int32.Parse(val);
                    } 
                    else if (def is Int16)
                    {
                        ret = (object)Int16.Parse(val);
                    }
                    else if (def is String)
                    {
                        ret = val;
                    }
                    else if (def is Boolean)
                    {
                        ret = (object)Boolean.Parse(val);
                    }
                    else if (def is Int64)
                    {
                        ret = (object)Int64.Parse(val);
                    }
                    else if (def is double)
                    {
                        ret = (object)double.Parse(val);
                    }
                    else if (def is char)
                    {
                        ret = (object)char.Parse(val);
                    }
                    else if (def is byte)
                    {
                        ret = (object)byte.Parse(val);
                    }
                }
                else 
                    ret = val;
            }

            return ret;
        }

        private static T GetValueOrDefault<T>(string key, T def)
        {
            if (_cache == null)
            {
                _cache = new Dictionary<string, object>();
            }
            else if (_cache.ContainsKey(key))
            {
                return (T)_cache[key];
            }

            object val = GetValueOrDefaultInternal(key, (T)def);

            _cache[key] = val;

            return (T)val;
        }

        public static short ListenPort
        {
            get
            {
                return GetValueOrDefault<short>("LISTEN_PORT", 15795);
            }
        }

        public static Boolean ForceOverlappedIo
        {
            get
            {
                return GetValueOrDefault<Boolean>("FORCE_OVERLAPPED_IO", false);
            }
        }

        public static string LogFile
        {
            get
            {
                return GetValueOrDefault<string>("LOG_FILE", null);
            }
        }

        public static int LogFileFlushInterval
        {
            get
            {
                return GetValueOrDefault<int>("LOG_FILE_FLUSH_INTERVAL", 10);
            }
        }

        public static int ListenBacklog
        {
            get
            {
                return GetValueOrDefault<int>("LISTEN_BACKLOG", 100);
            }
        }


        public static int MaxClients
        {
            get
            {
                return GetValueOrDefault<int>("MAX_CLIENTS", 4096);
            }
        }
        public static int LogLevel
        {
            get
            {
                return GetValueOrDefault<int>("LOG_LEVEL", 1);
            }
        }

        public static int MinClientBufferSize
        {
            get
            {
                return GetValueOrDefault<int>("MINIMUM_CLIENT_BUFFER_SIZE", 1024);
            }
        }

        public static int MaxClientBufferSize
        {
            get
            {
                return GetValueOrDefault<int>("MAXIMUM_CLIENT_BUFFER_SIZE", 16384);
            }
        }
    }

}
