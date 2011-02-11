using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace AcidStomp
{
    public static class StompStatistics
    {
        static int[] _incomingMessageSamples = new int[10];
        static int[] _outgoingMessageSamples = new int[10];

        static int _currentIncoming = 0;
        static int _currentOutgoing = 0;

        static int _connectedClients = 0;
        static DateTime _started = DateTime.Now;

        static bool _running;

        private static void StatisticsThread(object obj)
        {
            while (_running)
            {
                Thread.Sleep(950);

                int currentIncoming = Interlocked.Exchange(ref _currentIncoming, 0);
                int currentOutgoing = Interlocked.Exchange(ref _currentOutgoing, 0);

                Interlocked.Exchange(ref _incomingMessageSamples[DateTime.Now.Second % _incomingMessageSamples.Length], currentIncoming);
                Interlocked.Exchange(ref _outgoingMessageSamples[DateTime.Now.Second % _incomingMessageSamples.Length], currentOutgoing);                
                
            }

            _running = false;
        }

        public static void Stop()
        {
            _running = false;
        }

        public static void Start()
        {
            if (!_running)
            {
                _running = true;
                ThreadPool.QueueUserWorkItem(new WaitCallback(StatisticsThread), null);
            }
        }

        public static void CountIncomingMessage()
        {
            Interlocked.Increment(ref _incomingMessageSamples[DateTime.Now.Second % _incomingMessageSamples.Length]);
        }

        public static void CountOutgoingMessage()
        {
            Interlocked.Increment(ref _outgoingMessageSamples[DateTime.Now.Second % _outgoingMessageSamples.Length]);
        }

        public static void AddConnectedClient()
        {
            Interlocked.Increment(ref _connectedClients);
        }

        public static int ConnectedClients
        {
            get
            {
                return _connectedClients;
            }
        }

        public static void RemoveConnectedClient()
        {
            Interlocked.Decrement(ref _connectedClients);
        }

        public static double IncomingMessagesPerSecond       
        {
            get
            {
                double val = 0;

                for (int i = 0; i < _incomingMessageSamples.Length; i++)
                    val += (double)_incomingMessageSamples[i];

                val /= (double)_incomingMessageSamples.Length;

                return val;
            }
        }

        public static double OutgoingMessagesPerSecond
        {
            get
            {
                double val = 0;

                for (int i = 0; i < _outgoingMessageSamples.Length; i++)
                    val += (double)_outgoingMessageSamples[i];
                
                val /= (double)_outgoingMessageSamples.Length;

                return val;
            }
        }

        public static TimeSpan Uptime
        {
            get
            {
                return DateTime.Now - _started;
            }
        }


    }
}
