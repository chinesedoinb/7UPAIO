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
    internal class Xerath
    {
        public static Menu Menu, ComboMenu, HarassMenu, LaneClearMenu, JungleClearMenu, Misc, KillStealMenu, Ulti, Semi, draw;

        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        private static int WallCastT;
        private static Vector2 YasuoWallCastedPos;
        private static GameObject YasuoWall = null;
		public static void OnGameLoad()
        {
            if (!Player.CharacterName.Contains("Xerath")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 750f);
            Q.SetSkillshot(0.55f, 95f, float.MaxValue, false, SpellType.Line);
            Q.SetCharged("XerathArcanopulseChargeUp", "XerathArcanopulseChargeUp", 750, 1550, 1.5f);

            W = new Spell(SpellSlot.W, 950f);
            W.SetSkillshot(0.65f, 125f, float.MaxValue, false, SpellType.Circle);

            E = new Spell(SpellSlot.E, 1050f);
            E.SetSkillshot(0.25f, 60f, 1400f, true, SpellType.Line);

            R = new Spell(SpellSlot.R, 4990f);
            R.SetSkillshot(0.70f, 125f, float.MaxValue, false, SpellType.Circle);


            var MenuRyze = new Menu("Xerath", "[7UP]Xerath", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("ComboQ", "Use Q"));
            ComboMenu.Add(new MenuBool("ComboW", "Use W"));
            ComboMenu.Add(new MenuBool("ComboE", "Use E"));
            MenuRyze.Add(ComboMenu);
            HarassMenu = new Menu("Harass Settings", "Harass");
            HarassMenu.Add(new MenuBool("HarassQ", "Use Q"));
            HarassMenu.Add(new MenuBool("HarassW", "Use W"));
            HarassMenu.Add(new MenuBool("HarassE", "Use E"));
            HarassMenu.Add(new MenuSlider("Mana", "Min Mana Harass", 50));
            MenuRyze.Add(HarassMenu);
            LaneClearMenu = new Menu("LaneClear Settings", "LaneClear");
            LaneClearMenu.Add(new MenuBool("LaneQ", "Use Q"));
            LaneClearMenu.Add(new MenuSlider("MinQ", "Hit Minions LaneClear", 3, 1, 6));
            LaneClearMenu.Add(new MenuBool("LaneW", "Use W"));
            LaneClearMenu.Add(new MenuSlider("MinW", "Hit Minions LaneClear", 3, 1, 6));
            LaneClearMenu.Add(new MenuSlider("ManaLC", "Min Mana LaneClear", 60));
            MenuRyze.Add(LaneClearMenu);
            JungleClearMenu = new Menu("JungleClear Settings", "JungleClear");
            JungleClearMenu.Add(new MenuBool("QJungle", "Use Q JungleClear"));
            JungleClearMenu.Add(new MenuBool("WJungle", "Use W JungleClear"));
            JungleClearMenu.Add(new MenuBool("EJungle", "Use E JungleClear"));
            JungleClearMenu.Add(new MenuSlider("ManaLC", "Min Mana JungleClear [Q]", 40));
            MenuRyze.Add(JungleClearMenu);
            Misc = new Menu("Misc Settings", "Misc");
            Misc.Add(new MenuBool("qslowcast", "Slow Q Cast(high hitchance)"));
            Misc.Add(new MenuBool("rslowcast", "Slow R Cast(high hitchance)"));
            Misc.Add(new MenuBool("eantigapcloser", "Use E AntiGapcloser"));
            Misc.Add(new MenuBool("einterrupt", "Use E Interrupt Spell"));
            MenuRyze.Add(Misc);
            KillStealMenu = new Menu("KillSteal Settings", "KillSteal");
            KillStealMenu.Add(new MenuBool("KsQ", "Use Q KillSteal"));
            KillStealMenu.Add(new MenuBool("KsW", "Use W KillSteal"));
            KillStealMenu.Add(new MenuBool("KsE", "Use E KillSteal"));
            MenuRyze.Add(KillStealMenu);
            Ulti = new Menu("RSetting", "R Settings");
            Ulti.Add(new MenuKeyBind("RKey", "R Key", Keys.T, KeyBindType.Press));
            Ulti.Add(new MenuBool("NearMouse", "Near Mouse"));
            Ulti.Add(new MenuSlider("MouseZone", "Mouse Zone", 600, 0, 1200));
            MenuRyze.Add(Ulti);
            Semi = new Menu("Semi", "Semi Key");
            Semi.Add(new MenuKeyBind("WKey", "Semi W Key", Keys.W, KeyBindType.Press));
            Semi.Add(new MenuKeyBind("EKey", "Semi E Key", Keys.E, KeyBindType.Press));
            MenuRyze.Add(Semi);
            draw = new Menu("draw", "Drawings");
            draw.Add(new MenuBool("drawQ", "Draw Q"));
            draw.Add(new MenuBool("drawW", "Draw W"));
            draw.Add(new MenuBool("drawE", "Draw E"));
            draw.Add(new MenuBool("drawR", "Draw R"));
            draw.Add(new MenuBool("RMouse", "Draw R Mouse"));
            MenuRyze.Add(draw);
            MenuRyze.Attach();
            //Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            //Orbwalker.OnBeforeAttack += Orbwalker_OnBeforeAttack;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterrupterSpell += OnInterrupterSpell;
            AIBaseClient.OnProcessSpellCast += Obj_AI_Base_OnProcessSpellCast;
        }
        private static HitChance QHitchance => Misc["qslowcast"].GetValue<MenuBool>().Enabled ? HitChance.VeryHigh : HitChance.High;

        private static HitChance RHitchance => Misc["rslowcast"].GetValue<MenuBool>().Enabled ? HitChance.VeryHigh : HitChance.High;

        /*private static void Drawing_OnDraw(EventArgs args)
        {
            if (draw["drawQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, Color.Orange, 1);
            }
            if (draw["drawW"].GetValue<MenuBool>().Enabled && W.IsReady())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, Color.Orange, 1);
            }
            if (draw["drawE"].GetValue<MenuBool>().Enabled && E.IsReady())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, Color.Orange, 1);
            }
            if (draw["drawR"].GetValue<MenuBool>().Enabled && R.IsReady())
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, Color.Orange, 1);
            }
            if (draw["RMouse"].GetValue<MenuBool>().Enabled && Ulti["NearMouse"].GetValue<MenuBool>().Enabled && Ulti["MouseZone"].GetValue<MenuSlider>().Value > 0 && R.IsReady())
            {
                Render.Circle.DrawCircle(Game.CursorPos, Ulti["MouseZone"].GetValue<MenuSlider>().Value, Color.White, 1);
            }
        }*/
        private static void OnInterrupterSpell(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling())
            {
                return;
            }

            if (MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
            {
                return;
            }


            if (Misc["EAntiGapcloser"].GetValue<MenuBool>().Enabled && E.IsReady() && args.DangerLevel >= Interrupter.DangerLevel.Medium && sender.DistanceToPlayer() < E.Range)
            {
                var pred = E.GetPrediction(sender);
                if (pred.Hitchance >= HitChance.High)
                {
                    E.Cast(pred.CastPosition);
                }
            }
        }

        private static void Obj_AI_Base_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            //Last cast time of spells
            if (sender.IsMe)
            {
                if (args.SData.Name.ToString() == "SyndraQ")
                    Q.LastCastAttemptTime = Environment.TickCount;
                if (args.SData.Name.ToString() == "SyndraW" || args.SData.Name.ToString() == "syndrawcast")
                    W.LastCastAttemptTime = Environment.TickCount;
                if (args.SData.Name.ToString() == "SyndraE" || args.SData.Name.ToString() == "syndrae5")
                    E.LastCastAttemptTime = Environment.TickCount;
            }

            //Harass when enemy do attack
            //if (HarassMenu["HarassQ"].GetValue<MenuBool>().Enabled && sender.Type == Player.Type && sender.Team != Player.Team && args.SData.Name.ToLower().Contains("attack") && Player.Distance(sender) <= Math.Pow(Q.Range, 2) && Player.Mana / Player.MaxMana * 100 > HarassMenu["Mana"].GetValue<MenuSlider>().Value)
            //{
                //Q.Cast((AIHeroClient)sender);
            //}
            if (sender.IsValid && sender.Team == ObjectManager.Player.Team && args.SData.Name == "YasuoWMovingWall")
            {
                WallCastT = Environment.TickCount;
                YasuoWallCastedPos = sender.Position.ToVector2();
            }
        }


        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling())
            {
                return;
            }

            if (MenuGUI.IsChatOpen || MenuGUI.IsShopOpen)
            {
                return;
            }


            if (Misc["EAntiGapcloser"].GetValue<MenuBool>().Enabled && E.IsReady() && args.EndPosition.DistanceToPlayer() < 250)
            {
                var pred = E.GetPrediction(sender);
                if (pred.Hitchance >= HitChance.High)
                {
                    E.Cast(pred.CastPosition);
                }
            }
        }

        //public static void Orbwalker_OnBeforeAttack(object sender, BeforeAttackEventArgs args)
        //{
            //bool orbwalkAA = false;
            //if (orbwalkAA = !Q.IsReady() && (!W.IsReady() || !E.IsReady())) ;
            //{
                //args.Process = orbwalkAA;
            //}
        //}

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
                    LaneClear();
                    JungleClear();
                    break;
            }
            KillSteal();
            AutoR();
            SemiAutomatic();
        }
        private static void Combo()
        {
            if (!Q.IsCharging)
            {
                // ult check
                if (!ObjectManager.Player.HasBuff("XerathLocusOfPower2"))
                {
                    if (ComboMenu["ComboW"].GetValue<MenuBool>().Enabled && W.IsReady())
                    {
                        var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                        if (target != null && target.IsValidTarget(W.Range))
                        {
                            var pred = W.GetPrediction(target);
                            if (pred.Hitchance >= HitChance.VeryHigh)
                            {
                                W.Cast(pred.CastPosition);
                            }
                        }
                    }

                    if (ComboMenu["ComboE"].GetValue<MenuBool>().Enabled && E.IsReady())
                    {
                        var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                        if (target != null && target.IsValidTarget(E.Range))
                        {
                            var pred = E.GetPrediction(target);
                            if (pred.Hitchance >= HitChance.VeryHigh)
                            {
                                E.Cast(pred.CastPosition);
                            }
                        }
                    }

                    if (ComboMenu["ComboQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
                    {
                        var target = TargetSelector.GetTarget(Q.ChargedMaxRange, DamageType.Magical);
                        if (target != null && target.IsValidTarget(Q.ChargedMaxRange))
                        {
                            // slow buff = more hitchance || target too far and cant be w hit
                            if (!W.IsReady() || target.DistanceToPlayer() > 850)
                            {
                                var pred = Q.GetPrediction(target);
                                if (pred.Hitchance >= HitChance.High)
                                {
                                    Q.StartCharging();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                if (ComboMenu["ComboQ"].GetValue<MenuBool>().Enabled && Q.IsReady() && Q.IsCharging)
                {
                    var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        var pred = Q.GetPrediction(target);
                        if (pred.Hitchance >= QHitchance)
                        {
                            Q.ShootChargedSpell(pred.CastPosition);
                        }
                    }
                }
            }
        }

        private static void AutoR()
        {
            if (!ObjectManager.Player.HasBuff("XerathLocusOfPower2") || Q.IsCharging)
            {
                return;
            }
            var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);

            if (Ulti["NearMouse"].GetValue<MenuBool>().Enabled && Ulti["MouseZone"].GetValue<MenuSlider>().Value > 0)
            {
                target = TargetSelector.GetTargets(R.Range, DamageType.Magical).FirstOrDefault(x =>
                    x.Position.Distance(Game.CursorPos) <= Ulti["MouseZone"].GetValue<MenuSlider>().Value);
            }
            if (target != null && target.IsValidTarget(R.Range))
            {
                if (Ulti["RKey"].GetValue<MenuKeyBind>().Active)
                {
                    var pred = R.GetPrediction(target);
                    if (pred.Hitchance >= RHitchance)
                    {
                        R.Cast(pred.CastPosition);
                    }
                }
            }

        }
        private static void SemiAutomatic()
        {
            if (Semi["WKey"].GetValue<MenuKeyBind>().Active && W.IsReady())
            {
                var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                if (target != null && target.IsValidTarget(W.Range))
                {
                    W.Cast(target);
                }
            }

            if (Semi["EKey"].GetValue<MenuKeyBind>().Active && E.IsReady())
            {
                var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                if (target != null && target.IsValidTarget(E.Range))
                {
                    E.Cast(target);
                }
            }
        }

        public static double QDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                    (float)(new[] { 0, 70, 110, 150, 190, 230 }[Q.Level] + 0.848f * Player.FlatMagicDamageMod));

        }

        public static double WDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                (float)(new[] { 0, 60, 95, 130, 165, 200 }[W.Level] + 0.58f * Player.FlatMagicDamageMod));
        }

        public static double EDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                (float)(new[] { 0, 80, 110, 140, 170, 200 }[R.Level] + 0.448f * Player.FlatMagicDamageMod));
        }

        public static void KillSteal()
        {
            if (Player.HasBuff("XerathLocusOfPower2") || Q.IsCharging)
            {
                return;
            }
            var KsQ = KillStealMenu["KsQ"].GetValue<MenuBool>().Enabled;
            var KsW = KillStealMenu["KsW"].GetValue<MenuBool>().Enabled;
            var KsE = KillStealMenu["KsE"].GetValue<MenuBool>().Enabled;
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
                                var target1 = TargetSelector.GetTarget(Q.ChargedMaxRange, DamageType.Magical);
                                if (target1 != null && target1.IsValidTarget(Q.ChargedMaxRange))
                                {
                                    // slow buff = more hitchance || target too far and cant be w hit
                                    if (!W.IsReady() || target1.DistanceToPlayer() > 850)
                                    {
                                        var pred = Q.GetPrediction(target1);
                                        if (pred.Hitchance >= HitChance.High)
                                        {
                                            Q.StartCharging();
                                        }
                                    }
                                }
                                else
                                {
                                    var target2 = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                                    if (target2 != null && target2.IsValidTarget(Q.Range))
                                    {
                                        var pred = Q.GetPrediction(target2);
                                        if (pred.Hitchance >= QHitchance)
                                        {
                                            Q.ShootChargedSpell(pred.CastPosition);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                if (KsW && W.IsReady())
                {
                    if (target != null)
                    {
                        if (target.Health + target.AllShield <= WDamage(target))/*try*/
                        {
                            W.Cast(target);
                        }
                    }
                }
                if (KsE && E.IsReady() && target.IsValidTarget(500))
                {
                    if (target != null)
                    {
                        if (target.Health + target.AllShield <= EDamage(target))
                        {
                            E.Cast();
                        }
                    }
                }
            }
        }

        private static void Harass()
        {
            if (!Q.IsCharging)
            {
                // ult + mana check
                if (!ObjectManager.Player.HasBuff("XerathLocusOfPower2") &&
                    ObjectManager.Player.ManaPercent >= HarassMenu["Mana"].GetValue<MenuSlider>().Value)
                {
                    if (HarassMenu["HarassW"].GetValue<MenuBool>().Enabled && W.IsReady())
                    {
                        var target = TargetSelector.GetTarget(W.Range, DamageType.Magical);
                        if (target != null && target.IsValidTarget(W.Range))
                        {
                            var pred = W.GetPrediction(target);
                            if (pred.Hitchance >= HitChance.VeryHigh)
                            {
                                W.Cast(pred.CastPosition);
                                return;
                            }
                        }
                    }

                    if (HarassMenu["HarassE"].GetValue<MenuBool>().Enabled && E.IsReady())
                    {
                        var target = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                        if (target != null && target.IsValidTarget(E.Range))
                        {
                            var pred = E.GetPrediction(target);
                            if (pred.Hitchance >= HitChance.VeryHigh)
                            {
                                E.Cast(pred.CastPosition);
                                return;
                            }
                        }
                    }

                    if (HarassMenu["HarassQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
                    {
                        var target = TargetSelector.GetTarget(Q.ChargedMaxRange, DamageType.Magical);
                        if (target != null && target.IsValidTarget(Q.ChargedMaxRange))
                        {
                            // slow buff = more hitchance || target too far and cant be w hit
                            if (!W.IsReady() || target.DistanceToPlayer() > 850)
                            {
                                var pred = Q.GetPrediction(target);
                                if (pred.Hitchance >= HitChance.High)
                                {
                                    Q.StartCharging();
                                }
                            }
                        }
                    }
                }
            }
            else
            {
                // ignore mana check when q charge
                if (HarassMenu["HarassQ"].GetValue<MenuBool>().Enabled && Q.IsReady() && Q.IsCharging)
                {
                    var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                    if (target != null && target.IsValidTarget(Q.Range))
                    {
                        var pred = Q.GetPrediction(target);
                        if (pred.Hitchance >= QHitchance)
                        {
                            Q.ShootChargedSpell(pred.CastPosition);
                        }
                    }
                }
            }
        }
        private static void LaneClear()
        {
            if (!Q.IsCharging)
            {
                // ult + mana check
                if (!ObjectManager.Player.HasBuff("XerathLocusOfPower2") &&
                    ObjectManager.Player.ManaPercent >= LaneClearMenu["ManaLC"].GetValue<MenuSlider>().Value)
                {
                    if (LaneClearMenu["LaneW"].GetValue<MenuBool>().Enabled && W.IsReady())
                    {
                        var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(W.Range) && x.IsMinion())
                            .Cast<AIBaseClient>().ToList();
                        if (minions.Any())
                        {
                            var wFarmLocation = W.GetCircularFarmLocation(minions);
                            if (wFarmLocation.Position.IsValid() &&
                                wFarmLocation.MinionsHit >= LaneClearMenu["MinW"].GetValue<MenuSlider>().Value)
                            {
                                W.Cast(wFarmLocation.Position);
                                return;
                            }
                        }
                    }

                    if (LaneClearMenu["LaneQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
                    {
                        var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.ChargedMaxRange) && x.IsMinion())
                            .Cast<AIBaseClient>().ToList();
                        if (minions.Any())
                        {
                            var qFarmLocation = Q.GetLineFarmLocation(minions);
                            if (qFarmLocation.Position.IsValid() &&
                                qFarmLocation.MinionsHit >= LaneClearMenu["MinQ"].GetValue<MenuSlider>().Value)
                            {
                                Q.StartCharging();
                            }
                        }
                    }
                }
            }
            else
            {
                // ignore mana check when q charge
                if (LaneClearMenu["LaneQ"].GetValue<MenuBool>().Enabled && Q.IsReady() && Q.IsCharging)
                {
                    var minions = GameObjects.EnemyMinions.Where(x => x.IsValidTarget(Q.Range) && x.IsMinion())
                        .Cast<AIBaseClient>().ToList();
                    if (minions.Any())
                    {
                        var qFarmLocation = Q.GetLineFarmLocation(minions);
                        if (qFarmLocation.Position.IsValid() &&
                            qFarmLocation.MinionsHit >= LaneClearMenu["MinQ"].GetValue<MenuSlider>().Value)
                        {
                            Q.ShootChargedSpell(qFarmLocation.Position.ToVector3());
                        }
                    }
                }
            }
        }

        private static void JungleClear()
        {
            if (!Q.IsCharging)
            {
                // ult + mana check
                if (!ObjectManager.Player.HasBuff("XerathLocusOfPower2") &&
                    ObjectManager.Player.ManaPercent >= JungleClearMenu["ManaJG"].GetValue<MenuSlider>().Value)
                {
                    if (JungleClearMenu["WJungle"].GetValue<MenuBool>().Enabled && W.IsReady())
                    {
                        var mob = GameObjects.Jungle
                            .Where(x => x.IsValidTarget(W.Range) && x.GetJungleType() != JungleType.Unknown)
                            .OrderByDescending(x => x.MaxHealth).FirstOrDefault();

                        if (mob != null && mob.IsValidTarget(W.Range))
                        {
                            var pred = W.GetPrediction(mob);
                            if (pred.Hitchance >= HitChance.High)
                            {
                                W.Cast(pred.CastPosition);
                                return;
                            }
                        }
                    }

                    if (JungleClearMenu["EJungle"].GetValue<MenuBool>().Enabled && E.IsReady())
                    {
                        var mob = GameObjects.Jungle
                            .Where(x => x.IsValidTarget(E.Range) && x.GetJungleType() != JungleType.Unknown)
                            .OrderByDescending(x => x.MaxHealth).FirstOrDefault();

                        if (mob != null && mob.IsValidTarget(E.Range))
                        {
                            var pred = E.GetPrediction(mob);
                            if (pred.Hitchance >= HitChance.High)
                            {
                                E.Cast(pred.CastPosition);
                                return;
                            }
                        }
                    }

                    if (JungleClearMenu["QJungle"].GetValue<MenuBool>().Enabled && Q.IsReady())
                    {
                        var mob = GameObjects.Jungle
                            .Where(x => x.IsValidTarget(Q.ChargedMaxRange) && x.GetJungleType() != JungleType.Unknown)
                            .OrderByDescending(x => x.MaxHealth).FirstOrDefault();

                        if (mob != null && mob.IsValidTarget(Q.ChargedMaxRange))
                        {
                            Q.StartCharging();
                        }
                    }
                }
            }
            else
            {
                // ignore mana check when q charge
                if (JungleClearMenu["QJungle"].GetValue<MenuBool>().Enabled && Q.IsReady() && Q.IsCharging)
                {
                    var mob = GameObjects.Jungle
                        .Where(x => x.IsValidTarget(Q.ChargedMaxRange) && x.GetJungleType() != JungleType.Unknown)
                        .OrderByDescending(x => x.MaxHealth).FirstOrDefault();

                    if (mob != null && mob.IsValidTarget(Q.Range))
                    {
                        var pred = Q.GetPrediction(mob);
                        if (pred.Hitchance >= HitChance.High)
                        {
                            Q.ShootChargedSpell(pred.CastPosition);
                        }
                    }
                }
            }
        }

    }
}
