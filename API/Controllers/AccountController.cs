using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Dtos;
using API.Errors;
using API.Extensions;
using API.Helpers;
using AutoMapper;
using Core.Entities.Identity;
using Core.Interfaces;
using Core.Specifications;
using DataAccess.Identity;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace API.Controllers
{
    public class AccountController : BaseApiController
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly ITokenService _tokenService;
        private readonly IMapper _mapper;
        private readonly AppIdentityDbContext _context;

        public AccountController(UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, 
                    ITokenService tokenService, IMapper mapper, AppIdentityDbContext context)
        {
            _mapper = mapper;
            _tokenService = tokenService;
            _signInManager = signInManager;
            _userManager = userManager;
            _context = context;
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var user = await _userManager.FindByEmailFromClaimsPrinciple(HttpContext.User);

            return new UserDto
            {
                Email = user.Email,
                Token = await _tokenService.CreateToken(user),
                DisplayName = user.DisplayName
            };
        }

        [HttpGet("emailexists")]
        public async Task<ActionResult<bool>> CheckEmailExistsAsync([FromQuery] string email)
        {
            return await _userManager.FindByEmailAsync(email) != null;
        }

        [Authorize]
        [HttpGet("address")]
        public async Task<ActionResult<AddressDto>> GetUserAddress()
        {
            var user = await _userManager.FindByUserByClaimsPrincipleWithAddressAsync(HttpContext.User);

            return _mapper.Map<Address, AddressDto>(user.Address);
        }

        [Authorize]
        [HttpPut("address")]
        public async Task<ActionResult<AddressDto>> UpdateUserAddress(AddressDto address)
        {
            var user = await _userManager.FindByUserByClaimsPrincipleWithAddressAsync(HttpContext.User);

            user.Address = _mapper.Map<AddressDto, Address>(address);

            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded) return Ok(_mapper.Map<Address, AddressDto>(user.Address));

            return BadRequest("Problem updating the user");
        }

        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.FindByEmailAsync(loginDto.Email);

            if (user == null) return Unauthorized(new ApiResponse(401));

            var result = await _signInManager.CheckPasswordSignInAsync(user, loginDto.Password, false);

            if (!result.Succeeded) return Unauthorized(new ApiResponse(401));

            return new UserDto
            {
                Email = user.Email,
                Token = await _tokenService.CreateToken(user),
                DisplayName = user.DisplayName
            };
        }

        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (CheckEmailExistsAsync(registerDto.Email).Result.Value)
            {
                return new BadRequestObjectResult(new ApiValidationErrorResponse{Errors = new []{"Email address is in use"}});
            }

            var user = new AppUser
            {
                DisplayName = registerDto.DisplayName,
                Email = registerDto.Email,
                UserName = registerDto.Email
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (!result.Succeeded) return BadRequest(new ApiResponse(400));

            var roleAddResult = await _userManager.AddToRoleAsync(user, "Member");
            
            if (!roleAddResult.Succeeded) return BadRequest("Failed to add to role");

            return new UserDto
            {
                DisplayName = user.DisplayName,
                Token = await _tokenService.CreateToken(user),
                Email = user.Email
            };
        }

        private async Task<bool> CheckUserIsLockedOut(AppUser appUser)
        {
            var isLockedOut = await _userManager.IsLockedOutAsync(appUser);

            return isLockedOut;
        }

        private async Task<string> GetRolesNameListBySelectedUser(AppUser appUser)
        {
            var rolesNameList = await _userManager.GetRolesAsync(appUser);
            var strRolesList = "";

            foreach (var roleItem in rolesNameList)
            {
                strRolesList = strRolesList + roleItem + ", ";
            }

            if (!string.IsNullOrEmpty(strRolesList))
            {
                strRolesList = strRolesList.Substring(0, strRolesList.Length - 2);
            }

            return strRolesList;
        }

        private async Task<List<RolesListOfSelectedUser>> GetRolesListBySelectedUser(string userId)
        {
            // Get selected roles list checked by selected user
            var selectedRolesIds = await _context.UserRoles.Where(p => p.UserId == userId).Select(p => p.RoleId).ToListAsync();

            // Get Roles list from Database
            var rolesFromDB = await _context.Roles.Distinct().ToListAsync();

            // Return selected Roles within Roles list from Database
            List<RolesListOfSelectedUser> appRoles = new List<RolesListOfSelectedUser>();
            foreach (var itemRole in rolesFromDB)
            {
                RolesListOfSelectedUser newRole = new RolesListOfSelectedUser();
                newRole.Id = itemRole.Id;
                newRole.Name = itemRole.Name;
                newRole.SelectedRole = false;

                if (selectedRolesIds.Exists(str => str.Equals(itemRole.Id)))
                {
                    newRole.SelectedRole = true;
                }

                appRoles.Add(newRole);
            }

            return appRoles;
        }

        public Task<int> Complete()
        {
            return _context.SaveChangesAsync();
        }

        [HttpGet("userslist")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UsersWithRolesToReturnDto>> GetUsersList([FromQuery]UserSpecParams userParams)
        {
            int skip = userParams.PageSize * (userParams.PageIndex - 1);
            int take = userParams.PageSize;
            var query = _context.Users.Where(p => 1 == 1);
            
            if (!string.IsNullOrEmpty(userParams.Sort))
            {
                switch (userParams.Sort)
                {
                    case "usernameAsc":
                        query = query.OrderBy(p => p.UserName);
                        break;
                    case "usernameDesc":
                        query = query.OrderByDescending(p => p.UserName);
                        break;
                    default:
                        query = query.OrderBy(p => p.DisplayName);
                        break;
                }
            }

            query = query.Skip(skip).Take(take);

            var usersList = await query.ToListAsync();

            foreach (var perUser in usersList)
            {
                perUser.IsLockedOut = await CheckUserIsLockedOut(perUser);
                perUser.RolesNames = await GetRolesNameListBySelectedUser(perUser);
            }

            var totalItems = await _context.Users.CountAsync();

            var data = _mapper
                .Map<IReadOnlyList<AppUser>, IReadOnlyList<UsersWithRolesToReturnDto>>(usersList);

            return Ok(new Pagination<UsersWithRolesToReturnDto>(userParams.PageIndex, userParams.PageSize, totalItems, data));
        }
        
        [HttpGet("user/{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UsersWithRolesToReturnDto>> GetUser(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound(new ApiResponse(404));
            }

            var userFromDb = await _context.Users.SingleOrDefaultAsync(p => p.Id == id);
            if (userFromDb == null)
            {
                return NotFound(new ApiResponse(404));
            }

            userFromDb.IsLockedOut = await CheckUserIsLockedOut(userFromDb);
            userFromDb.RolesNames = await GetRolesNameListBySelectedUser(userFromDb);

            var userToUpdate = _mapper.Map<AppUser, UserToUpdate>(userFromDb);

            userToUpdate.RolesList = await GetRolesListBySelectedUser(id);

            return Ok(userToUpdate);
        }

        [HttpPut("{id}/update")]
        [Authorize(Roles = "Admin")]
        public async Task<ActionResult<UsersWithRolesToReturnDto>> UpdateUser(string id, UserToUpdate userToUpdate)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound(new ApiResponse(404));
            }

            var userFromDb = await _context.Users.SingleOrDefaultAsync(p => p.Id == id);
            if (userFromDb == null)
            {
                return NotFound(new ApiResponse(404));
            }
                        
            try
            {
                userFromDb.DisplayName = userToUpdate.DisplayName;
                userFromDb.PhoneNumber = userToUpdate.PhoneNumber;

                // Update roles list
                List<RolesListOfSelectedUser> appRoles = userToUpdate.RolesList;
                List<string> new_Roles = new List<string>();

                foreach (var itemRole in appRoles)
                {
                    if (itemRole.SelectedRole)
                    {
                        new_Roles.Add(itemRole.Name);
                    }
                }

                if (new_Roles.Count < 1)
                {
                    return BadRequest(new ApiResponse(400, "Please select at least one role."));
                }

                var old_Roles = await _userManager.GetRolesAsync(userFromDb);

                var result = await _userManager.RemoveFromRolesAsync(userFromDb, old_Roles);
                if (!result.Succeeded)
                    return BadRequest(new ApiResponse(400, "Failed to remove old roles."));

                result = await _userManager.AddToRolesAsync(userFromDb, new_Roles);
                if (!result.Succeeded)
                    return BadRequest(new ApiResponse(400, "Failed to add new roles."));

                _context.Entry(userFromDb).State = EntityState.Modified;

                var completed = await Complete();                
                if (completed <= 0) return BadRequest(new ApiResponse(400, "Problem in Update user!"));
                
                return Ok();
            }
            catch (Exception ex)
            {
                return BadRequest(new ApiResponse(400, "Problem in Update user! " + ex.Message));
            }
        }


        [HttpPost("{id}/lock")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> LockUser(string id, UserToLockOrUnlockDto appUser)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound(new ApiResponse(404));
            }

            var userFromDb = await _context.Users.SingleOrDefaultAsync(p => p.Id == id);
            if (userFromDb == null)
            {
                return NotFound(new ApiResponse(404));
            }

            userFromDb.LockoutEnd = DateTime.Now.AddYears(100);
            userFromDb.LockoutEnabled = true;
            userFromDb.LockoutReason = appUser.LockoutReason;
            userFromDb.AccessFailedCount = appUser.AccessFailedCount;
            userFromDb.IsLockedOut = true;

            _context.Entry(userFromDb).State = EntityState.Modified;

            var result = await Complete();
            if (result <= 0) return BadRequest(new ApiResponse(400, "Problem in Lock user!"));

            return Ok();
        }

        [HttpPost("{id}/unlock")]
        [Authorize(Roles = "Admin")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(ApiResponse), StatusCodes.Status404NotFound)]
        public async Task<ActionResult> UnLockUser(string id, UserToLockOrUnlockDto appUser)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound(new ApiResponse(404));
            }

            var userFromDb = await _context.Users.SingleOrDefaultAsync(p => p.Id == id);
            if (userFromDb == null)
            {
                return NotFound(new ApiResponse(404));
            }

            userFromDb.LockoutEnd = null;
            userFromDb.UnLockReason = appUser.UnLockReason;
            userFromDb.AccessFailedCount = appUser.AccessFailedCount;
            userFromDb.IsLockedOut = false;

            _context.Entry(userFromDb).State = EntityState.Modified;

            var result = await Complete();
            if (result <= 0) return BadRequest(new ApiResponse(400, "Problem in UnLock user!"));

            return Ok();
        }

    }
}
