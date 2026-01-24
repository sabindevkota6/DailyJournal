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
    }
}