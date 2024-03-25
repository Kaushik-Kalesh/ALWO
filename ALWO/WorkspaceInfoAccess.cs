using Microsoft.Data.Sqlite;
using Microsoft.UI.Xaml;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Windows.Storage;
using WindowsDesktop;

namespace ALWO
{
    public static class WorkspaceInfoAccess
    {
        private static string dbFileName = "workspaces_info.db";
        private static string dbPath;
        private static string tableName = "Workspaces";
        private interface FieldNames
        {
            const string PrimaryKey = "Primary_Key";
            const string WorkspaceName = "Workspace_Name";
            const string VDName = "VD_Name";
            const string VDInterval = "VD_Interval";
            const string ProcessName = "Process_Name";
            const string ProcessPath = "Process_Path";
        };

        public async static void InitializeDatabase()
        {
            await ApplicationData.Current.LocalFolder
                    .CreateFileAsync(dbFileName, CreationCollisionOption.OpenIfExists);           
            dbPath = Path.Combine(ApplicationData.Current.LocalFolder.Path, dbFileName);            

            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();

                string tableCommand = "CREATE TABLE IF NOT EXISTS Workspaces (Primary_Key INTEGER PRIMARY KEY, Workspace_Name NVARCHAR(20), " +
                                      "VD_Name NVARCHAR(20) NULL, VD_Interval INTEGER NULL, Process_Name NVARCHAR(20) NULL, Process_Path NVARCHAR(20) NULL);";

                var createTable = new SqliteCommand(tableCommand, db);

                createTable.ExecuteReader();
            }
        }

        public static void UpdateWorkspace(string workspaceName, ObservableCollection<ProcessItem> virtualDesktops)
        {
            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();

                foreach (var vd in virtualDesktops)
                {
                    foreach (var process in vd.Children)
                    {
                        var insertCommand = new SqliteCommand();
                        insertCommand.Connection = db;

                        insertCommand.CommandText = $"INSERT INTO {tableName} VALUES (NULL, @WorkspaceName, @VDName, @VDInterval, @ProcessName, @ProcessPath);";
                        insertCommand.Parameters.AddWithValue("@WorkspaceName", workspaceName);
                        insertCommand.Parameters.AddWithValue("@VDName", vd.VDName);
                        insertCommand.Parameters.AddWithValue("@VDInterval", vd.TimeInterval);
                        insertCommand.Parameters.AddWithValue("@ProcessName", process.ProcessName);
                        insertCommand.Parameters.AddWithValue("@ProcessPath", process.ProcessPath);

                        insertCommand.ExecuteReader();
                    }

                    if (vd.Children.Count == 0)
                    {
                        var insertCommand = new SqliteCommand();
                        insertCommand.Connection = db;

                        insertCommand.CommandText = $"INSERT INTO {tableName} VALUES (NULL, @WorkspaceName, @VDName, NULL, NULL, NULL);";
                        insertCommand.Parameters.AddWithValue("@WorkspaceName", workspaceName);
                        insertCommand.Parameters.AddWithValue("@VDName", vd.VDName);

                        insertCommand.ExecuteReader();
                    }
                }
            }
        }


        public static void DeleteWorkspace(string workspaceName)
        {
            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();

                var deleteCommand = new SqliteCommand();
                deleteCommand.Connection = db;

                deleteCommand.CommandText = $"DELETE FROM {tableName} WHERE {FieldNames.WorkspaceName}=@WorkspaceName;";
                deleteCommand.Parameters.AddWithValue("@WorkspaceName", workspaceName);

                deleteCommand.ExecuteReader();
            }
        }

        public static List<string> GetWorkspaceNames()
        {
            var workspaceNames = new List<string>();

            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();
                try
                {
                    var selectCommand = new SqliteCommand
                        ($"SELECT DISTINCT {FieldNames.WorkspaceName} FROM {tableName}", db);

                    SqliteDataReader query = selectCommand.ExecuteReader();

                    while (query.Read())
                    {
                        workspaceNames.Add(query.GetString(0));
                    }
                }
                catch { }
            }

            return workspaceNames;
        }

        public static ObservableCollection<ProcessItem> GetVirtualDesktops(string workspaceName)
        {
            var virtualDestops = new ObservableCollection<ProcessItem>();

            VirtualDesktop.GetDesktops().ToList().ForEach(e => virtualDestops.Add(new ProcessItem
            {
                VDName = e.Name,      
                TimeInterval = 1,
                IsVDNameEditable = false,
                EditToolsVisibility = Visibility.Collapsed
            }));

            using (var db = new SqliteConnection($"Filename={dbPath}"))
            {
                db.Open();

                foreach (var vd in virtualDestops)
                {
                    var selectCommand = new SqliteCommand($"SELECT {FieldNames.VDInterval}, {FieldNames.ProcessName}, {FieldNames.ProcessPath} " +
                                                 $"FROM {tableName} WHERE {FieldNames.VDName} = '{vd.VDName}' " +
                                                 $"AND {FieldNames.WorkspaceName}='{workspaceName}';", db);


                    SqliteDataReader query = selectCommand.ExecuteReader();
                    while (query.Read())
                    {
                        if(query.IsDBNull(0)) { continue; }

                        vd.Children.Add(new ProcessItem
                        {
                            VDName = vd.VDName,
                            TimeInterval = query.GetInt32(0),
                            ProcessName = query.GetString(1),
                            ProcessPath = query.GetString(2),
                            IsVDNameEditable = false,
                            EditToolsVisibility = Visibility.Collapsed
                        });
                    }
                }
            }

            return virtualDestops;
        }
    }
}