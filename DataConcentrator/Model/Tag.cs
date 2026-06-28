using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataConcentrator
{
    public class Tag : INotifyPropertyChanged
    {
        private string name;
        private string description;

        [Key]
        public string Name  // primarni kljuc Entity Framewourk
        {
            get { return name; }
            set { name = value; OnPropertyChanged("Name"); }
        }

        public string Description
        {
            get { return description; }
            set { description = value; OnPropertyChanged("Description"); }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        public void OnPropertyChanged(string property)  // salje obavjestenje
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
