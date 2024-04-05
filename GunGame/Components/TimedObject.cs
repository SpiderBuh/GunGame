using Mirror;
using PluginAPI.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;

namespace GunGame.Components
{
    public class TimedObject : MonoBehaviour
    {
        public TimedObject(float time, GameObject obj)
        {
            //Cassie.Message("A");
            MEC.Timing.CallDelayed(time, () =>
            {
                //Cassie.Message("B");
                NetworkServer.UnSpawn(obj);
            });
        }
    }
}