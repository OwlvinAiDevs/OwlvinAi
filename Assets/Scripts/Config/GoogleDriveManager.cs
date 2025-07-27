using UnityEngine;
using Google.Apis.Drive.v3;
using Google.Apis.Drive.v3.Data;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

public class DriveManager : MonoBehaviour
{
    private DriveService _driveService;
    private const string DatabaseFileName = "UserSchedule.db";
    private const string AppDataFolderMimeType = "application/vnd.google-apps.folder";
    private const string DatabaseMimeType = "application/x-sqlite3";

    public void SetDriveService(DriveService driveService)
    {
        _driveService = driveService;
    }

    // Called after authentication and service initialization
    public async Task SyncDatabaseToGoogleDrive()
    {
        if (_driveService == null)
        {
            Debug.LogError("Drive service is not initialized.");
            return Task.CompletedTask;
        }

        // Check if the database file exists on Google Drive (in app data folder)
        Google.Apis.Drive.v3.Data.File driveFile = await GetFileFromAppDataFolder(DatabaseFileName);

        if (driveFile == null)
        {
            Debug.Log("Database file not found in Google Drive app data folder. Uploading new file...");
            await UploadDatabase();
        }
        else
        {
            Debug.Log("Database file found in Google Drive app data folder. Checking for updates...");

            // Compare timestamps or content hashes to decide sync direction
            string localDbPath = Path.Combine(Application.persistentDataPath, DatabaseFileName);
            if (System.IO.File.Exists(localDbPath))
            {
                System.DateTime localDbLastModified = System.IO.File.GetLastWriteTimeUtc(localDbPath);
                System.DateTime driveDbLastModified = driveFile.ModifiedTime.HasValue ? driveFile.ModifiedTime.Value.ToUniversalTime() : System.DateTime.MinValue;

                if (localDbLastModified > driveDbLastModified)
                {
                    Debug.Log("Local database is newer. Updating Google Drive...");
                    await UpdateDatabase(driveFile.Id, localDbPath);
                }
                else if (driveDbLastModified > localDbLastModified)
                {
                    Debug.Log("Google Drive database is newer. Downloading to local...");
                    await DownloadDatabase(driveFile.Id, localDbPath);
                }
                else
                {
                    Debug.Log("Both databases are up-to-date.");
                }
            }
            else
            {
                Debug.Log("Local database file does not exist. Downloading from Google Drive...");
                await DownloadDatabase(driveFile.Id, localDbPath);
            }
        }
    }

    // Helper method to get a file from the app data folder
    private async Task<Google.Apis.Drive.v3.Data.File> GetFileFromAppDataFolder(string fileName)
    {
        try
        {
            // Query for the file in the app data folder
            FilesResource.ListRequest listRequest = _driveService.Files.List();
            listRequest.Spaces = "appDataFolder";
            listRequest.Fields = "files(id, name, modifiedTime)";
            listRequest.Q = $"name='{fileName}' and trashed=false";

            FileList fileList = await listRequest.ExecuteAsync();
            if (fileList.Files != null && fileList.Files.Count > 0)
            {
                return fileList.Files[0]; // Return the first matching file
            }
            return null; // File not found
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error retrieving file from Google Drive app data folder: {ex.Message}");
            return null; // Return null if an error occurs
        }
    }

    // Upload the local database file to Google Drive (creates new if it doesn't exist)
    public async Task UploadDatabase()
    {
        string localDbPath = Path.Combine(Application.persistentDataPath, DatabaseFileName);
        if (!System.IO.File.Exists(localDbPath))
        {
            Debug.LogError($"Local database not found at path: {localDbPath}");
            return;
        }

        try
        {
            Google.Apis.Drive.v3.Data.File fileMetadata = new Google.Apis.Drive.v3.Data.File()
            {
                Name = DatabaseFileName,
                Parents = new List<string> { "appDataFolder" }
            };

            using (var stream = new FileStream(localDbPath, FileMode.Open, FileAccess.Read))
            {
                FilesResource.CreateMediaUpload request = _driveService.Files.Create(fileMetadata, stream, DatabaseMimeType);
                request.Fields = "id, name";
                await request.UploadAsync();

                Google.Apis.Drive.v3.Data.File uploadedFile = request.ResponseBody;
                Debug.Log($"Database uploaded successfully. File ID: {uploadedFile.Id}, Name: {uploadedFile.Name}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error uploading database to Google Drive: {ex.Message}");
        }
    }

    // Update the existing database file on Google Drive
    public async Task UpdateDatabase(string fileId, string localDbPath)
    {
        if (!System.IO.File.Exists(localDbPath))
        {
            Debug.LogError($"Local database not found at path: {localDbPath}");
            return;
        }

        try
        {
            Google.Apis.Drive.v3.Data.File fileMetadata = new Google.Apis.Drive.v3.Data.File(); // No need to set name and parents, it will remain the same
            using (var stream = new FileStream(localDbPath, FileMode.Open, FileAccess.Read))
            {
                FilesResource.UpdateMediaUpload request = _driveService.Files.Update(fileMetadata, fileId, stream, DatabaseMimeType);
                request.Fields = "id, name, modifiedTime";
                await request.UploadAsync();

                Google.Apis.Drive.v3.Data.File updatedFile = request.ResponseBody;
                Debug.Log($"Database updated successfully. File ID: {updatedFile.Id}, Name: {updatedFile.Name}, Modified Time: {updatedFile.ModifiedTime}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error updating database on Google Drive: {ex.Message}");
        }
    }

    // Download the database file from Google Drive to local storage
    public async Task DownloadDatabase(string fileId, string localDbPath)
    {
        try
        {
            using (var stream = new FileStream(localDbPath, FileMode.Create, FileAccess.Write))
            {
                FilesResource.GetRequest request = _driveService.Files.Get(fileId);
                await request.DownloadAsync(stream);
                Debug.Log($"Database downloaded successfully to: {localDbPath}");
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogError($"Error downloading database from Google Drive: {ex.Message}");
        }
    }

    // Delete the database file from Google Drive
    public async Task DeleteDatabaseFromDrive()
    {
        if (_driveService == null) return;

        Google.Apis.Drive.v3.Data.File driveFile = await GetFileFromAppDataFolder(DatabaseFileName);
        if (driveFile != null)
        {
            try
            {
                await _driveService.Files.Delete(driveFile.Id).ExecuteAsync();
                Debug.Log($"Database file deleted from Google Drive: {driveFile.Name}");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"Error deleting database from Google Drive: {ex.Message}");
            }
        }
        else
        {
            Debug.Log("No database file found in Google Drive app data folder to delete.");
        }
    }
}