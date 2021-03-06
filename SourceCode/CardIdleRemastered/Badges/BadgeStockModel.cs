﻿using System;

namespace CardIdleRemastered.Badges
{
    public class BadgeStockModel
    {
        public string Name { get; set; }

        public string CardRelease { get; set; }

        public int Count { get; set; }

        public double Normal { get; set; }

        public double CardValue
        {
            get { return Math.Round(Normal / Count, 2); }
        }

        public double Foil { get; set; }

        public int NormalStock { get; set; }

        public int FoilStock { get; set; }
    }
}
