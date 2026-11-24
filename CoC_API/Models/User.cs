using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace CoC_API.Models
{
    public class User
    {
        public int idValue;
        public string nameValue = string.Empty;
        public string emailValue = string.Empty;
        public string passwordValue = string.Empty;
        public DateOnly birthDateValue;
        public string taxIdValue = string.Empty;
        public List<UserCaracteristics> caracteristicsValue = new();

        public int id
        {
            get => idValue;
            set
            {
                if (value < 0)
                    throw new ArgumentOutOfRangeException(nameof(id), "User ID must be greater than zero.");

                idValue = value;
            }
        }

        public string name
        {
            get => nameValue;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(name), "Name cannot be null or empty.");

                string[] nameParts = value.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (nameParts.Length < 2)
                    throw new FormatException("Name must contain at least a first and last name.");

                nameValue = value;
            }
        }

        public string email
        {
            get => emailValue;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(email), "Email cannot be null or empty.");

                if (!value.Contains("@"))
                    throw new FormatException("Email must contain '@'.");

                emailValue = value.Trim();
            }
        }

        [JsonIgnore]
        public string password
        {
            get => passwordValue;
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

                passwordValue = HashPassword(value);
            }
        }

        public DateOnly birthDate
        {
            get => birthDateValue;
            set
            {
                if (value > DateOnly.FromDateTime(DateTime.Today))
                    throw new ArgumentOutOfRangeException(nameof(birthDate), "Birth date cannot be in the future.");

                birthDateValue = value;
            }
        }

        public string taxId
        {
            get => taxIdValue;
            set
            {
                if (string.IsNullOrWhiteSpace(value))
                    throw new ArgumentNullException(nameof(taxId), "Tax ID cannot be null or empty.");

                if (value.Length != 9 || !value.All(char.IsDigit))
                    throw new FormatException("Tax ID must have exactly 9 digits.");

                taxIdValue = value;
            }
        }

        public List<UserCaracteristics> caracteristics
        {
            get => caracteristicsValue;
            set
            {
                if (value is null || value.Any(characteristic => !Enum.IsDefined(characteristic)))
                    throw new ArgumentException("Characteristics contain an invalid value.", nameof(caracteristics));
                if (value.Contains(UserCaracteristics.isAprovador) && value.Contains(UserCaracteristics.isAprovando))
                    throw new ArgumentException("A user cannot be both Aprovador and Aprovando.", nameof(caracteristics));

                caracteristicsValue = value;
            }
        }
        
        // Usado apenas pelo EF Core para materializar users lidos da BD.
        // Sem ele o EF chamaria o construtor abaixo, que valida e volta a fazer hash da password.
        private User() { }

        public User(int id, string name, string email, string password, DateOnly birthDate, string taxId, List<UserCaracteristics>? caracteristics)
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

        /// <summary>
        /// Edição parcial: qualquer argumento a null mantém o valor atual do utilizador.
        /// Se alguma validação falhar, o utilizador fica inalterado.
        /// </summary>
        public void UpdateUser(string? newName, string? newEmail, string? newPassword, List<UserCaracteristics>? newCaracteristics)
        {
            var previous = (nameValue, emailValue, passwordValue, caracteristicsValue);

            try
            {
                if (newName is not null) name = newName;
                if (newEmail is not null) email = newEmail;
                if (newPassword is not null) password = newPassword;

                if (newCaracteristics is not null)
                {
                    var updated = newCaracteristics.ToList();
                    if (!updated.Contains(UserCaracteristics.isActive))
                        updated.Add(UserCaracteristics.isActive);

                    caracteristics = updated;
                }
            }
            catch
            {
                (nameValue, emailValue, passwordValue, caracteristicsValue) = previous;
                throw;
            }
        }

        public void DeactivateUser()
        {
            if (!this.caracteristics.Contains(UserCaracteristics.isActive))
                throw new InvalidOperationException("User is already deactivated.");

            this.caracteristics = this.caracteristics
                .Where(characteristic => characteristic != UserCaracteristics.isActive)
                .ToList();
        }

        public bool HasCharacteristic(UserCaracteristics characteristic)
        {
            return caracteristics.Contains(characteristic);
        }

        public bool MatchesEmail(string? email)
        {
            return email is not null
                && string.Equals(emailValue, email.Trim(), StringComparison.OrdinalIgnoreCase);
        }

        public bool CheckPassword(string plainPassword)
        {
            var attempt = System.Text.Encoding.UTF8.GetBytes(HashPassword(plainPassword));
            var stored = System.Text.Encoding.UTF8.GetBytes(passwordValue);
            return CryptographicOperations.FixedTimeEquals(attempt, stored);
        }

        private string HashPassword(string password)
        {
            byte[] bytes = SHA256.HashData(System.Text.Encoding.UTF8.GetBytes(password));
            return Convert.ToHexString(bytes).ToLower();
        }

    }
}