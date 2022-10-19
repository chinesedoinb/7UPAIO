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
    internal class Cassiopeia
    {
        public static Menu Menu, ComboMenu, HarassMenu, LaneClearMenu,LastHitMenu, JGClear, Misc, Utilmate, draw;
        public static int Stage = 0;
        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }
        public static Spell Q, W, E, R;
        public static Spell Ignite, Flash;
        private static long dtBurstComboStart;
        private static long dtLastQCast;
        private static long dtLastSaveYourself;
        private static long dtLastECast;


        public static void OnGameLoad()
        {
            if (!Player.CharacterName.Contains("Cassiopeia")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 850);//, SkillShotType.Circular, 400, int.MaxValue, 130);
            W = new Spell(SpellSlot.W, 700);//, SkillShotType.Circular, 250, 250, 160);
            E = new Spell(SpellSlot.E, 700);
            R = new Spell(SpellSlot.R, 800);//, SkillShotType.Cone, 250, 250, 80);

            Q.SetSkillshot(0.7f, 75f, float.MaxValue, false, SpellType.Circle);
            W.SetSkillshot(0.75f, 160f, 1000, false, SpellType.Circle);
            E.SetTargetted(0.125f, 1000);
            R.SetSkillshot(0.5f, (float)(80 * Math.PI / 180), 3200, false, SpellType.Cone);

            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);
            Flash = new Spell(ObjectManager.Player.GetSpellSlot("summonerflash"));

            var MenuRyze = new Menu("Cassiopeia", "[7UP]Cassiopeia", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            //ComboMenu.Add(new MenuList("ComboMode", "Combo Mode:", new[] { "Dev Combo", "Rylai Combo" })).Permashow();
            ComboMenu.Add(new MenuBool("UseQCombo", "Use Q").SetValue(true));
            ComboMenu.Add(new MenuBool("UseWCombo", "Use W").SetValue(true));
            ComboMenu.Add(new MenuBool("UseECombo", "Use E").SetValue(true));
            ComboMenu.Add(new MenuBool("UseRCombo", "Use R").SetValue(true));
            ComboMenu.Add(new MenuBool("UseAACombo", "AA in Combo").SetValue(true));
            //ComboMenu.Add(new MenuBool("Mode", "Combo Mode Explain").SetValue(true).SetTooltip("Just Here To Explain Rylai Mode - Will Cast E, And Only Use Q Hit Chance Is High AND Target Have Slow Buff"));
            ComboMenu.Add(new MenuKeyBind("Rflash", "Use Flash R", Keys.S, KeyBindType.Press));
            ComboMenu.Add(new MenuSlider("Rminflash", "Min Enemies F + R", 2, 1, 5).SetTooltip("Set This Up! It will affect your game play"));
            //ComboMenu.Add(new MenuBool("UseRSaveYourself", "Use R Save Yourself"));
            //ComboMenu.Add(new MenuSlider("UseRSaveYourselfMinHealth", "Use R Save MinHealth", 25).SetTooltip("Set This Up! It will affect your game play"));
            MenuRyze.Add(ComboMenu);
            HarassMenu = new Menu("Harass Settings", " Harass");
            HarassMenu.Add(new MenuBool("UseQHarass", "Use Q"));
            HarassMenu.Add(new MenuBool("UseEHarass", "Use E"));
            MenuRyze.Add(HarassMenu);
            LaneClearMenu = new Menu("LaneClear Settings", "LaneClear");
            LaneClearMenu.Add(new MenuBool("UseQCL", "Use [Q] in clear ?", false));
            LaneClearMenu.Add(new MenuBool("UseWCL", "Use [W] in clear ?", false));
            LaneClearMenu.Add(new MenuBool("UseECL", "Use [E] in clear ?", false));
            LaneClearMenu.Add(new MenuBool("UseQLH", "Use [Q] in LastHit ?", false));
            LaneClearMenu.Add(new MenuBool("UseWLH", "Use [W] in LastHit ?", false));
            LaneClearMenu.Add(new MenuBool("UseELH", "Use [E] in LastHit ?"));
            LaneClearMenu.Add(new MenuSlider("ClearMana", "Minimum mana for clear %", 50));
            MenuRyze.Add(LaneClearMenu);
            LastHitMenu = new Menu("LastHit Settings", "LastHit");
            LastHitMenu.Add(new MenuBool("UseELastHit", "Use E"));
            LastHitMenu.Add(new MenuBool("UseAaFarm", "Use AA In LastHit"));
            MenuRyze.Add(LastHitMenu);
            JGClear = new Menu("JGClear Settings", "JGClear");
            JGClear.Add(new MenuBool("UseJQCL", "Use [Q] in Jclear ?"));
            JGClear.Add(new MenuBool("UseJWCL", "Use [W] in Jclear ?", false));
            JGClear.Add(new MenuBool("UseJECL", "Use [E] in Jclear ?"));
            JGClear.Add(new MenuBool("UseJQLH", "Use [Q] in JLastHit ?", false));
            JGClear.Add(new MenuBool("UseJWLH", "Use [W] in JLastHit ?", false));
            JGClear.Add(new MenuBool("UseJELH", "Use [E] in JLastHit ?"));
            JGClear.Add(new MenuSlider("ClearManaJ", "Minimum mana J for clear %", 50));
            MenuRyze.Add(JGClear);
            Misc = new Menu("Misc Settings", "Misc");
            //Misc.Add(new MenuBool("tower", "Auto Q Under Tower"));
            Misc.Add(new MenuBool("RAntiGapcloser", "R AntiGapcloser"));
            Misc.Add(new MenuBool("RInterrupetSpell", "R InterruptSpell"));
            Misc.Add(new MenuSlider("RAntiGapcloserMinHealth", "R AntiGapcloser Min Health", 60));
            Misc.Add(new MenuBool("UseQCC", "Use Q on CC"));
            MenuRyze.Add(Misc);
            Utilmate = new Menu("Utilamte", "Utilmate");
            Utilmate.Add(new MenuBool("UseAssistedUlt", "Use AssistedUlt"));
            Utilmate.Add(new MenuKeyBind("AssistedUltKey", "Assisted Ult Key", Keys.R, KeyBindType.Press));
            Utilmate.Add(new MenuBool("UseUltUnderTower", "Ult Enemy Under Tower"));
            Utilmate.Add(new MenuBool("RMinHit", "Min Enemies Hit"));
            Utilmate.Add(new MenuBool("RMinHitFacing", "Min Enemies Facing"));
            MenuRyze.Add(Utilmate);
            draw = new Menu("draw", "Drawing");
            draw.Add(new MenuBool("drawQ", "Draw Q", false));
            draw.Add(new MenuBool("drawW", "Draw W", false));
            draw.Add(new MenuBool("drawE", "Draw E", false));
            draw.Add(new MenuBool("drawR", "Draw R", false));
            MenuRyze.Add(draw);
            MenuRyze.Attach();

            //Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            Game.OnWndProc += Game_OnWndProc;
            AntiGapcloser.OnGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnInterrupterSpell += Interrupter2_OnInterruptableTarget;
            Spellbook.OnCastSpell += Spellbook_OnCastSpell;
            //Orbwalker.OnBeforeAttack += BeforeAttack;
        }

        /*public static bool PoisonWillExpire(this AIBaseClient target, float time)
        {
            var buff = target.Buffs.OrderByDescending(x => x.EndTime).FirstOrDefault(x => x.Type == BuffType.Poison && x.IsActive && x.IsValid);
            return buff == null || time > (buff.EndTime - Game.Time) * 1000f;
        }*/
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
        }*/
        private static void Game_OnWndProc(GameWndEventArgs args)
        {
            if (MenuGUI.IsChatOpen)
            {
                return;
            }

            var UseAssistedUlt = Utilmate["UseAssistedUlt"].GetValue<MenuBool>().Enabled;
            var AssistedUltKey = Utilmate["AssistedUltKey"].GetValue<MenuKeyBind>().Active;

            if (UseAssistedUlt && AssistedUltKey)
            {
                args.Process = false;
                CastAssistedUlt();
            }

        }

        private static void CastAssistedUlt()
        {
            var eTarget = TargetSelector.GetTarget(R.Range, DamageType.Magical);

            if (eTarget.IsValidTarget(R.Range) && R.IsReady())
            {
                R.Cast(eTarget.Position);
            }
        }
        private static void Interrupter2_OnInterruptableTarget(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            var RInterrupetSpell = Misc["RInterrupetSpell"].GetValue<MenuBool>().Enabled;
            var RAntiGapcloserMinHealth = Misc["RAntiGapcloserMinHealth"].GetValue<MenuSlider>().Value;

            if (RInterrupetSpell && Player.HealthPercent < RAntiGapcloserMinHealth && sender.IsValidTarget(R.Range) &&
                args.DangerLevel >= Interrupter.DangerLevel.High)
            {
                R.CastIfHitchanceEquals(sender, sender.IsMoving ? HitChance.High : HitChance.Medium);
            }
        }

        private static void AntiGapcloser_OnEnemyGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs e)
        {
            var RAntiGapcloser = Misc["RAntiGapcloser"].GetValue<MenuBool>().Enabled;
            var RAntiGapcloserMinHealth = Misc["RAntiGapcloserMinHealth"].GetValue<MenuSlider>().Value;

            if (RAntiGapcloser && Player.HealthPercent <= RAntiGapcloserMinHealth &&
                sender.IsValidTarget(R.Range) && R.IsReady() && !sender.IsInvulnerable)
            {
                R.Cast(sender.ServerPosition);
            }
        }
        private static void Spellbook_OnCastSpell(Spellbook sender, SpellbookCastSpellEventArgs args)
        {
            var menuItem = ComboMenu["Rflash"].GetValue<MenuKeyBind>().Active;

            if (!menuItem)
            {
                if (!sender.Owner.IsMe || args.Slot != SpellSlot.R)
                {
                    return;
                }

                if (
                    GameObjects.EnemyHeroes.Any(
                        x => x.IsValidTarget(R.Range + R.Width - 150) && !x.HasBuffOfType(BuffType.Invulnerability)))
                {
                    return;
                }

                args.Process = false;
            }
        }
        /*private static void BeforeAttack(object sender, BeforeAttackEventArgs args)
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
            {
                args.Process = ComboMenu["UseAACombo"].GetValue<MenuBool>().Enabled;

                var target = args.Target as AIBaseClient;

                if (target != null)
                {
                    if (E.IsReady() && target.HasBuffOfType(BuffType.Poison) && target.IsValidTarget(E.Range))
                    {
                        args.Process = false;
                    }
                }
            }

            /*if (Orbwalker.ActiveMode == OrbwalkerMode.LastHit)
            {
                args.Process = LastHitMenu["UseAaFarm"].GetValue<MenuBool>().Enabled;

                var target = args.Target as AIMinionClient;

                if (target != null)
                {
                    if (E.IsReady() && target.HasBuffOfType(BuffType.Poison) && target.IsValidTarget(E.Range))
                    {
                        args.Process = false;
                    }
                }
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear)
            {
                args.Process = LaneClearMenu["UseAaFarmLC"].GetValue<MenuBool>().Enabled;

                var target = args.Target as AIMinionClient;

                if (target != null)
                {
                    if (E.IsReady() && target.HasBuffOfType(BuffType.Poison) && target.IsValidTarget(E.Range))
                    {
                        args.Process = false;
                    }
                }
            }
        }*/

        private static void Game_OnUpdate(EventArgs args)
        {
            if (ObjectManager.Player.IsDead || ObjectManager.Player.IsRecalling() || MenuGUI.IsChatOpen || ObjectManager.Player.IsWindingUp)
            {
                return;
            }
            if (Player.IsDead || Player.IsRecalling())
            {
                return;
            }

            if (ComboMenu["Rflash"].GetValue<MenuKeyBind>().Active)
            {
                FlashCombo();
            }

            //UseUltUnderTower();
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    //BurstCombo();
                    return;
                case OrbwalkerMode.Harass:
                    Harass();
                    return;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    //LastHit();
                    return;
                case OrbwalkerMode.LastHit:
                    LastHit();
                    return;
            }
            ImmobileQ();
        }
        public static void FlashCombo()
        {
            if (GameObjects.EnemyHeroes.Count(x => x.IsValidTarget(R.Range + R.Width + 425f)) > 0 && R.IsReady() &&
            ObjectManager.Player.Spellbook.CanUseSpell(Player.GetSpellSlot("SummonerFlash")) == SpellState.Ready)
            {
                foreach (
                    var target in
                    GameObjects.EnemyHeroes.Where(
                        x =>
                            !x.IsDead && !x.IsZombie() && !x.IsDashing() && x.IsValidTarget(R.Range + R.Width + 425f) &&
                            x.Distance(Player) > R.Range))
                {
                    var flashPos = Player.Position.Extend(target.Position, 425f);
                    var rHit = GetRHitCount(flashPos);

                    if (rHit.Item1.Count >= ComboMenu["Rminflash"].GetValue<MenuSlider>().Value)
                    {
                        var castPos = Player.Position.Extend(rHit.Item2, -(Player.Position.Distance(rHit.Item2) * 2));

                        if (R.Cast(castPos))
                        {
                            DelayAction.Add(300 + Game.Ping / 2,
                                () =>
                                    ObjectManager.Player.Spellbook.CastSpell(Player.GetSpellSlot("SummonerFlash"),
                                        flashPos));
                        }
                    }
                }
            }

            if (Orbwalker.CanMove())
            {
                Orbwalker.Move(Game.CursorPos);

                Combo();
            }
        }
        /*public static void UseUltUnderTower()
        {
            /*var packetCast = Config.Item("PacketCast").GetValue<bool>();
            var UseUltUnderTower = Utilmate["UseUltUnderTower"].GetValue<MenuBool>().Enabled;

            if (UseUltUnderTower)
            {
                foreach (var eTarget in DevHelper.GetEnemyList())
                {
                    if (eTarget.IsValidTarget(R.Range) && eTarget.IsUnderEnemyTurret() && R.IsReady() && !eTarget.IsInvulnerable)
                    {
                        R.Cast(eTarget.Position);
                    }
                }
            }
        }*/
        public static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var targetQ2 = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target == null)
            {
                return;
            }
            if (ComboMenu["UseAACombo"].GetValue<MenuBool>().Enabled && Orbwalker.ActiveMode.HasFlag(OrbwalkerMode.Combo))
            {
                Orbwalker.AttackEnabled = true;
            }
            if (!ComboMenu["UseAACombo"].GetValue<MenuBool>().Enabled && Orbwalker.ActiveMode.HasFlag(OrbwalkerMode.Combo))
            {
                Orbwalker.AttackEnabled = false;
            }
            if (!Orbwalker.ActiveMode.HasFlag(OrbwalkerMode.Combo))
            {
                Orbwalker.AttackEnabled = false;
            }

            if (ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                var targetQ = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

                if (targetQ != null)
                {
                    Q.CastIfHitchanceEquals(targetQ, HitChance.High);
                }
            }
            if (ComboMenu["UseECombo"].GetValue<MenuBool>().Enabled)
            {
                if (!target.IsValidTarget(E.Range) && !E.IsReady())
                    return;
                {
                    if (E.IsReady())
                    {

                        E.Cast(target);
                        Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

                    }
                    if (E.IsReady())
                    {
                        E.Cast(target);

                    }
                }
            }

            if (ComboMenu["UseWCombo"].GetValue<MenuBool>().Enabled)
            {
                if (!W.IsReady() && Player.Distance(target) >= 500) return;
                {

                    var Wpred = W.GetPrediction(target);
                    if (Wpred.Hitchance >= HitChance.High && target.IsValidTarget(W.Range))
                    {
                            var Enemys = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(W.Range));
                            if (Enemys != null)
                            {
                                if (Enemys.Count() >= 2)
                                {
                                    W.Cast(target.Position);

                                }
                                else if (Enemys.Count() >= 1)
                                {
                                    W.Cast(target.Position);
                                }
                            }
                        
                    }

                }
            }
            if (!ComboMenu["UseWCombo"].GetValue<MenuBool>().Enabled)
            {
                W.Cast(target.Position);
            }
			if (ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled)
                {
                    if (Q.IsReady())
                    {
                        var canHitMoreThanOneTarget =
                          GameObjects.EnemyHeroes.OrderByDescending(x => x.CountEnemyHeroesInRange(Q.Width))
                          .FirstOrDefault(x => x.IsValidTarget(Q.Range) && x.CountEnemyHeroesInRange(Q.Width) >= 2);
                        if (canHitMoreThanOneTarget != null)
                        {
                            var getAllTargets = GameObjects.EnemyHeroes.Find(x => x.IsValidTarget() && x.IsValidTarget(Q.Width));
                            // var center = getAllTargets.Aggregate(Vector3.Zero, (current, x) => current + x.Position) / getAllTargets.Count;

                            var Qpred = Q.GetPrediction(target);
                            if (Qpred.Hitchance >= HitChance.High && target.IsValidTarget(Q.Range))
                            {
                                Q.Cast(target);
                            }
                        }
                    }

                }
            if (ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled)
            {
                if (Q.IsReady())
                {
                    var canHitMoreThanOneTarget =
                      GameObjects.EnemyHeroes.OrderByDescending(x => x.CountEnemyHeroesInRange(Q.Width))
                      .FirstOrDefault(x => x.IsValidTarget(Q.Range) && x.CountEnemyHeroesInRange(Q.Width) >= 1);
                    if (canHitMoreThanOneTarget != null)
                    {
                        var getAllTargets = GameObjects.EnemyHeroes.Find(x => x.IsValidTarget() && x.IsValidTarget(Q.Width));
                        //var center = getAllTargets.Aggregate(Vector3.Zero, (current, x) => current + x.Position) / getAllTargets.Count;
                        var Qpred = Q.GetPrediction(target);
                        if (Qpred.Hitchance >= HitChance.High && target.IsValidTarget(Q.Range))
                        {
                            Q.Cast(target);

                        }
                    }
                }
            }
			/*if (ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled)
                {

                    if (!target.IsValidTarget(Q.Range))
                        return;
                    {
                        if (Q.IsReady())
                        {
                            var Qpred = Q.GetPrediction(target);
                            if (Qpred.Hitchance >= HitChance.Medium && target.IsValidTarget(Q.Range))
                            {
                                if (!target.PoisonWillExpire(250))
                                    return;
                                {
                                    Q.Cast(target.Position);
                                    Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                                }

                            }


                        }

                    }
                }*/
			if (ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled)
                {
                    if (Q.IsReady())
                    {

                        var Qpred = Q.GetPrediction(target);
                        if (Qpred.Hitchance >= HitChance.Medium && target.IsValidTarget(Q.Range))
                        {
                            Q.Cast(target);
                            Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);                            
                        }

                    }
                }
			if (ComboMenu["UseRCombo"].GetValue<MenuBool>().Enabled && R.IsReady())
                {
                    var Enemys = GameObjects.EnemyHeroes.Where(x => x.IsValidTarget(R.Range - 25));
                    if (Enemys != null)
                    {
                        if (Enemys.Count() >= 3 && target.IsFacing(Player))
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, target);
                            R.Cast(target);
                        }
                        if (Enemys.Count() >= 3)
                        {
                            Player.IssueOrder(GameObjectOrder.MoveTo, target);
                            R.Cast(target);
                        }
                    }


                }
            if (ComboMenu["UseRCombo"].GetValue<MenuBool>().Enabled && R.IsReady())
            {
                if (!R.IsReady()) return;
                {
                    if (target.IsFacing(Player) && target.IsValidTarget(R.Range))
                    {
                        Player.IssueOrder(GameObjectOrder.MoveTo, target);
                        R.Cast(target.Position);
                    }
                }
                if (target.IsValidTarget(R.Range))
                {
                    Player.IssueOrder(GameObjectOrder.MoveTo, target);
                    R.Cast(target.Position);
                }

            }
        }
        public static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target == null) return;
            if (HarassMenu["UseEHarass"].GetValue<MenuBool>().Enabled)
            {
                if (!target.IsValidTarget(E.Range) && !E.IsReady())
                    return;
                {
                    if (E.IsReady())
                    {
                        E.Cast(target);
                        Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);

                    }
                    if (E.IsReady())
                    {
                        E.Cast(target);

                    }
                }
            }
            if (HarassMenu["UseQHarass"].GetValue<MenuBool>().Enabled)
            {
                if (Q.IsReady())
                {
                    var canHitMoreThanOneTarget =
                      GameObjects.EnemyHeroes.OrderByDescending(x => x.CountEnemyHeroesInRange(Q.Width))
                      .FirstOrDefault(x => x.IsValidTarget(Q.Range) && x.CountEnemyHeroesInRange(Q.Width) >= 1);
                    if (canHitMoreThanOneTarget != null)
                    {
                        var getAllTargets = GameObjects.EnemyHeroes.Find(x => x.IsValidTarget() && x.IsValidTarget(Q.Width));
                        //var center = getAllTargets.Aggregate(Vector3.Zero, (current, x) => current + x.Position) / getAllTargets.Count;
                        var Qpred = Q.GetPrediction(target);
                        if (Qpred.Hitchance >= HitChance.High && target.IsValidTarget(Q.Range))
                        {
                            Q.Cast(target);
                        }
                    }
                }
            }
        }
            /*private static void BurstCombo()
            {
                var eTarget = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);

                if (eTarget == null)
                {
                    return;
                }

                var useQ = Config.Item("UseQCombo").GetValue<bool>();
                var useW = Config.Item("UseWCombo").GetValue<bool>();
                var useE = Config.Item("UseECombo").GetValue<bool>();
                var useR = Config.Item("UseRCombo").GetValue<bool>();
                var useIgnite = Config.Item("UseIgnite").GetValue<bool>();
                /*var packetCast = Config.Item("PacketCast").GetValue<bool>();

                double totalComboDamage = 0;

                if (R.IsReady())
                {
                    totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.R);
                    totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.Q);
                    totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.E);
                }

                totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.Q);
                totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.E);
                totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.E);
                totalComboDamage += Player.GetSpellDamage(eTarget, SpellSlot.E);

                double totalManaCost = 0;

                if (R.IsReady())
                {
                    totalManaCost += ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).SData.ManaArray;
                }

                totalManaCost += ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).SData.Mana;

                //if (mustDebug)
                //{
                //    Chat.Print("BurstCombo Damage {0}/{1} {2}", Convert.ToInt32(totalComboDamage), Convert.ToInt32(eTarget.Health), eTarget.Health < totalComboDamage ? "BustKill" : "Harras");
                //    Chat.Print("BurstCombo Mana {0}/{1} {2}", Convert.ToInt32(totalManaCost), Convert.ToInt32(eTarget.Mana), Player.Mana >= totalManaCost ? "Mana OK" : "No Mana");
                //}

                if (eTarget.Health < totalComboDamage && Player.Mana >= totalManaCost && !eTarget.IsInvulnerable)
                {
                    if (R.IsReady() && useR && eTarget.IsValidTarget(R.Range) && eTarget.IsFacing(Player))
                    {
                        if (totalComboDamage * 0.3 < eTarget.Health) // Anti R OverKill
                        {
                            //if (mustDebug)
                            //    Chat.Print("BurstCombo R");
                            if (R.Cast(eTarget) == Spell.CastStates.SuccessfullyCasted)
                            {
                                dtBurstComboStart = Environment.TickCount;
                            }
                        }
                        else
                        {
                            //if (mustDebug)
                            //    Chat.Print("BurstCombo OverKill");
                            dtBurstComboStart = Environment.TickCount;
                        }
                    }
                }

            }*/

            public static void LastHit()
        {
            var MHR = GameObjects.EnemyMinions.Where(a => a.Distance(Player) <= Q.Range).OrderBy(a => a.Health).FirstOrDefault();
            if (MHR != null)
            {



                if (LaneClearMenu["UseQLH"].GetValue<MenuBool>().Enabled && Q.IsReady() && Player.ManaPercent > LaneClearMenu["ClearMana"].GetValue<MenuSlider>().Value && MHR.IsValidTarget(Q.Range) &&
                    Player.GetSpellDamage(MHR, SpellSlot.Q) >= MHR.Health)

                {
                    Q.Cast(MHR);
                }


                if (LaneClearMenu["UseWLH"].GetValue<MenuBool>().Enabled && W.IsReady() && Player.GetSpellDamage(MHR, SpellSlot.W) >= MHR.Health &&
                    Player.ManaPercent > LaneClearMenu["ClearMana"].GetValue<MenuSlider>().Value)
                {
                    W.Cast(MHR.Position);
                }




            }
        }
        public static void LaneClear()

        {
            if (Q.IsReady() && LaneClearMenu["UseQCL"].GetValue<MenuBool>().Enabled)
            {
                foreach (var minion in GetEnemyLaneMinionsTargetsInRange(Q.Range))
                {

                    if (minion.IsValidTarget(Q.Range) && minion != null && LaneClearMenu["UseQCL"].GetValue<MenuBool>().Enabled)
                    {
                        Q.CastOnUnit(minion);
                    }
                }
            }
            var MHR = GameObjects.EnemyMinions.Where(a => a.Distance(Player) <= Q.Range).OrderBy(a => a.Health).FirstOrDefault();
            if (MHR != null)

                if (LaneClearMenu["UseWCL"].GetValue<MenuBool>().Enabled)
                {
                    if (W.IsReady())
                    {
                        W.Cast(MHR.Position);
                    }

                }

            if (LaneClearMenu["UseECL"].GetValue<MenuBool>().Enabled && E.IsReady() && Player.ManaPercent > LaneClearMenu["ClearMana"].GetValue<MenuSlider>().Value && MHR.IsValidTarget(E.Range))

            {
                E.Cast(MHR);
            }
            foreach (var minion in GetEnemyLaneMinionsTargetsInRange(E.Range))
            {

                if (minion.Health <= GameObjects.Player.GetSpellDamage(minion, SpellSlot.E))
                {
                    if (LaneClearMenu["UseELH"].GetValue<MenuBool>().Enabled)
                    {
                        if (minion.Distance(GameObjects.Player) > 250)
                        {
                            E.CastOnUnit(minion);
                        }
                    }
                    if (LaneClearMenu["UseELH"].GetValue<MenuBool>().Enabled)
                    {
                        E.CastOnUnit(minion);
                    }

                }

            }

        }

        public static void JungleClear()
        {
            var MHR = GameObjects.Jungle.Where(a => a.Distance(Player) <= Q.Range).OrderBy(a => a.Health).FirstOrDefault();
            if (MHR != null)
            {
                if (Q.IsReady() && Player.ManaPercent > JGClear["ClearManaJ"].GetValue<MenuSlider>().Value && JGClear["UseJQCL"].GetValue<MenuBool>().Enabled && MHR.IsValidTarget(Q.Range))
                {
                    Q.Cast(MHR);
                }
            }

            if (W.IsReady() && Q.IsReady() == false && Player.ManaPercent > JGClear["ClearManaJ"].GetValue<MenuSlider>().Value && JGClear["UseJWCL"].GetValue<MenuBool>().Enabled && MHR.IsValidTarget(W.Range))
            {
                W.Cast(MHR.Position);
            }
            if (E.IsReady() && Player.ManaPercent > JGClear["ClearManaJ"].GetValue<MenuSlider>().Value && JGClear["UseJECL"].GetValue<MenuBool>().Enabled && MHR.IsValidTarget(E.Range))
            {
                E.Cast(MHR);
            }
        }

        private static void ImmobileQ()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            if (target == null)
            {
                return;
            }
            if (ComboMenu["UseQ"].GetValue<MenuBool>().Enabled && Misc["UseQCC"].GetValue<MenuBool>().Enabled)

            {
                if (Q.IsReady())
                {

                    var Qpred = Q.GetPrediction(target);
                    if (Qpred.Hitchance >= HitChance.Immobile && target.IsValidTarget(Q.Range))
                    {
                        Q.Cast(target);
                    }
                }
            }

        }


        public static List<AIMinionClient> GetGenericJungleMinionsTargets()
        {
            return GetGenericJungleMinionsTargetsInRange(float.MaxValue);
        }

        public static List<AIMinionClient> GetGenericJungleMinionsTargetsInRange(float range)
        {
            return GameObjects.Jungle.Where(m => !GameObjects.JungleSmall.Contains(m) && m.IsValidTarget(range)).ToList();
        }

        public static List<AIMinionClient> GetEnemyLaneMinionsTargets()
        {
            return GetEnemyLaneMinionsTargetsInRange(float.MaxValue);
        }

        public static List<AIMinionClient> GetEnemyLaneMinionsTargetsInRange(float range)
        {
            return GameObjects.EnemyMinions.Where(m => m.IsValidTarget(range)).ToList();
        }


        private static Tuple<List<AIHeroClient>, Vector3> GetRHitCount(Vector3 fromPos = default(Vector3))//SFX Challenger
        {
            if (fromPos == default(Vector3))
            {
                return new Tuple<List<AIHeroClient>, Vector3>(null, default(Vector3));
            }

            var predInput = new PredictionInput
            {
                Aoe = true,
                Collision = true,
                CollisionObjects = new[] { CollisionObjects.YasuoWall },
                Delay = R.Delay,
                From = fromPos,
                Radius = R.Width,
                Range = R.Range,
                Speed = R.Speed,
                Type = R.Type,
                RangeCheckFrom = fromPos,
                //UseBoundingRadius = true
            };

            var CastPosition = Vector3.Zero;
            var herosHit = new List<AIHeroClient>();
            var targetPosList = new List<RPosition>();

            foreach (var target in GameObjects.EnemyHeroes.Where(x => x.Distance(fromPos) <= R.Width + R.Range))
            {
                predInput.Unit = target;

                var pred = Prediction.GetPrediction(predInput);

                if (pred.Hitchance >= HitChance.High)
                {
                    targetPosList.Add(new RPosition(target, pred.UnitPosition));
                }
            }

            var circle = new Geometry.Circle(fromPos, R.Range).Points;
            foreach (var point in circle)
            {
                var hits = new List<AIHeroClient>();

                foreach (var position in targetPosList)
                {
                    R.UpdateSourcePosition(fromPos, fromPos);

                    if (R.WillHit(position.position, point.ToVector3()))
                    {
                        hits.Add(position.target);
                    }

                    R.UpdateSourcePosition();
                }

                if (hits.Count > herosHit.Count)
                {
                    CastPosition = point.ToVector3();
                    herosHit = hits;
                }
            }

            return new Tuple<List<AIHeroClient>, Vector3>(herosHit, CastPosition);
        }

        /*private static void CastE(Obj_AI_Base unit)
        {
            var PlayLegit = Config.Item("PlayLegit").GetValue<bool>();
            var DisableNFE = Config.Item("DisableNFE").GetValue<bool>();
            var LegitCastDelay = Config.Item("LegitCastDelay").GetValue<Slider>().Value;

            if (PlayLegit)
            {
                if (Environment.TickCount > dtLastECast + LegitCastDelay)
                {
                    E.CastOnUnit(unit);
                    dtLastECast = Environment.TickCount;
                }
            }
            else
            {
                E.CastOnUnit(unit);
                dtLastECast = Environment.TickCount;
            }
        }*/



        private static float GetPoisonBuffEndTime(AIBaseClient target)
        {
            var buffEndTime = target.Buffs.OrderByDescending(buff => buff.EndTime - Game.Time)
                    .Where(buff => buff.Type == BuffType.Poison)
                    .Select(buff => buff.EndTime)
                    .FirstOrDefault();

            return buffEndTime;
        }

        private static float GetEDamage(AIHeroClient hero)
        {
            return (float)Player.GetSpellDamage(hero, SpellSlot.E);
        }

        private class RPosition
        {
            internal AIHeroClient target;
            internal Vector3 position;

            public RPosition(AIHeroClient hero, Vector3 pos)
            {
                target = hero;
                position = pos;
            }
        }

    }
}

