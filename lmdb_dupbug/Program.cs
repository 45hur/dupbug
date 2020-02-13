using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace lmdb_dupbug
{
    class Program
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly Random random = new Random();

        private static void LoadLogConfig()
        {
            var repo = log4net.LogManager.CreateRepository(Assembly.GetEntryAssembly(), typeof(log4net.Repository.Hierarchy.Hierarchy));
            log4net.GlobalContext.Properties["pid"] = Process.GetCurrentProcess().Id;

            log4net.Config.XmlConfigurator.ConfigureAndWatch(repo, new FileInfo("log4net.config"));
        }

        public static string RandomString(int length)
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
            return new string(Enumerable.Repeat(chars, length)
              .Select(s => s[random.Next(s.Length)]).ToArray());
        }

        static void Main(string[] args)
        {
            LoadLogConfig();
            log.Info("Main");

            var list = new List<KeyValuePair<byte[], byte[]>>();
            log.Info("Fill list with random key-value pairs");
            for (var i = 0; i < 10000; i++)
            {
                var key = RandomString(16);
                var value = RandomString(16);
                if (i == 0)
                    log.Info($"First pair in the list - Key: '{key}', Value: '{value}'");

                var keyBytes = Encoding.ASCII.GetBytes(key);
                var valueBytes = Encoding.ASCII.GetBytes(value);
                var kvp = new KeyValuePair<byte[], byte[]>(
                    keyBytes, valueBytes);
                list.Add(kvp);
            }

            try
            {
                var lmdbdir = "/var/lmdb";
                if (Directory.Exists(lmdbdir))
                {
                    log.Info("Removing .mdb from previous run.");
                    var filesToDelete = Directory.GetFiles(lmdbdir);
                    foreach (var file in filesToDelete)
                    {
                        File.Delete(file);
                    }
                }
                else
                {
                    log.Info($"Creating directory {lmdbdir}");
                    Directory.CreateDirectory(lmdbdir);
                }
                log.Info($"Initialize LMDB at {lmdbdir}");
                var lmdb = new Lightning(lmdbdir, 1, 3145728); //3 megabytes more or less sufficient for 5 iterations

                log.Info($"Endless cycle of insertion of {list.Count} keyvaluepairs over and over.");
                while (true)
                {
                    foreach (var item in list)
                    {
                        lmdb.Put("test", item.Key, item.Value, LightningDB.DatabaseOpenFlags.Create | LightningDB.DatabaseOpenFlags.IntegerKey, LightningDB.CursorPutOptions.NoOverwrite);
                    }

                    log.Info($"Inserted {list.Count} records.");

                    //data.mdb is growing, feel free to check how many times key is in the .mdb file.
                    //I know this sample uses random strings, but in our case we used IPv6 as a key, which is an integer candidate.
                    //Problem is that LMDB completely ignores NoOverwrite flag.
                    //I tried to use put directly (not using cursor), it had a same effect.
                    Task.Delay(1000).Wait();
                }
            }
            catch (Exception ex)
            {
                log.Error(ex);
            }
        }
    }
}
