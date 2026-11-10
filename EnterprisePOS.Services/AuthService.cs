using System;
using EnterprisePOS.Data;
using EnterprisePOS.Domain;

namespace EnterprisePOS.Services
{
    public sealed class AuthService
    {
        private readonly AuthRepository repository = new AuthRepository();

        /// <summary>Returns the user and starts the session, or null with a safe failure message.</summary>
        public AppUser Login(string username, string password, out string failure)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrEmpty(password))
            {
                failure = "Enter your username and password.";
                return null;
            }

            AppUser user = repository.Authenticate(username.Trim(), password, out failure);
            if (user != null) Session.SignIn(user);
            return user;
        }

        public void Logout()
        {
            Session.SignOut();
        }
    }
}
