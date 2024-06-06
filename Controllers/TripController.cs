using APBDLab9.DTO;
using APBDLab9.Entities;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System;
using System.Linq;
using System.Threading.Tasks;
using APBDLab9.Context;

namespace APBDLab9.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TripsController : ControllerBase
    {
        private readonly TripsContext _context;
        private readonly ILogger<TripsController> _logger;

        public TripsController(TripsContext context, ILogger<TripsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult> GetTrips([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                if (_context.Trips == null)
                {
                    _logger.LogWarning("Trips data not available.");
                    return NotFound("Trips data not available.");
                }

                var trips = await _context.Trips
                    .OrderByDescending(t => t.DateFrom)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .Select(t => new TripDto
                    {
                        Name = t.Name,
                        Description = t.Description,
                        DateFrom = t.DateFrom,
                        DateTo = t.DateTo,
                        MaxPeople = t.MaxPeople,
                        Countries = t.IdCountries.Select(ct => new CountryDto
                        {
                            Name = ct.Name
                        }).ToList(),
                        Clients = t.ClientTrips.Select(ct => new ClientDto
                        {
                            FirstName = ct.IdClientNavigation.FirstName,
                            LastName = ct.IdClientNavigation.LastName
                        }).ToList()
                    })
                    .ToListAsync();

                var totalTrips = await _context.Trips.CountAsync();
                var totalPages = (int)Math.Ceiling(totalTrips / (double)pageSize);

                return Ok(new
                {
                    pageNum = page,
                    pageSize,
                    allPages = totalPages,
                    trips
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred.");
                return StatusCode(500, "Server error");
            }
        }

        [HttpPost("{idTrip}/clients")]
        public async Task<ActionResult> AddClientToTrip([FromBody] TripaddClientDto tripAddClientDto, int idTrip)
        {
            if (tripAddClientDto == null)
            {
                return BadRequest("Client data required.");
            }

            try
            {
                var client = await _context.Clients.FirstOrDefaultAsync(c => c.Pesel == tripAddClientDto.Pesel);
                if (client == null)
                {
                    return BadRequest("Client with the given PESEL number not exist.");
                }

                var trip = await _context.Trips.FirstOrDefaultAsync(t => t.IdTrip == idTrip);
                if (trip == null)
                {
                    return BadRequest("Trip does not exist.");
                }

                if (trip.DateFrom < DateTime.Now)
                {
                    return BadRequest("The trip already occurred.");
                }

                var clientTrip = await _context.ClientTrips.FirstOrDefaultAsync(ct => ct.IdClient == client.IdClient && ct.IdTrip == trip.IdTrip);
                if (clientTrip != null)
                {
                    return BadRequest("The client is already registered for the trip.");
                }

                var newClientTrip = new ClientTrip
                {
                    IdClient = client.IdClient,
                    IdTrip = trip.IdTrip,
                    RegisteredAt = DateTime.Now,
                    PaymentDate = tripAddClientDto.PaymentDate
                };

                _context.ClientTrips.Add(newClientTrip);
                await _context.SaveChangesAsync();

                return Ok("Client added to the trip.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred .");
                return StatusCode(500, "Server error");
            }
        }
    }
}
