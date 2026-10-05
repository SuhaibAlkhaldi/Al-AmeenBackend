using DLPManagementSystem.Service.Interface;
using Microsoft.AspNetCore.DataProtection;

namespace DLPManagementSystem.Service.Service
{
    // Uses its own purpose string, so a payload protected here can never be unprotected by another feature's protector
    // (for example FileKeyProtectionService's "CompanyDlp.FileKeys") even though they share one key ring.
    public sealed class FileVersionTextProtector : IFileVersionTextProtector
    {
        private readonly IDataProtector _protector;

        public FileVersionTextProtector(IDataProtectionProvider dataProtectionProvider)
        {
            _protector = dataProtectionProvider.CreateProtector("CompanyDlp.FileVersionContent");
        }

        public string Protect(string plainText) => _protector.Protect(plainText);

        public string Unprotect(string protectedText) => _protector.Unprotect(protectedText);
    }
}
