using Sandbox.Game.EntityComponents;
using Sandbox.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using SpaceEngineers.Game.ModAPI.Ingame;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text;
using VRage;
using VRage.Collections;
using VRage.Game;
using VRage.Game.Components;
using VRage.Game.GUI.TextPanel;
using VRage.Game.ModAPI.Ingame;
using VRage.Game.ModAPI.Ingame.Utilities;
using VRage.Game.ObjectBuilders.Definitions;
using VRageMath;

namespace IngameScript
{
    partial class Program : MyGridProgram
    {
        MyCommandLine commandLine = new MyCommandLine();
        Dictionary<string, Action<string>> commandDictionary = new Dictionary<string, Action<string>>();
        ConnectorCraneArm connectorArm;
        public Program()
        {
            Runtime.UpdateFrequency = UpdateFrequency.Update1;
            connectorArm = new ConnectorCraneArm(this, 0);
            commandDictionary["EndEffectorControl"] = connectorArm.endEffectorControl;
        }

        public void Save()
        {

        }

        public void Main(string argument, UpdateType updateSource)
        {
            if (commandLine.TryParse(argument))
            {
                string commandName = commandLine.Argument(0);
                string commandArgument = commandLine.Argument(1);
                Action<string> command;

                if (commandName != null && commandArgument != null)
                {
                    if (commandDictionary.TryGetValue(commandName, out command))
                    {
                        command(commandArgument);
                    }
                }
            }

            connectorArm.Run();
        }
    }
}
