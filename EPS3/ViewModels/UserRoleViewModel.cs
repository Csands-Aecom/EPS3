using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using EPS3.Models;

namespace EPS3.ViewModels
{
    public class UserRoleViewModel
    {
        public User User { get; set; }
        public ICollection<UserRole> UserRoles { get; set; }
    }
}
