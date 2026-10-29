namespace Focus.Application.Common.Exceptions;

public class AiBudgetExceededException : Exception
{
    public AiBudgetExceededException()
        : base("Gunluk yapay zeka kullanim kotasina ulastin. Yarin tekrar deneyebilirsin.")
    {
    }
}
