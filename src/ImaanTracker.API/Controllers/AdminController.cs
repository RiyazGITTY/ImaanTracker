using ImaanTracker.API.DTOs;
using ImaanTracker.Core.Entities;
using ImaanTracker.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace ImaanTracker.API.Controllers;

[Authorize(Roles = "Admin")]
[ApiController]
[Route("api/[controller]")]
public class AdminController : ControllerBase
{
    private readonly AppDbContext _db;
    private readonly UserManager<ApplicationUser> _userManager;

    public AdminController(AppDbContext db, UserManager<ApplicationUser> userManager)
    {
        _db = db;
        _userManager = userManager;
    }

    private string CurrentUserId => User.FindFirstValue(ClaimTypes.NameIdentifier)!;

    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary()
    {
        var today = DateTime.UtcNow.Date;
        var totalUsers = await _db.Users.CountAsync();
        var totalPrayerLogs = await _db.DailyPrayerLogs.CountAsync();
        var completedToday = await _db.DailyPrayerLogs.CountAsync(l => l.LogDate.Date == today && l.IsPerfectDay);
        var totalPoints = await _db.Users.SumAsync(u => u.TotalPoints);

        return Ok(new
        {
            TotalUsers = totalUsers,
            TotalPrayerLogs = totalPrayerLogs,
            PerfectDaysToday = completedToday,
            TotalPoints = totalPoints
        });
    }

    [HttpGet("users")]
    public async Task<IActionResult> GetUsers()
    {
        var users = await _db.Users
            .OrderBy(u => u.FullName)
            .Select(u => new
            {
                u.Id,
                u.FullName,
                u.Email,
                u.PhoneNumber,
                u.City,
                u.Country,
                u.TotalPoints,
                u.ImaanLevel,
                u.CreatedAt,
                PrayerLogCount = u.PrayerLogs.Count
            })
            .ToListAsync();

        var result = new List<AdminUserDto>();
        foreach (var user in users)
        {
            var appUser = await _userManager.FindByIdAsync(user.Id);
            result.Add(new AdminUserDto(
                user.Id,
                user.FullName,
                user.Email ?? "",
                user.PhoneNumber,
                user.City,
                user.Country,
                user.TotalPoints,
                user.ImaanLevel,
                user.CreatedAt,
                appUser is not null && await _userManager.IsInRoleAsync(appUser, "Admin"),
                user.PrayerLogCount
            ));
        }

        return Ok(result);
    }

    [HttpPut("users/{id}")]
    public async Task<IActionResult> UpdateUser(string id, [FromBody] AdminUpdateUserDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound("User not found.");

        user.FullName = dto.FullName.Trim();
        user.PhoneNumber = dto.PhoneNumber?.Trim();
        user.City = dto.City.Trim();
        user.Country = dto.Country.Trim();
        user.TotalPoints = dto.TotalPoints;
        user.ImaanLevel = dto.ImaanLevel.Trim();

        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return NoContent();
    }

    [HttpPost("users/{id}/reset-password")]
    public async Task<IActionResult> ResetPassword(string id, [FromBody] AdminResetPasswordDto dto)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound("User not found.");

        var token = await _userManager.GeneratePasswordResetTokenAsync(user);
        var result = await _userManager.ResetPasswordAsync(user, token, dto.NewPassword);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return NoContent();
    }

    [HttpPost("users/{id}/make-admin")]
    public async Task<IActionResult> MakeAdmin(string id)
    {
        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound("User not found.");

        if (!await _userManager.IsInRoleAsync(user, "Admin"))
        {
            var result = await _userManager.AddToRoleAsync(user, "Admin");
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));
        }

        return NoContent();
    }

    [HttpPost("users/{id}/remove-admin")]
    public async Task<IActionResult> RemoveAdmin(string id)
    {
        if (id == CurrentUserId)
            return BadRequest("You cannot remove your own admin access.");

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound("User not found.");

        if (await _userManager.IsInRoleAsync(user, "Admin"))
        {
            var result = await _userManager.RemoveFromRoleAsync(user, "Admin");
            if (!result.Succeeded)
                return BadRequest(result.Errors.Select(e => e.Description));
        }

        return NoContent();
    }

    [HttpDelete("users/{id}")]
    public async Task<IActionResult> DeleteUser(string id)
    {
        if (id == CurrentUserId)
            return BadRequest("You cannot delete your own admin account.");

        var user = await _userManager.FindByIdAsync(id);
        if (user is null) return NotFound("User not found.");

        var result = await _userManager.DeleteAsync(user);
        if (!result.Succeeded)
            return BadRequest(result.Errors.Select(e => e.Description));

        return NoContent();
    }
}
