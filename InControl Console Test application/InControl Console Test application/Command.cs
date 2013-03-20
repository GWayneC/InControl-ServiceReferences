using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using InControlCommunicator;
using InControlServiceReference.InControlService;

namespace InControl_Console_Test_application
{
    public class Action
    {
        public Action(string actionStr, CommandActions action)
        {
            actionString = actionStr;
            commandAction = action;
        }
        public string actionString { get; set; }
        public CommandActions commandAction{ get; set; }
    }
    public class Command
    {
        private List<Action> availableActions = null;
        public Command(string request,Communicator iClient)
        {
            CommandString = request;
            GetAvailableActions();
            Value = -1;
            Request = CommandActions.Unknown;
            IdentifyDevice(request,iClient);
            if (DeviceFound)
            {
                string newrequest = request.ToLower().Replace("the", "");
                if (DeviceIsThermostat)
                {
                    newrequest = newrequest.Replace(thermostat.name.ToLower(), "");
                }
                else
                {
                    newrequest = newrequest.Replace(device.name.ToLower(), "");
                }
                request = newrequest.Trim();
            }
            SetCommand(request);
            Valid = true;
        }
        private void GetAvailableActions()
        {
            availableActions = new List<Action>();
            availableActions.Add(new Action("is the", CommandActions.GetStatus));
            availableActions.Add(new Action("what is the status of the", CommandActions.GetStatus));
            availableActions.Add(new Action("turn on", CommandActions.TurnOn));
            availableActions.Add(new Action("turn off", CommandActions.TurnOff));
            availableActions.Add(new Action("turn up", CommandActions.TurnUp));
            availableActions.Add(new Action("turn down", CommandActions.TurnDown));
            availableActions.Add(new Action("raise the", CommandActions.TurnUp));
            availableActions.Add(new Action("lower the", CommandActions.TurnDown));
            availableActions.Add(new Action(" comfort ", CommandActions.SetComfort));
            availableActions.Add(new Action(" econ ", CommandActions.SetEcon));
        }
        private void SetCommand(string request)
        {
            if (request.ToLower().Contains("away"))
            {
                Request = CommandActions.AwayMode;
                return;
            }
            if (request.ToLower().Contains("properties"))
            {
                Request = CommandActions.GetProperties;
                return;
            }
            if (request.ToLower().Contains("status"))
            {
                Request = CommandActions.GetStatus;
                return;
            }
            if (request.ToLower().Contains("turn up"))
            {
                Request = (DeviceIsThermostat) ? CommandActions.SetComfort : CommandActions.TurnUp;
                return;
            }
            if (request.ToLower().Contains("turn on"))
            {
                Request = CommandActions.TurnOn;
                return;
            }
            if (request.ToLower().Contains("turn down"))
            {
                Request = (DeviceIsThermostat) ? CommandActions.SetEcon : CommandActions.TurnDown;
                return;
            }
            if (request.ToLower().Contains("turn off"))
            {
                Request = CommandActions.TurnOff;
                return;
            }
            CheckForValue(request);
            if (Value >= 0)
            {
                Request = CommandActions.Setvalue;
            }
            foreach (Action action in availableActions)
            {
                if (request.ToLower().Contains(action.actionString))
                {
                    Request = action.commandAction;
                    return;
                }
            }
        }
        private void IdentifyDevice(string request,Communicator client)
        {
            HaDevice d = null;
            Thermostat t = null;
            DeviceFound = true;
            //InControlCommunicator client = new InControlCommunicator();
            foreach (HaDevice dvc in client.Devices)
            {
                if (request.ToLower().Contains(dvc.name.ToLower()))
                {
                    DeviceIsThermostat = false;
                    device = dvc;
                    return;
                }
            }
            foreach (Thermostat tstat in client.Thermostats)
            {
                if (request.ToLower().Contains(tstat.name.ToLower()))
                {
                    DeviceIsThermostat = true;
                    thermostat = tstat;
                    return;
                }
            }
            foreach (string token in request.Split(' '))
            {
                if (!string.IsNullOrEmpty(token) && token.Length > 5)
                {
                    d = client.Devices.Find(o => (o.name.ToLower().StartsWith(token.ToLower())));
                    if (d != null)
                    {
                        DeviceIsThermostat = false;
                        device = d;
                        return;
                    }
                    t = client.Thermostats.Find(o => (o.name.ToLower().StartsWith(token.ToLower())));
                    if (t != null)
                    {
                        DeviceIsThermostat = true;
                        thermostat = t;
                        return;
                    }
                }
            }
            if (request.ToLower().Contains("all"))
            {
                device = new HaDevice();
                device.deviceName = "All";
                return;
            }
            if (request.ToLower().Contains("both"))
            {
                device = new HaDevice();
                device.deviceName = "both";
                return;
            }
            DeviceFound = false;
        }
        private void CheckForValue(string request)
        {
            Regex re = new Regex(@"\d+");
            Match m = re.Match(request);

            if (m.Success)
            {
                Value = Convert.ToInt16(m.Value);
            }
            else
            {
                Value = -1;
            }
        }
        public CommandActions Request { get; set; }
        public int Value { get; set; }
        public HaDevice device { get; set; }
        public Thermostat thermostat { get; set; }
        public bool DeviceIsThermostat { get; set; }
        public bool DeviceFound { get; set; }
        public bool Valid { get; set; }
        public string CommandString { get; set; }
    }
}
