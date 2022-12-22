using System.Runtime.Serialization;

namespace Play.Identity.Service.Exceptions
{
    [Serializable]
    internal class InsufficientUserGilException : Exception
    {
        public Guid UserId;
        public decimal GilToDebit;
        public InsufficientUserGilException(Guid userId, decimal gilToDebit) : base($"Not enough gil to debit {gilToDebit} from user '{userId}'")
        {
            UserId = userId;
            GilToDebit = gilToDebit;
        }
    }
}