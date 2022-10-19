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
    internal class Caitlyn
    {
        public static Menu Menu, ComboMenu, HarassMenu, LaneClearMenu, JungleClearMenu, Misc, KillStealMenu, draw;

        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static AIHeroClient blitz = null;

        public static int LastCastWTick;

        public static readonly List<GameObject> trapList = new List<GameObject>();

        public static bool canCastR = true;

        // private static bool headshotReady = ObjectManager.Player.Buffs.Any(buff => buff.DisplayName == "CaitlynHeadshotReady");

        public static string[] dangerousEnemies =
        {
            "Alistar", "Garen", "Zed", "Fizz", "Rengar", "JarvanIV", "Irelia", "Amumu", "DrMundo", "Ryze", "Fiora", "KhaZix", "LeeSin", "Riven", "Lissandra", "Vayne", "Lucian", "Zyra", "Akali", "Aatrox", "Samira", "Vex", "Viego", "Volibear", "Master Yi", "Tryndamere", "Sett", "Kayn"
        };

        public static void OnGameLoad()
        {
            if (!Player.CharacterName.Contains("Caitlyn")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 1240);
            W = new Spell(SpellSlot.W, 820);
            E = new Spell(SpellSlot.E, 900);
            R = new Spell(SpellSlot.R, 2000);

            Q.SetSkillshot(0.50f, 50f, 2000f, false, SpellType.Line);
            E.SetSkillshot(0.25f, 60f, 1600f, true, SpellType.Line);



            var MenuRyze = new Menu("Caitlyn", "[7UP]Caitlyn", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("UseQC", "Combo Q"));
            ComboMenu.Add(new MenuBool("Combo.Q.Use.Urf", "Q: Urf Mode", false));
            ComboMenu.Add(new MenuKeyBind("UseQMC", "Semi Q Key", Keys.G, KeyBindType.Press)).AddPermashow();
            ComboMenu.Add(new MenuBool("UseWC", "Combo W"));
            ComboMenu.Add(new MenuBool("UseEC", "Combo E"));
            ComboMenu.Add(new MenuBool("UseRC", "Combo R"));
            ComboMenu.Add(new MenuKeyBind("UseRMC", "Semi R Key", Keys.T, KeyBindType.Press)).AddPermashow();
            MenuRyze.Add(ComboMenu);
            HarassMenu = new Menu("Harass Settings", "Harass");
            HarassMenu.Add(new MenuBool("UseQH", "Use Q"));
            MenuRyze.Add(HarassMenu);
            LaneClearMenu = new Menu("LaneClear Settings", "LaneClear");
            LaneClearMenu.Add(new MenuBool("LaneQ", "Use Q"));
            LaneClearMenu.Add(new MenuSlider("MinQ", "Hit Minions LaneClear", 4, 1, 6));
            LaneClearMenu.Add(new MenuSlider("ManaLC", "Min Mana LaneClear", 60));
            MenuRyze.Add(LaneClearMenu);
            JungleClearMenu = new Menu("JungleClear Settings", "JungleClear");
            JungleClearMenu.Add(new MenuBool("QJungle", "Use Q JungleClear"));
            JungleClearMenu.Add(new MenuBool("EJungle", "Use E JungleClear"));
            JungleClearMenu.Add(new MenuSlider("MnJungle", "Min Mana JungleClear [Q]", 60));
            MenuRyze.Add(JungleClearMenu);
            Misc = new Menu("Misc Settings", "Misc");
            Misc.Add(new MenuBool("Misc.W.Interrupt", "W Interrupt"));
            Misc.Add(new MenuBool("Misc.AntiGapCloser", "E Anti Gapcloser"));
            Misc.Add(new MenuKeyBind("UseEQC", "Use E-Q Combo", Keys.S, KeyBindType.Press)).AddPermashow();
            Misc.Add(new MenuList("AutoQI", "Auto Q (Stun/Snare/Taunt/Slow)", new[] { "Off", "On: Everytime", "On: Combo Mode" }, 2)).AddPermashow();
            Misc.Add(new MenuList("AutoWI", "Auto W (Stun/Snare/Taunt)", new[] { "Off", "On: Everytime", "On: Combo Mode" }, 2)).AddPermashow();
            if (blitz != null)
            {
                Misc.Add(new MenuBool("AutoWB", "Auto W (Blitz)"));
            }
            MenuRyze.Add(Misc);
            KillStealMenu = new Menu("KillSteal Settings", "KillSteal");
            KillStealMenu.Add(new MenuBool("KsQ", "Use Q KillSteal"));
            MenuRyze.Add(KillStealMenu);
            draw = new Menu("draw", "Drawing");
            draw.Add(new MenuBool("drawQ", "Draw Q"));
            draw.Add(new MenuBool("drawW", "Draw W"));
            draw.Add(new MenuBool("drawE", "Draw E"));
            MenuRyze.Add(draw);
            MenuRyze.Attach();
            //Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            //Orbwalker.OnAfterAttack += Orbwalker_OnAfterAttack;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            AIBaseClient.OnProcessSpellCast += ObjAiBaseOnOnProcessSpellCast;
            Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;
            GameObject.OnDelete += OnDeleteObject;
            GameObject.OnCreate += OnCreateObject;
            foreach (var hero in GameObjects.AllyHeroes)
            {
                if (hero.CharacterName.Equals("Blitzcrank"))
                {
                    blitz = hero;
                }
            }

            AIBaseClient.OnBuffAdd += (sender, args) =>
            {
                // return;

                if (W.IsReady())
                {
                    var aBuff =
                        (from fBuffs in
                            sender.Buffs.Where(
                                s =>
                                    sender.IsEnemy
                                    && sender.Distance(ObjectManager.Player.Position) <= W.Range)
                         from b in new[]
                         {
                                "teleport", /* Teleport */
                                "pantheon_grandskyfall_jump", /* Pantheon */ 
                                "crowstorm", /* FiddleScitck */
                                "zhonya", "katarinar", /* Katarita */
                                "MissFortuneBulletTime", /* MissFortune */
                                "gate", /* Twisted Fate */
                                "chronorevive" /* Zilean */
                            }
                         where args.Buff.Name.ToLower().Contains(b)
                         select fBuffs).FirstOrDefault();

                    //if (aBuff != null && aBuff.StartTime + CommonUtils.GetRandomDelay(250, 1000) <= Game.Time)
                    //if (aBuff != null && aBuff.StartTime + CommonUtils.GetRandomDelay(250, 1000) <= Game.Time)
                    {
                        //LeagueSharp.Common.Utility.DelayAction.Add(CommonUtils.GetRandomDelay(250, 1000), () =>
                        //{
                        //CastW(sender.Position);
                        //});
                        //W.Cast(sender.Position);
                    }
                }
            };
        }
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
        }*/
        public static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Misc["Misc.AntiGapCloser"].GetValue<MenuBool>().Enabled && E.IsReady())
            {
                return;
            }

            if (sender.Distance(ObjectManager.Player.Position) <= 200)
            {
                if (sender.IsValidTarget(E.Range))
                    if (E.IsReady())
                        E.Cast(sender.Position);
                if (sender.IsValidTarget(W.Range))
                    if (W.IsReady())
                        W.Cast(sender);
            }
        }
        public static void ExecuteCombo()
        {
            if (ComboMenu["Combo.Q.Use.Urf"].GetValue<MenuBool>().Enabled)
            {
                var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (t.IsValidTarget() && !t.IsValidTarget(Player.GetRealAutoAttackRange(null) + 65))
                    Q.Cast(t);
            }
            ExecuteCombo();
        }
        private static void ObjAiBaseOnOnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsEnemy && sender is AITurretClient && args.Target.IsMe)
            {
                canCastR = false;
            }
            else
            {
                canCastR = true;
            }
        }
        public static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            if (Misc["Misc.W.Interrupt"].GetValue<MenuBool>().Enabled)
                if (args.DangerLevel >= Interrupter.DangerLevel.Medium)
                    if (sender.IsValidTarget(W.Range))
                        if (W.IsReady())
                            if (!trapList.Any(x => x.IsValid && sender.Position.Distance(x.Position) <= 100))
                                W.Cast(sender.Position);
        }
        private static void OnDeleteObject(GameObject sender, EventArgs args)
        {
            if (sender.IsAlly)
                if (sender.Name == "Cupcake Trap")
                    trapList.RemoveAll(x => x.NetworkId == sender.NetworkId);
        }

        private static void OnCreateObject(GameObject sender, EventArgs args)
        {
            if (sender.IsAlly)
                if (sender.Name == "Cupcake Trap")
                    trapList.Add(sender);
        }
        private static void CastQ(AIBaseClient t)
        {
            if (Q.CanCast(t))
            {
                var qPrediction = Q.GetPrediction(t);
                var hithere = qPrediction.CastPosition.Extend(ObjectManager.Player.Position, -100);

                if (qPrediction.Hitchance >= HitChance.High)
                {
                    Q.Cast(hithere);
                }
            }
        }

        bool CastE()
        {
            if (!E.IsReady())
            {
                return false;
            }

            var t = TargetSelector.GetTarget(E.Range, DamageType.Magical);

            if (t != null)
            {
                var nPrediction = E.GetPrediction(t);
                var nHitPosition = nPrediction.CastPosition.Extend(ObjectManager.Player.Position, -130);
                if (nPrediction.Hitchance >= HitChance.High)
                {
                    E.Cast(nHitPosition);
                }
            }
            if (t != null)
            {
                E.Cast(t);
            }

            return false;
        }

        private static void CastE(AIBaseClient t)
        {
            if (E.CanCast(t))
            {
                var pred = E.GetPrediction(t);
                var hithere = pred.CastPosition.Extend(ObjectManager.Player.Position, -100);

                if (pred.Hitchance >= HitChance.High)
                {
                    E.Cast(hithere);
                }
            }
        }


        private static void CastW(Vector3 pos, bool delayControl = true)
        {
            var enemy =
                GameObjects.EnemyHeroes.Find(
                    e =>
                        e.IsValidTarget(Player.GetRealAutoAttackRange(null) + 65) &&
                        e.Health < ObjectManager.Player.TotalAttackDamage * 2);
            if (enemy != null)
            {
                return;
            }

            if (!W.IsReady())
            {
                return;
            }

            //if (headshotReady)
            //{
            //    return;
            //}

            if (delayControl && LastCastWTick + 2000 > Variables.GameTimeTickCount)
            {
                return;
            }

            if (!trapList.Any(x => x.IsValid && pos.Distance(x.Position) <= 100))
                W.Cast(pos);

            //W.Cast(pos);
            LastCastWTick = Variables.GameTimeTickCount;
        }

        private static void CastW2(AIBaseClient t)
        {
            if (t.IsValidTarget(W.Range))
            {
                BuffType[] buffList =
                {
                    BuffType.Fear,
                    BuffType.Taunt,
                    BuffType.Stun,
                    BuffType.Slow,
                    BuffType.Snare
                };

                foreach (var b in buffList.Where(t.HasBuffOfType))
                {
                    CastW(t.Position, false);
                }
            }
        }
        private static bool EnemyHasBuff(AIHeroClient t)
        {
            BuffType[] buffList =
            {
                BuffType.Knockup,
                BuffType.Taunt,
                BuffType.Stun,
                BuffType.Snare
            };

            return buffList.Where(t.HasBuffOfType).Any();
        }
        private static void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || MenuGUI.IsChatOpen || ObjectManager.Player.IsWindingUp)
            {
                return;
            }

            if (!Player.CanCast)
            {
                return;
            }

            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Harass:
                    DoHarass();
                    break;
                case OrbwalkerMode.Combo:
                    DoCombo();
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
            }
            Auto();
            KillSteal();
            SemiKey();
        }
        public static void DoCombo()
        {

            var useQ = ComboMenu["UseQC"].GetValue<MenuBool>().Enabled;
            var useW = ComboMenu["UseWC"].GetValue<MenuBool>().Enabled;
            var useE = ComboMenu["UseEC"].GetValue<MenuBool>().Enabled;
            var useR = ComboMenu["UseRC"].GetValue<MenuBool>().Enabled;

            if (!Orbwalker.CanMove(40, false))
            {
                return;
            }
            if (useQ && Q.IsReady())
            {
                var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (t != null)
                {
                    CastQ(t);
                }
            }

            if (useE && E.IsReady())
            {
                var enemies = GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(E.Range));
                var objAiHeroes = enemies as AIHeroClient[] ?? enemies.ToArray();
                IEnumerable<AIHeroClient> nResult =
                    (from e in objAiHeroes join d in dangerousEnemies on e.CharacterName equals d select e)
                        .Distinct();

                foreach (var n in nResult.Where(n => n.IsFacing(ObjectManager.Player)))
                {
                    if (n.IsValidTarget(Player.GetRealAutoAttackRange(null) + 65 - 300) && E.GetPrediction(n).CollisionObjects.Count == 0)
                    {
                        E.Cast(n.Position);
                        if (W.IsReady())
                            W.Cast(n.Position);
                    }
                }
                //if (GetValue<bool>("E.ProtectDistance"))
                //{
                //    foreach (var n in HeroManager.Enemies)
                //    {
                //        if (GetValue<bool>("E." + n.ChampionName + ".ProtectDistance") &&
                //            n.Distance(ObjectManager.Player) < E.Range - 100)
                //        {
                //            E.Cast(n.Position);
                //        }

                //    }
                //}
                foreach (
                    var enemy in
                        GameObjects.EnemyHeroes.Where(
                            e =>
                                e.IsValidTarget(E.Range) && e.Health >= ObjectManager.Player.TotalAttackDamage * 2 &&
                                e.IsFacing(ObjectManager.Player) && e.IsValidTarget(E.Range - 300) &&
                                E.GetPrediction(e).CollisionObjects.Count == 0))
                {
                    E.Cast(enemy.Position);
                    var targetBehind = enemy.Position.ToVector2().Extend(ObjectManager.Player.Position.ToVector2(), -140);
                    if (W.IsReady() && ObjectManager.Player.Distance(targetBehind) <= W.Range)
                    {
                        W.Cast(enemy.Position);
                    }
                    if (Q.IsReady())
                    {
                        Q.Cast(enemy.Position);
                    }
                }
            }

            if (useW && W.IsReady())
            {
                var nResult = GameObjects.EnemyHeroes.Where(e => e.IsValidTarget(W.Range));
                foreach (var n in nResult)
                {
                    if (ObjectManager.Player.Distance(n) < 450 && n.IsFacing(ObjectManager.Player))
                    {
                        CastW(ObjectManager.Player.Position);
                    }
                }
            }

            if (R.IsReady() && useR)
            {
                foreach (
                    var e in
                        GameObjects.EnemyHeroes.Where(
                            e =>
                                e.IsValidTarget(R.Range) && e.Health <= R.GetDamage(e) &&
                                ObjectManager.Player.CountEnemyHeroesInRange(Player.GetRealAutoAttackRange(null) + 350) ==
                                0 &&
                                !Player.InAutoAttackRange(e) && canCastR))
                {
                    R.CastOnUnit(e);
                }
            }
        }
        public static void Orbwalking_AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            var t = target as AIHeroClient;
            if (t == null || unit.IsMe) return;

            var useQ = ComboMenu["UseQ"].GetValue<MenuBool>().Enabled;
            if (useQ) Q.Cast(t, false, true);
            var useQH = HarassMenu["UseQH"].GetValue<MenuBool>().Enabled;
            if (useQH) Q.Cast(t, false, true);
            Orbwalking_AfterAttack(unit, target);
        }
        public static void DoHarass()
        {
            if (HarassMenu["UseQH"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (t != null)
                {
                    CastQ(t);
                }
            }
        }
        public static void Auto()
        {
            R.Range = 500 * (R.Level == 0 ? 1 : R.Level) + 1500;

            AIHeroClient t;

            if (Misc["AutoWI"].GetValue<MenuList>().Index != 0 && W.IsReady())
            {
                foreach (
                    var hero in
                        GameObjects.EnemyHeroes.Where(h => h.IsValidTarget(W.Range) && h.HasBuffOfType(BuffType.Stun)))
                {
                    CastW(hero.Position, false);
                }
            }

            if (W.IsReady() &&
                (Misc["AutoWI"].GetValue<MenuList>().Index == 1 ||
                 (Misc["AutoWI"].GetValue<MenuList>().Index == 2)))
            {
                t = TargetSelector.GetTarget(W.Range, DamageType.Physical);
                if (t.IsValidTarget(W.Range))
                {
                    if (t.HasBuffOfType(BuffType.Stun) || t.HasBuffOfType(BuffType.Snare) ||
                        t.HasBuffOfType(BuffType.Taunt) || t.HasBuffOfType(BuffType.Knockup) ||
                        t.HasBuff("zhonyasringshield") || t.HasBuff("Recall"))
                    {
                        CastW(t.Position);
                    }

                    if (t.HasBuffOfType(BuffType.Slow) && t.IsValidTarget(E.Range - 200))
                    {
                        //W.Cast(t.Position.Extend(ObjectManager.Player.Position, +200));
                        //W.Cast(t.Position.Extend(ObjectManager.Player.Position, -200));

                        var hit = t.IsFacing(ObjectManager.Player)
                            ? t.Position.Extend(ObjectManager.Player.Position, +200)
                            : t.Position.Extend(ObjectManager.Player.Position, -200);
                        CastW(hit);
                    }
                }
            }

            if (Q.IsReady() &&
                (Misc["AutoQI"].GetValue<MenuList>().Index == 1 ||
                 (Misc["AutoQI"].GetValue<MenuList>().Index == 2)))
            {
                t = TargetSelector.GetTarget(Q.Range - 30, DamageType.Physical);
                if (t.IsValidTarget(Q.Range)
                    &&
                    (t.HasBuffOfType(BuffType.Stun) ||
                     t.HasBuffOfType(BuffType.Snare) ||
                     t.HasBuffOfType(BuffType.Taunt) ||
                     (t.Health <= ObjectManager.Player.GetSpellDamage(t, SpellSlot.Q)
                      && !t.IsValidTarget(Player.GetRealAutoAttackRange(null) + 65))))
                {
                    CastQ(t);
                }
            }

        }
        public static double QDamage(AIBaseClient target)
        {
            return Player.CalculateDamage(target, DamageType.Physical,
                    (float)(new[] { 0, 50, 90, 130, 170, 210 }[Q.Level] + 1.3f * Player.FlatPhysicalDamageMod));

        }
        public static void KillSteal()
        {
            if (Player.HasBuff("CaitlynHeadShot"))
            {

            }
            var KsQ = KillStealMenu["KsQ"].GetValue<MenuBool>().Enabled;

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
            }
        }
        public static void SemiKey()
        {
            if (ComboMenu["UseQMC"].GetValue<MenuKeyBind>().Active && Q.IsReady())
            {
                var t = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
                if (t.IsValidTarget())
                {
                    CastQ(t);
                }
            }
            if (Misc["UseEQC"].GetValue<MenuKeyBind>().Active && E.IsReady() && Q.IsReady())
            {
                var t = TargetSelector.GetTarget(E.Range, DamageType.Physical);
                if (t.IsValidTarget(E.Range))
                {
                    CastE(t);
                    CastQ(t);
                }

            }
            if (ComboMenu["UseRMC"].GetValue<MenuKeyBind>().Active && R.IsReady())
            {
                var t = TargetSelector.GetTarget(R.Range, DamageType.Physical);
                if (t.IsValidTarget(R.Range))
                {
                    R.Cast(t);
                }
            }
            
        }
        public static void LaneClear()
        {
            var mana = LaneClearMenu["ManaLC"].GetValue<MenuSlider>().Value;
            var useQ = LaneClearMenu["LaneQ"].GetValue<MenuBool>().Enabled;
            var minQ = LaneClearMenu["MinQ"].GetValue<MenuSlider>().Value;
            var minions = GameObjects.EnemyMinions.Where(e => e.IsValidTarget(Q.Range) && e.IsMinion())
                .Cast<AIBaseClient>().ToList();
            var qFarmLocation = Q.GetLineFarmLocation(minions, Q.Width);
            if (qFarmLocation.Position.IsValid())
            
                if (Player.ManaPercent <= mana)
                {
                    return;
                }
            foreach (var minion in minions)
            {
                if (useQ && Q.IsReady() && minion.IsValidTarget(Q.Range) && qFarmLocation.MinionsHit >= minQ)
                {
                    Q.Cast(qFarmLocation.Position);
                }
            }

        }

        public static void JungleClear()
        {
            if (!Orbwalker.CanMove(1, false))
            {
                return;
            }
            var useQ = JungleClearMenu["QJungle"].GetValue<MenuBool>().Enabled;
            var useE = JungleClearMenu["EJungle"].GetValue<MenuBool>().Enabled;
            var mana = JungleClearMenu["MnJungle"].GetValue<MenuSlider>().Value;
            var jungleMonsters = GameObjects.Jungle.Where(j => j.IsValidTarget(Q.Range)).FirstOrDefault(j => j.IsValidTarget(Q.Range));
            if (Player.ManaPercent < mana)
            {
                return;
            }
            if (jungleMonsters != null)
            {
                if (useQ && Q.IsReady() && jungleMonsters.IsValidTarget(Q.Range))
                {
                    Q.Cast(jungleMonsters);
                }

                if (useE && E.IsReady() && jungleMonsters.IsValidTarget(E.Range))
                {
                    E.Cast(jungleMonsters);
                }
            }

        }

    }
}

