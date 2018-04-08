using System;
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

        /// <summary>
        /// Compares release version with app version
        /// </summary>
        public bool IsOlderThan(Version version)
        {
            var num = new[] { version.Major, version.Minor, version.Build, version.Revision };
            int delta = Enumerable.Range(0, num.Length)
                .Select(i => num[i] - Version[i])
                .SkipWhile(d => d == 0)
                .FirstOrDefault();
            return delta < 0;
        }
    }
}
