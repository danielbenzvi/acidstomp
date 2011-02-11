using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;

namespace AcidStomp
{
    class Program
    {
        static void Main(string[] args)
        {
            StompLogger.LogInfo("AcidStomp " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + " - Starting in port " + StompConfiguration.ListenPort);

            StompServer server = new StompServer(StompConfiguration.ListenPort);

            try
            {
                server.Start();

                DateTime lastLogFlush = DateTime.Now;

                while (server.IsRunning)
                {
                    try
                    {
                        if (StompConfiguration.LogFile != null)
                        {
                            if ((DateTime.Now - lastLogFlush).TotalSeconds >= StompConfiguration.LogFileFlushInterval)
                            {
                                StompLogger.Flush();
                                lastLogFlush = DateTime.Now;
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        StompLogger.LogException("Failed to flush log file", ex);
                    }


                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                StompLogger.LogException("Error initializing listen server ", ex);
            }
        }
    }
}
