using BepInEx;
using System;
using System.Runtime.CompilerServices;
using System.Security.Permissions;
using UnityEngine;

#pragma warning disable CS0618 // Do not remove the following line.
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace NoDamageRng;

[BepInPlugin("com.dual.no-damage-rng", "No Damage RNG", "1.1.0")]
sealed class Plugin : BaseUnityPlugin
{
    private sealed class BiteDamage
    {
        public float Damage;
        public int RecoveryCooldown;
    }

    private readonly ConditionalWeakTable<Player, BiteDamage> playerData = new();

    private BiteDamage Damage(Player p) => playerData.GetValue(p, _ => new());

    private void Hurt(Player p, float damage)
    {
        float dmg = Options.DamageMultiplier.Value * damage;

        Damage(p).Damage += dmg;
        Damage(p).RecoveryCooldown = Options.RecoveryCooldown.Value * 40;

        Logger.LogDebug($"Player {p.playerState.playerNumber} received {dmg:0.00} damage");
    }

    public void OnEnable()
    {
        // Add config
        On.RainWorld.OnModsInit += RainWorld_OnModsInit;

        // Show injury
        On.PlayerGraphics.Update += PlayerGraphics_Update;

        // Prevent instant deaths
        On.Lizard.Bite += Lizard_Bite;
        On.Creature.Violence += Creature_Violence;

        // Damage reset cooldown
        On.Player.Update += Player_Update;
    }

    private void RainWorld_OnModsInit(On.RainWorld.orig_OnModsInit orig, RainWorld self)
    {
        orig(self);

        MachineConnector.SetRegisteredOI("no-damage-rng", new Options());
    }

    private void PlayerGraphics_Update(On.PlayerGraphics.orig_Update orig, PlayerGraphics self)
    {
        float lastBreath = self.breath;

        orig(self);

        if (Damage(self.player).Damage >= 0.5f) {
            // Breathe faster, close eyes, and twitch a lot while injured
            self.breath += (self.breath - lastBreath) * 0.5f;
            self.blink = 5;

            if (UnityEngine.Random.value < 1 / 60f) {
                int part = UnityEngine.Random.value < 0.5f ? 0 : 1;
                Vector2 nudge = RWCustom.Custom.RNV() * (2 + 4 * UnityEngine.Random.value);
                self.NudgeDrawPosition(part, nudge);
            }
        }
    }

    private void Lizard_Bite(On.Lizard.orig_Bite orig, Lizard liz, BodyChunk chunk)
    {
        if (chunk.owner is not Player p || liz.AI.friendTracker?.friend == p || liz.lizardParams.biteDamageChance <= 0) {
            orig(liz, chunk);
            return;
        }

        float damageChance = liz.lizardParams.biteDamageChance;

        Hurt(p, damageChance * p.DeathByBiteMultiplier());

        try {
            liz.lizardParams.biteDamageChance = Damage(p).Damage >= 1 ? 1 : 0;
            orig(liz, chunk);
        }
        finally {
            liz.lizardParams.biteDamageChance = damageChance;
        }
    }

    private void Creature_Violence(On.Creature.orig_Violence orig, Creature self, BodyChunk source, Vector2? m, BodyChunk chunk, PhysicalObject.Appendage.Pos _, Creature.DamageType type, float damage, float stun)
    {
        if (self is Player p && source?.owner is Creature crit) {
            float killChance = 0;

            if (crit is BigSpider spider && !spider.spitter) {
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

            if (killChance > 0) {
                Hurt(p, killChance);

                if (damage < 1.5f && Damage(p).Damage >= 1) {
                    damage = 1.5f;
                }
            }
        }

        orig(self, source, m, chunk, _, type, damage, stun);
    }

    private void Player_Update(On.Player.orig_Update orig, Player p, bool eu)
    {
        orig(p, eu);

        if (p.grabbedBy?.Count != 0 || Options.RecoveryCooldown.Value < 0) {
            return;
        }

        BiteDamage dmg = Damage(p);

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
