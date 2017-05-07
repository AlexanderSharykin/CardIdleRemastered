using System.Linq;

namespace CardIdleRemastered
{
    public class ReleaseInfo
    {
        public string Title { get; set; }
        
        public string Date { get; set; }

        private int[] _version;
        public int[] Version
        {
            get
            {
                if (_version == null && Title != null)                
                    _version = (Title.ToLower().Trim('v') + ".0.0").Split('.').Take(4).Select(int.Parse).ToArray();                
                
                return _version;
            }            
        }
    }
}
