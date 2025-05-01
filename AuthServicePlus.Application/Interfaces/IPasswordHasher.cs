using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServicePlus.Application.Interfaces
{
    public interface IPasswordHasher
    {
        string Hash(string password);
    }
}
