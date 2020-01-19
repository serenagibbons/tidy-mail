using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tidy_Mail
{
    class Email : INotifyPropertyChanged
    {
        private bool isSelected;

        public string Id { get; set; }

        public bool IsSelected {
            get { return isSelected; }
            set { isSelected = value; NotifyPropertyChanged(); }
        }
        public string Snippet { get; set; }
        public string From { get; set; }
        public string Date { get; set; }
        public string Subject { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;

        private void NotifyPropertyChanged(string propertyName = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }
}
