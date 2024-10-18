using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using ToDoApp.Data;

namespace ToDoApp.Hubs
{
    public class SignalHub : Hub
    {
        private readonly ApplicationDbContext _context;
        public SignalHub(ApplicationDbContext context)
        {
            _context = context;
        }

    }
}
