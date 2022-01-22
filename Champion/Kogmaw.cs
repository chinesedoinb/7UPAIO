using System;
using System.Collections.Generic;
using System.Linq;

using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;

using SharpDX;

namespace AIO7UP.Champions
{
    internal class KogMaw
    {
        private static Spell Q, W, E, R;
        private static Menu Config;
        private static float QMANA = 0, WMANA = 0, EMANA = 0, RMANA = 0;
        private static bool attackNow;
        private static int tickIndex;
        

        
        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "KogMaw")
            {
                return;
            }
            
            Q = new Spell(SpellSlot.Q, 1200f);
            W = new Spell(SpellSlot.W, 1000f);
            E = new Spell(SpellSlot.E, 1360f);
            R = new Spell(SpellSlot.R, 1300f);

            Q.SetSkillshot(0.25f, 80f, 1650f, true, SpellType.Line);
            E.SetSkillshot(0.25f, 120f, 1400f, false, SpellType.Line);
            R.SetSkillshot(0.85f, 120f, float.MaxValue, false, SpellType.Circle);

            Config = new Menu("Kogmaw", "[7UP]: Kog'Maw", true);
            var QConfig = new Menu("QConfig", "Q Settings");
            var WConfig = new Menu("WConfig", "W Settings");
            var EConfig = new Menu("EConfig", "E Settings");
            var RConfig = new Menu("RConfig", "R Settings");
            var Draw = new Menu("Draw", "Draw");
            var Farm = new Menu("Farm", "Farm");

            QConfig.Add(new MenuBool("autoQ", "Auto Q", true));
            QConfig.Add(new MenuBool("harrasQ", "Harass Q", true));

            EConfig.Add(new MenuBool("autoE", "Auto E", true));
            EConfig.Add(new MenuBool("HarrasE", "Harass E", true));
            EConfig.Add(new MenuBool("AGC", "AntiGapcloserE", true));

            WConfig.Add(new MenuBool("autoW", "Auto W", true));
            WConfig.Add(new MenuBool("harasW", "Harass W on max range", true));

            RConfig.Add(new MenuBool("autoR", "Auto R", true));
            RConfig.Add(new MenuSlider("RmaxHp", "Target max % HP", 50, 0, 100));
            RConfig.Add(new MenuSlider("comboStack", "Max combo stack R", 2, 0, 10));
            RConfig.Add(new MenuSlider("harasStack", "Max haras stack R", 1, 0, 10));
            RConfig.Add(new MenuBool("Rcc", "R cc", true));
            RConfig.Add(new MenuBool("Rslow", "R slow", true));
            RConfig.Add(new MenuBool("Raoe", "R aoe", true));
            RConfig.Add(new MenuBool("Raa", "R only out off AA range", false));

            Draw.Add(new MenuBool("ComboInfo", "R killable info", true));
            Draw.Add(new MenuBool("qRange", "Q range", false));
            Draw.Add(new MenuBool("wRange", "W range", false));
            Draw.Add(new MenuBool("eRange", "E range", false));
            Draw.Add(new MenuBool("rRange", "R range", false));
            Draw.Add(new MenuBool("onlyRdy", "Draw only ready spells", true));

            Config.Add(new MenuBool("sheen", "Sheen logic", true));
            Config.Add(new MenuBool("AApriority", "AA priority over spell", true));
            Config.Add(new MenuBool("manaDisable", "Disable Mana Manager", false));
            Config.Add(new MenuBool("credit", "Credit: Sebby", false));

            Farm.Add(new MenuBool("farmW", "LaneClear W", true));
            Farm.Add(new MenuBool("farmE", "LaneClear E", true));
            Farm.Add(new MenuSlider("LCminions", "LaneClear minimum minions", 2,0, 10));
            Farm.Add(new MenuSlider("Mana", "LaneClear  Mana", 80, 0, 100));
            Farm.Add(new MenuBool("jungleW", "Jungle clear W", true));
            Farm.Add(new MenuBool("jungleE", "Jungle clear E", true));

            Config.Add(QConfig);
            Config.Add(WConfig);
            Config.Add(EConfig);
            Config.Add(RConfig);
            Config.Add(Draw);
            Config.Add(Farm);
            Config.Attach();

            GameEvent.OnGameTick += OnGameUpdate;
            Drawing.OnDraw += OnDraw;
            AntiGapcloser.OnGapcloser += OnGapcloser;
            Orbwalker.OnBeforeAttack += OnBeforeAttack;
            Orbwalker.OnAfterAttack += OnAfterAttack;
        }
        
        private static void OnGapcloser(AIBaseClient sender, AntiGapcloser.GapcloserArgs args)
        {
            if (Config["EConfig"].GetValue<MenuBool>("AGC").Enabled && E.IsReady() && ObjectManager.Player.Mana > RMANA + EMANA)
            {
                if (sender.IsValidTarget(E.Range))
                {
                    E.Cast(sender);
                }
            }
        }

        private static void OnBeforeAttack(object sender, BeforeAttackEventArgs args)
        {
            attackNow = true;
            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && W.IsReady() && ObjectManager.Player.ManaPercent > Config["Farm"].GetValue<MenuSlider>("Mana").Value)
            {
                var minions = GameObjects.GetMinions(ObjectManager.Player.Position, 650);

                if (minions.Count >= Config["Farm"].GetValue<MenuSlider>("LCminions").Value)
                {
                    if (Config["Farm"].GetValue<MenuBool>("farmW").Enabled && minions.Count > 1)
                        W.Cast();
                }
            }
        }

        private static void OnAfterAttack(object sender, AfterAttackEventArgs args)
        {
            attackNow = false;
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (LagFree(0))
            {
                R.Range = 1050 + 250 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Level;
                W.Range = 630 + 20 * ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Level;
                SetMana();
                Jungle();

            }
            if (LagFree(1) && E.IsReady() && !ObjectManager.Player.Spellbook.IsAutoAttack && Config["EConfig"].GetValue<MenuBool>("autoE").Enabled)
                LogicE();

            if (LagFree(2) && Q.IsReady() && !ObjectManager.Player.Spellbook.IsAutoAttack && Config["QConfig"].GetValue<MenuBool>("autoQ").Enabled)
                LogicQ();

            if (LagFree(3) && W.IsReady() && Config["WConfig"].GetValue<MenuBool>("autoW").Enabled)
                LogicW();

            if (LagFree(4) && R.IsReady() && !ObjectManager.Player.Spellbook.IsAutoAttack)
                LogicR();

            tickIndex++;
            if (tickIndex > 4)
                tickIndex = 0;
        }
        
        private static void Jungle()
        {
            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && ObjectManager.Player.Mana > RMANA + QMANA)
            {
                var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(650)).OrderBy(x => x.MaxHealth).ToList<AIBaseClient>();
                if (mobs.Count > 0)
                {
                    var mob = mobs[0];
                    if (E.IsReady() && Config["Farm"].GetValue<MenuBool>("jungleE").Enabled)
                    {
                        E.Cast(mob.Position);
                        return;
                    }
                    else if (W.IsReady() && Config["Farm"].GetValue<MenuBool>("jungleW").Enabled)
                    {
                        W.Cast();
                        return;
                    }

                }
            }
        }
        
        private static void LogicR()
        {
            if (Config["RConfig"].GetValue<MenuBool>("autoR").Enabled && Sheen())
            {
                var target = TargetSelector.GetTarget(R.Range, DamageType.Magical);

                if (target.IsValidTarget(R.Range) && target.HealthPercent < Config["RConfig"].GetValue<MenuSlider>("RmaxHp").Value)
                {
                    if (Config["RConfig"].GetValue<MenuBool>("Raa").Enabled && target.InAutoAttackRange())
                        return;

                    var harasStack = Config["RConfig"].GetValue<MenuSlider>("harasStack").Value;
                    var comboStack = Config["RConfig"].GetValue<MenuSlider>("comboStack").Value;
                    var countR = GetRStacks();

                    var Rdmg = R.GetDamage(target);
                    Rdmg = Rdmg + target.CountAllyHeroesInRange(500) * Rdmg;

                    if (R.GetDamage(target) > target.Health - ObjectManager.Player.CalculateDamage(target, DamageType.Physical, 1))
                        R.Cast(target);
                    else if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && Rdmg * 2 > target.Health &&
                             ObjectManager.Player.Mana > RMANA * 3)
                        R.Cast(target);
                    else if (countR < comboStack + 2 && ObjectManager.Player.Mana > RMANA * 3)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget(R.Range) && !Orbwalker.CanMove()))
                        {
                            R.Cast(enemy, true);
                        }
                    }

                    if (target.HasBuffOfType(BuffType.Slow) && Config["RConfig"].GetValue<MenuBool>("Rslow").Enabled &&
                        countR < comboStack + 1 && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        R.Cast(target);
                    else if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && countR < comboStack &&
                             ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        R.Cast(target);
                    else if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && countR < harasStack &&
                             ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        R.Cast(target);
                }
            }
        }

        private static void LogicW()
        {
            if (ObjectManager.Player.CountEnemyHeroesInRange(W.Range) > 0)
            {
                if (Orbwalker.ActiveMode == OrbwalkerMode.Combo)
                    W.Cast();
                else if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && Config["EConfig"].GetValue<MenuBool>("harasW").Enabled && ObjectManager.Player.CountEnemyHeroesInRange(ObjectManager.Player.AttackRange) > 0)
                    W.Cast();
            }
        }

        private static void LogicQ()
        {
            if (Sheen())
            {
                var t = TargetSelector.GetTarget(Q.Range, DamageType.Magical);
                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);
                    var eDmg = E.GetDamage(t);
                    if (t.IsValidTarget(W.Range) && qDmg + eDmg > t.Health)
                        Q.Cast(t);
                    else if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && ObjectManager.Player.Mana > RMANA + QMANA * 2 + EMANA)
                        Q.Cast(t);
                    else if ((Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && ObjectManager.Player.Mana > RMANA + EMANA + QMANA * 2 + WMANA) &&
                             Config["QConfig"].GetValue<MenuBool>("harrasQ").Enabled && !ObjectManager.Player.IsUnderEnemyTurret())
                        Q.Cast(t);
                    else if ((Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.LaneClear) && ObjectManager.Player.Mana > RMANA + QMANA + EMANA)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy =>
                            enemy.IsValidTarget(Q.Range) && !Orbwalker.CanMove()))
                            Q.Cast(enemy, true);

                    }
                }
            }
        }

        private static void LogicE()
        {
            if (Sheen())
            {
                var t = TargetSelector.GetTarget(E.Range, DamageType.Magical);
                if (t.IsValidTarget())
                {
                    var qDmg = Q.GetDamage(t);
                    var eDmg = E.GetDamage(t);
                    if (eDmg > t.Health)
                        E.Cast(t);
                    else if (eDmg + qDmg > t.Health && Q.IsReady())
                        E.Cast(t);
                    else if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA)
                        E.Cast(t);
                    else if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && Config["EConfig"].GetValue<MenuBool>("HarrasE").Enabled &&
                             ObjectManager.Player.Mana > RMANA + WMANA + EMANA + QMANA + EMANA)
                        E.Cast(t);
                    else if ((Orbwalker.ActiveMode == OrbwalkerMode.Combo || Orbwalker.ActiveMode == OrbwalkerMode.LaneClear) && ObjectManager.Player.Mana > RMANA + WMANA + EMANA)
                    {
                        foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy =>
                            enemy.IsValidTarget(E.Range) && !Orbwalker.CanMove()))
                            E.Cast(enemy, true);
                    }
                }
                else if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && ObjectManager.Player.ManaPercent > Config["Farm"].GetValue<MenuSlider>("Mana").Value && Config["Farm"].GetValue<MenuBool>("farmE").Enabled && ObjectManager.Player.Mana > RMANA + EMANA)
                {
                    var minionList = GameObjects.GetMinions(ObjectManager.Player.Position, E.Range);
                    var farmPosition = E.GetLineFarmLocation(minionList.ToList(), E.Width);

                    if (farmPosition.MinionsHit >= Config["Farm"].GetValue<MenuSlider>("LCminions").Value)
                        E.Cast(farmPosition.Position);
                }
            }
        }

        private static bool Sheen()
        {
            var target = Orbwalker.GetTarget();
            if (!(target is AIHeroClient))
                attackNow = true;
            if (target.IsValidTarget() && ObjectManager.Player.HasBuff("sheen") && Config.GetValue<MenuBool>("sheen").Enabled && target is AIHeroClient)
            {
                return false;
            }
            else if (target.IsValidTarget() && Config.GetValue<MenuBool>("AApriority").Enabled && target is AIHeroClient && !attackNow)
            {
                return false;
            }
            else
            {
                return true;
            }
        }
        
        public static bool LagFree(int offset)
        {
            if (tickIndex == offset)
                return true;
            else
                return false;
        }
        
        private static void SetMana()
        {
            if ((Config.GetValue<MenuBool>("manaDisable").Enabled && Orbwalker.ActiveMode == OrbwalkerMode.Combo) || ObjectManager.Player.HealthPercent < 20)
            {
                QMANA = 0;
                WMANA = 0;
                EMANA = 0;
                RMANA = 0;
                return;
            }

            QMANA = Q.Mana;
            WMANA = W.Mana;
            EMANA = E.Mana;

            if (!R.IsReady())
                RMANA = QMANA - ObjectManager.Player.PARRegenRate * Q.Instance.Cooldown;
            else
                RMANA = R.Mana;
        }
        
        private static int GetRStacks()
        {
            foreach (var buff in ObjectManager.Player.Buffs)
            {
                if (buff.Name == "kogmawlivingartillerycost")
                    return buff.Count;
            }
            return 0;
        }
        
        private static void OnDraw(EventArgs args)
        {
            if (Config["Draw"].GetValue<MenuBool>("ComboInfo").Enabled)
            {
                var combo = "haras";
                foreach (var enemy in GameObjects.EnemyHeroes.Where(enemy => enemy.IsValidTarget()))
                {
                    if (R.GetDamage(enemy) > enemy.Health)
                    {
                        combo = "KILL R";
                        drawText(combo, enemy, System.Drawing.Color.GreenYellow);
                    }
                    else
                    {
                        combo = (int)(enemy.Health / R.GetDamage(enemy)) + " R";
                        drawText(combo, enemy, System.Drawing.Color.Red);
                    }
                }
            }
            if (Config["Draw"].GetValue<MenuBool>("qRange").Enabled)
            {
                if (Config["Draw"].GetValue<MenuBool>("onlyRdy").Enabled)
                {
                    if (Q.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.Cyan, 1);
            }
            if (Config["Draw"].GetValue<MenuBool>("wRange").Enabled)
            {
                if (Config["Draw"].GetValue<MenuBool>("onlyRdy").Enabled)
                {
                    if (W.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, W.Range, System.Drawing.Color.Orange, 1);
            }
            if (Config["Draw"].GetValue<MenuBool>("eRange").Enabled)
            {
                if (Config["Draw"].GetValue<MenuBool>("onlyRdy").Enabled)
                {
                    if (E.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.Yellow, 1);
            }
            if (Config["Draw"].GetValue<MenuBool>("rRange").Enabled)
            {
                if (Config["Draw"].GetValue<MenuBool>("onlyRdy").Enabled)
                {
                    if (R.IsReady())
                        Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1);
                }
                else
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, R.Range, System.Drawing.Color.Gray, 1);
            }
        }
        
        private static void drawText(string msg, AIHeroClient Hero, System.Drawing.Color color)
        {
            var wts = Drawing.WorldToScreen(Hero.Position);
            Drawing.DrawText(wts[0] - (msg.Length) * 5, wts[1], color, msg);
        }
    }
}