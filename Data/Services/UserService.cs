using DailyJournal.Core.Services;
using DailyJournal.Data.Models;
using System;
using System.Threading.Tasks;

namespace SabinDevkota.Data.Services
{
    public class UserService : IUserService
    {
        private readonly IDatabaseService _databaseService;

        public UserService(IDatabaseService databaseService)
        {
            _databaseService = databaseService;
        }

        private async Task InitializeTableAsync()
        {
            await _databaseService.Connection.CreateTableAsync<User>();
        }

        public async Task<bool> HasPinAsync()
        {
            await InitializeTableAsync();
            var user = await _databaseService.Connection.Table<User>().FirstOrDefaultAsync();
            return user is not null && user.Pin is >= 1000 and <= 9999;
        }

        public async Task CreatePinAsync(int pin)
        {
            await InitializeTableAsync();

          
            await _databaseService.Connection.DeleteAllAsync<User>();

            var user = new User
            {
                Id = Guid.NewGuid(),
                Pin = pin
            };

            await _databaseService.Connection.InsertAsync(user);
        }

        public async Task<bool> ValidatePinAsync(int pin)
        {
            await InitializeTableAsync();

            var user = await _databaseService.Connection.Table<User>().FirstOrDefaultAsync();
            return user != null && user.Pin == pin;
        }

        public async Task<bool> ChangePinAsync(int oldPin, int newPin)
        {
            try
            {
                await InitializeTableAsync();

                System.Diagnostics.Debug.WriteLine($"ChangePinAsync called with oldPin: {oldPin}, newPin: {newPin}");

                // Validate new PIN format first
                if (newPin < 1000 || newPin > 9999)
                {
                    System.Diagnostics.Debug.WriteLine($"Invalid new PIN format: {newPin}");
                    throw new ArgumentException("New PIN must be a 4-digit number");
                }

                // Validate old PIN first
                var user = await _databaseService.Connection.Table<User>().FirstOrDefaultAsync();
        
                System.Diagnostics.Debug.WriteLine($"User found: {user != null}, User PIN: {user?.Pin}");
                
                if (user == null || user.Pin != oldPin)
                {
                    System.Diagnostics.Debug.WriteLine("Old PIN validation failed");
                    return false; // Old PIN is incorrect
                }

                // Update PIN
                user.Pin = newPin;
                var rowsAffected = await _databaseService.Connection.UpdateAsync(user);
      
                System.Diagnostics.Debug.WriteLine($"Update completed. Rows affected: {rowsAffected}");
        
                return true;
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