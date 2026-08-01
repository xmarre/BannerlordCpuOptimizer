using System.ComponentModel;
using GameCampaign = TaleWorlds.CampaignSystem.Campaign;

namespace BannerlordCpuOptimizer.Optimization
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public struct CareerChoicePatchState
    {
        internal CareerChoiceCallState Inner;
    }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class CareerChoicePatchBridge
    {
        public static bool Begin(
            string id,
            out object cachedResult,
            out CareerChoicePatchState state)
        {
            bool served = CareerChoiceCache.TryServeOrBegin(
                id,
                GameCampaign.Current,
                out cachedResult,
                out CareerChoiceCallState innerState);
            state = new CareerChoicePatchState { Inner = innerState };
            return !served;
        }

        public static void Complete(
            string id,
            object result,
            CareerChoicePatchState state)
        {
            CareerChoiceCache.CompleteCall(id, result, state.Inner);
        }
    }
}
