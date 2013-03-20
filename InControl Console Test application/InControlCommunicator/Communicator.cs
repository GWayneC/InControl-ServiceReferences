using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using System.Reflection;
using InControlServiceReference.InControlService;
using InControlServiceReference;

namespace InControlCommunicator
{
    public class Communicator
    {
        private string url = string.Empty;
        private string password = string.Empty;
        private int port = 0;
        public Communicator(string serviceUrl, string passwordString,int port1)
        {
            url = serviceUrl;
            password = passwordString;
            port = port1;
            GetDevices();
            LastUpdate = DateTime.Now;
        }
        #region IZwaveInterface Members
        public Thermostat GetThermostat(string id)
        {
            var service = HaService.getService(url, port);
            Thermostat device = (Thermostat)service.getDevice(password, id);
            Logger.LogInfo(String.Format("The {0}.Level = {1} last change {2}", device.name, device.level, device.lastLevelUpdate));
            return device;
        }
        public HaDevice GetDevice(Guid id)
        {
            return GetDevice(id.ToString());
        }
        public HaDevice GetDevice(HaDevice device)
        {
            return GetDevice(device.deviceId.ToString());
        }
        public HaDevice GetDevice(string id)
        {
            var service = HaService.getService(url, port);
            HaDevice device = service.getDevice(password, id);
            return device;
        }
        public string TurnUp(string deviceId)
        {
            //TODO: add handling for cooling
            var service = HaService.getService(url, port);
            HaDevice device = service.getDevice(password, deviceId);
            if (device.deviceType == DeviceType.Thermostat)
            {
                Thermostat t = (Thermostat)device;
                if (t.thermostatSystemState == ThermoSystemMode.Heat)
                {
                    return GetThermostatState(t);
                }
                if ((int)t.level < 99)
                {
                    return SetComfortMode(t.deviceId.ToString());
                }
                if (((int)t.level > 0) && (t.thermostatSystemState == ThermoSystemMode.Heat))
                {
                    ThermoSetPoint heating1 = t.thermostatSetPoints.ToList<ThermoSetPoint>().Find(s => (s.pointName == "Heating1"));
                    return SetThermostat(t.deviceId.ToString(), (int)heating1.temperature + 2);
                }
                if (((int)t.level > 0) && (t.thermostatSystemState == ThermoSystemMode.Cool))
                {
                    ThermoSetPoint heating1 = t.thermostatSetPoints.ToList<ThermoSetPoint>().Find(s => (s.pointName == "Cooling1"));
                    return SetThermostat(t.deviceId.ToString(), (int)heating1.temperature - 2);
                }

            }
            else
            {
                int currentLevel = (int)device.level;
                return Dim(device.deviceId.ToString(), currentLevel + 5);
            }
            return "Not implemented";
        }
        public string TurnDown(string deviceId)
        {
            //TODO: add handling for cooling
            var service = HaService.getService(url, port);
            HaDevice device = service.getDevice(password, deviceId);
            if (device.deviceType == DeviceType.Thermostat)
            {
                //if thermostat is running return
                //if thermostat is in econ mode set to comfort
                //if thermostat is in comfort and not running increase the setpoint by 2 degrees
                //if thermostat is in HEAT mode reduce the Heating setpoint by 2 degrees 
            }
            else
            {
                int newlevel = (int)device.level - 5;
                if (newlevel < 0)
                {
                    return TurnOff(device);
                }
                else
                {
                    return Dim(device.deviceId.ToString(), newlevel);
                }

            }
            return "Not implemented";
        }
        public string TurnOff(Guid deviceId)
        {
            return TurnOff(deviceId.ToString());
        }
        public string TurnOff(HaDevice device)
        {
            return TurnOff(device.deviceId.ToString());
        }
        public string TurnOff(string deviceId)
        {
            var service = HaService.getService(url, port);
            Guid deviceGuid = Guid.Parse(deviceId);
            service.setDeviceState(password, deviceGuid, false, (byte)0);
            return GetDeviceState(service.getDevice(password, deviceId));
        }
        public string TurnOn(Guid deviceId)
        {
            return TurnOn(deviceId.ToString());
        }
        public string TurnOn(HaDevice device)
        {
            return TurnOn(device.deviceId.ToString());
        }
        public string TurnOn(string deviceId)
        {
            var service = HaService.getService(url, port);
            Guid deviceGuid = Guid.Parse(deviceId);
            service.setDeviceState(password, deviceGuid, true, (byte)100);
            return GetDeviceState(service.getDevice(password, deviceId));
        }
        public int GetDevices()
        {
            var service = HaService.getService(url, port);
            Devices = new List<HaDevice>();
            Thermostats = new List<Thermostat>();
            Devices = service.devices(password).ToList<HaDevice>();
            foreach (HaDevice d in Devices.FindAll(o => (o.deviceType == DeviceType.Thermostat)))
            {
                Thermostats.Add((Thermostat)d);
            }
            int dcount = Devices.Count;
            Devices.RemoveAll(o => (o.deviceType == DeviceType.Thermostat));
            Devices.RemoveAll(o => (o.deviceType == DeviceType.LevelDisplayer));
            return dcount;
        }
        public string GetDeviceState(object obj)
        {
            if (obj is Thermostat)
            {
                return GetThermostatState((Thermostat)obj);
            }
            return GetDeviceState((HaDevice)obj);
        }
        public string GetThermostatState(Thermostat thermostat)
        {
            string tstate = "The {0} is set to {1}({2}), is {3} and the temp. is {4}";
            SensorReading setpoint = null;
            int level = (int)thermostat.level;
            string setMode = "unknown";
            if (level >= 99)
            {
                setMode = "comfort";
            }
            if (level == 0)
            {
                setMode = "econ";
            }
            else
            {
                setMode = "comfort";
            }
            string currentState = string.Empty;
            try
            {
                #region setpoints
                if ((thermostat.sr != null) && (thermostat.sr.Length > 0))
                {
                    List<SensorReading> setpoints = thermostat.sr.ToList<SensorReading>();
                    SensorReading temperature = setpoints.Find(s => (s.name.StartsWith("Temperature")));
                    string state = string.Empty;
                    switch (thermostat.thermostatSystemMode)
                    {
                        case ThermoSystemMode.Heat:
                            setpoint = (setMode == "comfort") ? setpoints.Find(s => (s.name == "Heating1")) : setpoints.Find(s => (s.name == "EnergyHeat"));
                            break;
                        case ThermoSystemMode.Cool:
                            setpoint = (setMode == "comfort") ? setpoints.Find(s => (s.name == "Cooling1")) : setpoints.Find(s => (s.name == "EnergyCool"));
                            break;
                        case ThermoSystemMode.Off:
                            return "The " + thermostat.name + " is set to " + setMode + ", and is off, current temp. is " + thermostat.currentThermTempF;
                        case ThermoSystemMode.EnergySaveCool:
                            setpoint = setpoints.Find(s => (s.name == "EnergyHeat"));
                            break;
                        case ThermoSystemMode.EnergySaveHeat:
                            setpoint = setpoints.Find(s => (s.name == "EnergyCool"));
                            break;
                        default:
                            Console.WriteLine(thermostat.thermostatSystemMode.ToString());
                            break;
                    }
                }
                #endregion
                string sysState = (thermostat.thermostatSystemState.ToString().ToUpper() == "OFF") ? "idle" : thermostat.thermostatSystemState.ToString().ToLower() + "ing";
                string t_state = String.Format(tstate, thermostat.deviceName, setMode, setpoint.value, sysState, thermostat.currentThermTempF);
                return t_state;
            }
            catch
            {
                return "Unable to retrieve " + thermostat.name + " information";
            }
        }
        public string GetDeviceState(HaDevice device)
        {
            string status = string.Empty;
            switch (device.deviceType)
            {
                case DeviceType.Thermostat:
                    if ((device.sr != null) && (device.sr.Length > 0))
                    {
                        List<SensorReading> setpoints = device.sr.ToList<SensorReading>();
                        SensorReading temperature = setpoints.Find(s => (s.name.StartsWith("Temperature")));
                        string state = string.Empty;
                        if (device.level > 0)
                        {
                            SensorReading heating1 = setpoints.Find(s => (s.name == "Heating1"));
                            SensorReading cooling1 = setpoints.Find(s => (s.name == "Cooling1"));
                            state = "Comfort(" + heating1.value + ")";
                        }
                        else
                        {
                            SensorReading energyHeat = setpoints.Find(s => (s.name == "EnergyHeat"));
                            SensorReading energyCool = setpoints.Find(s => (s.name == "EnergyCool"));
                            state = "Econ(" + energyHeat.value + ")";
                        }
                        status = String.Format("The {0} is set to {1} and the temp. is {2}", device.deviceName, state, temperature.value);
                    }
                    else
                    {
                        status = "Unable to get readings from " + device.name;
                    }
                    break;
                case DeviceType.DimmerSwitch:
                    if (device.level == 0)
                    {
                        status = "The " + device.name + " is off";
                    }
                    else
                    {
                        status = "The " + device.name + " is on ";
                        if (device.level < 100)
                        {
                            status = status + " and is at " + device.level + "%";
                        }
                    }
                    break;
                case DeviceType.ZonePlayer:
                case DeviceType.StandardSwitch:
                    status = String.Format("The {0} is {1}", device.name, (device.level == 0) ? "off" : "on");
                    break;
                case DeviceType.LevelDisplayer:
                    status = String.Format("The value of {0} is {1}", device.name, device.level);
                    break;
                default:
                    Console.WriteLine(device.name + " is " + device.deviceType.ToString());
                    break;
            }
            return status;
        }
        public List<SceneDTO> GetScenes()
        {
            var service = HaService.getService(url, port);
            return service.getScenes(password).ToList<SceneDTO>();
        }
        public string Dim(string deviceId, int level)
        {
            var service = HaService.getService(url, port);
            Guid deviceGuid = Guid.Parse(deviceId);
            service.setDeviceState(password, deviceGuid, true, (byte)level);
            return GetDeviceState(service.getDevice(password, deviceId));
        }
        public string SetThermostat(string deviceId, int temperature)
        {
            Thermostat t = Thermostats.Find(o => (o.deviceId.ToString() == deviceId));
            return SetTemperature(t, temperature);
        }
        public string ActivateComfortMode(string deviceId)
        {
            throw new NotImplementedException();
        }
        public string ActivateSaveMode(string deviceId)
        {
            throw new NotImplementedException();
        }
        public string ActivateScene(string sceneName)
        {
            throw new NotImplementedException();
        }
        public string SetEconMode(string deviceId)
        {
            var service = HaService.getService(url, port);
            Guid deviceGuid = Guid.Parse(deviceId);
            service.setDeviceState(password, deviceGuid, false, (byte)0);
            return GetThermostatState((Thermostat)service.getDevice(password, deviceId));
        }
        public string SetComfortMode(string deviceId)
        {
            var service = HaService.getService(url, port);
            Guid deviceGuid = Guid.Parse(deviceId);
            service.setDeviceState(password, deviceGuid, true, (byte)255);
            return GetThermostatState((Thermostat)service.getDevice(password, deviceId));
        }
        public string SetThermostat(string deviceId, string sysMode)
        {
            string request = String.Format("{0}/thermoSetSystemMode?nodeId={1}&systemMode={2}&password={3}", url, deviceId, sysMode, password);
            ThermoSystemMode mode = (ThermoSystemMode)Enum.Parse(typeof(ThermoSystemMode), sysMode, true);
            var service = HaService.getService(url, port);
            service.setThermoSystemMode(password, deviceId, mode);
            return GetThermostatState((Thermostat)service.getDevice(password, deviceId));
        }
        public string SetTemperature(Thermostat t, int temperature)
        {
            var service = HaService.getService(url, port);
            string setPointname = string.Empty;
            switch (t.thermostatSystemMode)
            {
                case ThermoSystemMode.Heat:
                    service.setThermoSetpoint(password, t.deviceId.ToString(), "Heating1", (decimal)temperature);
                    setPointname = "Heating1";
                    break;
                case ThermoSystemMode.Cool:
                    service.setThermoSetpoint(password, t.deviceId.ToString(), "Cooling1", (decimal)temperature);
                    setPointname = "Cooling1";
                    break;
                default:
                    if ((DateTime.Now.Month >= 1) && (DateTime.Now.Month <= 4))
                    {
                        service.setThermoSetpoint(password, t.deviceId.ToString(), "Heating1", (decimal)temperature);
                        setPointname = "Heating1";
                    }
                    break;
            }
            Thermostat tChanged = (Thermostat)service.getDevice(password, t.nodeId.ToString());
            return GetThermostatState(tChanged);
        }
        #endregion
        #region Helper Methods
        public void GetObjectProperties(object device)
        {
            Console.ForegroundColor = ConsoleColor.Green;
            foreach (PropertyInfo prop in device.GetType().GetProperties())
            {
                if (prop.CanRead)
                {
                    switch (prop.Name)
                    {
                        case "thermoStatStateTracker":
                            StateTracker tracker = (StateTracker)prop.GetValue(device, null);
                            break;
                        case "thermostatSetPoints":
                            Console.WriteLine(prop.Name);
                            object o = prop.GetValue(device, null);
                            if (o != null)
                            {
                                ThermoSetPoint[] os = (ThermoSetPoint[])o;
                                foreach (ThermoSetPoint tsp in os)
                                {
                                    Console.WriteLine("  -" + tsp.pointName + " = " + tsp.temperature);
                                }
                            }
                            break;
                        case "levelPresets":
                            Console.WriteLine(prop.Name);
                            object l = prop.GetValue(device, null);
                            if (l != null)
                            {
                                LevelPreset[] lps = (LevelPreset[])l;
                                foreach (LevelPreset lp in lps)
                                {
                                    Console.WriteLine("  -" + lp.presetLocation + " = " + lp.level);
                                }
                            }
                            break;
                        case "sr":
                            Console.WriteLine(prop.Name);
                            object s = prop.GetValue(device, null);
                            if (s != null)
                            {
                                SensorReading[] srs = (SensorReading[])s;
                                foreach (SensorReading sr in srs)
                                {
                                    Console.WriteLine("  -" + sr.name + " = " + sr.value);
                                }
                            }
                            break;
                        default:
                            Console.WriteLine(prop.Name + " = " + prop.GetValue(device, null));
                            break;
                    }

                }
            }
            Console.ResetColor();
        }
        #endregion
        public List<Thermostat> Thermostats { get; set; }
        public List<HaDevice> Devices { get; set; }
        public int DeviceCount { get { return this.Devices.Count + this.Thermostats.Count; } }
        public DateTime LastUpdate { get; set; }
        #region IDeviceManagerInterface Members
        public void Connect()
        {
            throw new NotImplementedException();
        }
        public void Disconnect()
        {
            throw new NotImplementedException();
        }
        public bool Connected
        {
            get { throw new NotImplementedException(); }
        }
        #endregion
    }
}
