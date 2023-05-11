using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.IO;

namespace DevelopersHub.RealtimeNetworking.Server
{
    public static class Data
    {
        public class Player
        {
            public int level = 0;
            public int xp = 0;
            public string username;
            public int power = 0;
            public int pfp = 0;
            public List<Character> characters = new List<Character>();
        }
        public class Character
        {
            public int char_index = 0;
            public int level = 0;
            public int xp = 0;
            public string characterName;
            public string cName;
            public int stars = 0;
            public int shards = 0;
            public int min_shards = 0;
            public int power = 0;
            public bool activated = false;
        }
        public class ServerCharacter
        {
            public string characterName;
            public string cName;
            public int min_shards = 0;
            public int power = 0;
        }

        public async static Task<String> Serialize<T>(this T target)
        {
            Task<string> task = Task.Run(() => 
            {
                XmlSerializer xml = new XmlSerializer(typeof(T));
                StringWriter writer = new StringWriter();
                xml.Serialize(writer, target);
                return writer.ToString();
            });
            return await task;
        }

        public async static Task<T> Deserialize<T>(this string target)
        {
            Task<T> task = Task.Run(() =>
            {
                XmlSerializer xml = new XmlSerializer(typeof(T));
                StringReader reader = new StringReader(target);
                return (T)xml.Deserialize(reader);
            });
            return await task;
        }
    }
}
