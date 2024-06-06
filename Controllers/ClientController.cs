using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using APBDLab9.Context;

namespace APBDLab9.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ClientsController : ControllerBase
    {
        private readonly TripsContext _dbContext;
        private readonly ILogger<ClientsController> _logger;

        public ClientsController(TripsContext dbContext, ILogger<ClientsController> logger)
        {
            _dbContext = dbContext;
            _logger = logger;
        }
        
        private async Task<bool> ClientExists(int id) 
        {
            return await _dbContext.Clients.AnyAsync(e => e.IdClient == id);
        }
        
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteClientById(int id)
        {
            try
            {
                var client = await _dbContext.Clients.FindAsync(id);
                if (client == null)
                {
                    _logger.LogWarning($"Client ID: {id} not found.");
                    return NotFound("Client not found.");
                }
                _dbContext.Clients.Remove(client);
                await _dbContext.SaveChangesAsync();
                _logger.LogInformation($"Client ID: {id} deleted.");
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error occurred while deleting client ID: {id}.");
                return StatusCode(500, "Server error");
            }
        }
    }
}