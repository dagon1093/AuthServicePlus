using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace AuthServicePlus.Application.DTOs
{
    public class LoginUserDto
    {
        public string Username { get; set; } = null!;
        public string Password { get; set; } = null!;
    }
}
