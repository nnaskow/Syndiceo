using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Syndiceo.Data.Models;
using SyndiceoWeb.Areas.Identity.Data;

public class DiscussionsController : Controller
{
    private readonly SyndiceoDBContext _context;
    private readonly UserManager<SyndiceoWebUser> _userManager;

    public DiscussionsController(SyndiceoDBContext context, UserManager<SyndiceoWebUser> userManager)
    {
        _context = context;
        _userManager = userManager;
    }

    public async Task<IActionResult> Index(int? id)
    {
        var discussions = await _context.Discussions
            .Include(d => d.Replies)
            .ThenInclude(r => r.User)
            .OrderByDescending(d => d.CreatedAt)
            .ToListAsync();

        ViewBag.SelectedId = id;
        var user = await _userManager.GetUserAsync(User);
        if (user != null)
        {
            user.LastDiscussionsView = DateTime.Now;
            await _userManager.UpdateAsync(user);
        }

        return View(discussions);
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Create(string title, string content, bool isClosed)
    {
        if (string.IsNullOrWhiteSpace(title) || string.IsNullOrWhiteSpace(content))
        {
            return RedirectToAction(nameof(Index));
        }

        var discussion = new Discussion
        {
            Title = title.Trim(),   
            Content = content.Trim(),
            IsClosed = isClosed,
            CreatedAt = DateTime.Now
        };

        _context.Discussions.Add(discussion);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { id = discussion.Id });
    }

    [HttpPost]
    public async Task<IActionResult> Reply(int discussionId, string message)
    {
        var discussion = await _context.Discussions.FindAsync(discussionId);
        if (discussion == null || discussion.IsClosed) return BadRequest();

        var reply = new DiscussionReply
        {
            Text = message,
            DiscussionId = discussionId,
            UserId = _userManager.GetUserId(User),
            CreatedAt = DateTime.Now
        };

        _context.DiscussionReplies.Add(reply);
        await _context.SaveChangesAsync();

        return RedirectToAction(nameof(Index), new { id = discussionId });
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> Delete(int id)
    {
        var discussion = await _context.Discussions
            .Include(d => d.Replies) 
            .FirstOrDefaultAsync(d => d.Id == id);

        if (discussion != null)
        {
            _context.DiscussionReplies.RemoveRange(discussion.Replies);

            _context.Discussions.Remove(discussion);

            await _context.SaveChangesAsync();
        }

        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> ToggleStatus(int id)
    {
        var discussion = await _context.Discussions.FindAsync(id);
        if (discussion != null)
        {
            discussion.IsClosed = !discussion.IsClosed;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index), new { id = id });
    }
}