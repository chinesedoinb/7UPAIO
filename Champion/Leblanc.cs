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
    internal class Leblanc
    {
        public static Menu Menu, ComboMenu, HarassMenu, LaneClearMenu, JungleClearMenu, drawMenu;
        public static AIHeroClient Player
        {
            get { return ObjectManager.Player; }
        }
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;
        public static Spell Ignite;
        public static Item DFG;


        public static void OnGameLoad()
        {
            if (!Player.CharacterName.Contains("Leblanc")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 700);
            W = new Spell(SpellSlot.W, 600);
            E = new Spell(SpellSlot.E, 950);
            R = new Spell(SpellSlot.R, 700);

            W.SetSkillshot(0.5f, 220, 1300, false, SpellType.Circle);
            E.SetSkillshot(0.25f, 95, 1600, true, SpellType.Line);

            Ignite = new Spell(ObjectManager.Player.GetSpellSlot("summonerdot"), 600);
            DFG = new Item(3142, 10);

            var MenuRyze = new Menu("Leblanc", "[7UP]Leblanc", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuList("ComboMode", "Combo Mode: ", new[] { "Q+R+W+E", "Q+W+R+E", "W+Q+R+E" }, 0)).Permashow();
            ComboMenu.Add(new MenuBool("UseQCombo", "Use Q").SetValue(true));
            ComboMenu.Add(new MenuBool("UseWCombo", "Use W").SetValue(true));
            ComboMenu.Add(new MenuBool("UseECombo", "Use E").SetValue(true));
            ComboMenu.Add(new MenuBool("UseRCombo", "Use R").SetValue(true));
            ComboMenu.Add(new MenuBool("UseDFGCombo", "Use DFG").SetValue(true));
            ComboMenu.Add(new MenuBool("BackCombo", "Back W LowHP/MP or delay").SetValue(true));
            MenuRyze.Add(ComboMenu);
            HarassMenu = new Menu("Harass Settings", "Harass");
            HarassMenu.Add(new MenuBool("UseQHarass", "Use Q").SetValue(true));
            HarassMenu.Add(new MenuBool("UseWHarass", "Use W").SetValue(true));
            HarassMenu.Add(new MenuBool("UseEHarass", "Use E").SetValue(false));
            HarassMenu.Add(new MenuBool("UseRHarass", "Use R").SetValue(false));
            HarassMenu.Add(new MenuBool("UseWQHarass", "Use W+Q Out Range").SetValue(true));
            HarassMenu.Add(new MenuBool("BackHarass", "Back W end Harass").SetValue(true));
            HarassMenu.Add(new MenuKeyBind("harassToggleQ", "Use Q (toggle)", Keys.T, KeyBindType.Toggle)).Permashow();
            MenuRyze.Add(HarassMenu);
            LaneClearMenu = new Menu("LaneClear Settings", "LaneClear");
            LaneClearMenu.Add(new MenuBool("UseQFarm", "Use Q"));
            LaneClearMenu.Add(new MenuBool("UseWFarm", "Use W"));
            LaneClearMenu.Add(new MenuSlider("waveNumW", "Minions to hit with W", 4, 1, 10));
            MenuRyze.Add(LaneClearMenu);
            JungleClearMenu = new Menu("JungleClear Settings", "JungleClear");
            JungleClearMenu.Add(new MenuBool("UseQJFarm", "Use Q"));
            JungleClearMenu.Add(new MenuBool("UseWJFarm", "Use W"));
            JungleClearMenu.Add(new MenuBool("UseEJFarm", "Use E"));
            MenuRyze.Add(JungleClearMenu);
            drawMenu = new Menu("Drawing", "Drawing");
            drawMenu.Add(new MenuBool("DrawQ", "Draw Q").SetValue(true));
            drawMenu.Add(new MenuBool("DrawW", "Draw W").SetValue(true));
            drawMenu.Add(new MenuBool("DrawE", "Draw E").SetValue(true));
            MenuRyze.Add(drawMenu);
            MenuRyze.Attach();
            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Interrupter.OnInterrupterSpell += Interrupter_OnPossibleToInterrupt;
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (drawMenu["DrawQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, Q.Range, Q.IsReady() ? Color.Aqua : Color.Red);
            }
            if (drawMenu["DrawW"].GetValue<MenuBool>().Enabled && W.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, W.Range, W.IsReady() ? Color.Aqua : Color.Red);
            }
            if (drawMenu["DrawE"].GetValue<MenuBool>().Enabled && E.IsReady())
            {
                Render.Circle.DrawCircle(Player.Position, E.Range, E.IsReady() ? Color.Aqua : Color.Red);
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
                    ToggleHarass();
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
            }

        }
        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs args)
        {
            PredictionOutput ePred = E.GetPrediction(sender);
            if (ePred.Hitchance >= HitChance.High)
                E.Cast(ePred.CastPosition);
        }

        private static void Interrupter_OnPossibleToInterrupt(AIHeroClient sender, Interrupter.InterruptSpellArgs args)
        {
            PredictionOutput ePred = E.GetPrediction(sender);
            if (ePred.Hitchance >= HitChance.High)
                E.Cast(ePred.CastPosition);
        }

        private static float GetComboDamage(AIBaseClient enemy)
        {
            var damage = 0d;

            if (Q.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.Q);


            if (W.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.W);

            if (E.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.E);


            if (R.IsReady())
                damage += ObjectManager.Player.GetSpellDamage(enemy, SpellSlot.W);

            return (float)damage * (1.2f);
        }

        private static void Combo()
        {
            var ComboMode = ComboMenu["ComboMode"].GetValue<MenuList>().Index;

            switch (ComboMode)
            {
                case 0:
                    Combo1();
                    break;
                case 1:
                    Combo2();
                    break;
                case 2:
                    Combo3();
                    break;
            }
        }

        private static void Combo1() // Q+R+W+E
        {
            var qTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var wTarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
            var rTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            var comboDamage = wTarget != null ? GetComboDamage(wTarget) : 0;

            var useQ = ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled;
            var useW = ComboMenu["UseWCombo"].GetValue<MenuBool>().Enabled;
            var useE = ComboMenu["UseECombo"].GetValue<MenuBool>().Enabled;
            var useR = ComboMenu["UseRCombo"].GetValue<MenuBool>().Enabled;
            var userDFG = ComboMenu["UseDFGCombo"].GetValue<MenuBool>().Enabled;

            if (Q.IsReady() && useQ)
            {
                if (qTarget != null)
                {
                    Q.CastOnUnit(qTarget);

                    if (userDFG && DFG.IsReady)
                    {
                        DFG.Cast(wTarget);
                    }

                    if (Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                    {
                        R.CastOnUnit(qTarget);
                    }
                }
            }
            if (R.IsReady() && useR)
            {
                if (rTarget != null)
                    R.CastOnUnit(rTarget);
                if (userDFG && DFG.IsReady)
                {
                    DFG.Cast(wTarget);
                }

                if (Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                {
                    R.CastOnUnit(qTarget);
                }
            }
            else
            {
                if (useW && wTarget != null && W.IsReady() && !LeblancPulo())
                {
                    W.CastOnUnit(wTarget);
                }

                if (userDFG && DFG.IsReady)
                {
                    DFG.Cast(wTarget);
                }

                if (useE && eTarget != null && E.IsReady())
                {
                    PredictionOutput ePred = E.GetPrediction(eTarget);
                    if (ePred.Hitchance >= HitChance.High)
                        E.Cast(ePred.CastPosition);
                }

                if (useQ && qTarget != null && Q.IsReady())
                {
                    Q.CastOnUnit(qTarget);
                }

                if (useR && qTarget != null && R.IsReady() && Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                {
                    R.Cast(qTarget);
                }

                if (ComboMenu["BackCombo"].GetValue<MenuBool>().Enabled && LeblancPulo() && (qTarget == null ||
                    !W.IsReady() && !Q.IsReady() && !R.IsReady() ||
                    GetHPPercent() < 15 ||
                    GetMPPercent() < 15))
                {
                    W.Cast();
                }


            }

        }

        private static void Combo2() // Q+W+R+E
        {
            var qTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var wTarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
            var rTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            var comboDamage = wTarget != null ? GetComboDamage(wTarget) : 0;

            var useQ = ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled;
            var useW = ComboMenu["UseWCombo"].GetValue<MenuBool>().Enabled;
            var useE = ComboMenu["UseECombo"].GetValue<MenuBool>().Enabled;
            var useR = ComboMenu["UseRCombo"].GetValue<MenuBool>().Enabled;

            if (Q.IsReady() && useQ)
            {
                if (qTarget != null)
                {
                    Q.CastOnUnit(qTarget);

                }
            }
            if (W.IsReady() && useW && !LeblancPulo())
            {
                if (wTarget != null)
                    W.CastOnUnit(wTarget);
            }

            if (R.IsReady() && useR && !LeblancPulo())

            {
                if (rTarget != null)
                    R.CastOnUnit(rTarget);
                if (Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos") && !LeblancPulo())
                {
                    R.CastOnUnit(qTarget);
                }
            }
            if (E.IsReady() && useE)
            {
                PredictionOutput ePred = E.GetPrediction(eTarget);
                if (ePred.Hitchance >= HitChance.High)
                    E.Cast(ePred.CastPosition);
            }
            else
            {
        
                if (useE && eTarget != null && E.IsReady())
                {
                    PredictionOutput ePred = E.GetPrediction(eTarget);
                    if (ePred.Hitchance >= HitChance.High)
                        E.Cast(ePred.CastPosition);
                }
                if (R.IsReady() && useR && !LeblancPulo())

                {
                    if (rTarget != null)
                        R.CastOnUnit(rTarget);
                    if (Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                    {
                        R.CastOnUnit(eTarget);
                    }
                }

                if (useW && wTarget != null && W.IsReady() && !LeblancPulo())
                {
                    W.CastOnUnit(wTarget);
                }

                if (useQ && qTarget != null && Q.IsReady())
                {
                    Q.CastOnUnit(qTarget);
                }

                if (useR && qTarget != null && R.IsReady() && (Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos") ||
                                                               Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancSlideM")))
                {
                    R.Cast(qTarget);
                }

                if (ComboMenu["BackCombo"].GetValue<MenuBool>().Enabled && LeblancPulo() && (qTarget == null ||
                    !W.IsReady() && !Q.IsReady() && !R.IsReady() ||
                    GetHPPercent() < 30 ||
                    GetMPPercent() < 30))
                {
                    W.Cast();
                }
            }

            if (wTarget != null)
            {
                if (Player.Distance(wTarget) < 650 && comboDamage > wTarget.Health)
                {
                    W.Cast(wTarget);
                }
            }
        }

        private static void Combo3() // W+Q+R+E
        {
            var qTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var wTarget = TargetSelector.GetTarget(W.Range, DamageType.Magical);
            var rTarget = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var eTarget = TargetSelector.GetTarget(E.Range, DamageType.Magical);
            var comboDamage = wTarget != null ? GetComboDamage(wTarget) : 0;

            var useQ = ComboMenu["UseQCombo"].GetValue<MenuBool>().Enabled;
            var useW = ComboMenu["UseWCombo"].GetValue<MenuBool>().Enabled;
            var useE = ComboMenu["UseECombo"].GetValue<MenuBool>().Enabled;
            var useR = ComboMenu["UseRCombo"].GetValue<MenuBool>().Enabled;


            if (W.IsReady() && useW && !LeblancPulo())
            {
                if (wTarget != null)
                    W.CastOnUnit(wTarget);
            }

            if (Q.IsReady() && useQ)
            {
                if (qTarget != null)
                {
                    Q.CastOnUnit(qTarget);


                    if (Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                    {
                        R.CastOnUnit(qTarget);
                    }
                }
            }

            if (R.IsReady() && useR)

            {
                if (rTarget != null)
                    R.CastOnUnit(qTarget);
                if (Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos") && !LeblancPulo())
                {
                    R.CastOnUnit(qTarget);
                }
            }
            if (E.IsReady() && useE)
            {
                PredictionOutput ePred = E.GetPrediction(eTarget);
                if (ePred.Hitchance >= HitChance.High)
                    E.Cast(ePred.CastPosition);
                
            }
            else
            {

                if (useE && eTarget != null && E.IsReady())
                {
                    PredictionOutput ePred = E.GetPrediction(eTarget);
                    if (ePred.Hitchance >= HitChance.High)
                        E.Cast(ePred.CastPosition);
                }
                if (R.IsReady() && useR && !LeblancPulo())

                {
                    if (rTarget != null)
                        R.CastOnUnit(rTarget);
                    if (Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                    {
                        R.CastOnUnit(eTarget);
                    }
                }

                if (useW && wTarget != null && W.IsReady() && !LeblancPulo())
                {
                    W.CastOnUnit(wTarget);
                }

                if (useQ && qTarget != null && Q.IsReady())
                {
                    Q.CastOnUnit(qTarget);
                }

                if (useR && qTarget != null && R.IsReady() && (Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos") ||
                                                               Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancSlideM")))
                {
                    R.Cast(qTarget);
                }

                if (ComboMenu["BackCombo"].GetValue<MenuBool>().Enabled && LeblancPulo() && (qTarget == null ||
                    !W.IsReady() && !Q.IsReady() && !R.IsReady() ||
                    GetHPPercent() < 30 ||
                    GetMPPercent() < 30))
                {
                    W.Cast();
                }
            }

        }




        private static float GetHPPercent()
        {
            return (ObjectManager.Player.Health / ObjectManager.Player.MaxHealth) * 100f;
        }
        private static float GetMPPercent()
        {
            return (ObjectManager.Player.Mana / ObjectManager.Player.MaxMana) * 100f;
        }

        private static void ToggleHarass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);

            if (target != null && Q.IsReady())
            {
                Q.CastOnUnit(target);
            }

        }

        private static bool LeblancPulo()
        {
            if (!W.IsReady() || Player.Spellbook.GetSpell(SpellSlot.W).Name == "leblancslidereturn")
            {
                return true;
            }
            else
            {
                return false;
            }

        }

        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
            var rtarget = TargetSelector.GetTarget(W.Range + Q.Range, DamageType.Magical);

            var useWQHarass = HarassMenu["UseWQHarass"].GetValue<MenuBool>().Enabled;
            var useQHarass = HarassMenu["UseQHarass"].GetValue<MenuBool>().Enabled;
            var useWHarass = HarassMenu["UseWHarass"].GetValue<MenuBool>().Enabled;
            var useEHarass = HarassMenu["UseEHarass"].GetValue<MenuBool>().Enabled;
            var useRHarass = HarassMenu["UseRHarass"].GetValue<MenuBool>().Enabled;
            var back = HarassMenu["BackHarass"].GetValue<MenuBool>().Enabled;

            if (useWQHarass && Player.Distance(rtarget) > Q.Range && Player.Distance(rtarget) <= W.Range + Q.Range)
            {
                if (W.IsReady() && Q.IsReady())
                {
                    W.Cast(rtarget.Position);
                }

                if (target != null && useQHarass && Q.IsReady() && Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancSlideM"))
                {
                    Q.CastOnUnit(target);
                }

            }
            else
            {
                if (target != null && useRHarass && R.IsReady() && Player.Spellbook.GetSpell(SpellSlot.R).Name.Contains("LeblancChaos"))
                {
                    R.CastOnUnit(target);
                }

                if (target != null && useQHarass && Q.IsReady())
                {
                    Q.CastOnUnit(target);
                }

                if (target != null && useWHarass && !LeblancPulo())
                {
                    W.CastOnUnit(target);
                }

                if (target != null && useEHarass)
                {
                    PredictionOutput ePred = E.GetPrediction(target);
                    if (ePred.Hitchance >= HitChance.High)
                        E.Cast(ePred.CastPosition);
                }

                if (useWQHarass && back && LeblancPulo() && !Q.IsReady())
                {
                    if (useRHarass && R.IsReady()) return;
                    if (useEHarass && E.IsReady()) return;

                    W.Cast();
                }
            }
        }

        private static void LaneClear()
        {
            var useQ = LaneClearMenu["UseQFarm"].GetValue<MenuBool>().Enabled;
            var useW = LaneClearMenu["UseWFarm"].GetValue<MenuBool>().Enabled;
            var useE = LaneClearMenu["waveNumW"].GetValue<MenuSlider>().Value;
        }

        private static void JungleClear()
        {
            var useQ = JungleClearMenu["UseQJFarm"].GetValue<MenuBool>().Enabled;
            var useW = JungleClearMenu["UseWJFarm"].GetValue<MenuBool>().Enabled;
            var useE = JungleClearMenu["UseEJFarm"].GetValue<MenuBool>().Enabled;

        }

    }
}

        


    


