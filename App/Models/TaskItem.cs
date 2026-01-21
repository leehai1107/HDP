using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace App.Models
{
    public class TaskItem : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        
        private string _name = string.Empty;
        public string Name
        {
            get => _name;
            set
            {
                if (_name != value)
                {
                    _name = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _description = string.Empty;
        public string Description
        {
            get => _description;
            set
            {
                if (_description != value)
                {
                    _description = value;
                    OnPropertyChanged();
                }
            }
        }

        public DateTime CreateDate { get; set; } = DateTime.Now;

        private DateTime _endDate = DateTime.Now.AddDays(1);
        public DateTime EndDate
        {
            get => _endDate;
            set
            {
                if (_endDate != value)
                {
                    _endDate = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RemainingDays));
                }
            }
        }

        private string _attachmentPath = string.Empty;
        public string AttachmentPath
        {
            get => _attachmentPath;
            set
            {
                if (_attachmentPath != value)
                {
                    _attachmentPath = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _status = "Pending";
        public string Status
        {
            get => _status;
            set
            {
                if (_status != value)
                {
                    _status = value;
                    OnPropertyChanged();
                }
            }
        }

        private bool _isDone;
        public bool IsDone
        {
            get => _isDone;
            set
            {
                if (_isDone != value)
                {
                    _isDone = value;
                    OnPropertyChanged();
                    OnPropertyChanged(nameof(RemainingDays));
                }
            }
        }

        private string _assignedTo = string.Empty;
        public string AssignedTo
        {
            get => _assignedTo;
            set
            {
                if (_assignedTo != value)
                {
                    _assignedTo = value;
                    OnPropertyChanged();
                }
            }
        }

        private string _assignedToId = string.Empty;
        public string AssignedToId
        {
            get => _assignedToId;
            set
            {
                if (_assignedToId != value)
                {
                    _assignedToId = value;
                    OnPropertyChanged();
                }
            }
        }

        public int RemainingDays
        {
            get
            {
                if (IsDone) return 0;
                var timeSpan = EndDate.Date - DateTime.Now.Date;
                return (int)timeSpan.TotalDays;
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
