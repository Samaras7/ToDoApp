using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using ToDoApp.Data;
using ToDoApp.Hubs;
using ToDoApp.Models;

namespace ToDoApp.Controllers
{
    public class TasksController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IHubContext<SignalHub> _hubContext;

        public TasksController(ApplicationDbContext context, IHubContext<SignalHub> hubContext)
        {
            _context = context;
            _hubContext = hubContext;
        }

        public async System.Threading.Tasks.Task SendNotifications()
        {
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var tasksForTodayCount = await _context.Tasks.CountAsync(t => t.DueDate.Date == today && !t.IsCompleted);
            var tasksForTomorrowCount = await _context.Tasks.CountAsync(t => t.DueDate.Date == tomorrow && !t.IsCompleted);

            await _hubContext.Clients.All.SendAsync("ReceiveMessage", tasksForTodayCount, tasksForTomorrowCount);
        }



        public async Task<IActionResult> Index(DateTime? date)
        {
            DateTime selectedDate = date ?? DateTime.Today;
            ViewBag.SelectedDate = selectedDate;

            var tasks = await _context.Tasks
                .Where(t => t.DueDate.Date == selectedDate.Date)
                .ToListAsync();

            await SendNotifications();

            return View(tasks);
        }

        public IActionResult Create(DateTime? date)
        {
            ViewBag.SelectedDate = date ?? DateTime.Today;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create([Bind("Id,Title,Description,DueDate,IsCompleted")] Models.Task task)
        {
            if (ModelState.IsValid)
            {
                _context.Add(task);
                await _context.SaveChangesAsync();
                await SendNotifications();
                return RedirectToAction(nameof(Index)); 
            }

            return View(task); 
        }


        public async Task<IActionResult> Edit(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FindAsync(id);
            if (task == null)
            {
                return NotFound();
            }
            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,DueDate,IsCompleted")] Models.Task task)
        {
            if (id != task.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                try
                {
                    _context.Update(task);
                    await _context.SaveChangesAsync();
                    await SendNotifications();
                    return RedirectToAction(nameof(Index));
                }
                catch (DbUpdateConcurrencyException)
                {
                    if (!TaskExists(task.Id))
                    {
                        return NotFound();
                    }
                    throw; 
                }
            }
            return View(task);
        }

        public async Task<IActionResult> Delete(int? id)
        {
            if (id == null)
            {
                return NotFound();
            }

            var task = await _context.Tasks.FirstOrDefaultAsync(m => m.Id == id);
            if (task == null)
            {
                return NotFound();
            }

            return View(task);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
                await SendNotifications();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> UpdateTaskStatus([FromBody] TaskStatusUpdateModel model)
        {
            var task = await _context.Tasks.FindAsync(model.Id);
            if (task == null)
            {
                return NotFound();
            }

            task.IsCompleted = model.IsCompleted;
            _context.Tasks.Update(task);
            await _context.SaveChangesAsync();

            await SendNotifications();
            return Ok();
        }

        public class TaskStatusUpdateModel
        {
            public int Id { get; set; }
            public bool IsCompleted { get; set; }
        }

        private bool TaskExists(int id)
        {
            return _context.Tasks.Any(e => e.Id == id);
        }
    }
}
