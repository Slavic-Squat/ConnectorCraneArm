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
    partial class Program
    {
        public class ConnectorCraneArm
        {
            private Program program;
            private int ID;
            private CraneArm craneArm;
            private IMyShipController controller;
            private IMyTextSurfaceProvider controllerScreens;
            
            public ConnectorCraneArm(Program program, int ID)
            {
                this.program = program;
                this.ID = ID;

                controller = (IMyShipController)program.GridTerminalSystem.GetBlockWithName($"Connector Arm Controller {ID}");
                controllerScreens = (IMyTextSurfaceProvider)controller;

                craneArm = new CraneArm(program, ID, 10, 10, controller, 0.1f, 2, false);
            }

            public void Run()
            {
                craneArm.Run();
            }

            public void endEffectorControl(string commandString)
            {
                bool command;
                bool.TryParse(commandString, out command);
                craneArm.eeControlled = command;
            }
        }
    }
}
