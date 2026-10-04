using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;

namespace LibraryOnline.Models
{
    [Table("Users")]
    public class AppUser
    {
        [Key, StringLength(128)]
        public string Id { get; set; } = Guid.NewGuid().ToString();

        [Required, StringLength(256), Index(IsUnique = true)]
        public string Email { get; set; }

        [Required, StringLength(20)]
        public string Role { get; set; } = "Member";
        public string PasswordHash { get; set; }
        public string SecurityStamp { get; set; } = Guid.NewGuid().ToString();
        public string PhoneNumber { get; set; }
        public int AccessFailedCount { get; set; }
        public DateTime? LockoutEndDateUtc { get; set; }

        [Required, StringLength(100)]
        public string FullName { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }

    public class Category
    {
        public int Id { get; set; }

        [Required, StringLength(80), Index(IsUnique = true)]
        public string Name { get; set; }
    }

    public class Book
    {
        public int Id { get; set; }

        [Required, StringLength(200)]
        public string Title { get; set; }

        [Required, StringLength(150)]
        public string Author { get; set; }

        [Required, StringLength(150)]
        public string Publisher { get; set; }

        [Range(1000, 2100)]
        public int PublicationYear { get; set; }

        [Required, StringLength(20), Index(IsUnique = true)]
        public string ISBN { get; set; }

        [Required, StringLength(5000)]
        public string Description { get; set; }

        [StringLength(1000)]
        public string CoverImage { get; set; }
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }

        [Range(1, 10000)]
        public int TotalCopies { get; set; }
        public int AvailableCopies { get; set; }
        public bool IsActive { get; set; } = true;

        [Timestamp]
        public byte[] RowVersion { get; set; }
    }

    public enum LoanStatus
    {
        Reserved,
        Borrowed,
        Returned,
        Overdue,
        Cancelled,
    }

    public class Loan
    {
        public int Id { get; set; }

        [Required]
        public string MemberId { get; set; }
        public virtual AppUser Member { get; set; }
        public int BookId { get; set; }
        public virtual Book Book { get; set; }
        public LoanStatus Status { get; set; }
        public DateTime ReservedAt { get; set; }
        public DateTime PickupExpiresAt { get; set; }
        public DateTime? BorrowedAt { get; set; }
        public DateTime? DueAt { get; set; }
        public DateTime? ReturnedAt { get; set; }
        public decimal DailyFineRate { get; set; }
        public int RenewalCount { get; set; }
    }

    public class Fine
    {
        public int Id { get; set; }

        [Index(IsUnique = true)]
        public int LoanId { get; set; }
        public virtual Loan Loan { get; set; }
        public decimal Amount { get; set; }
        public decimal Paid { get; set; }
    }

    public class FinePayment
    {
        public int Id { get; set; }
        public int FineId { get; set; }
        public virtual Fine Fine { get; set; }
        public decimal Amount { get; set; }
        public DateTime PaidAt { get; set; }
        public string RecordedBy { get; set; }

        [StringLength(200)]
        public string Note { get; set; }
    }

    public enum RenewalStatus
    {
        Pending,
        Approved,
        Rejected,
    }

    public class RenewalRequest
    {
        public int Id { get; set; }
        public int LoanId { get; set; }
        public virtual Loan Loan { get; set; }
        public DateTime RequestedAt { get; set; }
        public RenewalStatus Status { get; set; }

        [StringLength(400)]
        public string Note { get; set; }
        public DateTime? DecidedAt { get; set; }
    }

    public class Notification
    {
        public int Id { get; set; }

        [Required]
        public string MemberId { get; set; }

        [Required, StringLength(200), Index(IsUnique = true)]
        public string EventKey { get; set; }

        [Required, StringLength(500)]
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }

    public class ReadingList
    {
        public int Id { get; set; }

        [Required]
        public string MemberId { get; set; }

        [Required, StringLength(80)]
        public string Name { get; set; }
    }

    public class ReadingListItem
    {
        public int Id { get; set; }

        [Index("IX_ListBook", 1, IsUnique = true)]
        public int ReadingListId { get; set; }
        public virtual ReadingList ReadingList { get; set; }

        [Index("IX_ListBook", 2, IsUnique = true)]
        public int BookId { get; set; }
        public virtual Book Book { get; set; }
    }

    public class GenrePreference
    {
        public int Id { get; set; }

        [Required, StringLength(128), Index("IX_MemberGenre", 1, IsUnique = true)]
        public string MemberId { get; set; }

        [Index("IX_MemberGenre", 2, IsUnique = true)]
        public int CategoryId { get; set; }
        public virtual Category Category { get; set; }
    }

    public class LibraryPolicy
    {
        public int Id { get; set; }
        public int MaxActiveLoans { get; set; } = 5;
        public int LoanDays { get; set; } = 14;
        public int PickupDays { get; set; } = 3;
        public int RenewalDays { get; set; } = 7;
        public int MaxRenewals { get; set; } = 2;
        public decimal FinePerDay { get; set; } = 5000;

        [Required, StringLength(100)]
        public string LibraryName { get; set; } = "Thư viện Mở";
    }

    public class LibraryDb : DbContext
    {
        public LibraryDb()
            : base("LibraryDb")
        {
            Configuration.LazyLoadingEnabled = false;
            Configuration.ProxyCreationEnabled = false;
        }

        public DbSet<AppUser> Users { get; set; }
        public DbSet<Category> Categories { get; set; }
        public DbSet<Book> Books { get; set; }
        public DbSet<Loan> Loans { get; set; }
        public DbSet<Fine> Fines { get; set; }
        public DbSet<FinePayment> FinePayments { get; set; }
        public DbSet<RenewalRequest> Renewals { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<ReadingList> ReadingLists { get; set; }
        public DbSet<ReadingListItem> ReadingListItems { get; set; }
        public DbSet<GenrePreference> GenrePreferences { get; set; }
        public DbSet<LibraryPolicy> Policies { get; set; }

        protected override void OnModelCreating(DbModelBuilder b)
        {
            base.OnModelCreating(b);
            b.Conventions.Remove<System.Data.Entity.ModelConfiguration.Conventions.OneToManyCascadeDeleteConvention>();
            b.Entity<AppUser>().Property(x => x.Email).HasMaxLength(256);
        }
    }
}
