using Mirror;
using UnityEngine;

namespace GunGame.Components
{
    public class TimedObject : Component
    {
        public void StartCountdown(float delayS = 45)
        {
            MEC.Timing.CallDelayed(delayS, () =>
            {
                NetworkServer.UnSpawn(gameObject);
            });
        }
    }
}