using System;
using System.Threading.Tasks;

namespace Utils.Abstractions
{
    public interface ICryptoValidator
    {
        Task<bool> Validate(string desiredSymbol, DateTime time);
    }
}