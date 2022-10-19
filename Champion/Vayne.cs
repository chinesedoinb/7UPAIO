using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;
using SharpDX;
using Color = System.Drawing.Color;
using static EnsoulSharp.SDK.Items;
using SharpDX.Direct3D9;

namespace AIO7UP.Champions
{
    internal class Vayne
    {
        public static Menu Menu, ComboMenu, HarrassMenu, Laneclear, JungleClear, Misc, QSetting, ESetting, RSetting;

        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }
        public static Spell Q, W, E, R;
        public static Spell Ignite;

        public static void OnGameLoad()
        {
            if (!Player.CharacterName.Contains("Vayne")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 300f);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 550f);
            R = new Spell(SpellSlot.R);

            E.SetTargetted(0.25f, 2200f);

            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);

            var MenuRyze = new Menu("Vayne", "[7UP]Vayne", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("ComboQ", "Use Q"));
            ComboMenu.Add(new MenuBool("AQALogic", "Use AA-Q-AA Logic"));
            ComboMenu.Add(new MenuBool("ComboE", "Use E"));
            ComboMenu.Add(new MenuBool("ComboR", "Use R"));
            ComboMenu.Add(new MenuSlider("ComboRCount", "Enemies to use R", 3, 1, 5));
            ComboMenu.Add(new MenuSlider("ComboRHp", "Use R|Or Player HealthPercent <= x%", 45));
            MenuRyze.Add(ComboMenu);
            /*HarrassMenu = new Menu("Harass Setting", "Harass");
            HarrassMenu.Add(new MenuBool("HarassQ", "Use Q"));
            HarrassMenu.Add(new MenuBool("HarassQ2Passive", "Use Q|Only Target have 2 Passive"));
            HarrassMenu.Add(new MenuBool("HarassE", "Use E|Only Target have 2 Passive", false));
            HarrassMenu.Add(new MenuSlider("HarassMana", "When Player ManaPercent >= x%", 60));
            MenuRyze.Add(HarrassMenu);*/
            Laneclear = new Menu("Laneclear Setting", "LaneClear");
            Laneclear.Add(new MenuBool("LaneClearQ", "Use Q", false));
            Laneclear.Add(new MenuBool("LaneClearQTurret", "Use Q|Attack Tower"));
            Laneclear.Add(new MenuSlider("LaneClearMana", "When Player ManaPercent >= %", 60));
            MenuRyze.Add(Laneclear);
            JungleClear = new Menu("Jungle Settings", "JungleClear");
            JungleClear.Add(new MenuBool("JungleClearQ", "Use Q"));
            JungleClear.Add(new MenuBool("JungleClearE", "Use E"));
            JungleClear.Add(new MenuSlider("JungleClearMana", "When Player ManaPercent >= x%", 40));
            MenuRyze.Add(JungleClear);
            Misc = new Menu("Misc Settings", "Misc");
            //Misc.Add(new MenuBool("Forcus", "Force 2 Passive Target"));
            Misc.Add(new MenuBool("Interrupt", "Interrupt Danger Spells"));
            Misc.Add(new MenuBool("AntiAlistar", "Interrupt Alistar W"));
            Misc.Add(new MenuBool("AntiRengar", "Interrupt Rengar Jump"));
            Misc.Add(new MenuBool("AntiKhazix", "Interrupt Khazix R"));
			Misc.Add(new MenuBool("AntiJax", "Interrupt Jax Q"));
			Misc.Add(new MenuBool("AntiPan", "Interrupt Pan W"));
            Misc.Add(new MenuSeparator("Anti Gap", "Anti Gapcloser"));
            Misc.Add(new MenuBool("Gapcloser", "Anti Gapcloser"));
            foreach (var target in GameObjects.EnemyHeroes)
            {
                Misc.Add(new MenuBool("AntiGapcloser" + target.CharacterName.ToLower(), target.CharacterName));

            }
            MenuRyze.Add(Misc);
            QSetting = new Menu("QSetting", " Q Setting");
            QSetting.Add(new MenuBool("QCheck", "Use Q|Safe Check?"));
            QSetting.Add(new MenuBool("QTurret", "Use Q|Dont Cast To Turret"));
            QSetting.Add(new MenuBool("QMelee", "Use Q|Anti Melee"));
            MenuRyze.Add(QSetting);
            ESetting = new Menu("ESetting", " E Setting");
            ESetting.Add(new MenuList("EMode", "Use E Mode:", new[] { "Default", "VHR", "Marksman", "SharpShooter", "OKTW" })).AddPermashow();
            ESetting.Add(new MenuSlider("ComboEPush", "Use E|Push Tolerance", 0, -50, 50));
            ESetting.Add(new MenuBool("AutoE", "Auto E?"));
            foreach (var target in GameObjects.EnemyHeroes)
            {
                ESetting.Add(new MenuBool("AutoE" + target.CharacterName.ToLower(), target.CharacterName));

            }
            MenuRyze.Add(ESetting);
            RSetting = new Menu("RSetting", " R Setting");
            RSetting.Add(new MenuBool("visibleR", "Unvisable block AA"));
            RSetting.Add(new MenuSlider("AutoRCount", "Auto R|When Enemies Counts >= x", 3, 1, 5));
            RSetting.Add(new MenuSlider("AutoRRange", "Auto R|Search Enemies Range", 600, 500, 1200));
            MenuRyze.Add(RSetting);
            MenuRyze.Attach();


            Game.OnUpdate += Game_OnUpdate;
            Orbwalker.OnBeforeAttack += BeforeAttack;
            Orbwalker.OnAfterAttack += AfterAttack;
            AIBaseClient.OnProcessSpellCast += OnProcessSpellCast;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterrupterSpell += OnInterruptableTarget;
            GameObject.OnCreate += OnCreateObject;

        }



        private static void OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs Args)
        {
            if (sender == null || !sender.IsEnemy || !sender.IsMelee || sender.Type != Player.Type || Args.Target == null)
            {
                return;
            }

            if (Args.Target.IsMe)
            {
                if (QSetting["QMelee"].GetValue<MenuBool>().Enabled && Q.IsReady())
                {
                    Q.Cast(Player.Position.Extend(sender.Position, -Q.Range));
                }
            }
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs e)
        {
            if (E.IsReady())
            {
                if (Misc["AntiAlistar"].GetValue<MenuBool>().Enabled && sender.CharacterName == "Alistar" && e.Type == AntiGapcloser.GapcloserType.Targeted)
                {
                    E.CastOnUnit(sender);
                }
				
				if (Misc["AntiJax"].GetValue<MenuBool>().Enabled && sender.CharacterName == "Jax" && e.Type == AntiGapcloser.GapcloserType.Targeted)
                {
                    E.CastOnUnit(sender);
                }
				if (Misc["AntiPan"].GetValue<MenuBool>().Enabled && sender.CharacterName == "Pantheon" && e.Type == AntiGapcloser.GapcloserType.Targeted)
                {
                    E.CastOnUnit(sender);
                }

                if (Misc["Gapcloser"].GetValue<MenuBool>().Enabled && Misc["AntiGapcloser" + sender.CharacterName.ToLower()].GetValue<MenuBool>().Enabled)
                {
                    if (sender.DistanceToPlayer() <= 200 && sender.IsValid)
                    {
                        E.CastOnUnit(sender);
                    }
                }
            }
            if (e.SpellName == "ZedR")
                return;
            if (e.EndPosition.DistanceToPlayer() < e.StartPosition.DistanceToPlayer())
            {
                if (e.EndPosition.DistanceToPlayer() <= 300 && sender.IsValidTarget(550))
                {
                    if (E.Cast(sender) == CastStates.SuccessfullyCasted)
                        return;
                }
                else
                {
                    return;
                }
            }
        }

        private static void BeforeAttack(object sender, BeforeAttackEventArgs args)
        {
            if (RSetting["visibleR"].GetValue<MenuBool>().Enabled && Player.HasBuff("vaynetumblefade") && Player.CountEnemyHeroesInRange(800) > 1)
            {
                args.Process = false;
            }
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo ||
                Orbwalker.ActiveMode == OrbwalkerMode.Harass)
            {
                var ForcusTarget =
                    GameObjects.EnemyHeroes.FirstOrDefault(
                        x =>
                            x.IsValidTarget(Player.GetRealAutoAttackRange()) &&
                            x.HasBuff("VayneSilveredDebuff") && x.GetBuffCount("VayneSilveredDebuff") == 2);

                /*if (Menu.GetBool("Forcus") && ForcusTarget != null)
                {
                    Orbwalker.ForceTarget(ForcusTarget);
                }
                else
                {
                    Orbwalker.ForceTarget(null);
                }*/
            }
        }

        private static void AfterAttack(object sender, AfterAttackEventArgs args)
        {

            if (args.Target == null || !args.Target.IsValidTarget())
            {
                return;
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                if (ComboMenu["AQALogic"].GetValue<MenuBool>().Enabled)
                {
                    var target = args.Target as AIMinionClient;

                    if (target != null && !Player.IsDead && !Player.IsZombie() && Q.IsReady())
                    {
                        QLogic(target);
                    }
                }
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                if (args is AITurretClient)
                {
                    if (Player.ManaPercent <= Laneclear["LaneClearMana"].GetValue<MenuSlider>().Value)
                    {
                        if (Laneclear["LaneClearQ"].GetValue<MenuBool>().Enabled && Laneclear["LaneClearQTurret"].GetValue<MenuBool>().Enabled &&
                            Player.CountEnemyHeroesInRange(900) == 0 && Q.IsReady())
                        {
                            Q.Cast(Game.CursorPos);
                        }
                    }
                }
                else if (args is AIMinionClient)
                {
                    if (Player.ManaPercent <= Laneclear["LaneClearMana"].GetValue<MenuSlider>().Value)
                    {
                        if (JungleClear["JungleClearQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
                        {
                            var mobs =
                                GameObjects.GetMinions(Player.Position, 800, MinionTypes.All, MinionTeam.Ally,
                                    MinionOrderTypes.MaxHealth);

                            if (mobs.Any())
                            {
                                Q.Cast(Game.CursorPos);
                            }
                        }
                    }
                }
            }
        }

        private static void OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs Args)
        {
            if (Misc["Interrupt"].GetValue<MenuBool>().Enabled && E.IsReady() && sender.IsEnemy && sender.IsValidTarget(E.Range))
            {
                if (Args.DangerLevel >= Interrupter.DangerLevel.High)
                {
                    E.CastOnUnit(sender);
                }
            }
        }
        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            var Rengar = GameObjects.EnemyHeroes.Find(heros => heros.CharacterName.Equals("Rengar"));
            var Khazix = GameObjects.EnemyHeroes.Find(heros => heros.CharacterName.Equals("Khazix"));

            if (Rengar != null && Misc["AntiRengar"].GetValue<MenuBool>().Enabled)
            {
                if (sender.Name == "Rengar_LeapSound.troy" && sender.Position.Distance(Player.Position) < E.Range)
                {
                    E.CastOnUnit(Rengar);
                }
            }

            if (Khazix != null && Misc["AntiKhazix"].GetValue<MenuBool>().Enabled)
            {
                if (sender.Name == "Khazix_Base_E_Tar.troy" && sender.Position.Distance(Player.Position) <= 300)
                {
                    E.CastOnUnit(Khazix);
                }
            }
        }

        private static void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || MenuGUI.IsChatOpen || ObjectManager.Player.IsWindingUp)
            {
                return;
            }
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    return;
                case OrbwalkerMode.Harass:
                    Harass();
                    return;
                    case OrbwalkerMode.LaneClear:
                    LaneClear();
                    Jungle();
                    return;
            }
            AutoLogic();
        }


        private static void QLogic(AIBaseClient target)
        {
            if (!Q.IsReady())
            {
                return;
            }

            var qPosition = Player.ServerPosition.Extend(Game.CursorPos, Q.Range);
            var targetDisQ = target.ServerPosition.Distance(qPosition);
            var canQ = false;

            if (QSetting["QTurret"].GetValue<MenuBool>().Enabled && qPosition.IsUnderEnemyTurret())
            {
                canQ = false;
            }

            if (QSetting["QCheck"].GetValue<MenuBool>().Enabled)
            {
                if (qPosition.CountEnemyHeroesInRange(300f) >= 3)
                {
                    Q.Cast(Game.CursorPos);
                    canQ = false;
                }

                //Catilyn W
                if (ObjectManager
                        .Get<AIMinionClient>()
                        .FirstOrDefault(
                            x =>
                                x != null && x.IsValid &&
                                x.Name.ToLower().Contains("yordletrap_idle_red.troy") &&
                                x.Position.Distance(qPosition) <= 100) != null)
                {
                    canQ = false;
                }

                //Jinx E
                if (ObjectManager.Get<AIMinionClient>()
                        .FirstOrDefault(x => x.IsValid && x.IsEnemy && x.Name == "k" &&
                                             x.Position.Distance(qPosition) <= 100) != null)
                {
                    canQ = false;
                }

                //Teemo R
                if (ObjectManager.Get<AIMinionClient>()
                        .FirstOrDefault(x => x.IsValid && x.IsEnemy && x.Name == "Noxious Trap" &&
                                             x.Position.Distance(qPosition) <= 100) != null)
                {
                    canQ = false;
                }
            }

            if (targetDisQ >= Q.Range && targetDisQ <= Q.Range * 2)
            {
                Q.Cast(Game.CursorPos);
                canQ = true;
            }

            if (canQ)
            {
                Q.Cast(Game.CursorPos);
                canQ = false;
            }
        }

        private static void ELogic(AIBaseClient target)
        {
            if (target != null)
            {
                switch (ESetting["EMode"].GetValue<MenuList>().Index)
                {
                    case 0:
                        {
                            var EPred = E.GetPrediction(target);
                            var PD = 425 + ESetting["ComboEPush"].GetValue<MenuSlider>().Value;
                            var PP = EPred.UnitPosition.Extend(Player.Position, -PD);

                            for (int i = 1; i < PD; i += (int)target.BoundingRadius)
                            {
                                var VL = EPred.UnitPosition.Extend(Player.Position, -i);
                                var J4 = ObjectManager.Get<AIBaseClient>()
                                    .Any(f => f.Distance(PP) <= target.BoundingRadius && f.Name.ToLower() == "beacon");
                                var CF = NavMesh.GetCollisionFlags(VL);

                                if (CF.HasFlag(CollisionFlags.Wall) || CF.HasFlag(CollisionFlags.Building) || J4)
                                {
                                    E.CastOnUnit(target);
                                    return;
                                }
                            }
                        }
                        break;
                    case 1:
                        {
                            var pushDistance = 425 + ESetting["ComboEPush"].GetValue<MenuSlider>().Value;
                            var Prediction = E.GetPrediction(target);
                            var endPosition = Prediction.UnitPosition.Extend
                                (Player.ServerPosition, -pushDistance);

                            if (Prediction.Hitchance >= HitChance.VeryHigh)
                            {
                                if (endPosition.IsWall())
                                {
                                    var condemnRectangle = new Geometry.Rectangle(target.ServerPosition.ToVector2(),
                                        endPosition.ToVector2(), target.BoundingRadius);

                                    if (
                                        condemnRectangle.Points.Count(
                                            point =>
                                                NavMesh.GetCollisionFlags(point.X, point.Y)
                                                    .HasFlag(CollisionFlags.Wall)) >=
                                        condemnRectangle.Points.Count * (20 / 100f))
                                    {
                                        E.CastOnUnit(target);
                                    }
                                }
                                else
                                {
                                    var step = pushDistance / 5f;
                                    for (float i = 0; i < pushDistance; i += step)
                                    {
                                        var endPositionEx = Prediction.UnitPosition.Extend(Player.ServerPosition, -i);
                                        if (endPositionEx.IsWall())
                                        {
                                            var condemnRectangle =
                                                new Geometry.Rectangle(target.ServerPosition.ToVector2(),
                                                    endPosition.ToVector2(), target.BoundingRadius);

                                            if (
                                                condemnRectangle.Points.Count(
                                                    point =>
                                                        NavMesh.GetCollisionFlags(point.X, point.Y)
                                                            .HasFlag(CollisionFlags.Wall)) >=
                                                condemnRectangle.Points.Count * (20 / 100f))
                                            {
                                                E.CastOnUnit(target);
                                            }
                                            return;
                                        }
                                    }
                                }
                            }
                        }
                        break;
                    case 2:
                        {
                            for (var i = 1; i < 8; i++)
                            {
                                var targetBehind = target.Position +
                                                   Vector3.Normalize(target.ServerPosition - Player.Position) * i * 50;

                                if (targetBehind.IsWall() && target.IsValidTarget(E.Range))
                                {
                                    E.CastOnUnit(target);
                                    return;
                                }
                            }
                        }
                        break;
                    case 3:
                        {
                            var prediction = E.GetPrediction(target);

                            if (prediction.Hitchance >= HitChance.High)
                            {
                                var finalPosition = prediction.UnitPosition.Extend(Player.Position, -400);

                                if (finalPosition.IsWall())
                                {
                                    E.CastOnUnit(target);
                                    return;
                                }

                                for (var i = 1; i < 400; i += 50)
                                {
                                    var loc3 = prediction.UnitPosition.Extend(Player.Position, -i);

                                    if (loc3.IsWall())
                                    {
                                        E.CastOnUnit(target);
                                        return;
                                    }
                                }
                            }
                        }
                        break;
                    case 4:
                        {
                            var prepos = E.GetPrediction(target);
                            float pushDistance = 470;
                            var radius = 250;
                            var start2 = target.ServerPosition;
                            var end2 = prepos.CastPosition.Extend(Player.ServerPosition, -pushDistance);
                            var start = start2.ToVector2();
                            var end = end2.ToVector2();
                            var dir = (end - start).Normalized();
                            var pDir = dir.Perpendicular();
                            var rightEndPos = end + pDir * radius;
                            var leftEndPos = end - pDir * radius;
                            var rEndPos = new Vector3(rightEndPos.X, rightEndPos.Y, Player.Position.Z);
                            var lEndPos = new Vector3(leftEndPos.X, leftEndPos.Y, Player.Position.Z);
                            var step = start2.Distance(rEndPos) / 10;

                            for (var i = 0; i < 10; i++)
                            {
                                var pr = start2.Extend(rEndPos, step * i);
                                var pl = start2.Extend(lEndPos, step * i);

                                if (pr.IsWall() && pl.IsWall())
                                {
                                    E.CastOnUnit(target);
                                    return;
                                }
                            }
                        }
                        break;
                }
            }
        }

        public static void AutoLogic()
        {
            if (ESetting["AutoE"].GetValue<MenuBool>().Enabled && E.IsReady() && !Player.IsUnderEnemyTurret())
            {
                if (Orbwalker.ActiveMode != OrbwalkerMode.Combo)
                {
                    foreach (
                        var target in
                        GameObjects.EnemyHeroes.Where(
                            x =>
                                x.IsValidTarget(E.Range) && !x.HasBuffOfType(BuffType.SpellShield) &&
                                ESetting["AutoE" + x.CharacterName.ToLower()].GetValue<MenuBool>().Enabled))
                    {
                        if (target != null)
                        {
                            ELogic(target);
                        }
                    }
                }
            }

            if (RSetting["AutoR"].GetValue<MenuBool>().Enabled && R.IsReady() &&
                Player.CountEnemyHeroesInRange(RSetting["AutoRRange"].GetValue<MenuSlider>().Value) >= RSetting["AutoRCount"].GetValue<MenuSlider>().Value)
            {
                R.Cast();
            }
        }

        public static void Combo()
        {
            var target1 = TargetSelector.GetTarget(
    GameObjects.Player.GetRealAutoAttackRange(GameObjects.Player) + 300,
    DamageType.Physical);
            if (Player.Spellbook.IsAutoAttack)
            {
                return;
            }

            if (ComboMenu["ComboR"].GetValue<MenuBool>().Enabled && R.IsReady())
            {
                if (Player.CountEnemyHeroesInRange(800) >= ComboMenu["ComboRCount"].GetValue<MenuSlider>().Value)
                {
                    R.Cast();
                }

                if (Player.CountEnemyHeroesInRange(600) >= 1 && Player.HealthPercent <= ComboMenu["ComboRHp"].GetValue<MenuSlider>().Value)
                {
                    R.Cast();
                }
            }

            if (ComboMenu["ComboE"].GetValue<MenuBool>().Enabled && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

                if (target != null)
                {
                    ELogic(target);
                }
            }

            if (ComboMenu["ComboQ"].GetValue<MenuBool>().Enabled && Q.IsReady() && !CanCondemnStun(target1, default(Vector3), false))
            {
                var target = TargetSelector.GetTarget(800, DamageType.Physical);
                var dashPosition = GameObjects.Player.Position.Extend(Game.CursorPos, Q.Range);
                Q.Cast(dashPosition);
                if (target != null)
                {
                    if (ComboMenu["AQALogic"].GetValue<MenuBool>().Enabled && Player.InAutoAttackRange(target))
                    {
                        QLogic(target); return;
                    }

                    QLogic(target);
                }
            }
        }

        public static void Harass()
        {
            var useQ = HarrassMenu["HarassQ"].GetValue<MenuBool>().Enabled;
            var useE = HarrassMenu["HarassE"].GetValue<MenuBool>().Enabled;

            var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            var target = TargetSelector.GetTarget(E.Range, DamageType.Physical);

            if (!target.IsValidTarget())
            {
                return;
            }

            if (useQ && Q.IsReady() && GetWStacks(t) == 2)
            {
                var dashPosition = GameObjects.Player.Position.Extend(Game.CursorPos, Q.Range);
                Q.Cast(dashPosition);
            }

            if (useE && E.IsReady() && CanCondemnStun(target))
            {
                E.Cast(target);
            }
        }
        public static void LaneClear()
        {
            if (Player.ManaPercent <= Laneclear["LaneClearMana"].GetValue<MenuSlider>().Value)
            {
                if (Laneclear["LaneClearQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
                {
                    var minions =
                        GameObjects.GetMinions(Player.Position, 700)
                            .Where(m => m.Health < Q.GetDamage(m) + Player.GetAutoAttackDamage(m));

                    var minion = minions.FirstOrDefault();

                    if (minion != null)
                    {
                        if (minion.Distance(Player.Position.Extend(Game.CursorPos, Q.Range)) <=
                            Player.GetRealAutoAttackRange())
                        {
                            Q.Cast(Player.Position.Extend(Game.CursorPos, Q.Range));
                            //Orbwalker.ForceTarget(minions.FirstOrDefault());
                        }
                    }
                }
            }
        }
        public static void Jungle()
        {
            if (Player.ManaPercent <= JungleClear["JungleClearMana"].GetValue<MenuSlider>().Value)
            {
                if (JungleClear["JungleClearE"].GetValue<MenuBool>().Enabled && E.IsReady())
                {
                    var mob =
                        GameObjects.GetMinions(Player.Position, E.Range, MinionTypes.All, MinionTeam.Ally,
                                MinionOrderTypes.MaxHealth)
                            .FirstOrDefault(
                                x =>
                                    !x.Name.ToLower().Contains("mini") && !x.Name.ToLower().Contains("baron") &&
                                    !x.Name.ToLower().Contains("dragon") && !x.Name.ToLower().Contains("crab") &&
                                    !x.Name.ToLower().Contains("herald"));

                    if (mob != null && mob.IsValidTarget(E.Range))
                    {
                        E.CastOnUnit(mob);
                    }
                }
            }
        }
        private static int GetWStacks(AIBaseClient target)
        {
            return target.GetBuffCount("VayneSilveredDebuff");
        }

        private static bool CanCondemnStun(AIBaseClient target, Vector3 startPos = default(Vector3), bool casting = true)
        {
            if (startPos == default(Vector3))
            {
                startPos = GameObjects.Player.Position;
            }

            var knockbackPos = startPos.Extend(
                target.Position,
                startPos.Distance(target.Position) + 350);

            var flags = NavMesh.GetCollisionFlags(knockbackPos);
            var collision = flags.HasFlag(CollisionFlags.Building) || flags.HasFlag(CollisionFlags.Wall);


            if (!casting || !QSetting["QCheck"].GetValue<MenuBool>().Enabled)
            {
                return collision;
            }

            if (!QSetting["QCheck"].GetValue<MenuBool>().Enabled)
            {
                return collision;
            }
            return collision;
        }
    }
}

