using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DailyJournal.Core.Services
{
    public interface IUserService
    {
        Task<bool> HasPinAsync();
        Task CreatePinAsync(int pin);
        Task<bool> ValidatePinAsync(int pin);
    }
}
