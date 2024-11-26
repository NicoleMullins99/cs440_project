using Unity.MLAgents;

namespace Project.GameSystems.ML_Agents {
    public class ExtendedDecisionRequester : DecisionRequester {
        public bool IsEnabled = true;

        protected override bool ShouldRequestDecision(DecisionRequestContext context) {
            return base.ShouldRequestDecision(context) && IsEnabled;
        }

        protected override bool ShouldRequestAction(DecisionRequestContext context) {
            return base.ShouldRequestAction(context) && IsEnabled;
        }
    }
}