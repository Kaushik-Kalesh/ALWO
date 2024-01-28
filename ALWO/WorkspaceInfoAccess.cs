using Microsoft.Data.Sqlite;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;

namespace ALWO
{
    public static class WorkspaceInfoAccess
    {
        private static string dbFileName;
        private static string dbPath;
        private static string tableName;
        private static List<string> fieldNames = new List<string>();

        public async static void InitializeDatabase()
        {
            dbFileName = "workspaces_info.db";
            await ApplicationData.Current.LocalFolder
                    .CreateFileAsync(dbFileName, CreationCollisionOption.OpenIfExists);
            dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, dbFileName);
            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();

                string tableCommand = "CREATE TABLE IF NOT EXISTS Workspaces (Primary_Key INTEGER PRIMARY KEY, Workspace_Name NVARCHAR(20), Process_Paths NVARCHAR(512) NULL);";
                tableName = "Workspaces";
                fieldNames.Add("Primary_Key");
                fieldNames.Add("Workspace_Name");
                fieldNames.Add("Process_Paths");

                var createTable = new SqliteCommand(tableCommand, db);

                createTable.ExecuteReader();
            }
        }

        public static void AddWorkspace(string workspaceNamw, string encodedProcessPaths)
        {
            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();

                var insertCommand = new SqliteCommand();
                insertCommand.Connection = db;

                insertCommand.CommandText = $"INSERT INTO {tableName} VALUES (NULL, @WorkspaceName, @ProcessPaths);";
                insertCommand.Parameters.AddWithValue("@WorkspaceName", workspaceNamw);
                insertCommand.Parameters.AddWithValue("@ProcessPaths", encodedProcessPaths);

                insertCommand.ExecuteReader();
            }
        }

        public static void UpdateWorkspace(string workspaceName, string encodedProcessPaths)
        {
            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();

                var updateCommand = new SqliteCommand();
                updateCommand.Connection = db;

                updateCommand.CommandText = $"UPDATE {tableName} set {fieldNames[2]}=@ProcessPaths where {fieldNames[1]}=@WorkspaceName;";
                updateCommand.Parameters.AddWithValue("@WorkspaceName", workspaceName);
                updateCommand.Parameters.AddWithValue("@ProcessPaths", encodedProcessPaths);


                updateCommand.ExecuteReader();
            }
        }

        public static List<string> GetWorkspaceNames()
        {
            var workspaceNames = new List<string>();

            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();
                var selectCommand = new SqliteCommand
                    ($"SELECT {fieldNames[1]} from {tableName}", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while(query.Read())
                {
                    workspaceNames.Add(query.GetString(0));
                }
            }

            return workspaceNames;
        }

        public static List<string> GetProcessPaths(string workspaceName)
        {
            var processPaths = new List<string>();

            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();
                var selectCommand = new SqliteCommand
                    ($"SELECT {fieldNames[2]} from {tableName} where {fieldNames[1]} = '{workspaceName}';", db);

                SqliteDataReader query = selectCommand.ExecuteReader();

                while (query.Read())
                {
                    foreach (string processPath in query.GetString(0).Split(","))
                    {
                        processPaths.Add(processPath);
                    }
                }
            }

            return processPaths;
        }

        public static void DeleteWorkspace(string workspaceName)
        {
            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();

                var deleteCommand = new SqliteCommand();
                deleteCommand.Connection = db;

                deleteCommand.CommandText = $"DELETE from {tableName} where {fieldNames[1]}=@WorkspaceName;";
                deleteCommand.Parameters.AddWithValue("@WorkspaceName", workspaceName);

                deleteCommand.ExecuteReader();
            }
        }
    }
}