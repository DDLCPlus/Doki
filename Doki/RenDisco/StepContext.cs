namespace RenDisco
{
    public class StepContext(int? choice = null)
    {
        public int? Choice { get; set; } = choice;
        public bool Proceed { get; set; } = true;
    }
}