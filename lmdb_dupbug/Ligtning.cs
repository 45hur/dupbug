using System;
using System.Reflection;
using System.Text;

using LightningDB;

namespace lmdb_dupbug
{
    public class Lightning : IDisposable
    {
        private static readonly log4net.ILog log = log4net.LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        protected LightningEnvironment env;
        protected static object locker = new object();

        public Lightning()
            : this("/var/lmdb", 1, 1073741824)
        {
        }

        public Lightning(string envPath, int numOfDb, long mapsize)
        {
            env = new LightningEnvironment(envPath)
            {
                MaxDatabases = numOfDb,
                MapSize = mapsize, //1048576 * 4096
                MaxReaders = 4096
            };

            var openflags = /*EnvironmentOpenFlags.WriteMap | EnvironmentOpenFlags.MapAsync |*/ EnvironmentOpenFlags.NoThreadLocalStorage | EnvironmentOpenFlags.NoSync;
            env.Open(openflags);
        }

        public void Dispose()
        {
            env.Dispose();
        }

        public void Put(string dbname, string key, string value)
        {
            Put(dbname, Encoding.UTF8.GetBytes(key), Encoding.UTF8.GetBytes(value));
        }

        public void Put(string dbname, byte[] key, byte[] value)
        {
            Put(dbname, key, value, DatabaseOpenFlags.Create, CursorPutOptions.NoOverwrite);
        }

        public void Put(string dbname, byte[] key, byte[] value, DatabaseOpenFlags flags, CursorPutOptions options)
        {
            lock (locker)
            {
                using (var tx = env.BeginTransaction())
                {
                    try
                    {
                        using (var db = tx.OpenDatabase(dbname, new DatabaseConfiguration { Flags = flags }))
                        {
                            if (!tx.ContainsKey(db, key))
                            {
                                using (var cursor = tx.CreateCursor(db))
                                {
                                    cursor.Put(key, value, options);
                                }

                                tx.Commit();
                            }
                            else
                            {
                                tx.Abort();
                            }
                        }
                    }
                    catch (LightningException ex)
                    {
                        log.Error($"Put {dbname} key {key.Length}", ex);

                        tx.Abort();
                    }
                    catch (Exception ex2)
                    {
                        log.Error("Put2", ex2);
                    }

                    env.Flush(true);
                }
            }
        }

        public string Get(string dbName, string key)
        {
            var bytes = Get(dbName, Encoding.UTF8.GetBytes(key));
            if (bytes == null || bytes.Length == 0)
                return string.Empty;

            return Encoding.UTF8.GetString(bytes);
        }

        public byte[] Get(string dbName, byte[] key)
        {
            lock (locker)
            {
                byte[] result = null;
                using (var tx = env.BeginTransaction(TransactionBeginFlags.ReadOnly))
                {
                    try
                    {
                        using (var db = tx.OpenDatabase(dbName))
                        {
                            result = tx.Get(db, key);
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error("Get", ex);
                    }
                    finally
                    {
                        tx.Abort();
                    }
                }
                return result;
            }
        }

        public void Del(string dbName, string key)
        {
            Del(dbName, Encoding.UTF8.GetBytes(key));
        }

        public void Del(string dbName, byte[] key)
        {
            lock (locker)
            {
                using (var tx = env.BeginTransaction(TransactionBeginFlags.None))
                {
                    try
                    {
                        using (var db = tx.OpenDatabase(dbName, new DatabaseConfiguration() { Flags = DatabaseOpenFlags.DuplicatesSort }))
                        {
                            if (tx.ContainsKey(db, key))
                            {
                                using (var cur = tx.CreateCursor(db))
                                {
                                    while (cur.MoveTo(key))
                                    {
                                        cur.Delete();
                                    }
                                }
                            }
                            tx.Commit();
                        }
                    }
                    catch (Exception ex)
                    {
                        log.Error($"Del {dbName} key {key.Length}", ex);
                        tx.Abort();
                    }
                    finally
                    {
                        env.Flush(true);
                    }
                }
            }
        }
    }
}