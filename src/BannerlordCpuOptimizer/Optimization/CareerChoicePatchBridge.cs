using System.ComponentModel;
using GameCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace BannerlordCpuOptimizer.Optimization
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class CareerChoicePatchBridge
    {
        public static bool Begin(
            string id,
            out object cachedResult,
            out CareerChoiceCallState state)
        {
            bool served = CareerChoiceCache.TryServeOrBegin(
                id,
                GameCampaign.Current,
                out cachedResult,
                out state);
            return !served;
        }

        public static void Complete(
            string id,
            object result,
            CareerChoiceCallState state)
        {
            CareerChoiceCache.CompleteCall(id, result, state);
        }
    }
}
