using GoldBranchAI.Data;
using GoldBranchAI.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using System.Text.Json;

namespace GoldBranchAI.Controllers
{
    [Authorize]
    [Route("api/task-views")]
    public class TaskViewController : Controller
    {
        private readonly AppDbContext _context;

        public TaskViewController(AppDbContext context)
        {
            _context = context;
        }

        private int? CurrentUserId()
        {
            var raw = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(raw, out var id) ? id : null;
        }

        [HttpGet]
        public async Task<IActionResult> List()
        {
            var uid = CurrentUserId();
            if (uid == null) return Unauthorized();

            var views = await _context.TaskFilterViews
                .Where(v => v.AppUserId == uid.Value)
                .OrderByDescending(v => v.CreatedAt)
                .Select(v => new { v.Id, v.Name, v.StateJson, v.CreatedAt })
                .ToListAsync();

            return Json(new { views });
        }

        public class SaveTaskViewRequest
        {
            public string? Name { get; set; }
            public object? State { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Save([FromBody] SaveTaskViewRequest req)
        {
            var uid = CurrentUserId();
            if (uid == null) return Unauthorized();

            var name = (req.Name ?? "").Trim();
            if (string.IsNullOrWhiteSpace(name)) return BadRequest(new { message = "Name is required." });
            if (name.Length > 80) return BadRequest(new { message = "Name is too long." });
            if (req.State == null) return BadRequest(new { message = "State is required." });

            var json = JsonSerializer.Serialize(req.State);
            if (json.Length > 1200) return BadRequest(new { message = "State is too large." });

            var exists = await _context.TaskFilterViews.FirstOrDefaultAsync(v => v.AppUserId == uid.Value && v.Name == name);
            if (exists != null)
            {
                exists.StateJson = json;
                await _context.SaveChangesAsync();
                return Json(new { success = true, view = new { exists.Id, exists.Name, exists.StateJson, exists.CreatedAt } });
            }

            var view = new TaskFilterView
            {
                AppUserId = uid.Value,
                Name = name,
                StateJson = json,
                CreatedAt = DateTime.Now
            };

            _context.TaskFilterViews.Add(view);
            await _context.SaveChangesAsync();

            return Json(new { success = true, view = new { view.Id, view.Name, view.StateJson, view.CreatedAt } });
        }

        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var uid = CurrentUserId();
            if (uid == null) return Unauthorized();

            var view = await _context.TaskFilterViews.FirstOrDefaultAsync(v => v.Id == id && v.AppUserId == uid.Value);
            if (view == null) return NotFound(new { message = "Not found." });

            _context.TaskFilterViews.Remove(view);
            await _context.SaveChangesAsync();
            return Json(new { success = true });
        }
    }
}

