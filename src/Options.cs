using Menu.Remix.MixedUI;
using UnityEngine;

namespace NoDamageRng;

sealed class Options : OptionInterface
{
    public static Configurable<float> DamageMultiplier;
    public static Configurable<int> RecoveryCooldown;

    public Options()
    {
        DamageMultiplier = config.Bind("cfgBiteDamageMul", 1f, new ConfigAcceptableRange<float>(0, 5));
        RecoveryCooldown = config.Bind("cfgRecoveryCooldown", -1, new ConfigAcceptableRange<int>(-1, 60));
    }

    public override void Initialize()
    {
        base.Initialize();

        Tabs = new OpTab[] { new OpTab(this) };

        const string desc =
            "Mod behavior\n" +
            "\n+ When hit by a predator, you take damage instead of randomly being killed." +
            "\n+ Accumulating too much damage kills you." +
            "\n+ Some time after being released, or after sleeping, you recover from damage.";
        const string vanillaDesc =
            "Vanilla behavior\n" +
            "\n+ When not killed outright, you can still escape with your life if your captor drops you before reaching a den." +
            "\n+ After a moment, you will become paralyzed, so throw a rock or spear quickly!";

        var labelAuthor = new OpLabel(20, 600 - 30, "by Dual", true);
        var labelVersion = new OpLabel(20, 600 - 30 - 40, "github.com/Dual-Iron/no-bite-rng");
        var labelNote = new OpLabel(400, 600 - 30 - 20, ":)");

        var size = new Vector2(300 - 20, 150);
        var pos = new Vector2(10, 200 - size.y / 2);
        var rectDescription = new OpRect(pos, size) { description = "Mod mechanics" };
        var labelDescription = new OpLabelLong(pos + new Vector2(10, 0), rectDescription.size - Vector2.one * 20, desc, true, FLabelAlignment.Left);

        pos.x = 310;
        var rectVanillaDescription = new OpRect(pos, size) { description = "Tips on escaping capture" };
        var labelVanillaDescription = new OpLabelLong(pos + new Vector2(10, 0), rectDescription.size - Vector2.one * 20, vanillaDesc, true, FLabelAlignment.Left);

        var top = 200;
        var labelDmgMul = new OpLabel(new(20, 600 - top), Vector2.zero, "Damage multiplier", FLabelAlignment.Left);
        var draggerDmgMul = new OpFloatSlider(DamageMultiplier, new Vector2(180, 600 - top - 6), 320, decimalNum: 1) {
            description = "Increases or decreases how much damage you take from RNG-based attacks.",
        };

        var labelDmgRegen = new OpLabel(new(20, 600 - top - 40), Vector2.zero, "Recovery cooldown", FLabelAlignment.Left);
        var labelSeconds = new OpLabel(new(516, 600 - top - 40), Vector2.zero, "seconds", FLabelAlignment.Left);
        var draggerDmgRegen = new OpSlider(RecoveryCooldown, new Vector2(180, 600 - top - 46), 320) {
            description = "After this delay, you rapidly recover from damage. If set to -1, damage is only reset after sleeping.",
        };

        Tabs[0].AddItems(
            rectVanillaDescription,
            labelVanillaDescription,
            rectDescription,
            labelDescription,
            labelAuthor,
            labelVersion,
            labelNote,
            labelDmgMul,
            draggerDmgMul,
            labelDmgRegen,
            labelSeconds,
            draggerDmgRegen
        );
    }
}
