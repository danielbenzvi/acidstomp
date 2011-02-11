using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Reflection;
using System.Configuration;

namespace AcidStomp
{
    class Program
    {
        static void Main(string[] args)
        {
            if (StompLogger.CanLogInfo)
                StompLogger.LogInfo("AcidStomp " + Assembly.GetExecutingAssembly().GetName().Version.ToString() + " - Starting in port " + StompConfiguration.ListenPort);
                       

            try
            {
                StompServer server = new StompServer(StompConfiguration.ListenPort);

                StompStatistics.Start();

                server.Start();

                DateTime lastLogFlush = DateTime.Now;
                DateTime lastStatistics = DateTime.Now;

                while (server.IsRunning)
                {
                    try
                    {
                        if (StompConfiguration.LogStatisticsInterval > 0 && (DateTime.Now - lastStatistics).TotalSeconds >= StompConfiguration.LogStatisticsInterval)
                        {
                            StompLogger.LogInfo(
                                String.Format("(uptime {0}, connected clients: {1}, messages [in: {2}/sec - out: {3}/sec])",
                                StompStatistics.Uptime.ToString(),
                                StompStatistics.ConnectedClients,
                                StompStatistics.IncomingMessagesPerSecond,
                                StompStatistics.OutgoingMessagesPerSecond));

                            lastStatistics = DateTime.Now;
                        }

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
                        if (StompLogger.CanLogException)
                            StompLogger.LogException("Failed to flush log file", ex);
                    }



                    Thread.Sleep(1000);
                }
            }
            catch (Exception ex)
            {
                if (StompLogger.CanLogException)
                    StompLogger.LogException("Error initializing listen server ", ex);
            }
        }
    }
}
