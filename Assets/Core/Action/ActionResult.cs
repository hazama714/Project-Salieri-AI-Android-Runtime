namespace SalieriAI.Core.Action
{
    public sealed class ActionResult
    {
        public bool Success { get; }
        public string ActionId { get; }
        public string Message { get; }

        public ActionResult(bool success, string actionId, string message)
        {
            Success = success;
            ActionId = actionId;
            Message = message;
        }

        public static ActionResult Ok(string actionId, string message)
        {
            return new ActionResult(true, actionId, message);
        }

        public static ActionResult Failed(string actionId, string message)
        {
            return new ActionResult(false, actionId, message);
        }
    }
}