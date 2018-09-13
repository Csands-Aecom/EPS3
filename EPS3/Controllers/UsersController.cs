using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using EPS3.DataContexts;
using EPS3.Models;
using EPS3.Helpers;
using EPS3.ViewModels;

namespace EPS3.Controllers
{
    public class UsersController : Controller
    {
        private readonly EPSContext _context;
        private PermissionsUtils _pu;
        public UsersController(EPSContext context)
        {
            _context = context;
            _pu = new PermissionsUtils(context);
        }

        // GET: Users
        public async Task<IActionResult> Index()
        {
            ViewBag.UserIsAdmin = UserIsAdmin();
            return View(await _context.Users.Include(u => u.Roles).ToListAsync());
        }

        // GET: Users/Details/5
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var user = await _context.Users
                .Include(u => u.Roles)
                .SingleOrDefaultAsync(m => m.UserID == id);
            if (user == null)
            {
                return NotFound();
            }

            return View(user);
        }

        //GET Users/Test
        public async Task<IActionResult> Test()
        {
            // *** DO NOT USE *** string userLoginSSP = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            string userLoginUIN = HttpContext.User.Identity.Name;

            List<User> users = new List<User>();
            //User user1 = await _context.Users
            //    .SingleOrDefaultAsync(m => m.UserLogin == userLoginSSP);
            User user2 = await _context.Users
                .SingleOrDefaultAsync(m => m.UserLogin == userLoginUIN);
            //users.Add(user1);
            users.Add(user2);
            return View(users);
        }

        // GET: Users/Create
        public IActionResult Create()
        {
            User user = GetCurrentUser();

            if (GetRolesList(user).Contains(ConstantStrings.AdminRole))
            {
                return View();
            }
            else
            {
                return RedirectToAction("Index", "Users");
            }
        }

        // POST: Users/Create
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("UserID,FirstName,LastName,UserLogin,Email,Phone,ReceiveEmails")] User user)
        {
            if (ModelState.IsValid)
            {
                //user.ReceiveEmails = 1;
                _context.Add(user);
                await _context.SaveChangesAsync();
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Edit/5
        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var UserVM = new UserRoleViewModel();

            User user = GetCurrentUser();
            if (GetRolesList(user).Contains(ConstantStrings.AdminRole))
            {
                UserVM.User = await _context.Users
                .Include(u => u.Roles)
                .SingleOrDefaultAsync(u => u.UserID == id);
                string userRoles = "";
                foreach (UserRole role in UserVM.User.Roles)
                {
                    userRoles += role.Role;
                }
                ViewBag.UserRoles = userRoles;
                List<String> rolesList = new List<String>
                {
                    ConstantStrings.Originator,
                    ConstantStrings.FinanceReviewer,
                    ConstantStrings.WPReviewer,
                    ConstantStrings.CFMSubmitter,
                    ConstantStrings.AdminRole
                };
                ViewBag.RolesList = rolesList;

                if (UserVM == null)
                {
                    return NotFound();
                }
                return View(UserVM);
            }
            else
            {
                return RedirectToAction("Index", "Users");
            }
        }

        // POST: Users/Edit/5
        // To protect from overposting attacks, please enable the specific properties you want to bind to, for 
        // more details see http://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("UserID,FirstName,LastName,UserLogin,Email,Phone,ReceiveEmails")] User user, string userRoles)
        {
            if (id != user.UserID)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(user);
                    await _context.SaveChangesAsync();

                    // add user roles
                    UpdateUserRoles(user, userRoles);
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!UserExists(user.UserID))
                    {
                        return NotFound();
                    }
                    else
                    {
                        throw;
                    }
                }
                return RedirectToAction(nameof(Index));
            }
            return View(user);
        }

        // GET: Users/Delete/5
        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            User user = GetCurrentUser();
            if (GetRolesList(user).Contains(ConstantStrings.AdminRole))
            {
                var userToDelete = await _context.Users
                    .Include(u => u.Roles)
                    .SingleOrDefaultAsync(m => m.UserID == id);
                if (user == null)
                {
                    return NotFound();
                }
                return View(user);
            }
            else
            {
                return RedirectToAction("Index", "Users");
            }
        }

        // POST: Users/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var user = await _context.Users.SingleOrDefaultAsync(m => m.UserID == id);
            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private bool UserExists(int id)
        {
            return _context.Users.Any(e => e.UserID == id);
        }

        private string GetLogin()
        {
            string userLogin = "";
            PermissionsUtils pu = new PermissionsUtils(_context);
            if (Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") == "Development")
            {
                userLogin = System.Security.Principal.WindowsIdentity.GetCurrent().Name.ToString();
            }
            else
            {
                userLogin = HttpContext.User.Identity.Name;
            }
            return pu.GetLogin(userLogin);
        }
        private User GetCurrentUser()
        {
            return _pu.GetUser(GetLogin());
        }

        private bool UserIsAdmin()
        {
            string userLogin = GetLogin();
            EPS3.Models.User currentUser = _context.Users.Include(u => u.Roles).SingleOrDefault(u => u.UserLogin == userLogin);
            foreach(UserRole role in currentUser.Roles)
            {
                if (role.Role.Equals(ConstantStrings.AdminRole)){
                    return true;
                }
            }
            return false;
        }

        private void UpdateUserRoles(User user, string userRoles)
        {
            List<string> possibleRoles = new List<string>();
            possibleRoles.Add(ConstantStrings.AdminRole);
            possibleRoles.Add(ConstantStrings.Originator);
            possibleRoles.Add(ConstantStrings.FinanceReviewer);
            possibleRoles.Add(ConstantStrings.WPReviewer);
            possibleRoles.Add(ConstantStrings.CFMSubmitter);

            string existingRoles = GetRolesList(user);
            foreach(string role in possibleRoles)
            {
                if (existingRoles.Contains(role))
                {
                    if (!userRoles.Contains(role))
                    {
                        //delete the role
                        RemoveRole(user, role);
                    }
                }
                else
                {
                    //not an existing role
                    if (userRoles.Contains(role))
                    {
                        //add the role
                        AddRole(user, role);
                    }
                }
            }
        }
        private void AddRole(User user, string roleName)
        {
            try
            {
                UserRole userRole = new UserRole();
                {
                    userRole.Role = roleName;
                    userRole.UserID = user.UserID;
                    userRole.BeginDate = DateTime.Now.Date;
                }
                _context.UserRoles.Add(userRole);
                _context.SaveChanges();
            }catch(Exception e)
            {
                throw;
            }
        }
        private void RemoveRole(User user, string roleName)
        {
            try
            {
                List<UserRole> roles = _context.UserRoles.Where(u => u.UserID == user.UserID).ToList();
                foreach (UserRole role in roles)
                {
                    if (role.Role.Equals(roleName))
                    {
                        _context.UserRoles.Remove(role);
                        _context.SaveChanges();
                    }
                }
            }
            catch
            {
                throw;
            }
        }
        public string GetRolesList(User user)
        {
            List<UserRole> userRoles = _context.UserRoles.Where(r => r.UserID == user.UserID).AsNoTracking().ToList();
            string roles = "";
            foreach (UserRole role in userRoles)
            {
                roles += role.Role;
            }
            return roles;
        }
    }
}
