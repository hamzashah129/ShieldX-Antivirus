using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace ShieldX.Models
{
    /// <summary>
    /// Represents a data breach record from HaveIBeenPwned API
    /// </summary>
    public class BreachData : INotifyPropertyChanged
    {
        private string _title;
        private string _breachName;
        private DateTime _breachDate;
        private DateTime _addedDate;
        private DateTime _modifiedDate;
        private int _pwnCount;
        private string _description;
        private bool _isVerified;
        private bool _isFabricated;
        private bool _isSensitive;
        private bool _isRetired;
        private bool _isSpamList;
        private List<string> _dataClasses;
        private string _domain;
        private string _logoPath;

        /// <summary>Name of the breach</summary>
        public string Title
        {
            get => _title;
            set { if (_title != value) { _title = value; OnPropertyChanged(); } }
        }

        /// <summary>Formal name as defined by HIBP</summary>
        public string Name { get; set; } = "";

        /// <summary>Date when the breach occurred</summary>
        public DateTime BreachDate
        {
            get => _breachDate;
            set { if (_breachDate != value) { _breachDate = value; OnPropertyChanged(); } }
        }

        /// <summary>When the breach was added to HIBP</summary>
        public DateTime AddedDate
        {
            get => _addedDate;
            set { if (_addedDate != value) { _addedDate = value; OnPropertyChanged(); } }
        }

        /// <summary>When the breach was last modified in HIBP</summary>
        public DateTime ModifiedDate
        {
            get => _modifiedDate;
            set { if (_modifiedDate != value) { _modifiedDate = value; OnPropertyChanged(); } }
        }

        /// <summary>Total number of accounts pwned in this breach</summary>
        public int PwnCount
        {
            get => _pwnCount;
            set { if (_pwnCount != value) { _pwnCount = value; OnPropertyChanged(); } }
        }

        /// <summary>Description of the breach</summary>
        public string Description
        {
            get => _description;
            set { if (_description != value) { _description = value; OnPropertyChanged(); } }
        }

        /// <summary>Whether the breach is verified</summary>
        public bool IsVerified
        {
            get => _isVerified;
            set { if (_isVerified != value) { _isVerified = value; OnPropertyChanged(); } }
        }

        /// <summary>Whether the breach is fabricated</summary>
        public bool IsFabricated
        {
            get => _isFabricated;
            set { if (_isFabricated != value) { _isFabricated = value; OnPropertyChanged(); } }
        }

        /// <summary>Whether the breach contains sensitive personal data</summary>
        public bool IsSensitive
        {
            get => _isSensitive;
            set { if (_isSensitive != value) { _isSensitive = value; OnPropertyChanged(); } }
        }

        /// <summary>Whether the breach has been retired</summary>
        public bool IsRetired
        {
            get => _isRetired;
            set { if (_isRetired != value) { _isRetired = value; OnPropertyChanged(); } }
        }

        /// <summary>Whether this is a spam list rather than a breach</summary>
        public bool IsSpamList
        {
            get => _isSpamList;
            set { if (_isSpamList != value) { _isSpamList = value; OnPropertyChanged(); } }
        }

        /// <summary>Types of data exposed in the breach</summary>
        public List<string> DataClasses
        {
            get => _dataClasses;
            set { if (_dataClasses != value) { _dataClasses = value; OnPropertyChanged(); } }
        }

        /// <summary>Domain associated with the breach</summary>
        public string Domain
        {
            get => _domain;
            set { if (_domain != value) { _domain = value; OnPropertyChanged(); } }
        }

        /// <summary>URL to the logo for the breach</summary>
        public string LogoPath
        {
            get => _logoPath;
            set { if (_logoPath != value) { _logoPath = value; OnPropertyChanged(); } }
        }

        /// <summary>Formatted breach date string</summary>
        public string BreachDateLabel => BreachDate.ToString("MMMM d, yyyy");

        /// <summary>Formatted PWN count with thousands separator</summary>
        public string PwnCountLabel => PwnCount.ToString("N0");

        /// <summary>Comma-separated list of exposed data types</summary>
        public string DataClassesLabel => DataClasses != null ? string.Join(", ", DataClasses) : "Unknown";

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string name = "") =>
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));
    }
}
