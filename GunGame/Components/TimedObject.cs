using Mirror;
using UnityEngine;

namespace GunGame.Components
{
    public class TimedObject : MonoBehaviour
    {
        public TimedObject(float time, GameObject obj)
        {
            MEC.Timing.CallDelayed(time, () =>
            {
                NetworkServer.UnSpawn(obj);
            });
        }
    }
}