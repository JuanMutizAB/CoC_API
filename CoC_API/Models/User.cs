using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;

namespace CoC_API.Models
{

    public class User
    {

        public int id
        {
            get => id;
            set => id = value;
        }

        public string name
        {
            get => name;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(name), "Name cannot be null or empty.");

                string[] nameParts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (nameParts.Length < 2)
                    throw new FormatException("Name must contain at least a first and last name.");

                name = value;
            }
        }

        private string email
        {
            get => email;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(email), "Email cannot be null or empty.");

                if (!value.Contains("@"))
                    throw new FormatException("Email must contain '@'.");

                email = value;
            }
        }

        private string password
        {
            get => password;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(password), "Password cannot be null or empty.");

                if (value.Length <= 8)
                    throw new ArgumentOutOfRangeException(nameof(password), "Password must have more than 8 characters.");

                if (!value.Any(char.IsUpper))
                    throw new FormatException("Password must contain at least one uppercase letter.");

                if (!value.Any(char.IsLower))
                    throw new FormatException("Password must contain at least one lowercase letter.");

                if (!value.Any(char.IsDigit))
                    throw new FormatException("Password must contain at least one digit.");

                if (!value.Any(c => !char.IsLetterOrDigit(c)))
                    throw new FormatException("Password must contain at least one special character.");

                password = HashPassword(value);
            }
        }

        private DateTime birthDate
        {
            get => birthDate;
            set
            {
                if (value > DateTime.Now)
                    throw new ArgumentOutOfRangeException(nameof(birthDate), "Birth date cannot be in the future.");

                birthDate = value;
            }
        }

        private string taxId
        {
            get => taxId;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(taxId), "Tax ID cannot be null or empty.");

                if (value.Length != 9 || !value.All(char.IsDigit))
                    throw new FormatException("Tax ID must have exactly 9 digits.");

                taxId = value;
            }
        }

        private List<UserCaracteristics> caracteristics
        {
            get => caracteristics;
            set => 
            {
                if (newCaracteristics.Any(value => !Enum.IsDefined(value)))
                    throw new ArgumentException("Characteristics contain an invalid value.", nameof(newCaracteristics));

                caracteristics = value;
            }
        }
        
        public User(int id, string name, string email, string password, DateTime birthDate, string taxId, List<UserCaracteristics> caracteristics)
        {
            this.id = id;
            this.name = name;
            this.email = email;
            this.password = password;
            this.birthDate = birthDate;
            this.taxId = taxId;
            this.caracteristics = caracteristics ?? new List<UserCaracteristics>();
            if (!this.caracteristics.Contains(UserCaracteristics.isActive))
            {
                this.caracteristics.Add(UserCaracteristics.isActive);
            }
        }

        public void UpdateUser(string newName, string newEmail, string newPassword, List<UserCaracteristics> newCaracteristics)
        {
            if (newCaracteristics is null || newCaracteristics.Any(value => !Enum.IsDefined(value)))
                throw new ArgumentException("Characteristics contain an invalid value.", nameof(newCaracteristics));

            this.name = newName;
            this.email = newEmail;
            this.password = newPassword;
            this.caracteristics = newCaracteristics;
        }

        public void DeactivateUser()
        {
            if (!this.caracteristics.Contains(UserCaracteristics.isActive))
                throw new InvalidOperationException("User is already deactivated.");

            this.caracteristics.Remove(UserCaracteristics.isActive);
        }

        private string HashPassword(string password)
        {
            byte[] bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }




    }
}