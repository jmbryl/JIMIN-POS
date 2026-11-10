using System;
using System.Data;
using System.Text.RegularExpressions;
using EnterprisePOS.Data;
using EnterprisePOS.Domain;

namespace EnterprisePOS.Services
{
    public sealed class UserService
    {
        private readonly UserRepository repository = new UserRepository();

        public DataTable GetAll()
        {
            Session.Require(Permission.ManageUsers);
            return repository.GetAll();
        }

        public DataTable GetRoles()
        {
            Session.Require(Permission.ManageUsers);
            return repository.GetRoles();
        }

        public AppUser GetById(int userId)
        {
            Session.Require(Permission.ManageUsers);
            return repository.GetById(userId);
        }

        public int Create(string username, string fullName, int roleId, string password)
        {
            Session.Require(Permission.ManageUsers);

            if (string.IsNullOrWhiteSpace(username) ||
                !Regex.IsMatch(username.Trim(), @"^[A-Za-z0-9._-]{3,50}$"))
                throw new ArgumentException("Username must be 3-50 characters: letters, numbers, dot, dash or underscore.");

            ValidateName(fullName);
            if (roleId <= 0) throw new ArgumentException("Choose a role.");
            ValidatePassword(password);

            return repository.Create(username.Trim(), fullName.Trim(), roleId, password);
        }

        /// <summary>Updates a user. A non-blank newPassword also resets the password.</summary>
        public void Update(int userId, string fullName, int roleId, bool isActive, string newPassword)
        {
            Session.Require(Permission.ManageUsers);

            ValidateName(fullName);
            if (roleId <= 0) throw new ArgumentException("Choose a role.");

            AppUser target = repository.GetById(userId);
            if (target == null) throw new InvalidOperationException("User not found.");

            bool isSelf = target.UserId == Session.CurrentUser.UserId;
            if (isSelf && !isActive)
                throw new InvalidOperationException("You cannot deactivate your own account.");
            if (isSelf && roleId != target.RoleId)
                throw new InvalidOperationException("You cannot change your own role.");

            bool targetIsActiveAdmin = target.IsActive &&
                string.Equals(target.RoleName, RoleNames.Admin, StringComparison.OrdinalIgnoreCase);
            bool staysActiveAdmin = isActive && roleId == target.RoleId;

            if (targetIsActiveAdmin && !staysActiveAdmin && repository.CountActiveAdmins(userId) == 0)
                throw new InvalidOperationException("At least one active administrator must remain.");

            if (!string.IsNullOrEmpty(newPassword)) ValidatePassword(newPassword);

            repository.Update(userId, fullName.Trim(), roleId, isActive);
            if (!string.IsNullOrEmpty(newPassword)) repository.ResetPassword(userId, newPassword);
        }

        private static void ValidateName(string fullName)
        {
            if (string.IsNullOrWhiteSpace(fullName)) throw new ArgumentException("Full name is required.");
            if (fullName.Trim().Length > 100) throw new ArgumentException("Full name must be 100 characters or fewer.");
        }

        private static void ValidatePassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8)
                throw new ArgumentException("The password must be at least 8 characters.");
            if (password.Length > 64)
                throw new ArgumentException("The password must be 64 characters or fewer.");
            foreach (char c in password)
            {
                if (c < 32 || c > 126)
                    throw new ArgumentException("Use only standard keyboard characters (letters, numbers and symbols) in the password.");
            }
        }
    }
}
