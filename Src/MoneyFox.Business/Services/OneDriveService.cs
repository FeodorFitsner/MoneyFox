﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.OneDrive.Sdk;
using MoneyFox.Business.Extensions;
using MoneyFox.Foundation.Constants;
using MoneyFox.Foundation.Interfaces;
using MvvmCross.Platform;
using MvvmCross.Platform.Platform;
using MvvmCross.Plugins.File;

namespace MoneyFox.Business.Services
{
    public class OneDriveService : IBackupService
    {
        private readonly IMvxFileStore fileStore;
        private readonly IOneDriveAuthenticator oneDriveAuthenticator;

        public OneDriveService(IMvxFileStore fileStore, IOneDriveAuthenticator oneDriveAuthenticator)
        {
            this.fileStore = fileStore;
            this.oneDriveAuthenticator = oneDriveAuthenticator;
        }

        private IOneDriveClient OneDriveClient { get; set; }

        private Item BackupFolder { get; set; }
        private Item ArchiveFolder { get; set; }

        /// <summary>
        ///     Login User to OneDrive.
        /// </summary>
        public async Task Login()
        {
            OneDriveClient = await oneDriveAuthenticator.LoginAsync();
        }

        /// <summary>
        ///     Logout User from OneDrive.
        /// </summary>
        public async Task Logout()
        {
            await oneDriveAuthenticator.LogoutAsync();
        }

        /// <summary>
        ///     Uploads a copy of the current database.
        /// </summary>
        /// <returns>Returns a TaskCompletionType which indicates if the task was successful or not</returns>
        public async Task<bool> Upload()
        {
            if (OneDriveClient == null)
            {
                OneDriveClient = await oneDriveAuthenticator.LoginAsync();
            }

            await LoadBackupFolder();
            await LoadArchiveFolder();

            await DeleteCleanupOldBackups();
            await ArchiveCurrentBackup();

            using (var dbstream = fileStore.OpenRead(DatabaseConstants.DB_NAME))
            {
                var uploadedItem = await OneDriveClient
                    .Drive
                    .Root
                    .ItemWithPath(Path.Combine(DatabaseConstants.BACKUP_FOLDER_NAME,
                                                DatabaseConstants.BACKUP_NAME))
                    .Content
                    .Request()
                    .PutAsync<Item>(dbstream);

                return uploadedItem != null;
            }
        }

        private async Task DeleteCleanupOldBackups()
        {
            var archiveBackups = await OneDriveClient.Drive.Items[ArchiveFolder?.Id].Children.Request().GetAsync();

            if(archiveBackups.Count < 5) return;
            var oldestBackup = archiveBackups.OrderByDescending(x => x.CreatedDateTime).Last();

            await OneDriveClient.Drive.Items[oldestBackup?.Id].Request().DeleteAsync();
        }

        private async Task ArchiveCurrentBackup()
        {
            var backups = await OneDriveClient.Drive.Items[BackupFolder?.Id].Children.Request().GetAsync();
            var currentBackup = backups.FirstOrDefault(x => x.Name == DatabaseConstants.BACKUP_NAME);

            if (currentBackup == null) return;

            var updateItem = new Item
            {
                ParentReference = new ItemReference {Id = ArchiveFolder.Id},
                Name = string.Format(DatabaseConstants.BACKUP_ARCHIVE_NAME, DateTime.Now.ToString("yyyy-M-d_hh-mm-ssss"))
            };

            var itemWithUpdates = await OneDriveClient
                .Drive
                .Items[currentBackup.Id]
                .Request()
                .UpdateAsync(updateItem);
        }

        /// <summary>
        ///     Restores the file with the passed name
        /// </summary
        /// <param name="backupname">Name of the backup to restore</param>
        /// <param name="dbName">filename in which the database shall be restored.</param>
        /// <returns>TaskCompletionType which indicates if the task was successful or not</returns>
        public async Task Restore(string backupname, string dbName)
        {
            if (OneDriveClient == null)
            {
                OneDriveClient = await oneDriveAuthenticator.LoginAsync();
            }

            await LoadBackupFolder();

            var children = await OneDriveClient.Drive.Items[BackupFolder?.Id].Children.Request().GetAsync();
            var existingBackup = children.FirstOrDefault(x => x.Name == backupname);

            if (existingBackup != null)
            {
                var backup = await OneDriveClient.Drive.Items[existingBackup.Id].Content.Request().GetAsync();
                if (fileStore.Exists(dbName))
                {
                    fileStore.DeleteFile(dbName);
                }
                fileStore.WriteFile(dbName, backup.ReadToEnd());
            }
        }

        /// <summary>
        ///     Get's the modification date for the existing backup.
        ///     If there is no backup yet, it will return <see cref="DateTime.MinValue" />
        /// </summary>
        /// <returns>Date of the last backup.</returns>
        public async Task<DateTime> GetBackupDate()
        {
            if (OneDriveClient == null)
            {
                OneDriveClient = await oneDriveAuthenticator.LoginAsync();
            }

            await LoadBackupFolder();

            try
            {
                var children = await OneDriveClient.Drive.Items[BackupFolder?.Id].Children.Request().GetAsync();
                var existingBackup = children.FirstOrDefault(x => x.Name == DatabaseConstants.BACKUP_NAME);

                if (existingBackup != null)
                {
                    return existingBackup.LastModifiedDateTime?.DateTime ?? DateTime.MinValue;
                }
            }
            catch (Exception ex)
            {
                Mvx.Trace(MvxTraceLevel.Error, ex.Message);
            }

            return DateTime.MinValue;
        }

        /// <summary>
        ///     Gets a list with all the filenames who are available in the backup folder.
        ///     The name of the backupfolder is defined in the Constants.
        /// </summary>
        /// <returns>A list with all filenames.</returns>
        public async Task<List<string>> GetFileNames()
        {
            if (OneDriveClient == null)
            {
                OneDriveClient = await oneDriveAuthenticator.LoginAsync();
            }

            await LoadBackupFolder();

            var children = await OneDriveClient.Drive.Items[BackupFolder?.Id].Children.Request().GetAsync();
            return children.Select(x => x.Name).ToList();
        }

        private async Task LoadBackupFolder()
        {
            if (BackupFolder != null)
            {
                return;
            }

            var children = await OneDriveClient.Drive.Root.Children.Request().GetAsync();
            BackupFolder =
                children.CurrentPage.FirstOrDefault(x => x.Name == DatabaseConstants.BACKUP_FOLDER_NAME);

            if (BackupFolder == null)
            {
                await CreateBackupFolder();
            }
        }

        private async Task CreateBackupFolder()
        {
            var folderToCreate = new Item
            {
                Name = DatabaseConstants.BACKUP_FOLDER_NAME,
                Folder = new Folder()
            };

            var root = await OneDriveClient.Drive.Root.Request().GetAsync();

            BackupFolder = await OneDriveClient.Drive.Items[root.Id].Children.Request()
                .AddAsync(folderToCreate);
        }

        private async Task LoadArchiveFolder()
        {
            if (ArchiveFolder != null)
            {
                return;
            }

            var children = await OneDriveClient.Drive.Root.Children.Request().GetAsync();
            ArchiveFolder = children.CurrentPage.FirstOrDefault(x => x.Name == DatabaseConstants.ARCHIVE_FOLDER_NAME);

            if (ArchiveFolder == null)
            {
                await CreateArchiveFolder();
            }
        }

        private async Task CreateArchiveFolder()
        {
            var folderToCreate = new Item
            {
                Name = DatabaseConstants.ARCHIVE_FOLDER_NAME,
                Folder = new Folder()
            };

            ArchiveFolder =
                await OneDriveClient.Drive.Items[BackupFolder?.Id].Children.Request().AddAsync(folderToCreate);
        }
    }
}