using MediatR;
using Microsoft.AspNetCore.Mvc;
using PagueVeloz.Application.Commands.Customers;
using PagueVeloz.Application.Queries.Customers;

namespace PagueVeloz.API.Controllers;

/// <summary>
/// Controller for managing customers.
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class CustomerController : ControllerBase
{
    private readonly IMediator _mediator;

    public CustomerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    /// <summary>
    /// Creates a new customer.
    /// </summary>
    /// <param name="command">Customer creation data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The created customer.</returns>
    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCustomerCommand command, CancellationToken cancellationToken)
    {
        var customer = await _mediator.Send(command, cancellationToken);
        return CreatedAtAction(nameof(GetById), new { id = customer.Id }, customer);
    }

    /// <summary>
    /// Retrieves a customer by their ID.
    /// </summary>
    /// <param name="id">Customer ID.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>The customer details.</returns>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var customer = await _mediator.Send(new GetCustomerQuery(id), cancellationToken);
        if (customer is null) return NotFound();
        return Ok(customer);
    }
}
