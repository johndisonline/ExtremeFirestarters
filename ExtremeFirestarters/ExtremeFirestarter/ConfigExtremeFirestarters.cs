using Newtonsoft.Json;
using System.Collections.Generic;
using Vintagestory.API.Common;
using Vintagestory.API.Util;

public class ConfigExtremeFirestarters
{
    [JsonProperty(Order = 1)]
    public string ModModeDescription => "(0, 1, 2, or 3) An overall switch for which settings are used. 1 is Hardcore, 2 is Vanilla+ and 3 is Items Only. See mod page for what these settings are. Set to 0 for custom settings.";

    [JsonProperty(Order = 2)]
    public int ModMode { get; set; }// = 1;


    [JsonProperty(Order = 3)]
    public string HungerDamageDescription => "(Boolean) Whether or not to take away from hunger bar with every attempt. Will be overridden by ModMode unless it's set to custom.";

    [JsonProperty(Order = 4)]
    public bool HungerDamage { get; set; }// = true;


    [JsonProperty(Order = 5)]
    public string HealthDamageDescription => "(Boolean) Whether or not to have a chance of inflicting health damage with every attempt. Will be overridden by ModMode unless it's set to custom.";

    [JsonProperty(Order = 6)]
    public bool HealthDamage { get; set; }// = true;


    [JsonProperty(Order = 7)]
    public string DurabilityTweakDescription => "(Boolean) Whether or not to impact durability BEFORE the chance of success. Will be overridden by ModMode unless it's set to custom.";

    [JsonProperty(Order = 8)]
    public bool DurabilityTweak { get; set; }// = true;


    [JsonProperty(Order = 9)]
    public string IncreasedDurabiltyDescription => "(Boolean) Whether or not to increase durability back to vanilla+ values. Will be overridden by ModMode unless it's set to custom.";

    [JsonProperty(Order = 10)]
    public bool IncreasedDurabilty { get; set; }// = false;


    [JsonProperty(Order = 11)]
    public string IncreaseDurabilityAmountDescription => "(Float) How much to increase durability by. (More specifically - the chance it will impact durability). Will be applied if IncreasedDurability is on.";

    [JsonProperty(Order = 12)]
    public float IncreaseDurabilityAmount { get; set; }// = 10;


    [JsonProperty(Order = 13)]
    public string QuickerFirestartingDescription => "(Boolean) Whether or not to increase the speed at which fires are started. Will be overridden by ModMode unless it's set to custom.";

    [JsonProperty(Order = 14)]
    public bool QuickerFirestarting { get; set; }// = false;


    [JsonProperty(Order = 15)]
    public string IncreaseSucessAmountDescription => "(Float) Multiplier for increasing the success rate of starting a fire. Will be applied if QuickerFirestarting is on.";

    [JsonProperty(Order = 16)]
    public float IncreaseSucessAmount { get; set; }// = 7.5f;


    [JsonProperty(Order = 17)]
    public string IncreaseTimeAmountDescription => "(Float) Multiplier for shortening the amount of time each attempt takes. Will be applied if QuickerFirestarting is on.";

    [JsonProperty(Order = 18)]
    public float IncreaseTimeAmount { get; set; }// = 5;

    public ConfigExtremeFirestarters(ICoreAPI api, ConfigExtremeFirestarters previousConfig = null)
    {
        if (previousConfig != null)
        {
            ModMode = previousConfig.ModMode;
            HungerDamage = previousConfig.HungerDamage;
            HealthDamage = previousConfig.HealthDamage;
            DurabilityTweak = previousConfig.DurabilityTweak;
            IncreasedDurabilty = previousConfig.IncreasedDurabilty;
            IncreaseDurabilityAmount = previousConfig.IncreaseDurabilityAmount;
            QuickerFirestarting = previousConfig.QuickerFirestarting;
            IncreaseSucessAmount = previousConfig.IncreaseSucessAmount;
            IncreaseTimeAmount = previousConfig.IncreaseTimeAmount;
        } else
        {
            ModMode = 1;
            HungerDamage = true;
            HealthDamage = true;
            DurabilityTweak = true;
            IncreasedDurabilty = false;
            IncreaseDurabilityAmount = 10;
            QuickerFirestarting = false;
            IncreaseSucessAmount = 7.5f;
            IncreaseTimeAmount = 5;
        }
    }


}