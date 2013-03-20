using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using InControlServiceReference.InControlService;
using InControlCommunicator;
using System.Threading;

namespace InControl_Console_Test_application {
    class Program
    {
        static string name = string.Empty;
        static bool deviceISThermostat = false;
        static Communicator client = null;
        static void Main(string[] args)
        {
            Console.WindowWidth = Console.WindowWidth + 5;
            do
            {
                Console.Write("Hi, please enter your name: ");
                name = Console.ReadLine();
                Console.Clear();
            } while (String.IsNullOrEmpty(name));
            string url = InControl_Console_Test_application.Properties.Settings.Default.Server;
            string password = InControl_Console_Test_application.Properties.Settings.Default.Password;
            int port = InControl_Console_Test_application.Properties.Settings.Default.Port;
            client = new Communicator(url, password, port);
            foreach (SceneDTO scene in client.GetScenes())
            {
                Console.WriteLine(scene.sceneName);
            }
            Console.WriteLine("Hello " + name + ", welcome, there are " + client.DeviceCount + " devices that I can control");
            DisplayDeviceState();

            Console.Write("\nSo what can I do for you today? ");
            //client.GetScenes();
            string request = Console.ReadLine();

            while (request != "quit")
            {
                if (request.ToLower() == "status")
                {
                    DisplayDeviceState();
                }
                else if (request.ToLower() == "wizard")
                {
                    RunWizard();
                }
                else if (request.ToLower() == "away mode")
                {
                    foreach (HaDevice d in client.Devices)
                    {
                        Animate("Turning off " + d.name);
                        Animate(client.TurnOff(d.deviceId));
                    }
                    foreach (Thermostat t in client.Thermostats)
                    {
                        Animate("Turning down " + t.name);
                        Animate(client.SetEconMode(t.deviceId.ToString()));
                    }
                    Console.WriteLine("Waiting 5 seconds for devices to update");
                    int per = 0;
                    for (int delay = 5; delay > 0; delay--)
                    {
                        per += 20;
                        RenderConsoleProgress(per, '\u2592', ConsoleColor.Green, per + "%");
                        Thread.Sleep(1000);
                    }
                    DisplayDeviceState();
                }
                else
                {
                    var device = GetDevice(request);
                    WriteLine('*');
                    Console.WriteLine(client.GetDeviceState(device));
                    WriteLine('*');
                    Console.WriteLine("Executing the request");
                    Console.WriteLine(Process_Response(request, device));
                    Console.WriteLine("Waiting 5 seconds for devices to update");
                    int per = 0;
                    for (int delay = 5; delay > 0; delay--)
                    {
                        per += 20;
                        RenderConsoleProgress(per, '\u2592', ConsoleColor.Green, per + "%");
                        Thread.Sleep(1000);
                    }
                    Console.WriteLine("\nRefreshing device statuses");
                    client.GetDevices();
                    if (deviceISThermostat)
                    {
                        Thermostat t = (Thermostat)device;
                        device = client.Thermostats.Find(o => (o.deviceId == t.deviceId));
                    }
                    else
                    {
                        HaDevice d = (HaDevice)device;
                        device = client.Devices.Find(o => (o.deviceId == d.deviceId));
                    }
                    WriteLine('*');
                    Console.WriteLine(client.GetDeviceState(device));
                    WriteLine('*');
                }

                Animate("What would you like me to do next? ");
                request = Console.ReadLine();
                Console.Clear();
            }
            int pe2r = 0;
            for (int q = 10; q > 0; q--)
            {
                pe2r += 10;
                RenderConsoleProgress(pe2r, '\u2592', ConsoleColor.Red, " exiting in " + q + " seconds");
                Thread.Sleep(1000);
            }
        }
        private static void RunWizard()
        {
            var device = GetDevice("");
            WriteLine('~');
            //Animate(client.GetDeviceState(device));
            WriteLine('~');
            if (deviceISThermostat)
            {

            }
            else
            {

            }
        }
        static string Process_Response(string request, object device)
        {
            HaDevice d = null;
            Thermostat t = null;
            if (device is Thermostat)
            {
                t = (Thermostat)device;

            }
            if (device is HaDevice)
            {
                d = (HaDevice)device;
            }
            Command command = new Command(request, client);
            switch (command.Request)
            {
                case CommandActions.Unknown:
                    client.GetDeviceState(device);
                    break;
                case CommandActions.Setvalue:
                    if (device is Thermostat)
                    {
                        return client.SetTemperature(t, command.Value);
                    }
                    if (device is HaDevice)
                    {
                        return client.Dim(d.deviceId.ToString(), command.Value);
                    }
                    break;
                case CommandActions.TurnOn:
                    return client.TurnOn(d.deviceId.ToString());
                    break;
                case CommandActions.TurnOff:
                    return client.TurnOff(d.deviceId.ToString());
                    break;
                case CommandActions.SetTemperature:
                    return client.SetTemperature(t, command.Value);
                    break;
                case CommandActions.SetEcon:
                    return client.SetEconMode(t.deviceId.ToString());
                    break;
                case CommandActions.SetComfort:
                    return client.SetComfortMode(t.deviceId.ToString());
                    break;
                case CommandActions.GetStatus:
                    return client.GetDeviceState(device);
                    break;
                case CommandActions.Dim:
                    return client.Dim(d.deviceId.ToString(), command.Value);
                    break;
                case CommandActions.GetProperties:
                    client.GetObjectProperties((d == null) ? t : d);
                    break;
            }
            return "Nothing was done!";
        }
        static void DisplayDeviceState()
        {
            Console.Clear();
            client.GetDevices();
            WriteLine('*');
            Console.BackgroundColor = ConsoleColor.Blue;
            Console.ForegroundColor = ConsoleColor.White;
            //Animate("STATUS");
            Console.WriteLine("                                     STATUS                                         ");
            Console.ResetColor();
            WriteLine('*');
            foreach (HaDevice d in client.Devices)
            {
                string dstate = client.GetDeviceState(d);
                if (dstate.Contains("is on")) { Console.ForegroundColor = ConsoleColor.Yellow; }
                Animate(dstate);
                Console.ResetColor();
            }
            WriteLine('-');
            foreach (Thermostat t in client.Thermostats)
            {
                string tstate = client.GetThermostatState(t);
                if (tstate.Contains("ing")) { Console.ForegroundColor = ConsoleColor.Red; }
                Animate(tstate);
                Console.ResetColor();
            }
            WriteLine('-');
        }
        static void Animate(string text)
        {
            int top = Console.CursorTop;
            int left = Console.CursorLeft;
            int maxleft = Console.WindowWidth - text.Length;
            String newText = "";
            if (maxleft - left < 1)
            {
                Console.Write(" " + text);
                return;
            }
            for (int position = left; position < maxleft - 2; position++)
            {
                Thread.Sleep(10);
                for (Console.CursorLeft = 0; Console.CursorLeft < position; )
                {
                    Console.Write(" ");
                }
                Console.CursorLeft = position;
                Console.Write(text);
            }
            int cp = Console.CursorLeft;
            newText = "";
            for (int p = cp - text.Length; p > 0; p--)
            {
                Thread.Sleep(10);
                Console.CursorLeft = p;
                newText = newText + " ";
                Console.Write(text + newText);
            }
            Console.CursorTop = top;
            Console.CursorLeft = left;
            Console.WriteLine();
        }
        static object GetDevice(string response)
        {
            HaDevice d = null;
            Thermostat t = null;
            foreach (HaDevice dvc in client.Devices)
            {
                if (response.ToLower().Contains(dvc.name.ToLower()))
                {
                    deviceISThermostat = false;
                    return dvc;
                }
            }
            foreach (Thermostat tstat in client.Thermostats)
            {
                if (response.ToLower().Contains(tstat.name.ToLower()))
                {
                    deviceISThermostat = true;
                    return tstat;
                }
            }
            foreach (string token in response.Split(' '))
            {
                if (!string.IsNullOrEmpty(token) && token.Length > 5)
                {
                    d = client.Devices.Find(o => (o.name.ToLower().StartsWith(token.ToLower())));
                    if (d != null)
                    {
                        deviceISThermostat = false; ;
                        return d;
                    }
                    t = client.Thermostats.Find(o => (o.name.ToLower().StartsWith(token.ToLower())));
                    if (t != null)
                    {
                        deviceISThermostat = true;
                        return t;
                    }
                }
            }
            Console.WriteLine("I could not identify a device\n");
            do
            {
                WriteLine('#');
                Console.WriteLine("                           DEVICE LIST");
                WriteLine('#');
                foreach (HaDevice dvc in client.Devices)
                {
                    if (dvc.deviceType != DeviceType.LevelDisplayer)
                        Console.WriteLine(String.Format("{0:00} : {1}", dvc.nodeId, dvc.name));
                }
                foreach (Thermostat tstat in client.Thermostats)
                {
                    Console.WriteLine(String.Format("{0:00} : {1}", tstat.nodeId, tstat.name));
                }
                Console.Write("\nPlease enter the device number: ");
                String nodeID = Console.ReadLine();
                t = client.Thermostats.Find(o => (o.nodeId.ToString() == nodeID));
                d = client.Devices.Find(p => (p.nodeId.ToString() == nodeID));
            } while ((t == null) & (d == null));
            if (t == null)
            {
                deviceISThermostat = false; ;
                return d;
            }
            deviceISThermostat = true;
            return t;
        }
        public static void OverwriteConsoleMessage(string message)
        {
            Console.CursorLeft = 0;
            int maxCharacterWidth = Console.WindowWidth - 1;
            if (message.Length > maxCharacterWidth)
            {
                message = message.Substring(0, maxCharacterWidth - 3) + "...";
            }
            message = message + new string(' ', maxCharacterWidth - message.Length);
            Console.Write(message);
        }
        public static void RenderConsoleProgress(int percentage)
        {
            RenderConsoleProgress(percentage, '\u2590', Console.ForegroundColor, "");
        }
        public static void RenderConsoleProgress(int percentage, char progressBarCharacter,
                  ConsoleColor color, string message)
        {
            Console.CursorVisible = false;
            ConsoleColor originalColor = Console.ForegroundColor;
            Console.ForegroundColor = color;
            Console.CursorLeft = 0;
            int width = Console.WindowWidth - 1;
            int newWidth = (int)((width * percentage) / 100d);
            string progBar = new string(progressBarCharacter, newWidth) +
                  new string(' ', width - newWidth);
            Console.Write(progBar);
            if (string.IsNullOrEmpty(message)) message = "";
            //Console.CursorTop++;
            Console.Write(Environment.NewLine);
            OverwriteConsoleMessage(message);
            Console.CursorTop--;
            Console.ForegroundColor = originalColor;
            Console.CursorVisible = true;
        }
        public static void WriteLine(char c)
        {
            do
            {

                Console.Write(c);
            }
            while (Console.CursorLeft < Console.WindowWidth - 1);
            Console.WriteLine();
        }
    }
}
