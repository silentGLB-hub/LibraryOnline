using System;
using System.Data;
using System.Data.Entity;
using System.Linq;
using LibraryOnline.Models;

namespace LibraryOnline.Services
{
    public class RuleException : Exception
    {
        public RuleException(string message)
            : base(message) { }
    }

    public sealed class LibraryService : IDisposable
    {
        public readonly LibraryDb Db = new LibraryDb();

        public void Dispose()
        {
            Db.Dispose();
        }

        public T Write<T>(Func<T> action)
        {
            using (var tx = Db.Database.BeginTransaction(IsolationLevel.Serializable))
            {
                Db.Database.ExecuteSqlCommand(
                    "DECLARE @r int; EXEC @r = sp_getapplock @Resource=N'LibraryOnline.Circulation', @LockMode='Exclusive', @LockOwner='Transaction', @LockTimeout=15000; IF @r < 0 THROW 51000, 'Library is busy. Retry.', 1;"
                );
                var result = action();
                Db.SaveChanges();
                tx.Commit();
                return result;
            }
        }

        public LibraryPolicy Policy => Db.Policies.Single();

        public void Notify(string member, string key, string message)
        {
            if (
                !Db.Notifications.Any(n => n.EventKey == key)
                && !Db.Notifications.Local.Any(n => n.EventKey == key)
            )
                Db.Notifications.Add(
                    new Notification
                    {
                        MemberId = member,
                        EventKey = key,
                        Message = message,
                        CreatedAt = DateTime.UtcNow,
                    }
                );
        }

        public void Sweep()
        {
            Write(() =>
            {
                var now = DateTime.UtcNow;
                var today = now.Date;
                foreach (
                    var l in Db
                        .Loans.Include(x => x.Book)
                        .Where(x =>
                            x.Status == LoanStatus.Reserved
                            || x.Status == LoanStatus.Borrowed
                            || x.Status == LoanStatus.Overdue
                        )
                        .ToList()
                )
                {
                    if (l.Status == LoanStatus.Reserved)
                    {
                        if (l.PickupExpiresAt < now)
                        {
                            l.Status = LoanStatus.Cancelled;
                            l.Book.AvailableCopies++;
                            Notify(
                                l.MemberId,
                                "expired:" + l.Id,
                                "Đặt sách #" + l.Id + " đã hết hạn nhận."
                            );
                        }
                        continue;
                    }
                    if (l.DueAt.Value.Date < today)
                    {
                        l.Status = LoanStatus.Overdue;
                        var fine = Db.Fines.SingleOrDefault(f => f.LoanId == l.Id);
                        if (fine == null)
                        {
                            fine = new Fine { LoanId = l.Id };
                            Db.Fines.Add(fine);
                        }
                        fine.Amount = (today - l.DueAt.Value.Date).Days * l.DailyFineRate;
                        Notify(
                            l.MemberId,
                            "overdue:" + l.Id,
                            "Sách “" + l.Book.Title + "” đã quá hạn. Vui lòng trả sách."
                        );
                        if (fine.Amount > fine.Paid)
                            Notify(
                                l.MemberId,
                                "fine:" + l.Id + ":" + today.ToString("yyyyMMdd"),
                                "Khoản phạt #"
                                    + l.Id
                                    + " còn "
                                    + (fine.Amount - fine.Paid).ToString("N0")
                                    + " đ."
                            );
                    }
                    else if ((l.DueAt.Value.Date - today).Days <= 2)
                        Notify(
                            l.MemberId,
                            "due:" + l.Id + ":" + l.DueAt.Value.ToString("yyyyMMdd"),
                            "Sách “"
                                + l.Book.Title
                                + "” đến hạn ngày "
                                + l.DueAt.Value.ToString("dd/MM/yyyy")
                                + "."
                        );
                }
                return true;
            });
        }

        public Loan Reserve(string member, int bookId)
        {
            return Write(() =>
            {
                var u = Db.Users.Find(member);
                if (u == null || !u.IsActive)
                    throw new RuleException("Tài khoản không hoạt động.");
                var b = Db.Books.Find(bookId);
                if (b == null || !b.IsActive || b.AvailableCopies < 1)
                    throw new RuleException("Sách hiện không còn bản để đặt.");
                if (
                    Db.Loans.Count(x =>
                        x.MemberId == member
                        && (
                            x.Status == LoanStatus.Reserved
                            || x.Status == LoanStatus.Borrowed
                            || x.Status == LoanStatus.Overdue
                        )
                    ) >= Policy.MaxActiveLoans
                )
                    throw new RuleException("Bạn đã đạt giới hạn sách đang mượn/đặt.");
                if (
                    Db.Loans.Any(x =>
                        x.MemberId == member
                        && x.BookId == bookId
                        && (
                            x.Status == LoanStatus.Reserved
                            || x.Status == LoanStatus.Borrowed
                            || x.Status == LoanStatus.Overdue
                        )
                    )
                )
                    throw new RuleException("Bạn đã mượn hoặc đặt tựa sách này.");
                b.AvailableCopies--;
                var l = new Loan
                {
                    MemberId = member,
                    BookId = bookId,
                    Status = LoanStatus.Reserved,
                    ReservedAt = DateTime.UtcNow,
                    PickupExpiresAt = DateTime.UtcNow.AddDays(Policy.PickupDays),
                    DailyFineRate = Policy.FinePerDay,
                };
                Db.Loans.Add(l);
                Db.SaveChanges();
                Notify(
                    member,
                    "reserved:" + l.Id,
                    "Đã giữ sách “"
                        + b.Title
                        + "”. Nhận trước "
                        + l.PickupExpiresAt.ToString("dd/MM/yyyy HH:mm")
                        + " UTC."
                );
                return l;
            });
        }

        public Loan ChangeLoan(int id, string action, string actor, bool staff)
        {
            return Write(() =>
            {
                var l = Db.Loans.Include(x => x.Book).SingleOrDefault(x => x.Id == id);
                if (l == null || (!staff && l.MemberId != actor))
                    throw new RuleException("Không tìm thấy phiếu mượn.");
                if (action == "cancel")
                {
                    if (l.Status != LoanStatus.Reserved)
                        throw new RuleException("Chỉ hủy được phiếu đang đặt.");
                    l.Status = LoanStatus.Cancelled;
                    l.Book.AvailableCopies++;
                }
                else if (action == "checkout" && staff)
                {
                    if (l.Status != LoanStatus.Reserved || l.PickupExpiresAt < DateTime.UtcNow)
                        throw new RuleException("Phiếu không còn hiệu lực nhận sách.");
                    if (!Db.Users.Find(l.MemberId).IsActive)
                        throw new RuleException("Thành viên đã bị khóa.");
                    l.Status = LoanStatus.Borrowed;
                    l.BorrowedAt = DateTime.UtcNow;
                    l.DueAt = DateTime.UtcNow.Date.AddDays(Policy.LoanDays);
                    l.DailyFineRate = Policy.FinePerDay;
                }
                else if (action == "return" && staff)
                {
                    if (l.Status != LoanStatus.Borrowed && l.Status != LoanStatus.Overdue)
                        throw new RuleException("Phiếu không ở trạng thái đang mượn.");
                    l.ReturnedAt = DateTime.UtcNow;
                    l.Status = LoanStatus.Returned;
                    l.Book.AvailableCopies++;
                    foreach (
                        var r in Db.Renewals.Where(r =>
                            r.LoanId == id && r.Status == RenewalStatus.Pending
                        )
                    )
                    {
                        r.Status = RenewalStatus.Rejected;
                        r.Note = "Sách đã trả.";
                        r.DecidedAt = DateTime.UtcNow;
                    }
                }
                else
                    throw new RuleException("Thao tác không hợp lệ.");
                Notify(
                    l.MemberId,
                    "loan:" + id + ":" + action,
                    "Phiếu #"
                        + id
                        + ": "
                        + (
                            action == "return" ? "đã trả sách"
                            : action == "cancel" ? "đã hủy đặt sách"
                            : "đã nhận sách, hạn trả " + l.DueAt.Value.ToString("dd/MM/yyyy")
                        )
                        + "."
                );
                return l;
            });
        }

        public RenewalRequest RequestRenewal(string member, int id)
        {
            return Write(() =>
            {
                var l = Db.Loans.Find(id);
                if (l == null || l.MemberId != member)
                    throw new RuleException("Không tìm thấy phiếu mượn.");
                CheckRenewal(l);
                if (Db.Renewals.Any(x => x.LoanId == id && x.Status == RenewalStatus.Pending))
                    throw new RuleException("Đã có yêu cầu gia hạn chờ duyệt.");
                var r = new RenewalRequest
                {
                    LoanId = id,
                    RequestedAt = DateTime.UtcNow,
                    Status = RenewalStatus.Pending,
                };
                Db.Renewals.Add(r);
                return r;
            });
        }

        private void CheckRenewal(Loan l)
        {
            if (l.Status != LoanStatus.Borrowed || l.DueAt.Value.Date <= DateTime.UtcNow.Date)
                throw new RuleException("Cần xin gia hạn trước ngày đến hạn.");
            if (l.RenewalCount >= Policy.MaxRenewals)
                throw new RuleException("Đã hết số lần gia hạn.");
            if (
                Db.Loans.Any(x =>
                    x.BookId == l.BookId
                    && x.MemberId != l.MemberId
                    && x.Status == LoanStatus.Reserved
                )
            )
                throw new RuleException("Có thành viên khác đang đặt tựa sách này.");
        }

        public RenewalRequest DecideRenewal(int id, bool approve, string note)
        {
            return Write(() =>
            {
                var r = Db.Renewals.Include(x => x.Loan).SingleOrDefault(x => x.Id == id);
                if (r == null || r.Status != RenewalStatus.Pending)
                    throw new RuleException("Yêu cầu không còn chờ duyệt.");
                if (approve)
                {
                    CheckRenewal(r.Loan);
                    r.Loan.DueAt = r.Loan.DueAt.Value.AddDays(Policy.RenewalDays);
                    r.Loan.RenewalCount++;
                }
                r.Status = approve ? RenewalStatus.Approved : RenewalStatus.Rejected;
                r.Note = note;
                r.DecidedAt = DateTime.UtcNow;
                Notify(
                    r.Loan.MemberId,
                    "renewal:" + id,
                    "Gia hạn phiếu #"
                        + r.LoanId
                        + (approve ? " được chấp thuận." : " bị từ chối. ")
                        + note
                );
                return r;
            });
        }

        public FinePayment Pay(int fineId, decimal amount, string actor, string note)
        {
            return Write(() =>
            {
                var f = Db.Fines.Include(x => x.Loan).SingleOrDefault(x => x.Id == fineId);
                if (
                    f == null
                    || amount <= 0
                    || decimal.Round(amount, 0) != amount
                    || amount > f.Amount - f.Paid
                )
                    throw new RuleException("Số tiền phải là số nguyên dương và không vượt dư nợ.");
                f.Paid += amount;
                var p = new FinePayment
                {
                    FineId = fineId,
                    Amount = amount,
                    PaidAt = DateTime.UtcNow,
                    RecordedBy = actor,
                    Note = note,
                };
                Db.FinePayments.Add(p);
                Db.SaveChanges();
                Notify(
                    f.Loan.MemberId,
                    "payment:" + p.Id,
                    "Đã ghi nhận thanh toán "
                        + amount.ToString("N0")
                        + " đ cho phiếu #"
                        + f.LoanId
                        + "."
                );
                return p;
            });
        }
    }
}
