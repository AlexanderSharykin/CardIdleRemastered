using System;

namespace CardIdleRemastered
{
    public class BadgeLevelData : ObservableModel
    {
        private bool _isCompleted;
        public string Level { get; set; }

        public string PictureUrl { get; set; }

        public string Name { get; set; }

        public bool IsCompleted
        {
            get { return _isCompleted; }
            set
            {
                _isCompleted = value;
                OnPropertyChanged();
            }
        }
    }
}
