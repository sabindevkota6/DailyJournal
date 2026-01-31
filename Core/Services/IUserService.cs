namespace DailyJournal.Core.Services
{
    // Interface for user authentication and PIN management
    public interface IUserService
    {
        // Checking if a PIN has been set up
        Task<bool> HasPinAsync();

        // Creating a new PIN for the user
        Task CreatePinAsync(int pin);

        // Validating the entered PIN against stored PIN
        Task<bool> ValidatePinAsync(int pin);

        // Changing the PIN from old to new after validation
        Task<bool> ChangePinAsync(int oldPin, int newPin);
    }
}
