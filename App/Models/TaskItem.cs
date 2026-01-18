using System;
using System.ComponentModel;
using System.Runtime.CompilerServices;

namespace App.Models
{
    public class TaskItem : INotifyPropertyChanged
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime CreateDate { get; set; } = DateTime.Now;
        public DateTime EndDate { get; set; } = DateTime.Now.AddDays(1);
        public string AttachmentPath { get; set; } = string.Empty;

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
                }
            }
        }

        public event PropertyChangedEventHandler? PropertyChanged;

        protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
