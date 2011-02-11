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
    public static class StompLogger
    {
        private static List<String> _writeLogBuffer = new List<string>();
        
        public static void Flush()
        {
            if (StompConfiguration.LogFile == null)
                return;

            List<String> buffers = null;

            lock (_writeLogBuffer)
            {
                if (_writeLogBuffer.Count > 0)
                {
                    buffers = _writeLogBuffer;
                    _writeLogBuffer = new List<string>();
                }
            }

            if (buffers != null)
            {
                StringBuilder theRealBuffer = new StringBuilder(buffers.Count * 128);

                foreach (String buffer in buffers)
                {
                    theRealBuffer.AppendLine(buffer);
                }

                File.AppendAllText(StompConfiguration.LogFile, theRealBuffer.ToString());
            }
        }

        private static void LogData(string severity, string message)
        {
            string buf = "(" + DateTime.Now.ToString() + ") - [" + severity + "]\t - " + message;

            if (StompConfiguration.LogFile != null)
            {
                lock (_writeLogBuffer)
                {
                    _writeLogBuffer.Add(buf);
                }
            }

            Console.Out.WriteLine(buf);
        }

        public static bool CanLogWarning
        {
            get
            {
                return StompConfiguration.LogLevel >= 1;
            }
        }

        public static bool CanLogInfo
        {
            get
            {
                return StompConfiguration.LogLevel >= 0;
            }
        }

        public static bool CanLogException
        {
            get
            {
                return StompConfiguration.LogLevel >= 0;
            }
        }


        public static bool CanLogDebug
        {
            get
            {
                return StompConfiguration.LogLevel >= 2;
            }
        }

        public static void LogWarning(string message)
        {
            if (StompConfiguration.LogLevel >= 1)
                LogData("WARNING", message);
        }


        public static void LogInfo(string message)
        {
            if (StompConfiguration.LogLevel >= 0)
                LogData("INFO", message);                
        }


        public static void LogDebug(string message)
        {
            if (StompConfiguration.LogLevel >= 2)
                LogData("DEBUG", message);                    
        }

        public static void LogException(string message, Exception ex)
        {
            if (StompConfiguration.LogLevel >= 0)
                LogData("EXCEPTION", message + ex.ToString() + "\n\t\t***** EXCEPTION DATA *****\n\t\t" + ex.ToString());
        }

    }
}
