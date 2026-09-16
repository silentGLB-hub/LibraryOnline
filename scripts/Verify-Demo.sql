-- Read-only verification; run after the first website request.
USE LibraryOnlineDemo;
SELECT r.Name AS Role,COUNT(*) AS Accounts FROM AspNetUsers u JOIN AspNetUserRoles ur ON u.Id=ur.UserId JOIN AspNetRoles r ON ur.RoleId=r.Id GROUP BY r.Name;
SELECT 'Categories' AS Dataset,COUNT(*) AS Actual,8 AS Minimum FROM Categories
UNION ALL SELECT 'Books',COUNT(*),50 FROM Books
UNION ALL SELECT 'Loans',COUNT(*),30 FROM Loans
UNION ALL SELECT 'Overdue',COUNT(*),10 FROM Loans WHERE Status=3
UNION ALL SELECT 'Fines',COUNT(*),10 FROM Fines
UNION ALL SELECT 'Payments',COUNT(*),5 FROM FinePayments
UNION ALL SELECT 'Reservation requests',COUNT(*),10 FROM Loans
UNION ALL SELECT 'Renewals',COUNT(*),5 FROM RenewalRequests;
-- This must return zero rows: stock is total minus reserved/borrowed/overdue.
SELECT b.Id,b.Title,b.TotalCopies,b.AvailableCopies,COUNT(l.Id) AS Held FROM Books b
LEFT JOIN Loans l ON l.BookId=b.Id AND l.Status IN (0,1,3)
GROUP BY b.Id,b.Title,b.TotalCopies,b.AvailableCopies
HAVING b.AvailableCopies<>b.TotalCopies-COUNT(l.Id) OR b.AvailableCopies<0;
SELECT Status,COUNT(*) AS Count FROM Loans GROUP BY Status;
SELECT u.Email FROM AspNetUsers u JOIN AspNetUserRoles ur ON ur.UserId=u.Id JOIN AspNetRoles r ON r.Id=ur.RoleId WHERE r.Name='Member' AND NOT EXISTS(SELECT 1 FROM Loans l WHERE l.MemberId=u.Id);
SELECT f.Id,f.Amount,f.Paid,COALESCE(SUM(p.Amount),0) AS RecordedPayments FROM Fines f LEFT JOIN FinePayments p ON p.FineId=f.Id GROUP BY f.Id,f.Amount,f.Paid HAVING f.Paid<>COALESCE(SUM(p.Amount),0);
