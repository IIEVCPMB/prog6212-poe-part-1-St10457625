using PROG_POE_Part_1.Data;
using PROG_POE_Part_1.Models;
using Microsoft.EntityFrameworkCore;

public class ClaimService
{
    private readonly AppDbContext _context;

    public ClaimService(AppDbContext context)
    {
        _context = context;
    }

    // Get all claims for a lecturer
    public async Task<List<Claim>> GetClaimsByLecturerAsync(int lecturerId)
    {
        return await _context.Claims
            .Include(c => c.Documents)
            .Include(c => c.Reviews)
            .Where(c => c.Lecturer_ID == lecturerId)
            .ToListAsync();
    }

    // Get all claims
    public async Task<List<Claim>> GetAllClaimsAsync()
    {
        return await _context.Claims
            .Include(c => c.Reviews)
            .Include(c => c.Documents)
            .ToListAsync();
    }

    // Get claim by ID
    public async Task<Claim?> GetClaimByIDAsync(int id)
    {
        return await _context.Claims
            .Include(c => c.Documents)
            .Include(c => c.Reviews)
            .FirstOrDefaultAsync(c => c.Claim_ID == id);
    }

    // Get claims for a lecturer for a specific month
    public async Task<decimal> GetClaimsByLecturerMonthAsync(int lecturerId, int month, int year)
    {
        return await _context.Claims
            .Where(c => c.Lecturer_ID == lecturerId
                     && c.Date_Submitted.Month == month
                     && c.Date_Submitted.Year == year)
            .SumAsync(c => c.Total_Hours);
    }

    // Add claim
    public async Task<bool> AddClaimAsync(Claim claim)
    {
        await _context.Claims.AddAsync(claim);
        return await _context.SaveChangesAsync() > 0;
    }

    // Update claim
    public async Task<bool> UpdateClaimAsync(Claim claim)
    {
        _context.Claims.Update(claim);
        return await _context.SaveChangesAsync() > 0;
    }

    public async Task<List<Claim>> GetClaimsByStatusAsync(Status status)
    {
        return await _context.Claims
            .Include(c => c.Reviews)
            .Include(c => c.Documents)
            .Where(c => c.Status == status)
            .ToListAsync();
    }

    public async Task AddReviewAsync(ClaimReview review)
    {
        _context.ClaimReviews.Add(review);
        await _context.SaveChangesAsync();
    }

}
