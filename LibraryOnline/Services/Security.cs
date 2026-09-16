using System;
using System.Security.Cryptography;
using LibraryOnline.Models;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;

namespace LibraryOnline.Services
{
    public class ModernPasswordHasher : IPasswordHasher
    {
        public string HashPassword(string password)
        {
            using (var rng = RandomNumberGenerator.Create())
            {
                var salt = new byte[16];
                rng.GetBytes(salt);
                using (
                    var k = new Rfc2898DeriveBytes(password, salt, 150000, HashAlgorithmName.SHA256)
                )
                    return "PBKDF2-SHA256$150000$"
                        + Convert.ToBase64String(salt)
                        + "$"
                        + Convert.ToBase64String(k.GetBytes(32));
            }
        }

        public PasswordVerificationResult VerifyHashedPassword(string hash, string password)
        {
            try
            {
                var p = hash.Split('$');
                if (p.Length != 4 || p[0] != "PBKDF2-SHA256")
                    return PasswordVerificationResult.Failed;
                using (
                    var k = new Rfc2898DeriveBytes(
                        password,
                        Convert.FromBase64String(p[2]),
                        int.Parse(p[1]),
                        HashAlgorithmName.SHA256
                    )
                )
                {
                    var a = k.GetBytes(32);
                    var b = Convert.FromBase64String(p[3]);
                    int diff = a.Length ^ b.Length;
                    for (int i = 0; i < a.Length && i < b.Length; i++)
                        diff |= a[i] ^ b[i];
                    return diff == 0
                        ? PasswordVerificationResult.Success
                        : PasswordVerificationResult.Failed;
                }
            }
            catch
            {
                return PasswordVerificationResult.Failed;
            }
        }
    }

    public static class Accounts
    {
        public static UserManager<AppUser> Manager(LibraryDb db)
        {
            return new UserManager<AppUser>(new UserStore<AppUser>(db))
            {
                PasswordHasher = new ModernPasswordHasher(),
                UserValidator = new UserValidator<AppUser>(
                    new UserManager<AppUser>(new UserStore<AppUser>(db))
                )
                {
                    AllowOnlyAlphanumericUserNames = false,
                    RequireUniqueEmail = true,
                },
                PasswordValidator = new PasswordValidator
                {
                    RequiredLength = 10,
                    RequireDigit = true,
                    RequireLowercase = true,
                    RequireUppercase = true,
                    RequireNonLetterOrDigit = true,
                },
                UserLockoutEnabledByDefault = true,
                MaxFailedAccessAttemptsBeforeLockout = 5,
                DefaultAccountLockoutTimeSpan = TimeSpan.FromMinutes(15),
            };
        }
    }
}
