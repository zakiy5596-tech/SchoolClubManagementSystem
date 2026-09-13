namespace WebApplication1.Models
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Advisor = "Advisor";
        public const string Student = "Student";
        public const string Parent = "Parent";

        public static readonly string[] All = { Admin, Advisor, Student, Parent };
    }
}
