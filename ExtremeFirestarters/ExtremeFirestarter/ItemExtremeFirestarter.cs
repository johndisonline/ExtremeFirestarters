using Vintagestory.API.Server;
using Vintagestory.API.Common;
using Vintagestory.GameContent;
using Vintagestory.API.Util;
using Vintagestory.API.Config;
using Vintagestory.API.Datastructures;
using Vintagestory.API.MathTools;


namespace ExtremeFirestarters
{
	public class ItemExtremeFirestarter : ItemFirestarter
	{
        //a lot of code has to be copied and pasted because igniteAnimation is private on the parent.
        string igniteAnimation;
        string igniteSound;

        public override void OnLoaded(ICoreAPI api)
        {
            igniteAnimation = Attributes["igniteAnimation"].AsString("startfire");
            igniteSound = Attributes["igniteSound"].AsString("sounds/player/handdrill");
        }

        public string igniteSoundCacheString()
        {
            return Code.FirstCodePart() + "firestartersound";
        }


        public override void OnHeldInteractStart(ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel, bool firstEvent, ref EnumHandHandling handling)
        {
            base.OnHeldInteractStart(slot, byEntity, blockSel, entitySel, firstEvent, ref handling);
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

            byEntity.AnimManager.StartAnimation(igniteAnimation);

            if (api.Side == EnumAppSide.Client)
            {
                api.Event.UnregisterCallback(ObjectCacheUtil.TryGet<long>(api, igniteSoundCacheString()));
                api.ObjectCache[igniteSoundCacheString()] = api.Event.RegisterCallback((dt) => byEntity.World.PlaySoundAt(new AssetLocation(igniteSound), byEntity, byPlayer, false, 16), 0);
            }
        }

        public override bool OnHeldInteractStep(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
            //The magic lines that speed up or slow down the firestarters.
            float startSpeed = slot.Itemstack.Item.Attributes["startSpeed"].AsFloat(1);
			float scaledSecondsUsed = secondsUsed * startSpeed;
            return base.OnHeldInteractStep(scaledSecondsUsed, slot, byEntity, blockSel, entitySel);
		}
		public override void OnHeldInteractStop(float secondsUsed, ItemSlot slot, EntityAgent byEntity, BlockSelection blockSel, EntitySelection entitySel)
		{
                //Stop the animation of the player, 
                byEntity.AnimManager.StopAnimation(igniteAnimation);
                //then cancel if on the client.
                if (api.World.Side == EnumAppSide.Client) return;
                //or if no block is targeted.
                if (blockSel == null) return;

                //Pull the start speed to get a scaled secondsUsed value.
                float startSpeed = slot.Itemstack.Item.Attributes["startSpeed"].AsFloat(1);
                float scaledSecondsUsed = secondsUsed * startSpeed;

                float startRate = slot.Itemstack.Item.Attributes["startRate"].AsFloat(1);

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
                if (fullHungerDamage > 0)
                {
                    //This is apparently how you get current saturation.
                    ITreeAttribute currentHungerAttr = byEntity.WatchedAttributes.GetTreeAttribute("hunger");
                    if (currentHungerAttr != null)
                    {
                        float currentHunger = currentHungerAttr.GetFloat("currentsaturation", 0);

                        //IF there's more damage being done than saturation left, take the rest of the saturation, otherwise do the full hunger damage.
                        float takenHungerDamage = fullHungerDamage > currentHunger ? currentHunger : fullHungerDamage;
                        //Calculate what to carry over. Divided by 150 here because saturation is on a scale of ~1500 and health ~10
                        carryoverHungerDamage = (fullHungerDamage - takenHungerDamage)/150;
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
                if (fullHealthDamage > 0)
                {
                    DamageSource damage = new DamageSource()
                    {
                        Type = EnumDamageType.Injury
                    };
                    byEntity.ReceiveDamage(damage, fullHealthDamage);
                    byPlayer.SendMessage(GlobalConstants.GeneralChatGroup, "You accidentally hurt yourself trying to light a fire.", EnumChatType.Notification);
                }

                //Damage the item BEFORE seeing if it's successful, but first grab tool info in case it *is* broken.
                string brokenItemName = slot.Itemstack.GetName();
                string brokenReturnItemName = slot.Itemstack.Item.Attributes["returnItemOnBroken"].AsString("");
                float returnItemOnBrokenChance = (float)slot.Itemstack.Item.Attributes["returnItemOnBrokenChance"].AsDouble(0f);
            
                DamageItem(api.World, byEntity, slot);

                //Check to see if the item has broken.
                if(slot.Itemstack == null)
                {   
                    if(brokenReturnItemName != "")
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

                //Now that we've applied all the effects, check the attributes and see if starting the fire is actually successful.
                float checkSuccess = (float) api.World.Rand.NextDouble();
                if (startRate < checkSuccess)
                {
                    return;
                }

                //Set the block on fire.
                if (!byEntity.World.Claims.TryAccess(byPlayer, blockSel.Position, EnumBlockAccessFlags.Use))
                {
                    return;
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
                            //op.MovedQuantity = 1;
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
    }
}