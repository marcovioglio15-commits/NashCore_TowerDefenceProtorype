using UnityEngine;

namespace Scriptables.Turrets
{
    /// <summary>
    /// Immutable aggregate holding turret statistics after applying optional free-aim multipliers.
    /// </summary>
    public readonly struct TurretStatSnapshot
    {
        #region Variables And Properties
        #region Durability
        public float Health { get; }
        public float Armor { get; }
        public float MagicResistance { get; }
        public float PassiveRegenPerSecond { get; }
        #endregion

        #region Targeting
        public float Range { get; }
        public float TurnRate { get; }
        public float DeadZoneRadius { get; }
        public float RetargetInterval { get; }
        #endregion

        #region Automatic Fire
        public float AutomaticCadenceSeconds { get; }
        public int AutomaticProjectilesPerShot { get; }
        public float AutomaticInterProjectileDelay { get; }
        public float AutomaticConeAngleDegrees { get; }
        public TurretFirePattern AutomaticPattern { get; }
        #endregion

        #region Free Aim Fire
        public float FreeAimCadenceSeconds { get; }
        public int FreeAimProjectilesPerShot { get; }
        public float FreeAimInterProjectileDelay { get; }
        public float FreeAimConeAngleDegrees { get; }
        public TurretFirePattern FreeAimPattern { get; }
        #endregion

        #region Sustain
        public int MagazineSize { get; }
        public float ReloadSeconds { get; }
        public float MaxHeat { get; }
        public float HeatDissipationSeconds { get; }
        #endregion

        #region Miscellaneous
        public float ModeSwitchSeconds { get; }
        public int BuildCost { get; }
        public int UpkeepCost { get; }
        public float SalvageDelay { get; }
        public float RefundRatio { get; }
        public float FootprintRadius { get; }
        public float Clearance { get; }
        public float PlacementHeightOffset { get; }
        public bool AlignWithGrid { get; }
        #endregion
        #endregion

        #region Methods
        #region Constructors
        public TurretStatSnapshot(float health, float armor, float magicResistance, float passiveRegenPerSecond, float range, float turnRate, float deadZoneRadius, float retargetInterval, float automaticCadenceSeconds, int automaticProjectilesPerShot, float automaticInterProjectileDelay, float automaticConeAngleDegrees, TurretFirePattern automaticPattern, float freeAimCadenceSeconds, int freeAimProjectilesPerShot, float freeAimInterProjectileDelay, float freeAimConeAngleDegrees, TurretFirePattern freeAimPattern, int magazineSize, float reloadSeconds, float maxHeat, float heatDissipationSeconds, float modeSwitchSeconds, int buildCost, int upkeepCost, float salvageDelay, float refundRatio, float footprintRadius, float clearance, float placementHeightOffset, bool alignWithGrid)
        {
            Health = health;
            Armor = armor;
            MagicResistance = magicResistance;
            PassiveRegenPerSecond = passiveRegenPerSecond;
            Range = range;
            TurnRate = turnRate;
            DeadZoneRadius = deadZoneRadius;
            RetargetInterval = retargetInterval;
            AutomaticCadenceSeconds = automaticCadenceSeconds;
            AutomaticProjectilesPerShot = automaticProjectilesPerShot;
            AutomaticInterProjectileDelay = automaticInterProjectileDelay;
            AutomaticConeAngleDegrees = automaticConeAngleDegrees;
            AutomaticPattern = automaticPattern;
            FreeAimCadenceSeconds = freeAimCadenceSeconds;
            FreeAimProjectilesPerShot = freeAimProjectilesPerShot;
            FreeAimInterProjectileDelay = freeAimInterProjectileDelay;
            FreeAimConeAngleDegrees = freeAimConeAngleDegrees;
            FreeAimPattern = freeAimPattern;
            MagazineSize = magazineSize;
            ReloadSeconds = reloadSeconds;
            MaxHeat = maxHeat;
            HeatDissipationSeconds = heatDissipationSeconds;
            ModeSwitchSeconds = modeSwitchSeconds;
            BuildCost = buildCost;
            UpkeepCost = upkeepCost;
            SalvageDelay = salvageDelay;
            RefundRatio = refundRatio;
            FootprintRadius = footprintRadius;
            Clearance = clearance;
            PlacementHeightOffset = placementHeightOffset;
            AlignWithGrid = alignWithGrid;
        }
        #endregion

        #region Factory
        public static TurretStatSnapshot Create(TurretClassDefinition definition, bool applyFreeAimMultipliers)
        {
            if (definition == null)
                return default;

            TurretClassDefinition.FreeAimMultipliers multipliers = applyFreeAimMultipliers ? definition.FreeAimMultiplierSettings : TurretClassDefinition.FreeAimMultipliers.Identity;

            float health = ApplyFloatMultiplier(definition.Durability.Health, multipliers.Health, 1f);
            float armor = ApplyFloatMultiplier(definition.Durability.Armor, multipliers.Armor, 0f);
            float magicResistance = ApplyFloatMultiplier(definition.Durability.MagicResistance, multipliers.MagicResistance, 0f);
            float passiveRegenPerSecond = ApplyFloatMultiplier(definition.Durability.PassiveRegenPerSecond, multipliers.PassiveRegenPerSecond, 0f);

            float range = ApplyFloatMultiplier(definition.Targeting.Range, multipliers.Range, 0.5f);
            float turnRate = ApplyFloatMultiplier(definition.Targeting.TurnRate, multipliers.TurnRate, 0f);
            float deadZoneRadius = ApplyFloatMultiplier(definition.Targeting.DeadZoneRadius, multipliers.DeadZoneRadius, 0f);
            float retargetInterval = ApplyFloatMultiplier(definition.Targeting.RetargetInterval, multipliers.RetargetInterval, 0.05f);

            float automaticCadenceSeconds = ApplyFloatMultiplier(definition.AutomaticFire.CadenceSeconds, multipliers.AutomaticCadenceSeconds, 0.02f);
            int automaticProjectilesPerShot = ApplyIntMultiplier(definition.AutomaticFire.ProjectilesPerShot, multipliers.AutomaticProjectilesPerShot, 1);
            float automaticInterProjectileDelay = ApplyFloatMultiplier(definition.AutomaticFire.InterProjectileDelay, multipliers.AutomaticInterProjectileDelay, 0f);
            float automaticConeAngleDegrees = ApplyFloatMultiplier(definition.AutomaticFire.ConeAngleDegrees, multipliers.AutomaticConeAngleDegrees, 0f);

            float freeAimCadenceSeconds = ApplyFloatMultiplier(definition.FreeAimFire.CadenceSeconds, multipliers.FreeAimCadenceSeconds, 0.02f);
            int freeAimProjectilesPerShot = ApplyIntMultiplier(definition.FreeAimFire.ProjectilesPerShot, multipliers.FreeAimProjectilesPerShot, 1);
            float freeAimInterProjectileDelay = ApplyFloatMultiplier(definition.FreeAimFire.InterProjectileDelay, multipliers.FreeAimInterProjectileDelay, 0f);
            float freeAimConeAngleDegrees = ApplyFloatMultiplier(definition.FreeAimFire.ConeAngleDegrees, multipliers.FreeAimConeAngleDegrees, 0f);

            int magazineSize = ApplyIntMultiplier(definition.Sustain.MagazineSize, multipliers.MagazineSize, 1);
            float reloadSeconds = ApplyFloatMultiplier(definition.Sustain.ReloadSeconds, multipliers.ReloadSeconds, 0f);
            float maxHeat = ApplyFloatMultiplier(definition.Sustain.MaxHeat, multipliers.MaxHeat, 0f);
            float heatDissipationSeconds = ApplyFloatMultiplier(definition.Sustain.HeatDissipationSeconds, multipliers.HeatDissipationSeconds, 0.01f);

            float modeSwitchSeconds = ApplyFloatMultiplier(definition.ModeSwitchSeconds, multipliers.ModeSwitchSeconds, 0.01f);
            int buildCost = ApplyIntMultiplier(definition.Economy.BuildCost, multipliers.BuildCost, 0);
            int upkeepCost = ApplyIntMultiplier(definition.Economy.UpkeepCost, multipliers.UpkeepCost, 0);
            float salvageDelay = ApplyFloatMultiplier(definition.Economy.SalvageDelay, multipliers.SalvageDelay, 0f);
            float refundRatio = Mathf.Clamp01(definition.Economy.RefundRatio * multipliers.RefundRatio);
            float footprintRadius = ApplyFloatMultiplier(definition.Placement.FootprintRadius, multipliers.FootprintRadius, 0.05f);
            float clearance = ApplyFloatMultiplier(definition.Placement.Clearance, multipliers.Clearance, 0f);
            float placementHeightOffset = definition.Placement.HeightOffset * multipliers.PlacementHeightOffset;

            TurretStatSnapshot snapshot = new TurretStatSnapshot(health, armor, magicResistance, passiveRegenPerSecond, range, turnRate, deadZoneRadius, retargetInterval, automaticCadenceSeconds, automaticProjectilesPerShot, automaticInterProjectileDelay, automaticConeAngleDegrees, definition.AutomaticFire.Pattern, freeAimCadenceSeconds, freeAimProjectilesPerShot, freeAimInterProjectileDelay, freeAimConeAngleDegrees, definition.FreeAimFire.Pattern, magazineSize, reloadSeconds, maxHeat, heatDissipationSeconds, modeSwitchSeconds, buildCost, upkeepCost, salvageDelay, refundRatio, footprintRadius, clearance, placementHeightOffset, definition.Placement.AlignWithGrid);
            return snapshot;
        }
        #endregion

        #region Helpers
        private static float ApplyFloatMultiplier(float value, float multiplier, float minimum)
        {
            float scaled = value * multiplier;
            if (scaled < minimum)
                return minimum;

            return scaled;
        }

        private static int ApplyIntMultiplier(int value, float multiplier, int minimum)
        {
            float scaled = value * multiplier;
            int rounded = Mathf.RoundToInt(scaled);
            if (rounded < minimum)
                return minimum;

            return rounded;
        }
        #endregion
        #endregion
    }
}
