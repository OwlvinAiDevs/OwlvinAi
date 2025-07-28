using UnityEngine;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Util.Store;
using System.IO;
using System.Threading;
using Google.Apis.Services;
using Google.Apis.Drive.v3;
using Google.Apis.Calendar.v3;


public class GoogleAuthenticator : MonoBehaviour
{
    public TextAsset clientSecretJson;

    // Scopes define the permissions the app is requesting from the user.
    private string[] Scopes = new string[]
    {
        // For access to UserSchedule.db on user's Google Drive
        DriveService.Scope.DriveAppdata,
        // For read/write access to the user's calendar events
        CalendarService.Scope.CalendarEvents
    };

    private UserCredential credential;

    public static CalendarService calendarService { get; private set; }
    public static DriveService driveService { get; private set; }
    public static bool IsAuthenticated { get; private set; } = false;

    /// <summary>
    /// Starts the Google authentication process. This can be called from a UI button.
    /// </summary>
    public async void AuthenticateGoogle()
    {
        if (clientSecretJson == null)
        {
            Debug.LogError("Client secret JSON file is not assigned in the Inspector.");
            return;
        }

        // The FileDataStore caches the user's access and refresh tokens to avoid
        // asking for permission every time the app launches.
        IDataStore dataStore = new FileDataStore(Path.Combine(Application.persistentDataPath, "GoogleAuthToken"));

        try
        {
            // Load client secrets from the provided JSON asset.
            GoogleClientSecrets clientSecrets = GoogleClientSecrets.FromStream(new MemoryStream(clientSecretJson.bytes));

            Debug.Log("Starting Google authentication...");
            // Trigger the OAuth 2.0 authorization flow.
            // This will open a browser for the user to sign in and grant permissions.
            credential = await GoogleWebAuthorizationBroker.AuthorizeAsync(
                clientSecrets.Secrets,
                Scopes,
                "user", // A unique identifier for the user.
                CancellationToken.None,
                dataStore
            );

            Debug.Log("Google authentication successful. User ID: " + credential.UserId);

            // Once authenticated, initialize the Google services.
            InitializeGoogleServices();
            IsAuthenticated = true;
        }
        catch (System.Exception ex)
        {
            IsAuthenticated = false;
            Debug.LogError($"Google authentication failed: {ex.Message}");
        }
    }

    /// <summary>
    /// Initializes the Google services (Drive, Calendar) using the user's credentials.
    /// </summary>
    private void InitializeGoogleServices()
    {
        if (credential != null)
        {
            // Create a base client service initializer.
            var baseClientInitializer = new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = "OwlvinAi",
            };

            // Initialize the Drive service.
            driveService = new DriveService(baseClientInitializer);
            Debug.Log("Google Drive service initialized successfully.");

            // Initialize the Calendar service.
            calendarService = new CalendarService(baseClientInitializer);
            Debug.Log("Google Calendar service initialized successfully.");

            // Initialize our static manager with the new service instance.
            GoogleCalendarManager.Initialize(calendarService);
        }
        else
        {
            Debug.LogError("Cannot initialize Google services without valid credentials.");
        }
    }
}
