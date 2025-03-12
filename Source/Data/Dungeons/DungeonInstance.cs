﻿
using Source.Models;
using Source.Triggers.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using WCSharp.Api;
using WCSharp.Events;
using WCSharp.Shared.Data;
using WCSharp.Shared.Extensions;
using static WCSharp.Api.Common;
namespace Source.Data.Dungeons
{
    public abstract class DungeonInstance : TriggerInstance
    {
        private List<destructable> _walls = new();

        protected DungeonData Data { get; set; }

        private int ID_BLOCK_WALL_STAGE_1 => FourCC("Dofw");
        private int ID_BLOCK_WALL_STAGE_2 => FourCC("Dofv");

        protected abstract IEnumerable<Rectangle> GetRegionsGuards();
        protected abstract IEnumerable<Rectangle> GetRegionsMiniBosses();
        protected abstract Rectangle GetEnterRegion();

        protected virtual void SetupGates()
        {
#if DEBUG
            Console.WriteLine($"dungeon setup gates: {Data.Stages.Count}");
#endif
        }
        protected abstract string GetDungeonName();
        protected abstract Rectangle GetRegionFinallBoss();

        public DungeonData GetDungeonData()
        {
            if (Data is null)
            {
                Data = new();

                SetupGuards();
                SetupMiniBosses();
                SetupFinalBoss();
                SetupGates();
                SetupEnterRegion();
            }

            

            return Data;

        }

        private void SetupEnterRegion()
        {
            Data.EnterRegion = GetEnterRegion();
        }

        private void SetupFinalBoss()
        {
            var region = GetRegionFinallBoss();
            group group = group.Create();
            GroupEnumUnitsInRect(group, region.Rect, null);

            foreach (var unit in group.ToList())
            {
                if (!string.IsNullOrEmpty(unit.HeroName))
                {
                    Data.FinalBoss = unit;
                    break;
                }
            }

            DestroyGroup(group);
            PlayerUnitEvents.Register(UnitEvent.Dies, RestoreDungeon, Data.FinalBoss);
#if DEBUG
            Console.WriteLine($"dungeon setup final boss: {Data.FinalBoss.Name}");
#endif


        }

        private void RestoreDungeon()
        {
            trigger triggerRestartDungeon = trigger.Create();
            triggerRestartDungeon.AddAction(() =>
            {
                
                var heroes = PlayerHeroesList.Heroes.Where(x => x.Alive);

                foreach (var hero in heroes)
                {
                    hero.Life = hero.MaxLife;
                    hero.Mana = hero.MaxMana;
                    var regionTown = Regions.HeroSpawn.GetRandomPoint();
                    hero.X = regionTown.X;
                    hero.Y = regionTown.Y;
                }

                
                RestartDungeon();
                DestroyTrigger(triggerRestartDungeon);
            });

            triggerRestartDungeon.Execute();
        }

        private void RestartDungeon()
        {
            timer timerRestart = timer.Create();
            timerdialog timerdialog = CreateTimerDialog(timerRestart);
            TimerDialogDisplay(timerdialog, true);
            timerdialog.SetTitle(GetDungeonName());

            timerRestart.Start(MapConfig.DelayRespawnDungeon, false, () =>
            {
                RestoreGuards();
                RestoreBosses();
                RestoreFinalBoss();
                TimerDialogDisplay(timerdialog, false);
                DestroyTimerDialog(timerdialog);
                DestroyTimer(timerRestart);
            });
        }

        private void SetupMiniBosses()
        {
            var regionsBosses = GetRegionsMiniBosses();

            foreach (var region in regionsBosses)
            {
                BossData bossData = new();
                group group = group.Create();
                GroupEnumUnitsInRect(group, region.Rect, null);
                List<DungeonGuardData> guards = new();
                bool bossIsSelected = false;
                foreach (var unit in group.ToList())
                {
                    if (!string.IsNullOrEmpty(unit.HeroName) && !bossIsSelected)
                    {
                        bossIsSelected = true;
                        bossData.Boss = unit;
                    }

                    else
                    {
                        DungeonGuardData guard = new(unit);
                        guards.Add(guard);
                    }
                }

                bossData.Guards = guards;
                Data.Bosses.Add(region, bossData);

            }

#if DEBUG
            Console.WriteLine($"dungeon setup mini bosses: {GetDungeonName()}");
#endif
        }

        private void SetupGuards()
        {
            var _regionsGuards = GetRegionsGuards();

            List<group> _groupsGuards = new();

            foreach (var region in _regionsGuards)
            {
                group newGroup = group.Create();
                GroupEnumUnitsInRect(newGroup, region.Rect, null);
                _groupsGuards.Add(newGroup);
            }

            List<DungeonGuardData> guards = new List<DungeonGuardData>();
            DungeonGuardData guardData = new DungeonGuardData();
            foreach (var group in _groupsGuards.ToList())
            {
                guards = new();
                foreach (var unit in group.ToList())
                {
                    guardData = new(unit);
                    guards.Add(guardData);


                }
                Data.Guards.Add(group, guards);

            }

#if DEBUG
            Console.WriteLine($"dungeon setup guards: {GetDungeonName()}");
#endif
        }

        private void RestoreGuards()
        {
            foreach (var guardsData in Data.Guards)
            {
                var group = guardsData.Key;
                var guards = guardsData.Value.ToList();
                group.Clear();
                for (int i = 0; i < guards.Count; i++)
                {
                    int id = guards[i].IDGuard;
                    float face = guards[i].Face;
                    float x = guards[i].X;
                    float y = guards[i].Y;
                    var newUnit = unit.Create(MapConfig.DungeonPlayer, id, x, y, face);
                    newUnit.DefaultAcquireRange = MapConfig.DefaultAcquireRange;
                    group.Add(newUnit);
                }


            }
        }

        private void RestoreBosses()
        {
            foreach (var boss in Data.Bosses)
            {
                var region = boss.Key;
                var bossData = boss.Value;

                foreach (var guardData in bossData.Guards)
                {
                    int id = guardData.IDGuard;
                    float face = guardData.Face;
                    float x = guardData.X;
                    float y = guardData.Y;
                    var newUnit = unit.Create(MapConfig.DungeonPlayer, id, x, y, face);
                    newUnit.DefaultAcquireRange = MapConfig.DefaultAcquireRange;



                }

                var bossUnit = bossData.Boss;
                var newBoss = unit.Create(MapConfig.DungeonPlayer, bossUnit.UnitType, bossUnit.X, bossUnit.Y, bossUnit.Facing);
                newBoss.HeroLevel = bossData.Boss.HeroLevel;
                RemoveUnit(bossUnit);
                bossData.Boss = newBoss;
            }

        }

        private void RestoreFinalBoss()
        {

            var regionBoss = GetRegionFinallBoss();
            unit finalBoss = unit.Create(MapConfig.DungeonPlayer, Data.FinalBoss.UnitType, regionBoss.Center.X, regionBoss.Center.Y, Data.FinalBoss.Facing);
            finalBoss.HeroLevel = Data.FinalBoss.HeroLevel;
            RemoveUnit(Data.FinalBoss);
            Data.FinalBoss = finalBoss;
        }

        public override trigger GetTrigger()
        {
            trigger trigger = trigger.Create();
            trigger.AddAction(() =>
            {
                GetDungeonData();
            });
            return trigger;
        }
        protected void ListenStage(group group, Rectangle stage)
        {
            foreach (var unit in group.ToList())
            {
                PlayerUnitEvents.Register(UnitEvent.Dies, CheckGroupGateStatus, unit);
            }
            Data.Stages.Add(group, stage);

#if DEBUG
            Console.WriteLine("LISTEN GATE");
#endif
        }

        private void CheckGroupGateStatus()
        {
            bool isRemoving = false;
            group targetGroup = null;
            Rectangle targetStage = null;
            var unit = GetTriggerUnit();
            PlayerUnitEvents.Unregister(UnitEvent.Dies, CheckGroupGateStatus, unit);
            foreach (var gateData in Data.Stages)
            {
                group group = gateData.Key;
                var gate = gateData.Value;

                if (group.Contains(unit))
                {
                    if (group.ToList().All(x => !x.Alive))
                    {
#if DEBUG
                        isRemoving = true;
                        targetGroup = group;
                        targetStage = gate;
#endif
                        break;
                    }
                }
            }

            if (isRemoving)
            {
                EnumDestructablesInRect(targetStage.Rect, null, () =>
                {
                    if (GetEnumDestructable().Type == ID_BLOCK_WALL_STAGE_1 || GetEnumDestructable().Type == ID_BLOCK_WALL_STAGE_2)
                    {
                        GetEnumDestructable().Kill();
                        _walls.Add(GetEnumDestructable());
                    }
                });
            }
        }

    }

}
