namespace DLPManagementSystem.Common
{
    // Single source of truth for the password length rule and its bilingual message - referenced by
    // the [MinLength] annotations (defense in depth, in case a caller reaches a service method
    // without going through model binding) and by the explicit checks in UserService/AuthService,
    // which is what actually produces the message a real HTTP caller sees (see the
    // InvalidModelStateResponseFactory special-case in Program.cs for why the explicit checks alone
    // wouldn't be reachable otherwise).
    public static class PasswordPolicy
    {
        public const int MinLength = 10;

        public const string MessageEn = "Password must be at least 10 characters.";
        public const string MessageAr = "يجب أن تتكون كلمة المرور من 10 أحرف على الأقل.";
    }
}
