using System;
using System.Collections.Generic;
using System.Threading;
using System.Reflection;

using ICities;
using ColossalFramework;
using ColossalFramework.Plugins;
using ColossalFramework.Math;
using ColossalFramework.UI;
using UnityEngine;

namespace EnhancedGarbageTruckAI
{
    internal sealed class Helper
    {
        private static Helper _Instance = null;
        public static Helper Instance {
			get {
				if (Helper._Instance == null)
					Helper._Instance = new Helper (); // TODO: Why do we need a instance at all?
				return Helper._Instance;
			}
		}

        internal bool GameLoaded = false;

        public void Log(string message)
        {
            Debug.Log(String.Format("{0}: {1}", Settings.Instance.Tag, message));
        }

        public void NotifyPlayer(string message)
        {
            DebugOutputPanel.AddMessage(PluginManager.MessageType.Message, String.Format("{0}: {1}", Settings.Instance.Tag, message));
            Log(message);
        }
    }
}