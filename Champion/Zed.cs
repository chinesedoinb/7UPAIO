using System;
using System.Collections.Generic;
using System.Linq;
using EnsoulSharp;
using EnsoulSharp.SDK;
using EnsoulSharp.SDK.MenuUI;
using SharpDX;
using Color = System.Drawing.Color;

namespace AIO7UP.Champions
{
    internal class Zed
    {
        private static List<Spell> SpellList = new List<Spell>();
        private static Spell _q, _w, _e, _r;
        private static Menu _config;
        private static readonly AIHeroClient _player = ObjectManager.Player;
        private static SpellSlot _igniteSlot;
        private static Items.Item _tiamat, _titanic, _hydra, _blade, _rand, _youmuu;
        private static Vector3 linepos;
        private static int clockon;
        private static int countults;
        private static int countdanger;
        private static int ticktock;
        private static Vector3 rpos;
        private static int shadowdelay;
        private static readonly int delayw = 500;

        private static AIHeroClient GetEnemy => TargetSelector.SelectedTarget == null
            ? TargetSelector.GetTarget(1400, DamageType.Magical)
            : TargetSelector.SelectedTarget;

        private static AIMinionClient WShadow
        {
            get
            {
                return ObjectManager.Get<AIMinionClient>().FirstOrDefault(minion =>
                    minion.IsVisible && minion.IsAlly && minion.Position != rpos && minion.Name == "Shadow");
            }
        }

        private static AIMinionClient RShadow
        {
            get
            {
                return ObjectManager.Get<AIMinionClient>().FirstOrDefault(minion =>
                    minion.IsVisible && minion.IsAlly && minion.Position == rpos && minion.Name == "Shadow");
            }
        }

        private static UltCastStage UltStage
        {
            get
            {
                if (!_r.IsReady()) return UltCastStage.Cooldown;
                return ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "ZedR"
                    ? UltCastStage.First
                    : UltCastStage.Second;
            }
        }

        private static ShadowCastStage ShadowStage
        {
            get
            {
                if (!_w.IsReady()) return ShadowCastStage.Cooldown;
                return ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "ZedW"
                    ? ShadowCastStage.First
                    : ShadowCastStage.Second;
            }
        }



        public static void OnGameLoad()
        {
            if (ObjectManager.Player.CharacterName != "Zed") return;
            _q = new Spell(SpellSlot.Q, 900f);
            _w = new Spell(SpellSlot.W, 700f);
            _e = new Spell(SpellSlot.E, 270f);
            _r = new Spell(SpellSlot.R, 650f);
            _q.SetSkillshot(0.25f, 50f, 1700f, false, SpellType.Line);
            _blade = new Items.Item(ItemId.Blade_of_The_Ruined_King, 425f);
            _hydra = new Items.Item(ItemId.Ravenous_Hydra, 250f);
            _tiamat = new Items.Item(ItemId.Tiamat, 250f);
            _rand = new Items.Item(ItemId.Randuins_Omen, 490f);
            _youmuu = new Items.Item(ItemId.Youmuus_Ghostblade, 10);
            _titanic = new Items.Item(ItemId.Titanic_Hydra, 250f);
            _igniteSlot = _player.GetSpellSlot("SummonerDot");
            _config = new Menu("Zed", "[7UP] Zed", true);
            _config.Add(new Menu("Combo", "Combo"));
            ((Menu) _config["Combo"]).Add(new MenuBool("UseWC", "Use W (also gap close)"));
            ((Menu) _config["Combo"]).Add(new MenuBool("UseIgnitecombo", "Use Ignite(rush for it)"));
            ((Menu) _config["Combo"]).Add(new MenuBool("UseUlt", "Use Ultimate"));
            ((Menu) _config["Combo"]).Add(new MenuKeyBind("ActiveCombo", "Combo!", Keys.Space, KeyBindType.Press));
            ((Menu) _config["Combo"]).Add(new MenuKeyBind("TheLine", "The Line Combo", Keys.T, KeyBindType.Press));
            _config.Add(new Menu("Harass", "Harass"));
            ((Menu) _config["Harass"]).Add(new MenuKeyBind("longhar", "Long Poke (toggle)", Keys.U,
                KeyBindType.Toggle));
            ((Menu) _config["Harass"]).Add(new MenuBool("UseItemsharass", "Use Tiamat/Hydra"));
            ((Menu) _config["Harass"]).Add(new MenuBool("UseWH", "Use W"));
            ((Menu) _config["Harass"]).Add(new MenuKeyBind("ActiveHarass", "Harass!", Keys.C, KeyBindType.Press));
            _config.Add(new Menu("items", "items"));
            ((Menu) _config["items"]).Add(new Menu("Offensive", "Offensive"));
            ((Menu) _config["items"]["Offensive"]).Add(new MenuBool("Youmuu", "Use Youmuu's"));
            ((Menu) _config["items"]["Offensive"]).Add(new MenuBool("Tiamat", "Use Tiamat"));
            ((Menu) _config["items"]["Offensive"]).Add(new MenuBool("Hydra", "Use Hydra"));
            ((Menu) _config["items"]["Offensive"]).Add(new MenuBool("Bilge", "Use Bilge"));
            ((Menu) _config["items"]["Offensive"]).Add(new MenuSlider("BilgeEnemyhp", "If Enemy Hp <", 85, 1));
            ((Menu) _config["items"]["Offensive"]).Add(new MenuSlider("Bilgemyhp", "Or your Hp < ", 85, 1));
            ((Menu) _config["items"]["Offensive"]).Add(new MenuBool("Blade", "Use Blade"));
            ((Menu) _config["items"]["Offensive"]).Add(new MenuSlider("BladeEnemyhp", "If Enemy Hp <", 85, 1));
            ((Menu) _config["items"]["Offensive"]).Add(new MenuSlider("Blademyhp", "Or Your  Hp <", 85, 1));
            ((Menu) _config["items"]).Add(new Menu("Deffensive", "Deffensive"));
            ((Menu) _config["items"]["Deffensive"]).Add(new MenuBool("Omen", "Use Randuin Omen"));
            ((Menu) _config["items"]["Deffensive"]).Add(new MenuSlider("Omenenemys", "Randuin if enemys>", 2, 1, 5));
            ((Menu) _config["items"]["Deffensive"]).Add(new MenuBool("lotis", "Use Iron Solari"));
            ((Menu) _config["items"]["Deffensive"]).Add(new MenuSlider("lotisminhp", "Solari if Ally Hp<", 35, 1));
            _config.Add(new Menu("Farm", "Farm"));
            ((Menu) _config["Farm"]).Add(new Menu("LaneFarm", "LaneFarm"));
            ((Menu) _config["Farm"]["LaneFarm"]).Add(new MenuBool("UseItemslane", "Use Hydra/Tiamat"));
            ((Menu) _config["Farm"]["LaneFarm"]).Add(new MenuBool("UseQL", "Q LaneClear"));
            ((Menu) _config["Farm"]["LaneFarm"]).Add(new MenuBool("UseEL", "E LaneClear"));
            ((Menu) _config["Farm"]["LaneFarm"]).Add(new MenuSlider("Energylane", "Energy Lane% >", 45, 1));
            ((Menu) _config["Farm"]["LaneFarm"]).Add(new MenuKeyBind("Activelane", "Lane clear!", Keys.S,
                KeyBindType.Press));
            ((Menu) _config["Farm"]).Add(new Menu("LastHit", "LastHit"));
            ((Menu) _config["Farm"]["LastHit"]).Add(new MenuBool("UseQLH", "Q LastHit"));
            ((Menu) _config["Farm"]["LastHit"]).Add(new MenuBool("UseELH", "E LastHit"));
            ((Menu) _config["Farm"]["LastHit"]).Add(new MenuSlider("Energylast", "Energy lasthit% >", 85, 1));
            ((Menu) _config["Farm"]["LastHit"]).Add(
                new MenuKeyBind("ActiveLast", "LastHit!", Keys.X, KeyBindType.Press));
            ((Menu) _config["Farm"]).Add(new Menu("Jungle", "Jungle"));
            ((Menu) _config["Farm"]["Jungle"]).Add(new MenuBool("UseItemsjungle", "Use Hydra/Tiamat"));
            ((Menu) _config["Farm"]["Jungle"]).Add(new MenuBool("UseQJ", "Q Jungle"));
            ((Menu) _config["Farm"]["Jungle"]).Add(new MenuBool("UseWJ", "W Jungle"));
            ((Menu) _config["Farm"]["Jungle"]).Add(new MenuBool("UseEJ", "E Jungle"));
            ((Menu) _config["Farm"]["Jungle"]).Add(new MenuSlider("Energyjungle", "Energy Jungle% >", 85, 1));
            ((Menu) _config["Farm"]["Jungle"]).Add(
                new MenuKeyBind("Activejungle", "Jungle!", Keys.S, KeyBindType.Press));
            _config.Add(new Menu("Misc", "Misc"));
            ((Menu) _config["Misc"]).Add(new MenuBool("UseIgnitekill", "Use Ignite KillSteal"));
            ((Menu) _config["Misc"]).Add(new MenuBool("UseQM", "Use Q KillSteal"));
            ((Menu) _config["Misc"]).Add(new MenuBool("UseEM", "Use E KillSteal"));
            ((Menu) _config["Misc"]).Add(new MenuBool("AutoE", "Auto E"));
            ((Menu) _config["Misc"]).Add(new MenuBool("rdodge", "R Dodge Dangerous"));
            foreach (var e in GameObjects.EnemyHeroes)
            {
                var rdata = e.Spellbook.GetSpell(SpellSlot.R);
                if (DangerDB.DangerousList.Any(spell => spell.Contains(rdata.SData.Name)))
                    ((Menu) _config["Misc"]).Add(new MenuBool("ds" + e.CharacterName, rdata.SData.Name));
            }

            _config.Add(new Menu("Drawings", "Drawings"));
            ((Menu) _config["Drawings"]).Add(new MenuBool("DrawQ", "Draw Q"));
            ((Menu) _config["Drawings"]).Add(new MenuBool("DrawE", "Draw E"));
            ((Menu) _config["Drawings"]).Add(new MenuBool("DrawQW", "Draw long harras"));
            ((Menu) _config["Drawings"]).Add(new MenuBool("DrawR", "Draw R"));
            ((Menu) _config["Drawings"]).Add(new MenuBool("DrawHP", "Draw HP bar"));
            ((Menu) _config["Drawings"]).Add(new MenuBool("shadowd", "Shadow Position"));
            ((Menu) _config["Drawings"]).Add(new MenuBool("damagetest", "Damage Text"));
            ((Menu) _config["Drawings"]).Add(new MenuBool("CircleLag", "Lag Free Circles"));
            ((Menu) _config["Drawings"]).Add(new MenuSlider("CircleQuality", "Circles Quality", 100, 10));
            ((Menu) _config["Drawings"]).Add(new MenuSlider("CircleThickness", "Circles Thickness", 1, 1, 10));
            _config.Attach();
            Drawing.OnDraw += OnDraw;
            GameEvent.OnGameTick += OnGameUpdate;
            AIBaseClient.OnDoCast += OnDoCast;
        }

        private static void OnDoCast(AIBaseClient sender, AIBaseClientProcessSpellCastEventArgs args)
        {
            if (!(sender is AIHeroClient)) return;
            if (sender.IsMe && args.SData.Name == "zedult") ticktock = Variables.TickCount + 200;
            else if (sender.IsEnemy)
                if (_config["Misc"].GetValue<MenuBool>("rdodge").Enabled && (_r.IsReady() || _r.Name == "ZedR2") &&
                    _config["Misc"].GetValue<MenuBool>("ds" + sender.CharacterName).Enabled)
                    if (DangerDB.DangerousList.Any(spell => spell.Contains(args.SData.Name)) &&
                        (sender.Distance(_player.Position) < 650f || _player.Distance(args.End) <= 250f))
                    {
                        if (args.SData.Name == "SyndraR")
                        {
                            clockon = Variables.TickCount + 150;
                            countdanger = countdanger + 1;
                        }
                        else
                        {
                            var target = TargetSelector.GetTarget(640, DamageType.Magical);
                            _r.Cast(target);
                        }
                    }
        }

        private static void OnGameUpdate(EventArgs args)
        {
            if (_config["Combo"].GetValue<MenuKeyBind>("ActiveCombo").Active) Combo(GetEnemy);
            if (_config["Combo"].GetValue<MenuKeyBind>("TheLine").Active) TheLine(GetEnemy);
            if (_config["Harass"].GetValue<MenuKeyBind>("ActiveHarass").Active) Harass(GetEnemy);
            if (_config["Farm"]["LaneFarm"].GetValue<MenuKeyBind>("Activelane").Active) Laneclear();
            if (_config["Farm"]["Jungle"].GetValue<MenuKeyBind>("Activejungle").Active) JungleClear();
            if (_config["Farm"]["LastHit"].GetValue<MenuKeyBind>("ActiveLast").Active) LastHit();
            if (_config["Misc"].GetValue<MenuBool>("AutoE").Enabled) CastE();
            if (Variables.TickCount >= clockon && countdanger > countults)
            {
                _r.Cast(TargetSelector.GetTarget(640, DamageType.Magical));
                countults = countults + 1;
            }

            if (LastCast.LastCastPacketSent != null && LastCast.LastCastPacketSent.Slot == SpellSlot.R)
            {
                var shadow = ObjectManager.Get<AIMinionClient>().FirstOrDefault(minion =>
                    minion.IsVisible && minion.IsAlly && minion.Name == "Shadow");
                if (shadow != null) rpos = shadow.Position;
            }

            KillSteal();
        }

        private static float ComboDamage(AIBaseClient enemy)
        {
            if (enemy == null) return 0;
            var damage = 0d;
            if (_igniteSlot != SpellSlot.Unknown && _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, SummonerSpell.Ignite);
            if (_player.CanUseItem((int) ItemId.Tiamat)) damage += _player.BaseAttackDamage * 0.6;
            if (_player.CanUseItem((int) ItemId.Ravenous_Hydra)) damage += _player.BaseAttackDamage * 0.6;
            if (_player.CanUseItem((int) ItemId.Titanic_Hydra))
                damage += _player.BaseAttackDamage * 0.4 + _player.MaxHealth * 0.1;
            if (_player.CanUseItem((int) ItemId.Blade_of_The_Ruined_King)) damage += 100;
            if (_player.CanUseItem((int) ItemId.Blighting_Jewel)) damage += 100;
            if (_q.IsReady()) damage += _player.GetSpellDamage(enemy, SpellSlot.Q);
            if (_w.IsReady() && _q.IsReady()) damage += _player.GetSpellDamage(enemy, SpellSlot.Q) / 2;
            if (_e.IsReady()) damage += _player.GetSpellDamage(enemy, SpellSlot.E);
            if (_r.IsReady()) damage += _player.GetSpellDamage(enemy, SpellSlot.R);
            damage += (_r.Level * 0.15 + 0.05) *
                      (damage - ObjectManager.Player.GetSummonerSpellDamage(enemy, SummonerSpell.Ignite));
            return (float) damage;
        }

        private static void Combo(AIHeroClient t)
        {
            var target = t;
            if (target == null) return;
            var overkill = _player.GetSpellDamage(target, SpellSlot.Q) + _player.GetSpellDamage(target, SpellSlot.E) +
                           _player.GetAutoAttackDamage(target) * 2;
            var doubleu = _player.Spellbook.GetSpell(SpellSlot.W);
            if (_config["Combo"].GetValue<MenuBool>("UseUlt").Enabled && UltStage == UltCastStage.First &&
                (overkill < target.Health || !_w.IsReady() && doubleu.Cooldown > 2f &&
                    _player.GetSpellDamage(target, SpellSlot.Q) < target.Health &&
                    target.Distance(_player.Position) > 400))
            {
                if (target.Distance(_player.Position) > 700 && target.MoveSpeed > _player.MoveSpeed ||
                    target.Distance(_player.Position) > 800)
                {
                    CastW(target);
                    _w.Cast();
                }

                _r.Cast(target);
            }
            else
            {
                if (target != null && _config["Combo"].GetValue<MenuBool>("UseIgnitecombo").Enabled &&
                    _igniteSlot != SpellSlot.Unknown && _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                    if (ComboDamage(target) > target.Health || target.HasBuff("zedulttargetmark"))
                        _player.Spellbook.CastSpell(_igniteSlot, target);
                if (target != null && ShadowStage == ShadowCastStage.First &&
                    _config["Combo"].GetValue<MenuBool>("UseWC").Enabled && target.Distance(_player.Position) > 400 &&
                    target.Distance(_player.Position) < 1300) CastW(target);
                if (target != null && ShadowStage == ShadowCastStage.Second &&
                    _config["Combo"].GetValue<MenuBool>("UseWC").Enabled &&
                    target.Distance(WShadow.Position) < target.Distance(_player.Position)) _w.Cast();
                UseItemes(target);
                CastE();
                CastQ(target);
            }
        }

        private static void TheLine(AIHeroClient t)
        {
            var target = t;
            if (target == null)
            {
                _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
                return;
            }

            _player.IssueOrder(GameObjectOrder.AttackUnit, target);
            if (!_r.IsReady() || target.Distance(_player.Position) >= 640) return;
            if (UltStage == UltCastStage.First) _r.Cast(target);
            linepos = target.Position.Extend(_player.Position, -500);
            if (target != null && ShadowStage == ShadowCastStage.First && UltStage == UltCastStage.Second)
            {
                UseItemes(target);
                if (LastCast.LastCastPacketSent != null && LastCast.LastCastPacketSent.Slot != SpellSlot.W)
                {
                    _w.Cast(linepos);
                    CastE();
                    CastQ(target);
                    if (target != null && _config["Combo"].GetValue<MenuBool>("UseIgnitecombo").Enabled &&
                        _igniteSlot != SpellSlot.Unknown &&
                        _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                        _player.Spellbook.CastSpell(_igniteSlot, target);
                }
            }

            if (target != null && WShadow != null && UltStage == UltCastStage.Second &&
                target.Distance(_player.Position) > 250 &&
                target.Distance(WShadow.Position) < target.Distance(_player.Position)) _w.Cast();
        }

        private static void _CastQ(AIHeroClient target)
        {
            throw new NotImplementedException();
        }

        private static void Harass(AIHeroClient t)
        {
            var target = t;
            if (target == null) return;
            var useItemsH = _config["Harass"].GetValue<MenuBool>("UseItemsharass");
            if (target.IsValidTarget() && _config["Harass"].GetValue<MenuKeyBind>("longhar").Active && _w.IsReady() &&
                _q.IsReady() && ObjectManager.Player.Mana >
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost +
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ManaCost &&
                target.Distance(_player.Position) > 850 && target.Distance(_player.Position) < 1400) CastW(target);
            if (target.IsValidTarget() &&
                (ShadowStage == ShadowCastStage.Second || ShadowStage == ShadowCastStage.Cooldown ||
                 !_config["Harass"].GetValue<MenuBool>("UseWH").Enabled) && _q.IsReady() &&
                (target.Distance(_player.Position) <= 900 || target.Distance(WShadow.Position) <= 900)) CastQ(target);
            if (target.IsValidTarget() && _w.IsReady() && _q.IsReady() &&
                _config["Harass"].GetValue<MenuBool>("UseWH").Enabled && ObjectManager.Player.Mana >
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost +
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ManaCost)
                if (target.Distance(_player.Position) < 750)
                    CastW(target);
            CastE();
            if (useItemsH.Enabled && _tiamat.IsReady && target.Distance(_player.Position) < _tiamat.Range)
                _tiamat.Cast();
            if (useItemsH.Enabled && _hydra.IsReady && target.Distance(_player.Position) < _hydra.Range) _hydra.Cast();
        }

        private static void Laneclear()
        {
            var allMinionsQ = GameObjects.GetMinions(ObjectManager.Player.Position, _q.Range);
            var allMinionsE = GameObjects.GetMinions(ObjectManager.Player.Position, _e.Range);
            var mymana = _player.Mana >= _player.MaxMana *
                _config["Farm"]["LaneFarm"].GetValue<MenuSlider>("Energylane").Value / 100;
            var useItemsl = _config["Farm"]["LaneFarm"].GetValue<MenuBool>("UseItemslane").Enabled;
            var useQl = _config["Farm"]["LaneFarm"].GetValue<MenuBool>("UseQL").Enabled;
            var useEl = _config["Farm"]["LaneFarm"].GetValue<MenuBool>("UseEL").Enabled;
            if (_q.IsReady() && useQl && mymana)
            {
                var fl2 = _q.GetLineFarmLocation(allMinionsQ.ToList(), _q.Width);
                if (fl2.MinionsHit >= 3) _q.Cast(fl2.Position);
                else
                    foreach (var minion in allMinionsQ)
                        if (!minion.InAutoAttackRange() &&
                            minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.Q))
                            _q.Cast(minion);
            }

            if (_e.IsReady() && useEl && mymana)
            {
                if (allMinionsE.Count > 2) _e.Cast();
                else
                    foreach (var minion in allMinionsE)
                        if (!minion.InAutoAttackRange() &&
                            minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.E))
                            _e.Cast();
            }

            if (useItemsl && _tiamat.IsReady && allMinionsE.Count > 2) _tiamat.Cast();
            if (useItemsl && _hydra.IsReady && allMinionsE.Count > 2) _hydra.Cast();
        }

        private static void LastHit()
        {
            var allMinions = GameObjects.GetMinions(ObjectManager.Player.Position, _q.Range);
            var mymana = _player.Mana >=
                         _player.MaxMana * _config["Farm"]["LastHit"].GetValue<MenuSlider>("Energylast").Value / 100;
            var useQ = _config["Farm"]["LastHit"].GetValue<MenuBool>("UseQLH").Enabled;
            var useE = _config["Farm"]["LastHit"].GetValue<MenuBool>("UseELH").Enabled;
            foreach (var minion in allMinions)
            {
                if (mymana && useQ && _q.IsReady() && _player.Distance(minion.Position) < _q.Range &&
                    minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.Q)) _q.Cast(minion);
                if (mymana && _e.IsReady() && useE && _player.Distance(minion.Position) < _e.Range &&
                    minion.Health < 0.95 * _player.GetSpellDamage(minion, SpellSlot.E)) _e.Cast();
            }
        }

        private static void JungleClear()
        {
            var mobs = GameObjects.Jungle.Where(x => x.IsValidTarget(_q.Range)).OrderBy(x => x.MaxHealth)
                .ToList<AIBaseClient>();
            var mymana = _player.Mana >= _player.MaxMana *
                _config["Farm"]["Jungle"].GetValue<MenuSlider>("Energyjungle").Value / 100;
            var useItemsJ = _config["Farm"]["Jungle"].GetValue<MenuBool>("UseItemsjungle").Enabled;
            var useQ = _config["Farm"]["Jungle"].GetValue<MenuBool>("UseQJ").Enabled;
            var useW = _config["Farm"]["Jungle"].GetValue<MenuBool>("UseWJ").Enabled;
            var useE = _config["Farm"]["Jungle"].GetValue<MenuBool>("UseEJ").Enabled;
            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (mymana && _w.IsReady() && useW && _player.Distance(mob.Position) < _q.Range) _w.Cast(mob.Position);
                if (mymana && useQ && _q.IsReady() && _player.Distance(mob.Position) < _q.Range) CastQ(mob);
                if (mymana && _e.IsReady() && useE && _player.Distance(mob.Position) < _e.Range) _e.Cast();
                if (useItemsJ && _tiamat.IsReady && _player.Distance(mob.Position) < _tiamat.Range) _tiamat.Cast();
                if (useItemsJ && _hydra.IsReady && _player.Distance(mob.Position) < _hydra.Range) _hydra.Cast();
            }
        }

        private static void UseItemes(AIHeroClient target)
        {
            var iBilge = _config["items"]["Offensive"].GetValue<MenuBool>("Bilge").Enabled;
            var iBilgeEnemyhp = target.Health <= target.MaxHealth *
                _config["items"]["Offensive"].GetValue<MenuSlider>("BilgeEnemyhp").Value / 100;
            var iBilgemyhp = _player.Health <= _player.MaxHealth *
                _config["items"]["Offensive"].GetValue<MenuSlider>("Bilgemyhp").Value / 100;
            var iBlade = _config["items"]["Offensive"].GetValue<MenuBool>("Blade").Enabled;
            var iBladeEnemyhp = target.Health <= target.MaxHealth *
                _config["items"]["Offensive"].GetValue<MenuSlider>("BladeEnemyhp").Value / 100;
            var iBlademyhp = _player.Health <= _player.MaxHealth *
                _config["items"]["Offensive"].GetValue<MenuSlider>("Blademyhp").Value / 100;
            var iOmen = _config["items"]["Deffensive"].GetValue<MenuBool>("Omen").Enabled;
            var iOmenenemys = ObjectManager.Get<AIHeroClient>().Count(hero => hero.IsValidTarget(450)) >=
                              _config["items"]["Deffensive"].GetValue<MenuSlider>("Omenenemys").Value;
            var iTiamat = _config["items"]["Offensive"].GetValue<MenuBool>("Tiamat").Enabled;
            var iHydra = _config["items"]["Offensive"].GetValue<MenuBool>("Hydra").Enabled;
            var iYoumuu = _config["items"]["Offensive"].GetValue<MenuBool>("Youmuu").Enabled;
            if (_player.Distance(target.Position) <= 450 && iBlade && (iBladeEnemyhp || iBlademyhp) && _blade.IsReady)
                _blade.Cast(target);
            if (_player.Distance(target.Position) <= 300 && iTiamat && _tiamat.IsReady) _tiamat.Cast();
            if (_player.Distance(target.Position) <= 300 && iHydra && _hydra.IsReady) _hydra.Cast();
            if (iOmenenemys && iOmen && _rand.IsReady) _rand.Cast();
            if (_player.Distance(target.Position) <= 350 && iYoumuu && _youmuu.IsReady) _youmuu.Cast();
        }

        private static void CastW(AIBaseClient target)
        {
            if (delayw >= Environment.TickCount - shadowdelay || ShadowStage != ShadowCastStage.First ||
                target.HasBuff("zedulttargetmark") && LastCast.LastCastPacketSent != null &&
                LastCast.LastCastPacketSent.Slot == SpellSlot.R && UltStage == UltCastStage.Cooldown) return;
            var herew = target.Position.Extend(ObjectManager.Player.Position, -200);
            _w.Cast(herew);
            shadowdelay = Environment.TickCount;
        }

        private static void CastQ(AIBaseClient target)
        {
            if (!_q.IsReady() || target == null) return;
            if (WShadow != null && target.Distance(WShadow.Position) <= 900 && target.Distance(_player.Position) > 450)
            {
                var shadowpred = _q.GetPrediction(target);
                _q.UpdateSourcePosition(WShadow.Position, WShadow.Position);
                if (shadowpred.Hitchance >= HitChance.Medium) _q.Cast(target);
            }
            else
            {
                _q.UpdateSourcePosition(_player.Position, _player.Position);
                var normalpred = _q.GetPrediction(target);
                if (normalpred.CastPosition.Distance(_player.Position) < 900 &&
                    normalpred.Hitchance >= HitChance.Medium) _q.Cast(target);
            }
        }

        private static void CastE()
        {
            if (!_e.IsReady()) return;
            if (ObjectManager.Get<AIHeroClient>().Count(hero =>
                    hero.IsValidTarget() && (hero.Distance(ObjectManager.Player.Position) <= _e.Range ||
                                             WShadow != null && hero.Distance(WShadow.Position) <= _e.Range)) >
                0) _e.Cast();
        }

        private static void KillSteal()
        {
            var target = TargetSelector.GetTarget(2000, DamageType.Magical);
            if (target == null) return;
            var igniteDmg = _player.GetSummonerSpellDamage(target, SummonerSpell.Ignite);
            if (target.IsValidTarget() && _config["Misc"].GetValue<MenuBool>("UseIgnitekill").Enabled &&
                _igniteSlot != SpellSlot.Unknown && _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                if (igniteDmg > target.Health && _player.Distance(target.Position) <= 600)
                    _player.Spellbook.CastSpell(_igniteSlot, target);
            if (target.IsValidTarget() && _q.IsReady() && _config["Misc"].GetValue<MenuBool>("UseQM").Enabled &&
                _q.GetDamage(target) > target.Health)
            {
                if (_player.Distance(target.Position) <= _q.Range)
                {
                    _q.Cast(target);
                }
                else if (WShadow != null && WShadow.Distance(target.Position) <= _q.Range)
                {
                    _q.UpdateSourcePosition(WShadow.Position, WShadow.Position);
                    _q.Cast(target);
                }
                else if (RShadow != null && RShadow.Distance(target.Position) <= _q.Range)
                {
                    _q.UpdateSourcePosition(RShadow.Position, RShadow.Position);
                    _q.Cast(target);
                }
            }

            if (target.IsValidTarget() && _q.IsReady() && _config["Misc"].GetValue<MenuBool>("UseQM").Enabled &&
                _q.GetDamage(target) > target.Health)
            {
                if (_player.Distance(target.Position) <= _q.Range)
                {
                    _q.Cast(target);
                }
                else if (WShadow != null && WShadow.Distance(target.Position) <= _q.Range)
                {
                    _q.UpdateSourcePosition(WShadow.Position, WShadow.Position);
                    _q.Cast(target);
                }
            }

            if (_e.IsReady() && _config["Misc"].GetValue<MenuBool>("UseEM").Enabled)
            {
                var t = TargetSelector.GetTarget(_e.Range, DamageType.Magical);
                if (t == null) return;
                if (_e.GetDamage(t) > t.Health &&
                    (_player.Distance(t.Position) <= _e.Range || WShadow.Distance(t.Position) <= _e.Range)) _e.Cast();
            }
        }

        private static void OnDraw(EventArgs args)
        {
            if (RShadow != null) Render.Circle.DrawCircle(RShadow.Position, RShadow.BoundingRadius * 2, Color.Blue);
            if (_config["Drawings"].GetValue<MenuBool>("shadowd").Enabled)
                if (WShadow != null)
                {
                    if (ShadowStage == ShadowCastStage.Cooldown)
                        Render.Circle.DrawCircle(WShadow.Position, WShadow.BoundingRadius * 1.5f, Color.Red);
                    else if (WShadow != null && ShadowStage == ShadowCastStage.Second)
                        Render.Circle.DrawCircle(WShadow.Position, WShadow.BoundingRadius * 1.5f, Color.Yellow);
                }

            if (_config["Drawings"].GetValue<MenuBool>("damagetest").Enabled)
                foreach (var enemyVisible in ObjectManager.Get<AIHeroClient>()
                    .Where(enemyVisible => enemyVisible.IsValidTarget()))
                {
                    var worldToScreen = Drawing.WorldToScreen(enemyVisible.Position);
                    if (ComboDamage(enemyVisible) > enemyVisible.Health)
                        Drawing.DrawText(worldToScreen.X + 50, worldToScreen.Y - 40, Color.Red, "Combo=Rekt");
                    else if (ComboDamage(enemyVisible) + _player.GetAutoAttackDamage(enemyVisible) * 2 >
                             enemyVisible.Health)
                        Drawing.DrawText(worldToScreen.X + 50, worldToScreen.Y - 40, Color.Orange,
                            "Combo + 2 AA = Rekt");
                    else
                        Drawing.DrawText(worldToScreen.X + 50, worldToScreen.Y - 40, Color.Green,
                            "Unkillable with combo + 2AA");
                }

            if (_config["Drawings"].GetValue<MenuBool>("CircleLag").Enabled)
            {
                if (_config["Drawings"].GetValue<MenuBool>("DrawQ").Enabled)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _q.Range, Color.Blue);
                if (_config["Drawings"].GetValue<MenuBool>("DrawE").Enabled)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _e.Range, Color.White);
                if (_config["Drawings"].GetValue<MenuBool>("DrawQW").Enabled &&
                    _config["Harass"].GetValue<MenuKeyBind>("longhar").Active)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, 1400, Color.Yellow);
                if (_config["Drawings"].GetValue<MenuBool>("DrawR").Enabled)
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _r.Range, Color.Blue);
            }
            else
            {
                if (_config["Drawings"].GetValue<MenuBool>("DrawQ").Enabled)
                    Drawing.DrawCircle(ObjectManager.Player.Position, _q.Range, Color.White);
                if (_config["Drawings"].GetValue<MenuBool>("DrawE").Enabled)
                    Drawing.DrawCircle(ObjectManager.Player.Position, _e.Range, Color.White);
                if (_config["Drawings"].GetValue<MenuBool>("DrawQW").Enabled &&
                    _config["Harass"].GetValue<MenuKeyBind>("longhar").Active)
                    Drawing.DrawCircle(ObjectManager.Player.Position, 1400, Color.White);
                if (_config["Drawings"].GetValue<MenuBool>("DrawR").Enabled)
                    Drawing.DrawCircle(ObjectManager.Player.Position, _r.Range, Color.White);
            }
        }

        internal enum UltCastStage
        {
            First,
            Second,
            Cooldown
        }

        internal enum ShadowCastStage
        {
            First,
            Second,
            Cooldown
        }

        private class DangerDB
        {
            public static readonly List<string> DangerousList = new List<string>
            {
                "AhriSeduce",
                "CurseoftheSadMummy",
                "InfernalGuardian",
                "EnchantedCrystalArrow",
                "AzirR",
                "BrandWildfire",
                "CassiopeiaPetrifyingGaze",
                "DariusExecute",
                "DravenRCast",
                "EvelynnR",
                "EzrealTrueshotBarrage",
                "Terrify",
                "GalioIdolOfDurand",
                "GarenR",
                "GravesChargeShot",
                "HecarimUlt",
                "LissandraR",
                "LuxMaliceCannon",
                "UFSlash",
                "AlZaharNetherGrasp",
                "OrianaDetonateCommand",
                "LeonaSolarFlare",
                "SejuaniGlacialPrisonStart",
                "SonaCrescendo",
                "VarusR",
                "GragasR",
                "GnarR",
                "FizzMarinerDoom",
                "SyndraR"
            };

            public static List<string> DodgeW = new List<string>
            {
                "AhriSeduce",
                "AkaliR",
                "AkaliRb",
                "CurseoftheSadMummy",
                "InfernalGuardian",
                "EnchantedCrystalArrow",
                "AzirR",
                "BrandWildfire",
                "CassiopeiaPetrifyingGaze",
                "DariusExecute",
                "DravenRCast",
                "EvelynnR",
                "EzrealTrueshotBarrage",
                "Terrify",
                "GalioIdolOfDurand",
                "GarenR",
                "GravesChargeShot",
                "HecarimUlt",
                "LissandraR",
                "LuxMaliceCannon",
                "UFSlash",
                "AlZaharNetherGrasp",
                "OrianaDetonateCommand",
                "LeonaSolarFlare",
                "SejuaniGlacialPrisonStart",
                "SonaCrescendo",
                "VarusR",
                "GragasR",
                "GnarR",
                "FizzMarinerDoom",
                "SyndraR"
            };
        }
    }
}