A sample code of LMDB storage overflow.

* NoOverwrite flag is used
* Keys are 16 bytes long
* Record are inserted with IntegerKey flag.

You can use VisualStudio 2019 or Visual Studio for Mac to debug
Eventually you can use docker and run run_me.sh.

Sample console output:
2020-02-13 11:27:58,830 [1-1] INFO [lmdb_dupbug.Program.?] - Main
2020-02-13 11:27:58,857 [1-1] INFO [lmdb_dupbug.Program.?] - Fill list with random key-value pairs
2020-02-13 11:27:58,861 [1-1] INFO [lmdb_dupbug.Program.?] - First pair in the list - Key: 'T53VB95Z1J0ETL0N', Value: 'DTRADPDL53T8ZVQD'
2020-02-13 11:27:58,876 [1-1] INFO [lmdb_dupbug.Program.?] - Creating directory /var/lmdb
2020-02-13 11:27:58,880 [1-1] INFO [lmdb_dupbug.Program.?] - Initialize LMDB at /var/lmdb
2020-02-13 11:27:58,884 [1-1] INFO [lmdb_dupbug.Program.?] - Endless cycle of insertion of 10000 keyvaluepairs over and over.
2020-02-13 11:28:04,351 [1-1] INFO [lmdb_dupbug.Program.?] - Inserted 10000 records.
2020-02-13 11:28:12,242 [1-1] INFO [lmdb_dupbug.Program.?] - Inserted 10000 records.
2020-02-13 11:28:19,482 [1-1] INFO [lmdb_dupbug.Program.?] - Inserted 10000 records.
2020-02-13 11:28:25,348 [1-1] INFO [lmdb_dupbug.Program.?] - Inserted 10000 records.
2020-02-13 11:28:30,836 [1-1] INFO [lmdb_dupbug.Program.?] - Inserted 10000 records.
2020-02-13 11:28:35,046 [1-1] ERROR [lmdb_dupbug.Lightning.?] - Put test key 16
LightningDB -30792: MDB_MAP_FULL: Environment mapsize limit reached

After that one can verify, that .mdb is 3MB in size an key T53VB95Z1J0ETL0N is present multiple times.
