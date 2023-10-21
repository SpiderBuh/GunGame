using Footprinting;
using Mirror;
using PlayerRoles;
using PlayerRoles.FirstPersonControl;
using PlayerStatsSystem;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;

namespace GunGame.HitRegModules
{
    public class KnifeDamageHandler : AttackerDamageHandler
    {
        public override Footprint Attacker { get; protected set; }
        private string deathReason = "Skill issued";

        public override bool AllowSelfDamage => false;
        private readonly Vector3 MoveDirection;

        public override float Damage { get; protected set; }
        public bool Backstab { get; protected set; }

        public override string ServerLogsText => $"{(Backstab ? "Massively skill issued" : "Skill issued")} by {Attacker.Nickname}";

        public KnifeDamageHandler()
        {
            Attacker = default;
            Damage = 0;
            Backstab = false;
        }
        public KnifeDamageHandler(ReferenceHub attacker, float damage, Vector3 moveDirection, bool backstab = false)
        {
            Attacker = new Footprint(attacker);
            Damage = damage;
            MoveDirection = moveDirection;
            Backstab = backstab;
        }

        public override HandlerOutput ApplyDamage(ReferenceHub ply)
        {
            StartVelocity = ply.GetVelocity();
            StartVelocity.y = Mathf.Max(StartVelocity.y, 0f);
            HealthStat module = ply.playerStats.GetModule<HealthStat>();
            if (Damage <= 0f)
            {
                return HandlerOutput.Nothing;
            }

            if (Backstab)
            {
                Damage *= 8f;
            }
            ProcessDamage(ply);
            module.CurValue -= Damage;
            StartVelocity += (MoveDirection.NormalizeIgnoreY() * 0.1f + Vector3.up * 0.02f) * Damage;
            if (!(module.CurValue <= 0f))
            {
                return HandlerOutput.Damaged;
            }

            return HandlerOutput.Death;
        }

        public override void WriteAdditionalData(NetworkWriter writer)
        {
            base.WriteAdditionalData(writer);
            writer.WriteString(deathReason);
        }

        public override void ReadAdditionalData(NetworkReader reader)
        {
            base.ReadAdditionalData(reader);
            deathReason = reader.ReadString();
        }
    }
}
