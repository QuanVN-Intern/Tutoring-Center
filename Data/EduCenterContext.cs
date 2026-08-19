using Microsoft.EntityFrameworkCore;
using EduCenterManagement.Models;
using System.Security.Cryptography;
using System.Text;

namespace EduCenterManagement.Data
{
    public class EduCenterContext : DbContext
    {
        public EduCenterContext(DbContextOptions<EduCenterContext> options)
            : base(options)
        {
        }

        public DbSet<Role> Roles => Set<Role>();
        public DbSet<User> Users => Set<User>();
        public DbSet<Facility> Facilities => Set<Facility>();
        public DbSet<Room> Rooms => Set<Room>();
        public DbSet<Shift> Shifts => Set<Shift>();
        public DbSet<Subject> Subjects => Set<Subject>();
        public DbSet<Class> Classes => Set<Class>();
        public DbSet<ClassSession> ClassSessions => Set<ClassSession>();
        public DbSet<Enrollment> Enrollments => Set<Enrollment>();
        public DbSet<Attendance> Attendance => Set<Attendance>();
        public DbSet<Tuition> Tuitions => Set<Tuition>();
        public DbSet<Payment> Payments => Set<Payment>();
        public DbSet<RoomChangeRequest> RoomChangeRequests => Set<RoomChangeRequest>();
        public DbSet<SystemSetting> SystemSettings => Set<SystemSetting>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Indexes & Unique Constraints
            modelBuilder.Entity<Role>()
                .HasIndex(r => r.RoleName)
                .IsUnique();

            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();

            modelBuilder.Entity<Room>()
                .HasIndex(r => new { r.FacilityId, r.RoomNumber })
                .IsUnique();

            modelBuilder.Entity<Subject>()
                .HasIndex(s => s.SubjectName)
                .IsUnique();

            modelBuilder.Entity<ClassSession>()
                .HasIndex(cs => new { cs.ClassId, cs.SessionDate })
                .IsUnique();

            modelBuilder.Entity<Enrollment>()
                .HasIndex(e => new { e.StudentId, e.ClassId })
                .IsUnique();

            modelBuilder.Entity<Attendance>()
                .HasIndex(a => new { a.SessionId, a.StudentId })
                .IsUnique();

            // Computed column TotalAmount
            modelBuilder.Entity<Tuition>()
                .Property(t => t.TotalAmount)
                .HasComputedColumnSql("[SessionsAttended] * [AmountPerSession]", stored: true);

            // Relationships configuration to prevent multiple cascade path conflicts
            modelBuilder.Entity<Class>()
                .HasOne(c => c.Lecturer)
                .WithMany(u => u.LecturerClasses)
                .HasForeignKey(c => c.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Class>()
                .HasOne(c => c.Creator)
                .WithMany()
                .HasForeignKey(c => c.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Student)
                .WithMany(u => u.StudentAttendances)
                .HasForeignKey(a => a.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Attendance>()
                .HasOne(a => a.Recorder)
                .WithMany(u => u.RecordedAttendances)
                .HasForeignKey(a => a.RecordedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<Payment>()
                .HasOne(p => p.Receiver)
                .WithMany(u => u.ReceivedPayments)
                .HasForeignKey(p => p.ReceivedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomChangeRequest>()
                .HasOne(r => r.Lecturer)
                .WithMany(u => u.LecturerRequests)
                .HasForeignKey(r => r.LecturerId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomChangeRequest>()
                .HasOne(r => r.Reviewer)
                .WithMany(u => u.ReviewedRequests)
                .HasForeignKey(r => r.ReviewedBy)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomChangeRequest>()
                .HasOne(r => r.CurrentRoom)
                .WithMany()
                .HasForeignKey(r => r.CurrentRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomChangeRequest>()
                .HasOne(r => r.RequestedRoom)
                .WithMany()
                .HasForeignKey(r => r.RequestedRoomId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomChangeRequest>()
                .HasOne(r => r.CurrentShift)
                .WithMany()
                .HasForeignKey(r => r.CurrentShiftId)
                .OnDelete(DeleteBehavior.Restrict);

            modelBuilder.Entity<RoomChangeRequest>()
                .HasOne(r => r.RequestedShift)
                .WithMany()
                .HasForeignKey(r => r.RequestedShiftId)
                .OnDelete(DeleteBehavior.Restrict);
        }

        public static string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }

        public void SeedInitialData()
        {
            Database.EnsureCreated();

            // 1. Roles
            if (!Roles.Any())
            {
                Roles.AddRange(
                    new Role { RoleName = "Admin" },
                    new Role { RoleName = "GrandManager" },
                    new Role { RoleName = "FacilityManager" },
                    new Role { RoleName = "Lecturer" },
                    new Role { RoleName = "Student" }
                );
                SaveChanges();
            }

            // 2. Facilities
            if (!Facilities.Any())
            {
                Facilities.AddRange(
                    new Facility { FacilityName = "Cơ sở 1", Address = "123 Đường Nguyễn Trãi, Quận 1, TP.HCM" },
                    new Facility { FacilityName = "Cơ sở 2", Address = "456 Đường Lê Văn Việt, TP. Thủ Đức, TP.HCM" },
                    new Facility { FacilityName = "Cơ sở 3", Address = "789 Đường Cầu Giấy, Q. Cầu Giấy, Hà Nội" }
                );
                SaveChanges();
            }

            // 3. Rooms (6 rooms / facility = 18 rooms)
            if (!Rooms.Any())
            {
                var facilities = Facilities.ToList();
                foreach (var f in facilities)
                {
                    for (int r = 1; r <= 6; r++)
                    {
                        Rooms.Add(new Room
                        {
                            FacilityId = f.FacilityId,
                            RoomNumber = $"P{f.FacilityId}0{r}",
                            Capacity = 30,
                            Condition = (r == 6 && f.FacilityId == 1) ? "Closed" : "Open"
                        });
                    }
                }
                SaveChanges();
            }

            // 4. Shifts
            if (!Shifts.Any())
            {
                Shifts.AddRange(
                    new Shift { ShiftName = "Ca 1", StartTime = new TimeSpan(4, 30, 0), EndTime = new TimeSpan(6, 0, 0) },
                    new Shift { ShiftName = "Ca 2", StartTime = new TimeSpan(6, 0, 0), EndTime = new TimeSpan(7, 30, 0) },
                    new Shift { ShiftName = "Ca 3", StartTime = new TimeSpan(7, 30, 0), EndTime = new TimeSpan(9, 0, 0) }
                );
                SaveChanges();
            }

            // 5. Subjects
            if (!Subjects.Any())
            {
                Subjects.AddRange(
                    new Subject { SubjectName = "Math" },
                    new Subject { SubjectName = "Literature" },
                    new Subject { SubjectName = "Physics" },
                    new Subject { SubjectName = "English" },
                    new Subject { SubjectName = "Chemistry" }
                );
                SaveChanges();
            }

            // 6. SystemSettings
            if (!SystemSettings.Any())
            {
                SystemSettings.Add(new SystemSetting
                {
                    SettingKey = "TUITION_PER_SESSION",
                    SettingValue = "200000",
                    Description = "Học phí mặc định mỗi buổi học (VNĐ)"
                });
                SaveChanges();
            }

            // 7. Ensure Users for 5 roles exist with valid password hash
            string defaultPasswordHash = HashPassword("123456");

            var adminRole = Roles.First(r => r.RoleName == "Admin").RoleId;
            var grandRole = Roles.First(r => r.RoleName == "GrandManager").RoleId;
            var facilityRole = Roles.First(r => r.RoleName == "FacilityManager").RoleId;
            var lecturerRole = Roles.First(r => r.RoleName == "Lecturer").RoleId;
            var studentRole = Roles.First(r => r.RoleName == "Student").RoleId;

            // Fix any placeholder CHANGE_ME_HASH from initial script
            var changeMeUsers = Users.Where(u => u.PasswordHash == "CHANGE_ME_HASH").ToList();
            foreach (var u in changeMeUsers)
            {
                u.PasswordHash = defaultPasswordHash;
            }
            if (changeMeUsers.Any()) SaveChanges();

            var requiredDemoUsers = new List<(string Name, string Email, int RoleId, string Phone)>
            {
                ("System Admin", "admin@educenter.local", adminRole, "0901111111"),
                ("Nguyễn Văn Management", "grandmanager@educenter.local", grandRole, "0902222222"),
                ("Trần Thị Cơ Sở 1", "fm1@educenter.local", facilityRole, "0903333333"),
                ("Lê Văn Facility 2", "fm2@educenter.local", facilityRole, "0903333334"),
                ("Thầy Giáo Toán", "lecturer.math@educenter.local", lecturerRole, "0904444444"),
                ("Cô Giáo Văn", "lecturer.lit@educenter.local", lecturerRole, "0904444445"),
                ("Thầy Giáo Lý", "lecturer.phy@educenter.local", lecturerRole, "0904444446"),
                ("Học Viên Nguyễn Văn A", "student.a@educenter.local", studentRole, "0905555555"),
                ("Học Viên Trần Thị B", "student.b@educenter.local", studentRole, "0905555556"),
                ("Học Viên Lê Văn C", "student.c@educenter.local", studentRole, "0905555557")
            };

            foreach (var item in requiredDemoUsers)
            {
                var existingUser = Users.FirstOrDefault(u => u.Email.ToLower() == item.Email.ToLower());
                if (existingUser == null)
                {
                    Users.Add(new User
                    {
                        FullName = item.Name,
                        Email = item.Email,
                        PasswordHash = defaultPasswordHash,
                        RoleId = item.RoleId,
                        IsActive = true,
                        PhoneNumber = item.Phone,
                        CreatedAt = DateTime.Now
                    });
                }
                else
                {
                    existingUser.PasswordHash = defaultPasswordHash;
                    existingUser.IsActive = true;
                }
            }
            SaveChanges();

            // 8. Sample Classes & Sessions & Enrollments for demonstration
            if (!Classes.Any())
            {
                var math = Subjects.First(s => s.SubjectName == "Math").SubjectId;
                var lit = Subjects.First(s => s.SubjectName == "Literature").SubjectId;

                var lecMath = Users.FirstOrDefault(u => u.Email.ToLower() == "lecturer.math@educenter.local")?.UserId ?? 1;
                var lecLit = Users.FirstOrDefault(u => u.Email.ToLower() == "lecturer.lit@educenter.local")?.UserId ?? 1;

                var fac1 = Facilities.First(f => f.FacilityName == "Cơ sở 1").FacilityId;
                var room101 = Rooms.First(r => r.RoomNumber == "P101").RoomId;
                var room102 = Rooms.First(r => r.RoomNumber == "P102").RoomId;

                var shift1 = Shifts.First(s => s.ShiftName == "Ca 1").ShiftId;
                var shift2 = Shifts.First(s => s.ShiftName == "Ca 2").ShiftId;

                var grandUser = Users.FirstOrDefault(u => u.Email.ToLower() == "grandmanager@educenter.local")?.UserId ?? 1;

                var classMath = new Class
                {
                    ClassName = "MATH-K12-01",
                    SubjectId = math,
                    LecturerId = lecMath,
                    FacilityId = fac1,
                    RoomId = room101,
                    ShiftId = shift1,
                    DaysOfWeek = "2,4,6",
                    StartDate = DateTime.Today.AddDays(-14),
                    EndDate = DateTime.Today.AddDays(30),
                    Status = "Active",
                    CreatedBy = grandUser
                };

                var classLit = new Class
                {
                    ClassName = "LIT-K12-01",
                    SubjectId = lit,
                    LecturerId = lecLit,
                    FacilityId = fac1,
                    RoomId = room102,
                    ShiftId = shift2,
                    DaysOfWeek = "3,5,7",
                    StartDate = DateTime.Today.AddDays(-14),
                    EndDate = DateTime.Today.AddDays(30),
                    Status = "Active",
                    CreatedBy = grandUser
                };

                Classes.AddRange(classMath, classLit);
                SaveChanges();

                // Enrollments
                var stA = Users.FirstOrDefault(u => u.Email.ToLower() == "student.a@educenter.local")?.UserId ?? 1;
                var stB = Users.FirstOrDefault(u => u.Email.ToLower() == "student.b@educenter.local")?.UserId ?? 1;
                var stC = Users.FirstOrDefault(u => u.Email.ToLower() == "student.c@educenter.local")?.UserId ?? 1;

                Enrollments.AddRange(
                    new Enrollment { StudentId = stA, ClassId = classMath.ClassId, EnrollDate = DateTime.Today.AddDays(-14), Status = "Active" },
                    new Enrollment { StudentId = stB, ClassId = classMath.ClassId, EnrollDate = DateTime.Today.AddDays(-14), Status = "Active" },
                    new Enrollment { StudentId = stC, ClassId = classLit.ClassId, EnrollDate = DateTime.Today.AddDays(-14), Status = "Active" }
                );
                SaveChanges();

                // Sessions & Attendance
                for (int i = 0; i < 4; i++)
                {
                    var sessionDate = DateTime.Today.AddDays(-12 + (i * 2));
                    var sessionMath = new ClassSession
                    {
                        ClassId = classMath.ClassId,
                        SessionDate = sessionDate,
                        RoomId = room101,
                        ShiftId = shift1,
                        Status = "Completed"
                    };
                    ClassSessions.Add(sessionMath);
                    SaveChanges();

                    Attendance.AddRange(
                        new Attendance { SessionId = sessionMath.SessionId, StudentId = stA, Status = "Present", RecordedBy = lecMath, RecordedAt = sessionDate },
                        new Attendance { SessionId = sessionMath.SessionId, StudentId = stB, Status = (i % 2 == 0 ? "Present" : "ExcusedAbsent"), RecordedBy = lecMath, RecordedAt = sessionDate }
                    );
                    SaveChanges();
                }

                // Tuitions
                var tuitionA = new Tuition
                {
                    StudentId = stA,
                    ClassId = classMath.ClassId,
                    PeriodFrom = DateTime.Today.AddDays(-14),
                    PeriodTo = DateTime.Today,
                    SessionsAttended = 4,
                    AmountPerSession = 200000,
                    PaymentStatus = "Paid",
                    DueDate = DateTime.Today.AddDays(7)
                };

                var tuitionB = new Tuition
                {
                    StudentId = stB,
                    ClassId = classMath.ClassId,
                    PeriodFrom = DateTime.Today.AddDays(-14),
                    PeriodTo = DateTime.Today,
                    SessionsAttended = 2,
                    AmountPerSession = 200000,
                    PaymentStatus = "Unpaid",
                    DueDate = DateTime.Today.AddDays(7)
                };

                Tuitions.AddRange(tuitionA, tuitionB);
                SaveChanges();

                // Payment for A
                Payments.Add(new Payment
                {
                    TuitionId = tuitionA.TuitionId,
                    AmountPaid = 800000,
                    PaymentDate = DateTime.Now,
                    PaymentMethod = "Bank Transfer",
                    ReceivedBy = grandUser,
                    ReceiptNumber = "REC-2026-001"
                });
                SaveChanges();
            }
        }
    }
}
