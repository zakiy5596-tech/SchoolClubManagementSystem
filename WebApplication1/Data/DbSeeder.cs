using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using WebApplication1.Models;

namespace WebApplication1.Data
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
            var context = services.GetRequiredService<ApplicationDbContext>();

            await context.Database.MigrateAsync();

            foreach (var role in Roles.All)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            const string adminEmail = "admin@scms.local";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);
            if (adminUser == null)
            {
                adminUser = new ApplicationUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    EmailConfirmed = true,
                    FullName = "System Administrator",
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.Now
                };

                var result = await userManager.CreateAsync(adminUser, "Admin@12345");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(adminUser, Roles.Admin);
                }
            }

            await SeedDemoDataAsync(userManager, context);
            await SeedDemoParentAsync(userManager, context);
        }

        private static async Task SeedDemoParentAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            const string parentEmail = "p.smith@scms.local";
            if (await userManager.FindByEmailAsync(parentEmail) != null)
            {
                return;
            }

            var linkedStudents = await context.Students
                .Where(s => s.StudentNumber == "STU-1001" || s.StudentNumber == "STU-1002")
                .ToListAsync();

            if (!linkedStudents.Any())
            {
                return;
            }

            var parentUser = new ApplicationUser
            {
                UserName = parentEmail,
                Email = parentEmail,
                EmailConfirmed = true,
                FullName = "Robert Smith",
                Status = UserStatus.Active,
                CreatedAt = DateTime.Now
            };
            await userManager.CreateAsync(parentUser, "Parent@123");
            await userManager.AddToRoleAsync(parentUser, Roles.Parent);

            var parent = new Parent { UserId = parentUser.Id, Phone = "555-0301" };
            context.Parents.Add(parent);
            await context.SaveChangesAsync();

            foreach (var student in linkedStudents)
            {
                context.ParentStudents.Add(new ParentStudent { ParentId = parent.ParentId, StudentId = student.StudentId });
            }
            await context.SaveChangesAsync();
        }

        private static async Task SeedDemoDataAsync(UserManager<ApplicationUser> userManager, ApplicationDbContext context)
        {
            if (await context.Clubs.AnyAsync())
            {
                return;
            }

            async Task<ApplicationUser> CreateUserAsync(string email, string fullName, string role, string password)
            {
                var user = new ApplicationUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true,
                    FullName = fullName,
                    Status = UserStatus.Active,
                    CreatedAt = DateTime.Now
                };
                await userManager.CreateAsync(user, password);
                await userManager.AddToRoleAsync(user, role);
                return user;
            }

            var advisor1User = await CreateUserAsync("j.carter@scms.local", "James Carter", Roles.Advisor, "Advisor@123");
            var advisor2User = await CreateUserAsync("m.lopez@scms.local", "Maria Lopez", Roles.Advisor, "Advisor@123");

            var advisor1 = new Advisor { UserId = advisor1User.Id, Phone = "555-0101" };
            var advisor2 = new Advisor { UserId = advisor2User.Id, Phone = "555-0102" };
            context.Advisors.AddRange(advisor1, advisor2);
            await context.SaveChangesAsync();

            var chess = new Club { ClubName = "Chess Club", Description = "Strategic thinking and competitive chess play for all skill levels.", Category = "Academic", AdvisorId = advisor1.AdvisorId, DateCreated = DateTime.Now.AddMonths(-6) };
            var drama = new Club { ClubName = "Drama Society", Description = "Acting, stage production, and theatre arts.", Category = "Arts", AdvisorId = advisor2.AdvisorId, DateCreated = DateTime.Now.AddMonths(-5) };
            var robotics = new Club { ClubName = "Robotics Club", Description = "Design, build, and program competitive robots.", Category = "Technology", AdvisorId = advisor1.AdvisorId, DateCreated = DateTime.Now.AddMonths(-4) };
            var basketball = new Club { ClubName = "Basketball Club", Description = "School basketball training and friendly matches.", Category = "Sports", AdvisorId = advisor2.AdvisorId, DateCreated = DateTime.Now.AddMonths(-3) };
            context.Clubs.AddRange(chess, drama, robotics, basketball);
            await context.SaveChangesAsync();

            var studentUsers = new List<Student>();
            var studentSeeds = new (string Email, string Name, string Number)[]
            {
                ("a.smith@scms.local", "Ava Smith", "STU-1001"),
                ("b.jones@scms.local", "Ben Jones", "STU-1002"),
                ("c.brown@scms.local", "Chloe Brown", "STU-1003"),
                ("d.wilson@scms.local", "Daniel Wilson", "STU-1004"),
                ("e.davis@scms.local", "Ella Davis", "STU-1005"),
            };

            foreach (var s in studentSeeds)
            {
                var user = await CreateUserAsync(s.Email, s.Name, Roles.Student, "Student@123");
                var student = new Student { UserId = user.Id, StudentNumber = s.Number, Phone = "555-02" + s.Number[^2..] };
                context.Students.Add(student);
                studentUsers.Add(student);
            }
            await context.SaveChangesAsync();

            context.ClubMemberships.AddRange(
                new ClubMembership { StudentId = studentUsers[0].StudentId, ClubId = chess.ClubId, Status = MembershipStatus.Approved, DateJoined = DateTime.Now.AddMonths(-2), RequestedAt = DateTime.Now.AddMonths(-2) },
                new ClubMembership { StudentId = studentUsers[1].StudentId, ClubId = chess.ClubId, Status = MembershipStatus.Pending, RequestedAt = DateTime.Now.AddDays(-3) },
                new ClubMembership { StudentId = studentUsers[2].StudentId, ClubId = drama.ClubId, Status = MembershipStatus.Approved, DateJoined = DateTime.Now.AddMonths(-1), RequestedAt = DateTime.Now.AddMonths(-1) },
                new ClubMembership { StudentId = studentUsers[3].StudentId, ClubId = robotics.ClubId, Status = MembershipStatus.Pending, RequestedAt = DateTime.Now.AddDays(-1) },
                new ClubMembership { StudentId = studentUsers[4].StudentId, ClubId = basketball.ClubId, Status = MembershipStatus.Rejected, RequestedAt = DateTime.Now.AddDays(-10) },
                new ClubMembership { StudentId = studentUsers[0].StudentId, ClubId = robotics.ClubId, Status = MembershipStatus.Approved, DateJoined = DateTime.Now.AddDays(-20), RequestedAt = DateTime.Now.AddDays(-20) }
            );

            context.Events.AddRange(
                new Event { ClubId = chess.ClubId, Title = "Weekly Chess Tournament", Description = "Round-robin chess tournament open to all members.", EventDate = DateTime.Today.AddDays(5), StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(17, 0, 0), Location = "Room 204", CreatedBy = advisor1User.Id, CreatedAt = DateTime.Now },
                new Event { ClubId = drama.ClubId, Title = "Spring Play Auditions", Description = "Auditions for the annual spring theatre production.", EventDate = DateTime.Today.AddDays(10), StartTime = new TimeSpan(14, 0, 0), EndTime = new TimeSpan(16, 0, 0), Location = "Main Hall", CreatedBy = advisor2User.Id, CreatedAt = DateTime.Now },
                new Event { ClubId = robotics.ClubId, Title = "Robotics Workshop", Description = "Hands-on workshop building line-following robots.", EventDate = DateTime.Today.AddDays(2), StartTime = new TimeSpan(9, 0, 0), EndTime = new TimeSpan(12, 0, 0), Location = "Science Lab 1", CreatedBy = advisor1User.Id, CreatedAt = DateTime.Now },
                new Event { ClubId = chess.ClubId, Title = "Chess Strategy Seminar", Description = "Introductory seminar on opening strategy.", EventDate = DateTime.Today.AddDays(-15), StartTime = new TimeSpan(15, 0, 0), EndTime = new TimeSpan(16, 30, 0), Location = "Room 204", CreatedBy = advisor1User.Id, CreatedAt = DateTime.Now.AddDays(-20) },
                new Event { ClubId = basketball.ClubId, Title = "Inter-Club Friendly Match", Description = "Friendly basketball match against the neighboring school.", EventDate = DateTime.Today.AddDays(-7), StartTime = new TimeSpan(16, 0, 0), EndTime = new TimeSpan(18, 0, 0), Location = "School Gym", CreatedBy = advisor2User.Id, CreatedAt = DateTime.Now.AddDays(-10) }
            );

            await context.SaveChangesAsync();
        }
    }
}
