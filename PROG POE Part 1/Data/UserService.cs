using PROG_POE_Part_1.Data;
using PROG_POE_Part_1.Models;
using Microsoft.EntityFrameworkCore;

public class UserService
{
    private readonly AppDbContext _context;

    public UserService(AppDbContext context)
    {
        _context = context;
    }

    public List<User> GetUsers() => _context.Users.ToList();
    public User GetUserByID(int id) => _context.Users.Find(id);
    public bool AddUser(User user)
    {
        _context.Users.Add(user);
        return _context.SaveChanges() > 0;
    }
    public bool UpdateUser(User user)
    {
        _context.Users.Update(user);
        return _context.SaveChanges() > 0;
    }
    public User? ValidateLogin(string email, string password)
    {
        return _context.Users.FirstOrDefault(u => u.Email == email && u.Password == password);
    }

    public async Task<List<User>> GetUsersAsync() => await _context.Users.ToListAsync();

    public async Task<User?> GetUserByIDAsync(int id) => await _context.Users.FindAsync(id);

    public async Task<bool> AddUserAsync(User user)
    {
        await _context.Users.AddAsync(user);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<bool> UpdateUserAsync(User user)
    {
        _context.Users.Update(user);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<User?> ValidateLoginAsync(string email, string password)
    {
        return await _context.Users.FirstOrDefaultAsync(u => u.Email == email && u.Password == password);
    }
}
