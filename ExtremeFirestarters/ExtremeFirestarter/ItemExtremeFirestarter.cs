using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Util;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;

using Vintagestory.API.Client;
using System;
using Vintagestory.API.MathTools;
using ConfigureEverything.Configuration;


namespace ExtremeFirestarters
{
    public class ItemExtremeFirestarter : Item
    {
        /*
         * Config settings
         */
        ConfigExtremeFirestarters ExtremeFirestartersConfig;

        string igniteAnimation;
        string igniteSound;

        int modMode = 1; //0 = Custom, 1 = Hardcore , 2 = Vanilla+ , 3 = Items Only
        //the following variables are only taken into account when Custom is set.
        bool hungerDamage = true;
        bool healthDamage = true;
        bool durabilityTweak = true;

        bool increasedDurabilty = false;
        float increaseDurabilityAmount = 10;
        bool quickerFirestarting = false;
        float increaseSucessAmount = 7.5f;
        float increaseTimeAmount = 5;
        //These last three floats will apply whenever their corresponding setting is on, whether by custom or modMode.

        private bool SettingsShouldDoHungerDamage()
        {
            return modMode == 1 || modMode == 2 || (modMode == 0 && hungerDamage);
        }
        private bool SettingsShouldDoHealthDamage()
        {
            return modMode == 1 || (modMode == 0 && healthDamage);
        }
        private bool SettingsShouldDoDurabilityTweak()
        {
            return modMode == 1 || modMode == 2 || (modMode == 0 && durabilityTweak);
        }
        private bool SettingsShouldDoIncreasedDurability()
        {
            return modMode == 3 || modMode == 2 || (modMode == 0 && increasedDurabilty);
        }
        private bool SettingsShouldDoQuickerFirestarting()
        {
            return modMode == 3 || modMode == 2 || (modMode == 0 && quickerFirestarting);
        }

        //Applies the item settings, character classes, mod settings, and other buffs to get a single speed multiplier.
        public float TotalSpeed(ItemSlot slot, EntityAgent byEntity)
        {
            float statSpeed = byEntity.Stats.GetBlended("firestartingSpeed");
            if (SettingsShouldDoQuickerFirestarting())
            {
                statSpeed *= increaseTimeAmount;
            }
            float baseSpeed = slot.Itemstack.Item.Attributes["startSpeed"].AsFloat(1);
            return baseSpeed * statSpeed;
        }

        //Applies the item settings, character classes, mod settings, and other buffs to get a single start rate multiplier.
        public float TotalStartRate(ItemSlot slot, EntityAgent byEntity)
        {
            float successRate = byEntity.Stats.GetBlended("firestartingSuccess");
            if (SettingsShouldDoQuickerFirestarting())
            {
                successRate *= increaseSucessAmount;
            }
            float baseSsuccessRate = slot.Itemstack.Item.Attributes["startRate"].AsFloat(1);
            return baseSsuccessRate * successRate;
        }

        //Applies character classes, mod settings, and other buffs to get a single durability multiplier.
        private float DurabilityMultiplier(EntityAgent byEntity)
        {
            float durabilityChance = byEntity.Stats.GetBlended("firestartingDurability");
            if (SettingsShouldDoIncreasedDurability())
            {
                durabilityChance *= increaseDurabilityAmount;
            }
            return durabilityChance;
        }


        /*
         * Setup
         */
        public override void OnLoaded(ICoreAPI api)
        {
            //Pull in settings from json file.
            igniteAnimation = Attributes["igniteAnimation"].AsString("startfire");
            igniteSound = Attributes["igniteSound"].AsString("sounds/player/handdrill");

            //Pull in settings from config file.
            ExtremeFirestartersConfig = ModConfig.ReadConfig<ConfigExtremeFirestarters>(api, $"ExtremeFirestarter/config.json");
            modMode = ExtremeFirestartersConfig.ModMode;
            hungerDamage = ExtremeFirestartersConfig.HungerDamage;
            healthDamage = ExtremeFirestartersConfig.HealthDamage;
            durabilityTweak = ExtremeFirestartersConfig.DurabilityTweak;
            increasedDurabilty = ExtremeFirestartersConfig.IncreasedDurabilty;
            increaseDurabilityAmount = ExtremeFirestartersConfig.IncreaseDurabilityAmount;
            quickerFirestarting = ExtremeFirestartersConfig.QuickerFirestarting;
            increaseSucessAmount = ExtremeFirestartersConfig.IncreaseSucessAmount;
            increaseTimeAmount = ExtremeFirestartersConfig.IncreaseTimeAmount;
        }

        public string igniteSoundCacheString()
        {
            return Code.FirstCodePart() + "sound";
        }


        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);

            //IF there's no selection, the player doesn't own the block, or it's not ignitable, then cancel.
            if (blockSel == null) return;
            Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);

            IPlayer byPlayer = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return;
            }

            EnumIgniteState state = EnumIgniteState.NotIgnitable;
            if (!(block is IIgnitable ign) || (state = ign.OnTryIgniteBlock(byEntity, blockSel.Position, 0)) != EnumIgniteState.Ignitable)
            {
                if (state == EnumIgniteState.NotIgnitablePreventDefault) handling = EnumHandHandling.PreventDefault;
                return;
            }

            handling = EnumHandHandling.PreventDefault;

            //Start the animation.
            byEntity.AnimManager.StartAnimation(igniteAnimation);

            //Play the sound.
            if (api.Side == EnumAppSide.Client)
            {
                api.Event.UnregisterCallback(ObjectCacheUtil.TryGet<long>(api, igniteSoundCacheString()));
                api.ObjectCache[igniteSoundCacheString()] = api.Event.RegisterCallback((dt) => byEntity.World.PlaySoundAt(new AssetLocation(igniteSound), byEntity, byPlayer, false, 16), 0);
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            //Do checks for ownership and ignition status again.
            if (blockSel == null)
            {
                api.Event.UnregisterCallback(ObjectCacheUtil.TryGet<long>(api, igniteSoundCacheString()));
                return false;
            }

            IPlayer player = (byEntity as EntityPlayer)?.Player;
            if (!byEntity.World.Claims.TryAccess(player, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                api.Event.UnregisterCallback(ObjectCacheUtil.TryGet<long>(api, igniteSoundCacheString()));
                return false;
            }

            Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);
            EnumIgniteState enumIgniteState = EnumIgniteState.NotIgnitable;

            //Multiply the actual seconds used by the scale to manipulate how long it takes.
            float scaledSecondsUsed = secondsUsed * TotalSpeed(slot, byEntity);

            //Try to ignite the block.
            if (block is IIgnitable ignitable)
            {
                enumIgniteState = ignitable.OnTryIgniteBlock(byEntity, blockSel.Position, scaledSecondsUsed);
            }

            if (enumIgniteState == EnumIgniteState.NotIgnitable || enumIgniteState == EnumIgniteState.NotIgnitablePreventDefault)
            {
                api.Event.UnregisterCallback(ObjectCacheUtil.TryGet<long>(api, igniteSoundCacheString()));
                return false;
            }

            if (byEntity.World is IClientWorldAccessor)
            {
                ModelTransform modelTransform = new ModelTransform();
                modelTransform.EnsureDefaultValues();
                float num = GameMath.Clamp(1f - 2f * scaledSecondsUsed, 0f, 1f);
                Random rand = api.World.Rand;
                modelTransform.Translation.Set(num * num * num * 1.6f - 1.6f, 0f, 0f);
                modelTransform.Rotation.Y = 0f - Math.Min(scaledSecondsUsed * 120f, 30f);
                if (scaledSecondsUsed > 0.5f)
                {
                    modelTransform.Translation.Add((float)rand.NextDouble() * 0.1f, (float)rand.NextDouble() * 0.1f, (float)rand.NextDouble() * 0.1f);
                    (api as ICoreClientAPI).World.SetCameraShake(0.04f);
                }
            }

            return enumIgniteState == EnumIgniteState.Ignitable;
        }

        private void CheckDurabilityAndReturnItem(ItemSlot slot, EntityAgent byEntity)
        {
            IServerPlayer byPlayer = (byEntity as EntityPlayer)?.Player as IServerPlayer;

            //First grab tool info in case it *is* broken.
            string brokenItemName = slot.Itemstack.GetName();
            string brokenReturnItemName = slot.Itemstack.Item.Attributes["returnItemOnBroken"].AsString("");
            float returnItemOnBrokenChance = (float)slot.Itemstack.Item.Attributes["returnItemOnBrokenChance"].AsDouble(0f);

            //A shortcut to increasing durability - roll to see if durability is taken.
            if (SettingsShouldDoIncreasedDurability() && (float)api.World.Rand.NextDouble() > (1 / DurabilityMultiplier(byEntity)))
            {
                return;
            }

            DamageItem(api.World, byEntity, slot);

            //Check to see if the item has broken, to potentially return an item.
            if (slot.Itemstack == null)
            {
                if (brokenReturnItemName != "")
                {
                    //Check the attributes and see if the item return is successful.
                    float checkReturnSuccess = (float)api.World.Rand.NextDouble();
                    if (returnItemOnBrokenChance > checkReturnSuccess)
                    {
                        var item = api.World.SearchItems(new AssetLocation(brokenReturnItemName))[0];

                        var itemStack = new ItemStack(item);

                        byPlayer.InventoryManager.TryGiveItemstack(itemStack);
                        string returnedItemName = itemStack.GetName();
                        byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, $"Your {brokenItemName} has broken, but you were able to recover a {returnedItemName}.", EnumChatType.Notification);
                    }
                }
            }
        }

        public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
        {
            //Stop the animation of the player, 
            byEntity.AnimManager.StopAnimation(igniteAnimation);

            //then cancel if on the client.
            if (api.World.Side == EnumAppSide.Client) return;
            //or if no block is targeted.
            if (blockSel == null) return;

            //Get a scaled secondsUsed value.
            float scaledSecondsUsed = secondsUsed * TotalSpeed(slot, byEntity);
            float startRate = TotalStartRate(slot, byEntity);

            //We have a block, see if we can set it on fire.
            Block block = byEntity.World.BlockAccessor.GetBlock(blockSel.Position);

            EnumIgniteState igniteState = EnumIgniteState.NotIgnitable;
            var ign = block as IIgnitable;
            if (ign != null) igniteState = ign.OnTryIgniteBlock(byEntity, blockSel.Position, scaledSecondsUsed);

            //If not, return.
            if (igniteState != EnumIgniteState.IgniteNow)
            {
                api.Event.UnregisterCallback(ObjectCacheUtil.TryGet<long>(api, igniteSoundCacheString()));
                return;
            }

            //Calculate how much, if any "hunger damage" is done.
            float hungerDamageAverage = slot.Itemstack.Item.Attributes["hungerDamageAvg"].AsFloat(0.0f);
            float hungerDamageVariance = slot.Itemstack.Item.Attributes["hungerDamageVar"].AsFloat(0.0f);
            float hungerDamageRandom = ((float)api.World.Rand.NextDouble() * 2) - 1;
            float fullHungerDamage = (hungerDamageRandom * hungerDamageVariance) + hungerDamageAverage;

            //It's technically possible to go under 0 hunger, so this block takes care of that.
            //If there's more hungerDamage than saturation left, it stores in this variable and will do extra damage to health.
            float carryoverHungerDamage = 0;

            //Only apply the hunger damage if the setting is on.
            if (SettingsShouldDoHungerDamage() && fullHungerDamage > 0)
            {
                ITreeAttribute currentHungerAttr = byEntity.WatchedAttributes.GetTreeAttribute("hunger");
                if (currentHungerAttr != null)
                {
                    float currentHunger = currentHungerAttr.GetFloat("currentsaturation", 0);

                    //IF there's more damage being done than saturation left, take the rest of the saturation, otherwise do the full hunger damage.
                    float takenHungerDamage = fullHungerDamage > currentHunger ? currentHunger : fullHungerDamage;
                    //Calculate what to carry over. Divided by 150 here because saturation is on a scale of ~1500 and health ~10
                    carryoverHungerDamage = (fullHungerDamage - takenHungerDamage) / 150;
                    //Apply the damage.
                    byEntity.ReceiveSaturation(-1 * takenHungerDamage);
                }
            }

            //Calculate how much, if any, health is done.
            float healthDamageChance = slot.Itemstack.Item.Attributes["healthDamageChance"].AsFloat(0.0f);
            bool healthDamageHit = (float)api.World.Rand.NextDouble() <= healthDamageChance;
            float fullHealthDamage = 0;

            IServerPlayer byPlayer = (byEntity as EntityPlayer)?.Player as IServerPlayer;

            if (healthDamageHit)
            {
                float healthDamageAverage = slot.Itemstack.Item.Attributes["healthDamageAvg"].AsFloat(0.0f);
                float healthDamageVariance = slot.Itemstack.Item.Attributes["healthDamageVar"].AsFloat(0.0f);

                float healthDamageRandom = ((float)api.World.Rand.NextDouble() * 2) - 1;
                fullHealthDamage = (healthDamageRandom * healthDamageVariance) + healthDamageAverage;
            }

            //Add together our two possible sources of health damage.
            fullHealthDamage += carryoverHungerDamage;

            //Only apply it if settings allow it.
            if (SettingsShouldDoHealthDamage() && fullHealthDamage > 0)
            {
                DamageSource damage = new DamageSource()
                {
                    Type = EnumDamageType.Injury
                };
                byEntity.ReceiveDamage(damage, fullHealthDamage);
                byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "You accidentally hurt yourself trying to light a fire.", EnumChatType.Notification);
            }

            //If the durability tweak is on, do the durability damage now.
            if (SettingsShouldDoDurabilityTweak())
            {
                CheckDurabilityAndReturnItem(slot, byEntity);
            }

            //Now that we've applied all the effects, check the attributes and see if starting the fire is actually successful.
            float checkSuccess = (float)api.World.Rand.NextDouble();
            if (startRate < checkSuccess)
            {
                return;
            }

            //Set the block on fire.
            if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
            {
                return;
            }

            //If the durability tweak is turned off, apply now as normal.
            if (!SettingsShouldDoDurabilityTweak())
            {
                CheckDurabilityAndReturnItem(slot, byEntity);
            }

            EnumHandling handled = EnumHandling.PassThrough;
            ign.OnTryIgniteBlockOver(byEntity, blockSel.Position, scaledSecondsUsed, ref handled);
        }

        public override bool OnHeldInteractCancel(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, EnumItemUseCancelReason cancelReason)
        {
            byEntity.AnimManager.StopAnimation(igniteAnimation);
            api.Event.UnregisterCallback(ObjectCacheUtil.TryGet<long>(api, igniteSoundCacheString()));
            return true;
        }

        //Functions to help merge "match" stacks.
        public override int GetMergableQuantity(ItemStack sinkStack, ItemStack sourceStack, EnumMergePriority priority)
        {
            if (priority == EnumMergePriority.DirectMerge)
            {

                if (sinkStack.Collectible.Code == sourceStack.Collectible.Code)
                {
                    bool mergeDurability = sinkStack.Item.Attributes["mergeDurability"].AsBool();
                    if (mergeDurability)
                    {
                        int quantitySink = sinkStack.Item.GetRemainingDurability(sinkStack);
                        int quantitySource = sourceStack.Item.GetRemainingDurability(sourceStack);
                        int totalQuanity = quantitySink + quantitySource;
                        int maxQuanity = sinkStack.Item.GetMaxDurability(sinkStack);
                        if (totalQuanity <= maxQuanity)
                        {
                            return 1;
                        }
                    }
                }
            }

            return base.GetMergableQuantity(sinkStack, sourceStack, priority);
        }

        public override void TryMergeStacks(ItemStackMergeOperation op)
        {
            if (op.CurrentPriority == EnumMergePriority.DirectMerge)
            {
                ItemStack sinkStack = op.SinkSlot.Itemstack;
                ItemStack sourceStack = op.SourceSlot.Itemstack;

                if (sinkStack.Collectible.Code == sourceStack.Collectible.Code)
                {
                    bool mergeDurability = sinkStack.Item.Attributes["mergeDurability"].AsBool();
                    if (mergeDurability)
                    {
                        int quantitySink = sinkStack.Item.GetRemainingDurability(sinkStack);
                        int quantitySource = sourceStack.Item.GetRemainingDurability(sourceStack);
                        int totalQuanity = quantitySink + quantitySource;
                        int maxQuanity = sinkStack.Item.GetMaxDurability(sinkStack);
                        if (totalQuanity <= maxQuanity)
                        {
                            op.SinkSlot.Itemstack.Item.SetDurability(sinkStack, totalQuanity);
                            op.SourceSlot.TakeOutWhole();
                            op.SinkSlot.MarkDirty();
                            op.SourceSlot.MarkDirty();
                            return;
                        }
                    }
                }
            }

            base.TryMergeStacks(op);
        }

        public override WorldInteraction[] GetHeldInteractionHelp(ItemSlot inSlot)
        {
            return new WorldInteraction[1]
            {
            new WorldInteraction
            {
                HotKeyCode = "shift",
                ActionLangCode = "heldhelp-igniteblock",
                MouseButton = EnumMouseButton.Right
            }
            };
        }
    }
}