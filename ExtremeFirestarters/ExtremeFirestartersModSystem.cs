using ConfigureEverything.Configuration;
using ExtremeFirestarters;
using Vintagestory.API.Client;
using Vintagestory.API.Common;
using Vintagestory.API.Config;
using Vintagestory.API.Server;

namespace ExtremeFirestarters
{
    public class ExtremeFirestartersModSystem : ModSystem
    {
        ICoreAPI api = null;
        ICoreServerAPI sapi = null;
        ICoreClientAPI capi = null;
        public static ConfigExtremeFirestarters ExtremeFirestartersConfig { get; private set; }

        public override void Start(ICoreAPI api)
        {
            this.api = api;
            api.RegisterItemClass("ItemExtremeFirestarter", typeof(ItemExtremeFirestarter));
        }
        public override void StartClientSide(ICoreClientAPI capi) { this.capi = capi; }
        public override void StartServerSide(ICoreServerAPI sapi)
        {
            this.sapi = sapi;
            ExtremeFirestartersConfig = ModConfig.ReadConfig<ConfigExtremeFirestarters>(api, $"ExtremeFirestarter/config.json");
        }
    }
}
