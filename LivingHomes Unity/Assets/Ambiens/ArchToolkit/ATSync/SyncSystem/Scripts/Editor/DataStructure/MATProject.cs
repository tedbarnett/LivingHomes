using System.IO;
using UnityEngine;
using System.Collections.Generic;
using System;
using Archtoolkit.ATImport.Utils;

namespace Archtoolkit.ATImport
{
    [Serializable]
    public class MATProject
    {
        public string projectName;
        
        public FileInfo fileInfo;

        public bool isOpenedInUI = false;
        
        public List<Avrs> views = new List<Avrs>();

        public MATProject(string projectName)
        {
            this.projectName = projectName;
        }

        public MATProject(string projectName,FileInfo fileInfo)
        {
            this.projectName = projectName;
           
            this.fileInfo = fileInfo;
        }
    }

    [Serializable]
    public class Avrs
    {
        public string name
        {
            get
            {
                return Path.GetFileNameWithoutExtension(this.projectPath);
            }
        }
        
        public string projectPath;

        public Texture2D thumbnail;

        public bool enableAutoSync = true;

        public bool isImporting = false;

        public GameObject root;

        public Action<Avrs> OnProjectChanged;

        public bool isOpened
        {
            get
            {
                return GameObject.Find(name + "_Root") != null;
            }
        }

        public bool autoSync = false;

        private FileSystemWatcher watcher = new FileSystemWatcher();

        public Avrs(string projectPath, Texture2D thumb)
        {
            this.projectPath = projectPath;
            this.thumbnail = thumb;

            TextureUtils.Bilinear(this.thumbnail, 32, 32);

            watcher.Path = Path.GetDirectoryName(this.projectPath);

            watcher.NotifyFilter = NotifyFilters.LastWrite;

            // Only watch text files.
            watcher.Filter = "*.avrs";

            // Add event handlers.
            watcher.Changed += Watcher_Changed;
            watcher.Created += Watcher_Changed;


            // Begin watching.
            watcher.EnableRaisingEvents = true;

            if (UnityEditor.EditorPrefs.HasKey(projectPath))
            {
                this.autoSync = UnityEditor.EditorPrefs.GetBool(projectPath);
            }
        }

        public void SetSyncTo(bool sync)
        {
            this.autoSync = sync;
            UnityEditor.EditorPrefs.SetBool(projectPath,this.autoSync);

        }

        private void Watcher_Changed(object sender, FileSystemEventArgs e)
        {
            if (autoSync)
            {
                if (this.OnProjectChanged != null)
                    this.OnProjectChanged(this);
            }
        }
    }

}