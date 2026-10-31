using System.Threading.Tasks;
using System.Net;
using RIMAPI.Core;
using RIMAPI.Models;
using RIMAPI.Services;

namespace RIMAPI.Controllers
{
    public class OrderController
    {
        private readonly IOrderService _orderService;

        public OrderController(IOrderService orderService)
        {
            _orderService = orderService;
        }

        [Post("/api/v1/order/designate/area")]
        public async Task DesignateArea(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<DesignateRequestDto>();
            var result = _orderService.DesignateArea(body);
            await context.SendJsonResponse(result);
        }

        // Roofing a rectangle (fox ticket #3418): a sealed room stays at the
        // outside temperature until a constructor builds the roof, so the order
        // marks the Build roof area and queues the game's own build-roof job.
        [Post("/api/v1/order/roof")]
        [EndpointMetadata("Mark the Build roof area over a rectangle and queue the game's own build-roof job, so a sealed room gets its roof")]
        public async Task BuildRoof(HttpListenerContext context)
        {
            var body = await context.Request.ReadBodyAsync<RoofRequestDto>();
            var result = _orderService.BuildRoof(body);
            await context.SendJsonResponse(result);
        }
    }
}
