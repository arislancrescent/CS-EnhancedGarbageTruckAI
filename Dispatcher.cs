using System;
using System.Collections.Generic;
using System.Threading;

using ICities;
using ColossalFramework;
using ColossalFramework.Math;
using ColossalFramework.Plugins;
using ColossalFramework.UI;
using UnityEngine;

namespace EnhancedGarbageTruckAI
{
    public class Dispatcher : ThreadingExtensionBase
    {
        private Settings _settings;
        private Helper _helper;

        private string _collecting = ColossalFramework.Globalization.Locale.Get("VEHICLE_STATUS_GARBAGE_COLLECT");

        private bool _initialized;
        private bool _baselined;
        private bool _terminated;

        private Dictionary<ushort, Landfill> _landfills;
        private Dictionary<ushort, DateTime> _master;

        protected bool IsOverwatched()
        {
            #if DEBUG

            return true;

            #else

            foreach (var plugin in PluginManager.instance.GetPluginsInfo())
            {
                if (plugin.publishedFileID.AsUInt64 == 421028969)
                    return true;
            }

            return false;

            #endif
        }

        public override void OnCreated(IThreading threading)
        {
            _settings = Settings.Instance;
            _helper = Helper.Instance;

            _initialized = false;
            _baselined = false;
            _terminated = false;

            base.OnCreated(threading);
        }

        public override void OnBeforeSimulationTick()
        {
            if (_terminated) return;

            if (!_helper.GameLoaded)
            {
                _initialized = false;
                _baselined = false;
                return;
            }

            base.OnBeforeSimulationTick();
        }

        public override void OnUpdate(float realTimeDelta, float simulationTimeDelta)
        {
            if (_terminated) return;

            if (!_helper.GameLoaded) return;

            try
            {
                if (!_initialized)
                {
                    if (!IsOverwatched())
                    {
                        _helper.NotifyPlayer("Skylines Overwatch not found. Terminating...");
                        _terminated = true;

                        return;
                    }

                    SkylinesOverwatch.Settings.Instance.Enable.BuildingMonitor = true;
                    SkylinesOverwatch.Settings.Instance.Enable.VehicleMonitor = true;

                    _landfills = new Dictionary<ushort, Landfill>();
                    _master = new Dictionary<ushort, DateTime>();

                    _initialized = true;

                    _helper.NotifyPlayer("Initialized");
                }
                else if (!_baselined)
                {
                    CreateBaseline();
                }
                else
                {
                    ProcessNewLandfills();
                    ProcessRemovedLandfills();
                    RemoveIncomingOffers();

                    ProcessNewPickups();

                    ProcessIdleGarbageTrucks();
                    UpdateGarbageTrucks();
                }
            }
            catch (Exception e)
            {
                string error = String.Format("Failed to {0}\r\n", !_initialized ? "initialize" : "updated");
                error += String.Format("Error: {0}\r\n", e.Message);
                error += "\r\n";
                error += "==== STACK TRACE ====\r\n";
                error += e.StackTrace;

                _helper.Log(error);

                if (!_initialized)
                    _terminated = true;
            }

            base.OnUpdate(realTimeDelta, simulationTimeDelta);
        }

        public override void OnReleased()
        {
            _initialized = false;
            _baselined = false;
            _terminated = false;

            base.OnReleased();
        }

        private void CreateBaseline()
        {
            SkylinesOverwatch.Data data = SkylinesOverwatch.Data.Instance;

            foreach (ushort id in data.LandfillSites)
                _landfills.Add(id, new Landfill(id, ref _master));

            foreach (ushort pickup in data.BuildingsWithGarbage)
            {
                foreach (ushort id in _landfills.Keys)
                    _landfills[id].AddPickup(pickup);
            }

            _baselined = true;
        }

        private void ProcessNewLandfills()
        {
            SkylinesOverwatch.Data data = SkylinesOverwatch.Data.Instance;

            foreach (ushort x in data.BuildingsUpdated)
            {
                if (!data.IsLandfillSite(x))
                    continue;

                if (_landfills.ContainsKey(x))
                    continue;

                _landfills.Add(x, new Landfill(x, ref _master));

                foreach (ushort pickup in data.BuildingsWithGarbage)
                {
                    foreach (ushort id in _landfills.Keys)
                        _landfills[id].AddPickup(pickup);
                }
            }
        }

        private void ProcessRemovedLandfills()
        {
            SkylinesOverwatch.Data data = SkylinesOverwatch.Data.Instance;

            foreach (ushort id in data.BuildingsRemoved)
                _landfills.Remove(id);
        }

        private void RemoveIncomingOffers()
        {
            foreach (ushort id in _landfills.Keys)
                _landfills[id].RemoveIncomingOffers();
        }

        private void ProcessNewPickups()
        {
            SkylinesOverwatch.Data data = SkylinesOverwatch.Data.Instance;

            foreach (ushort pickup in data.BuildingsUpdated)
            {
                if (data.IsBuildingWithGarbage(pickup))
                {
                    foreach (ushort id in _landfills.Keys)
                        _landfills[id].AddPickup(pickup);
                }
                else
                {
                    foreach (ushort id in _landfills.Keys)
                        _landfills[id].AddCheckup(pickup);
                }
            }
        }

        private void ProcessIdleGarbageTrucks()
        {
            SkylinesOverwatch.Data data = SkylinesOverwatch.Data.Instance;

            foreach (ushort x in data.BuildingsUpdated)
            {
                if (!data.IsLandfillSite(x))
                    continue;

                if (!_landfills.ContainsKey(x))
                    continue;

                _landfills[x].DispatchIdleVehicle();
            }
        }

        private void UpdateGarbageTrucks()
        {
            SkylinesOverwatch.Data data = SkylinesOverwatch.Data.Instance;
            Vehicle[] vehicles = Singleton<VehicleManager>.instance.m_vehicles.m_buffer;
            InstanceID instanceID = new InstanceID();

            foreach (ushort id in data.VehiclesUpdated)
            {
                if (!data.IsHearse(id))
                    continue;

                Vehicle v = vehicles[id];

                if (!_landfills.ContainsKey(v.m_sourceBuilding))
                    continue;

                if (v.Info.m_vehicleAI.GetLocalizedStatus(id, ref v, out instanceID) != _collecting)
                    continue;

                ushort target = _landfills[v.m_sourceBuilding].AssignTarget(v);

                if (target != 0 && target != v.m_targetBuilding)
                    v.Info.m_vehicleAI.SetTarget(id, ref vehicles[id], target);
            }
        }
    }
}

