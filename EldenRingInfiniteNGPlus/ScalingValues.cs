namespace EldenRingInfiniteNGPlus;

internal static class ScalingValues
{
    /// <summary>
    /// Modify initial `multiplier` (an area- or boss-specific initial multiplier) according to NG+ level.
    /// </summary>
    /// <param name="multiplier"></param>
    /// <param name="fieldName"></param>
    /// <param name="ngPlusLevel"></param>
    /// <returns></returns>
    public static float CalculateStackedScaling(float multiplier, string fieldName, int ngPlusLevel)
    {
        if (ngPlusLevel == 0)
            return multiplier;  // internal NG+ will be set to zero, so we reset the param to vanilla 
        if (ngPlusLevel == 1)
            return multiplier;  // applied just once (will be vanilla field value)

        // For NG+2 to NG+7, we multiply the scaling value by another multiplier.
        // Note that this is NOT exponential growth.
        if (ngPlusLevel <= 7)
            return multiplier * DefaultAdditionalScaling[fieldName][ngPlusLevel - 2];

        // NG+8 to Infinity: we bump up the NG+7 scaling value by a fixed amount per NG+ level (field-dependent).
        float ngPlus7Scaling = DefaultAdditionalScaling[fieldName][5];  // for NG+7
        float additiveScaling = (ngPlusLevel - 7) * CustomAdditionalScaling[fieldName];
        return multiplier * (ngPlus7Scaling + additiveScaling);
    }

    /// <summary>
    /// Boss rewards are multiplied in NG+ by some internal function, which I have loosely copied into the
    /// `BossNgRuneScaling` dictionary. However, we don't even need that; since the game applies the initial NG+1
    /// scaling automatically, all we need to do is scale the base reward itself by the 'haveSoulRate'
    /// `DefaultAdditionalScaling` plus my `CustomAdditionalScaling` for the simulated NG+ level.
    /// </summary>
    /// <param name="ngPlusLevel"></param>
    /// <returns></returns>
    public static float CalculateAdditionalBossRewardScaling(int ngPlusLevel)
    {
        if (ngPlusLevel == 0)
            return 1f;  // don't modify `GameAreaParam` (internal NG+ level will be set to zero)
        if (ngPlusLevel == 1)
            return 1f;  // still don't modify `GameAreaParam` (internal NG+1 scaling is applied automatically)

        // For NG+2 to NG+7, we multiply the scaling value by another multiplier.
        // Note that this is NOT exponential growth.
        if (ngPlusLevel <= 7)
            return DefaultAdditionalScaling["haveSoulRate"][ngPlusLevel - 2];

        // NG+8 to Infinity: we bump up the NG+7 scaling value by a fixed amount per NG+ level.
        float ngPlus7Scaling = DefaultAdditionalScaling["haveSoulRate"][5];  // for NG+7
        float additiveScaling = (ngPlusLevel - 7) * CustomAdditionalScaling["haveSoulRate"];
        return ngPlus7Scaling + additiveScaling;
    }

    // Default NG+X area scaling multipliers (NOT exponents) for NG+2 to NG+7.
    static Dictionary<string, float[]> DefaultAdditionalScaling { get; } = new()
    {
        ["maxHpRate"] = [1.1f, 1.15f, 1.2f, 1.3f, 1.35f, 1.4f],
        ["maxStaminaRate"] = [1.1f, 1.15f, 1.2f, 1.3f, 1.35f, 1.4f],  // not given in online table; copied from HP
        ["haveSoulRate"] = [1.1f, 1.125f, 1.2f, 1.225f, 1.25f, 1.275f],
        ["physicsAttackPowerRate"] = [1.1f, 1.15f, 1.2f, 1.3f, 1.35f, 1.45f],
        ["magicAttackPowerRate"] = [1.1f, 1.15f, 1.2f, 1.3f, 1.35f, 1.45f],
        ["fireAttackPowerRate"] = [1.1f, 1.15f, 1.2f, 1.3f, 1.35f, 1.45f],
        ["thunderAttackPowerRate"] = [1.1f, 1.15f, 1.2f, 1.3f, 1.35f, 1.45f],
        ["physicsDiffenceRate"] = [1.025f, 1.05f, 1.1f, 1.15f, 1.2f, 1.3f],
        ["magicDiffenceRate"] = [1.025f, 1.05f, 1.1f, 1.15f, 1.2f, 1.3f],
        ["fireDiffenceRate"] = [1.025f, 1.05f, 1.1f, 1.15f, 1.2f, 1.3f],
        ["thunderDiffenceRate"] = [1.025f, 1.05f, 1.1f, 1.15f, 1.2f, 1.3f],
        ["staminaAttackRate"] = [1.4f, 1.5f, 1.6f, 1.7f, 1.8f, 1.9f],
        ["registPoizonChangeRate"] = [1.015f, 1.03f, 1.045f, 1.06f, 1.075f, 1.09f],
        ["registDiseaseChangeRate"] = [1.015f, 1.03f, 1.045f, 1.06f, 1.075f, 1.09f],
        ["registBloodChangeRate"] = [1.015f, 1.03f, 1.045f, 1.06f, 1.075f, 1.09f],
        ["darkDiffenceRate"] = [1.025f, 1.05f, 1.1f, 1.15f, 1.2f, 1.3f],
        ["darkAttackPowerRate"] = [1.015f, 1.03f, 1.045f, 1.06f, 1.075f, 1.09f],
        ["registFreezeChangeRate"] = [1.015f, 1.03f, 1.045f, 1.06f, 1.075f, 1.09f],
        ["registSleepChangeRate"] = [1.015f, 1.03f, 1.045f, 1.06f, 1.075f, 1.09f],
        ["registMadnessChangeRate"] = [1.015f, 1.03f, 1.045f, 1.06f, 1.075f, 1.09f],
    };

    /// <summary>
    /// Values added to above multipliers for each NG+ level beyond 7. Generally 5%, sometimes left.
    /// </summary>
    static Dictionary<string, float> CustomAdditionalScaling { get; } = new()
    {
        ["maxHpRate"] = 0.05f,
        ["maxStaminaRate"] = 0.05f,
        ["haveSoulRate"] = 0.025f,
        ["physicsAttackPowerRate"] = 0.05f,
        ["magicAttackPowerRate"] = 0.05f,
        ["fireAttackPowerRate"] = 0.05f,
        ["thunderAttackPowerRate"] = 0.05f,
        ["physicsDiffenceRate"] = 0.05f,
        ["magicDiffenceRate"] = 0.05f,
        ["fireDiffenceRate"] = 0.05f,
        ["thunderDiffenceRate"] = 0.05f,
        ["staminaAttackRate"] = 0.05f,
        ["registPoizonChangeRate"] = 0.015f,
        ["registDiseaseChangeRate"] = 0.015f,
        ["registBloodChangeRate"] = 0.015f,
        ["darkDiffenceRate"] = 0.05f,
        ["darkAttackPowerRate"] = 0.05f,
        ["registFreezeChangeRate"] = 0.015f,
        ["registSleepChangeRate"] = 0.015f,
        ["registMadnessChangeRate"] = 0.015f,
    };

    /// <summary>
    /// Vanilla values for 'area' scaling effects 7400-7600 (base game).
    /// </summary>
    public static Dictionary<int, Dictionary<string, float>> AreaInitialScaling { get; } = new()
    {
        [7400] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 3.434f,
            ["maxStaminaRate"] = 1.888f,
            ["haveSoulRate"] = 5f,
            ["physicsAttackPowerRate"] = 1.902f,
            ["magicAttackPowerRate"] = 1.902f,
            ["fireAttackPowerRate"] = 1.902f,
            ["thunderAttackPowerRate"] = 1.902f,
            ["physicsDiffenceRate"] = 1.172f,
            ["magicDiffenceRate"] = 1.172f,
            ["fireDiffenceRate"] = 1.172f,
            ["thunderDiffenceRate"] = 1.172f,
            ["staminaAttackRate"] = 1.2f,
            ["registPoizonChangeRate"] = 1.097f,
            ["registDiseaseChangeRate"] = 1.097f,
            ["registBloodChangeRate"] = 1.097f,
            ["darkDiffenceRate"] = 1.172f,
            ["darkAttackPowerRate"] = 1.902f,
            ["registFreezeChangeRate"] = 1.097f,
            ["registSleepChangeRate"] = 1.097f,
            ["registMadnessChangeRate"] = 1.097f,
        },
        [7410] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 3.122f,
            ["maxStaminaRate"] = 1.911f,
            ["haveSoulRate"] = 5f,
            ["physicsAttackPowerRate"] = 1.965f,
            ["magicAttackPowerRate"] = 1.965f,
            ["fireAttackPowerRate"] = 1.965f,
            ["thunderAttackPowerRate"] = 1.965f,
            ["physicsDiffenceRate"] = 1.173f,
            ["magicDiffenceRate"] = 1.173f,
            ["fireDiffenceRate"] = 1.173f,
            ["thunderDiffenceRate"] = 1.173f,
            ["staminaAttackRate"] = 1.191f,
            ["registPoizonChangeRate"] = 1.095f,
            ["registDiseaseChangeRate"] = 1.095f,
            ["registBloodChangeRate"] = 1.095f,
            ["darkDiffenceRate"] = 1.173f,
            ["darkAttackPowerRate"] = 1.965f,
            ["registFreezeChangeRate"] = 1.095f,
            ["registSleepChangeRate"] = 1.095f,
            ["registMadnessChangeRate"] = 1.095f,
        },
        [7420] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 2.857f,
            ["maxStaminaRate"] = 1.775f,
            ["haveSoulRate"] = 5f,
            ["physicsAttackPowerRate"] = 1.768f,
            ["magicAttackPowerRate"] = 1.768f,
            ["fireAttackPowerRate"] = 1.768f,
            ["thunderAttackPowerRate"] = 1.768f,
            ["physicsDiffenceRate"] = 1.174f,
            ["magicDiffenceRate"] = 1.174f,
            ["fireDiffenceRate"] = 1.174f,
            ["thunderDiffenceRate"] = 1.174f,
            ["staminaAttackRate"] = 1.182f,
            ["registPoizonChangeRate"] = 1.093f,
            ["registDiseaseChangeRate"] = 1.093f,
            ["registBloodChangeRate"] = 1.093f,
            ["darkDiffenceRate"] = 1.174f,
            ["darkAttackPowerRate"] = 1.768f,
            ["registFreezeChangeRate"] = 1.093f,
            ["registSleepChangeRate"] = 1.093f,
            ["registMadnessChangeRate"] = 1.093f,
        },
        [7430] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 2.355f,
            ["maxStaminaRate"] = 1.795f,
            ["haveSoulRate"] = 5f,
            ["physicsAttackPowerRate"] = 1.68f,
            ["magicAttackPowerRate"] = 1.68f,
            ["fireAttackPowerRate"] = 1.68f,
            ["thunderAttackPowerRate"] = 1.68f,
            ["physicsDiffenceRate"] = 1.175f,
            ["magicDiffenceRate"] = 1.175f,
            ["fireDiffenceRate"] = 1.175f,
            ["thunderDiffenceRate"] = 1.175f,
            ["staminaAttackRate"] = 1.173f,
            ["registPoizonChangeRate"] = 1.091f,
            ["registDiseaseChangeRate"] = 1.091f,
            ["registBloodChangeRate"] = 1.091f,
            ["darkDiffenceRate"] = 1.175f,
            ["darkAttackPowerRate"] = 1.68f,
            ["registFreezeChangeRate"] = 1.091f,
            ["registSleepChangeRate"] = 1.091f,
            ["registMadnessChangeRate"] = 1.091f,
        },
        [7440] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 2.146f,
            ["maxStaminaRate"] = 1.648f,
            ["haveSoulRate"] = 4f,
            ["physicsAttackPowerRate"] = 1.609f,
            ["magicAttackPowerRate"] = 1.609f,
            ["fireAttackPowerRate"] = 1.609f,
            ["thunderAttackPowerRate"] = 1.609f,
            ["physicsDiffenceRate"] = 1.176f,
            ["magicDiffenceRate"] = 1.176f,
            ["fireDiffenceRate"] = 1.176f,
            ["thunderDiffenceRate"] = 1.176f,
            ["staminaAttackRate"] = 1.164f,
            ["registPoizonChangeRate"] = 1.089f,
            ["registDiseaseChangeRate"] = 1.089f,
            ["registBloodChangeRate"] = 1.089f,
            ["darkDiffenceRate"] = 1.176f,
            ["darkAttackPowerRate"] = 1.609f,
            ["registFreezeChangeRate"] = 1.089f,
            ["registSleepChangeRate"] = 1.089f,
            ["registMadnessChangeRate"] = 1.089f,
        },
        [7450] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 2.099f,
            ["maxStaminaRate"] = 1.666f,
            ["haveSoulRate"] = 4f,
            ["physicsAttackPowerRate"] = 1.483f,
            ["magicAttackPowerRate"] = 1.483f,
            ["fireAttackPowerRate"] = 1.483f,
            ["thunderAttackPowerRate"] = 1.483f,
            ["physicsDiffenceRate"] = 1.177f,
            ["magicDiffenceRate"] = 1.177f,
            ["fireDiffenceRate"] = 1.177f,
            ["thunderDiffenceRate"] = 1.177f,
            ["staminaAttackRate"] = 1.155f,
            ["registPoizonChangeRate"] = 1.087f,
            ["registDiseaseChangeRate"] = 1.087f,
            ["registBloodChangeRate"] = 1.087f,
            ["darkDiffenceRate"] = 1.177f,
            ["darkAttackPowerRate"] = 1.483f,
            ["registFreezeChangeRate"] = 1.087f,
            ["registSleepChangeRate"] = 1.087f,
            ["registMadnessChangeRate"] = 1.087f,
        },
        [7460] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.845f,
            ["maxStaminaRate"] = 1.551f,
            ["haveSoulRate"] = 4f,
            ["physicsAttackPowerRate"] = 1.4f,
            ["magicAttackPowerRate"] = 1.4f,
            ["fireAttackPowerRate"] = 1.4f,
            ["thunderAttackPowerRate"] = 1.4f,
            ["physicsDiffenceRate"] = 1.178f,
            ["magicDiffenceRate"] = 1.178f,
            ["fireDiffenceRate"] = 1.178f,
            ["thunderDiffenceRate"] = 1.178f,
            ["staminaAttackRate"] = 1.146f,
            ["registPoizonChangeRate"] = 1.085f,
            ["registDiseaseChangeRate"] = 1.085f,
            ["registBloodChangeRate"] = 1.085f,
            ["darkDiffenceRate"] = 1.178f,
            ["darkAttackPowerRate"] = 1.4f,
            ["registFreezeChangeRate"] = 1.085f,
            ["registSleepChangeRate"] = 1.085f,
            ["registMadnessChangeRate"] = 1.085f,
        },
        [7470] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.523f,
            ["maxStaminaRate"] = 1.551f,
            ["haveSoulRate"] = 3f,
            ["physicsAttackPowerRate"] = 1.314f,
            ["magicAttackPowerRate"] = 1.314f,
            ["fireAttackPowerRate"] = 1.314f,
            ["thunderAttackPowerRate"] = 1.314f,
            ["physicsDiffenceRate"] = 1.179f,
            ["magicDiffenceRate"] = 1.179f,
            ["fireDiffenceRate"] = 1.179f,
            ["thunderDiffenceRate"] = 1.179f,
            ["staminaAttackRate"] = 1.137f,
            ["registPoizonChangeRate"] = 1.083f,
            ["registDiseaseChangeRate"] = 1.083f,
            ["registBloodChangeRate"] = 1.083f,
            ["darkDiffenceRate"] = 1.179f,
            ["darkAttackPowerRate"] = 1.314f,
            ["registFreezeChangeRate"] = 1.083f,
            ["registSleepChangeRate"] = 1.083f,
            ["registMadnessChangeRate"] = 1.083f,
        },
        [7480] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.479f,
            ["maxStaminaRate"] = 1.428f,
            ["haveSoulRate"] = 3f,
            ["physicsAttackPowerRate"] = 1.239f,
            ["magicAttackPowerRate"] = 1.239f,
            ["fireAttackPowerRate"] = 1.239f,
            ["thunderAttackPowerRate"] = 1.239f,
            ["physicsDiffenceRate"] = 1.18f,
            ["magicDiffenceRate"] = 1.18f,
            ["fireDiffenceRate"] = 1.18f,
            ["thunderDiffenceRate"] = 1.18f,
            ["staminaAttackRate"] = 1.128f,
            ["registPoizonChangeRate"] = 1.081f,
            ["registDiseaseChangeRate"] = 1.081f,
            ["registBloodChangeRate"] = 1.081f,
            ["darkDiffenceRate"] = 1.18f,
            ["darkAttackPowerRate"] = 1.239f,
            ["registFreezeChangeRate"] = 1.081f,
            ["registSleepChangeRate"] = 1.081f,
            ["registMadnessChangeRate"] = 1.081f,
        },
        [7490] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.461f,
            ["maxStaminaRate"] = 1.343f,
            ["haveSoulRate"] = 3f,
            ["physicsAttackPowerRate"] = 1.2f,
            ["magicAttackPowerRate"] = 1.2f,
            ["fireAttackPowerRate"] = 1.2f,
            ["thunderAttackPowerRate"] = 1.2f,
            ["physicsDiffenceRate"] = 1.181f,
            ["magicDiffenceRate"] = 1.181f,
            ["fireDiffenceRate"] = 1.181f,
            ["thunderDiffenceRate"] = 1.181f,
            ["staminaAttackRate"] = 1.119f,
            ["registPoizonChangeRate"] = 1.079f,
            ["registDiseaseChangeRate"] = 1.079f,
            ["registBloodChangeRate"] = 1.079f,
            ["darkDiffenceRate"] = 1.181f,
            ["darkAttackPowerRate"] = 1.2f,
            ["registFreezeChangeRate"] = 1.079f,
            ["registSleepChangeRate"] = 1.079f,
            ["registMadnessChangeRate"] = 1.079f,
        },
        [7500] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.296f,
            ["maxStaminaRate"] = 1.343f,
            ["haveSoulRate"] = 3f,
            ["physicsAttackPowerRate"] = 1.15f,
            ["magicAttackPowerRate"] = 1.15f,
            ["fireAttackPowerRate"] = 1.15f,
            ["thunderAttackPowerRate"] = 1.15f,
            ["physicsDiffenceRate"] = 1.182f,
            ["magicDiffenceRate"] = 1.182f,
            ["fireDiffenceRate"] = 1.182f,
            ["thunderDiffenceRate"] = 1.182f,
            ["staminaAttackRate"] = 1.11f,
            ["registPoizonChangeRate"] = 1.077f,
            ["registDiseaseChangeRate"] = 1.077f,
            ["registBloodChangeRate"] = 1.077f,
            ["darkDiffenceRate"] = 1.182f,
            ["darkAttackPowerRate"] = 1.15f,
            ["registFreezeChangeRate"] = 1.077f,
            ["registSleepChangeRate"] = 1.077f,
            ["registMadnessChangeRate"] = 1.077f,
        },
        [7510] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.253f,
            ["maxStaminaRate"] = 1.25f,
            ["haveSoulRate"] = 3f,
            ["physicsAttackPowerRate"] = 1.141f,
            ["magicAttackPowerRate"] = 1.141f,
            ["fireAttackPowerRate"] = 1.141f,
            ["thunderAttackPowerRate"] = 1.141f,
            ["physicsDiffenceRate"] = 1.183f,
            ["magicDiffenceRate"] = 1.183f,
            ["fireDiffenceRate"] = 1.183f,
            ["thunderDiffenceRate"] = 1.183f,
            ["staminaAttackRate"] = 1.101f,
            ["registPoizonChangeRate"] = 1.075f,
            ["registDiseaseChangeRate"] = 1.075f,
            ["registBloodChangeRate"] = 1.075f,
            ["darkDiffenceRate"] = 1.183f,
            ["darkAttackPowerRate"] = 1.141f,
            ["registFreezeChangeRate"] = 1.075f,
            ["registSleepChangeRate"] = 1.075f,
            ["registMadnessChangeRate"] = 1.075f,
        },
        [7520] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.264f,
            ["maxStaminaRate"] = 1.25f,
            ["haveSoulRate"] = 2f,
            ["physicsAttackPowerRate"] = 1.135f,
            ["magicAttackPowerRate"] = 1.135f,
            ["fireAttackPowerRate"] = 1.135f,
            ["thunderAttackPowerRate"] = 1.135f,
            ["physicsDiffenceRate"] = 1.184f,
            ["magicDiffenceRate"] = 1.184f,
            ["fireDiffenceRate"] = 1.184f,
            ["thunderDiffenceRate"] = 1.184f,
            ["staminaAttackRate"] = 1.092f,
            ["registPoizonChangeRate"] = 1.073f,
            ["registDiseaseChangeRate"] = 1.073f,
            ["registBloodChangeRate"] = 1.073f,
            ["darkDiffenceRate"] = 1.184f,
            ["darkAttackPowerRate"] = 1.135f,
            ["registFreezeChangeRate"] = 1.073f,
            ["registSleepChangeRate"] = 1.073f,
            ["registMadnessChangeRate"] = 1.073f,
        },
        [7530] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.191f,
            ["maxStaminaRate"] = 1.184f,
            ["haveSoulRate"] = 2f,
            ["physicsAttackPowerRate"] = 1.134f,
            ["magicAttackPowerRate"] = 1.134f,
            ["fireAttackPowerRate"] = 1.134f,
            ["thunderAttackPowerRate"] = 1.134f,
            ["physicsDiffenceRate"] = 1.185f,
            ["magicDiffenceRate"] = 1.185f,
            ["fireDiffenceRate"] = 1.185f,
            ["thunderDiffenceRate"] = 1.185f,
            ["staminaAttackRate"] = 1.083f,
            ["registPoizonChangeRate"] = 1.071f,
            ["registDiseaseChangeRate"] = 1.071f,
            ["registBloodChangeRate"] = 1.071f,
            ["darkDiffenceRate"] = 1.185f,
            ["darkAttackPowerRate"] = 1.134f,
            ["registFreezeChangeRate"] = 1.071f,
            ["registSleepChangeRate"] = 1.071f,
            ["registMadnessChangeRate"] = 1.071f,
        },
        [7540] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.008f,
            ["maxStaminaRate"] = 1.111f,
            ["haveSoulRate"] = 2f,
            ["physicsAttackPowerRate"] = 1.122f,
            ["magicAttackPowerRate"] = 1.122f,
            ["fireAttackPowerRate"] = 1.122f,
            ["thunderAttackPowerRate"] = 1.122f,
            ["physicsDiffenceRate"] = 1.186f,
            ["magicDiffenceRate"] = 1.186f,
            ["fireDiffenceRate"] = 1.186f,
            ["thunderDiffenceRate"] = 1.186f,
            ["staminaAttackRate"] = 1.074f,
            ["registPoizonChangeRate"] = 1.069f,
            ["registDiseaseChangeRate"] = 1.069f,
            ["registBloodChangeRate"] = 1.069f,
            ["darkDiffenceRate"] = 1.186f,
            ["darkAttackPowerRate"] = 1.122f,
            ["registFreezeChangeRate"] = 1.069f,
            ["registSleepChangeRate"] = 1.069f,
            ["registMadnessChangeRate"] = 1.069f,
        },
        [7550] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.001f,
            ["maxStaminaRate"] = 1.058f,
            ["haveSoulRate"] = 2f,
            ["physicsAttackPowerRate"] = 1.094f,
            ["magicAttackPowerRate"] = 1.094f,
            ["fireAttackPowerRate"] = 1.094f,
            ["thunderAttackPowerRate"] = 1.094f,
            ["physicsDiffenceRate"] = 1.2f,
            ["magicDiffenceRate"] = 1.2f,
            ["fireDiffenceRate"] = 1.2f,
            ["thunderDiffenceRate"] = 1.2f,
            ["staminaAttackRate"] = 1.065f,
            ["registPoizonChangeRate"] = 1.07f,
            ["registDiseaseChangeRate"] = 1.07f,
            ["registBloodChangeRate"] = 1.07f,
            ["darkDiffenceRate"] = 1.2f,
            ["darkAttackPowerRate"] = 1.094f,
            ["registFreezeChangeRate"] = 1.07f,
            ["registSleepChangeRate"] = 1.07f,
            ["registMadnessChangeRate"] = 1.07f,
        },
        [7560] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.015f,
            ["maxStaminaRate"] = 1.058f,
            ["haveSoulRate"] = 2f,
            ["physicsAttackPowerRate"] = 1.134f,
            ["magicAttackPowerRate"] = 1.134f,
            ["fireAttackPowerRate"] = 1.134f,
            ["thunderAttackPowerRate"] = 1.134f,
            ["physicsDiffenceRate"] = 1.2f,
            ["magicDiffenceRate"] = 1.2f,
            ["fireDiffenceRate"] = 1.2f,
            ["thunderDiffenceRate"] = 1.2f,
            ["staminaAttackRate"] = 1.07f,
            ["registPoizonChangeRate"] = 1.07f,
            ["registDiseaseChangeRate"] = 1.07f,
            ["registBloodChangeRate"] = 1.07f,
            ["darkDiffenceRate"] = 1.2f,
            ["darkAttackPowerRate"] = 1.134f,
            ["registFreezeChangeRate"] = 1.07f,
            ["registSleepChangeRate"] = 1.07f,
            ["registMadnessChangeRate"] = 1.07f,
        },
        [7570] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.001f,
            ["maxStaminaRate"] = 1.058f,
            ["haveSoulRate"] = 2f,
            ["physicsAttackPowerRate"] = 1.094f,
            ["magicAttackPowerRate"] = 1.094f,
            ["fireAttackPowerRate"] = 1.094f,
            ["thunderAttackPowerRate"] = 1.094f,
            ["physicsDiffenceRate"] = 1.2f,
            ["magicDiffenceRate"] = 1.2f,
            ["fireDiffenceRate"] = 1.2f,
            ["thunderDiffenceRate"] = 1.2f,
            ["staminaAttackRate"] = 1.065f,
            ["registPoizonChangeRate"] = 1.07f,
            ["registDiseaseChangeRate"] = 1.07f,
            ["registBloodChangeRate"] = 1.07f,
            ["darkDiffenceRate"] = 1.2f,
            ["darkAttackPowerRate"] = 1.094f,
            ["registFreezeChangeRate"] = 1.07f,
            ["registSleepChangeRate"] = 1.07f,
            ["registMadnessChangeRate"] = 1.07f,
        },
        [7580] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.015f,
            ["maxStaminaRate"] = 1.058f,
            ["haveSoulRate"] = 2f,
            ["physicsAttackPowerRate"] = 1.134f,
            ["magicAttackPowerRate"] = 1.134f,
            ["fireAttackPowerRate"] = 1.134f,
            ["thunderAttackPowerRate"] = 1.134f,
            ["physicsDiffenceRate"] = 1.2f,
            ["magicDiffenceRate"] = 1.2f,
            ["fireDiffenceRate"] = 1.2f,
            ["thunderDiffenceRate"] = 1.2f,
            ["staminaAttackRate"] = 1.07f,
            ["registPoizonChangeRate"] = 1f,
            ["registDiseaseChangeRate"] = 1f,
            ["registBloodChangeRate"] = 1f,
            ["darkDiffenceRate"] = 1.2f,
            ["darkAttackPowerRate"] = 1.134f,
            ["registFreezeChangeRate"] = 1f,
            ["registSleepChangeRate"] = 1f,
            ["registMadnessChangeRate"] = 1f,
        },
        [7590] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.015f,
            ["maxStaminaRate"] = 1.058f,
            ["haveSoulRate"] = 2f,
            ["physicsAttackPowerRate"] = 1.134f,
            ["magicAttackPowerRate"] = 1.134f,
            ["fireAttackPowerRate"] = 1.134f,
            ["thunderAttackPowerRate"] = 1.134f,
            ["physicsDiffenceRate"] = 1.2f,
            ["magicDiffenceRate"] = 1.2f,
            ["fireDiffenceRate"] = 1.2f,
            ["thunderDiffenceRate"] = 1.2f,
            ["staminaAttackRate"] = 1.07f,
            ["registPoizonChangeRate"] = 1f,
            ["registDiseaseChangeRate"] = 1f,
            ["registBloodChangeRate"] = 1f,
            ["darkDiffenceRate"] = 1.2f,
            ["darkAttackPowerRate"] = 1.134f,
            ["registFreezeChangeRate"] = 1f,
            ["registSleepChangeRate"] = 1f,
            ["registMadnessChangeRate"] = 1f,
        },
        [7600] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.015f,
            ["maxStaminaRate"] = 1.058f,
            ["haveSoulRate"] = 2f,
            ["physicsAttackPowerRate"] = 1.134f,
            ["magicAttackPowerRate"] = 1.134f,
            ["fireAttackPowerRate"] = 1.134f,
            ["thunderAttackPowerRate"] = 1.134f,
            ["physicsDiffenceRate"] = 1.2f,
            ["magicDiffenceRate"] = 1.2f,
            ["fireDiffenceRate"] = 1.2f,
            ["thunderDiffenceRate"] = 1.2f,
            ["staminaAttackRate"] = 1.07f,
            ["registPoizonChangeRate"] = 1f,
            ["registDiseaseChangeRate"] = 1f,
            ["registBloodChangeRate"] = 1f,
            ["darkDiffenceRate"] = 1.2f,
            ["darkAttackPowerRate"] = 1.134f,
            ["registFreezeChangeRate"] = 1f,
            ["registSleepChangeRate"] = 1f,
            ["registMadnessChangeRate"] = 1f,
        },
    };

    /// <summary>
    /// Vanilla values for 'area' scaling effects 7400-7600 (base game).
    /// </summary>
    public static Dictionary<int, Dictionary<string, float>> DLCAreaInitialScaling { get; } = new()
    {
        
        [20007400] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0981221f,
            ["maxStaminaRate"] = 1.065234f,
            ["physicsAttackPowerRate"] = 1.0237443f,
            ["magicAttackPowerRate"] = 1.0237443f,
            ["fireAttackPowerRate"] = 1.0237443f,
            ["thunderAttackPowerRate"] = 1.0237443f,
            ["physicsDiffenceRate"] = 1.0276664f,
            ["magicDiffenceRate"] = 1.0276664f,
            ["fireDiffenceRate"] = 1.0276664f,
            ["thunderDiffenceRate"] = 1.0276664f,
            ["staminaAttackRate"] = 1.0252846f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.18f,
            ["registDiseaseChangeRate"] = 1.18f,
            ["registBloodChangeRate"] = 1.18f,
            ["darkDiffenceRate"] = 1.0276664f,
            ["darkAttackPowerRate"] = 1.0237443f,
            ["registFreezeChangeRate"] = 1.18f,
            ["registSleepChangeRate"] = 1.18f,
            ["registMadnessChangeRate"] = 1.18f,
        },
        [20007410] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0839258f,
            ["maxStaminaRate"] = 1.0626585f,
            ["physicsAttackPowerRate"] = 1.0225012f,
            ["magicAttackPowerRate"] = 1.0225012f,
            ["fireAttackPowerRate"] = 1.0225012f,
            ["thunderAttackPowerRate"] = 1.0225012f,
            ["physicsDiffenceRate"] = 1.0268171f,
            ["magicDiffenceRate"] = 1.0268171f,
            ["fireDiffenceRate"] = 1.0268171f,
            ["thunderDiffenceRate"] = 1.0268171f,
            ["staminaAttackRate"] = 1.0229326f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.166474f,
            ["registDiseaseChangeRate"] = 1.166474f,
            ["registBloodChangeRate"] = 1.166474f,
            ["darkDiffenceRate"] = 1.0268171f,
            ["darkAttackPowerRate"] = 1.0225012f,
            ["registFreezeChangeRate"] = 1.166474f,
            ["registSleepChangeRate"] = 1.166474f,
            ["registMadnessChangeRate"] = 1.166474f,
        },
        [20007420] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0779487f,
            ["maxStaminaRate"] = 1.0606076f,
            ["physicsAttackPowerRate"] = 1.021676f,
            ["magicAttackPowerRate"] = 1.021676f,
            ["fireAttackPowerRate"] = 1.021676f,
            ["thunderAttackPowerRate"] = 1.021676f,
            ["physicsDiffenceRate"] = 1.0263934f,
            ["magicDiffenceRate"] = 1.0263934f,
            ["fireDiffenceRate"] = 1.0263934f,
            ["thunderDiffenceRate"] = 1.0263934f,
            ["staminaAttackRate"] = 1.0213766f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.1577142f,
            ["registDiseaseChangeRate"] = 1.1577142f,
            ["registBloodChangeRate"] = 1.1577142f,
            ["darkDiffenceRate"] = 1.0263934f,
            ["darkAttackPowerRate"] = 1.021676f,
            ["registFreezeChangeRate"] = 1.1577142f,
            ["registSleepChangeRate"] = 1.1577142f,
            ["registMadnessChangeRate"] = 1.1577142f,
        },
        [20007430] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0781562f,
            ["maxStaminaRate"] = 1.0534942f,
            ["physicsAttackPowerRate"] = 1.0204428f,
            ["magicAttackPowerRate"] = 1.0204428f,
            ["fireAttackPowerRate"] = 1.0204428f,
            ["thunderAttackPowerRate"] = 1.0204428f,
            ["physicsDiffenceRate"] = 1.0251267f,
            ["magicDiffenceRate"] = 1.0251267f,
            ["fireDiffenceRate"] = 1.0251267f,
            ["thunderDiffenceRate"] = 1.0251267f,
            ["staminaAttackRate"] = 1.01906f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.1449438f,
            ["registDiseaseChangeRate"] = 1.1449438f,
            ["registBloodChangeRate"] = 1.1449438f,
            ["darkDiffenceRate"] = 1.0251267f,
            ["darkAttackPowerRate"] = 1.0204428f,
            ["registFreezeChangeRate"] = 1.1449438f,
            ["registSleepChangeRate"] = 1.1449438f,
            ["registMadnessChangeRate"] = 1.1449438f,
        },
        [20007440] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0782156f,
            ["maxStaminaRate"] = 1.0464803f,
            ["physicsAttackPowerRate"] = 1.0200331f,
            ["magicAttackPowerRate"] = 1.0200331f,
            ["fireAttackPowerRate"] = 1.0200331f,
            ["thunderAttackPowerRate"] = 1.0200331f,
            ["physicsDiffenceRate"] = 1.0238662f,
            ["magicDiffenceRate"] = 1.0238662f,
            ["fireDiffenceRate"] = 1.0238662f,
            ["thunderDiffenceRate"] = 1.0238662f,
            ["staminaAttackRate"] = 1.0182925f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.1407821f,
            ["registDiseaseChangeRate"] = 1.1407821f,
            ["registBloodChangeRate"] = 1.1407821f,
            ["darkDiffenceRate"] = 1.0238662f,
            ["darkAttackPowerRate"] = 1.0200331f,
            ["registFreezeChangeRate"] = 1.1407821f,
            ["registSleepChangeRate"] = 1.1407821f,
            ["registMadnessChangeRate"] = 1.1407821f,
        },
        [20007450] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0768288f,
            ["maxStaminaRate"] = 1.0376052f,
            ["physicsAttackPowerRate"] = 1.0196241f,
            ["magicAttackPowerRate"] = 1.0196241f,
            ["fireAttackPowerRate"] = 1.0196241f,
            ["thunderAttackPowerRate"] = 1.0196241f,
            ["physicsDiffenceRate"] = 1.0221951f,
            ["magicDiffenceRate"] = 1.0221951f,
            ["fireDiffenceRate"] = 1.0221951f,
            ["thunderDiffenceRate"] = 1.0221951f,
            ["staminaAttackRate"] = 1.0175273f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.1366667f,
            ["registDiseaseChangeRate"] = 1.1366667f,
            ["registBloodChangeRate"] = 1.1366667f,
            ["darkDiffenceRate"] = 1.0221951f,
            ["darkAttackPowerRate"] = 1.0196241f,
            ["registFreezeChangeRate"] = 1.1366667f,
            ["registSleepChangeRate"] = 1.1366667f,
            ["registMadnessChangeRate"] = 1.1366667f,
        },
        [20007460] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0770154f,
            ["maxStaminaRate"] = 1.0337112f,
            ["physicsAttackPowerRate"] = 1.0179944f,
            ["magicAttackPowerRate"] = 1.0179944f,
            ["fireAttackPowerRate"] = 1.0179944f,
            ["thunderAttackPowerRate"] = 1.0179944f,
            ["physicsDiffenceRate"] = 1.0209489f,
            ["magicDiffenceRate"] = 1.0209489f,
            ["fireDiffenceRate"] = 1.0209489f,
            ["thunderDiffenceRate"] = 1.0209489f,
            ["staminaAttackRate"] = 1.0167644f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.1206522f,
            ["registDiseaseChangeRate"] = 1.1206522f,
            ["registBloodChangeRate"] = 1.1206522f,
            ["darkDiffenceRate"] = 1.0209489f,
            ["darkAttackPowerRate"] = 1.0179944f,
            ["registFreezeChangeRate"] = 1.1206522f,
            ["registSleepChangeRate"] = 1.1206522f,
            ["registMadnessChangeRate"] = 1.1206522f,
        },
        [20007470] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0771002f,
            ["maxStaminaRate"] = 1.0298479f,
            ["physicsAttackPowerRate"] = 1.0171835f,
            ["magicAttackPowerRate"] = 1.0171835f,
            ["fireAttackPowerRate"] = 1.0171835f,
            ["thunderAttackPowerRate"] = 1.0171835f,
            ["physicsDiffenceRate"] = 1.0197088f,
            ["magicDiffenceRate"] = 1.0197088f,
            ["fireDiffenceRate"] = 1.0197088f,
            ["thunderDiffenceRate"] = 1.0197088f,
            ["staminaAttackRate"] = 1.0160037f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.1129032f,
            ["registDiseaseChangeRate"] = 1.1129032f,
            ["registBloodChangeRate"] = 1.1129032f,
            ["darkDiffenceRate"] = 1.0197088f,
            ["darkAttackPowerRate"] = 1.0171835f,
            ["registFreezeChangeRate"] = 1.1129032f,
            ["registSleepChangeRate"] = 1.1129032f,
            ["registMadnessChangeRate"] = 1.1129032f,
        },
        [20007480] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0693364f,
            ["maxStaminaRate"] = 1.0260147f,
            ["physicsAttackPowerRate"] = 1.0163752f,
            ["magicAttackPowerRate"] = 1.0163752f,
            ["fireAttackPowerRate"] = 1.0163752f,
            ["thunderAttackPowerRate"] = 1.0163752f,
            ["physicsDiffenceRate"] = 1.0184746f,
            ["magicDiffenceRate"] = 1.0184746f,
            ["fireDiffenceRate"] = 1.0184746f,
            ["thunderDiffenceRate"] = 1.0184746f,
            ["staminaAttackRate"] = 1.0152454f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.1053191f,
            ["registDiseaseChangeRate"] = 1.1053191f,
            ["registBloodChangeRate"] = 1.1053191f,
            ["darkDiffenceRate"] = 1.0184746f,
            ["darkAttackPowerRate"] = 1.0163752f,
            ["registFreezeChangeRate"] = 1.1053191f,
            ["registSleepChangeRate"] = 1.1053191f,
            ["registMadnessChangeRate"] = 1.1053191f,
        },
        [20007490] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0541972f,
            ["maxStaminaRate"] = 1.0222114f,
            ["physicsAttackPowerRate"] = 1.0147662f,
            ["magicAttackPowerRate"] = 1.0147662f,
            ["fireAttackPowerRate"] = 1.0147662f,
            ["thunderAttackPowerRate"] = 1.0147662f,
            ["physicsDiffenceRate"] = 1.0172464f,
            ["magicDiffenceRate"] = 1.0172464f,
            ["fireDiffenceRate"] = 1.0172464f,
            ["thunderDiffenceRate"] = 1.0172464f,
            ["staminaAttackRate"] = 1.0144893f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.090625f,
            ["registDiseaseChangeRate"] = 1.090625f,
            ["registBloodChangeRate"] = 1.090625f,
            ["darkDiffenceRate"] = 1.0172464f,
            ["darkAttackPowerRate"] = 1.0147662f,
            ["registFreezeChangeRate"] = 1.090625f,
            ["registSleepChangeRate"] = 1.090625f,
            ["registMadnessChangeRate"] = 1.090625f,
        },
        [20007500] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0456709f,
            ["maxStaminaRate"] = 1.0184376f,
            ["physicsAttackPowerRate"] = 1.0139655f,
            ["magicAttackPowerRate"] = 1.0139655f,
            ["fireAttackPowerRate"] = 1.0139655f,
            ["thunderAttackPowerRate"] = 1.0139655f,
            ["physicsDiffenceRate"] = 1.0160241f,
            ["magicDiffenceRate"] = 1.0160241f,
            ["fireDiffenceRate"] = 1.0160241f,
            ["thunderDiffenceRate"] = 1.0160241f,
            ["staminaAttackRate"] = 1.0137355f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.0835052f,
            ["registDiseaseChangeRate"] = 1.0835052f,
            ["registBloodChangeRate"] = 1.0835052f,
            ["darkDiffenceRate"] = 1.0160241f,
            ["darkAttackPowerRate"] = 1.0139655f,
            ["registFreezeChangeRate"] = 1.0835052f,
            ["registSleepChangeRate"] = 1.0835052f,
            ["registMadnessChangeRate"] = 1.0835052f,
        },
        [20007510] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0388143f,
            ["maxStaminaRate"] = 1.0146931f,
            ["physicsAttackPowerRate"] = 1.0131674f,
            ["magicAttackPowerRate"] = 1.0131674f,
            ["fireAttackPowerRate"] = 1.0131674f,
            ["thunderAttackPowerRate"] = 1.0131674f,
            ["physicsDiffenceRate"] = 1.0148077f,
            ["magicDiffenceRate"] = 1.0148077f,
            ["fireDiffenceRate"] = 1.0148077f,
            ["thunderDiffenceRate"] = 1.0148077f,
            ["staminaAttackRate"] = 1.0129839f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.0765306f,
            ["registDiseaseChangeRate"] = 1.0765306f,
            ["registBloodChangeRate"] = 1.0765306f,
            ["darkDiffenceRate"] = 1.0148077f,
            ["darkAttackPowerRate"] = 1.0131674f,
            ["registFreezeChangeRate"] = 1.0765306f,
            ["registSleepChangeRate"] = 1.0765306f,
            ["registMadnessChangeRate"] = 1.0765306f,
        },
        [20007520] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0313383f,
            ["maxStaminaRate"] = 1.0109774f,
            ["physicsAttackPowerRate"] = 1.0123718f,
            ["magicAttackPowerRate"] = 1.0123718f,
            ["fireAttackPowerRate"] = 1.0123718f,
            ["thunderAttackPowerRate"] = 1.0123718f,
            ["physicsDiffenceRate"] = 1.0135971f,
            ["magicDiffenceRate"] = 1.0135971f,
            ["fireDiffenceRate"] = 1.0135971f,
            ["thunderDiffenceRate"] = 1.0135971f,
            ["staminaAttackRate"] = 1.0122347f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.069697f,
            ["registDiseaseChangeRate"] = 1.069697f,
            ["registBloodChangeRate"] = 1.069697f,
            ["darkDiffenceRate"] = 1.0135971f,
            ["darkAttackPowerRate"] = 1.0123718f,
            ["registFreezeChangeRate"] = 1.069697f,
            ["registSleepChangeRate"] = 1.069697f,
            ["registMadnessChangeRate"] = 1.069697f,
        },
        [20007530] = new Dictionary<string, float>
        {
            ["maxHpRate"] = 1.0206945f,
            ["maxStaminaRate"] = 1.0072901f,
            ["physicsAttackPowerRate"] = 1.011183f,
            ["magicAttackPowerRate"] = 1.011183f,
            ["fireAttackPowerRate"] = 1.011183f,
            ["thunderAttackPowerRate"] = 1.011183f,
            ["physicsDiffenceRate"] = 1.0123924f,
            ["magicDiffenceRate"] = 1.0123924f,
            ["fireDiffenceRate"] = 1.0123924f,
            ["thunderDiffenceRate"] = 1.0123924f,
            ["staminaAttackRate"] = 1.0114875f,
            ["haveSoulRate"] = 2f,
            ["registPoizonChangeRate"] = 1.0597014f,
            ["registDiseaseChangeRate"] = 1.0597014f,
            ["registBloodChangeRate"] = 1.0597014f,
            ["darkDiffenceRate"] = 1.0123924f,
            ["darkAttackPowerRate"] = 1.011183f,
            ["registFreezeChangeRate"] = 1.0597014f,
            ["registSleepChangeRate"] = 1.0597014f,
            ["registMadnessChangeRate"] = 1.0597014f,
        },
    };
    
    /// <summary>
    /// Default (single-player) 'bonusSoul' rune rewards for every boss.
    ///
    /// Not separated into base and DLC, since it is not used for iteration.
    /// </summary>
    public static Dictionary<long, uint> DefaultBossRewards { get; } = new()
    {
        // Base
        [10000800] = 20000,
        [10000850] = 12000,
        [10010800] = 3200,
        [11000800] = 120000,
        [11000850] = 80000,
        [11050800] = 300000,
        [11050850] = 150000,
        [12010800] = 12000,
        [12010850] = 58000,
        [12020800] = 30000,
        [12020830] = 16000,
        [12020850] = 10000,
        [12030390] = 25000,
        [12030800] = 40000,
        [12030850] = 90000,
        [12040800] = 80000,
        [12050800] = 420000,
        [12080800] = 13000,
        [12090800] = 24000,
        [13000800] = 220000,
        [13000830] = 280000,
        [13000850] = 170000,
        [14000800] = 40000,
        [14000850] = 14000,
        [15000800] = 480000,
        [15000850] = 200000,
        [16000800] = 130000,
        [16000850] = 50000,
        [16000860] = 10000,
        [18000800] = 15000,
        [18000850] = 400,
        [19000800] = 500000,
        // DLC
        [20000800] = 120000,  // [Belurat, Tower Settlement] Divine Beast Dancing Lion
        [20010800] = 500000,  // [Enir-Ilim] Promised Consort Radahn
        [20010850] = 0,  // unused?
        [21000850] = 200000,  // [Shadow Keep] Golden Hippopotamus
        [21010800] = 400000,  // [Specimen Storehouse] Messmer the Impaler
        [22000800] = 220000,  // [Stone Coffin Fissure] Putrescent Knight
        [25000800] = 420000,  // [Finger Birthing Grounds] Metyr, Mother of Fingers
        [28000800] = 410000,  // [Midra's Manse] Midra, Lord of Frenzied Flame
        // Base
        [30000800] = 2200,
        [30010800] = 2400,
        [30020800] = 1300,
        [30030800] = 3000,
        [30040800] = 1700,
        [30050800] = 3500,
        [30050850] = 4200,
        [30060800] = 3200,
        [30070800] = 12000,
        [30080800] = 20000,
        [30090800] = 21000,
        [30100800] = 28000,
        [30100801] = 0,
        [30110800] = 1600,
        [30120800] = 9400,
        [30120801] = 0,
        [30130800] = 15000,
        [30140800] = 7400,
        [30150800] = 6800,
        [30160800] = 64000,
        [30170800] = 83000,
        [30180800] = 48000,
        [30190800] = 78000,
        [30200800] = 50000,
        [31000800] = 0,
        [31010800] = 2600,
        [31020800] = 2100,
        [31030800] = 1000,
        [31040800] = 3300,
        [31050800] = 3600,
        [31060800] = 3300,
        [31070800] = 10000,
        [31090800] = 11000,
        [31100800] = 65000,
        [31110800] = 7100,
        [31120800] = 93000,
        [31150800] = 1200,
        [31170800] = 1700,
        [31180800] = 8400,
        [31190800] = 9000,
        [31190850] = 9000,
        [31200800] = 7000,
        [31210800] = 6700,
        [31220800] = 70000,
        [32000800] = 2000,
        [32010800] = 1800,
        [32020800] = 3000,
        [32040800] = 9600,
        [32050800] = 9000,
        [32050801] = 0,
        [32070800] = 7500,
        [32080800] = 7600,
        [32110800] = 120000,
        [34120800] = 16000,
        [34130800] = 94000,
        [34140850] = 29000,
        [35000800] = 100000,
        [35000850] = 30000,
        [39200800] = 24000,
        // DLC
        [40000800] = 110000,  // [Fog Rift Catacombs] Death Knight
        [40010800] = 130000,  // [Scorpion River Catacombs] Death Knight
        [41000800] = 80000,  // [Belurat Gaol] Demi-Human Swordmaster Onze
        [41010800] = 100000,  // [Bonny Gaol] Curseblade Labirith
        [41020800] = 160000,  // [Lamenter's Gaol] Lamenter (NOTE: has no multi reward by default) 
        [43000800] = 80000,  // [Rivermouth Cave] Chief Bloodfiend
        [43010800] = 130000,  // [Dragon's Pit] Ancient Dragon-man
        // Base
        [1033420800] = 80000,
        [1033430800] = 5800,
        [1033450800] = 4600,
        [1034420800] = 120000,
        [1034450800] = 14000,
        [1034480800] = 3100,
        [1034500800] = 12000,
        [1035420800] = 4900,
        [1035500800] = 10000,
        [1035530800] = 19000,
        [1036450340] = 7800,
        [1036480340] = 5600,
        [1036500800] = 3600,
        [1036540800] = 21000,
        [1037420340] = 6600,
        [1037460800] = 6000,
        [1037510800] = 60000,
        [1037530800] = 13000,
        [1037540810] = 18000,
        [1038410800] = 3800,
        [1038480800] = 5800,
        [1038510800] = 8500,
        [1038520340] = 14000,
        [1039430340] = 5600,
        [1039440800] = 4700,
        [1039500800] = 26000,
        [1039510800] = 10000,
        [1039540800] = 24000,
        [1040520800] = 10800,
        [1040530800] = 8800,
        [1041500800] = 11000,
        [1041510800] = 20000,
        [1041520800] = 60000,
        [1041530800] = 10000,
        [1042330800] = 5400,
        [1042360800] = 3200,
        [1042370800] = 2100,
        [1042380800] = 2800,
        [1042380850] = 2700,
        [1042550800] = 14000,
        [1043300800] = 3800,
        [1043330800] = 3600,
        [1043360800] = 5000,
        [1043370340] = 2400,
        [1043530800] = 20000,
        [1044320340] = 3900,
        [1044320342] = 3400,
        [1044350800] = 1900,
        [1044360800] = 1100,
        [1044530800] = 29000,
        [1045390800] = 2400,
        [1045520800] = 50000,
        [1047400800] = 9600,
        [1048370800] = 38000,
        [1048400800] = 6300,
        [1048410800] = 50000,
        [1048510800] = 36000,
        [1048570800] = 220000,
        [1049370800] = 8500,
        [1049370850] = 15000,
        [1049380800] = 12000,
        [1049390800] = 6400,
        [1049390850] = 7800,
        [1049520800] = 60000,
        [1050560800] = 180000,
        [1050570800] = 77000,
        [1050570850] = 160000,
        [1051360800] = 16000,
        [1051400800] = 91000,
        [1051430800] = 88000,
        [1051570800] = 90000,
        [1052380800] = 70000,
        [1052410800] = 80000,
        [1052410850] = 42000,
        [1052520800] = 180000,
        [1052560800] = 70000,
        [1053560800] = 75000,
        [1054560800] = 100000,
        [1248550800] = 84000,
        // DLC
        [2044450800] = 380000,  // [Ancient Ruins of Rauh] Romina, Saint of the Bud 
        [2044470800] = 210000,  // [Ancient Ruins of Rauh] Rugalea the Great Red Bear 
        [2045440800] = 100000,  // [Gravesite Plain] Ghostflame Dragon 
        [2046380800] = 80000,  // [Cerulean Coast] Dancer of Ranah 
        [2046400800] = 100000,  // [Cerulean Coast] Demi-Human Queen Marigga 
        [2046410800] = 70000,  // [Gravesite Plain] Knight of the Solitary Gaol (NO MULTI) 
        [2046450800] = 80000,  // [Gravesite Plain] Red Bear 
        [2046460800] = 180000,  // [Gravesite Plain] Divine Beast Dancing Lion (NO MULTI)
        [2047390800] = 230000,  // [Cerulean Coast] Death Rite Bird 
        [2047450800] = 80000,  // [Scadu Altus] Black Knight Garrew 
        [2048380850] = 120000,  // [Cerulean Coast] Ghostflame Dragon 
        [2048440800] = 240000,  // [Gravesite Plain] Rellana, Twin Moon Knight 
        [2049410800] = 90000,  // [Jagged Peak] Jagged Peak Drake (Solo 
        [2049430800] = 120000,  // [Moorth Highway] Ghostflame Dragon 
        [2049430850] = 80000,  // [Scadu Altus] Black Knight Edredd 
        [2049450800] = 180000,  // [Scadu Altus] Ralva the Great Red Bear 
        [2049480800] = 230000,  // [Scadu Altus] Commander Gaius 
        [2050470800] = 120000,  // [Hinterland] Tree Sentinel - Torch 
        [2050480800] = 260000,  // [Scaduview] Scadutree Avatar 
        [2050480860] = 120000,  // [Hinterland] Tree Sentinel 
        [2051440800] = 90000,  // [Scadu Altus] Rakshasa 
        [2051450720] = 210000,  // unknown 
        [2052400800] = 120000,  // [Jagged Peak] Jagged Peak Drake (Duo) 
        [2052430800] = 260000,  // [Abyssal Woods] Jori, the Elder Inquisitor 
        [2052480800] = 170000,  // [Finger Ruins] Fallingstar Beast (NO MULTI)
        [2054390800] = 490000,  // [Jagged Peak] Bayle the Dread 
        [2054390850] = 200000,  // [Jagged Peak] Ancient Dragon Senessax 
    };

    /// <summary>
    /// Initial (NG+1) scaling for every boss.
    /// </summary>
    public static readonly Dictionary<long, float> BossNgRuneScaling = new()
    {
        // Base
        [10000800] = 4f, // Godrick the Grafted - Stormveil Castle
        [10000850] = 4f, // Margit, the Fell Omen - Stormveil Castle
        [10010800] = 5f, // Grafted Scion - Chapel of Anticipation
        [11000800] = 3f, // Morgott, the Omen King - Leyndell
        [11000850] = 2f, // Godfrey, First Elden Lord - Leyndell
        [11050800] = 2f, // Hoarah Loux - Leyndell
        [11050850] = 2f, // Sir Gideon Ofnir, the All-Knowing - Leyndell
        [12010800] = 3f, // Dragonkin Soldier of Nokstella - Ainsel River
        [12010850] = 2f, // Dragonkin Soldier - Lake of Rot
        [12020800] = 3f, // Valiant Gargoyles - Siofra River
        [12020830] = 4f, // Dragonkin Soldier - Siofra River
        [12020850] = 3f, // Mimic Tear - Siofra River
        [12030390] = 2f, // Crucible Knight Sirulia - Deeproot Depths
        [12030800] = 2f, // Fia's Champion - Deeproot Depths
        [12030850] = 2f, // Lichdragon Fortissax - Deeproot Depths
        [12040800] = 2f, // Astel, Naturalborn of the Void - Lake of Rot
        [12050800] = 2f, // Mohg, Lord of Blood - Mohgwyn Palace
        [12080800] = 4f, // Ancestor Spirit - Siofra River
        [12090800] = 3f, // Regal Ancestor Spirit - Nokron, Eternal City
        [13000800] = 2f, // Maliketh, The Black Blade - Crumbling Farum Azula
        [13000830] = 2f, // Dragonlord Placidusax - Crumbling Farum Azula
        [13000850] = 2f, // Godskin Duo - Crumbling Farum Azula
        [14000800] = 3f, // Rennala, Queen of the Full Moon - Academy of Raya Lucaria
        [14000850] = 4f, // Red Wolf of Radagon - Academy of Raya Lucaria
        [15000800] = 2f, // Malenia, Blade of Miquella - Miquella's Haligtree
        [15000850] = 2f, // Loretta, Knight of the Haligtree - Miquella's Haligtree
        [16000800] = 2f, // Rykard, Lord of Blasphemy - Volcano Manor
        [16000850] = 2f, // Godskin Noble - Volcano Manor
        [16000860] = 2f, // Abductor Virgins - Volcano Manor
        [18000800] = 5f, // Ulcerated Tree Spirit - Stranded Graveyard
        [18000850] = 5f, // Soldier of Godrick - Stranded Graveyard
        [19000800] = 2f, // Elden Beast - Elden Throne
        [30000800] = 5f, // Cemetery Shade - Tombsward Catacombs (Limgrave)
        [30010800] = 5f, // Erdtree Burial Watchdog - Impaler's Catacombs (Weeping Penisula)
        [30020800] = 5f, // Erdtree Burial Watchdog - Stormfoot Catacombs (Limgrave)
        [30030800] = 2f, // Spirit-Caller Snail - Road's End Catacombs (Liurnia)
        [30040800] = 5f, // Grave Warden Duelist - Murkwater Catacombs (Limgrave)
        [30050800] = 2f, // Cemetery Shade - Black Knife Catacombs (Liurnia)
        [30050850] = 2f, // Black Knife Assassin - Black Knife Catacombs (Liurnia)
        [30060800] = 2f, // Erdtree Burial Watchdog - Cliffbottom Catacombs (Liurnia)
        [30070800] = 3f, // Erdtree Burial Watchdog - Wyndham Catacombs (Altus Plateau)
        [30080800] = 3f, // Ancient Hero of Zamor - Sainted Hero's Grave (Altus Plateau)
        [30090800] = 3f, // Red Wolf of the Champion - Gelmir Hero's Grave (Mt. Gelmir)
        [30100800] = 3f, // Crucible Knight Ordovis - Auriza Hero's Grave (Altus Plateau)
        [30100801] = 3f, // Crucible Knight (Tree Spear) - Auriza Hero's Grave (Altus Plateau)
        [30110800] = 5f, // Black Knife Assassin - Deathtouched Catacombs (Limgrave)
        [30120800] = 3f, // Misbegotten Warrior - Unsightly Catacombs (Mt. Gelmir)
        [30120801] = 3f, // Perfumer Tricia - Unsightly Catacombs (Mt. Gelmir)
        [30130800] = 3f, // Grave Warden Duelist - Auriza Side Tomb (Altus Plateau)
        [30140800] = 3f, // Erdtree Burial Watchdog - Minor Erdtree Catacombs (Caelid)
        [30150800] = 3f, // Cemetery Shade - Caelid Catacombs (Caelid)
        [30160800] = 2f, // Putrid Tree Spirit - War-Dead Catacombs (Caelid)
        [30170800] = 2f, // Ancient Hero of Zamor - Giant-Conquering Hero's Grave (Mountaintops)
        [30180800] = 2f, // Ulcerated Tree Sprit - Giants' Mountaintop Catacombs (Mountaintops)
        [30190800] = 2f, // Putrid Grave Warden Duelist - Consecrated Snowfield Catacombs (Snowfield)
        [30200800] = 2f, // Stray Mimic Tear - Hidden Path to the Haligtree
        [31000800] = 5f, // Patches - Murkwater Cave (Limgrave)
        [31010800] = 5f, // Runebear - Earthbore Cave (Weeping Penisula)
        [31020800] = 5f, // Miranda the Blighted Bloom - Tombsward Cave (Limgrave)
        [31030800] = 5f, // Beastman of Farum Azula - Groveside Cave (Limgrave)
        [31040800] = 4f, // Cleanrot Knight - Stillwater Cave (Liurnia)
        [31050800] = 4f, // Bloodhound Knight - Lakeside Crystal Cave (Liurnia)
        [31060800] = 4f, // Crystalians - Academy Crystal Cave (Liurnia)
        [31070800] = 3f, // Kindred of Rot - Seethewater Cave (Mt. Gelmir)
        [31090800] = 3f, // Demi-Human Queen Margot - Volcano Cave (Mt. Gelmir)
        [31100800] = 2f, // Beastman of Farum Azula - Dragonbarrow Cave (Dragonbarrow)
        [31110800] = 3f, // Putrid Crystalians - Sellia Hideaway (Caelid)
        [31120800] = 2f, // Misbegotten Crusader - Cave of the Forlorn (Mountaintops)
        [31150800] = 5f, // Demi-Human Chief - Coastal Cave (Limgrave)
        [31170800] = 5f, // Guardian Golem - Highroad Cave (Limgrave)
        [31180800] = 3f, // Miranda the Blighted Bloom - Perfumer's Grotto (Altus Plateau)
        [31190800] = 3f, // Black Knife Assassin - Sage's Cave (Altus Plateau)
        [31190850] = 3f, // Necromancer Garris - Sage's Cave (Altus Plateau)
        [31200800] = 3f, // Cleanrot Knight - Abandoned Cave (Caelid)
        [31210800] = 3f, // Frenzied Duelist - Gaol Cave (Caelid)
        [31220800] = 2f, // Spirit-Caller Snail - Spiritcaller's Cave (Mountaintops)
        [32000800] = 5f, // Scaly Misbegotten - Morne Tunnel (Weeping Penisula)
        [32010800] = 5f, // Stonedigger Troll - Limgrave Tunnels (Limgrave)
        [32020800] = 4f, // Crystalian (Ringblade) - Raya Lucaria Crystal Tunnel (Liurnia)
        [32040800] = 3f, // Stonedigger Troll - Old Altus Tunnel (Altus Plateau)
        [32050800] = 3f, // Crystalian (Ringblade) - Altus Tunnel (Altus Plateau)
        [32050801] = 3f, // Crystalian (Spear) - Altus Tunnel (Altus Plateau)
        [32070800] = 3f, // Magma Wyrm - Gael Tunnel (Caelid)
        [32080800] = 3f, // Fallingstar Beast - Sellia Crystal Tunnel (Caelid)
        [32110800] = 2f, // Astel, Stars of Darkness - Yelough Anix Tunnel (Snowfield)
        [34120800] = 3f, // Onyx Lord - Divine Tower of West Altus (Altus Plateau)
        [34130800] = 2f, // Godskin Apostle - Divine Tower of Caelid (Caelid)
        [34140850] = 3f, // Fell Twins - Divine Tower of East Altus (Capital Outskirts)
        [35000800] = 2f, // Mohg, The Omen - Subterranean Shunning-Grounds (Leyndell)
        [35000850] = 2f, // Esgar, Priest of Blood - Subterranean Shunning-Grounds (Leyndell)
        [39200800] = 3f, // Magma Wyrm Makar - Ruin-Strewn Precipice (Liurnia)
        [1033420800] = 4f, // Alecto, Black Knife Ringleader - Moonlight Altar (Liurnia)
        [1033430800] = 4f, // Erdtree Avatar - Revenger's Shack (Liurnia)
        [1033450800] = 4f, // Bols, Carian Knight - Cuckoo's Evergaol (Liurnia)
        [1034420800] = 4f, // Glintstone Dragon Adula - Moonfolk Ruins (Liurnia)
        [1034450800] = 4f, // Glintstone Dragon Smarag - Meeting Place (Liurnia)
        [1034480800] = 4f, // Royal Revenant - Kingsrealm Ruins (Liurnia)
        [1034500800] = 3f, // Glintstone Dragon Adula - Ranni's Rise (Liurnia)
        [1035420800] = 4f, // Omenkiller - Village of the Albinaurics (Liurnia)
        [1035500800] = 3f, // Royal Knight Loretta - Carian Manor (Liurnia)
        [1035530800] = 3f, // Magma Wyrm - Seethewater Terminus (Mt. Gelmir)
        [1036450340] = 4f, // Death Rite Bird - Gate Town Northwest (Liurnia)
        [1036480340] = 4f, // Night's Cavalry - East Raya Lucaria Gate (Liurnia)
        [1036500800] = 3f, // Onyx Lord - Royal Grave Evergaol (Liurnia)
        [1036540800] = 3f, // Full-Grown Fallingstar Beast - Crater (Mt. Gelmir)
        [1037420340] = 4f, // Death Rite Bird - Laskyar Ruins (Liurnia)
        [1037460800] = 3f, // Ball-Bearing Hunter - Church of Vows (Liurnia)
        [1037510800] = 3f, // Ancient Dragon Lansseax - Abandoned Coffin (Altus Plateau)
        [1037530800] = 3f, // Demi-Human Queen - Primeval Sorcerer Azur (Mt. Gelmir)
        [1037540810] = 3f, // Ulcerated Tree Spirit - Minor Erdtree (Mt. Gelmir)
        [1038410800] = 4f, // Adan, Thief of Fire - Malefactor's Evergaol (Liurnia)
        [1038480800] = 4f, // Erdtree Avatar - Minor Erdtree (Liurnia)
        [1038510800] = 3f, // Demi-Human Queen - Lux Ruins (Altus Plateau)
        [1038520340] = 3f, // Tibia Mariner - Wyndham Ruins (Altus Plateau)
        [1039430340] = 3f, // Night's Cavalry - Liurnia Highway Far North (Liurnia)
        [1039440800] = 4f, // Tibia Mariner - Jarburg (Liurnia)
        [1039500800] = 3f, // Godefroy the Grafted - Golden Lineage Evergaol (Altus Plateau)
        [1039510800] = 3f, // Night's Cavalry - Altus Highway Junction (Altus Plateau)
        [1039540800] = 3f, // Elemer of the Briar - Shaded Castle (Altus Plateau)
        [1040520800] = 3f, // Black Knife Assassin - Sainted Hero's Grave Entrance (Altus Plateau)
        [1040530800] = 3f, // Sanguine Noble - Writheblood Ruins (Altus Plateau)
        [1041500800] = 3f, // Fallingstar Beast - South of Tree Sentinel Duo (Altus Plateau)
        [1041510800] = 3f, // Tree Sentinel - Tree Sentinel Duo (Altus Plateau)
        [1041520800] = 3f, // Ancient Dragon Lansseax - Rampartside Path (Altus Plateau)
        [1041530800] = 3f, // Wormface - Woodfolk Ruins (Altus Plateau)
        [1042330800] = 5f, // Ancient Hero of Zamor - Weeping Evergaol (Weeping Penisula)
        [1042360800] = 5f, // Tree Sentinel - Church of Elleh (Limgrave)
        [1042370800] = 5f, // Crucible Knight - Stormhill Evergaol (Limgrave)
        [1042380800] = 5f, // Death Rite Bird - Stormgate (Limgrave)
        [1042380850] = 5f, // Ball-Bearing Hunter - Warmaster's Shack (Limgrave)
        [1042550800] = 3f, // Godskin Apostle - Windmill Heights (Altus Plateau)
        [1043300800] = 5f, // Leonine Misbegotten - Castle Morne (Weeping Penisula)
        [1043330800] = 5f, // Erdtree Avatar - Minor Erdtree (Weeping Penisula)
        [1043360800] = 5f, // Flying Dragon Agheel - Dragon-Burnt Ruins (Limgrave)
        [1043370340] = 5f, // Night's Cavalry - Agheel Lake North (Limgrave)
        [1043530800] = 3f, // Ball-Bearing Hunter - Hermit Merchant's Shack (Capital Outskirts)
        [1044320340] = 5f, // Death Rite Bird - Castle Morne Approach (Weeping Penisula)
        [1044320342] = 5f, // Night's Cavalry - Castle Morne Approach (Weeping Penisula)
        [1044350800] = 5f, // Bloodhound Knight Darriwill - Forlorn Hound Evergaol (Limgrave)
        [1044360800] = 5f, // Mad Pumpkin Head - Waypoint Ruins (Limgrave)
        [1044530800] = 3f, // Death Rite Bird - Minor Erdtree (Capital Outskirts)
        [1045390800] = 5f, // Tibia Mariner - Summonwater Village (Limgrave)
        [1045520800] = 3f, // Draconic Tree Sentinel - Capital Rampart (Capital Outskirts)
        [1047400800] = 3f, // Putrid Avatar - Minor Erdtree (Caelid)
        [1048370800] = 3f, // Decaying Ekzykes - Caelid Highway South (Caelid)
        [1048400800] = 3f, // Monstrous Dog - Southwest of Caelid Highway South (Caelid)
        [1048410800] = 2f, // Ball-Bearing Hunter - Isolated Merchant's Shack (Dragonbarrow)
        [1048510800] = 2f, // Night's Cavalry - Forbidden Lands (Mountaintops)
        [1048570800] = 2f, // Death Rite Bird - Ordina, Liturgical Town (Snowfield)
        [1049370800] = 3f, // Night's Cavalry - Southern Aeonia Swamp Bank (Caelid)
        [1049370850] = 3f, // Death Rite Bird - Southern Aeonia Swamp Bank (Caelid)
        [1049380800] = 3f, // Commander O'Neil - East Aeonia Swamp (Caelid)
        [1049390800] = 3f, // Nox Priest - West Sellia (Caelid)
        [1049390850] = 3f, // Battlemage Hugues - Sellia Crystal Tunnel Entrance (Caelid)
        [1049520800] = 2f, // Black Blade Kindred - Before Grand Lift of Rold (Mountaintops)
        [1050560800] = 2f, // Great Wyrm Theodorix - Albinauric Rise (Mountaintops)
        [1050570800] = 2f, // Death Rite Bird - West of Castle So (Mountaintops)
        [1050570850] = 2f, // Putrid Avatar - Minor Erdtree (Snowfield)
        [1051360800] = 3f, // Crucible Knight - Redmane Castle (Caelid)
        [1051400800] = 2f, // Putrid Avatar - Dragonbarrow Fork (Caelid)
        [1051430800] = 2f, // Black Blade Kindred - Bestial Sanctum (Caelid)
        [1051570800] = 2f, // Commander Niall - Castle Soul (Mountaintops)
        [1052380800] = 3f, // Starscourge Radahn - Battlefield (Caelid)
        [1052410800] = 2f, // Flying Dragon Greyll - Dragonbarrow (Caelid)
        [1052410850] = 2f, // Night's Cavalry - Dragonbarrow (Caelid)
        [1052520800] = 2f, // Fire Giant - Giant's Forge (Mountaintops)
        [1052560800] = 2f, // Erdtree Avatar - Minor Erdtree (Mountaintops)
        [1053560800] = 2f, // Roundtable Knight Vyke - Lord Contender's Evergaol (Mountaintops)
        [1054560800] = 2f, // Borealis the Freezing Fog - Freezing Fields (Mountaintops)
        [1248550800] = 2f, // Night's Cavalry - Sourthwest (Mountaintops)
       
    };

    /// <summary>
    /// NOTE: All DLC boss use the 'base endgame' 2x scaling for now. Not sure if this is correct.
    /// </summary>
    public static readonly Dictionary<long, float> DLCBossNgRuneScaling = new()
    {
        [20000800] = 2f, // [Belurat, Tower Settlement] Divine Beast Dancing Lion
        [20010800] = 2f, // [Enir-Ilim] Promised Consort Radahn
        [20010850] = 2f, // unused?
        [21000850] = 2f, // [Shadow Keep] Golden Hippopotamus
        [21010800] = 2f, // [Specimen Storehouse] Messmer the Impaler
        [22000800] = 2f, // [Stone Coffin Fissure] Putrescent Knight
        [25000800] = 2f, // [Finger Birthing Grounds] Metyr, Mother of Fingers
        [28000800] = 2f, // [Midra's Manse] Midra, Lord of Frenzied Flame
        [40000800] = 2f, // [Fog Rift Catacombs] Death Knight
        [40010800] = 2f, // [Scorpion River Catacombs] Death Knight
        [41000800] = 2f, // [Belurat Gaol] Demi-Human Swordmaster Onze
        [41010800] = 2f, // [Bonny Gaol] Curseblade Labirith
        [41020800] = 2f, // [Lamenter's Gaol] Lamenter (NOTE: has no multi reward by default) 
        [43000800] = 2f, // [Rivermouth Cave] Chief Bloodfiend
        [43010800] = 2f, // [Dragon's Pit] Ancient Dragon-man
        [2044450800] = 2f, // [Ancient Ruins of Rauh] Romina, Saint of the Bud
        [2044470800] = 2f, // [Ancient Ruins of Rauh] Rugalea the Great Red Bear
        [2045440800] = 2f, // [Gravesite Plain] Ghostflame Dragon
        [2046380800] = 2f, // [Cerulean Coast] Dancer of Ranah
        [2046400800] = 2f, // [Cerulean Coast] Demi-Human Queen Marigga
        [2046410800] = 2f, // [Gravesite Plain] Knight of the Solitary Gaol (NO MULTI)
        [2046450800] = 2f, // [Gravesite Plain] Red Bear
        [2046460800] = 2f, // [Gravesite Plain] Divine Beast Dancing Lion (NO MULTI)
        [2047390800] = 2f, // [Cerulean Coast] Death Rite Bird
        [2047450800] = 2f, // [Scadu Altus] Black Knight Garrew
        [2048380850] = 2f, // [Cerulean Coast] Ghostflame Dragon
        [2048440800] = 2f, // [Gravesite Plain] Rellana, Twin Moon Knight
        [2049410800] = 2f, // [Jagged Peak] Jagged Peak Drake (Solo
        [2049430800] = 2f, // [Moorth Highway] Ghostflame Dragon
        [2049430850] = 2f, // [Scadu Altus] Black Knight Edredd
        [2049450800] = 2f, // [Scadu Altus] Ralva the Great Red Bear
        [2049480800] = 2f, // [Scadu Altus] Commander Gaius
        [2050470800] = 2f, // [Hinterland] Tree Sentinel - Torch
        [2050480800] = 2f, // [Scaduview] Scadutree Avatar
        [2050480860] = 2f, // [Hinterland] Tree Sentinel
        [2051440800] = 2f, // [Scadu Altus] Rakshasa
        [2051450720] = 2f, // unknown
        [2052400800] = 2f, // [Jagged Peak] Jagged Peak Drake (Duo)
        [2052430800] = 2f, // [Abyssal Woods] Jori, the Elder Inquisitor
        [2052480800] = 2f, // [Finger Ruins] Fallingstar Beast (NO MULTI)
        [2054390800] = 2f, // [Jagged Peak] Bayle the Dread
        [2054390850] = 2f, // [Jagged Peak] Ancient Dragon Senessax
    };
    
    /// <summary>
    /// Different levels of NG+ scaling (area-dependent) in SpEffectParam (base game).
    /// </summary>
    public static int[] ScalingEffectRows { get; } =
    [
        7400, 7410, 7420, 7430, 7440, 7450, 7460, 7470, 7480, 7490,
        7500, 7510, 7520, 7530, 7540, 7550, 7560, 7570, 7580, 7590,
        7600,
    ];
    
    /// <summary>
    /// Different levels of NG+ scaling (area-dependent) in SpEffectParam (base game).
    ///
    /// The final level, 7530, is used for Bayle the Dread only.
    /// </summary>
    public static int[] DLCScalingEffectRows { get; } =
    [
        20007400, 20007410, 20007420, 20007430, 20007440, 20007450, 20007460, 20007470, 20007480, 20007490,
        20007500, 20007510, 20007520, 20007530,
    ];
    
    /// <summary>
    /// All the SpEffectParam fields that change in NG+ effects.
    /// </summary>
    public static string[] EffectFields { get; } =
    [
        "maxHpRate",
        "maxStaminaRate",
        "haveSoulRate",
        "physicsAttackPowerRate",
        "magicAttackPowerRate",
        "fireAttackPowerRate",
        "thunderAttackPowerRate",
        "physicsDiffenceRate",
        "magicDiffenceRate",
        "fireDiffenceRate",
        "thunderDiffenceRate",
        "staminaAttackRate",
        "registPoizonChangeRate",
        "registDiseaseChangeRate",
        "registBloodChangeRate",
        "darkDiffenceRate",
        "darkAttackPowerRate",
        "registFreezeChangeRate",
        "registSleepChangeRate",
        "registMadnessChangeRate",
    ];
}