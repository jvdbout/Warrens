﻿using NetMud.DataStructure.Base.System;
using System;
using System.IO;
using System.Web.Hosting;

namespace NetMud.DataAccess.FileSystem
{
    public class BackingData : FileAccessor
    {
        /// <summary>
        /// Root directory where all the backup stuff gets saved too
        /// </summary>
        public override string BaseDirectory
        {
            get
            {
                return HostingEnvironment.MapPath("BackingData/");
            }
        }

        /// <summary>
        /// The default directory name for when files are rolled over or archived
        /// </summary>
        public override string DatedBackupDirectory
        {
            get
            {
                return String.Format("{0}{1}{2}{3}_{4}{5}/",
                                        ArchiveDirectoryName
                                        , DateTime.Now.Year
                                        , DateTime.Now.Month
                                        , DateTime.Now.Day
                                        , DateTime.Now.Hour
                                        , DateTime.Now.Minute);
            }
        }

        public void WriteEntity(IEntityBackingData entity)
        {
            var dirName = BaseDirectory + entity.GetType().Name + CurrentDirectoryName;

            if (!VerifyDirectory(dirName))
                throw new Exception("Unable to locate or create base live data directory.");

            var entityFileName = GetEntityFilename(entity);

            if (string.IsNullOrWhiteSpace(entityFileName))
                return;

            var fullFileName = dirName + entityFileName;
            var archiveFileDirectory = BaseDirectory + entity.GetType().Name + DatedBackupDirectory;

            try
            {
                RollingArchiveFile(fullFileName, archiveFileDirectory + entityFileName, archiveFileDirectory);
                WriteToFile(fullFileName, entity.Serialize());
            }
            catch (Exception ex)
            {
                LoggingUtility.LogError(ex);
            }
        }

        /// <summary>
        /// Creates rolling files since backing data is dated by minute
        /// </summary>
        /// <param name="currentFile">full path of current file name</param>
        /// <param name="archiveFile">full path of archive file name</param>
        /// <param name="archiveDirectory">archive directory</param>
        private void RollingArchiveFile(string currentFile, string archiveFile, string archiveDirectory)
        {
            if (File.Exists(archiveFile))
            {
                var archiveDir = new DirectoryInfo(archiveDirectory);
                var count = archiveDir.GetFiles(archiveFile + ".*").Length;

                File.Move(archiveFile, String.Format("{0}.{1}", archiveFile + count + 1));
            }

            File.Move(currentFile, archiveFile);
        }

        /// <summary>
        /// Gets the statically formatted filename for an entity
        /// </summary>
        /// <param name="entity">The entity in question</param>
        /// <returns>the filename</returns>
        private string GetEntityFilename(IEntityBackingData entity)
        {
            return String.Format("{0}.{1}", entity.ID, entity.GetType().Name);
        }
    }
}
