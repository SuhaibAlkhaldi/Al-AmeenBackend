namespace DLPManagementSystem.Service.Interface
{
    // Encrypts a file version's extracted text at rest (see FileVersionContent). Kept behind an interface so the
    // diff logic and tests never depend on where the DataProtection key ring lives.
    public interface IFileVersionTextProtector
    {
        string Protect(string plainText);

        string Unprotect(string protectedText);
    }
}
