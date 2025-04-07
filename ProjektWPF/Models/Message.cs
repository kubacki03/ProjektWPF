using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ProjektWPF.Models
{
    using System.ComponentModel;

    namespace ProjektWPF.Models
    {
        public class Message : INotifyPropertyChanged
        {
            private string _author;
            private string _content;

            public string Author
            {
                get { return _author; }
                set
                {
                    if (_author != value)
                    {
                        _author = value;
                        OnPropertyChanged(nameof(Author));
                    }
                }
            }

            public string Content
            {
                get { return _content; }
                set
                {
                    if (_content != value)
                    {
                        _content = value;
                        OnPropertyChanged(nameof(Content));
                    }
                }
            }

            public event PropertyChangedEventHandler PropertyChanged;

            protected virtual void OnPropertyChanged(string propertyName)
            {
                PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            }
        }
    }




}
