using UnityEngine;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;


public class GoogleAuthenticator : MonoBehaviour
{
    public TextAsset clientSecretJson;

    private string[] Scopes = new string[]
    {
        Google.Apis.Drive.v3.DriveService.Scope.DriveAppdata, // For access to UserSchedule.db on user's Google Drive
        Google.Apis.Calendar.v3.CalendarService.Scope.CalendarEvents // For access to user's calendar events
    };

    private UserCredential credential;

    // Start the authentication process
    public async void AuthenticateGoogle()
    {
        if (clientSecretJson == null)
        {
            Debug.LogError("Client secret JSON file is not set.");
            return;
        }

        IDataStore dataStore = new FileDataStore(Path.Combine(Application.persistentDataPath, "GoogleAuthToken"));

        try
        {
            // Open a browser window for user authentication
            GoogleClientSecrets clientSecrets = GoogleClientSecrets.FromStream(new MemoryStream(clientSecretJson.bytes));
            Debug.Log("Starting Google authentication...");
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets.Secrets,
                Scopes,
                "user", // User identifier, can be any string
                CancellationToken.None,
                dataStore
            );

            Debug.Log("Google authentication successful. User ID: " + credential.UserId);

            // Initialize Google services with the authenticated credential
            InitializeGoogleServices();
        }
        catch (System.Exception ex)
        {
            Debug.LogError("Google authentication failed: " + ex.Message);
        }
    }

    private void InitializeGoogleServices()
    {
        if (credential != null)
        {
            // Initialize Google services with the authenticated credential
            var driveService = new Google.Apis.Drive.v3.DriveService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "OwlvinAi"
            });
            Debug.Log("Google Drive service initialized.");

            var calendarService = new Google.Apis.Calendar.v3.CalendarService(new Google.Apis.Services.BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "OwlvinAi"
            });
            Debug.Log("Google Calendar service initialized.");
        }
    }
}
