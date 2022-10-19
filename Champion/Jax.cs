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
    internal class Jax
    {
        private static Menu Config;
        private static Spell Q, W, E, R;
        private static bool justE, justWJ;

        private static Array ItemIds = new[]
        {
            3077,
            3074,
            3748,
        };
        

        
        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Jax")
            {
                return;
            }
            
            Q = new Spell(SpellSlot.Q, 700);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E, 350);
            R = new Spell(SpellSlot.R);
            
            Config = new Menu("Jax ", "[7UP] Jax", true);
            
            var menuD = new Menu("dsettings", "Drawings ");
            menuD.Add(new MenuBool("drawqq", "Draw Q range", true));
            menuD.Add(new MenuBool("drawee", "Draw E range", true));
            Config.Add(menuD);
            
            var menuC = new Menu("csettings", "Combo ");
            menuC.Add(new MenuBool("useq", "Use Q", true));
            menuC.Add(new MenuBool("useqLimit", "   Limit usage", true));
            menuC.Add(new MenuBool("useqSec", "Use Q to secure kills", false));
            menuC.Add(new MenuBool("usew", "Use W", true));
            menuC.Add(new MenuBool("useeStun", "Use E to stun", false));
            menuC.Add(new MenuBool("useeAA", "Block AA from target", true));
            menuC.Add(new MenuBool("user", "Use R", true));
            menuC.Add(new MenuSlider("userMin", "   Min enemies around", 2, 1, 5));
            menuC.Add(new MenuBool("useIgnite", "Use Ignite", true));
            Config.Add(menuC);
            
            var menuH = new Menu("Hsettings", "Harass ");
            menuH.Add(new MenuBool("useqH", "Use Q", true));
            menuH.Add(new MenuBool("usewH", "Use W on target", true));
            menuH.Add(new MenuSlider("minmanaH", "Keep X% mana", 1, 1, 100));
            Config.Add(menuH);
            
            var menuLC = new Menu("Lcsettings", "LaneClear ");
            menuLC.Add(new MenuBool("useqLC", "Use Q", true));
            menuLC.Add(new MenuBool("usewLC", "Use w", true));
            menuLC.Add(new MenuSlider("minmana", "Keep X% mana", 1, 1, 100));
            Config.Add(menuLC);

            var menuM = new Menu("Msettings", "Misc ");
            menuM.Add(new MenuKeyBind("wardJump", "Ward jump", Keys.Z, KeyBindType.Press)).AddPermashow();

            Config.Add(menuM);
            Config.Attach();
            
            GameEvent.OnGameTick += OnGameUpdate;
            //Drawing.OnDraw += OnDraw;
            Dash.OnDash += OnDash;
            Orbwalker.OnAfterAttack += OnAfterAttack;
        }
        
        private static void OnGameUpdate(EventArgs args)
        {
            Orbwalker.MoveEnabled = true;
            
            switch (Orbwalker.ActiveMode)
            {
                case OrbwalkerMode.Combo:
                    Combo();
                    break;
                case OrbwalkerMode.Harass:
                    Harass();
                    break;
                case OrbwalkerMode.LaneClear:
                    Clear();
                    break;
                case OrbwalkerMode.LastHit:
                    break;
                default:
                    break;
            }
            
            if (Config["Msettings"].GetValue<MenuKeyBind>("wardJump").Active)
            {
                WardJump();
            }
        }

        private static void OnAfterAttack(object sender, AfterAttackEventArgs args)
        {
            if (args.Target == null || !args.Target.IsValidTarget())
            {
                return;
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && args.Target is AIHeroClient &&
                Config["csettings"].GetValue<MenuBool>("usew").Enabled)
            {
                if (W.Cast() || castHydra(args.Target))
                {
                    Orbwalker.ResetAutoAttackTimer();
                }
            }

            if (Orbwalker.ActiveMode == OrbwalkerMode.LaneClear && args.Target is AIMinionClient &&
                Config["Lcsettings"].GetValue<MenuBool>("usewLC").Enabled && GameObjects.AttackableUnits.Where(x =>
                        x.IsValidTarget(ObjectManager.Player.GetRealAutoAttackRange()) && x.IsEnemy)
                    .Any(m => m.Health > ObjectManager.Player.GetAutoAttackDamage((AIBaseClient) args.Target)))
            {
                if (W.Cast() || castHydra(args.Target))
                {
                    Orbwalker.ResetAutoAttackTimer();
                }
            }
        }
        
        private static void OnDash(AIBaseClient sender, Dash.DashArgs args)
        {
            if (sender.Distance(ObjectManager.Player.Position) > Q.Range || !Q.IsReady())
            {
                return;
            }
            if (Orbwalker.ActiveMode == OrbwalkerMode.Combo && Config["csettings"].GetValue<MenuBool>("useq").Enabled &&
                args.EndPos.Distance(ObjectManager.Player.Position) > Q.Range &&
                args.EndPos.Distance(ObjectManager.Player) > args.StartPos.Distance(ObjectManager.Player))
            {
                Q.CastOnUnit(sender);
            }
        }
        
        /*private static void OnDraw(EventArgs args)
        {
            if (Config["dsettings"].GetValue<MenuBool>("drawqq").Enabled)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, Q.Range, System.Drawing.Color.DodgerBlue);
            }
            if (Config["dsettings"].GetValue<MenuBool>("drawee").Enabled)
            {
                Render.Circle.DrawCircle(ObjectManager.Player.Position, E.Range, System.Drawing.Color.DodgerBlue);
            }
        }*/
        
        private static bool Eactive => ObjectManager.Player.HasBuff("JaxCounterStrike");

        private static void HandleECombo()
        {
            if (!Eactive && Config["csettings"].GetValue<MenuBool>("useeStun").Enabled && E.IsReady() && !justE)
            {
                justE = true;
                DelayAction.Add(
                    new Random().Next(10, 60), () =>
                    {
                        E.Cast();
                        justE = false;
                    });
            }
        }
        
        private static bool castHydra(AttackableUnit target)
        {
            var result = false;
            
            foreach (int itemId in ItemIds)
            {
                if (Items.CanUseItem(ObjectManager.Player, itemId))
                {
                    result = ObjectManager.Player.UseItem(itemId);
                }
                if(result)
                    return result;
            }
            return result;
        }
        
        private static void WardJump()
        {
            ObjectManager.Player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            
            if (!Q.IsReady())
            {
                return;
            }
            
            var wardSlot = ObjectManager.Player.GetWardSlot();
            var pos = Game.CursorPos;
            if (pos.Distance(ObjectManager.Player.Position) > 600)
            {
                pos = ObjectManager.Player.Position.Extend(pos, 600);
            }

            var jumpObj = GetJumpObj(pos);
            if (jumpObj != null)
            {
                Q.CastOnUnit(jumpObj);
            }
            else
            {
                if (wardSlot != null && ObjectManager.Player.CanUseItem((int)wardSlot.Slot) &&
                    (ObjectManager.Player.Spellbook.CanUseSpell(wardSlot.SpellSlot) == SpellState.Ready && !justWJ))
                {
                    justWJ = true;
                    DelayAction.Add(new Random().Next(1000, 1500), () => { justWJ = false; });
                    ObjectManager.Player.Spellbook.CastSpell(wardSlot.SpellSlot, pos);
                    DelayAction.Add(
                        150, () =>
                        {
                            var predWard = GetJumpObj(pos);
                            if (predWard != null && Q.IsReady())
                            {
                                Q.CastOnUnit(predWard);
                            }
                        });
                }
            }
        }

        public static AIBaseClient GetJumpObj(Vector3 pos)
        {
            return
                ObjectManager.Get<AIBaseClient>()
                    .Where(
                        obj =>
                            obj.IsValidTarget(600, false) && pos.Distance(obj.Position) <= 100 &&
                            (obj is AIMinionClient || obj is AIHeroClient))
                    .OrderBy(obj => obj.Distance(pos))
                    .FirstOrDefault();
        }
        
        private static void Combo()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);

            if (target == null || target.IsInvulnerable || target.IsMagicalImmune)
            {
                return;
            }
            
            var ignitedmg = (float)ObjectManager.Player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);
            var hasIgnite = ObjectManager.Player.Spellbook.CanUseSpell(ObjectManager.Player.GetSpellSlot("SummonerDot")) == SpellState.Ready;
            if (Config["csettings"].GetValue<MenuBool>("useIgnite").Enabled && ignitedmg > target.Health && hasIgnite &&
                ((target.Distance(ObjectManager.Player) > ObjectManager.Player.GetRealAutoAttackRange() &&
                  (!Q.IsReady() || Q.Mana < ObjectManager.Player.Mana)) || ObjectManager.Player.HealthPercent < 35))
            {
                ObjectManager.Player.Spellbook.CastSpell(ObjectManager.Player.GetSpellSlot("SummonerDot"), target);
            }
            
            if (Q.CanCast(target))
            {
                if (Config["csettings"].GetValue<MenuBool>("useqLimit").Enabled)
                {
                    if (ObjectManager.Player.CountEnemyHeroesInRange(Q.Range) == 1 && Config["csettings"].GetValue<MenuBool>("useq").Enabled &&
                        (target.Distance(ObjectManager.Player) > ObjectManager.Player.GetRealAutoAttackRange() ||
                         (Q.GetDamage(target) > target.Health) &&
                         (ObjectManager.Player.HealthPercent < 50 || ObjectManager.Player.CountAllyHeroesInRange(900) > 0)))
                    {
                        if (Q.CastOnUnit(target))
                        {
                            HandleECombo();
                        }
                    }
                    if ((ObjectManager.Player.CountEnemyHeroesInRange(Q.Range) > 1 && Config["csettings"].GetValue<MenuBool>("useqSec").Enabled &&
                         Q.GetDamage(target) > target.Health) || ObjectManager.Player.HealthPercent < 35f ||
                        target.Distance(ObjectManager.Player) > ObjectManager.Player.GetRealAutoAttackRange())
                    {
                        if (Q.CastOnUnit(target))
                        {
                            HandleECombo();
                        }
                    }
                }
                else
                {
                    if (Q.CastOnUnit(target))
                    {
                        HandleECombo();
                    }
                }
            }
            
            if (R.IsReady() && Config["csettings"].GetValue<MenuBool>("user").Enabled)
            {
                if (ObjectManager.Player.CountEnemyHeroesInRange(Q.Range) >= Config["csettings"].GetValue<MenuSlider>("userMin").Value)
                {
                    R.Cast();
                }
            }
            
            if (Config["csettings"].GetValue<MenuBool>("useeAA").Enabled && !Eactive &&
                HealthPrediction.GetPrediction(ObjectManager.Player, 100, 70) < ObjectManager.Player.Health - target.GetAutoAttackDamage(ObjectManager.Player))
            {
                E.Cast();
            }
            
            if (Eactive)
            {
                if (E.IsReady() && target.IsValidTarget() && !target.IsMagicalImmune &&
                    ((Prediction.GetPrediction(target, 0.1f).UnitPosition.Distance(ObjectManager.Player.Position) >
                         ObjectManager.Player.GetRealAutoAttackRange() && target.Distance(ObjectManager.Player.Position) <= E.Range) ||
                     Config["csettings"].GetValue<MenuBool>("useeStun").Enabled))
                {
                    E.Cast();
                }
            }
            else
            {
                if (Config["csettings"].GetValue<MenuBool>("useeStun").Enabled &&
                    Prediction.GetPrediction(target, 0.1f).UnitPosition.Distance(ObjectManager.Player.Position) <
                    ObjectManager.Player.GetRealAutoAttackRange() && target.Distance(ObjectManager.Player.Position) <= E.Range)
                {
                    E.Cast();
                }
            }
        }
        
        private static void Harass()
        {
            var target = TargetSelector.GetTarget(Q.Range, DamageType.Physical);
            var perc = Config["Hsettings"].GetValue<MenuSlider>("minmanaH").Value / 100f;
            
            if (ObjectManager.Player.Mana < ObjectManager.Player.MaxMana * perc || target == null)
            {
                return;
            }
            
            if (Config["Hsettings"].GetValue<MenuBool>("useqH").Enabled && Orbwalker.CanMove() && !ObjectManager.Player.IsWindingUp &&
                Q.CanCast(target))
            {
                Q.CastOnUnit(target);
            }
        }
        
        private static void Clear()
        {
            var perc = Config["Lcsettings"].GetValue<MenuSlider>("minmana").Value / 100f;
            if (ObjectManager.Player.Mana < ObjectManager.Player.MaxMana * perc)
            {
                return;
            }
            
            if (Q.IsReady() && Config["Lcsettings"].GetValue<MenuBool>("useqLC").Enabled)
            {
                var minions =
                    GameObjects.EnemyMinions.Where(m => (m.IsMinion() || m.IsJungle()) && Q.CanCast(m) && (Q.GetDamage(m) > m.Health || m.Health > ObjectManager.Player.GetAutoAttackDamage(m) * 5))
                        .OrderByDescending(m => Q.GetDamage(m) > m.Health)
                        .ThenBy(m => m.DistanceToPlayer());
                foreach (var mini in minions)
                {
                    if (!Orbwalker.CanAttack() && mini.DistanceToPlayer() <= ObjectManager.Player.GetRealAutoAttackRange())
                    {
                        Q.CastOnUnit(mini);
                        return;
                    }
                    if (Orbwalker.CanMove() && !ObjectManager.Player.IsWindingUp &&
                        mini.DistanceToPlayer() > ObjectManager.Player.GetRealAutoAttackRange())
                    {
                        Q.CastOnUnit(mini);
                        return;
                    }
                }
            }
        }
    }
}