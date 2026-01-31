using DailyJournal.Core.Services;
using DailyJournal.Data.Models;

namespace SabinDevkota.Data.Services
{
    // service class for user authentication and PIN management
    public class UserService : IUserService
    {
        // database service dependency
        private readonly IDatabaseService _databaseService;

        // constructor injecting the database service
        public UserService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        // ensuring the User table exists in the database
        private async Task InitializeTableAsync()
        {
            try
            {
                await _databaseService.Connection.CreateTableAsync<User>();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error initializing User table: {ex.Message}");
                throw;
            }
        }

        // checking if a valid 4 digit PIN has been set
        public async Task<bool> HasPinAsync()
        {
            try
            {
                await InitializeTableAsync();
                var user = await _databaseService.Connection.Table<User>().FirstOrDefaultAsync();
                // PIN must be a 4 digit number (1000 to 9999)
                return user is not null && user.Pin is >= 1000 and <= 9999;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error checking PIN existence: {ex.Message}");
                throw;
            }
        }

        // creating a new PIN for the user
        public async Task CreatePinAsync(int pin)
        {
            try
            {
                await InitializeTableAsync();

                // validating PIN is 4 digits
                if (pin < 1000 || pin > 9999)
                {
                    throw new ArgumentException("PIN must be a 4-digit number");
                }

                // clearing existing users before creating new one
                await _databaseService.Connection.DeleteAllAsync<User>();

                var user = new User
                {
                    Id = Guid.NewGuid(),
                    Pin = pin
                };

                await _databaseService.Connection.InsertAsync(user);
                System.Diagnostics.Debug.WriteLine($"PIN created successfully");
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error creating PIN: {ex.Message}");
                throw;
            }
        }

        // validating the entered PIN against the stored PIN
        public async Task<bool> ValidatePinAsync(int pin)
        {
            try
            {
                await InitializeTableAsync();
                var user = await _databaseService.Connection.Table<User>().FirstOrDefaultAsync();
                return user != null && user.Pin == pin;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error validating PIN: {ex.Message}");
                throw;
            }
        }

        // changing the PIN from old to new after validating old PIN
        public async Task<bool> ChangePinAsync(int oldPin, int newPin)
        {
            try
            {
                await InitializeTableAsync();

                System.Diagnostics.Debug.WriteLine($"ChangePinAsync called with oldPin: {oldPin}, newPin: {newPin}");

                // validating new PIN is 4 digits
                if (newPin < 1000 || newPin > 9999)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid new PIN format: {newPin}");
                    throw new ArgumentException("New PIN must be a 4-digit number");
                }

                var user = await _databaseService.Connection.Table<User>().FirstOrDefaultAsync();

                System.Diagnostics.Debug.WriteLine($"User found: {user != null}, User PIN: {user?.Pin}");

                // verifying old PIN matches before changing
                if (user == null || user.Pin != oldPin)
                {
                    System.Diagnostics.Debug.WriteLine("Old PIN validation failed");
                    return false;
                }

                // updating to new PIN
                user.Pin = newPin;
                var rowsAffected = await _databaseService.Connection.UpdateAsync(user);

                System.Diagnostics.Debug.WriteLine($"Update completed. Rows affected: {rowsAffected}");

                return true;
            }
            catch (ArgumentException)
            {
                throw;
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in ChangePinAsync: {ex.Message}");
                System.Diagnostics.Debug.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}