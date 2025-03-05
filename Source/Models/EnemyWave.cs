﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WCSharp.Api;
using static WCSharp.Api.Common;
namespace Source.Models
{
    public class EnemyWave
    {
        public Queue<string> Units { get; private set; }
        public int Count => Units.Count;

        public EnemyWave (Dictionary<string, int> data)
        {
            Units = new();
            foreach (var item in data)
            {
                for (int i = 0; i < item.Value; i++)
                {
                    Units.Enqueue(item.Key);
                }
            }
        }

        public unit Turn ()
        {
            var region = Regions.regionSpawn;
            string id = Units.Dequeue();
            var newUnit = unit.Create(MapConfig.EnemyPlayerWave, FourCC(id), region.Center.X, region.Center.Y);
            var portal = Portal.GetPortal();
            float x = portal.X;
            float y = portal.Y;
            IssuePointOrder(newUnit, "attack", x, y);
            newUnit.AttackEnabled1 = false;
            newUnit.AttackEnabled2 = false;
            newUnit.AttackBaseDamage1 = 0;
            newUnit.AttackBaseDamage2 = 0;

            return newUnit;
        }
    }
}
