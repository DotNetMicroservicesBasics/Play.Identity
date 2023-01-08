using MassTransit;
using Microsoft.AspNetCore.Identity;
using Play.Identity.Contracts;
using Play.Identity.Service.Entities;
using Play.Identity.Service.Exceptions;

namespace Play.Identity.Service.Consumers
{
    public class DebitGilConsumer : IConsumer<DebitGil>
    {

        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<DebitGilConsumer> _logger;

        public DebitGilConsumer(UserManager<ApplicationUser> userManager, ILogger<DebitGilConsumer> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task Consume(ConsumeContext<DebitGil> context)
        {
            var message = context.Message;
            _logger.LogInformation("Receive Grant Debit Gil Event of {Gil} from user {UserId} with CorrelationId {CorrelationId}", message.Gil, message.UserId, message.CorrelationId);
            var user = await _userManager.FindByIdAsync(message.UserId.ToString());
            if (user == null)
            {
                throw new UnknownUserException(message.UserId);
            }

            if (user.MessageIds.Contains(context.MessageId.Value))
            {
                await context.Publish(new GilDebited(message.CorrelationId));
                return;
            }
            user.MessageIds.Add(context.MessageId.Value);
            user.Gil -= message.Gil;

            if (user.Gil < 0)
            {
                _logger.LogWarning("Not enough gil to debit {gilToDebit} from user {userId} with CorrelationId {CorrelationId}", message.Gil, message.UserId, message.CorrelationId);
                throw new InsufficientUserGilException(message.UserId, message.Gil);
            }

            await _userManager.UpdateAsync(user);

            var gilDebitedTask = context.Publish(new GilDebited(message.CorrelationId));

            var userUpdatedTask = context.Publish(new UserUpdated(user.Id, user.Email, user.Gil));

            Task.WaitAll(gilDebitedTask, userUpdatedTask);

        }
    }
}