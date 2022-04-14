using System;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.Utility;
using EnsoulSharp.SDK.MenuUI;
using SharpDX;


namespace AIO7UP.Champions
{
    internal class Taliyah
    {
        public static Menu Menu, ComboMenu, HarassMenu, LaneClearMenu, JungleClearMenu, Misc;
        public static Vector3 lastE;
        public static int lastETick = Environment.TickCount;
        public static bool Q5x = true;
        public static bool EWCasting = false;
        public static GameObject selectedGObj = null;
        public static bool pull_push_enemy = false;
        public static AIHeroClient _Player
        {
            get { return ObjectManager.Player; }
        }
        public static Spell Q;
        public static Spell W;
        public static Spell E;
        public static Spell R;

        public static void OnGameLoad()
        {
            if (!_Player.CharacterName.Contains("Taliyah")) return;
            Bootstrap.Init(null);
            Q = new Spell(SpellSlot.Q, 900f);
            Q.SetSkillshot(0.5f, 60f, Q.Instance.SData.MissileSpeed, true, SpellType.Line);
            W = new Spell(SpellSlot.W, 750f);
            W.SetSkillshot(0.8f, 50f, float.MaxValue, false, SpellType.Cone);
            E = new Spell(SpellSlot.E, 700f);
	        E.SetSkillshot(0.25f, 150f, 2000f, false, SpellType.Line);


            var MenuRyze = new Menu("Taliyah", "[7UP]Taliyah", true);
            ComboMenu = new Menu("Combo Settings", "Combo");
            ComboMenu.Add(new MenuBool("UseQ", "Use Q"));
            ComboMenu.Add(new MenuBool("UseW", "Use W"));
            ComboMenu.Add(new MenuBool("UseE", "Use E"));
            MenuRyze.Add(ComboMenu);
            HarassMenu = new Menu("Harass Settings", "Harass");
            HarassMenu.Add(new MenuBool("HarassQ", "Use Q"));
            HarassMenu.Add(new MenuSlider("ManaQ", "Min Mana Harass", 40, 0, 100));
            MenuRyze.Add(HarassMenu);
            LaneClearMenu = new Menu("LaneClear Settings", "LaneClear");
            LaneClearMenu.Add(new MenuBool("LaneQ", "Use Q"));
            LaneClearMenu.Add(new MenuBool("LaneEW", "Use EW"));
            LaneClearMenu.Add(new MenuSlider("MinQ", "MinQ LaneClear", 3, 1, 6));
            LaneClearMenu.Add(new MenuSlider("MinEW", "MinEW LaneClear", 5, 1, 6));
            LaneClearMenu.Add(new MenuSlider("ManaLC", "Min Mana LaneClear", 40, 0, 100));
            MenuRyze.Add(LaneClearMenu);
            JungleClearMenu = new Menu("LaneClear Settings", "LaneClear");
            JungleClearMenu.Add(new MenuBool("LaneQ", "Use Q"));
            JungleClearMenu.Add(new MenuBool("LaneEW", "Use EW"));
            Misc = new Menu("Misc Settings", "Misc");
            Misc.Add(new MenuBool("onlyq5", "Only cast 5x Q"));
            Misc.Add(new MenuBool("antigap", "Auto E to Gapclosers"));
            Misc.Add(new MenuBool("interrupt", "Auto W to interrupt spells"));
            Misc.Add(new MenuKeyBind("pullenemy", "Pull Selected Target", Keys.T, KeyBindType.Press));
            Misc.Add(new MenuKeyBind("pushenemy", "Push Selected Target", Keys.G, KeyBindType.Press));
            MenuRyze.Add(Misc);
            MenuRyze.Attach();

            Game.OnUpdate += Game_OnUpdate;
            AntiGapcloser.OnGapcloser += Gapcloser_OnGapcloser;
            Game.OnWndProc += Game_OnWndProc;
            AIBaseClient.OnProcessSpellCast += AIHeroClient_OnProcessSpellCast;
            Interrupter.OnInterrupterSpell += Events_OnInterruptableTarget;
            GameObject.OnCreate += GameObject_OnCreate;
            GameObject.OnDelete += GameObject_OnDelete;
        }

        private static void Game_OnWndProc(GameWndEventArgs args)
        {
            if (args.Msg == (uint)WindowsKeyMessages.LBUTTONDOWN)
            {
                selectedGObj = ObjectManager.Get<AIBaseClient>().Where(p => p.IsValid && !p.IsMe && !p.IsDead && p.Distance(Game.CursorPos.ToVector2()) < 200 && p.IsAlly).FirstOrDefault();
            }
        }

        private static void AIHeroClient_OnProcessSpellCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (sender.IsMe && args.Slot == SpellSlot.E)
                lastETick = Environment.TickCount;
        }

        private static void GameObject_OnCreate(GameObject sender, EventArgs args)
        {
            if (sender.IsAlly && sender.Name == "Taliyah_Base_Q_aoe_bright.troy")
                Q5x = false;
        }
        private static void GameObject_OnDelete(GameObject sender, EventArgs args)
        {
            if (sender.IsAlly && sender.Name == "Taliyah_Base_Q_aoe_bright.troy")
                Q5x = true;
        }

        private static void Events_OnInterruptableTarget(object sender, Interrupter.InterruptSpellArgs e)
        {
            if (Misc["taliyah.interrupt"].GetValue<MenuBool>().Enabled)
            {
                if (e.Sender.DistanceToPlayer() < W.Range)
                    W.Cast(e.Sender.ServerPosition, ObjectManager.Player.ServerPosition);
            }
        }

        private static void Gapcloser_OnGapcloser(AIHeroClient sender, AntiGapcloser.GapcloserArgs e)
        {
            if (Misc["AntiGap"].GetValue<MenuBool>().Enabled && sender.IsEnemy && sender.Position.Distance(_Player) < E.Range)
            {
                E.Cast(sender);
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
                    break;
                case OrbwalkerMode.LaneClear:
                    LaneClear();
                    JungleClear();
                    break;
            }
            CheckKeyBindings();

        }

        public static void Combo()
        {
            if (W.Instance.Name == "TaliyahWNoClick")
            {
                //
            }
            else
            {
                if (W.IsReady()) //killable W
                {
                    var target = W.GetTarget();
                    if (target != null && target.Health < WDamage(target) - 50)
                    {
                        var pred = W.GetPrediction(target);
                        if (pred.Hitchance >= HitChance.High)
                            W.Cast(pred.UnitPosition, _Player.ServerPosition);
                    }

                }

                if (!EWCasting)
                {
                    if (E.IsReady() && ComboMenu["UseE"].GetValue<MenuBool>().Enabled)
                    {
                        if (W.IsReady() && ComboMenu["UseW"].GetValue<MenuBool>().Enabled)
                        {
                            //e w combo
                            var target = W.GetTarget();
                            if (target != null)
                            {
                                var pred = W.GetPrediction(target);
                                if (pred.Hitchance >= HitChance.High)
                                {
                                    lastE = _Player.ServerPosition;
                                    E.Cast(_Player.ServerPosition.ToVector2() + (pred.CastPosition.ToVector2() - _Player.ServerPosition.ToVector2()).Normalized() * (E.Range - 200));
                                    DelayAction.Add(250, () => { W.Cast(pred.UnitPosition, lastE); EWCasting = false; });
                                    EWCasting = true;
                                }
                            }
                            return;
                        }
                        else
                        {
                            var target = E.GetTarget();
                            if (target != null)
                            {
                                E.Cast(target);
                                lastE = ObjectManager.Player.ServerPosition;
                            }
                        }
                    }
                }
                if (W.IsReady() && ComboMenu["UseW"].GetValue<MenuBool>().Enabled && !EWCasting)
                {
                    var target = W.GetTarget();
                    if (target != null)
                    {
                        var pred = W.GetPrediction(target);
                        if (pred.Hitchance >= HitChance.High)
                            W.Cast(pred.UnitPosition, pred.UnitPosition);
                    }
                }
            }
            var q_target = Q.GetTarget();
            if (q_target != null && ComboMenu["UseQ"].GetValue<MenuBool>().Enabled && (!Misc["onlyq5"].GetValue<MenuBool>().Enabled || Q5x))
                Q.Cast(q_target);

        }
        public static void JungleClear()
        {
            var minionCount = GameObjects.Jungle.Where(x => x.IsValidTarget(E.Range)).ToList();

            {
                foreach (var minion in minionCount)
                {
                    if (_Player.ManaPercent < LaneClearMenu["ManaLC"].GetValue<MenuSlider>().Value)
                        return;
                    if (JungleClearMenu["LaneQ"].GetValue<MenuBool>().Enabled && Q.IsReady()
                        && minion.IsValidTarget(125)
                        )
                    {
                        Q.Cast();
                    }

                    if (JungleClearMenu["LaneEW"].GetValue<MenuBool>().Enabled && W.IsReady() && E.IsReady()
                        && minion.IsValidTarget(W.Range)
                       )
                    {
                        E.Cast(minion.Position);
                        lastE = _Player.ServerPosition;
                        if (W.Instance.Name == "TaliyahW")
                            W.Cast(minion.Position);
                    }

                }
            }
        }
        public static void LaneClear()
        {
            if (_Player.ManaPercent < LaneClearMenu["ManaLC"].GetValue<MenuSlider>().Value)
                return;

            if (LaneClearMenu["LaneQ"].GetValue<MenuBool>().Enabled && Q.IsReady())
            {
                var farm = Q.GetCircularFarmLocation(ObjectManager.Get<AIMinionClient>().Where(p => p.IsEnemy && p.DistanceToPlayer() < Q.Range).ToList());
                if (farm.MinionsHit >= LaneClearMenu["MinQ"].GetValue<MenuSlider>().Value)
                    Q.Cast(farm.Position);
            }

            if (LaneClearMenu["LaneEW"].GetValue<MenuBool>().Enabled && W.IsReady() && E.IsReady())
            {
                var farm = W.GetCircularFarmLocation(ObjectManager.Get<AIMinionClient>().Where(p => p.IsEnemy && p.DistanceToPlayer() < W.Range).ToList());
                if (farm.MinionsHit >= LaneClearMenu["MinEW"].GetValue<MenuSlider>().Value)
                {
                    E.Cast(farm.Position);
                    lastE = _Player.ServerPosition;
                    if (W.Instance.Name == "TaliyahW")
                        W.Cast(farm.Position, lastE.ToVector2());
                }
            }
        }
        private static void CheckKeyBindings()
        {
            if (Misc["pullenemy"].GetValue<MenuKeyBind>().Active || Misc["pushenemy"].GetValue<MenuKeyBind>().Active)
            {

                if (!pull_push_enemy && TargetSelector.SelectedTarget != null && TargetSelector.SelectedTarget.IsValidTarget(W.Range))
                {
                    Vector3 push_position = _Player.ServerPosition;

                    if (Misc["pushenemy"].GetValue<MenuKeyBind>().Active)
                    {
                        if (selectedGObj != null && selectedGObj.Distance(_Player) < 1000)
                            push_position = selectedGObj.Position;
                        else
                            push_position = TargetSelector.SelectedTarget.ServerPosition + (TargetSelector.SelectedTarget.ServerPosition - _Player.ServerPosition).Normalized() * 50f;
                    }
                    var pred = W.GetPrediction(TargetSelector.SelectedTarget);
                    if (pred.Hitchance >= HitChance.High)
                    {
                        pull_push_enemy = true;
                        W.Cast(pred.UnitPosition, push_position);
                        DelayAction.Add(250, () => pull_push_enemy = false);
                    }
                }
            }
        }

        public static void Harass()
        {
            if (_Player.ManaPercent < HarassMenu["ManaQ"].GetValue<MenuSlider>().Value)
                return;

            if (HarassMenu["HarassQ"].GetValue<MenuBool>().Enabled)
            {
                var target = Q.GetTarget();
                if (target != null)
                    Q.Cast(target);
            }
        }

        public static double WDamage(AIBaseClient target)
        {
            return _Player.CalculateDamage(target, DamageType.Magical,
                (float)(new[] { 0, 60, 80, 100, 120, 140 }[W.Level] + 0.6f * _Player.FlatPhysicalDamageMod));
        }

    }
}


