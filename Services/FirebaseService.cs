using FirebaseAdmin;
using FirebaseAdmin.Auth;
using Google.Apis.Auth.OAuth2;

namespace BauCuaTomCa.Services;

public class FirebaseService
{
    public FirebaseService(IConfiguration config)
    {
        if (FirebaseApp.DefaultInstance != null) return;

        var credentialPath = config["Firebase:CredentialPath"];
        FirebaseApp.Create(new AppOptions
        {
            Credential = GoogleCredential.FromFile(credentialPath)
        });
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
