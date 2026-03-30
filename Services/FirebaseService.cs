using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace BauCuaTomCa.Services;

public class FirebaseService
{
    public FirebaseService(IConfiguration config)
    {
        if (FirebaseApp.DefaultInstance != null) return;

        var jsonCredential = Environment.GetEnvironmentVariable("FIREBASE_CREDENTIALS");
        if (!string.IsNullOrEmpty(jsonCredential))
        {
            FirebaseApp.Create(new AppOptions
            {
                Credential = GoogleCredential.FromJson(jsonCredential)
            });
        }
        else
        {
            var credentialPath = config["Firebase:CredentialPath"];
            if (!string.IsNullOrEmpty(credentialPath) && System.IO.File.Exists(credentialPath))
            {
                FirebaseApp.Create(new AppOptions
                {
                    Credential = GoogleCredential.FromFile(credentialPath)
                });
            }
        }
    }

    public async Task<FirebaseToken?> VerifyTokenAsync(string idToken)
    {
        try
        {
            return await FirebaseAuth.DefaultInstance.VerifyIdTokenAsync(idToken);
        }
        catch
        {
            return null;
        }
    }
}
