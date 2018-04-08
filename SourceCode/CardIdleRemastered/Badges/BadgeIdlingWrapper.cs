using System;

namespace CardIdleRemastered
{
    public class BadgeIdlingWrapper
    {
        public BadgeModel Badge { get; set; }

        /// <summary>
        /// Play hours registered by Steam
        /// </summary>
        public double Hours { get; set; }

        /// <summary>
        /// Idling minutes counted by idler
        /// </summary>
        public int Minutes { get; set; }

        public bool IsTrial { get; set; }
    }
}
