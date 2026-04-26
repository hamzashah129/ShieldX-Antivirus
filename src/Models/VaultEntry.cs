using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShieldX.Models
{
    /// <summary>
    /// Represents a single password vault entry
    /// </summary>
    public class VaultEntry : INotifyPropertyChanged
    {
        private string _id;
        private string _title;
        private string _username;
        private string _password;
        private string _website;
        private string _notes;
        private DateTime _createdDate;
        private DateTime _modifiedDate;

        public string Id
        {
            get => _id;
            set
            {
                if (_id != value)
                {
                    _id = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Title
        {
            get => _title;
            set
            {
                if (_title != value)
                {
                    _title = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Username
        {
            get => _username;
            set
            {
                if (_username != value)
                {
                    _username = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Password
        {
            get => _password;
            set
            {
                if (_password != value)
                {
                    _password = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Website
        {
            get => _website;
            set
            {
                if (_website != value)
                {
                    _website = value;
                    OnPropertyChanged();
                }
            }
        }

        public string Notes
        {
            get => _notes;
            set
            {
                if (_notes != value)
                {
                    _notes = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime CreatedDate
        {
            get => _createdDate;
            set
            {
                if (_createdDate != value)
                {
                    _createdDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime ModifiedDate
        {
            get => _modifiedDate;
            set
            {
                if (_modifiedDate != value)
                {
                    _modifiedDate = value;
                    OnPropertyChanged();
                }
            }
        }

        public string DisplayTitle => string.IsNullOrWhiteSpace(Title) ? "Untitled Entry" : Title;
        public string DisplayUsername => string.IsNullOrWhiteSpace(Username) ? "No username" : Username;
        public string CreatedLabel => CreatedDate.ToString("MMM dd, yyyy");
        public string PasswordStrength
        {
            get
            {
                if (string.IsNullOrEmpty(Password))
                    return "None";
                if (Password.Length < 8)
                    return "Weak";
                if (Password.Length < 12)
                    return "Fair";
                if (Password.Length < 16)
                    return "Good";
                return "Strong";
            }
        }
        // Aliases for backward compatibility
        public string Site
        {
            get => Website;
            set => Website = value;
        }

        private string _category = "General";
        public string Category
        {
            get => _category;
            set
            {
                if (_category != value)
                {
                    _category = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _added = DateTime.Now.ToString();
        public string Added
        {
            get => _added;
            set
            {
                if (_added != value)
                {
                    _added = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _showPassword;
        public bool ShowPassword
        {
            get => _showPassword;
            set
            {
                if (_showPassword != value)
                {
                    _showPassword = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(DisplayPassword));
                }
            }
        }

        public string DisplayPassword => ShowPassword ? Password : new string('*', Password?.Length ?? 0);

        public void ToggleShow()
        {
            ShowPassword = !ShowPassword;
        }
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
