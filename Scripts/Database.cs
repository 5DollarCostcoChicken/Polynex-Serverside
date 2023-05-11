using System;
using MySql.Data.MySqlClient;
using System.Data;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace DevelopersHub.RealtimeNetworking.Server
{
    class Database
    {

        #region MySQL
        private static MySqlConnection _mysqlConnection;
        private const string _mysqlServer = "127.0.0.1";
        private const string _mysqlUsername = "root";
        private const string _mysqlPassword = "";
        private const string _mysqlDatabase = "polynex_accounts";

        public static MySqlConnection mysqlConnection
        {
            get
            {
                if (_mysqlConnection == null || _mysqlConnection.State == ConnectionState.Closed)
                {
                    try
                    {
                        _mysqlConnection = new MySqlConnection("SERVER=" + _mysqlServer + "; DATABASE=" + _mysqlDatabase + "; UID=" + _mysqlUsername + "; PASSWORD=" + _mysqlPassword + ";");
                        _mysqlConnection.Open();
                        Console.WriteLine("Connection established with MySQL database.");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed to connect the MySQL database.");
                    }
                }
                else if (_mysqlConnection.State == ConnectionState.Broken)
                {
                    try
                    {
                        _mysqlConnection.Close();
                        _mysqlConnection = new MySqlConnection("SERVER=" + _mysqlServer + "; DATABASE=" + _mysqlDatabase + "; UID=" + _mysqlUsername + "; PASSWORD=" + _mysqlPassword + ";");
                        _mysqlConnection.Open();
                        Console.WriteLine("Connection re-established with MySQL database.");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Failed to connect the MySQL database.");
                    }
                }
                return _mysqlConnection;
            }
        }

        public static void Demo_MySQL_1()
        {
            string query = String.Format("UPDATE table SET int_column = {0}, string_column = '{1}', datetime_column = NOW();", 123, "Hello World");
            using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
            {
                command.ExecuteNonQuery();
            }
        }

        public static void Demo_MySQL_2()
        {
            string query = String.Format("SELECT column1, column2 FROM table WHERE column3 = {0} ORDER BY column1 DESC;", 123);
            using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
            {
                using (MySqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.HasRows)
                    {
                        while (reader.Read())
                        {
                            int column1 = int.Parse(reader["column1"].ToString());
                            string column2 = reader["column2"].ToString();
                        }
                    }
                }
            }
        }

        public async static void AuthenticatePlayer(int id, string device)
        {
            long account_id = await AuthenticatePlayerAsync(id, device);
            Server.clients[id].device = device;
            Server.clients[id].account = account_id;
            Sender.TCP_Send(id, 1, account_id);
        }
        private async static Task<long> AuthenticatePlayerAsync(int id, string device)
        {
            Task<long> task = Task.Run(() =>
            {
                long account_id = 0;
                string query = String.Format("SELECT id FROM accounts WHERE device_id = '{0}';", device);
                bool found = false;
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                account_id = long.Parse(reader["id"].ToString());
                                found = true;
                            }
                        }
                    }
                }
                if (!found)
                {
                    query = String.Format("INSERT INTO accounts (device_id) VALUES('{0}');", device);
                    using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                    {
                        command.ExecuteNonQuery();
                        account_id = command.LastInsertedId;
                        ResetAccount((int)account_id, device);
                        //whatever needs to be done when an account is created for the first time
                    }
                }
                return account_id;
            });
            return await task;
        }

        public async static void GetPlayerData(int id, string device)
        {
            long account_id = Server.clients[id].account;
            await CheckForMissingCharactersAsync(account_id);
            Data.Player player = await GetPlayerDataAsync(id, device);
            List<Data.Character> characters = await GetCharactersAsync(account_id);
            player.characters = characters;
            Packet packet = new Packet();
            packet.Write(2);
            string playerData = await Data.Serialize<Data.Player>(player);
            packet.Write(playerData);
            Sender.TCP_Send(id, packet);
        }
        private async static Task<Data.Player> GetPlayerDataAsync(int id, string device)
        {
            Task<Data.Player> task = Task.Run(() =>
            {
                Data.Player data = new Data.Player();
                string query = String.Format("SELECT id, level, xp, username, power FROM accounts WHERE device_id = '{0}';", device);
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                //data.id = long.Parse(reader["id"].ToString());
                                data.level = int.Parse(reader["level"].ToString());
                                data.xp = int.Parse(reader["xp"].ToString());
                                data.username = reader["username"].ToString();
                                data.power = int.Parse(reader["power"].ToString());
                            }
                        }
                    }
                }
                return data;
            });
            return await task;
        }


        public async static void AddIntVar(int id, string device, string variable, int addAmount)
        {
            await AddIntVarAsync(Server.clients[id].account, variable, addAmount);
            Data.Player player = await GetPlayerDataAsync(id, device);
            Packet packet = new Packet();
            string playerData = await Data.Serialize<Data.Player>(player);
            packet.Write(2);
            packet.Write(playerData);
            Sender.TCP_Send(id, packet);
        }
        private async static Task<long> AddIntVarAsync(long account_id, string variable, int addAmount)
        {
            Task<long> task = Task.Run(() =>
            {
                long id = account_id;
                string query = String.Format("UPDATE accounts SET " + variable + " = " + variable + " + {1} WHERE id = {0};", account_id, addAmount);
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    command.ExecuteNonQuery();
                }
                return id;
            });
            return await task;
        }

        public async static void UpdateUsername(int id, string device, string newName)
        {
            await UpdateUsernameAsync(Server.clients[id].account, newName);
            Data.Player player = await GetPlayerDataAsync(id, device);
            Packet packet = new Packet();
            string playerData = await Data.Serialize<Data.Player>(player);
            packet.Write(2);
            packet.Write(playerData);
            Sender.TCP_Send(id, packet);
        }
        private async static Task<long> UpdateUsernameAsync(long account_id, string newName)
        {
            Task<long> task = Task.Run(() =>
            {
                long id = account_id;
                string query = String.Format("UPDATE accounts SET username = N'{1}' WHERE id = {0};", account_id, newName);
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    command.ExecuteNonQuery();
                }
                return id;
            });
            return await task;
        }
        private async static Task<List<Data.Character>> GetCharactersAsync(long account)
        {
            Task<List<Data.Character>> task = Task.Run(() =>
            {
                List<Data.Character> data = new List<Data.Character>();
                string query = String.Format("SELECT char_index, global_id, level, xp, name, stars, shards, min_shards, power, activated FROM characters WHERE account_id = '{0}';", account);
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Data.Character character = new Data.Character();
                                character.char_index = int.Parse(reader["char_index"].ToString());
                                character.characterName = reader["global_id"].ToString();
                                character.level = int.Parse(reader["level"].ToString());
                                character.xp = int.Parse(reader["xp"].ToString());
                                character.cName = reader["name"].ToString();
                                character.stars = int.Parse(reader["stars"].ToString());
                                character.shards = int.Parse(reader["shards"].ToString());
                                character.min_shards = int.Parse(reader["min_shards"].ToString());
                                character.power = int.Parse(reader["power"].ToString());
                                int active = int.Parse(reader["activated"].ToString());
                                if (active == 1)
                                    character.activated = true;
                                else
                                    character.activated = false;
                                data.Add(character);
                            }
                        }
                    }
                }
                return data;
            });
            return await task;
        }
        private async static Task<List<Data.ServerCharacter>> GetServerCharacterAsync(string id)
        {
            Task<List<Data.ServerCharacter>> task = Task.Run(() =>
            {
                List<Data.ServerCharacter> data = new List<Data.ServerCharacter>();
                string query = String.Format("SELECT id, level FROM server_characters WHERE global_id = '{0}';", id);
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                Data.ServerCharacter character = new Data.ServerCharacter();
                                character.power = int.Parse(reader["power"].ToString());
                                data.Add(character);
                                
                            }
                        }
                    }
                }
                return data;
            });
            return await task;
        }

        public async static void ResetAccount(int id, string device)
        {
            await ResetAccountAsync(Server.clients[id].account);
            await ResetAccountCharactersAsync(Server.clients[id].account);
            Data.Player player = await GetPlayerDataAsync(id, device);
            Packet packet = new Packet();
            string playerData = await Data.Serialize<Data.Player>(player);
            packet.Write(2);
            packet.Write(playerData);
            Sender.TCP_Send(id, packet);
        }
        private async static Task<long> ResetAccountAsync(long account_id)
        {
            Task<long> task = Task.Run(() =>
            {
                long id = account_id;
                string query = String.Format("UPDATE accounts SET level = 1, xp = 0, power = 0, pfp = 0 WHERE id = {0};", account_id);
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    command.ExecuteNonQuery();
                }
                return id;
            });
            return await task;
        }
        private async static Task<long> ResetAccountCharactersAsync(long account_id)
        {
            Task<long> task = Task.Run(() =>
            {
                Data.Player data = new Data.Player();

                long length;
                //getting length of number of characters
                string query = String.Format("SELECT COUNT(*) FROM server_characters;");
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    command.CommandText = query;
                    length = (long)command.ExecuteScalar();
                }
                long id = account_id;

                //remove previous characters of the account
                string query2 = String.Format("DELETE FROM characters WHERE account_id = '{0}';", account_id);
                using (MySqlCommand command2 = new MySqlCommand(query2, mysqlConnection))
                {
                    command2.ExecuteNonQuery();
                }

                //insert server characters data into account characters
                for (int i = 0; i < length; i++)
                {
                    string characterName = "null";
                    string cName = "null";
                    int power = 0;
                    int min_shards = 0;

                    string query3 = String.Format("SELECT global_id, name, power, min_shards FROM server_characters WHERE id = '{0}';", i);
                    using (MySqlCommand command = new MySqlCommand(query3, mysqlConnection))
                    {
                        using (MySqlDataReader reader = command.ExecuteReader())
                        {
                            if (reader.HasRows)
                            {
                                while (reader.Read())
                                {
                                    Data.ServerCharacter character = new Data.ServerCharacter();
                                    character.characterName = reader["global_id"].ToString();
                                    character.cName = reader["name"].ToString();
                                    character.power = int.Parse(reader["power"].ToString());
                                    character.min_shards = int.Parse(reader["min_shards"].ToString());
                                    characterName = character.characterName;
                                    cName = character.cName;
                                    power = character.power;
                                    min_shards = character.min_shards;
                                }
                            }
                        }
                    }
                    string query4 = String.Format("INSERT INTO characters (char_index, global_id, account_id, name, power, min_shards) VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}');", i, characterName, account_id, cName, power, min_shards);
                    using (MySqlCommand command2 = new MySqlCommand(query4, mysqlConnection))
                    {
                        command2.ExecuteNonQuery();
                    }
                }
                return id;
            });
            return await task;
        }
        
        
        private async static Task<long> CheckForMissingCharactersAsync(long account_id)
        {
            Task<long> task = Task.Run(() =>
            {
                Data.Player data = new Data.Player();

                long length;
                //getting length of number of characters
                string query = String.Format("SELECT COUNT(*) FROM server_characters;");
                using (MySqlCommand command = new MySqlCommand(query, mysqlConnection))
                {
                    command.CommandText = query;
                    length = (long)command.ExecuteScalar();
                }
                long id = account_id;

                //getting all characters of account
                List<int> chars = new List<int>();
                string query2 = String.Format("SELECT char_index FROM characters WHERE account_id = '{0}';", account_id);
                using (MySqlCommand command = new MySqlCommand(query2, mysqlConnection))
                {
                    using (MySqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.HasRows)
                        {
                            while (reader.Read())
                            {
                                chars.Add(int.Parse(reader["char_index"].ToString()));
                            }
                        }
                    }
                }

                //insert server characters data into account characters if it doesn't already exist
                for (int i = 0; i < length; i++)
                {
                    if (!chars.Contains(i)) {
                        string characterName = "null";
                        string cName = "null";
                        int power = 0;
                        int min_shards = 0;

                        string query3 = String.Format("SELECT global_id, name, power, min_shards FROM server_characters WHERE id = '{0}';", i);
                        using (MySqlCommand command = new MySqlCommand(query3, mysqlConnection))
                        {
                            using (MySqlDataReader reader = command.ExecuteReader())
                            {
                                if (reader.HasRows)
                                {
                                    while (reader.Read())
                                    {
                                        Data.ServerCharacter character = new Data.ServerCharacter();
                                        character.characterName = reader["global_id"].ToString();
                                        character.cName = reader["name"].ToString();
                                        character.power = int.Parse(reader["power"].ToString());
                                        character.min_shards = int.Parse(reader["min_shards"].ToString());
                                        characterName = character.characterName;
                                        cName = character.cName;
                                        power = character.power;
                                        min_shards = character.min_shards;
                                    }
                                }
                            }
                        }
                        string query4 = String.Format("INSERT INTO characters (char_index, global_id, account_id, name, power, min_shards) VALUES('{0}', '{1}', '{2}', '{3}', '{4}', '{5}');", i, characterName, account_id, cName, power, min_shards);
                        using (MySqlCommand command2 = new MySqlCommand(query4, mysqlConnection))
                        {
                            command2.ExecuteNonQuery();
                        }
                    }
                }
                return id;
            });
            return await task;
        }
        #endregion
    }
}