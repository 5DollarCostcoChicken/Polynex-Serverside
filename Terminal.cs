using System;
using System.Numerics;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Terminal
    {

        #region Update
        public const int updatesPerSecond = 30;
        public static void Update()
        {
            
        }
        #endregion

        #region Connection
        public const int maxPlayers = 100000;
        public const int port = 5555;
        public static void OnClientConnected(int id, string ip)
        {
            
        }

        public static void OnClientDisconnected(int id, string ip)
        {
            
        }
        #endregion

        #region Data
        public static void ReceivedPacket(int clientID, Packet packet)
        {
            int id = packet.ReadInt();
            string device = packet.ReadString();
            switch (id)
            {
                case 2: //sync player data
                    Database.GetPlayerData(clientID, device);
                    break;
                case 3: //changing any user stat that is an int
                    string variable = packet.ReadString();
                    int addAmount = packet.ReadInt();
                    Database.AddIntVar(clientID, device, variable, addAmount);
                    break;
                case 4: //changing the username
                    string newName = packet.ReadString();
                    Database.UpdateUsername(clientID, device, newName);
                    break;
                case 5: //changing the username
                    Database.ResetAccount(clientID, device);
                    break;
            }
        }

        public static void ReceivedBytes(int clientID, int packetID, byte[] data)
        {
            
        }

        public static void ReceivedString(int clientID, int packetID, string data)
        {
            switch (packetID)
            {
                case 1: // request for authentication
                    Database.AuthenticatePlayer(clientID, data);
                    break;
                case 2: //sync player data
                    Database.GetPlayerData(clientID, data);
                    break;
            }
        }

        public static void ReceivedInteger(int clientID, int packetID, int data)
        {
            
        }

        public static void ReceivedFloat(int clientID, int packetID, float data)
        {

        }

        public static void ReceivedBoolean(int clientID, int packetID, bool data)
        {

        }

        public static void ReceivedVector3(int clientID, int packetID, Vector3 data)
        {

        }

        public static void ReceivedQuaternion(int clientID, int packetID, Quaternion data)
        {

        }

        public static void ReceivedLong(int clientID, int packetID, long data)
        {

        }

        public static void ReceivedShort(int clientID, int packetID, short data)
        {

        }

        public static void ReceivedByte(int clientID, int packetID, byte data)
        {

        }

        public static void ReceivedEvent(int clientID, int packetID)
        {

        }
        #endregion

    }
}