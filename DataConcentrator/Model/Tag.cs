using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace DataConcentrator
{
    public class Tag : INotifyPropertyChanged
    {
        private string name;
        private string description;

        [Key]   // primarni kljuc u bazi podataka
        // ne mogu postojati 2 taga sa istim imenom
        public string Name  
        {
            get { return name; }
            set { name = value; OnPropertyChanged("Name"); }
        }

        public string Description
        {
            get { return description; }
            set { description = value; OnPropertyChanged("Description"); }
        }

        public string IOAddress { get; set; }

        public event PropertyChangedEventHandler PropertyChanged;   // prikaz u tabeli

        public void OnPropertyChanged(string property)  // salje obavjestenje svima koji su se pretplatili
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
        }
    }
}
