using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace InControlCommunicator
{
    public enum CommandActions
    {
        AwayMode,
        SetComfort,
        SetEcon,
        Dim,
        SetTemperature,
        TurnOn,
        TurnOff,
        TurnUp,
        TurnDown,
        GetStatus,
        Setvalue,
        GetProperties,
        Unknown,
    }
    public enum DeviceState
    {
        on,
        off,
        heating,
        cooling,
        running,
        unknown
    }
}

