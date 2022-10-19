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
    internal class Jayce
    {
        public static Menu Menu, ComboMenu, HarassMenu, LaneClearMenu, JungleClearMenu, Misc, KillStealMenu, Items;
        public static bool Hammer, Cannon;
        public static int Stage = 0;
        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }
        public static Spell CannonQ, CannonQExt, CannonW, CannonE, R, HammerQ, HammerW, HammerE;
        public static float RangeQ, RangeQExt, RangeW, RangeE, RangeR;
        private static AIHeroClient castedQon = null;

        public static Spell Ignite;
        public static void OnGameLoad()
        {
            if (!Player.CharacterName.Contains("Jayce")) return;
            Bootstrap.Init(null);
            CannonQ = new Spell(SpellSlot.Q, 1050);
            CannonQExt = new Spell(SpellSlot.Q, 1650);
            HammerQ = new Spell(SpellSlot.Q, 600);
            CannonW = new Spell(SpellSlot.W);
            HammerW = new Spell(SpellSlot.W, 350);
            CannonE = new Spell(SpellSlot.E, 650);
            HammerE = new Spell(SpellSlot.E, 240);

            R = new Spell(SpellSlot.R);

            CannonQ.SetSkillshot(0.25f, 79, 1200, true, SpellType.Line);
            CannonQExt.SetSkillshot(0.35f, 98, 1600, true, SpellType.Line);
            HammerQ.SetTargetted(0.25f, float.MaxValue);
            CannonE.SetSkillshot(0.1f, 120, float.MaxValue, false, SpellType.Circle);
            HammerE.SetTargetted(.25f, float.MaxValue);

            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);

            var MenuRyze = new Menu("Jayce", "[7UP]Jayce", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("q.cannon", "Cannon (Q)").SetValue(true));
            ComboMenu.Add(new MenuBool("w.cannon", "Cannon (W)").SetValue(true));
            ComboMenu.Add(new MenuBool("e.cannon", "Cannon (E)").SetValue(true));
            ComboMenu.Add(new MenuBool("combo.switch", "Auto Switch").SetValue(true));
            ComboMenu.Add(new MenuBool("q.hammer", "Hammer (Q)").SetValue(true));
            ComboMenu.Add(new MenuBool("w.hammer", "Hammer (W)").SetValue(true));
            ComboMenu.Add(new MenuBool("e.hammer", "Hammer (E)").SetValue(true));
            ComboMenu.Add(new MenuSlider("eAway", "Gate distance from side", 60, 3, 60));
            MenuRyze.Add(ComboMenu);
            HarassMenu = new Menu("Harass Settings", "Harass");
            HarassMenu.Add(new MenuBool("q.cannon.harass", "Cannon (Q)").SetValue(true));
            HarassMenu.Add(new MenuBool("e.cannon.harass", "Cannon (E)").SetValue(true));
            HarassMenu.Add(new MenuSlider("harass.mana", "Min. Mana Percent", 20, 0, 100));
            MenuRyze.Add(HarassMenu);
            /*LaneClearMenu = new Menu("LaneClear Settings", "LaneClear");
            LaneClearMenu.Add(new MenuBool("q.cannon.clear", "Cannon (Q)").SetValue(true));
            LaneClearMenu.Add(new MenuBool("e.cannon.clear", "Cannon (E)").SetValue(true));
            LaneClearMenu.Add(new MenuBool("clear.switch", "Auto Switch").SetValue(true));
            LaneClearMenu.Add(new MenuSlider("clear.minion.count", "Min. Minion Count", 3, 1, 5));
            LaneClearMenu.Add(new MenuSlider("clear.mana", "Min. Mana Percent", 20, 0, 100));
            MenuRyze.Add(LaneClearMenu);*/
            JungleClearMenu = new Menu("JungleClear Settings", "JungleClear");
            JungleClearMenu.Add(new MenuBool("q.cannon.jungle", "Cannon (Q)").SetValue(true));
            JungleClearMenu.Add(new MenuBool("w.cannon.jungle", "Cannon (W)").SetValue(true));
            JungleClearMenu.Add(new MenuBool("e.cannon.jungle", "Cannon (E)").SetValue(true));
            JungleClearMenu.Add(new MenuBool("jungle.switch", "Auto Switch").SetValue(true));
            JungleClearMenu.Add(new MenuBool("q.hammer.jungle", "Hammer (Q)").SetValue(true));
            JungleClearMenu.Add(new MenuBool("w.hammer.jungle", "Hammer (W)").SetValue(true));
            JungleClearMenu.Add(new MenuBool("e.hammer.jungle", "Hammer (E)").SetValue(true));
            MenuRyze.Add(JungleClearMenu);
            Misc = new Menu("Misc Settings", "Misc");
            Misc.Add(new MenuBool("interrupt.hammer.e", "Interrupter (Hammer E)"));
            Misc.Add(new MenuBool("gapcloser.hammer.e", "Gapcloser (Hammer E)"));
            Misc.Add(new MenuBool("extra", "packets"));
            MenuRyze.Add(Misc);
            KillStealMenu = new Menu("KillSteal Settings", "KillSteal");
            KillStealMenu.Add(new MenuBool("ign", "Use [Ignite] KillSteal"));
            MenuRyze.Add(KillStealMenu);
            MenuRyze.Attach();

            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;
        }

        private static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Hammer)
            {
                if (Misc["interrupt.hammer.e"].GetValue<MenuBool>().Enabled)
                {
                    if (sender.IsValidTarget(1000))
                    {
                        //Render.Circle.DrawCircle(sender.Position, sender.BoundingRadius, Color.Gold, 5);
                        var targetpos = Drawing.WorldToScreen(sender.Position);
                        Drawing.DrawText(targetpos[0] - 40, targetpos[1] + 20, Color.Gold, "Interrupt");
                    }
                    if (HammerE.CanCast(sender))
                    {
                        HammerE.Cast(sender);
                    }
                }
            }
        }
        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs e)
        {
            if (Hammer)
            {
                if (Misc["gapcloser.hammer.e"].GetValue<MenuBool>().Enabled)
                {
                    if (sender.IsValidTarget(1000))
                    {
                        //Render.Circle.DrawCircle(sender.Position, sender.BoundingRadius, Color.Gold, 5);
                        var targetpos = Drawing.WorldToScreen(sender.Position);
                        Drawing.DrawText(targetpos[0] - 40, targetpos[1] + 20, Color.Gold, "Gapcloser");
                    }
                    if (HammerE.CanCast(sender))
                    {
                        HammerE.Cast(sender);
                    }
                }
            }
        }
        private static float getBestRange()
        {
            float range = 0;
            if (!Hammer)
            {
                if (CannonQ.IsReady() && CannonE.IsReady())
                {
                    range = 1750;
                }
                else if (CannonQ.IsReady())
                {
                    range = 1150;
                }
                else
                {
                    range = 500;
                }
            }
            else
            {
                if (CannonQ.IsReady())
                {
                    range = 600;
                }
                else
                {
                    range = 300;
                }
            }
            return range + 50;
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
                    break;
                case OrbwalkerMode.LaneClear:
                    //LaneClear();
                    JungleClear();
                    break;
            }
            KillSteal();
        }

        public static void Combo()
        {
            var target = TargetSelector.GetTarget(getBestRange(), DamageType.Physical);
            if (Cannon || !Player.IsMelee)
            {

                if (ComboMenu["q.cannon"].GetValue<MenuBool>().Enabled && ComboMenu["e.cannon"].GetValue<MenuBool>().Enabled && CannonQ.IsReady() && CannonE.IsReady())
                {
                    castQEPred(target);
                }

                if (ComboMenu["q.cannon"].GetValue<MenuBool>().Enabled && CannonQ.IsReady() && !CannonE.IsReady())
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(CannonQ.Range)))
                    {
                        if (CannonQ.GetPrediction(enemy).Hitchance >= HitChance.VeryHigh)
                        {
                            CannonQ.Cast(enemy);
                        }
                    }
                }

                if (ComboMenu["w.cannon"].GetValue<MenuBool>().Enabled && CannonW.IsReady())
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(600)))
                    {
                        CannonW.Cast();
                    }
                }

                if (ComboMenu["combo.switch"].GetValue<MenuBool>().Enabled && !CannonQ.IsReady() && !CannonE.IsReady() && !CannonW.IsReady())
                {
                    R.Cast();
                }
            }
            if (Hammer || Player.IsMelee)
            {
                if (ComboMenu["q.hammer"].GetValue<MenuBool>().Enabled && HammerQ.IsReady())
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(HammerQ.Range)))
                    {
                        HammerQ.Cast(enemy);
                    }
                }
                if (ComboMenu["w.hammer"].GetValue<MenuBool>().Enabled && HammerW.IsReady())
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(600)))
                    {
                        HammerW.Cast();
                    }
                }
                if (ComboMenu["e.hammer"].GetValue<MenuBool>().Enabled && HammerE.IsReady())
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(HammerE.Range)))
                    {
                        HammerE.Cast(enemy);
                    }
                }
                if (ComboMenu["combo.switch"].GetValue<MenuBool>().Enabled && !HammerQ.IsReady() && !HammerW.IsReady() && !HammerE.IsReady())
                {
                    R.Cast();
                }
            }

        }


        public static void Harass()
        {
            var target = TargetSelector.GetTarget(getBestRange(), DamageType.Physical);

            if (Hammer || Player.IsMelee && Player.ManaPercent < HarassMenu["harass.mana"].GetValue<MenuSlider>().Value)
            {
                return;
            }


            if (Cannon || !Player.IsMelee)
            {
                if (HarassMenu["q.cannon.harass"].GetValue<MenuBool>().Enabled && HarassMenu["e.cannon.harass"].GetValue<MenuBool>().Enabled && CannonQ.IsReady() &&
                    CannonE.IsReady())
                {
                    castQEPred(target);
                }
                if (HarassMenu["q.cannon.harass"].GetValue<MenuBool>().Enabled && CannonQ.IsReady() && !CannonE.IsReady())
                {
                    foreach (var enemy in GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(CannonQ.Range)))
                    {
                        if (CannonQ.GetPrediction(enemy).Hitchance >= HitChance.VeryHigh)
                        {
                            CannonQ.Cast(enemy);
                        }
                    }
                }
            }

        }

        public static void JungleClear()
        {
            if (Hammer || Player.IsMelee)
            {
                var mob = GameObjects.GetMinions(Player.ServerPosition, 700);
                if (mob == null || (mob.Count == 0))
                {
                    return;
                }
                if (HammerQ.CanCast(Player) && JungleClearMenu["q.hammer.jungle"].GetValue<MenuBool>().Enabled)
                {
                    HammerQ.CastOnUnit(Player);
                }
                if (Player.Distance(Player.Position) < 600 && JungleClearMenu["w.hammer.jungle"].GetValue<MenuBool>().Enabled)
                {
                    HammerW.Cast();
                }
                if (HammerE.CanCast(Player) && JungleClearMenu["e.hammer.jungle"].GetValue<MenuBool>().Enabled)
                {
                    HammerE.CastOnUnit(Player);
                }
                if (!HammerQ.IsReady() && !HammerW.IsReady() && !HammerE.IsReady() && JungleClearMenu["jungle.switch"].GetValue<MenuBool>().Enabled)
                {
                    R.Cast();
                }
            }
            if (Cannon || !Player.IsMelee)
            {
                var mob = GameObjects.GetMinions(Player.ServerPosition, 700);
                if (mob == null || (mob.Count == 0))
                {
                    return;
                }
                if (CannonQ.IsReady() && CannonE.IsReady() && JungleClearMenu["q.cannon.jungle"].GetValue<MenuBool>().Enabled && JungleClearMenu["e.cannon.jungle"].GetValue<MenuBool>().Enabled)
                {
                    JungleExt();
                }
                if (CannonQ.IsReady() && !CannonE.IsReady() && CannonQ.CanCast(Player) && JungleClearMenu["q.cannon.jungle"].GetValue<MenuBool>().Enabled)
                {
                    CannonQ.CastOnUnit(Player);
                }
                if (Player.Distance(Player.Position) < 600 && JungleClearMenu["w.cannon.jungle"].GetValue<MenuBool>().Enabled)
                {
                    CannonW.Cast();
                }
                if (!CannonQ.IsReady() && !CannonW.IsReady() && !CannonE.IsReady() && JungleClearMenu["jungle.switch"].GetValue<MenuBool>().Enabled)
                {
                    R.Cast();
                }
            }
        }

        public static void JungleExt()
        {
            var mob = GameObjects.GetMinions(Player.ServerPosition, 700); 
            var pos2 = Player.Position.Extend(new Vector3(Player.Position.X, Player.Position.Y, Player.Position.Z), CannonE.Range);
            if (pos2.Distance(Player.Position) < CannonQ.Range && Player.Distance(Player.Position) > CannonE.Range)
            {
                CannonE.Cast(pos2);

                if (CannonQExt.GetPrediction(Player).Hitchance >= HitChance.VeryHigh)
                {
                    CannonQExt.Cast(pos2);
                }
            }
            if (pos2.Distance(Player.Position) < CannonQ.Range && Player.Distance(Player.Position) < CannonE.Range)
            {
                var YUZ = Player.Position.Extend(new Vector3(Player.Position.X, Player.Position.Y + 200, Player.Position.Z), 200);
                CannonE.Cast(YUZ);
                if (CannonQExt.GetPrediction(Player).Hitchance >= HitChance.VeryHigh)
                {
                    CannonQExt.Cast(pos2);
                }
            }
        }


        private static void castQEPred(AIHeroClient target)
        {
            if (Hammer || target == null)
                return;
            PredictionOutput po = CannonQExt.GetPrediction(target);
            var dist = Player.Distance(po.UnitPosition);
            if (po.Hitchance >= HitChance.Medium && dist < (CannonQExt.Range + target.BoundingRadius))
            {
                // if()
                //doExploit(target);
                // else
                // {
                if (shootQE(po.CastPosition, dist > 550))
                    castedQon = target;
                // }
            }
            else if (po.Hitchance == HitChance.Collision)
            {
                AIBaseClient fistCol = po.CollisionObjects.OrderBy(unit => unit.Distance(Player.Position)).First();
                if (fistCol.Distance(po.UnitPosition) < (180 - fistCol.BoundingRadius / 2) && fistCol.Distance(target.Position) < (180 - fistCol.BoundingRadius / 2))
                {
                    shootQE(po.CastPosition);
                }
            }
        }

        private static bool shootQE(Vector3 pos, bool man = false)
        {
            try
            {
                if (Hammer && R.IsReady())
                    R.Cast();
                if (!CannonE.IsReady() || !CannonQ.IsReady() || Hammer)
                    return false;
                if(Misc["extra"].GetValue<MenuBool>().Enabled)
                {
                    packetCastQ(pos.ToVector2());
                    packetCastE(getParalelVec(pos));
                }
                else
                {
                    Vector3 bPos = Player.Position - Vector3.Normalize(pos - Player.Position) * 50;

                    Player.IssueOrder(GameObjectOrder.MoveTo, bPos);
                    CannonQ.Cast(pos);
                    if (man)
                        CannonE.Cast(getParalelVec(pos));
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return true;
        }

        private static Vector2 getParalelVec(Vector3 pos)
        {
            if (ComboMenu["e.cannon"].GetValue<MenuBool>().Enabled)
            {
                Random rnd = new Random();
                int neg = rnd.Next(0, 1);
                int away = ComboMenu["eAway"].GetValue<MenuSlider>().Value;
                away = (neg == 1) ? away : -away;
                var v2 = Vector3.Normalize(pos - Player.Position) * away;
                var bom = new Vector2(v2.Y, -v2.X);
                return Player.Position.ToVector2() + bom;
            }
            else
            {
                var dpos = Player.Distance(pos);
                var v2 = Vector3.Normalize(pos - Player.Position) * ((dpos < 300) ? dpos + 10 : 300);
                var bom = new Vector2(v2.X, v2.Y);
                return Player.Position.ToVector2() + bom;
            }
        }

        private static void packetCastQ(Vector2 pos)
        {

            CannonQ.Cast(pos);
            //Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, SpellSlot.Q, Player.NetworkId, pos.X, pos.Y, Player.Position.X, Player.Position.Y)).Send();
        }

        private static void packetCastE(Vector2 pos)
        {

            CannonE.Cast(pos);
            //Packet.C2S.Cast.Encoded(new Packet.C2S.Cast.Struct(0, SpellSlot.E, Player.NetworkId, pos.X, pos.Y, Player.Position.X, Player.Position.Y)).Send();
        }
        public bool Range { get { return CannonQ.Instance.Name.ToLower() == "jayceshockblast"; } }




        /*public static double QDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                    (float)(new[] { 0, 80, 120, 140, 160, 180 }[Q.Level] + 1.1f * Player.FlatPhysicalDamageMod));

        }

        public static double WDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                (float)(new[] { 0, 60, 90, 120, 150, 180 }[W.Level] + 0.6f * Player.FlatPhysicalDamageMod));
        }

        public static double RDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                (float)(new[] { 0, 80, 120, 160 }[R.Level] + 0.8f * Player.FlatPhysicalDamageMod));
        }*/

        public static void KillSteal()
        {
            foreach (var target in GameObjects.EnemyHeroes.Where(hero => hero.IsValidTarget(HammerQ.Range) && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))
                /*if (Player.HasBuff("TalonEHop"))
                {

                }
                var KsQ = KillStealMenu["KsQ"].GetValue<MenuBool>().Enabled;
                var KsW = KillStealMenu["KsW"].GetValue<MenuBool>().Enabled;
                var KsR = KillStealMenu["KsR"].GetValue<MenuBool>().Enabled;
                foreach (var target in GameObjects.EnemyHeroes.Where(hero => hero.IsValidTarget(W.Range) && !hero.HasBuff("JudicatorIntervention") && !hero.HasBuff("kindredrnodeathbuff") && !hero.HasBuff("Undying Rage")))
                {
                    if (KsQ && Q.IsReady())
                    {
                        if (target != null)
                        {
                            if (Player.Distance(target) > 150)
                            {
                                if (target.Health + target.AllShield <= QDamage(target))
                                {
                                    Q.Cast(target);
                                }
                            }
                            else
                            {
                                if (target.Health + target.AllShield <= QDamage(target) * 1.5f)
                                {
                                    Q.Cast(target);
                                }
                            }
                        }
                    }
                    if (KsW && W.IsReady())
                    {
                        if (target != null)
                        {
                            if (target.Health + target.AllShield <= WDamage(target))/*try*/
                /*{
                    W.Cast(target);
                }
            }
        }
        if (KsR && R.IsReady() && target.IsValidTarget(500))
        {
            if (target != null)
            {
                if (target.Health + target.AllShield <= RDamage(target))
                {
                    R.Cast();
                }
            }
        }*/
                if (Ignite != null && KillStealMenu["ign"].GetValue<MenuBool>().Enabled && Ignite.IsReady())
                {
                    if (target.Health + target.AllShield < Player.GetSummonerSpellDamage(target, SummonerSpell.Ignite))
                    {
                        Ignite.Cast(target);
                    }
                }
            }
        }
    }

