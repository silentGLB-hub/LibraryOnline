using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Security.Cryptography;
using LibraryOnline.Models;
using Microsoft.AspNet.Identity;

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
        public static AccountService Manager(LibraryDb db) => new AccountService(db);
    }

    // One user has one role. Password hashing and lockout remain enforced.
    public class AccountService
    {
        readonly LibraryDb db;
        readonly ModernPasswordHasher hasher = new ModernPasswordHasher();

        public AccountService(LibraryDb db)
        {
            this.db = db;
        }

        public AppUser FindByEmail(string email) => db.Users.SingleOrDefault(x => x.Email == email);

        public IList<string> GetRoles(string id) => new[] { db.Users.Find(id).Role };

        public bool IsLockedOut(string id) => db.Users.Find(id).LockoutEndDateUtc > DateTime.UtcNow;

        public bool CheckPassword(AppUser u, string password) =>
            hasher.VerifyHashedPassword(u.PasswordHash, password)
            != PasswordVerificationResult.Failed;

        public void AccessFailed(string id)
        {
            var u = db.Users.Find(id);
            if (++u.AccessFailedCount >= 5)
            {
                u.LockoutEndDateUtc = DateTime.UtcNow.AddMinutes(15);
                u.AccessFailedCount = 0;
            }
            db.SaveChanges();
        }

        public void ResetAccessFailedCount(string id)
        {
            var u = db.Users.Find(id);
            u.AccessFailedCount = 0;
            u.LockoutEndDateUtc = null;
            db.SaveChanges();
        }

        IdentityResult ValidatePassword(string password) =>
            new PasswordValidator
            {
                RequiredLength = 10,
                RequireDigit = true,
                RequireLowercase = true,
                RequireUppercase = true,
                RequireNonLetterOrDigit = true,
            }
                .ValidateAsync(password)
                .GetAwaiter()
                .GetResult();

        public IdentityResult Create(AppUser u, string password)
        {
            if (string.IsNullOrWhiteSpace(u.Email) || !new EmailAddressAttribute().IsValid(u.Email))
                return IdentityResult.Failed("Email không hợp lệ.");
            u.Email = u.Email.Trim();
            if (db.Users.Any(x => x.Email == u.Email))
                return IdentityResult.Failed("Email đã được sử dụng.");
            var result = ValidatePassword(password);
            if (!result.Succeeded)
                return result;
            u.PasswordHash = hasher.HashPassword(password);
            db.Users.Add(u);
            db.SaveChanges();
            return IdentityResult.Success;
        }

        public IdentityResult AddToRole(string id, string role)
        {
            if (!new[] { "Member", "Librarian", "Administrator" }.Contains(role))
                return IdentityResult.Failed("Vai trò không hợp lệ.");
            var u = db.Users.Find(id);
            u.Role = role;
            db.SaveChanges();
            return IdentityResult.Success;
        }

        public IdentityResult ChangePassword(string id, string oldPassword, string newPassword)
        {
            var u = db.Users.Find(id);
            if (!CheckPassword(u, oldPassword))
                return IdentityResult.Failed("Mật khẩu cũ không đúng.");
            var result = ValidatePassword(newPassword);
            if (!result.Succeeded)
                return result;
            u.PasswordHash = hasher.HashPassword(newPassword);
            u.SecurityStamp = Guid.NewGuid().ToString();
            db.SaveChanges();
            return IdentityResult.Success;
        }
    }
}
