using System;
using System.Collections.Generic;

namespace EnhancedGarbageTruckAI
{
    public sealed class Settings
    {
        private Settings()
        {
            Tag = "[ARIS] Enhanced Garbage Truck AI";

            DispatchGap = 5;
        }

        private static Settings _Instance = null;
        public static Settings Instance {
			get {
				if (Settings._Instance == null)
					Settings._Instance = new Settings (); // TODO: Why not make Tag and DispatcherGap static?
				return Settings._Instance;
			}
		}

        public readonly string Tag;

        public readonly int DispatchGap;
    }
}