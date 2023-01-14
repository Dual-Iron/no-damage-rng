using BepInEx;
using System;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618 // Do not remove the following line.
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace NoDamageRng;

[BepInPlugin("com.dual.no-damage-rng", "No Damage RNG", "1.0.0")]
sealed class Plugin : BaseUnityPlugin
{
    private sealed class BiteDamage
    {
        public float Damage;
        public int RecoveryCooldown;
    }

    private readonly ConditionalWeakTable<Player, BiteDamage> playerData = new();

    private BiteDamage Damage(Player p) => playerData.GetValue(p, _ => new());

    public void OnEnable()
    {
        // Add config
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;

        // Prevent instant deaths
        On.Creature.Violence += Creature_Violence;

        // Damage reset cooldown
        On.Player.Update += Player_Update;
    }

    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        MachineConnector.SetRegisteredOI("no-damage-rng", new Options());
    }

    private void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? m, BodyChunk chunk, PhysicalObject.Appendage.Pos _, Creature.DamageType type, float damage, float stun)
    {
        if (self is Player p && source?.owner is Creature crit) {
            float killChance = 0;

            if (crit is Lizard liz && liz.AI.friendTracker?.friend != p && liz.lizardParams.biteDamageChance > 0) {
                killChance = liz.lizardParams.biteDamageChance * p.DeathByBiteMultiplier();
                damage = 0;
            }
            else if (crit is BigSpider spider && !spider.spitter) {
                killChance = 0.5f;
                damage = 0.4f;
            }
            else if (crit is DropBug dropBug && dropBug.fromCeilingJump) {
                killChance = 0.2f;
                damage = 0.4f;
            }
            else if (crit is EggBug eggBug && eggBug.stabCount >= 2) {
                killChance = 0.5f;
                damage = 0.5f;
            }

            Damage(p).Damage += killChance * Options.DamageMultiplier.Value;
            Damage(p).RecoveryCooldown = Options.RecoveryCooldown.Value;

            if (Damage(p).Damage >= 1) {
                damage = Mathf.Max(damage, 1.5f);
            }
            else if (damage == 0) {
                return;
            }
        }

        orig(self, source, m, chunk, _, type, damage, stun);
    }

    private void Player_Update(On.Player.orig_Update orig, Player p, bool eu)
    {
        orig(p, eu);

        BiteDamage dmg = Damage(p);

        if (p.grabbedBy?.Count == 0 && Options.RecoveryCooldown.Value != -1) {
            dmg.RecoveryCooldown -= 1;

            if (dmg.RecoveryCooldown < 0) {
                // Decrease to 0 over 5 seconds
                dmg.Damage -= 1f / 40f / 5f;
            }
            if (dmg.Damage < 0) {
                dmg.Damage = 0;
            }
        }
    }
}
